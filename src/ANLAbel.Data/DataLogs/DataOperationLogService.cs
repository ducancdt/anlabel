using System.Text.Json;

namespace ANLAbel.Data.DataLogs;

/// <summary>
/// Appends one JSON line per data-source operation to a local log file. This is a
/// best-effort trace (database-plan.md TC6): failures to write the log must never
/// block or fail the data operation itself, so all I/O errors are swallowed.
/// </summary>
public sealed class DataOperationLogService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

    public DataOperationLogService()
        : this(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ANLAbel",
            "logs",
            "data-operations.jsonl"))
    {
    }

    public DataOperationLogService(string logFilePath)
    {
        LogFilePath = logFilePath;
    }

    public string LogFilePath { get; }

    public Task AppendAsync(DataOperationLogEntry entry, CancellationToken cancellationToken = default)
    {
        return Task.Run(() => Append(entry), cancellationToken);
    }

    private void Append(DataOperationLogEntry entry)
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
            // Best-effort trace only — never block the data operation on a logging failure.
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
