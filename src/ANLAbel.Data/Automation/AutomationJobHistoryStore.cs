using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ANLAbel.Data.Automation;

/// <summary>Append-only, payload-free link between local automation and a durable print job.</summary>
public sealed record AutomationJobHistoryLink(long Sequence, DateTimeOffset TimestampUtc, string EventId, string PreparedBatchId, string JobId, string ManifestFingerprint, string PreviousHash, string IntegrityHash);

public sealed class AutomationJobHistoryStore
{
    private readonly string _path;
    public AutomationJobHistoryStore(string path) => _path = path;

    public IReadOnlyList<AutomationJobHistoryLink> ReadValid(out IReadOnlyList<string> diagnostics)
    {
        var links = new List<AutomationJobHistoryLink>(); var errors = new List<string>(); var previous = string.Empty;
        if (!File.Exists(_path)) { diagnostics = errors; return links; }
        foreach (var line in File.ReadLines(_path))
        {
            try
            {
                var link = JsonSerializer.Deserialize<AutomationJobHistoryLink>(line) ?? throw new JsonException("Empty automation history link.");
                if (link.Sequence != links.Count + 1 || link.PreviousHash != previous || link.IntegrityHash != Fingerprint(link with { IntegrityHash = string.Empty }) || string.IsNullOrWhiteSpace(link.EventId) || string.IsNullOrWhiteSpace(link.PreparedBatchId) || string.IsNullOrWhiteSpace(link.JobId)) throw new InvalidDataException("Automation job history integrity mismatch.");
                if (links.Any(item => item.EventId == link.EventId)) throw new InvalidDataException("Automation event already has a durable job link.");
                links.Add(link); previous = link.IntegrityHash;
            }
            catch (Exception ex) { errors.Add(ex.Message); break; }
        }
        diagnostics = errors; return links;
    }

    public bool TryAppend(string eventId, string batchId, string jobId, string manifestFingerprint, out AutomationJobHistoryLink? recorded, out string error)
    {
        if (string.IsNullOrWhiteSpace(eventId) || string.IsNullOrWhiteSpace(batchId) || string.IsNullOrWhiteSpace(jobId)) { recorded = null; error = "Automation event, batch and durable job IDs are required."; return false; }
        var existing = ReadValid(out var diagnostics);
        if (diagnostics.Count != 0) { recorded = null; error = "Automation job history requires repair before append."; return false; }
        if (existing.Any(item => item.EventId == eventId.Trim())) { recorded = null; error = "Automation event already has a durable job link."; return false; }
        var next = new AutomationJobHistoryLink(existing.Count + 1, DateTimeOffset.UtcNow, eventId.Trim(), batchId.Trim(), jobId.Trim(), manifestFingerprint?.Trim() ?? string.Empty, existing.LastOrDefault()?.IntegrityHash ?? string.Empty, string.Empty);
        next = next with { IntegrityHash = Fingerprint(next) };
        var directory = Path.GetDirectoryName(_path); if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
        using var stream = new FileStream(_path, FileMode.Append, FileAccess.Write, FileShare.Read, 16 * 1024, FileOptions.WriteThrough);
        using var writer = new StreamWriter(stream, new UTF8Encoding(false)); writer.WriteLine(JsonSerializer.Serialize(next)); writer.Flush(); stream.Flush(true);
        recorded = next; error = string.Empty; return true;
    }

    private static string Fingerprint(AutomationJobHistoryLink link) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{link.Sequence}|{link.TimestampUtc:O}|{link.EventId}|{link.PreparedBatchId}|{link.JobId}|{link.ManifestFingerprint}|{link.PreviousHash}")));
}
