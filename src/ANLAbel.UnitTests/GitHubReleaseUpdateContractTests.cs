using ANLAbel.Core.Updates;
using Xunit;

namespace ANLAbel.UnitTests;

public sealed class GitHubReleaseUpdateContractTests
{
    [Theory]
    [InlineData("v0.258", 0, 258, 0, 0)]
    [InlineData("0.258", 0, 258, 0, 0)]
    [InlineData("V1.2.3", 1, 2, 3, 0)]
    [InlineData("2.10.4.99", 2, 10, 4, 99)]
    [InlineData("v0.258-preview", 0, 258, 0, 0)]
    public void VersionParserExtractsNormalizedVersion(string input, int major, int minor, int build, int revision)
    {
        var parsed = GitHubReleaseParser.ParseVersion(input);
        Assert.NotNull(parsed);
        Assert.Equal(major, parsed!.Major);
        Assert.Equal(minor, parsed.Minor);
        Assert.Equal(build, parsed.Build);
        Assert.Equal(revision, parsed.Revision);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    [InlineData("invalid-tag")]
    public void VersionParserReturnsNullForInvalidInput(string? input)
    {
        Assert.Null(GitHubReleaseParser.ParseVersion(input));
    }

    [Fact]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Assertions", "xUnit2000:Constants should be passed to the Assert.Equal() argument.", Justification = "Comparing output of dynamic method")]
    public void VersionComparisonCorrectlyIdentifiesNewerAndOlder()
    {
        Assert.True(GitHubReleaseParser.IsNewerVersion("0.257", "v0.258"));
        Assert.True(GitHubReleaseParser.IsNewerVersion("0.257", "0.258"));
        Assert.True(GitHubReleaseParser.IsNewerVersion("0.258", "0.258.1"));
        Assert.True(GitHubReleaseParser.IsNewerVersion("0.258", "v1.0.0"));

        Assert.False(GitHubReleaseParser.IsNewerVersion("0.258", "0.258"));
        Assert.False(GitHubReleaseParser.IsNewerVersion("0.258", "v0.258"));
        Assert.False(GitHubReleaseParser.IsNewerVersion("0.258", "0.257"));
        Assert.False(GitHubReleaseParser.IsNewerVersion("1.0.0", "0.258"));

        Assert.Equal(0, GitHubReleaseParser.CompareVersions("v0.258", "0.258"));
        Assert.True(GitHubReleaseParser.CompareVersions("0.257", "0.258") > 0);
        Assert.True(GitHubReleaseParser.CompareVersions("0.258", "0.257") < 0);
    }

    [Fact]
    public void ParseReleaseJsonExtractsMetadataAndInstallerAsset()
    {
        var sampleJson = """
        {
          "tag_name": "v0.258",
          "name": "ANLAbel v0.258 - GitHub Release Auto Update",
          "body": "### Features\n- Added automatic software updates from GitHub Releases.\n- Improved stability.",
          "html_url": "https://github.com/ducancdt/anlabel/releases/tag/v0.258",
          "published_at": "2026-08-17T10:00:00Z",
          "prerelease": false,
          "assets": [
            {
              "name": "checksums.txt",
              "browser_download_url": "https://github.com/ducancdt/anlabel/releases/download/v0.258/checksums.txt",
              "size": 512,
              "content_type": "text/plain"
            },
            {
              "name": "ANLAbel-v0.258-Setup-x64.exe",
              "browser_download_url": "https://github.com/ducancdt/anlabel/releases/download/v0.258/ANLAbel-v0.258-Setup-x64.exe",
              "size": 47185920,
              "content_type": "application/x-msdownload"
            },
            {
              "name": "ANLAbel-v0.258-Portable.zip",
              "browser_download_url": "https://github.com/ducancdt/anlabel/releases/download/v0.258/ANLAbel-v0.258-Portable.zip",
              "size": 45000000,
              "content_type": "application/zip"
            }
          ]
        }
        """;

        var release = GitHubReleaseParser.ParseReleaseJson(sampleJson);

        Assert.NotNull(release);
        Assert.Equal("v0.258", release!.TagName);
        Assert.Equal("0.258", release.VersionString);
        Assert.Equal("ANLAbel v0.258 - GitHub Release Auto Update", release.Title);
        Assert.Contains("Added automatic software updates", release.ReleaseNotes);
        Assert.Equal("https://github.com/ducancdt/anlabel/releases/tag/v0.258", release.HtmlUrl);
        Assert.False(release.IsPreRelease);
        Assert.Equal(3, release.Assets.Count);

        var installer = release.InstallerAsset;
        Assert.NotNull(installer);
        Assert.Equal("ANLAbel-v0.258-Setup-x64.exe", installer!.Name);
        Assert.True(installer.IsInstaller);
        Assert.Equal("https://github.com/ducancdt/anlabel/releases/download/v0.258/ANLAbel-v0.258-Setup-x64.exe", installer.DownloadUrl);
        Assert.Equal(47185920, installer.Size);
        Assert.Equal("45 MB", installer.FormattedSize);
    }

    [Fact]
    public void ParseReleaseJsonHandlesEmptyAndMalformedJsonGracefully()
    {
        Assert.Null(GitHubReleaseParser.ParseReleaseJson(""));
        Assert.Null(GitHubReleaseParser.ParseReleaseJson("{ invalid json }"));
        Assert.Null(GitHubReleaseParser.ParseReleaseJson("[]"));
    }

    [Fact]
    public void UpdateCheckResultEvaluatesAvailabilityCorrectly()
    {
        var release = new ReleaseInfo
        {
            TagName = "v0.259",
            VersionString = "0.259"
        };

        var availableResult = new UpdateCheckResult
        {
            Status = UpdateStatus.UpdateAvailable,
            CurrentVersion = "0.258",
            LatestRelease = release
        };
        Assert.True(availableResult.IsUpdateAvailable);

        var upToDateResult = new UpdateCheckResult
        {
            Status = UpdateStatus.UpToDate,
            CurrentVersion = "0.258",
            LatestRelease = release
        };
        Assert.False(upToDateResult.IsUpdateAvailable);

        var errorResult = new UpdateCheckResult
        {
            Status = UpdateStatus.Error,
            CurrentVersion = "0.258",
            ErrorMessage = "Network error"
        };
        Assert.False(errorResult.IsUpdateAvailable);
    }
}
