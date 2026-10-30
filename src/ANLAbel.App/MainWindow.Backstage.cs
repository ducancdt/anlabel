using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ANLAbel.Data.Preferences;
using Microsoft.Win32;

namespace ANLAbel.App;

/// <summary>
/// NiceLabel 10 backstage (Tệp; docs/NICELABEL_10_UI_LAYOUT.md, "Backstage"): it covers
/// the ribbon and the workspace. Pages (Bắt đầu, Mới, Mở, Giới thiệu) stay open;
/// commands (Lưu, In, Thoát, ...) close it and then run, so their dialogs open over
/// the designer.
/// </summary>
public partial class MainWindow
{
    private readonly RecentDocumentsService _recentDocuments = new();
    private bool _floatingPropertiesHiddenByBackstage;

    /// <summary>One "Tệp gần đây" row.</summary>
    public sealed record RecentDocumentItem(string Path)
    {
        public string Name => System.IO.Path.GetFileName(Path);

        public string Folder => System.IO.Path.GetDirectoryName(Path) ?? string.Empty;

        public bool IsMissing => !File.Exists(Path);
    }

    private void InitializeBackstage()
    {
        // Ctrl+O shows the Open page with the recent labels, as in NiceLabel 10.
        CommandBindings.Add(new CommandBinding(ApplicationCommands.Open, (_, _) => ShowBackstage("Open")));
        PreviewKeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape && BackstagePanel.Visibility == Visibility.Visible)
            {
                HideBackstage();
                e.Handled = true;
            }
        };
    }

    private void FileMenu_Click(object sender, RoutedEventArgs e)
    {
        ShowBackstage("Start");
        e.Handled = true;
    }

    private void OpenBackstage_Click(object sender, RoutedEventArgs e) => ShowBackstage("Open");

    private void ShowBackstage(string page)
    {
        CommitFocusedEdit();
        if (PropertiesPopup.IsOpen)
        {
            // The floating Properties panel is its own window and would float over the backstage.
            PropertiesPopup.IsOpen = false;
            _floatingPropertiesHiddenByBackstage = true;
        }

        RefreshRecentDocuments();
        BackstagePanel.Visibility = Visibility.Visible;
        var tab = page switch
        {
            "New" => BackstageNewTab,
            "Open" => BackstageOpenTab,
            "About" => BackstageAboutTab,
            "Protection" => BackstageProtectionTab,
            _ => BackstageStartTab
        };
        tab.IsChecked = true;
        ShowBackstagePage(page);
        Dispatcher.BeginInvoke(() => tab.Focus(), System.Windows.Threading.DispatcherPriority.Input);
    }

    private void HideBackstage()
    {
        if (BackstagePanel.Visibility != Visibility.Visible)
        {
            return;
        }

        BackstagePanel.Visibility = Visibility.Collapsed;
        if (_floatingPropertiesHiddenByBackstage)
        {
            _floatingPropertiesHiddenByBackstage = false;
            PropertiesPopup.IsOpen = true;
        }

        FileMenuButton.Focus();
    }

    private void ShowBackstagePage(string page)
    {
        BackstageStartPage.Visibility = page is "Start" ? Visibility.Visible : Visibility.Collapsed;
        BackstageNewPage.Visibility = page is "New" ? Visibility.Visible : Visibility.Collapsed;
        BackstageOpenPage.Visibility = page is "Open" ? Visibility.Visible : Visibility.Collapsed;
        BackstageAboutPage.Visibility = page is "About" ? Visibility.Visible : Visibility.Collapsed;
        BackstageProtectionPage.Visibility = page is "Protection" ? Visibility.Visible : Visibility.Collapsed;
    }

    private void ProtectionEnable_Click(object sender, RoutedEventArgs e)
    {
        string? password = null;
        if (ProtectionRequirePasswordBox.IsChecked == true)
        {
            if (ProtectionPasswordBox.Password.Length < 4)
            {
                MessageBox.Show(this, "Mật khẩu cần ít nhất 4 ký tự.", "Bảo vệ tem", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!string.Equals(ProtectionPasswordBox.Password, ProtectionPasswordConfirmBox.Password, StringComparison.Ordinal))
            {
                MessageBox.Show(this, "Hai lần nhập mật khẩu không khớp.", "Bảo vệ tem", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            password = ProtectionPasswordBox.Password;
        }

        if (!_viewModel.SetEditingProtection(true, password))
        {
            MessageBox.Show(this, _viewModel.StatusText, "Bảo vệ tem", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        ProtectionPasswordBox.Clear();
        ProtectionPasswordConfirmBox.Clear();
    }

    private void ProtectionDisable_Click(object sender, RoutedEventArgs e)
    {
        if (!_viewModel.SetEditingProtection(false, null))
        {
            MessageBox.Show(this, _viewModel.StatusText, "Bảo vệ tem", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void UnlockEditing_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.TryUnlockEditing(UnlockPasswordBox.Password);
        UnlockPasswordBox.Clear();
    }

    private void UnlockPasswordBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            UnlockEditing_Click(sender, e);
        }
    }

    private void BackstageTab_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton { Tag: string page })
        {
            ShowBackstagePage(page);
        }
    }

    private void CloseBackstage_Click(object sender, RoutedEventArgs e) => HideBackstage();

    private void BackstageNewLabel_Click(object sender, RoutedEventArgs e)
    {
        HideBackstage();
        New_Click(sender, e);
    }

    private void BackstageTemplates_Click(object sender, RoutedEventArgs e)
    {
        HideBackstage();
        TemplateLibrary_Click(sender, e);
    }

    private void BackstageBrowse_Click(object sender, RoutedEventArgs e)
    {
        HideBackstage();
        Open_Click(sender, e);
    }

    private async void BackstageImport_Click(object sender, RoutedEventArgs e)
    {
        HideBackstage();
        if (!await ConfirmDocumentCanBeReplacedAsync("nhập tem NiceLabel"))
        {
            return;
        }

        var dialog = new OpenFileDialog
        {
            Title = "Nhập tem NiceLabel",
            Filter = "Tem NiceLabel (*.nlbl;*.lbl)|*.nlbl;*.lbl|Tất cả tệp (*.*)|*.*"
        };
        if (dialog.ShowDialog(this) == true)
        {
            await OpenDocumentPathAsync(dialog.FileName);
        }
    }

    private void BackstageSave_Click(object sender, RoutedEventArgs e)
    {
        HideBackstage();
        Save_Click(sender, e);
    }

    private void BackstageSaveAs_Click(object sender, RoutedEventArgs e)
    {
        HideBackstage();
        SaveAs_Click(sender, e);
    }

    private void BackstagePrint_Click(object sender, RoutedEventArgs e)
    {
        HideBackstage();
        OpenPrintPreview();
    }

    private void BackstageRevisions_Click(object sender, RoutedEventArgs e)
    {
        HideBackstage();
        RevisionHistory_Click(sender, e);
    }

    private void BackstageOptions_Click(object sender, RoutedEventArgs e)
    {
        HideBackstage();
        Settings_Click(sender, e);
    }

    private void BackstagePrinterSetup_Click(object sender, RoutedEventArgs e)
    {
        HideBackstage();
        PrinterSetup_Click(sender, e);
    }

    // Goes through the normal close path, so unsaved changes are still asked about.
    private void BackstageExit_Click(object sender, RoutedEventArgs e)
    {
        HideBackstage();
        SystemCommands.CloseWindow(this);
    }

    private async void BackstageRecent_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: RecentDocumentItem item })
        {
            return;
        }

        if (item.IsMissing)
        {
            var remove = OperatorDialog.Ask(
                this,
                "Không tìm thấy tệp",
                [$"Không tìm thấy “{item.Name}” ở {item.Folder}.", "Tệp có thể đã bị đổi tên, chuyển chỗ hoặc nằm trên ổ chưa kết nối."],
                null,
                OperatorDialogTone.Warning,
                new OperatorDialogChoice("remove", "Bỏ khỏi danh sách", IsDefault: true),
                new OperatorDialogChoice("keep", "Giữ lại", IsCancel: true)) == "remove";
            if (remove)
            {
                ForgetRecentDocument(item.Path);
            }

            return;
        }

        HideBackstage();
        if (await ConfirmDocumentCanBeReplacedAsync("mở mẫu khác"))
        {
            await OpenDocumentPathAsync(item.Path);
        }
    }

    private void RefreshRecentDocuments()
    {
        var items = _recentDocuments.Load().Select(path => new RecentDocumentItem(path)).ToArray();
        StartRecentList.ItemsSource = items;
        OpenRecentList.ItemsSource = items;
        var empty = items.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
        StartRecentEmpty.Visibility = empty;
        OpenRecentEmpty.Visibility = empty;
    }

    /// <summary>A label just opened or saved goes to the top of "Tệp gần đây"; failing to record it never blocks the document.</summary>
    private void RememberRecentDocument(string path)
    {
        try
        {
            _recentDocuments.Add(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            _viewModel.StatusText = "Không ghi được danh sách tệp gần đây: " + ex.Message;
        }
    }

    private void ForgetRecentDocument(string path)
    {
        try
        {
            _recentDocuments.Remove(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _viewModel.StatusText = "Không ghi được danh sách tệp gần đây: " + ex.Message;
        }

        RefreshRecentDocuments();
    }
}
