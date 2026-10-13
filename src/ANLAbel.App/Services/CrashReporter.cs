using System.IO;
using ANLAbel.App.ViewModels;
using ANLAbel.Data;

namespace ANLAbel.App.Services;

/// <summary>
/// Writes plain-text failure reports to <c>%LOCALAPPDATA%\ANLAbel\logs</c>
/// (plan 13 F03). Unexpected errors get one <c>crash-*.log</c> each (the newest
/// <see cref="KeepCrashLogs"/> are kept); handled operation failures append to a
/// rotating <c>errors.log</c>. Writing a report never throws.
/// </summary>
public static class CrashReporter
{
    public const int KeepCrashLogs = 20;

    /// <summary>Writes a crash report and returns its path, or null when it could not be written.</summary>
    public static string? WriteCrashLog(Exception exception, string context)
    {
        try
        {
            Directory.CreateDirectory(AppDataPaths.LogsDirectory);
            var path = AppDataPaths.Log($"crash-{DateTime.Now:yyyyMMdd-HHmmss-fff}-{Environment.ProcessId}-{Guid.NewGuid().ToString("N")[..8]}.log");
            File.WriteAllText(path, Describe(exception, context));
            PruneCrashLogs();
            return path;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    /// <summary>Appends a handled failure to the rotating error log.</summary>
    public static void AppendErrorLog(Exception exception, string context)
    {
        try
        {
            Directory.CreateDirectory(AppDataPaths.LogsDirectory);
            var path = AppDataPaths.Log("errors.log");
            LogFileRotation.RotateIfNeeded(path);
            File.AppendAllText(path, Describe(exception, context) + Environment.NewLine + new string('-', 72) + Environment.NewLine);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // The failure itself was already reported to the operator.
        }
    }

    private static string Describe(Exception exception, string context)
    {
        return string.Join(
            Environment.NewLine,
            MainViewModel.ApplicationTitle,
            $"Thời điểm: {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
            $"Ngữ cảnh: {context}",
            $"Hệ điều hành: {Environment.OSVersion}",
            string.Empty,
            exception.ToString());
    }

    private static void PruneCrashLogs()
    {
        var old = Directory
            .EnumerateFiles(AppDataPaths.LogsDirectory, "crash-*.log")
            .OrderByDescending(path => Path.GetFileName(path), StringComparer.Ordinal)
            .Skip(KeepCrashLogs)
            .ToArray();
        foreach (var path in old)
        {
            try
            {
                File.Delete(path);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // Pruned at the next report.
            }
        }
    }
}
