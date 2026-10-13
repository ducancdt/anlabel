using System.Text;

namespace ANLAbel.Data;

/// <summary>
/// Writes a small file so a crash, power loss or full disk leaves either the
/// old or the new content, never a truncated file (audit 2026-10-13). The
/// temporary name is unique, so two windows saving at once cannot share it.
/// </summary>
public static class AtomicFile
{
    public static void WriteAllText(string path, string contents)
    {
        var fullPath = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(fullPath) ?? Environment.CurrentDirectory;
        Directory.CreateDirectory(directory);
        var temporaryPath = Path.Combine(directory, $".{Path.GetFileName(fullPath)}.{Guid.NewGuid():N}.tmp");
        try
        {
            using (var stream = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough))
            {
                var bytes = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false).GetBytes(contents);
                stream.Write(bytes, 0, bytes.Length);
                stream.Flush(flushToDisk: true);
            }

            File.Move(temporaryPath, fullPath, overwrite: true);
        }
        catch
        {
            try
            {
                File.Delete(temporaryPath);
            }
            catch (Exception cleanup) when (cleanup is IOException or UnauthorizedAccessException)
            {
                // Keep the original failure.
            }

            throw;
        }
    }

    /// <summary>Another process has the file open in a conflicting mode, usually for a moment.</summary>
    internal static bool IsSharingViolation(IOException exception)
        => (exception.HResult & 0xFFFF) is 32 or 33; // ERROR_SHARING_VIOLATION, ERROR_LOCK_VIOLATION
}
