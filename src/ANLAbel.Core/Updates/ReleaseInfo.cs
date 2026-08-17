namespace ANLAbel.Core.Updates;

/// <summary>
/// Status of the software update check.
/// </summary>
public enum UpdateStatus
{
    /// <summary>
    /// The application is running the latest available version.
    /// </summary>
    UpToDate,

    /// <summary>
    /// A newer version of the application is available for download.
    /// </summary>
    UpdateAvailable,

    /// <summary>
    /// Failed to check for updates due to a network error, rate limit, or server error.
    /// </summary>
    Error,

    /// <summary>
    /// Network connection is unavailable or host could not be resolved.
    /// </summary>
    Offline
}

/// <summary>
/// Metadata for an asset attached to a GitHub release.
/// </summary>
public sealed class ReleaseAssetInfo
{
    public string Name { get; init; } = string.Empty;
    public string DownloadUrl { get; init; } = string.Empty;
    public long Size { get; init; }
    public string ContentType { get; init; } = string.Empty;
    public bool IsInstaller => Name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Human-readable file size (e.g., "45.2 MB").
    /// </summary>
    public string FormattedSize
    {
        get
        {
            if (Size <= 0) return "0 B";
            string[] units = ["B", "KB", "MB", "GB"];
            double len = Size;
            int order = 0;
            while (len >= 1024 && order < units.Length - 1)
            {
                order++;
                len /= 1024;
            }
            return $"{len:0.##} {units[order]}";
        }
    }
}

/// <summary>
/// Information about a GitHub release.
/// </summary>
public sealed class ReleaseInfo
{
    public string TagName { get; init; } = string.Empty;
    public string VersionString { get; init; } = string.Empty;
    public Version? ParsedVersion { get; init; }
    public string Title { get; init; } = string.Empty;
    public string ReleaseNotes { get; init; } = string.Empty;
    public string HtmlUrl { get; init; } = string.Empty;
    public DateTimeOffset? PublishedAt { get; init; }
    public bool IsPreRelease { get; init; }
    public IReadOnlyList<ReleaseAssetInfo> Assets { get; init; } = [];

    /// <summary>
    /// The primary installer asset (e.g. ANLAbel-v*-Setup-x64.exe or any .exe asset).
    /// </summary>
    public ReleaseAssetInfo? InstallerAsset =>
        Assets.FirstOrDefault(a => a.Name.Contains("Setup", StringComparison.OrdinalIgnoreCase) && a.IsInstaller)
        ?? Assets.FirstOrDefault(a => a.IsInstaller)
        ?? Assets.FirstOrDefault();
}

/// <summary>
/// Result of checking for updates against GitHub Releases.
/// </summary>
public sealed class UpdateCheckResult
{
    public UpdateStatus Status { get; init; }
    public string CurrentVersion { get; init; } = string.Empty;
    public ReleaseInfo? LatestRelease { get; init; }
    public string? ErrorMessage { get; init; }

    public bool IsUpdateAvailable => Status == UpdateStatus.UpdateAvailable && LatestRelease is not null;
}
