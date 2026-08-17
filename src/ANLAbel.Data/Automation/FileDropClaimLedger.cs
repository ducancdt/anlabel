using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ANLAbel.Core.Automation;

namespace ANLAbel.Data.Automation;

public sealed record FileDropClaimLedgerEvent(
    long Sequence,
    DateTimeOffset TimestampUtc,
    FileDropEventIdentity Identity,
    FileDropEventState From,
    FileDropEventState To,
    string Reason,
    string PreviousHash,
    string IntegrityHash);

/// <summary>
/// A local, append-only fingerprint ledger. It records claims only; it has no
/// watcher, source-file mutation, queue access, or printer access.
/// </summary>
public sealed class FileDropClaimLedger
{
    private readonly string _path;

    public FileDropClaimLedger(string path) => _path = path;

    public IReadOnlyList<FileDropClaimLedgerEvent> ReadValid(out IReadOnlyList<string> diagnostics)
    {
        if (!File.Exists(_path))
        {
            diagnostics = Array.Empty<string>();
            return Array.Empty<FileDropClaimLedgerEvent>();
        }

        using var stream = new FileStream(_path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        return ReadValid(stream, out diagnostics);
    }

    public bool TryRecordDetection(FileDropEventIdentity identity, out FileDropClaimLedgerEvent? recorded, out string error)
    {
        return TryAppend(identity, FileDropEventState.Unknown, FileDropEventState.Detected, string.Empty, rejectDuplicate: true, out recorded, out error);
    }

    public bool TryTransition(FileDropEventIdentity identity, FileDropEventState to, string? reason, out FileDropClaimLedgerEvent? recorded, out string error)
    {
        EnsureIdentity(identity);
        var result = WithExclusiveStream(stream =>
        {
            var existing = ReadValid(stream, out var diagnostics);
            if (diagnostics.Count != 0)
            {
                return new LedgerWriteResult(false, null, "Automation claim ledger requires repair before a new transition.");
            }

            var latest = existing.LastOrDefault(item => item.Identity.EventId == identity.EventId);
            if (latest is null)
            {
                return new LedgerWriteResult(false, null, "A source must be detected before it can transition.");
            }

            return Append(stream, existing, identity, latest.To, to, reason ?? string.Empty);
        });
        recorded = result.Recorded;
        error = result.Error;
        return result.Succeeded;
    }

    private bool TryAppend(FileDropEventIdentity identity, FileDropEventState from, FileDropEventState to, string reason, bool rejectDuplicate, out FileDropClaimLedgerEvent? recorded, out string error)
    {
        EnsureIdentity(identity);
        var result = WithExclusiveStream(stream =>
        {
            var existing = ReadValid(stream, out var diagnostics);
            if (diagnostics.Count != 0)
            {
                return new LedgerWriteResult(false, null, "Automation claim ledger requires repair before a new event.");
            }

            if (rejectDuplicate && existing.Any(item => item.Identity.EventId == identity.EventId))
            {
                return new LedgerWriteResult(false, null, "Duplicate source notification already has a durable event identity.");
            }

            return Append(stream, existing, identity, from, to, reason);
        });
        recorded = result.Recorded;
        error = result.Error;
        return result.Succeeded;
    }

    private static LedgerWriteResult Append(FileStream stream, IReadOnlyList<FileDropClaimLedgerEvent> existing, FileDropEventIdentity identity, FileDropEventState from, FileDropEventState to, string reason)
    {
        if (!FileDropClaimContract.TryTransition(from, to, out var error))
            return new LedgerWriteResult(false, null, error);

        var next = new FileDropClaimLedgerEvent(
            existing.Count + 1,
            DateTimeOffset.UtcNow,
            identity,
            from,
            to,
            reason,
            existing.LastOrDefault()?.IntegrityHash ?? string.Empty,
            string.Empty);
        next = next with { IntegrityHash = Fingerprint(next) };

        stream.Position = stream.Length;
        using (var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), 1024, leaveOpen: true))
        {
            writer.WriteLine(JsonSerializer.Serialize(next));
            writer.Flush();
        }
        stream.Flush(flushToDisk: true);
        return new LedgerWriteResult(true, next, string.Empty);
    }

    private static IReadOnlyList<FileDropClaimLedgerEvent> ReadValid(Stream stream, out IReadOnlyList<string> diagnostics)
    {
        stream.Position = 0;
        var events = new List<FileDropClaimLedgerEvent>();
        var errors = new List<string>();
        var previous = string.Empty;
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, 1024, leaveOpen: true);
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            try
            {
                var item = JsonSerializer.Deserialize<FileDropClaimLedgerEvent>(line) ?? throw new JsonException("Empty event.");
                EnsureIdentity(item.Identity);
                if (item.Sequence != events.Count + 1 || item.PreviousHash != previous || item.IntegrityHash != Fingerprint(item with { IntegrityHash = string.Empty }))
                    throw new InvalidDataException("Automation claim ledger integrity mismatch.");
                if (!FileDropClaimContract.TryTransition(item.From, item.To, out var transitionError))
                    throw new InvalidDataException(transitionError);
                events.Add(item);
                previous = item.IntegrityHash;
            }
            catch (Exception ex)
            {
                errors.Add(ex.Message);
                break;
            }
        }
        diagnostics = errors;
        return events;
    }

    private LedgerWriteResult WithExclusiveStream(Func<FileStream, LedgerWriteResult> operation)
    {
        var directory = Path.GetDirectoryName(_path);
        if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
        using var stream = new FileStream(_path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, 16 * 1024, FileOptions.WriteThrough);
        return operation(stream);
    }

    private static string Fingerprint(FileDropClaimLedgerEvent item) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{item.Sequence}|{item.TimestampUtc:O}|{item.Identity.EventId}|{item.Identity.TriggerId}|{item.Identity.ConfigurationFingerprint}|{item.Identity.SourceFingerprint}|{item.From}|{item.To}|{item.Reason}|{item.PreviousHash}")));

    private static void EnsureIdentity(FileDropEventIdentity identity)
    {
        var expected = FileDropClaimContract.CreateIdentity(identity.TriggerId, identity.ConfigurationFingerprint, identity.SourceFingerprint);
        if (!string.Equals(identity.EventId, expected.EventId, StringComparison.Ordinal))
            throw new InvalidDataException("Automation event identity does not match its fingerprints.");
    }

    private sealed record LedgerWriteResult(bool Succeeded, FileDropClaimLedgerEvent? Recorded, string Error);
}
