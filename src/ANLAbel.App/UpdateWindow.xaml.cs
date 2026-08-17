using System.Windows;
using System.Windows.Media;
using ANLAbel.App.Services;
using ANLAbel.Core.Updates;

namespace ANLAbel.App;

public partial class UpdateWindow : Window
{
    private readonly AppUpdateService _updateService = new();
    private readonly string _currentVersion;
    private UpdateCheckResult? _checkResult;
    private CancellationTokenSource? _downloadCts;

    public UpdateWindow(string? currentVersion = null)
    {
        InitializeComponent();
        _currentVersion = currentVersion
            ?? System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString(2)
            ?? "0.258";
        CurrentVersionText.Text = $"v{_currentVersion}";
        Loaded += UpdateWindow_Loaded;
        Closing += UpdateWindow_Closing;
    }

    private async void UpdateWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await PerformUpdateCheckAsync();
    }

    private void UpdateWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        _downloadCts?.Cancel();
    }

    private async Task PerformUpdateCheckAsync()
    {
        CheckAgainButton.IsEnabled = false;
        InstallButton.IsEnabled = false;
        LatestVersionText.Text = "Đang kiểm tra...";
        ReleaseDateText.Text = string.Empty;
        ReleaseNotesText.Text = "Đang tải thông tin bản phát hành từ GitHub...";
        DownloadProgressSection.Visibility = Visibility.Collapsed;

        SetStatusBadge("Đang kiểm tra...", "#1464D2", "#EAF3FF", "#B7D7FF");

        _checkResult = await _updateService.CheckForUpdatesAsync(_currentVersion);

        CheckAgainButton.IsEnabled = true;

        if (_checkResult.Status == UpdateStatus.UpdateAvailable && _checkResult.LatestRelease is { } release)
        {
            LatestVersionText.Text = $"v{release.VersionString}";
            if (release.PublishedAt is { } pub)
            {
                ReleaseDateText.Text = $"Phát hành: {pub.ToLocalTime():dd/MM/yyyy HH:mm}";
            }

            ReleaseNotesText.Text = string.IsNullOrWhiteSpace(release.ReleaseNotes)
                ? (string.IsNullOrWhiteSpace(release.Title) ? "Không có mô tả chi tiết." : release.Title)
                : release.ReleaseNotes;

            SetStatusBadge("Có bản mới!", "#059669", "#ECFDF5", "#A7F3D0");
            InstallButton.IsEnabled = true;
        }
        else if (_checkResult.Status == UpdateStatus.UpToDate)
        {
            var releaseVer = _checkResult.LatestRelease?.VersionString ?? _currentVersion;
            LatestVersionText.Text = $"v{releaseVer}";
            if (_checkResult.LatestRelease?.PublishedAt is { } pub)
            {
                ReleaseDateText.Text = $"Phát hành: {pub.ToLocalTime():dd/MM/yyyy HH:mm}";
            }

            ReleaseNotesText.Text = $"Bạn đang sử dụng phiên bản mới nhất của ANLAbel (v{_currentVersion}). Không có bản cập nhật nào mới hơn.";
            SetStatusBadge("Đã mới nhất", "#0284C7", "#F0F9FF", "#BAE6FD");
            InstallButton.IsEnabled = false;
        }
        else if (_checkResult.Status == UpdateStatus.Offline)
        {
            LatestVersionText.Text = "Không có kết nối mạng";
            ReleaseNotesText.Text = "Không thể kết nối đến máy chủ GitHub. Vui lòng kiểm tra lại kết nối mạng Internet của bạn và thử lại.";
            SetStatusBadge("Không có mạng", "#D97706", "#FFFBEB", "#FDE68A");
            InstallButton.IsEnabled = false;
        }
        else
        {
            LatestVersionText.Text = "Không thể kiểm tra";
            ReleaseNotesText.Text = _checkResult.ErrorMessage ?? "Đã xảy ra lỗi khi kiểm tra bản cập nhật từ GitHub. Vui lòng thử lại sau hoặc mở trang phát hành trên trình duyệt.";
            SetStatusBadge("Lỗi kiểm tra", "#DC2626", "#FEF2F2", "#FECACA");
            InstallButton.IsEnabled = false;
        }
    }

    private void SetStatusBadge(string text, string fgHex, string bgHex, string borderHex)
    {
        StatusBadgeText.Text = text;
        StatusBadgeText.Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString(fgHex)!;
        StatusBadge.Background = (SolidColorBrush)new BrushConverter().ConvertFromString(bgHex)!;
        StatusBadge.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFromString(borderHex)!;
    }

    private async void InstallButton_Click(object sender, RoutedEventArgs e)
    {
        var release = _checkResult?.LatestRelease;
        var asset = release?.InstallerAsset;

        if (asset is null || string.IsNullOrWhiteSpace(asset.DownloadUrl))
        {
            var result = MessageBox.Show(
                "Không tìm thấy gói cài đặt tự động (.exe) trong bản phát hành này.\nBạn có muốn mở trang GitHub để tải thủ công không?",
                "Tải bản cập nhật",
                MessageBoxButton.YesNo,
                MessageBoxImage.Information);

            if (result == MessageBoxResult.Yes)
            {
                AppUpdateService.OpenReleasePage(release?.HtmlUrl);
            }
            return;
        }

        InstallButton.IsEnabled = false;
        CheckAgainButton.IsEnabled = false;
        CloseButton.IsEnabled = false;
        DownloadProgressSection.Visibility = Visibility.Visible;
        DownloadProgressBar.Value = 0;
        ProgressPercentText.Text = "0%";
        ProgressStatusText.Text = $"Đang tải {asset.Name}...";
        ProgressDetailText.Text = $"Kích thước: {asset.FormattedSize}";

        _downloadCts = new CancellationTokenSource();

        var progress = new Progress<(long downloadedBytes, long totalBytes, double percent)>(p =>
        {
            DownloadProgressBar.Value = p.percent;
            ProgressPercentText.Text = $"{p.percent:0.#}%";
            var downMB = p.downloadedBytes / (1024.0 * 1024.0);
            var totalMB = p.totalBytes / (1024.0 * 1024.0);
            ProgressDetailText.Text = $"{downMB:0.##} MB / {totalMB:0.##} MB";
        });

        try
        {
            var filePath = await _updateService.DownloadUpdateAsync(asset, progress, _downloadCts.Token);

            ProgressStatusText.Text = "Tải hoàn tất! Đang khởi động trình cài đặt...";
            ProgressPercentText.Text = "100%";

            var confirm = MessageBox.Show(
                $"Bản cập nhật đã được tải về thành công ({asset.Name}).\n\nỨng dụng sẽ đóng lại để thực hiện cài đặt phiên bản mới. Bạn có muốn tiếp tục không?",
                "Cập nhật ANLAbel",
                MessageBoxButton.OKCancel,
                MessageBoxImage.Question);

            if (confirm == MessageBoxResult.OK)
            {
                if (!AppUpdateService.LaunchInstaller(filePath, closeAppOnLaunch: true))
                {
                    MessageBox.Show(
                        $"Không thể tự động chạy bộ cài đặt tại:\n{filePath}\n\nVui lòng mở file trên để cài đặt thủ công.",
                        "Lỗi khởi chạy cài đặt",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
            }
            else
            {
                CloseButton.IsEnabled = true;
                InstallButton.IsEnabled = true;
                CheckAgainButton.IsEnabled = true;
            }
        }
        catch (OperationCanceledException)
        {
            ProgressStatusText.Text = "Đã hủy tải bản cập nhật.";
            CloseButton.IsEnabled = true;
            InstallButton.IsEnabled = true;
            CheckAgainButton.IsEnabled = true;
        }
        catch (Exception ex)
        {
            ProgressStatusText.Text = "Tải bản cập nhật thất bại.";
            ProgressDetailText.Text = ex.Message;
            CloseButton.IsEnabled = true;
            InstallButton.IsEnabled = true;
            CheckAgainButton.IsEnabled = true;

            var result = MessageBox.Show(
                $"Không thể tải bản cập nhật: {ex.Message}\n\nBạn có muốn mở trang GitHub để tải trực tiếp từ trình duyệt không?",
                "Lỗi tải cập nhật",
                MessageBoxButton.YesNo,
                MessageBoxImage.Error);

            if (result == MessageBoxResult.Yes)
            {
                AppUpdateService.OpenReleasePage(release?.HtmlUrl);
            }
        }
    }

    private void GitHubButton_Click(object sender, RoutedEventArgs e)
    {
        AppUpdateService.OpenReleasePage(_checkResult?.LatestRelease?.HtmlUrl);
    }

    private async void CheckAgainButton_Click(object sender, RoutedEventArgs e)
    {
        await PerformUpdateCheckAsync();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
