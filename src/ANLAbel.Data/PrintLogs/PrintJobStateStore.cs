using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Collections.Concurrent;
using ANLAbel.Core.Printing;

namespace ANLAbel.Data.PrintLogs;

/// <summary>
/// Durable append-only lifecycle store for print jobs. Each event is sequenced per
/// job and chained to the previous event hash, so a crash/reopen can recover the
/// latest known state without treating a partial or corrupt tail as success.
/// </summary>
public sealed class PrintJobStateStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> SharedWriteGates = new(StringComparer.OrdinalIgnoreCase);
    private readonly SemaphoreSlim _writeGate;
    private readonly Dictionary<string, RecoveryState> _states = new(StringComparer.Ordinal);
    private readonly List<string> _recoveryDiagnostics = new();
    private bool _loaded;

    public PrintJobStateStore()
        : this(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ANLAbel",
            "logs",
            "print-job-events.jsonl"))
    {
    }

    public PrintJobStateStore(string logFilePath)
    {
        if (string.IsNullOrWhiteSpace(logFilePath))
        {
            throw new ArgumentException("A state log path is required.", nameof(logFilePath));
        }

        LogFilePath = Path.GetFullPath(logFilePath);
        _writeGate = SharedWriteGates.GetOrAdd(LogFilePath, static _ => new SemaphoreSlim(1, 1));
    }

    public string LogFilePath { get; }

    /// <summary>
    /// Non-fatal diagnostics found while replaying a corrupt/incomplete tail. The
    /// valid prefix remains available, but callers can surface the warning to an
    /// operator instead of silently rebuilding or retrying a job.
    /// </summary>
    public IReadOnlyList<string> RecoveryDiagnostics
    {
        get
        {
            lock (_states)
            {
                return _recoveryDiagnostics.ToArray();
            }
        }
    }

    public async Task<PrintJobStateEvent> AppendAsync(
        PrintJobStateTransition transition,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(transition);
        await _writeGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // Multiple windows/services may share the same local event file. The
            // process-wide gate serializes writers; replay immediately before each
            // append so a second store cannot reuse a stale sequence/hash prefix.
            ReloadFromDisk();
            lock (_states)
            {
                if (_recoveryDiagnostics.Count > 0)
                {
                    throw new InvalidOperationException(
                        "Print-job event log has an invalid tail; append is blocked until an operator repairs or archives the log.");
                }
            }
            _states.TryGetValue(transition.JobId, out var current);
            PrintJobStateMachine.ValidateTransition(transition, current?.State);

            var sequence = (current?.Sequence ?? 0) + 1;
            var previousHash = current?.Hash ?? string.Empty;
            var stateEvent = new PrintJobStateEvent(
                transition.JobId,
                sequence,
                transition.From,
                transition.To,
                transition.TimestampUtc,
                transition.Reason,
                transition.PrinterName,
                transition.SpoolJobId,
                transition.QueueState,
                transition.DocumentHash,
                transition.SceneHash,
                transition.OutputContractHash,
                transition.PhysicalOutputVerified,
                previousHash,
                string.Empty,
                transition.OperatorAction,
                transition.RelatedJobId,
                transition.Actor,
                transition.TextResourceFingerprint,
                transition.ManifestFingerprint,
                transition.Manifest,
                transition.VerificationEvidence);
            if (stateEvent.Manifest is not null
                && (!string.Equals(stateEvent.Manifest.Fingerprint, stateEvent.ManifestFingerprint, StringComparison.Ordinal)
                    || !stateEvent.Manifest.IsFingerprintValid))
            {
                throw new InvalidOperationException("The print-job manifest metadata does not match its fingerprint.");
            }
            stateEvent = stateEvent with { IntegrityHash = ComputeHash(stateEvent) };

            await AppendLineAsync(stateEvent, cancellationToken).ConfigureAwait(false);
            lock (_states)
            {
                _states[transition.JobId] = new RecoveryState(
                    sequence,
                    transition.To,
                    stateEvent.IntegrityHash,
                    stateEvent);
            }

            return stateEvent;
        }
        finally
        {
            _writeGate.Release();
        }
    }

    public PrintJobLifecycleState? GetCurrentState(string jobId)
    {
        if (string.IsNullOrWhiteSpace(jobId))
        {
            return null;
        }

        EnsureLoaded();
        lock (_states)
        {
            return _states.TryGetValue(jobId, out var state) ? state.State : null;
        }
    }

    public async Task<IReadOnlyList<PrintJobStateEvent>> ReadEventsAsync(
        string jobId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(jobId))
        {
            return Array.Empty<PrintJobStateEvent>();
        }

        await _writeGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            EnsureLoaded();
            if (!File.Exists(LogFilePath))
            {
                return Array.Empty<PrintJobStateEvent>();
            }

            var lines = await File.ReadAllLinesAsync(LogFilePath, cancellationToken).ConfigureAwait(false);
            var events = new List<PrintJobStateEvent>();
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                try
                {
                    var item = JsonSerializer.Deserialize<PrintJobStateEvent>(line, JsonOptions);
                    if (item is not null && string.Equals(item.JobId, jobId, StringComparison.Ordinal))
                    {
                        events.Add(item);
                    }
                }
                catch (JsonException)
                {
                    // The replay diagnostics are populated by EnsureLoaded; reading
                    // a known job returns the valid prefix rather than failing open.
                }
            }

            return events.OrderBy(item => item.Sequence).ToArray();
        }
        finally
        {
            _writeGate.Release();
        }
    }

    /// <summary>
    /// Replays the event file under the shared writer gate and returns the last
    /// valid event for every job. A caller uses this snapshot to decide whether
    /// a job can be queried again or must be reviewed by an operator; it is never
    /// a signal to retry automatically.
    /// </summary>
    public async Task<PrintJobRecoverySnapshot> ReadRecoverySnapshotAsync(
        CancellationToken cancellationToken = default)
    {
        await _writeGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            ReloadFromDisk();
            lock (_states)
            {
                return new PrintJobRecoverySnapshot(
                    _states.Values
                        .Select(state => state.LastEvent)
                        .OrderByDescending(item => item.TimestampUtc)
                        .ThenBy(item => item.JobId, StringComparer.Ordinal)
                        .ToArray(),
                    _recoveryDiagnostics.ToArray());
            }
        }
        finally
        {
            _writeGate.Release();
        }
    }

    public async Task WaitForPendingWritesAsync(CancellationToken cancellationToken = default)
    {
        await _writeGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        _writeGate.Release();
    }

    private void EnsureLoaded()
    {
        lock (_states)
        {
            if (_loaded)
            {
                return;
            }

            _loaded = true;
            if (!File.Exists(LogFilePath))
            {
                return;
            }

            try
            {
                foreach (var line in File.ReadLines(LogFilePath))
                {
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }

                    PrintJobStateEvent? item;
                    try
                    {
                        item = JsonSerializer.Deserialize<PrintJobStateEvent>(line, JsonOptions);
                    }
                    catch (JsonException ex)
                    {
                        _recoveryDiagnostics.Add($"Ignored malformed print-job event tail: {ex.Message}");
                        break;
                    }

                    if (item is null || string.IsNullOrWhiteSpace(item.JobId))
                    {
                        _recoveryDiagnostics.Add("Ignored an empty print-job event record.");
                        break;
                    }

                    if (item.Manifest is not null
                        && (!string.Equals(item.Manifest.Fingerprint, item.ManifestFingerprint, StringComparison.Ordinal)
                            || !item.Manifest.IsFingerprintValid))
                    {
                        _recoveryDiagnostics.Add($"Stopped replay at an event with invalid manifest metadata for job '{item.JobId}'.");
                        break;
                    }

                    if (item.VerificationEvidence is not null
                        && !item.VerificationEvidence.IsFingerprintValid)
                    {
                        _recoveryDiagnostics.Add($"Stopped replay at an event with invalid physical-verification evidence for job '{item.JobId}'.");
                        break;
                    }

                    _states.TryGetValue(item.JobId, out var current);
                    var expectedSequence = (current?.Sequence ?? 0) + 1;
                    var expectedPreviousHash = current?.Hash ?? string.Empty;
                    if (item.Sequence != expectedSequence
                        || !string.Equals(item.PreviousHash, expectedPreviousHash, StringComparison.Ordinal)
                        || !IsIntegrityHashValid(item))
                    {
                        _recoveryDiagnostics.Add($"Stopped replay at invalid event for job '{item.JobId}' sequence {item.Sequence}.");
                        break;
                    }

                    _states[item.JobId] = new RecoveryState(
                        item.Sequence,
                        item.To,
                        item.IntegrityHash,
                        item);
                }
            }
            catch (IOException ex)
            {
                _recoveryDiagnostics.Add($"Could not replay print-job events: {ex.Message}");
            }
            catch (UnauthorizedAccessException ex)
            {
                _recoveryDiagnostics.Add($"Could not access print-job events: {ex.Message}");
            }
        }
    }

    private void ReloadFromDisk()
    {
        lock (_states)
        {
            _states.Clear();
            _recoveryDiagnostics.Clear();
            _loaded = false;
        }

        EnsureLoaded();
    }

    private async Task AppendLineAsync(PrintJobStateEvent stateEvent, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(LogFilePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var line = JsonSerializer.Serialize(stateEvent, JsonOptions) + Environment.NewLine;
        var bytes = Encoding.UTF8.GetBytes(line);
        await using var stream = new FileStream(
            LogFilePath,
            FileMode.Append,
            FileAccess.Write,
            FileShare.Read,
            bufferSize: 4096,
            options: FileOptions.Asynchronous | FileOptions.WriteThrough);
        await stream.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
        await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    private static string ComputeHash(PrintJobStateEvent stateEvent)
    {
        var canonical = string.Join("|", new[]
        {
            stateEvent.JobId,
            stateEvent.Sequence.ToString(System.Globalization.CultureInfo.InvariantCulture),
            stateEvent.From.ToString(),
            stateEvent.To.ToString(),
            stateEvent.TimestampUtc.ToUniversalTime().ToString("O", System.Globalization.CultureInfo.InvariantCulture),
            stateEvent.Reason,
            stateEvent.PrinterName,
            stateEvent.SpoolJobId?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty,
            stateEvent.QueueState,
            stateEvent.DocumentHash,
            stateEvent.SceneHash,
            stateEvent.OutputContractHash,
            stateEvent.PhysicalOutputVerified ? "1" : "0",
            stateEvent.OperatorAction.ToString(),
            stateEvent.RelatedJobId,
            stateEvent.Actor,
            stateEvent.TextResourceFingerprint,
            stateEvent.ManifestFingerprint,
            stateEvent.VerificationEvidence?.Fingerprint ?? string.Empty,
            stateEvent.PreviousHash
        });
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant();
    }

    private static bool IsIntegrityHashValid(PrintJobStateEvent stateEvent)
    {
        // Events written before the operator-lineage fields were introduced did
        // not include those fields in their canonical hash. Accept that exact
        // legacy form so an existing user's event file can be upgraded safely;
        // v0.108 events included text-resource identity but not the manifest;
        // every newly appended event uses the current canonical form below.
        return string.Equals(stateEvent.IntegrityHash, ComputeHash(stateEvent), StringComparison.Ordinal)
            || string.Equals(stateEvent.IntegrityHash, ComputePreviousCurrentHash(stateEvent), StringComparison.Ordinal)
            || string.Equals(stateEvent.IntegrityHash, ComputeLegacyHash(stateEvent), StringComparison.Ordinal);
    }

    private static string ComputePreviousCurrentHash(PrintJobStateEvent stateEvent)
    {
        var canonical = string.Join("|", new[]
        {
            stateEvent.JobId,
            stateEvent.Sequence.ToString(System.Globalization.CultureInfo.InvariantCulture),
            stateEvent.From.ToString(),
            stateEvent.To.ToString(),
            stateEvent.TimestampUtc.ToUniversalTime().ToString("O", System.Globalization.CultureInfo.InvariantCulture),
            stateEvent.Reason,
            stateEvent.PrinterName,
            stateEvent.SpoolJobId?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty,
            stateEvent.QueueState,
            stateEvent.DocumentHash,
            stateEvent.SceneHash,
            stateEvent.OutputContractHash,
            stateEvent.PhysicalOutputVerified ? "1" : "0",
            stateEvent.OperatorAction.ToString(),
            stateEvent.RelatedJobId,
            stateEvent.Actor,
            stateEvent.TextResourceFingerprint,
            stateEvent.PreviousHash
        });
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant();
    }

    private static string ComputeLegacyHash(PrintJobStateEvent stateEvent)
    {
        var canonical = string.Join("|", new[]
        {
            stateEvent.JobId,
            stateEvent.Sequence.ToString(System.Globalization.CultureInfo.InvariantCulture),
            stateEvent.From.ToString(),
            stateEvent.To.ToString(),
            stateEvent.TimestampUtc.ToUniversalTime().ToString("O", System.Globalization.CultureInfo.InvariantCulture),
            stateEvent.Reason,
            stateEvent.PrinterName,
            stateEvent.SpoolJobId?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty,
            stateEvent.QueueState,
            stateEvent.DocumentHash,
            stateEvent.SceneHash,
            stateEvent.OutputContractHash,
            stateEvent.PhysicalOutputVerified ? "1" : "0",
            stateEvent.PreviousHash
        });
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant();
    }

    private sealed record RecoveryState(
        long Sequence,
        PrintJobLifecycleState State,
        string Hash,
        PrintJobStateEvent LastEvent);
}

/// <summary>
/// Serialized durable event. Hash fields are generated by <see cref="PrintJobStateStore"/>;
/// callers cannot choose a sequence or mark a physical completion implicitly.
/// </summary>
public sealed record PrintJobStateEvent(
    string JobId,
    long Sequence,
    PrintJobLifecycleState From,
    PrintJobLifecycleState To,
    DateTimeOffset TimestampUtc,
    string Reason,
    string PrinterName,
    int? SpoolJobId,
    string QueueState,
    string DocumentHash,
    string SceneHash,
    string OutputContractHash,
    bool PhysicalOutputVerified,
    string PreviousHash,
    string IntegrityHash,
    PrintJobOperatorAction OperatorAction = PrintJobOperatorAction.None,
    string RelatedJobId = "",
    string Actor = "",
    string TextResourceFingerprint = "",
    string ManifestFingerprint = "",
    PrintJobManifest? Manifest = null,
    PhysicalOutputVerificationEvidence? VerificationEvidence = null);
