using System.Text.Json;

namespace ANLAbel.Data.PrintLogs;

/// <summary>
/// Appends one JSON line per print job to a local log file. This is a best-effort trace
/// (print-preview-reliability-plan.md item 3): failures to write the log must never block
/// or fail the print job itself, so all I/O errors are swallowed.
/// </summary>
public sealed class PrintOperationLogService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };
    private readonly SemaphoreSlim _writeGate = new(1, 1);

    public PrintOperationLogService()
        : this(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ANLAbel",
            "logs",
            "print-operations.jsonl"))
    {
    }

    public PrintOperationLogService(string logFilePath)
    {
        LogFilePath = logFilePath;
    }

    public string LogFilePath { get; }

    public async Task AppendAsync(PrintOperationLogEntry entry, CancellationToken cancellationToken = default)
    {
        // Serialize writes for this service instance. File.AppendAllText opens and closes
        // the file for every line; without a gate, concurrent fire-and-forget traces can
        // race each other and can leave the file locked while a caller observes the line.
        await _writeGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await Task.Run(() => Append(entry), CancellationToken.None).ConfigureAwait(false);
        }
        finally
        {
            _writeGate.Release();
        }
    }

    /// <summary>
    /// Waits until all writes already queued on this service have released the file.
    /// Production callers normally keep logging fire-and-forget; tests and orderly
    /// shutdown paths can use this barrier to make the append lifecycle deterministic.
    /// </summary>
    public async Task WaitForPendingWritesAsync(CancellationToken cancellationToken = default)
    {
        await _writeGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        _writeGate.Release();
    }

    /// <summary>Reads best-effort operation trace entries without changing print state.</summary>
    public async Task<(IReadOnlyList<PrintOperationLogEntry> Entries, IReadOnlyList<string> Diagnostics)> ReadAllAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(LogFilePath)) return (Array.Empty<PrintOperationLogEntry>(), Array.Empty<string>());
        var entries = new List<PrintOperationLogEntry>();
        var diagnostics = new List<string>();
        var lineNumber = 0;
        foreach (var line in await File.ReadAllLinesAsync(LogFilePath, cancellationToken).ConfigureAwait(false))
        {
            lineNumber++;
            if (string.IsNullOrWhiteSpace(line)) continue;
            try
            {
                var entry = JsonSerializer.Deserialize<PrintOperationLogEntry>(line);
                if (entry is null) diagnostics.Add($"Operation trace line {lineNumber} was empty.");
                else entries.Add(entry);
            }
            catch (JsonException ex)
            {
                diagnostics.Add($"Operation trace line {lineNumber} could not be read: {ex.Message}");
            }
        }
        return (entries.OrderByDescending(entry => entry.SpoolStatusObservedAtUtc ?? new DateTimeOffset(entry.TimestampLocal)).ToArray(), diagnostics);
    }

    private void Append(PrintOperationLogEntry entry)
    {
        try
        {
            var directory = Path.GetDirectoryName(LogFilePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var line = JsonSerializer.Serialize(entry, JsonOptions);
            File.AppendAllText(LogFilePath, line + Environment.NewLine);
        }
        catch (IOException)
        {
            // Best-effort trace only — never block the print job on a logging failure.
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
