using System.Text.Json;
using System.Text.RegularExpressions;

namespace ANLAbel.Core.Updates;

/// <summary>
/// Parser and comparison utilities for GitHub releases and application version tags.
/// </summary>
public static class GitHubReleaseParser
{
    private static readonly Regex VersionPattern = new(
        @"^v?(?<major>\d+)(?:\.(?<minor>\d+))?(?:\.(?<build>\d+))?(?:\.(?<revision>\d+))?",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    /// <summary>
    /// Parses a version string or git tag (e.g. "v0.258", "0.258", "1.0.4.0") into a System.Version.
    /// </summary>
    public static Version? ParseVersion(string? tagOrVersion)
    {
        if (string.IsNullOrWhiteSpace(tagOrVersion)) return null;

        var clean = tagOrVersion.Trim();
        var match = VersionPattern.Match(clean);
        if (!match.Success) return null;

        var major = int.Parse(match.Groups["major"].Value);
        var minor = match.Groups["minor"].Success ? int.Parse(match.Groups["minor"].Value) : 0;
        var build = match.Groups["build"].Success ? int.Parse(match.Groups["build"].Value) : 0;
        var revision = match.Groups["revision"].Success ? int.Parse(match.Groups["revision"].Value) : 0;

        return new Version(major, minor, build, revision);
    }

    /// <summary>
    /// Compares two version strings or git tags.
    /// Returns > 0 if versionB is newer than versionA, 0 if equal, and &lt; 0 if versionB is older.
    /// </summary>
    public static int CompareVersions(string? versionA, string? versionB)
    {
        var parsedA = ParseVersion(versionA);
        var parsedB = ParseVersion(versionB);

        if (parsedA is null && parsedB is null) return 0;
        if (parsedA is null) return 1; // versionB exists, so newer
        if (parsedB is null) return -1; // versionA exists, versionB null

        return parsedB.CompareTo(parsedA);
    }

    /// <summary>
    /// Checks if candidateVersion is newer than currentVersion.
    /// </summary>
    public static bool IsNewerVersion(string? currentVersion, string? candidateVersion)
    {
        return CompareVersions(currentVersion, candidateVersion) > 0;
    }

    /// <summary>
    /// Parses a GitHub Release JSON string into a ReleaseInfo instance.
    /// </summary>
    public static ReleaseInfo? ParseReleaseJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.ValueKind != JsonValueKind.Object) return null;

            var tagName = root.TryGetProperty("tag_name", out var tagProp) ? tagProp.GetString() ?? "" : "";
            var title = root.TryGetProperty("name", out var nameProp) ? nameProp.GetString() ?? "" : "";
            var body = root.TryGetProperty("body", out var bodyProp) ? bodyProp.GetString() ?? "" : "";
            var htmlUrl = root.TryGetProperty("html_url", out var urlProp) ? urlProp.GetString() ?? "" : "";
            var isPreRelease = root.TryGetProperty("prerelease", out var preProp) && preProp.GetBoolean();

            DateTimeOffset? publishedAt = null;
            if (root.TryGetProperty("published_at", out var pubProp) && pubProp.ValueKind == JsonValueKind.String)
            {
                if (DateTimeOffset.TryParse(pubProp.GetString(), out var dto))
                {
                    publishedAt = dto;
                }
            }

            var parsedVersion = ParseVersion(tagName);
            var versionString = parsedVersion is not null
                ? (parsedVersion.Build == 0 && parsedVersion.Revision == 0
                    ? $"{parsedVersion.Major}.{parsedVersion.Minor}"
                    : parsedVersion.ToString())
                : tagName.TrimStart('v', 'V');

            var assetsList = new List<ReleaseAssetInfo>();
            if (root.TryGetProperty("assets", out var assetsProp) && assetsProp.ValueKind == JsonValueKind.Array)
            {
                foreach (var assetElem in assetsProp.EnumerateArray())
                {
                    var assetName = assetElem.TryGetProperty("name", out var aName) ? aName.GetString() ?? "" : "";
                    var downloadUrl = assetElem.TryGetProperty("browser_download_url", out var aUrl) ? aUrl.GetString() ?? "" : "";
                    var size = assetElem.TryGetProperty("size", out var aSize) && aSize.TryGetInt64(out var s) ? s : 0;
                    var contentType = assetElem.TryGetProperty("content_type", out var aType) ? aType.GetString() ?? "" : "";

                    if (!string.IsNullOrEmpty(downloadUrl))
                    {
                        assetsList.Add(new ReleaseAssetInfo
                        {
                            Name = assetName,
                            DownloadUrl = downloadUrl,
                            Size = size,
                            ContentType = contentType
                        });
                    }
                }
            }

            return new ReleaseInfo
            {
                TagName = tagName,
                VersionString = versionString,
                ParsedVersion = parsedVersion,
                Title = string.IsNullOrWhiteSpace(title) ? tagName : title,
                ReleaseNotes = body,
                HtmlUrl = htmlUrl,
                PublishedAt = publishedAt,
                IsPreRelease = isPreRelease,
                Assets = assetsList
            };
        }
        catch
        {
            return null;
        }
    }
}
