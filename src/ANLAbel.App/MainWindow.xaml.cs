using System.Windows;
using System.IO;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using ANLAbel.App.Controls;
using ANLAbel.App.ViewModels;
using ANLAbel.Core.Printing;
using ANLAbel.Core.Enums;
using ANLAbel.Core.Geometry;
using ANLAbel.Core.Models;
using ANLAbel.Core.Text;
using ANLAbel.Data.PrintLogs;
using ANLAbel.Printing.RenderPipeline;
using Microsoft.Win32;

namespace ANLAbel.App;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel = new();
    private OperationsOverviewWindow? _operationsOverviewWindow;
    private PrintQueueConsoleWindow? _printQueueConsoleWindow;
    private PrintHistoryWindow? _printHistoryWindow;
    private AnalyticsWindow? _analyticsWindow;
    private DocumentLibraryWindow? _documentLibraryWindow;
    private LocalMaintenanceWindow? _localMaintenanceWindow;
    private DataWorkspaceWindow? _dataWorkspaceWindow;
    private DocumentWorkflowWindow? _documentWorkflowWindow;
    private AutomationWindow? _automationWindow;
    private bool _syncingContentSource;
    private bool _syncingExcelField;
    private bool _syncingRulerOffsets;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = _viewModel;
        DesignerCanvas.EditGestureStarted += (_, _) => _viewModel.BeginTemplateEditGesture();
        DesignerCanvas.EditGestureCompleted += (_, _) => _viewModel.CommitTemplateEditGesture();
        DesignerCanvas.EditGestureCanceled += (_, _) => _viewModel.CancelTemplateEditGesture();
        _viewModel.PropertyChanged += ViewModel_PropertyChanged;
        InputBindings.Add(new System.Windows.Input.KeyBinding(
            new ViewModels.RelayCommand(OpenPrintPreview),
            new System.Windows.Input.KeyGesture(System.Windows.Input.Key.P, System.Windows.Input.ModifierKeys.Control)));
        InputBindings.Add(new System.Windows.Input.KeyBinding(
            _viewModel.UndoCommand,
            new System.Windows.Input.KeyGesture(System.Windows.Input.Key.Z, System.Windows.Input.ModifierKeys.Control)));
        InputBindings.Add(new System.Windows.Input.KeyBinding(
            _viewModel.RedoCommand,
            new System.Windows.Input.KeyGesture(System.Windows.Input.Key.Y, System.Windows.Input.ModifierKeys.Control)));
        InputBindings.Add(new System.Windows.Input.KeyBinding(
            ANLAbel.App.Controls.LabelDesignerCanvas.DeleteSelectionCommand,
            new System.Windows.Input.KeyGesture(System.Windows.Input.Key.Delete)));
        InputBindings.Add(new System.Windows.Input.KeyBinding(
            new ViewModels.RelayCommand(() => new HelpWindow { Owner = this }.ShowDialog()),
            new System.Windows.Input.KeyGesture(System.Windows.Input.Key.F1)));
        PreviewKeyDown += MainWindow_PreviewKeyDown;
        PreviewMouseWheel += MainWindow_PreviewMouseWheel;
        DataObject.AddPastingHandler(ObjectTextBox, ObjectTextBox_Pasting);
    }

    private void MainWindow_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (e.OriginalSource is DependencyObject source && FindAncestor<ComboBox>(source) is { IsDropDownOpen: false })
        {
            e.Handled = true;
        }
    }

    private void DesignerScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        SyncRulerOffsets();
    }

    private void DesignerCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        // A zoom or template resize changes the scroll extent. Reapply the
        // single artboard offset after WPF measures the new canvas so rulers
        // cannot retain a stale independent scroll position.
        SyncRulerOffsets();
    }

    private void Ruler_GuideDragStarted(object sender, RulerGuideDragEventArgs e)
    {
        DesignerCanvas.BeginGuideDrag(
            e.RulerOrientation == System.Windows.Controls.Orientation.Horizontal
                ? LabelGuideOrientation.Vertical
                : LabelGuideOrientation.Horizontal,
            e.PositionMm);
    }

    private void Ruler_GuideDragging(object sender, RulerGuideDragEventArgs e)
    {
        DesignerCanvas.UpdateGuideDrag(e.PositionMm);
    }

    private void Ruler_GuideDragCompleted(object sender, RulerGuideDragEventArgs e)
    {
        DesignerCanvas.CompleteGuideDrag(e.PositionMm);
    }

    private void Ruler_GuideDragCanceled(object? sender, EventArgs e)
    {
        DesignerCanvas.CancelGuideDrag();
    }

    private void SyncRulerOffsets()
    {
        if (_syncingRulerOffsets
            || DesignerScrollViewer is null
            || HorizontalRulerScrollViewer is null
            || VerticalRulerScrollViewer is null)
        {
            return;
        }

        _syncingRulerOffsets = true;
        try
        {
            HorizontalRulerScrollViewer.ScrollToHorizontalOffset(DesignerScrollViewer.HorizontalOffset);
            VerticalRulerScrollViewer.ScrollToVerticalOffset(DesignerScrollViewer.VerticalOffset);
        }
        finally
        {
            _syncingRulerOffsets = false;
        }
    }

    private static T? FindAncestor<T>(DependencyObject? source) where T : DependencyObject
    {
        while (source is not null)
        {
            if (source is T match)
            {
                return match;
            }

            source = System.Windows.Media.VisualTreeHelper.GetParent(source);
        }

        return null;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        Loaded -= MainWindow_Loaded;

        ApplyAutomaticDisplayLayout();
        SyncRulerOffsets();

        UpdateContentSourceSelection();
        await _viewModel.RefreshPrinterQueueStatusAsync();
        await _viewModel.RefreshPrintRecoveryAsync();
    }

    private void ReviewPrintRecovery_Click(object sender, RoutedEventArgs e)
    {
        ShowPrintCenter();
    }

    private void ShowPrintCenter()
    {
        var center = new PrintCenterWindow(_viewModel, OpenApprovedReprintPreview)
        {
            Owner = this
        };
        center.ShowDialog();
    }

    private void OperationsOverview_Click(object sender, RoutedEventArgs e)
    {
        if (_operationsOverviewWindow is { IsVisible: true })
        {
            _operationsOverviewWindow.Activate();
            return;
        }

        var overview = new OperationsOverviewWindow(
            _viewModel,
            ShowPrinterSetupDialog,
            ShowPrintCenter,
            ShowPrintHistory)
        {
            Owner = this
        };
        overview.Closed += (_, _) => _operationsOverviewWindow = null;
        _operationsOverviewWindow = overview;
        overview.Show();
    }

    private void PrintQueueConsole_Click(object sender, RoutedEventArgs e)
    {
        if (_printQueueConsoleWindow is { IsVisible: true })
        {
            _printQueueConsoleWindow.Activate();
            return;
        }

        var console = new PrintQueueConsoleWindow(
            _viewModel,
            ShowPrinterSetupDialog,
            ShowPrintCenter,
            ShowPrintHistory)
        {
            Owner = this
        };
        console.Closed += (_, _) => _printQueueConsoleWindow = null;
        _printQueueConsoleWindow = console;
        console.Show();
    }

    private void OpenApprovedReprintPreview(PrintJobRecoveryCandidate candidate)
    {
        if (candidate.Manifest is null || !candidate.Manifest.IsFingerprintValid)
        {
            MessageBox.Show(
                this,
                "Lệnh in đã duyệt không có bản kê khai (manifest) hợp lệ nên không thể mở xem trước bảo vệ.",
                "Trung tâm in ấn",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        OpenPrintPreviewDialog(candidate.JobId, candidate.Manifest);
    }

    private async void ReviewPrintRecoveryLegacy_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await _viewModel.RefreshPrintRecoveryAsync();
            var report = _viewModel.PrintRecoveryReport;
            if (!report.HasPendingJobs && !report.RequiresRepair)
            {
                MessageBox.Show(this, report.UserFacingSummary, "Phục hồi in ấn", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var queueCandidate = report.Candidates.FirstOrDefault(
                candidate => candidate.Action == ANLAbel.Data.PrintLogs.PrintJobRecoveryAction.ReconcileQueue);
            if (queueCandidate is not null)
            {
                var queryChoice = MessageBox.Show(
                    this,
                    $"Đã có thông tin định danh máy in / lệnh in an toàn cho {queueCandidate.JobId}. Bạn có muốn truy vấn hàng đợi ngay bây giờ?\n\nThao tác này chỉ đọc trạng thái bộ đệm in và không bao giờ in lại.",
                    "Truy vấn hàng đợi in",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);
                if (queryChoice == MessageBoxResult.Yes)
                {
                    var reconciliation = await _viewModel.ReconcilePrintJobAsync(queueCandidate.JobId);
                    MessageBox.Show(
                        this,
                        reconciliation.Summary,
                        "Đối soát hàng đợi",
                        MessageBoxButton.OK,
                        reconciliation.Outcome == ANLAbel.Data.PrintLogs.PrintJobReconciliationOutcome.QueueObserved
                            ? MessageBoxImage.Information
                            : MessageBoxImage.Warning);

                    if (reconciliation.Outcome != PrintJobReconciliationOutcome.QueueObserved)
                    {
                        await OfferOperatorDecisionActionsAsync(queueCandidate);
                    }
                    return;
                }
            }

            var details = report.Candidates.Count == 0
                ? "Không có bản ghi lệnh in hoàn chỉnh nào."
                : string.Join(
                    Environment.NewLine,
                    report.Candidates.Select(candidate =>
                        $"• {candidate.JobId} — {candidate.State} — {candidate.Action}\n  {candidate.Reason}"));
            var message = $"{report.UserFacingSummary}\n\n{details}";
            if (report.StoreDiagnostics.Count > 0)
            {
                message += $"\n\nChẩn đoán:\n{string.Join(Environment.NewLine, report.StoreDiagnostics)}";
            }

            MessageBox.Show(
                this,
                message + "\n\nKhông thử lại tự động cho đến khi định danh hàng đợi/lệnh in được đối soát.",
                "Xem xét phục hồi in ấn",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            if (report.Candidates.Count == 1)
            {
                await OfferOperatorDecisionActionsAsync(report.Candidates[0]);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Phục hồi in ấn", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async Task OfferOperatorDecisionActionsAsync(PrintJobRecoveryCandidate candidate)
    {
        var acknowledge = MessageBox.Show(
            this,
            $"Xác nhận {candidate.JobId} đã được xem xét?\n\nThao tác này chỉ ghi nhận quyết định của người vận hành, không đánh dấu nhãn đã in.",
            "Xác nhận lệnh in",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        if (acknowledge == MessageBoxResult.Yes)
        {
            var result = await _viewModel.AcknowledgePrintJobAsync(candidate.JobId);
            MessageBox.Show(this, result.Summary, "Phục hồi in ấn", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var voidChoice = MessageBox.Show(
            this,
            $"Hủy bỏ {candidate.JobId} trong lịch sử bền vững?\n\nKhông có lệnh máy in nào được gửi, và lịch sử sự kiện vẫn được giữ lại.",
            "Hủy lệnh in không chắc chắn",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);
        if (voidChoice == MessageBoxResult.Yes)
        {
            var result = await _viewModel.VoidPrintJobAsync(candidate.JobId);
            MessageBox.Show(this, result.Summary, "Phục hồi in ấn", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var reprintChoice = MessageBox.Show(
            this,
            $"Tạo yêu cầu in lại liên kết cho {candidate.JobId}?\n\nMột lệnh mới sẽ được ghi nhận để xem xét, nhưng không tự động chuẩn bị hoặc phát lệnh.",
            "Yêu cầu in lại liên kết",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        if (reprintChoice == MessageBoxResult.Yes)
        {
            var result = await _viewModel.RequestPrintJobReprintAsync(candidate.JobId);
            MessageBox.Show(this, result.Summary, "Phục hồi in ấn", MessageBoxButton.OK, MessageBoxImage.Information);

            if (result.RelatedEvent?.Manifest is { } manifest)
            {
                var approveChoice = MessageBox.Show(
                    this,
                    $"Xem xét bản kê khai {manifest.Fingerprint} cho lệnh in liên kết?\n\nPhê duyệt chỉ ghi nhận sự đồng thuận rõ ràng của người vận hành. Lệnh sẽ không in cho đến khi mẫu tem/dữ liệu hiện tại khớp và được phát lệnh riêng.",
                    "Phê duyệt in lại liên kết",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);
                if (approveChoice == MessageBoxResult.Yes)
                {
                    var approval = await _viewModel.ApprovePrintJobReprintAsync(result.RelatedJobId, manifest);
                    MessageBox.Show(this, approval.Summary, "Phục hồi in ấn", MessageBoxButton.OK, MessageBoxImage.Information);
                    if (approval.Succeeded && approval.Event?.Manifest is { } approvedManifest)
                    {
                        var openChoice = MessageBox.Show(
                            this,
                            "Mở Xem trước khi in để chọn chính xác các dòng nguồn cho lệnh in đã duyệt này? Khung bảo vệ kê khai sẽ chặn nếu mẫu tem hoặc dữ liệu có thay đổi.",
                            "Chọn dòng in lại",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question);
                        if (openChoice == MessageBoxResult.Yes)
                        {
                            OpenPrintPreviewDialog(approval.JobId, approvedManifest);
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Chooses a workspace profile from the usable monitor area in WPF device-independent pixels.
    /// HD displays use the full work area; Full HD and larger displays use a centered, comfortable
    /// editing size so the label canvas remains readable without wasting desktop space.
    /// </summary>
    private void ApplyAutomaticDisplayLayout()
    {
        var workArea = SystemParameters.WorkArea;
        var isHdWorkspace = workArea.Width <= 1366 || workArea.Height <= 768;
        if (isHdWorkspace)
        {
            // At 1280x720 the usable height is usually about 680px after the taskbar.
            // Maximizing avoids a clipped bottom status bar. Keep both primary panels
            // visible; each panel scrolls independently and users can still collapse it.
            WindowState = WindowState.Maximized;
            _viewModel.IsToolboxVisible = true;
            _viewModel.IsPropertiesVisible = true;
            return;
        }

        // Full HD and higher: a roomy editing window without forcing a full-screen workspace.
        Width = Math.Min(workArea.Width, workArea.Width >= 1900 ? 1600 : 1440);
        Height = Math.Min(workArea.Height, workArea.Height >= 1000 ? 900 : 800);
        Left = workArea.Left + Math.Max(0, (workArea.Width - Width) / 2);
        Top = workArea.Top + Math.Max(0, (workArea.Height - Height) / 2);
        _viewModel.IsToolboxVisible = true;
        _viewModel.IsPropertiesVisible = true;
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(MainViewModel.SelectedObject) or nameof(MainViewModel.SelectedBindingKindText))
        {
            UpdateContentSourceSelection();
        }
    }

    private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (Keyboard.Modifiers != ModifierKeys.None || IsTextInputTarget(e.OriginalSource))
        {
            return;
        }

        switch (e.Key)
        {
            case Key.L:
                _viewModel.AddLineCommand.Execute(null);
                e.Handled = true;
                break;
            case Key.R:
                _viewModel.AddRectangleCommand.Execute(null);
                e.Handled = true;
                break;
            case Key.C:
                _viewModel.AddEllipseCommand.Execute(null);
                e.Handled = true;
                break;
        }
    }

    private static bool IsTextInputTarget(object originalSource)
    {
        if (originalSource is not DependencyObject source)
        {
            return Keyboard.FocusedElement is TextBoxBase or PasswordBox or ComboBox;
        }

        // Routed keyboard events may originate from a TextBox's internal
        // presenter/run rather than the TextBox itself. Walk the visual tree so
        // arrow keys in position/text editors never reach the canvas nudge path.
        return FindAncestor<TextBoxBase>(source) is not null
            || FindAncestor<PasswordBox>(source) is not null
            || FindAncestor<ComboBox>(source) is not null;
    }

    private void ContentSourceComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_syncingContentSource)
        {
            return;
        }

        if (sender is not ComboBox comboBox || comboBox.SelectedItem is not ComboBoxItem item || item.Tag is not string source)
        {
            return;
        }

        if (source == "Excel")
        {
            EnsureSelectedExcelField();
            BindSelectedObjectToCurrentExcelField();
        }
        else if (source == "Static")
        {
            _viewModel.ClearSelectedBindingCommand.Execute(null);
        }
        else if (source == "Binding")
        {
            // Show formula builder panel; formula is applied when user clicks Apply
        }
    }

    private void ExcelFieldComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_syncingExcelField || _syncingContentSource || ContentSourceComboBox is null)
        {
            return;
        }

        if (ContentSourceComboBox.SelectedItem is not ComboBoxItem { Tag: string source } || source != "Excel")
        {
            return;
        }

        BindSelectedObjectToCurrentExcelField();
    }

    private void EnsureSelectedExcelField()
    {
        if (_viewModel.SelectedLabelDatabaseField is not null)
        {
            return;
        }

        var firstField = _viewModel.LabelDatabaseFields.FirstOrDefault();
        if (firstField is null)
        {
            return;
        }

        _syncingExcelField = true;
        try
        {
            _viewModel.SelectedLabelDatabaseField = firstField;
        }
        finally
        {
            _syncingExcelField = false;
        }
    }

    private void BindSelectedObjectToCurrentExcelField()
    {
        var fieldName = _viewModel.SelectedLabelDatabaseField?.Name ?? _viewModel.SelectedExcelField;
        if (!string.IsNullOrWhiteSpace(fieldName) && _viewModel.BindSelectedAsExcelFieldCommand.CanExecute(fieldName))
        {
            _viewModel.BindSelectedAsExcelFieldCommand.Execute(fieldName);
        }
    }

    private void UpdateContentSourceSelection()
    {
        if (ContentSourceComboBox is null)
        {
            return;
        }

        _syncingContentSource = true;
        try
        {
            var targetTag = _viewModel.IsSelectedBindingFormula ? "Binding"
                          : _viewModel.HasSelectedBinding ? "Excel"
                          : "Static";
            foreach (var comboItem in ContentSourceComboBox.Items.OfType<ComboBoxItem>())
            {
                if (string.Equals(comboItem.Tag?.ToString(), targetTag, StringComparison.Ordinal))
                {
                    ContentSourceComboBox.SelectedItem = comboItem;
                    return;
                }
            }

            ContentSourceComboBox.SelectedIndex = 0;
        }
        finally
        {
            _syncingContentSource = false;
        }
    }

    private void PositionSizeTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || sender is not TextBox textBox)
        {
            return;
        }

        textBox.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
        e.Handled = true;
    }

    private void FontSizeCombo_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || sender is not ComboBox combo)
        {
            return;
        }

        ApplyTypedFontSize(combo);
        e.Handled = true;
    }

    private void FontSizeCombo_LostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is ComboBox combo)
        {
            ApplyTypedFontSize(combo);
        }
    }

    private void ApplyTypedFontSize(ComboBox combo)
    {
        if (_viewModel.SelectedObject is null)
        {
            return;
        }

        if (TextStylePickerCatalog.TryParseSizePt(combo.Text, out var sizePt))
        {
            _viewModel.SelectedObject.Style.FontSizePt = sizePt;
            combo.Text = sizePt.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
            return;
        }

        combo.Text = _viewModel.SelectedObject.Style.FontSizePt.ToString(
            "0.##",
            System.Globalization.CultureInfo.InvariantCulture);
    }

    private void TextPaddingPreset_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string paddingText }
            || !double.TryParse(
                paddingText,
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture,
                out var paddingMm)
            || _viewModel.SelectedObject is not { Type: ObjectType.TextBox } item)
        {
            return;
        }

        item.Style.TextPaddingMm = paddingMm;
        e.Handled = true;
    }

    private void ArrangeAlign_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string alignmentText }
            || !Enum.TryParse<LabelAlignmentMode>(alignmentText, out var alignment))
        {
            return;
        }

        var referenceText = (ArrangeReferenceCombo.SelectedItem as ComboBoxItem)?.Tag?.ToString();
        if (!Enum.TryParse<LabelArrangeReferenceMode>(referenceText, out var reference))
        {
            reference = LabelArrangeReferenceMode.SelectionBounds;
        }

        DesignerCanvas.AlignSelectedObjects(alignment, reference);
        e.Handled = true;
    }

    private void ArrangeDistribute_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string distributionText }
            || !Enum.TryParse<LabelDistributionMode>(distributionText, out var distribution))
        {
            return;
        }

        DesignerCanvas.DistributeSelectedObjects(distribution);
        e.Handled = true;
    }

    private void ArrangeBaseline_Click(object sender, RoutedEventArgs e)
    {
        DesignerCanvas.AlignSelectedTextBaselines();
        e.Handled = true;
    }

    private void ArrangeOptical_Click(object sender, RoutedEventArgs e)
    {
        DesignerCanvas.AlignSelectedTextOptically(
            OpticalAlignmentAxis.Horizontal,
            OpticalAlignmentAnchor.Center);
        e.Handled = true;
    }

    private void ManageDataSources_Click(object sender, RoutedEventArgs e)
    {
        ShowDatabaseManager();
    }

    private void DataWorkspace_Click(object sender, RoutedEventArgs e)
    {
        if (_dataWorkspaceWindow is { IsVisible: true }) { _dataWorkspaceWindow.Activate(); return; }
        var workspace = new DataWorkspaceWindow(_viewModel) { Owner = this };
        workspace.Closed += (_, _) => _dataWorkspaceWindow = null;
        _dataWorkspaceWindow = workspace;
        workspace.Show();
    }

    private void ShowDatabaseManager()
    {
        var window = new DatabaseManagerWindow(_viewModel) { Owner = this };
        window.ShowDialog();
    }

    private void ObjectTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        if (sender is not TextBox textBox || !CanApplyTextBoxInput(textBox, e.Text))
        {
            e.Handled = true;
        }
    }

    private void ObjectTextBox_Pasting(object sender, DataObjectPastingEventArgs e)
    {
        if (sender is not TextBox textBox)
        {
            e.CancelCommand();
            return;
        }

        var pastedText = e.DataObject.GetDataPresent(DataFormats.UnicodeText)
            ? e.DataObject.GetData(DataFormats.UnicodeText) as string
            : e.DataObject.GetData(DataFormats.Text) as string;
        if (!CanApplyTextBoxInput(textBox, pastedText ?? string.Empty))
        {
            e.CancelCommand();
        }
    }

    private bool CanApplyTextBoxInput(TextBox textBox, string input)
    {
        if (_viewModel.SelectedObject is not { Type: ObjectType.TextBox } item || string.IsNullOrEmpty(input))
        {
            return true;
        }

        var candidate = ReplaceSelection(textBox.Text, textBox.SelectionStart, textBox.SelectionLength, input);
        return !TextBoxOverflowDetector.ShouldBlockOverflow(item)
            || !IsTextBoxOverflowing(item, candidate);
    }

    private static string ReplaceSelection(string value, int selectionStart, int selectionLength, string replacement)
    {
        selectionStart = Math.Max(0, Math.Min(selectionStart, value.Length));
        selectionLength = Math.Max(0, Math.Min(selectionLength, value.Length - selectionStart));
        return value.Remove(selectionStart, selectionLength).Insert(selectionStart, replacement);
    }

    private static bool IsTextBoxOverflowing(LabelObject item, string value)
    {
        return TextBoxOverflowDetector.IsOverflowing(
            item,
            value,
            MmConverter.MmToDip(item.WidthMm),
            MmConverter.MmToDip(item.HeightMm));
    }

    private async void New_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new NewTemplateWindow { Owner = this };
        if (dialog.ShowDialog() == true)
        {
            _viewModel.NewTemplate(dialog.Request);
            await _viewModel.RefreshPrinterQueueStatusAsync();
        }
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        var filePath = _viewModel.CurrentFilePath;
        if (string.IsNullOrWhiteSpace(filePath))
        {
            var dialog = new SaveFileDialog
            {
                Title = "Lưu mẫu tem ANLAbel",
                Filter = "Mẫu tem ANLAbel (*.anlabel)|*.anlabel|Tệp JSON (*.json)|*.json|Tất cả tệp (*.*)|*.*",
                DefaultExt = ".anlabel",
                AddExtension = true,
                FileName = $"{_viewModel.Template.Name}.anlabel"
            };

            if (dialog.ShowDialog(this) != true)
            {
                return;
            }

            filePath = dialog.FileName;
        }

        try
        {
            await _viewModel.SaveAsync(filePath);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Lưu thất bại", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void Open_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Mở mẫu tem ANLAbel",
            Filter = "Mẫu tem ANLAbel (*.anlabel)|*.anlabel|Tệp JSON (*.json)|*.json|Tất cả tệp (*.*)|*.*"
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        try
        {
            await _viewModel.OpenAsync(dialog.FileName);
            await _viewModel.RefreshPrinterQueueStatusAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Mở thất bại", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void RevisionHistory_Click(object sender, RoutedEventArgs e)
    {
        ShowRevisionHistory();
    }

    private void ShowRevisionHistory()
    {
        if (string.IsNullOrWhiteSpace(_viewModel.CurrentFilePath))
        {
            MessageBox.Show(
                this,
                "Vui lòng lưu mẫu tem hiện tại trước. Lịch sử sửa đổi chỉ khả dụng cho các tệp đã lưu trên đĩa.",
                "Lịch sử sửa đổi",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        var window = new TemplateRevisionWindow(_viewModel) { Owner = this };
        window.ShowDialog();
    }

    private async void ImportExcel_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ExcelImportWindow(_viewModel) { Owner = this };
        dialog.ShowDialog();
    }

    private async void PrinterSetup_Click(object sender, RoutedEventArgs e)
    {
        ShowPrinterSetupDialog();
        await _viewModel.RefreshPrinterQueueStatusAsync();
    }

    private void PrintPreview_Click(object sender, RoutedEventArgs e)
    {
        OpenPrintPreview();
    }

    private void PrintCurrent_Click(object sender, RoutedEventArgs e)
    {
        if (!TryEnsurePrinterSelected())
        {
            return;
        }

        if (_viewModel.PrintCurrentRowCommand.CanExecute(null))
        {
            _viewModel.PrintCurrentRowCommand.Execute(null);
        }
    }

    private void PrintAllRows_Click(object sender, RoutedEventArgs e)
    {
        if (!TryEnsurePrinterSelected())
        {
            return;
        }

        if (_viewModel.PrintAllRowsCommand.CanExecute(null))
        {
            _viewModel.PrintAllRowsCommand.Execute(null);
        }
    }

    private bool TryEnsurePrinterSelected()
    {
        if (!string.IsNullOrWhiteSpace(_viewModel.Template.PrinterProfile.PrinterName))
        {
            return true;
        }

        ShowPrinterSetupDialog();
        return !string.IsNullOrWhiteSpace(_viewModel.Template.PrinterProfile.PrinterName);
    }

    private void PrintHistory_Click(object sender, RoutedEventArgs e)
    {
        ShowPrintHistory();
    }

    private void ShowPrintHistory()
    {
        if (_printHistoryWindow is { IsVisible: true }) { _printHistoryWindow.Activate(); return; }
        var history = new PrintHistoryWindow(_viewModel, ShowPrintCenter) { Owner = this };
        history.Closed += (_, _) => _printHistoryWindow = null;
        _printHistoryWindow = history;
        history.Show();
    }

    private void Analytics_Click(object sender, RoutedEventArgs e)
    {
        if (_analyticsWindow is { IsVisible: true }) { _analyticsWindow.Activate(); return; }
        var analytics = new AnalyticsWindow(_viewModel, ShowPrintHistory) { Owner = this };
        analytics.Closed += (_, _) => _analyticsWindow = null;
        _analyticsWindow = analytics;
        analytics.Show();
    }

    private void DocumentLibrary_Click(object sender, RoutedEventArgs e)
    {
        if (_documentLibraryWindow is { IsVisible: true }) { _documentLibraryWindow.Activate(); return; }
        var library = new DocumentLibraryWindow(GetLocalLibraryRoot, OpenLibraryDocumentAsync, ShowRevisionHistory) { Owner = this };
        library.Closed += (_, _) => _documentLibraryWindow = null;
        _documentLibraryWindow = library;
        library.Show();
    }

    private void DocumentWorkflow_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_viewModel.CurrentFilePath)) { MessageBox.Show(this, "Vui lòng lưu mẫu tem trước. Nhật ký quy trình gắn liền với tệp đã lưu.", "Quy trình tài liệu", MessageBoxButton.OK, MessageBoxImage.Information); return; }
        if (_documentWorkflowWindow is { IsVisible: true }) { _documentWorkflowWindow.Activate(); return; }
        var workflow = new DocumentWorkflowWindow(_viewModel) { Owner = this };
        workflow.Closed += (_, _) => _documentWorkflowWindow = null; _documentWorkflowWindow = workflow; workflow.Show();
    }

    private void Automation_Click(object sender, RoutedEventArgs e)
    {
        if (_automationWindow is { IsVisible: true }) { _automationWindow.Activate(); return; }
        var automation = new AutomationWindow(ShowPrintHistory, ShowPrintCenter) { Owner = this };
        automation.Closed += (_, _) => _automationWindow = null;
        _automationWindow = automation;
        automation.Show();
    }

    private void LocalMaintenance_Click(object sender, RoutedEventArgs e)
    {
        if (_localMaintenanceWindow is { IsVisible: true }) { _localMaintenanceWindow.Activate(); return; }
        var maintenance = new LocalMaintenanceWindow(
            _viewModel,
            ShowPrinterSetupDialog,
            ShowDatabaseManager,
            ShowPrintHistory,
            () => Analytics_Click(this, new RoutedEventArgs()),
            ShowPrintCenter) { Owner = this };
        maintenance.Closed += (_, _) => _localMaintenanceWindow = null;
        _localMaintenanceWindow = maintenance;
        maintenance.Show();
    }

    private string GetLocalLibraryRoot() => !string.IsNullOrWhiteSpace(_viewModel.CurrentFilePath)
        ? Path.GetDirectoryName(_viewModel.CurrentFilePath) ?? string.Empty
        : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "ANLAbel", "Templates");

    private async Task OpenLibraryDocumentAsync(string filePath)
    {
        try { await _viewModel.OpenAsync(filePath); await _viewModel.RefreshPrinterQueueStatusAsync(); _documentLibraryWindow?.Activate(); }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Mở thất bại", MessageBoxButton.OK, MessageBoxImage.Error); }
    }

    private async void ExportPrintHistory_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Title = "Xuất lịch sử in ra Excel",
            Filter = "Sổ làm việc Excel (*.xlsx)|*.xlsx",
            FileName = $"print-history-{DateTime.Now:yyyy-MM-dd}.xlsx"
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        Mouse.OverrideCursor = Cursors.Wait;
        try
        {
            await _viewModel.PrintLogService.ExportToExcelAsync(dialog.FileName);
            var openReport = MessageBox.Show(
                this,
                $"Đã xuất ra tệp {dialog.FileName}.\n\nBạn có muốn mở ngay bây giờ?",
                "Xuất thành công",
                MessageBoxButton.YesNo,
                MessageBoxImage.Information) == MessageBoxResult.Yes;
            if (openReport)
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = dialog.FileName,
                    UseShellExecute = true
                });
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Xuất thất bại", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            Mouse.OverrideCursor = null;
        }
    }

    private void Help_Click(object sender, RoutedEventArgs e)
    {
        new HelpWindow { Owner = this }.ShowDialog();
    }

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        new SettingsWindow { Owner = this }.ShowDialog();
    }

    private void CheckUpdates_Click(object sender, RoutedEventArgs e)
    {
        new UpdateWindow { Owner = this }.ShowDialog();
    }

    private TemplateLibrary.TemplateLibraryService? _templateLibrary;

    private async void TemplateLibrary_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _templateLibrary ??= new TemplateLibrary.TemplateLibraryService();
            var window = new TemplateLibraryWindow(_templateLibrary) { Owner = this };
            if (window.ShowDialog() == true && window.ChosenTemplate is not null)
            {
                await _viewModel.LoadTemplateFromLibraryAsync(window.ChosenTemplate);
                await _viewModel.RefreshPrinterQueueStatusAsync();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Thư viện mẫu tem", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void UnlinkExcel_Click(object sender, RoutedEventArgs e)
    {
        var confirmed = MessageBox.Show(
            this,
            "Hủy liên kết Excel khỏi mẫu tem này?\n\nCác đối tượng vẫn giữ trường liên kết nhưng sẽ hiển thị nội dung mẫu cho đến khi bạn nhập dữ liệu lại.",
            "Unlink Excel",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question) == MessageBoxResult.Yes;
        if (confirmed)
        {
            _viewModel.UnlinkExcel();
        }
    }

    private void DesignerScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if ((Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.Control)
        {
            return;
        }

        e.Handled = true;
        _viewModel.Zoom = e.Delta > 0
            ? Math.Min(4, _viewModel.Zoom + 0.1)
            : Math.Max(0.25, _viewModel.Zoom - 0.1);
    }

    private void OpenPrintPreview()
    {
        OpenPrintPreviewDialog();
    }

    private void OpenPrintPreviewDialog(string? approvedReprintJobId = null, PrintJobManifest? approvedReprintManifest = null)
    {
        try
        {
            if (!_viewModel.TryBuildPrintPreviewRows(out var preparedRows, out var transformError))
            {
                MessageBox.Show(this, $"Xem trước bị chặn: lỗi chuyển đổi dữ liệu. {transformError}", "Xem trước bị chặn", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dialog = new PrintPreviewWindow(_viewModel.Template, _viewModel.PreviewRow, _viewModel.ExcelDataView, _viewModel.PrintService, _viewModel.PrintLogService, _viewModel.CurrentFilePath, approvedReprintJobId, approvedReprintManifest, preparedRows, _viewModel.GetExcelDataSourceIdentity()) { Owner = this };
            dialog.ShowDialog();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Print preview open error: {ex}");
            MessageBox.Show(
                this,
                $"Không thể mở Xem trước khi in. {ex.Message}",
                "Xem trước không khả dụng",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private async void PrinterQueueWarning_Click(object sender, RoutedEventArgs e)
    {
        var choice = MessageBox.Show(
            this,
            $"{_viewModel.PrinterQueueStatusMessage}\n\nMở Cài đặt máy in ngay bây giờ để chọn hàng đợi đã xác thực?",
            "Hàng đợi máy in không khả dụng",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);
        if (choice != MessageBoxResult.Yes)
        {
            return;
        }

        ShowPrinterSetupDialog();
        await _viewModel.RefreshPrinterQueueStatusAsync();
    }

    private void ShowPrinterSetupDialog()
    {
        try
        {
            var printers = _viewModel.GetInstalledPrinters();
            if (printers.Count == 0)
            {
                MessageBox.Show(this, "Không tìm thấy máy in Windows nào.", "Cài đặt máy in", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var profile = _viewModel.Template.PrinterProfile;
            var dialog = new PrinterSetupWindow(
                printers,
                profile.PrinterName,
                string.IsNullOrWhiteSpace(profile.LabelStockName) ? profile.PaperName : profile.LabelStockName,
                _viewModel.Template.Orientation,
                profile.Dpi > 0 ? profile.Dpi : _viewModel.Template.Dpi,
                profile.PaperSizeSource,
                profile.DriverMediaIdentity,
                profile.SettingsSource,
                initialWidthMm: _viewModel.Template.WidthMm,
                initialHeightMm: _viewModel.Template.HeightMm,
                initialOffsetXMm: profile.OffsetXMm,
                initialOffsetYMm: profile.OffsetYMm,
                initialScaleX: profile.ScaleX,
                initialScaleY: profile.ScaleY,
                workingTemplate: _viewModel.Template,
                printService: _viewModel.PrintService) { Owner = this };
            if (dialog.ShowDialog() == true && dialog.SelectedPrinter is not null && dialog.SelectedPaper is not null)
            {
                _viewModel.ApplyPrinterSelection(
                    dialog.SelectedPrinter,
                    dialog.SelectedPaper,
                    dialog.SelectedDpi,
                    dialog.SelectedOrientation,
                    dialog.SelectedMedia,
                    dialog.SelectedPageSizeSource,
                    dialog.SelectedDriverIdentity,
                    dialog.SelectedSettingsSource,
                    dialog.SelectedOffsetXMm,
                    dialog.SelectedOffsetYMm,
                    dialog.SelectedScaleX,
                    dialog.SelectedScaleY);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Không thể đọc danh sách máy in đã cài đặt.\n\n{ex.Message}", "Cài đặt máy in", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
