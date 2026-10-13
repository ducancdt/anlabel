using System.Security.Cryptography;

namespace ANLAbel.Core.Data;

/// <summary>
/// Immutable evidence for a local, file-backed data snapshot. Metadata alone is
/// not sufficient: an upstream system can preserve a file timestamp while
/// replacing its contents, so the SHA-256 is the authority for freshness.
/// </summary>
public sealed record FileSourceIdentity(long Length, DateTime LastWriteTimeUtc, string Sha256)
{
    /// <summary>Captures current bytes and metadata without throwing for an unavailable source.</summary>
    public static bool TryCapture(string? filePath, out FileSourceIdentity? identity)
    {
        identity = null;
        if (string.IsNullOrWhiteSpace(filePath)) return false;

        try
        {
            var info = new FileInfo(filePath);
            if (!info.Exists) return false;
            var lengthBeforeRead = info.Length;
            var writeTimeBeforeRead = info.LastWriteTimeUtc;

            // Excel keeps an open workbook with write access. Asking for
            // FileShare.Read would fail while the operator has the file open and
            // turn an unchanged source into a false "stale" print block (plan 13
            // H15). Reading with ReadWrite sharing is safe: the length/time check
            // below still rejects a file that changes while it is hashed.
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            var sha256 = Convert.ToHexString(SHA256.HashData(stream));
            info.Refresh();
            if (info.Length != lengthBeforeRead || info.LastWriteTimeUtc != writeTimeBeforeRead)
            {
                return false;
            }
            identity = new FileSourceIdentity(info.Length, info.LastWriteTimeUtc, sha256);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            return false;
        }
    }

    /// <summary>A captured source is stale when it cannot be recaptured or its evidence differs.</summary>
    public static bool IsStale(FileSourceIdentity? captured, FileSourceIdentity? current) =>
        captured is not null && !Equals(captured, current);
}
