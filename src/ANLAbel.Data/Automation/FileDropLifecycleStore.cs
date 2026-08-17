using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ANLAbel.Data.Automation;

public sealed record FileDropLifecycleEvent(long Sequence, DateTimeOffset TimestampUtc, string State, string Detail, string PreviousHash, string IntegrityHash);

/// <summary>Separate durable lifecycle evidence; never a source-claim or print-job store.</summary>
public sealed class FileDropLifecycleStore
{
    private readonly string _path;
    public FileDropLifecycleStore(string path) => _path = path;

    public IReadOnlyList<FileDropLifecycleEvent> ReadValid(out IReadOnlyList<string> diagnostics)
    {
        var events = new List<FileDropLifecycleEvent>();
        var errors = new List<string>();
        var previous = string.Empty;
        if (!File.Exists(_path)) { diagnostics = errors; return events; }
        foreach (var line in File.ReadLines(_path))
        {
            try
            {
                var item = JsonSerializer.Deserialize<FileDropLifecycleEvent>(line) ?? throw new JsonException("Empty lifecycle event.");
                if (item.Sequence != events.Count + 1 || item.PreviousHash != previous || item.IntegrityHash != Fingerprint(item with { IntegrityHash = string.Empty }))
                    throw new InvalidDataException("Automation lifecycle integrity mismatch.");
                events.Add(item);
                previous = item.IntegrityHash;
            }
            catch (Exception ex) { errors.Add(ex.Message); break; }
        }
        diagnostics = errors;
        return events;
    }

    public bool TryAppend(string state, string detail, out FileDropLifecycleEvent? recorded, out string error)
    {
        if (string.IsNullOrWhiteSpace(state)) { recorded = null; error = "Lifecycle state is required."; return false; }
        var existing = ReadValid(out var diagnostics);
        if (diagnostics.Count != 0) { recorded = null; error = "Automation lifecycle journal requires repair before append."; return false; }
        var next = new FileDropLifecycleEvent(existing.Count + 1, DateTimeOffset.UtcNow, state.Trim(), detail ?? string.Empty, existing.LastOrDefault()?.IntegrityHash ?? string.Empty, string.Empty);
        next = next with { IntegrityHash = Fingerprint(next) };
        var directory = Path.GetDirectoryName(_path);
        if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
        using var stream = new FileStream(_path, FileMode.Append, FileAccess.Write, FileShare.Read, 16 * 1024, FileOptions.WriteThrough);
        using var writer = new StreamWriter(stream, new UTF8Encoding(false));
        writer.WriteLine(JsonSerializer.Serialize(next));
        writer.Flush();
        stream.Flush(flushToDisk: true);
        recorded = next;
        error = string.Empty;
        return true;
    }

    private static string Fingerprint(FileDropLifecycleEvent item) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{item.Sequence}|{item.TimestampUtc:O}|{item.State}|{item.Detail}|{item.PreviousHash}")));
}
