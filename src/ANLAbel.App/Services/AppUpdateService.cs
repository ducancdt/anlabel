using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Windows;
using ANLAbel.Core.Updates;

namespace ANLAbel.App.Services;

/// <summary>
/// Service for checking software updates against GitHub Releases, downloading setup packages, and launching the installer.
/// </summary>
public sealed class AppUpdateService
{
    private static readonly HttpClient HttpClient = CreateHttpClient();

    public const string DefaultGitHubRepo = "ducancdt/anlabel";
    public const string LatestReleaseUrl = "https://github.com/ducancdt/anlabel/releases/latest";
    public const string ApiLatestReleaseUrl = "https://api.github.com/repos/ducancdt/anlabel/releases/latest";

    private static HttpClient CreateHttpClient()
    {
        var client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(15)
        };
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("ANLAbel-App", "0.258"));
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));
        return client;
    }

    /// <summary>
    /// Checks for updates asynchronously without blocking the UI thread.
    /// </summary>
    public async Task<UpdateCheckResult> CheckForUpdatesAsync(string currentVersion, CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, ApiLatestReleaseUrl);
            using var response = await HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);

            if (response.StatusCode == System.Net.HttpStatusCode.Forbidden || response.StatusCode == (System.Net.HttpStatusCode)429)
            {
                return new UpdateCheckResult
                {
                    Status = UpdateStatus.Error,
                    CurrentVersion = currentVersion,
                    ErrorMessage = "GitHub API rate limit exceeded. Please try again later or visit the release page directly."
                };
            }

            if (!response.IsSuccessStatusCode)
            {
                return new UpdateCheckResult
                {
                    Status = UpdateStatus.Error,
                    CurrentVersion = currentVersion,
                    ErrorMessage = $"Server returned HTTP {(int)response.StatusCode} ({response.ReasonPhrase})"
                };
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            var release = GitHubReleaseParser.ParseReleaseJson(json);

            if (release is null || string.IsNullOrWhiteSpace(release.TagName))
            {
                return new UpdateCheckResult
                {
                    Status = UpdateStatus.Error,
                    CurrentVersion = currentVersion,
                    ErrorMessage = "Could not parse release metadata from GitHub response."
                };
            }

            var isNewer = GitHubReleaseParser.IsNewerVersion(currentVersion, release.TagName);

            return new UpdateCheckResult
            {
                Status = isNewer ? UpdateStatus.UpdateAvailable : UpdateStatus.UpToDate,
                CurrentVersion = currentVersion,
                LatestRelease = release
            };
        }
        catch (HttpRequestException ex)
        {
            return new UpdateCheckResult
            {
                Status = UpdateStatus.Offline,
                CurrentVersion = currentVersion,
                ErrorMessage = $"Network connection error: {ex.Message}"
            };
        }
        catch (TaskCanceledException)
        {
            return new UpdateCheckResult
            {
                Status = UpdateStatus.Error,
                CurrentVersion = currentVersion,
                ErrorMessage = "Update check request timed out."
            };
        }
        catch (Exception ex)
        {
            return new UpdateCheckResult
            {
                Status = UpdateStatus.Error,
                CurrentVersion = currentVersion,
                ErrorMessage = $"Unexpected error checking updates: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Downloads the specified release asset to a local temporary folder with progress reporting.
    /// </summary>
    public async Task<string> DownloadUpdateAsync(
        ReleaseAssetInfo asset,
        IProgress<(long downloadedBytes, long totalBytes, double percent)>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(asset.DownloadUrl))
        {
            throw new InvalidOperationException("Asset has no download URL.");
        }

        var tempDir = Path.Combine(Path.GetTempPath(), "ANLAbel_Updates");
        Directory.CreateDirectory(tempDir);

        var safeFileName = string.IsNullOrWhiteSpace(asset.Name) ? "ANLAbel-Setup.exe" : asset.Name;
        var destinationPath = Path.Combine(tempDir, safeFileName);

        using var response = await HttpClient.GetAsync(asset.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength ?? asset.Size;

        await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        await using var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true);

        var buffer = new byte[81920];
        long totalDownloaded = 0;
        int bytesRead;

        while ((bytesRead = await contentStream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken).ConfigureAwait(false)) > 0)
        {
            await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken).ConfigureAwait(false);
            totalDownloaded += bytesRead;

            if (totalBytes > 0)
            {
                var percent = Math.Clamp((double)totalDownloaded / totalBytes * 100.0, 0, 100.0);
                progress?.Report((totalDownloaded, totalBytes, percent));
            }
            else
            {
                progress?.Report((totalDownloaded, 0, 0));
            }
        }

        return destinationPath;
    }

    /// <summary>
    /// Launches the downloaded installer and optionally exits the current application.
    /// </summary>
    public static bool LaunchInstaller(string installerFilePath, bool closeAppOnLaunch = true)
    {
        if (!File.Exists(installerFilePath))
        {
            return false;
        }

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = installerFilePath,
                UseShellExecute = true
            };

            Process.Start(startInfo);

            if (closeAppOnLaunch)
            {
                Application.Current?.Dispatcher.BeginInvoke(new Action(() =>
                {
                    Application.Current.Shutdown();
                }));
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Opens the GitHub releases page in the default web browser.
    /// </summary>
    public static void OpenReleasePage(string? url = null)
    {
        var targetUrl = string.IsNullOrWhiteSpace(url) ? LatestReleaseUrl : url;
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = targetUrl,
                UseShellExecute = true
            });
        }
        catch
        {
            // Ignore failure if browser cannot be launched
        }
    }
}
