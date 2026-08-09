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

    public Task AppendAsync(PrintOperationLogEntry entry, CancellationToken cancellationToken = default)
    {
        return Task.Run(() => Append(entry), cancellationToken);
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
