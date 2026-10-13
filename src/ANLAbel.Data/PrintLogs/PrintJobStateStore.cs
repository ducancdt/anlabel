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
    private readonly List<string> _repairNotices = new();
    private bool _loaded;
    // Bytes of the file covered by validated events, and the file time seen
    // then. An append re-reads the file only when either changed (another
    // store or process wrote), instead of replaying months of events on every
    // print (plan 13 H18).
    private long _loadedLength;
    private DateTime _loadedLastWriteUtc;
    private TornTail? _tornTail;

    public PrintJobStateStore()
        : this(AppDataPaths.Log("print-job-events.jsonl"))
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

    /// <summary>
    /// Automatic or operator repairs done by this store: what was moved aside and
    /// where the original bytes were kept. Informational; printing is not blocked.
    /// </summary>
    public IReadOnlyList<string> RepairNotices
    {
        get
        {
            lock (_states)
            {
                return _repairNotices.ToArray();
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
            // process-wide gate serializes writers; the file is replayed again
            // whenever it changed since this store last read it, so a second
            // store cannot reuse a stale sequence/hash prefix. The work runs off
            // the caller's (UI) thread.
            await Task.Run(RefreshFromDiskIfChanged, cancellationToken).ConfigureAwait(false);
            if (TryRepairTornTail())
            {
                await Task.Run(ReloadFromDisk, cancellationToken).ConfigureAwait(false);
            }

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

            var expectedLength = _loadedLength + await AppendLineAsync(stateEvent, cancellationToken).ConfigureAwait(false);
            lock (_states)
            {
                _states[transition.JobId] = new RecoveryState(
                    sequence,
                    transition.To,
                    stateEvent.IntegrityHash,
                    stateEvent);
                var info = new FileInfo(LogFilePath);
                if (info.Exists && info.Length == expectedLength)
                {
                    _loadedLength = expectedLength;
                    _loadedLastWriteUtc = info.LastWriteTimeUtc;
                }
                else
                {
                    // Someone else appended too: replay before the next append.
                    _loadedLastWriteUtc = default;
                }
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
            await Task.Run(ReloadFromDisk, cancellationToken).ConfigureAwait(false);
            if (TryRepairTornTail())
            {
                await Task.Run(ReloadFromDisk, cancellationToken).ConfigureAwait(false);
            }

            lock (_states)
            {
                return new PrintJobRecoverySnapshot(
                    _states.Values
                        .Select(state => state.LastEvent)
                        .OrderByDescending(item => item.TimestampUtc)
                        .ThenBy(item => item.JobId, StringComparer.Ordinal)
                        .ToArray(),
                    _recoveryDiagnostics.ToArray(),
                    _repairNotices.ToArray());
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

    /// <summary>
    /// Operator action for a damaged log (plan 13 GĐ5, Figma 37:280). The whole
    /// file is copied to <c>print-job-events.hong-&lt;time&gt;.jsonl</c>; the active
    /// log keeps only the validated prefix, so every job state before the damage
    /// stays reviewable and printing can continue. Returns the archive path, or
    /// null when the log was not damaged.
    /// </summary>
    public async Task<string?> ArchiveDamagedLogAndStartNewAsync(CancellationToken cancellationToken = default)
    {
        await _writeGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await Task.Run(() =>
            {
                ReloadFromDisk();
                long validPrefix;
                lock (_states)
                {
                    if (_recoveryDiagnostics.Count == 0 || !File.Exists(LogFilePath))
                    {
                        return null;
                    }

                    validPrefix = _loadedLength;
                }

                var bytes = ReadAllBytesWithRetry(LogFilePath);
                var archive = CreateArchivePath();
                File.WriteAllBytes(archive, bytes);
                var temporary = LogFilePath + "." + Guid.NewGuid().ToString("N") + ".tmp";
                using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough))
                {
                    stream.Write(bytes, 0, (int)Math.Min(validPrefix, bytes.Length));
                    stream.Flush(flushToDisk: true);
                }

                File.Move(temporary, LogFilePath, overwrite: true);
                lock (_states)
                {
                    _repairNotices.Add($"Đã lưu trữ nhật ký lệnh in bị hỏng thành {Path.GetFileName(archive)} và tiếp tục với phần còn hợp lệ.");
                }

                ReloadFromDisk();
                return archive;
            }, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _writeGate.Release();
        }
    }

    private void RefreshFromDiskIfChanged()
    {
        lock (_states)
        {
            if (_loaded && _recoveryDiagnostics.Count == 0)
            {
                var info = new FileInfo(LogFilePath);
                var length = info.Exists ? info.Length : 0;
                var lastWrite = info.Exists ? info.LastWriteTimeUtc : default;
                if (length == _loadedLength && lastWrite == _loadedLastWriteUtc)
                {
                    return;
                }
            }
        }

        ReloadFromDisk();
    }

    /// <summary>
    /// A last line without its newline is a write cut off by power loss or a
    /// kill. It cannot be a committed event, so it is moved aside automatically:
    /// the full file is archived first, then the active log is cut back to the
    /// validated events. Any other damage still needs the operator action.
    /// </summary>
    private bool TryRepairTornTail()
    {
        TornTail? torn;
        lock (_states)
        {
            torn = _tornTail;
        }

        if (torn is null)
        {
            return false;
        }

        try
        {
            var bytes = ReadAllBytesWithRetry(LogFilePath);
            if (bytes.Length != torn.FileLength)
            {
                return false;
            }

            var archive = CreateArchivePath();
            File.WriteAllBytes(archive, bytes);
            using (var stream = new FileStream(LogFilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.Read))
            {
                stream.SetLength(torn.ValidPrefixLength);
                stream.Flush(flushToDisk: true);
            }

            lock (_states)
            {
                _tornTail = null;
                _repairNotices.Add($"Dòng cuối của nhật ký lệnh in bị ghi dở (thường do mất điện hoặc tắt máy) đã được cất vào {Path.GetFileName(archive)}; nhật ký tiếp tục từ sự kiện hợp lệ cuối cùng.");
            }

            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    private string CreateArchivePath()
    {
        var directory = Path.GetDirectoryName(LogFilePath) ?? ".";
        var stem = Path.GetFileNameWithoutExtension(LogFilePath);
        var stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss", System.Globalization.CultureInfo.InvariantCulture);
        var archive = Path.Combine(directory, $"{stem}.hong-{stamp}.jsonl");
        for (var counter = 1; File.Exists(archive); counter++)
        {
            archive = Path.Combine(directory, $"{stem}.hong-{stamp}-{counter}.jsonl");
        }

        return archive;
    }

    private static byte[] ReadAllBytesWithRetry(string path)
    {
        // Antivirus or backup tools hold the file briefly; a short retry keeps
        // such a lock from being reported as damage.
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                var bytes = new byte[stream.Length];
                stream.ReadExactly(bytes);
                return bytes;
            }
            catch (IOException) when (attempt < 5 && File.Exists(path))
            {
                Thread.Sleep(100);
            }
        }
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
            _tornTail = null;
            _loadedLength = 0;
            _loadedLastWriteUtc = default;
            if (!File.Exists(LogFilePath))
            {
                return;
            }

            byte[] bytes;
            try
            {
                bytes = ReadAllBytesWithRetry(LogFilePath);
                _loadedLastWriteUtc = File.GetLastWriteTimeUtc(LogFilePath);
            }
            catch (FileNotFoundException)
            {
                return;
            }
            catch (IOException ex)
            {
                _recoveryDiagnostics.Add($"Could not replay print-job events: {ex.Message}");
                return;
            }
            catch (UnauthorizedAccessException ex)
            {
                _recoveryDiagnostics.Add($"Could not access print-job events: {ex.Message}");
                return;
            }

            var position = 0;
            long validEnd = 0;
            while (position < bytes.Length)
            {
                var newline = Array.IndexOf(bytes, (byte)'\n', position);
                var contentEnd = newline < 0 ? bytes.Length : newline;
                var lineEnd = newline < 0 ? bytes.Length : newline + 1;
                var line = Encoding.UTF8.GetString(bytes, position, contentEnd - position).TrimEnd('\r');
                if (string.IsNullOrWhiteSpace(line))
                {
                    position = lineEnd;
                    validEnd = lineEnd;
                    continue;
                }

                var problem = ReplayLine(line);
                if (problem is not null)
                {
                    _recoveryDiagnostics.Add(problem);
                    if (newline < 0)
                    {
                        _tornTail = new TornTail(validEnd, bytes.Length);
                    }

                    break;
                }

                position = lineEnd;
                validEnd = lineEnd;
            }

            _loadedLength = validEnd;
        }
    }

    /// <summary>Validates one event line against the replayed chain; returns a diagnostic or null.</summary>
    private string? ReplayLine(string line)
    {
        PrintJobStateEvent? item;
        try
        {
            item = JsonSerializer.Deserialize<PrintJobStateEvent>(line, JsonOptions);
        }
        catch (JsonException ex)
        {
            return $"Ignored malformed print-job event tail: {ex.Message}";
        }

        if (item is null || string.IsNullOrWhiteSpace(item.JobId))
        {
            return "Ignored an empty print-job event record.";
        }

        if (item.Manifest is not null
            && (!string.Equals(item.Manifest.Fingerprint, item.ManifestFingerprint, StringComparison.Ordinal)
                || !item.Manifest.IsFingerprintValid))
        {
            return $"Stopped replay at an event with invalid manifest metadata for job '{item.JobId}'.";
        }

        if (item.VerificationEvidence is not null
            && !item.VerificationEvidence.IsFingerprintValid)
        {
            return $"Stopped replay at an event with invalid physical-verification evidence for job '{item.JobId}'.";
        }

        _states.TryGetValue(item.JobId, out var current);
        var expectedSequence = (current?.Sequence ?? 0) + 1;
        var expectedPreviousHash = current?.Hash ?? string.Empty;
        if (item.Sequence != expectedSequence
            || !string.Equals(item.PreviousHash, expectedPreviousHash, StringComparison.Ordinal)
            || !IsIntegrityHashValid(item))
        {
            return $"Stopped replay at invalid event for job '{item.JobId}' sequence {item.Sequence}.";
        }

        _states[item.JobId] = new RecoveryState(
            item.Sequence,
            item.To,
            item.IntegrityHash,
            item);
        return null;
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

    private async Task<long> AppendLineAsync(PrintJobStateEvent stateEvent, CancellationToken cancellationToken)
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
        return bytes.Length;
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

    private sealed record TornTail(long ValidPrefixLength, long FileLength);
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
