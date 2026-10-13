using System.ComponentModel;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using ANLAbel.App.Services;
using ANLAbel.App.ViewModels;
using ANLAbel.Core.Models;
using Microsoft.Win32;

namespace ANLAbel.App;

/// <summary>
/// Plan 13 GĐ2 (F01–F07): no lost edits. Unsaved-change protection on New,
/// Open, library open, revision restore and Close; Save As and the advertised
/// shortcuts; periodic crash-recovery copies and the startup recovery offer;
/// startup configuration notices; opening a file passed by Explorer.
/// UI states follow Figma ANLAbel-Shell node 36:137.
/// </summary>
public partial class MainWindow
{
    private const string DocumentFileFilter = "Mẫu tem ANLAbel (*.anlabel)|*.anlabel|Tệp JSON (*.json)|*.json|Tất cả tệp (*.*)|*.*";
    private static readonly TimeSpan AutosaveInterval = TimeSpan.FromSeconds(60);

    private readonly DocumentRecoveryService _recovery = new();
    private DispatcherTimer? _autosaveTimer;
    private bool _closeConfirmed;

    private enum SaveChoice
    {
        Save,
        Discard,
        Cancel
    }

    /// <summary>A document passed on the command line (double-click in Explorer).</summary>
    public string? StartupFilePath { get; init; }

    /// <summary>
    /// Set by the unexpected-error handler: close without the save prompt and
    /// keep this session's recovery copy so it is offered at the next start.
    /// </summary>
    internal bool SuppressClosePrompt { get; set; }

    /// <summary>The first existing .anlabel/.json path among command-line arguments.</summary>
    public static string? FindDocumentArgument(IEnumerable<string> arguments)
    {
        return arguments.FirstOrDefault(argument =>
            !string.IsNullOrWhiteSpace(argument)
            && (argument.EndsWith(".anlabel", StringComparison.OrdinalIgnoreCase)
                || argument.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            && File.Exists(argument));
    }

    private void InitializeDocumentLifecycle()
    {
        SetBinding(TitleProperty, new Binding(nameof(MainViewModel.WindowTitle)));
        CommandBindings.Add(new CommandBinding(ApplicationCommands.New, (sender, e) => New_Click(sender, e)));
        CommandBindings.Add(new CommandBinding(ApplicationCommands.Open, (sender, e) => Open_Click(sender, e)));
        CommandBindings.Add(new CommandBinding(ApplicationCommands.Save, (sender, e) => Save_Click(sender, e)));
        CommandBindings.Add(new CommandBinding(ApplicationCommands.SaveAs, (sender, e) => SaveAs_Click(sender, e)));
        InputBindings.Add(new KeyBinding(ApplicationCommands.SaveAs, Key.S, ModifierKeys.Control | ModifierKeys.Shift));
        Closing += MainWindow_Closing;
        Closed += MainWindow_Closed;
    }

    /// <summary>Runs after the window is shown: notices, recovery offer, Explorer file, autosave.</summary>
    private async Task CompleteStartupAsync()
    {
        // Autosave starts first: opening a file or the recovery offer can wait
        // on the spooler or a slow share, and the session must be protected
        // meanwhile (audit 2026-10-13).
        _autosaveTimer = new DispatcherTimer(DispatcherPriority.Background) { Interval = AutosaveInterval };
        _autosaveTimer.Tick += AutosaveTimer_Tick;
        _autosaveTimer.Start();

        foreach (var notice in _viewModel.StartupNotices)
        {
            OperatorDialog.Ask(
                this,
                "Đã khôi phục cấu hình",
                [notice],
                null,
                OperatorDialogTone.Warning,
                new OperatorDialogChoice("ok", "Đã hiểu", IsDefault: true, IsCancel: true));
        }

        var recoveredOpened = await OfferRecoveryAsync();
        if (!string.IsNullOrWhiteSpace(StartupFilePath))
        {
            if (recoveredOpened)
            {
                _viewModel.StatusText = $"Chưa mở {Path.GetFileName(StartupFilePath)} vì đang mở bản phục hồi. Hãy lưu bản phục hồi rồi mở lại tệp.";
            }
            else
            {
                await OpenDocumentPathAsync(StartupFilePath);
            }
        }
    }

    /// <summary>
    /// Before the update installer closes ANLAbel: ask to save like Close does.
    /// Returns false when the operator cancels; the installer must not start.
    /// </summary>
    internal async Task<bool> PrepareForUpdateInstallAsync()
    {
        if (!await ConfirmDocumentCanBeReplacedAsync("cài đặt bản cập nhật"))
        {
            return false;
        }

        _closeConfirmed = true;
        return true;
    }

    /// <summary>
    /// Windows is logging off or shutting down: there is no time to ask, and a
    /// prompt's answer would be ignored. Keep the edits as a recovery copy that
    /// is offered at the next start.
    /// </summary>
    internal void PrepareForSessionEnd()
    {
        CommitFocusedEdit();
        TryWriteRecoveryNow();
        SuppressClosePrompt = true;
    }

    private async void SaveAs_Click(object sender, RoutedEventArgs e)
    {
        await SaveDocumentAsync(saveAs: true);
    }

    /// <summary>
    /// Saves the open document. Returns true when it was written. A failure is
    /// reported in plain language; the edits stay open and in a recovery copy.
    /// </summary>
    private async Task<bool> SaveDocumentAsync(bool saveAs)
    {
        CommitFocusedEdit();
        if (_viewModel.IsSaving)
        {
            _viewModel.StatusText = "Đang lưu mẫu tem…";
            return false;
        }

        var filePath = _viewModel.CurrentFilePath;
        if (saveAs || string.IsNullOrWhiteSpace(filePath))
        {
            var dialog = new SaveFileDialog
            {
                Title = saveAs ? "Lưu thành mẫu tem ANLAbel" : "Lưu mẫu tem ANLAbel",
                Filter = DocumentFileFilter,
                DefaultExt = ".anlabel",
                AddExtension = true,
                FileName = $"{_viewModel.DocumentDisplayName}.anlabel"
            };
            if (!string.IsNullOrWhiteSpace(filePath) && Path.GetDirectoryName(filePath) is { Length: > 0 } directory)
            {
                dialog.InitialDirectory = directory;
            }

            if (dialog.ShowDialog(this) != true)
            {
                return false;
            }

            filePath = dialog.FileName;
        }

        try
        {
            await _viewModel.SaveAsync(filePath);
            _recovery.DeleteSession();
            return true;
        }
        catch (DocumentChangedOnDiskException)
        {
            var choice = OperatorDialog.Ask(
                this,
                "Tệp đã được lưu ở nơi khác",
                [
                    $"“{Path.GetFileName(filePath)}” đã được lưu từ cửa sổ ANLAbel khác hoặc máy khác sau khi bạn mở.",
                    "Ghi đè sẽ thay bản đó bằng bản của bạn; bản kia vẫn được giữ trong Lịch sử phiên bản. Lưu thành tệp khác sẽ giữ nguyên cả hai."
                ],
                null,
                OperatorDialogTone.Warning,
                new OperatorDialogChoice("saveas", "Lưu thành tệp khác…", IsDefault: true),
                new OperatorDialogChoice("overwrite", "Ghi đè"),
                new OperatorDialogChoice("cancel", "Hủy", IsCancel: true));
            switch (choice)
            {
                case "saveas":
                    return await SaveDocumentAsync(saveAs: true);
                case "overwrite":
                    try
                    {
                        await _viewModel.SaveAsync(filePath, overwriteExternalChanges: true);
                        _recovery.DeleteSession();
                        return true;
                    }
                    catch (Exception overwriteFailure)
                    {
                        _viewModel.StatusText = "Chưa lưu được: " + ANLAbel.App.Services.OperationFailureExplainer.Explain(overwriteFailure).StatusLine;
                        TryWriteRecoveryNow();
                        return false;
                    }

                default:
                    return false;
            }
        }
        catch (Exception ex)
        {
            // Every failure here means "not saved"; the operator needs the reason
            // and the reassurance that the edits are still open.
            var protectedCopy = TryWriteRecoveryNow();
            OperatorDialog.Ask(
                this,
                "Không lưu được mẫu tem",
                [
                    $"Chưa lưu được “{Path.GetFileName(filePath)}”: {ANLAbel.App.Services.OperationFailureExplainer.WithTechnicalIfGeneric(ANLAbel.App.Services.TemplateFailureExplainer.Explain(ex))}",
                    protectedCopy
                        ? "Thay đổi vẫn còn trong ANLAbel và đã có bản phục hồi tự động. Hãy kiểm tra quyền ghi hoặc dung lượng ổ đĩa rồi lưu lại, hoặc dùng Lưu thành… để lưu ra chỗ khác."
                        : "Thay đổi vẫn còn trong ANLAbel. Hãy kiểm tra quyền ghi hoặc dung lượng ổ đĩa rồi lưu lại, hoặc dùng Lưu thành… để lưu ra chỗ khác."
                ],
                null,
                OperatorDialogTone.Error,
                new OperatorDialogChoice("ok", "Đóng", IsDefault: true, IsCancel: true));
            return false;
        }
    }

    /// <summary>Returns true when the caller may replace or close the open document.</summary>
    private async Task<bool> ConfirmDocumentCanBeReplacedAsync(string actionPhrase)
    {
        CommitFocusedEdit();
        if (!_viewModel.HasUnsavedChanges())
        {
            return true;
        }

        return AskSaveChanges(actionPhrase) switch
        {
            SaveChoice.Save => await SaveDocumentAsync(saveAs: false),
            SaveChoice.Discard => true,
            _ => false
        };
    }

    private SaveChoice AskSaveChanges(string actionPhrase)
    {
        var choice = OperatorDialog.Ask(
            this,
            "Lưu thay đổi?",
            [
                $"Mẫu tem “{_viewModel.DocumentDisplayName}” có thay đổi chưa lưu.",
                $"Lưu trước khi {actionPhrase}?"
            ],
            null,
            OperatorDialogTone.Normal,
            new OperatorDialogChoice("save", "Lưu", IsDefault: true),
            new OperatorDialogChoice("discard", "Không lưu"),
            new OperatorDialogChoice("cancel", "Hủy", IsCancel: true));
        return choice switch
        {
            "save" => SaveChoice.Save,
            "discard" => SaveChoice.Discard,
            _ => SaveChoice.Cancel
        };
    }

    private async Task OpenDocumentPathAsync(string filePath)
    {
        try
        {
            await _viewModel.OpenAsync(filePath);
            await _viewModel.RefreshPrinterQueueStatusAsync();
        }
        catch (Exception ex)
        {
            var explained = ANLAbel.App.Services.TemplateFailureExplainer.Explain(ex);
            OperatorDialog.Ask(
                this,
                "Không mở được mẫu tem",
                [$"Không mở được “{Path.GetFileName(filePath)}”: {explained.Problem}", explained.NextAction, "Mẫu đang mở không bị thay đổi."],
                $"{filePath} — Chi tiết kỹ thuật: {explained.Technical}",
                OperatorDialogTone.Error,
                new OperatorDialogChoice("ok", "Đóng", IsDefault: true, IsCancel: true));
        }
    }

    private void MainWindow_Closing(object? sender, CancelEventArgs e)
    {
        if (_closeConfirmed || SuppressClosePrompt)
        {
            return;
        }

        CommitFocusedEdit();
        if (!_viewModel.HasUnsavedChanges())
        {
            _closeConfirmed = true;
            return;
        }

        switch (AskSaveChanges("đóng ANLAbel"))
        {
            case SaveChoice.Discard:
                _closeConfirmed = true;
                return;
            case SaveChoice.Save:
                // Saving is asynchronous: cancel this close and close again once written.
                e.Cancel = true;
                _ = SaveThenCloseAsync();
                return;
            default:
                e.Cancel = true;
                return;
        }
    }

    private async Task SaveThenCloseAsync()
    {
        if (await SaveDocumentAsync(saveAs: false))
        {
            CommitFocusedEdit();
            if (_viewModel.HasUnsavedChanges())
            {
                _viewModel.StatusText = "Đã lưu, nhưng có thay đổi mới trong lúc lưu. ANLAbel chưa đóng để bạn lưu tiếp.";
                return;
            }

            _closeConfirmed = true;
            Close();
        }
    }

    private void MainWindow_Closed(object? sender, EventArgs e)
    {
        _autosaveTimer?.Stop();
        if (_closeConfirmed)
        {
            // Saved, explicitly discarded, or nothing unsaved: nothing to recover.
            _recovery.DeleteSession();
        }
        else if (!SuppressClosePrompt)
        {
            // Closed without an answer that took effect (application shutdown
            // ignores Cancel and drops an async save): keep the edits so the
            // next start offers them (audit 2026-10-13).
            TryWriteRecoveryNow();
        }
    }

    private void AutosaveTimer_Tick(object? sender, EventArgs e)
    {
        if (_viewModel.IsSaving)
        {
            return;
        }

        LabelTemplate? copy;
        try
        {
            copy = _viewModel.CaptureRecoveryCopy();
        }
        catch (JsonException)
        {
            return;
        }

        var originalPath = _viewModel.CurrentFilePath;
        var documentName = _viewModel.DocumentDisplayName;
        var originalIdentity = _viewModel.DocumentFileIdentity;
        _ = Task.Run(() =>
        {
            try
            {
                if (copy is null)
                {
                    _recovery.DeleteSession();
                }
                else
                {
                    _recovery.Write(copy, originalPath, documentName, originalIdentity);
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // The next tick retries; the open document is unaffected.
            }
        });
    }

    /// <summary>
    /// Writes this session's recovery copy synchronously. Used on save failure
    /// and by the unexpected-error handler, so it never throws.
    /// </summary>
    internal bool TryWriteRecoveryNow()
    {
        try
        {
            var copy = _viewModel.CaptureRecoveryCopy();
            if (copy is null)
            {
                return false;
            }

            _recovery.Write(copy, _viewModel.CurrentFilePath, _viewModel.DocumentDisplayName, _viewModel.DocumentFileIdentity);
            return true;
        }
        catch (Exception)
        {
            // Best effort while another failure is already being handled.
            return false;
        }
    }

    internal string CurrentDocumentDisplayName => _viewModel.DocumentDisplayName;

    private async Task<bool> OfferRecoveryAsync()
    {
        IReadOnlyList<RecoveryEntry> orphans;
        try
        {
            orphans = _recovery.FindOrphans();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }

        foreach (var entry in orphans)
        {
            var choice = OperatorDialog.Ask(
                this,
                "Tìm thấy bản phục hồi",
                [
                    $"Lần trước ANLAbel đóng khi mẫu “{entry.DocumentName}” còn thay đổi chưa lưu.",
                    $"Bản phục hồi lúc {entry.SavedAtLocal:HH:mm} ngày {entry.SavedAtLocal:dd/MM/yyyy}."
                ],
                "Mở bản phục hồi sẽ mở mẫu ở trạng thái chưa lưu; bấm Lưu để ghi lại. Tệp gốc chưa bị thay đổi.",
                OperatorDialogTone.Normal,
                new OperatorDialogChoice("open", "Mở bản phục hồi", IsDefault: true),
                new OperatorDialogChoice("later", "Để sau", IsCancel: true),
                new OperatorDialogChoice("delete", "Xóa bản phục hồi"));
            if (choice == "delete")
            {
                DocumentRecoveryService.Delete(entry);
                continue;
            }

            if (choice != "open")
            {
                continue;
            }

            try
            {
                await _viewModel.OpenRecoveredAsync(entry.DocumentPath, entry.OriginalFilePath, entry.DocumentName, entry.OriginalFileIdentity);
                await _viewModel.RefreshPrinterQueueStatusAsync();
                // This session must protect the recovered content before the old copy goes.
                if (TryWriteRecoveryNow())
                {
                    DocumentRecoveryService.Delete(entry);
                }
                else
                {
                    _viewModel.StatusText = "Đã mở bản phục hồi. Chưa ghi được bản phục hồi mới nên bản cũ vẫn được giữ; hãy lưu mẫu sớm.";
                }

                return true;
            }
            catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException or JsonException)
            {
                OperatorDialog.Ask(
                    this,
                    "Không mở được bản phục hồi",
                    [ANLAbel.App.Services.OperationFailureExplainer.WithTechnicalIfGeneric(ANLAbel.App.Services.TemplateFailureExplainer.Explain(ex)), "Bản phục hồi vẫn được giữ lại để thử lần sau."],
                    entry.DocumentPath,
                    OperatorDialogTone.Error,
                    new OperatorDialogChoice("ok", "Đóng", IsDefault: true, IsCancel: true));
            }
        }

        return false;
    }

    /// <summary>
    /// Pushes a pending text/editable-combo edit into its binding so Save, New,
    /// Open and Close see the value the operator just typed.
    /// </summary>
    private static void CommitFocusedEdit()
    {
        switch (Keyboard.FocusedElement)
        {
            case TextBox textBox:
                textBox.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
                break;
            case ComboBox { IsEditable: true } comboBox:
                comboBox.GetBindingExpression(ComboBox.TextProperty)?.UpdateSource();
                break;
        }
    }
}
