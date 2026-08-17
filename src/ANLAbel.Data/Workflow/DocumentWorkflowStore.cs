using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ANLAbel.Core.Workflow;

namespace ANLAbel.Data.Workflow;

public sealed record DocumentWorkflowEvent(long Sequence, DateTimeOffset TimestampUtc, string DocumentId, string DocumentHash, DocumentWorkflowState From, DocumentWorkflowState To, string Actor, string Comment, string PreviousHash, string IntegrityHash);

/// <summary>Append-only local workflow audit, deliberately separate from print-job state.</summary>
public sealed class DocumentWorkflowStore
{
    private readonly string _path;
    public DocumentWorkflowStore(string path) => _path = path;
    public IReadOnlyList<DocumentWorkflowEvent> ReadValid(out IReadOnlyList<string> diagnostics)
    {
        var events = new List<DocumentWorkflowEvent>(); var errors = new List<string>(); var previous = string.Empty;
        if (!File.Exists(_path)) { diagnostics = errors; return events; }
        foreach (var line in File.ReadLines(_path))
        {
            try
            {
                var item = JsonSerializer.Deserialize<DocumentWorkflowEvent>(line) ?? throw new JsonException("Empty event.");
                if (item.Sequence != events.Count + 1 || item.PreviousHash != previous || item.IntegrityHash != Fingerprint(item with { IntegrityHash = string.Empty })) throw new InvalidDataException("Workflow audit integrity mismatch.");
                events.Add(item); previous = item.IntegrityHash;
            }
            catch (Exception ex) { errors.Add(ex.Message); break; }
        }
        diagnostics = errors; return events;
    }
    public DocumentWorkflowEvent Append(string documentId, string documentHash, DocumentWorkflowState from, DocumentWorkflowState to, string actor, string? comment)
    {
        if (!DocumentWorkflowContract.TryTransition(from, to, comment, out var error)) throw new InvalidOperationException(error);
        var existing = ReadValid(out var diagnostics); if (diagnostics.Count != 0) throw new InvalidDataException("Workflow audit requires repair before append.");
        var next = new DocumentWorkflowEvent(existing.Count + 1, DateTimeOffset.UtcNow, documentId, documentHash, from, to, string.IsNullOrWhiteSpace(actor) ? "local operator" : actor, comment ?? string.Empty, existing.LastOrDefault()?.IntegrityHash ?? string.Empty, string.Empty);
        next = next with { IntegrityHash = Fingerprint(next) };
        var directory = Path.GetDirectoryName(_path);
        if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
        using (var stream = new FileStream(_path, FileMode.Append, FileAccess.Write, FileShare.Read, 16 * 1024, FileOptions.WriteThrough))
        using (var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)))
        {
            writer.WriteLine(JsonSerializer.Serialize(next));
            writer.Flush();
            stream.Flush(flushToDisk: true);
        }
        return next;
    }
    private static string Fingerprint(DocumentWorkflowEvent item) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{item.Sequence}|{item.TimestampUtc:O}|{item.DocumentId}|{item.DocumentHash}|{item.From}|{item.To}|{item.Actor}|{item.Comment}|{item.PreviousHash}")));
}

/// <summary>Path-safe local sidecar identity; no workflow data is put in the template envelope.</summary>
public static class DocumentWorkflowSidecar
{
    public static string GetDocumentId(string templatePath)
    {
        if (string.IsNullOrWhiteSpace(templatePath)) throw new ArgumentException("A saved template path is required.", nameof(templatePath));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Path.GetFullPath(templatePath).ToUpperInvariant())));
    }
    public static string GetStorePath(string templatePath) => Path.GetFullPath(templatePath) + ".workflow.jsonl";
    public static DocumentWorkflowStore Open(string templatePath) => new(GetStorePath(templatePath));
}
