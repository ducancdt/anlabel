using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ANLAbel.App.Services;

namespace ANLAbel.App;

/// <summary>
/// Ribbon commands added with the NiceLabel 10 shell (docs/NICELABEL_10_UI_LAYOUT.md,
/// "Shell rebuild"): the Data tab database wizard, View zoom and gridlines, Help links.
/// </summary>
public partial class MainWindow
{
    // ---- Dữ liệu → Cơ sở dữ liệu ----------------------------------------------------------

    private void ImportTextFile_Click(object sender, RoutedEventArgs e)
    {
        new ExcelImportWindow(_viewModel, ExcelImportStart.BrowseTextFile) { Owner = this }.ShowDialog();
    }

    private void EditDatabase_Click(object sender, RoutedEventArgs e)
    {
        new ExcelImportWindow(_viewModel) { Owner = this }.ShowDialog();
    }

    private void SharedDataSources_Click(object sender, RoutedEventArgs e) => ShowDatabaseManager();

    // ---- Xem → Thu phóng ------------------------------------------------------------------

    private void ZoomPercentBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ZoomPercentBox.SelectedItem is ComboBoxItem { Content: string text })
        {
            ApplyZoomText(text);
            ZoomPercentBox.SelectedIndex = -1;
        }
    }

    private void ZoomPercentBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            ApplyZoomText(ZoomPercentBox.Text);
            e.Handled = true;
        }
    }

    private void ZoomPercentBox_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e) => ApplyZoomText(ZoomPercentBox.Text);

    /// <summary>"150", "150%" or "150 %" sets the zoom (25–400 %); anything else shows the current zoom again.</summary>
    private void ApplyZoomText(string? text)
    {
        var number = (text ?? string.Empty).Replace("%", string.Empty).Trim().Replace(',', '.');
        if (double.TryParse(number, NumberStyles.Float, CultureInfo.InvariantCulture, out var percent) && percent > 0)
        {
            _viewModel.Zoom = Math.Clamp(percent / 100.0, 0.25, 4.0);
        }

        ZoomPercentBox.SetCurrentValue(ComboBox.TextProperty, $"{_viewModel.ZoomPercent}%");
    }

    // ---- Xem → Lưới -------------------------------------------------------------------------

    private void GridlinesMenu_Opened(object sender, RoutedEventArgs e)
    {
        ShowGridMenuItem.IsChecked = DesignerCanvas.ShowGrid;
        foreach (var item in GridlinesMenu.Items.OfType<MenuItem>())
        {
            if (GridStepOf(item) is { } step)
            {
                item.IsChecked = Math.Abs(step - DesignerCanvas.GridStepMm) < 0.001;
            }
        }
    }

    private void ShowGridMenuItem_Click(object sender, RoutedEventArgs e)
    {
        DesignerCanvas.ShowGrid = ShowGridMenuItem.IsChecked;
    }

    private void GridStepMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem item && GridStepOf(item) is { } step)
        {
            DesignerCanvas.GridStepMm = step;
            DesignerCanvas.ShowGrid = true;
        }
    }

    private static double? GridStepOf(MenuItem item) =>
        item.Tag is string text && double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var step) ? step : null;

    // ---- Trợ giúp ---------------------------------------------------------------------------

    private void UserGuides_Click(object sender, RoutedEventArgs e)
    {
        // The installer ships the guide next to the program (installer/ANLAbel-x64.iss).
        var guide = Path.Combine(AppContext.BaseDirectory, "docs", "huong-dan-su-dung-ANLAbel.txt");
        if (File.Exists(guide))
        {
            OpenExternal(guide);
        }
        else
        {
            ShowHelpTopic("quickstart");
        }
    }

    private void TechnicalSupport_Click(object sender, RoutedEventArgs e) => OpenExternal(AppUpdateService.ProjectIssuesUrl);

    private void HomePage_Click(object sender, RoutedEventArgs e) => OpenExternal(AppUpdateService.ProjectHomeUrl);

    private void About_Click(object sender, RoutedEventArgs e) => ShowHelpTopic("about");

    private void ShowHelpTopic(string topic)
    {
        var help = new HelpWindow { Owner = this };
        help.ShowTopic(topic);
        help.ShowDialog();
    }

    private void OpenExternal(string target)
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = target, UseShellExecute = true });
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException or FileNotFoundException)
        {
            OperatorDialog.Ask(
                this,
                "Không mở được",
                [$"Không mở được {target}.", "Kiểm tra trình duyệt hoặc chương trình mở tệp mặc định của Windows rồi thử lại."],
                $"Chi tiết kỹ thuật: {ex.Message}",
                OperatorDialogTone.Warning,
                new OperatorDialogChoice("ok", "Đóng", IsDefault: true, IsCancel: true));
        }
    }
}
