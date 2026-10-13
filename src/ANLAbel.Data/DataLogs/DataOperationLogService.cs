using System.Collections.Concurrent;
using System.Diagnostics;
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

    // Serialize writes per-log-file rather than app-wide: two services pointed at
    // DIFFERENT files must not contend, while writes to the SAME file stay mutually
    // exclusive. Keyed by the normalized full path so distinct spellings of one file
    // still share a lock.
    private static readonly ConcurrentDictionary<string, object> FileLocks =
        new(StringComparer.OrdinalIgnoreCase);

    private object FileLock => FileLocks.GetOrAdd(NormalizePath(LogFilePath), _ => new object());

    private static string NormalizePath(string path)
    {
        try
        {
            return Path.GetFullPath(path);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            // A malformed path can't be normalized; fall back to the raw value so it
            // still locks consistently against itself.
            return path;
        }
    }

    public Task AppendAsync(DataOperationLogEntry entry, CancellationToken cancellationToken = default)
    {
        return Task.Run(() => Append(entry), cancellationToken);
    }

    private void Append(DataOperationLogEntry entry)
    {
        lock (FileLock)
        {
            for (var i = 0; i < 5; i++)
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
                    break;
                }
                catch (IOException) when (i < 4)
                {
                    Thread.Sleep(25);
                }
                catch (IOException)
                {
                    // Best-effort trace only — never block the data operation on a logging failure.
                }
                catch (UnauthorizedAccessException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    // Best-effort trace only — never block the data operation on a logging
                    // failure. Unlike I/O errors above, an unexpected fault here (e.g. a
                    // serialization bug) would otherwise escape as an unobserved Task
                    // exception; surface it observably instead of losing it silently.
                    Debug.WriteLine($"DataOperationLog write failed: {ex}");
                    break;
                }
            }
        }
    }
}
