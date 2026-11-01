using System.Windows;
using System.IO;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Media;
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
    private PrintHistoryWindow? _printHistoryWindow;
    private bool _syncingDataSource;
    private bool _syncingRulerOffsets;

    public MainWindow()
    {
        InitializeComponent();
        AccessibleNames.Apply(this);
        DataContext = _viewModel;
        _viewModel.PromptValuesRequested = (prompts, previous) => PromptValuesWindow.Ask(this, prompts, previous);
        InitializeDocumentLifecycle();
        InitializeWindowChrome();
        InitializeBackstage();
        DesignerCanvas.EditGestureStarted += (_, _) => _viewModel.BeginTemplateEditGesture();
        DesignerCanvas.EditGestureCompleted += (_, _) => _viewModel.CommitTemplateEditGesture();
        DesignerCanvas.EditGestureCanceled += (_, _) => _viewModel.CancelTemplateEditGesture();
        DesignerCanvas.DataSourceDropped += (_, e) => _viewModel.DropDataSource(e.Binding, e.XMm, e.YMm, e.Target, e.NewObject);
        _viewModel.SelectionProvider = () => DesignerCanvas.SelectedObjects;
        DesignerCanvas.DataColumnNames = () => _viewModel.KnownColumnNames;
        // The selection changes on a click or a key; the Properties title counts it after each.
        PreviewMouseUp += (_, _) => Dispatcher.BeginInvoke(_viewModel.RefreshSelectionScope, System.Windows.Threading.DispatcherPriority.Input);
        PreviewKeyUp += (_, _) => Dispatcher.BeginInvoke(_viewModel.RefreshSelectionScope, System.Windows.Threading.DispatcherPriority.Input);
        _viewModel.PropertyChanged += ViewModel_PropertyChanged;
        WatchLabelFields();
        BuildDesignerContextMenu();
        // The properties button follows the selection through moves, zoom, scroll and view rotation.
        DesignerCanvas.LayoutUpdated += (_, _) =>
        {
            FollowInPlaceEdit();
            PlaceSelectionPropertiesButton();
        };
        BuildObjectBrowserContextMenu();
        BuildDataTreeContextMenu();
        // Back from Excel: say whether the linked file is still open there.
        Activated += async (_, _) => await _viewModel.RefreshExcelOpenHintAsync();
        BindObjectBrowser();
        DesignerCanvas.PreviewMouseLeftButtonDown += DesignerCanvas_PropertiesDoubleClick;
        InputBindings.Add(new System.Windows.Input.KeyBinding(
            new ViewModels.RelayCommand(() =>
            {
                _viewModel.IsPropertiesPinned = !_viewModel.IsPropertiesPinned;
                _viewModel.IsPropertiesVisible = true;
                ApplyPropertiesPlacement();
            }),
            new System.Windows.Input.KeyGesture(System.Windows.Input.Key.F4)));
        InputBindings.Add(new System.Windows.Input.KeyBinding(
            new ViewModels.RelayCommand(OpenPropertiesByKey),
            new System.Windows.Input.KeyGesture(System.Windows.Input.Key.F2)));
        InputBindings.Add(new System.Windows.Input.KeyBinding(
            new ViewModels.RelayCommand(() => _formatPainterSource = _viewModel.SelectedObject),
            new System.Windows.Input.KeyGesture(System.Windows.Input.Key.C, System.Windows.Input.ModifierKeys.Control | System.Windows.Input.ModifierKeys.Shift)));
        InputBindings.Add(new System.Windows.Input.KeyBinding(
            new ViewModels.RelayCommand(() =>
            {
                if (!_viewModel.IsEditingLocked && _formatPainterSource is { } source && _viewModel.SelectedObject is { } target && !ReferenceEquals(source, target))
                {
                    ApplyFormatPainter(source, target);
                }
            }),
            new System.Windows.Input.KeyGesture(System.Windows.Input.Key.V, System.Windows.Input.ModifierKeys.Control | System.Windows.Input.ModifierKeys.Shift)));
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
        // NiceLabel 10's designer keys: select all, cut (the Cut button has always said
        // Ctrl+X), group and ungroup, find an object. A focused text box keeps its own.
        InputBindings.Add(new System.Windows.Input.KeyBinding(
            new ViewModels.RelayCommand(SelectAllByKey),
            new System.Windows.Input.KeyGesture(System.Windows.Input.Key.A, System.Windows.Input.ModifierKeys.Control)));
        InputBindings.Add(new System.Windows.Input.KeyBinding(
            new ViewModels.RelayCommand(() => RunUnlessLocked(() => DesignerCanvas.CutSelectedObjects())),
            new System.Windows.Input.KeyGesture(System.Windows.Input.Key.X, System.Windows.Input.ModifierKeys.Control)));
        InputBindings.Add(new System.Windows.Input.KeyBinding(
            new ViewModels.RelayCommand(() => RunUnlessLocked(() => DesignerCanvas.GroupSelectedObjects())),
            new System.Windows.Input.KeyGesture(System.Windows.Input.Key.G, System.Windows.Input.ModifierKeys.Control)));
        InputBindings.Add(new System.Windows.Input.KeyBinding(
            new ViewModels.RelayCommand(() => RunUnlessLocked(() => DesignerCanvas.UngroupSelectedObjects())),
            new System.Windows.Input.KeyGesture(System.Windows.Input.Key.G, System.Windows.Input.ModifierKeys.Control | System.Windows.Input.ModifierKeys.Shift)));
        InputBindings.Add(new System.Windows.Input.KeyBinding(
            new ViewModels.RelayCommand(FocusObjectSearch),
            new System.Windows.Input.KeyGesture(System.Windows.Input.Key.F, System.Windows.Input.ModifierKeys.Control)));
        InputBindings.Add(new System.Windows.Input.KeyBinding(
            new ViewModels.RelayCommand(() => ToggleTextStyle(style => style.Bold = !style.Bold)),
            new System.Windows.Input.KeyGesture(System.Windows.Input.Key.B, System.Windows.Input.ModifierKeys.Control)));
        InputBindings.Add(new System.Windows.Input.KeyBinding(
            new ViewModels.RelayCommand(() => ToggleTextStyle(style => style.Italic = !style.Italic)),
            new System.Windows.Input.KeyGesture(System.Windows.Input.Key.I, System.Windows.Input.ModifierKeys.Control)));
        InputBindings.Add(new System.Windows.Input.KeyBinding(
            new ViewModels.RelayCommand(() => ToggleTextStyle(style => style.Underline = !style.Underline)),
            new System.Windows.Input.KeyGesture(System.Windows.Input.Key.U, System.Windows.Input.ModifierKeys.Control)));
        // Ctrl with plus or minus zooms and Ctrl+0 shows 100 %, as in NiceLabel 10 and Windows programs.
        foreach (var (key, zoom) in new (System.Windows.Input.Key, Func<double, double>)[]
                 {
                     (System.Windows.Input.Key.OemPlus, current => Math.Min(4, current + 0.1)),
                     (System.Windows.Input.Key.Add, current => Math.Min(4, current + 0.1)),
                     (System.Windows.Input.Key.OemMinus, current => Math.Max(0.25, current - 0.1)),
                     (System.Windows.Input.Key.Subtract, current => Math.Max(0.25, current - 0.1)),
                     (System.Windows.Input.Key.D0, _ => 1),
                     (System.Windows.Input.Key.NumPad0, _ => 1)
                 })
        {
            InputBindings.Add(new System.Windows.Input.KeyBinding(
                new ViewModels.RelayCommand(() =>
                {
                    if (DesignerKeysApply())
                    {
                        _viewModel.Zoom = zoom(_viewModel.Zoom);
                    }
                }),
                new System.Windows.Input.KeyGesture(key, System.Windows.Input.ModifierKeys.Control)));
        }
        // PgUp/PgDn order the selected object and Tab walks the objects only on the label
        // itself, so lists, boxes and the designer's scroll bars keep their keys.
        DesignerCanvas.PreviewKeyDown += (_, e) =>
        {
            if (!IsTextInputTarget(e.OriginalSource)
                && (ArrangeByKey(e.Key, Keyboard.Modifiers) || SelectNextByKey(e.Key, Keyboard.Modifiers)))
            {
                e.Handled = true;
            }
        };
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

        UpdateDataSourceSelection();
        await _viewModel.RefreshPrinterQueueStatusAsync();
        await _viewModel.RefreshPrintRecoveryAsync();
        await CompleteStartupAsync();
    }

    private void ReviewPrintRecovery_Click(object sender, RoutedEventArgs e)
    {
        ShowPrintCenter();
    }

    private void MissingResources_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.SelectFirstMissingResource();
    }

    private void ShowPrintCenter()
    {
        var center = new PrintCenterWindow(_viewModel, OpenApprovedReprintPreview)
        {
            Owner = this
        };
        center.ShowDialog();
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
        if (e.PropertyName == nameof(MainViewModel.SelectedObject))
        {
            UpdateDataSourceSelection();
            // Not inside the list's own selection change: after it settles.
            Dispatcher.BeginInvoke(FollowSelectionInObjectBrowser, System.Windows.Threading.DispatcherPriority.Background);
        }

        // A shape has no Nguồn tab and a rich text box no Mặt nạ page: leave a hidden page.
        if (e.PropertyName == nameof(MainViewModel.SelectedObject))
        {
            if (_viewModel.IsShapeObjectSelected && PropertiesModeTabs.SelectedIndex == 0)
            {
                PropertiesModeTabs.SelectedIndex = 1;
            }

            if (_viewModel.IsRichTextObjectSelected && SourceSubTabs.SelectedIndex == 1)
            {
                SourceSubTabs.SelectedIndex = 0;
            }
        }

        // A 2D code has no Số kiểm tra or Chữ dưới mã page: leave a hidden page for the first.
        if (e.PropertyName is nameof(MainViewModel.IsSelectedLinearCode) && !_viewModel.IsSelectedLinearCode
            && BarcodeSubTabs.SelectedIndex is 1 or 2)
        {
            BarcodeSubTabs.SelectedIndex = 0;
        }
        else if (e.PropertyName == nameof(MainViewModel.SelectedBindingKindText))
        {
            // Not inside the list's own selection change when a pick in it changed the source.
            Dispatcher.BeginInvoke(UpdateDataSourceSelection, System.Windows.Threading.DispatcherPriority.Background);
        }

        if (e.PropertyName == nameof(MainViewModel.Template))
        {
            BindObjectBrowser();
            // Another label brings its own field list: show the selected object's column in it.
            WatchLabelFields();
            Dispatcher.BeginInvoke(UpdateDataSourceSelection, System.Windows.Threading.DispatcherPriority.Background);
        }

        if (e.PropertyName == nameof(MainViewModel.SelectedObject))
        {
            LoadRichTextEditor();
        }

        if (e.PropertyName == nameof(MainViewModel.SelectedObject)
            && FormatPainterToggle.IsChecked == true
            && _viewModel.SelectedObject is { } target
            && _formatPainterSource is { } source
            && !ReferenceEquals(source, target))
        {
            ApplyFormatPainter(source, target);
            FormatPainterToggle.IsChecked = false;
        }

        if (e.PropertyName is nameof(MainViewModel.IsPropertiesVisible) or nameof(MainViewModel.SelectedObject)
            && PropertiesPopup.IsOpen
            && (!_viewModel.IsPropertiesVisible || _viewModel.SelectedObject is null))
        {
            PropertiesPopup.IsOpen = false;
        }
    }

    private void PropertiesPin_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.IsPropertiesVisible = true;
        ApplyPropertiesPlacement();
        if (!_viewModel.IsPropertiesPinned)
        {
            OpenFloatingProperties();
        }
    }

    /// <summary>
    /// Moves the one Properties panel between the right-hand dock (pinned)
    /// and the floating popup beside the selection (unpinned), as NiceLabel 10.
    /// </summary>
    private void ApplyPropertiesPlacement()
    {
        if (_viewModel.IsPropertiesPinned)
        {
            PropertiesPopup.IsOpen = false;
            if (ReferenceEquals(PropertiesPanel.Parent, PropertiesPopupHost))
            {
                PropertiesPopupHost.Child = null;
                PropertiesPanel.Margin = new Thickness(0, 12, 12, 12);
                ShellGrid.Children.Add(PropertiesPanel);
            }

            return;
        }

        if (ReferenceEquals(PropertiesPanel.Parent, ShellGrid))
        {
            ShellGrid.Children.Remove(PropertiesPanel);
            PropertiesPanel.Margin = new Thickness(0);
            PropertiesPopupHost.Child = PropertiesPanel;
        }
    }

    private void DesignerCanvas_PropertiesDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount != 2)
        {
            return;
        }

        // Selection updates on this same gesture; open once it has.
        Dispatcher.BeginInvoke(() =>
        {
            if (_viewModel.SelectedObject is null)
            {
                return;
            }

            // NiceLabel 10: a double-click on a text edits it on the label.
            if (BeginInPlaceEdit())
            {
                return;
            }

            _viewModel.IsPropertiesVisible = true;
            if (!_viewModel.IsPropertiesPinned)
            {
                OpenFloatingProperties();
            }
        }, System.Windows.Threading.DispatcherPriority.Input);
    }

    /// <summary>
    /// NiceLabel 10's CommandBox: a properties button just outside the selected object's top-right
    /// corner, clear of the corner handle's 10 DIP target, so a compact text stays visible. Hidden with
    /// no selection, on a protected label and while a drawing tool is armed.
    /// </summary>
    private void PlaceSelectionPropertiesButton()
    {
        var button = SelectionPropertiesButton;
        if (_viewModel.SelectedObject is not { } item || !_viewModel.IsEditingAllowed || DesignerCanvas.DrawingTool is not null || _inPlaceEditor is not null
            || !DesignerCanvas.IsVisible && PresentationSource.FromVisual(DesignerCanvas) is not null)
        {
            button.Visibility = Visibility.Collapsed;
            return;
        }

        var zoom = _viewModel.Zoom;
        var frame = new Rect(
            MmConverter.MmToDip(item.XMm) * zoom,
            MmConverter.MmToDip(item.YMm) * zoom,
            Math.Max(0, MmConverter.MmToDip(item.WidthMm) * zoom),
            Math.Max(0, MmConverter.MmToDip(item.HeightMm) * zoom));
        if (item.Rotation % 360 != 0)
        {
            // An object turns about its centre: the button sits at its turned bounds.
            frame = new RotateTransform(item.Rotation, frame.X + frame.Width / 2, frame.Y + frame.Height / 2).TransformBounds(frame);
        }

        Point corner;
        try
        {
            corner = DesignerCanvas.TranslatePoint(new Point(frame.Right, frame.Top), SelectionCommandLayer);
        }
        catch (InvalidOperationException)
        {
            button.Visibility = Visibility.Collapsed;
            return;
        }

        var left = Math.Round(corner.X + SelectionPropertiesGap);
        var top = Math.Round(corner.Y - 1);
        if (Canvas.GetLeft(button) != left)
        {
            Canvas.SetLeft(button, left);
        }

        if (Canvas.GetTop(button) != top)
        {
            Canvas.SetTop(button, top);
        }

        button.Visibility = Visibility.Visible;
    }

    private TextBox? _inPlaceEditor;
    private string? _inPlaceTargetId;
    private string _inPlaceStartText = string.Empty;

    /// <summary>
    /// A Text or Text Box with fixed content, selected alone on an editable label: what in-place
    /// editing edits. Other objects, a text read from a source or several selected objects keep F2
    /// and the double-click opening the properties.
    /// </summary>
    private bool CanEditInPlace(LabelObject? item)
        => item is { Type: ObjectType.Text or ObjectType.TextBox } && string.IsNullOrWhiteSpace(item.BindingExpression)
            && !_viewModel.IsEditingLocked && DesignerCanvas.SelectedObjects.Count <= 1;

    /// <summary>
    /// NiceLabel 10 in-place text editing (owner decision 2026-11-01; DesignerLib
    /// TextSelectionEditItemView): a box over the selected text's frame, in its font at the zoom, its
    /// content selected to type over. The label follows the typing, so saving or printing during the
    /// edit uses the new text. Enter starts a new line, as in Properties › Nguồn › Chữ; Esc or leaving
    /// the box ends the edit and keeps the text, one undo step. The box takes the content box's input
    /// rules: a Text Box set to refuse overflow refuses it here too.
    /// </summary>
    private bool BeginInPlaceEdit()
    {
        if (_inPlaceEditor is not null || !CanEditInPlace(_viewModel.SelectedObject))
        {
            return false;
        }

        var item = _viewModel.SelectedObject!;
        var editor = new TextBox
        {
            Text = item.Text ?? string.Empty,
            FontFamily = new FontFamily(string.IsNullOrWhiteSpace(item.Style.FontFamily) ? "Arial" : item.Style.FontFamily),
            FontWeight = item.Style.Bold ? FontWeights.Bold : FontWeights.Normal,
            FontStyle = item.Style.Italic ? FontStyles.Italic : FontStyles.Normal,
            TextAlignment = item.Style.Alignment switch
            {
                TextAlignmentMode.Center => TextAlignment.Center,
                TextAlignmentMode.Right => TextAlignment.Right,
                TextAlignmentMode.Justify => TextAlignment.Justify,
                _ => TextAlignment.Left
            },
            AcceptsReturn = true,
            // A Text Box wraps inside its width; a Text runs free.
            TextWrapping = item.Type == ObjectType.TextBox ? TextWrapping.Wrap : TextWrapping.NoWrap,
            Padding = new Thickness(1),
            Margin = new Thickness(0),
            BorderThickness = new Thickness(1.5),
            BorderBrush = (Brush)FindResource("AppBlueBrush"),
            Background = Brushes.White,
            ToolTip = "Enter: xuống dòng · Esc hoặc bấm ra ngoài: xong (Ctrl+Z để hoàn tác)"
        };
        System.Windows.Automation.AutomationProperties.SetName(editor, "In-place text");
        System.Windows.Automation.AutomationProperties.SetAutomationId(editor, "Shell.Canvas.InPlaceText");
        _inPlaceEditor = editor;
        _inPlaceTargetId = item.Id;
        if (!PlaceInPlaceEditor(item))
        {
            _inPlaceEditor = null;
            _inPlaceTargetId = null;
            return false;
        }

        // The content box's input rules: a Text Box set to refuse overflow refuses it on the label too.
        editor.PreviewTextInput += ObjectTextBox_PreviewTextInput;
        DataObject.AddPastingHandler(editor, ObjectTextBox_Pasting);
        editor.PreviewKeyDown += InPlaceEditor_PreviewKeyDown;
        editor.LostKeyboardFocus += InPlaceEditor_LostKeyboardFocus;
        editor.TextChanged += (_, _) =>
        {
            if (ReferenceEquals(_inPlaceEditor, editor) && InPlaceTarget() is { } target)
            {
                _viewModel.SetObjectText(target, editor.Text);
            }
        };
        _inPlaceStartText = item.Text ?? string.Empty;
        _viewModel.BeginTemplateEditGesture($"Sửa chữ “{item.Name}”");
        SelectionCommandLayer.Children.Add(editor);
        SelectionPropertiesButton.Visibility = Visibility.Collapsed;
        editor.Focus();
        editor.SelectAll();
        _viewModel.StatusText = "Sửa chữ trên tem: Enter xuống dòng; Esc hoặc bấm ra ngoài để xong, Ctrl+Z để hoàn tác.";
        return true;
    }

    private LabelObject? InPlaceTarget()
        => _inPlaceTargetId is { } id ? _viewModel.Template.Objects.FirstOrDefault(item => item.Id == id) : null;

    /// <summary>
    /// Puts the box on its text's frame in the font size at the zoom; false when the frame cannot be
    /// placed. Run again on every layout, so the box stays on the text as the label scrolls or zooms.
    /// </summary>
    private bool PlaceInPlaceEditor(LabelObject item)
    {
        if (_inPlaceEditor is not { } editor)
        {
            return false;
        }

        var zoom = _viewModel.Zoom;
        var frame = new Rect(
            MmConverter.MmToDip(item.XMm) * zoom,
            MmConverter.MmToDip(item.YMm) * zoom,
            Math.Max(0, MmConverter.MmToDip(item.WidthMm) * zoom),
            Math.Max(0, MmConverter.MmToDip(item.HeightMm) * zoom));
        Point origin;
        try
        {
            origin = DesignerCanvas.TranslatePoint(frame.TopLeft, SelectionCommandLayer);
        }
        catch (InvalidOperationException)
        {
            return false;
        }

        editor.FontSize = Math.Max(8, item.Style.FontSizePt * 96 / 72 * zoom);
        editor.MinWidth = Math.Max(frame.Width, 60);
        editor.MinHeight = Math.Max(frame.Height, 22);
        editor.Width = item.Type == ObjectType.TextBox ? Math.Max(frame.Width, 60) : double.NaN;
        Canvas.SetLeft(editor, Math.Round(origin.X));
        Canvas.SetTop(editor, Math.Round(origin.Y));
        if (editor.RenderTransform is not RotateTransform turn)
        {
            editor.RenderTransform = turn = new RotateTransform();
        }

        // Turned as the text is, about its frame's centre.
        turn.Angle = item.Rotation;
        turn.CenterX = frame.Width / 2;
        turn.CenterY = frame.Height / 2;
        return true;
    }

    /// <summary>The box follows its text; a text gone from the label (deleted or undone) ends the edit.</summary>
    private void FollowInPlaceEdit()
    {
        if (_inPlaceEditor is null)
        {
            return;
        }

        if (InPlaceTarget() is { } item)
        {
            PlaceInPlaceEditor(item);
        }
        else
        {
            Dispatcher.BeginInvoke(() => EndInPlaceEdit(), System.Windows.Threading.DispatcherPriority.Input);
        }
    }

    private void InPlaceEditor_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        // NiceLabel 10: Esc ends the edit and keeps the text (Ctrl+Z takes it back); Enter is a new line.
        if (e.Key == Key.Escape)
        {
            EndInPlaceEdit();
            e.Handled = true;
        }
    }

    private void InPlaceEditor_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        // The box's own Cut / Copy / Paste menu keeps the edit open.
        if (sender is TextBox editor && e.NewFocus is DependencyObject next && IsInMenuOf(next, editor))
        {
            return;
        }

        EndInPlaceEdit();
    }

    private static bool IsInMenuOf(DependencyObject element, UIElement owner)
    {
        for (DependencyObject? node = element; node is not null; node = LogicalTreeHelper.GetParent(node) ?? (node is Visual ? VisualTreeHelper.GetParent(node) : null))
        {
            if (node is ContextMenu menu)
            {
                return ReferenceEquals(menu.PlacementTarget, owner);
            }
        }

        return false;
    }

    /// <summary>Ends the in-place edit: its text is already on the label; the edit becomes one undo step.</summary>
    private void EndInPlaceEdit()
    {
        if (_inPlaceEditor is not { } editor)
        {
            return;
        }

        var target = InPlaceTarget();
        var hadFocus = editor.IsKeyboardFocusWithin;
        _inPlaceEditor = null;
        _inPlaceTargetId = null;
        SelectionCommandLayer.Children.Remove(editor);
        _viewModel.CommitTemplateEditGesture();
        if (target is not null)
        {
            _viewModel.StatusText = string.Equals(target.Text ?? string.Empty, _inPlaceStartText, StringComparison.Ordinal)
                ? "Sẵn sàng"
                : $"Đã sửa chữ của {target.Name}.";
        }

        if (hadFocus)
        {
            // Back to the label, as NiceLabel returns to the element focused before.
            DesignerCanvas.Focus();
        }

        PlaceSelectionPropertiesButton();
    }

    /// <summary>From the corner to the button: past the corner handle's 5 DIP half target.</summary>
    private const double SelectionPropertiesGap = 8;

    private void SelectionPropertiesButton_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel.SelectedObject is null)
        {
            return;
        }

        _viewModel.IsPropertiesVisible = true;
        if (!_viewModel.IsPropertiesPinned)
        {
            OpenFloatingProperties();
        }

        e.Handled = true;
    }

    private void OpenFloatingProperties()
    {
        ApplyPropertiesPlacement();
        if (_viewModel.SelectedObject is not { } item)
        {
            return;
        }

        var zoom = _viewModel.Zoom;
        PropertiesPopup.HorizontalOffset = MmConverter.MmToDip(item.XMm + item.WidthMm) * zoom + 16;
        PropertiesPopup.VerticalOffset = Math.Max(0, MmConverter.MmToDip(item.YMm) * zoom - 8);
        PropertiesPopup.IsOpen = false;
        PropertiesPopup.IsOpen = true;
    }

    /// <summary>
    /// NiceLabel 10 Object Browser: every object of the label, filtered by
    /// the search box, on its own view so the canvas stays unfiltered.
    /// </summary>
    private void BindObjectBrowser()
    {
        // The selected object always stays in the list, so its highlighted row is the selection.
        ObjectBrowserList.ItemsSource = new System.Windows.Data.ListCollectionView(_viewModel.Template.Objects)
        {
            Filter = item => item is LabelObject labelObject
                && (ReferenceEquals(labelObject, _viewModel.SelectedObject) || MatchesObjectSearch(labelObject, ObjectBrowserSearch.Text))
        };
    }

    /// <summary>
    /// A selection the search hid (an object placed from the toolbox while the list is filtered):
    /// the list shows it and moves its highlight to it, else the old row stayed highlighted and a
    /// click on it did nothing.
    /// </summary>
    private void FollowSelectionInObjectBrowser()
    {
        if (_viewModel.SelectedObject is not { } selected
            || ObjectBrowserList.ItemsSource is not System.ComponentModel.ICollectionView view
            || view.Contains(selected))
        {
            return;
        }

        view.Refresh();
        System.Windows.Data.BindingOperations.GetBindingExpression(ObjectBrowserList, System.Windows.Controls.Primitives.Selector.SelectedItemProperty)?.UpdateTarget();
    }

    /// <summary>
    /// NiceLabel 10 Data explorer: the label's prompts, the data's columns and the functions,
    /// filtered by the search box. Each list is a CollectionViewSource of its own, so the other
    /// lists of the same items stay whole.
    /// </summary>
    private void DataTreeSource_Filter(object sender, System.Windows.Data.FilterEventArgs e)
    {
        var search = DataTreeSearch?.Text;
        var name = e.Item switch
        {
            PromptVariableDefinition prompt => prompt.Name,
            ANLAbel.Core.Data.DataTransformDefinition transform => transform.Name,
            string column => column,
            _ => null
        };
        e.Accepted = string.IsNullOrWhiteSpace(search) || (name ?? string.Empty).Contains(search.Trim(), StringComparison.CurrentCultureIgnoreCase);
    }

    private void DataTreeSearch_TextChanged(object sender, TextChangedEventArgs e)
    {
        foreach (var key in new[] { "DataTreePromptsSource", "DataTreeFieldsSource", "DataTreeFunctionsSource" })
        {
            ((System.Windows.Data.CollectionViewSource)Resources[key]).View?.Refresh();
        }
    }

    private void DataTreeAdd_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { ContextMenu: { } menu } button)
        {
            menu.PlacementTarget = button;
            menu.Placement = PlacementMode.Bottom;
            menu.IsOpen = true;
        }
    }

    /// <summary>
    /// The Quản lý dữ liệu page a double-click in the Data panel opens (NiceLabel 10
    /// DefineDataSourcesCommand): the prompt or function itself, the data source page for a
    /// column, the counter for the serial number, the print date page for the date.
    /// </summary>
    public static string? DataManagerPageFor(object? item) => item switch
    {
        PromptVariableDefinition prompt => DataManagerWindow.PromptPage + prompt.Name,
        ANLAbel.Core.Data.DataTransformDefinition function => DataManagerWindow.FunctionPage + function.Name,
        string => "Database",
        DataTreeField field when field.Binding.Contains(SerialCounterContract.Token, StringComparison.OrdinalIgnoreCase) => "Counter",
        DataTreeField => "Date",
        _ => null
    };

    /// <summary>The Data panel entry an element belongs to; none for the group rows.</summary>
    public static object? DataTreeItemFor(DependencyObject? element)
    {
        while (element is not null and not TreeViewItem)
        {
            element = element is Visual ? VisualTreeHelper.GetParent(element) : LogicalTreeHelper.GetParent(element);
        }

        // The group rows (Biến nhập khi in, Trường dữ liệu…) keep folding on a double-click.
        return element is TreeViewItem container && ItemsControl.ItemsControlFromItemContainer(container) is { } owner and not TreeView
            ? owner.ItemContainerGenerator.ItemFromContainer(container)
            : null;
    }

    private object? _dataTreeMenuEntry;
    private MenuItem? _dataTreeAddItem;
    private MenuItem? _dataTreeConnectItem;

    /// <summary>
    /// NiceLabel 10's Data explorer menu: on an entry, open it in Quản lý dữ liệu, or do what a
    /// drag does (a new Text connected to it, or the selected object connected), one undo step
    /// each. Group rows and empty space have no menu.
    /// </summary>
    private void BuildDataTreeContextMenu()
    {
        if (LogicalTreeHelper.GetParent(DataTreePrompts) is not TreeView tree)
        {
            return;
        }

        var edit = new MenuItem { Header = "Sửa trong Quản lý dữ liệu…" };
        edit.Click += (_, e) =>
        {
            if (DataManagerPageFor(_dataTreeMenuEntry) is { } page)
            {
                ShowDataManager(page);
            }

            e.Handled = true;
        };
        _dataTreeAddItem = new MenuItem { Header = "Thêm chữ gắn với nguồn này" };
        _dataTreeAddItem.Click += (_, e) =>
        {
            if (DataTreeBinding(_dataTreeMenuEntry) is { } binding)
            {
                _viewModel.DropDataSource(binding, 5, 5, null, newObject: true);
            }

            e.Handled = true;
        };
        _dataTreeConnectItem = new MenuItem();
        _dataTreeConnectItem.Click += (_, e) =>
        {
            if (DataTreeBinding(_dataTreeMenuEntry) is { } binding)
            {
                _viewModel.DropDataSource(binding, 0, 0, _viewModel.SelectedObject, newObject: false);
            }

            e.Handled = true;
        };
        var menu = new ContextMenu();
        foreach (var item in new Control[] { edit, new Separator(), _dataTreeAddItem, _dataTreeConnectItem })
        {
            menu.Items.Add(item);
        }

        tree.ContextMenu = menu;
        tree.ContextMenuOpening += (_, e) =>
        {
            if (!PrepareDataTreeMenu(DataTreeItemFor(e.OriginalSource as DependencyObject)))
            {
                e.Handled = true;
            }
        };
    }

    /// <summary>Readies the Data panel menu for an entry; false when there is none (a group row).</summary>
    private bool PrepareDataTreeMenu(object? entry)
    {
        if (DataTreeBinding(entry) is null || _dataTreeAddItem is null || _dataTreeConnectItem is null)
        {
            return false;
        }

        _dataTreeMenuEntry = entry;
        var editable = !_viewModel.IsEditingLocked;
        var target = _viewModel.SelectedObject;
        var takesData = target?.Type is ObjectType.Text or ObjectType.TextBox or ObjectType.BarcodeCode128 or ObjectType.QRCode or ObjectType.DataMatrix;
        _dataTreeAddItem.IsEnabled = editable;
        _dataTreeConnectItem.Header = takesData ? $"Gắn vào “{target!.Name}”" : "Gắn vào đối tượng đang chọn";
        _dataTreeConnectItem.IsEnabled = editable && takesData;
        return true;
    }

    /// <summary>Enter on a Data panel entry opens it, as a double-click does: the keyboard has no double-click.</summary>
    private void DataTree_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && DataManagerPageFor(DataTreeItemFor(e.OriginalSource as DependencyObject)) is { } page)
        {
            e.Handled = true;
            ShowDataManager(page);
        }
    }

    private void DataTree_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataManagerPageFor(DataTreeItemFor(e.OriginalSource as DependencyObject)) is { } page)
        {
            e.Handled = true;
            ShowDataManager(page);
        }
    }

    // NiceLabel 10 "Add new variable": Quản lý dữ liệu opens on the new item.
    private void AddPromptFromData_Click(object sender, RoutedEventArgs e) => ShowDataManager("NewPrompt");

    private void AddFunctionFromData_Click(object sender, RoutedEventArgs e) => ShowDataManager("NewFunction");

    /// <summary>
    /// NiceLabel 10 toolbox drop-down: the sources of the Data panel, by group; a pick places the
    /// tile's object already connected to it. Names show as written (an underscore is no access key).
    /// </summary>
    private void ToolboxSourceMenu_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string kindName } button || !Enum.TryParse<ObjectType>(kindName, out var kind))
        {
            return;
        }

        OpenSourceMenu(button, binding => _viewModel.AddConnectedObject(kind, binding));
        e.Handled = true;
    }

    /// <summary>
    /// NiceLabel 10's "Insert data source": the Data panel's sources by group under the button; a
    /// pick hands its binding over. Names show as written (an underscore is no access key) and are
    /// what a screen reader says.
    /// </summary>
    private void OpenSourceMenu(Button button, Action<string> pick)
    {
        var menu = new ContextMenu { PlacementTarget = button, Placement = PlacementMode.Bottom };
        foreach (var group in _viewModel.DataSourceChoicesFor(null).Where(choice => choice.Kind == DataSourceChoiceKind.Source).GroupBy(choice => choice.Group))
        {
            var header = new MenuItem { Header = new TextBlock { Text = group.Key } };
            System.Windows.Automation.AutomationProperties.SetName(header, group.Key);
            foreach (var choice in group)
            {
                var item = new MenuItem { Header = new TextBlock { Text = choice.Label }, ToolTip = choice.Binding };
                System.Windows.Automation.AutomationProperties.SetName(item, choice.Label);
                item.Click += (_, _) => pick(choice.Binding);
                header.Items.Add(item);
            }

            menu.Items.Add(header);
        }

        button.ContextMenu = menu;
        menu.IsOpen = true;
    }

    private static bool MatchesObjectSearch(LabelObject item, string? search)
        => string.IsNullOrWhiteSpace(search)
            || item.Name.Contains(search.Trim(), StringComparison.CurrentCultureIgnoreCase)
            || (item.Text ?? string.Empty).Contains(search.Trim(), StringComparison.CurrentCultureIgnoreCase);

    private void ObjectBrowserSearch_TextChanged(object sender, TextChangedEventArgs e)
    {
        (ObjectBrowserList.ItemsSource as System.ComponentModel.ICollectionView)?.Refresh();
    }

    private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (Keyboard.Modifiers != ModifierKeys.None || IsTextInputTarget(e.OriginalSource) || _viewModel.IsEditingLocked)
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
            // NiceLabel 10: Enter on the design surface edits the selected text in place.
            case Key.Enter when DesignerCanvas.IsKeyboardFocusWithin && DesignerCanvas.DrawingTool is null:
                e.Handled = BeginInPlaceEdit();
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

    /// <summary>
    /// The Source page's "Nguồn dữ liệu" list (NiceLabel 10 Connected data source) shows what
    /// the selected object prints. It is rebuilt from the Data panel's groups when the selection,
    /// the object's source, the label or its columns change, and when it opens. A rebuild only
    /// shows: the old Trường Excel list took a data load's move for a pick and bound the object.
    /// </summary>
    private void UpdateDataSourceSelection()
    {
        if (DataSourceComboBox is null)
        {
            return;
        }

        var choices = _viewModel.DataSourceChoicesFor(_viewModel.SelectedObject);
        var current = MainViewModel.CurrentDataSourceChoice(_viewModel.SelectedObject, choices);
        var view = new System.Windows.Data.ListCollectionView(choices.ToList());
        view.GroupDescriptions.Add(new System.Windows.Data.PropertyGroupDescription(nameof(DataSourceChoice.Group)));
        _syncingDataSource = true;
        try
        {
            DataSourceComboBox.ItemsSource = view;
            DataSourceComboBox.SelectedItem = current;
        }
        finally
        {
            _syncingDataSource = false;
        }

        ShowComposition(force: false);
    }

    private LabelObject? _compositionTarget;
    private string _compositionShown = string.Empty;

    /// <summary>
    /// The combined content box shows what the object prints: its composition, or the fixed text
    /// or single source a composition starts from. Text being typed in it is left alone, and it
    /// stays the text of the object it was showing.
    /// </summary>
    private void ShowComposition(bool force)
    {
        if (CompositionTextBox is null || (!force && CompositionTextBox.IsKeyboardFocusWithin))
        {
            return;
        }

        var item = _viewModel.SelectedObject;
        _compositionTarget = item;
        _compositionShown = item is null ? string.Empty
            : !string.IsNullOrWhiteSpace(item.BindingExpression) ? item.BindingExpression : item.Text ?? string.Empty;
        CompositionTextBox.Text = _compositionShown;
    }

    /// <summary>Applies what was typed, and only to the object the box was showing.</summary>
    private void ApplyComposition(string text)
    {
        if (_compositionTarget is { } target && !string.Equals(text, _compositionShown, StringComparison.Ordinal))
        {
            _viewModel.ComposeObject(target, text);
        }

        ShowComposition(force: true);
    }

    private void CompositionTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            ApplyComposition(CompositionTextBox.Text);
            CompositionTextBox.CaretIndex = CompositionTextBox.Text.Length;
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            ShowComposition(force: true);
            e.Handled = true;
        }
    }

    private void CompositionTextBox_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e) => ApplyComposition(CompositionTextBox.Text);

    /// <summary>
    /// NiceLabel 10's "Insert data source": the Data panel's sources by group; a pick goes in at
    /// the caret (or over the selected text) and applies. Names show as written.
    /// </summary>
    private void CompositionInsert_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button)
        {
            OpenSourceMenu(button, InsertIntoComposition);
            e.Handled = true;
        }
    }

    /// <summary>
    /// NiceLabel 10's condition editor inserts data sources: Chèn nguồn ▾ under Điều kiện hiển thị
    /// puts one at the caret (or over the selected text) and applies the condition.
    /// </summary>
    private void ConditionInsert_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button)
        {
            OpenSourceMenu(button, InsertIntoCondition);
            e.Handled = true;
        }
    }

    private void InsertIntoCondition(string token)
    {
        var box = VisibilityConditionBox;
        var start = Math.Clamp(box.SelectionStart, 0, box.Text.Length);
        var length = Math.Clamp(box.SelectionLength, 0, box.Text.Length - start);
        box.Text = box.Text.Remove(start, length).Insert(start, token);
        box.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
        box.CaretIndex = Math.Min(start + token.Length, box.Text.Length);
    }

    private void InsertIntoComposition(string token)
    {
        var box = CompositionTextBox;
        var start = Math.Clamp(box.SelectionStart, 0, box.Text.Length);
        var length = Math.Clamp(box.SelectionLength, 0, box.Text.Length - start);
        ApplyComposition(box.Text.Remove(start, length).Insert(start, token));
        box.CaretIndex = Math.Min(start + token.Length, box.Text.Length);
    }

    private void DataSourceComboBox_DropDownOpened(object? sender, EventArgs e) => UpdateDataSourceSelection();

    private void DataSourceComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Only a pick in the list connects the object, as a drop from the Data panel does. A
        // composition is built below and applied with its own button; the source the object
        // already reads changes nothing.
        if (_syncingDataSource || DataSourceComboBox.SelectedItem is not DataSourceChoice choice)
        {
            return;
        }

        _viewModel.ConnectSelectedObject(choice);
    }

    private System.Collections.Specialized.INotifyCollectionChanged? _watchedLabelFields;

    /// <summary>
    /// The field list belongs to the open label (its DatabaseConfig), so it is watched again
    /// when another label opens: a data load or a field added or removed changes the columns
    /// the source list offers.
    /// </summary>
    private void WatchLabelFields()
    {
        if (_watchedLabelFields is not null)
        {
            _watchedLabelFields.CollectionChanged -= LabelFields_CollectionChanged;
        }

        _watchedLabelFields = _viewModel.LabelDatabaseFields;
        _watchedLabelFields.CollectionChanged += LabelFields_CollectionChanged;
    }

    private void LabelFields_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        => Dispatcher.BeginInvoke(UpdateDataSourceSelection, System.Windows.Threading.DispatcherPriority.Background);

    private void PositionSizeTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || sender is not TextBox textBox)
        {
            return;
        }

        textBox.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
        e.Handled = true;
    }

    private bool _showingListValue;

    // The font boxes: family and size on the ribbon's Home and Text tabs and in Properties, and a
    // barcode's human readable font. Each box's Tag is bound two-way to its value with an explicit
    // update. A box shows the value whatever it is, as NiceLabel 10's editable boxes do: its list
    // entry when it has one, else the value as text with no entry picked; blank for none (the
    // selected texts differ). The boxes matched list values through SelectedItem, and WPF keeps the
    // old entry for a value outside the list, so a 48 pt or Arial Narrow text after another one
    // showed the other's size or font. A pick, or a typed value with Enter or on leaving the box, is
    // written through the Tag: a size in range, or a font from the licensed list.

    private object? ListEntry(object? value) => value switch
    {
        double size when _viewModel.FontSizes.Contains(size) => size,
        string family => _viewModel.FontFamilies.FirstOrDefault(name => string.Equals(name, family.Trim(), StringComparison.OrdinalIgnoreCase)),
        _ => null
    };

    private static string ShownText(object? value) => value switch
    {
        double size => size.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture),
        string family => family,
        _ => string.Empty
    };

    /// <summary>What a typed text sets: a size in range in a size box, a listed font in a font box.</summary>
    private object? TypedValue(ComboBox box) => box.ItemsSource is IReadOnlyList<double>
        ? TextStylePickerCatalog.TryParseSizePt(box.Text, out var size) ? size : null
        : ListEntry(box.Text);

    /// <summary>Shows the box's value; what is being typed is left alone.</summary>
    private void ShowListValue(ComboBox box, bool evenWhileTyping = false)
    {
        _showingListValue = true;
        try
        {
            box.SelectedItem = ListEntry(box.Tag);
            if (evenWhileTyping || !box.IsKeyboardFocusWithin)
            {
                box.Text = ShownText(box.Tag);
            }
        }
        finally
        {
            _showingListValue = false;
        }
    }

    /// <summary>Writes a value through the box's Tag; the value it shows already changes nothing.</summary>
    private static void SetListValue(ComboBox box, object value)
    {
        if (Equals(box.Tag, value))
        {
            return;
        }

        box.Tag = value;
        box.GetBindingExpression(FrameworkElement.TagProperty)?.UpdateSource();
    }

    private void ListValueBox_TargetUpdated(object sender, System.Windows.Data.DataTransferEventArgs e)
    {
        if (sender is ComboBox box && e.Property == FrameworkElement.TagProperty)
        {
            ShowListValue(box);
        }
    }

    private void ListValueBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // A pick: in the open list, or by UI Automation; not the entry the typing matches.
        if (!_showingListValue && sender is ComboBox { SelectedItem: { } value } box && (box.IsDropDownOpen || !box.IsKeyboardFocusWithin))
        {
            SetListValue(box, value);
        }
    }

    private void ListValueBox_DropDownClosed(object sender, EventArgs e)
    {
        if (sender is ComboBox { SelectedItem: { } value } box)
        {
            SetListValue(box, value);
        }
    }

    private void ListValueBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && sender is ComboBox box)
        {
            ApplyTypedListValue(box);
            e.Handled = true;
        }
    }

    private void ListValueBox_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (sender is ComboBox box && !box.IsKeyboardFocusWithin)
        {
            ApplyTypedListValue(box);
        }
    }

    private void ApplyTypedListValue(ComboBox box)
    {
        if (TypedValue(box) is { } value)
        {
            SetListValue(box, value);
        }

        ShowListValue(box, evenWhileTyping: true);
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

    // ==================== Right-click menus (NiceLabel 10) ====================

    private LabelObject? _contextMenuObject;
    private readonly List<Control> _objectMenuItems = new();
    private readonly List<Control> _labelMenuItems = new();
    private MenuItem? _contextLockItem;
    private MenuItem? _contextGroupItem;
    private MenuItem? _contextUngroupItem;
    private MenuItem? _contextPasteItem;
    private MenuItem? _contextSelectAllItem;
    private MenuItem? _contextEditTextItem;

    /// <summary>
    /// NiceLabel 10's right-click menus on the design surface: on an object its
    /// properties, clipboard, lock, group, arrange and align; on the label its
    /// properties, paste, select all and zoom. The canvas's own snapping, grid and guide
    /// items follow under "Căn và lưới". Each item runs the ribbon's handler.
    /// </summary>
    private void BuildDesignerContextMenu()
    {
        if (DesignerCanvas.ContextMenu is not { } menu)
        {
            return;
        }

        var canvasItems = menu.Items.Cast<object>().ToList();
        menu.Items.Clear();
        MenuItem Item(string header, string gesture, RoutedEventHandler click, string? tag = null)
        {
            var item = new MenuItem { Header = header, InputGestureText = gesture, Tag = tag };
            item.Click += click;
            return item;
        }

        RoutedEventHandler Run(ICommand command) => (_, e) =>
        {
            command.Execute(null);
            DesignerCanvas.Focus();
            e.Handled = true;
        };

        var properties = Item("Thuộc tính", string.Empty, (_, e) =>
        {
            OpenFloatingProperties();
            e.Handled = true;
        });
        // NiceLabel 10's Edit text: the selected text on the label.
        _contextEditTextItem = Item("Sửa chữ", "F2", (_, e) =>
        {
            BeginInPlaceEdit();
            e.Handled = true;
        });
        var cut = Item("Cắt", "Ctrl+X", Cut_Click);
        var copy = Item("Sao chép", "Ctrl+C", Copy_Click);
        // The whole selection, as the Delete key (the view model's command removes the key object only).
        var delete = Item("Xóa", "Delete", (_, e) =>
        {
            ANLAbel.App.Controls.LabelDesignerCanvas.DeleteSelectionCommand.Execute(null, DesignerCanvas);
            DesignerCanvas.Focus();
            e.Handled = true;
        });
        _contextPasteItem = Item("Dán", "Ctrl+V", Paste_Click);
        _contextLockItem = new MenuItem { Header = "Khóa vị trí", IsCheckable = true };
        _contextLockItem.Click += (_, e) =>
        {
            // The whole selection, as NiceLabel's property pages set every selected object.
            foreach (var item in DesignerCanvas.SelectedObjects.DefaultIfEmpty(_viewModel.SelectedObject).OfType<LabelObject>())
            {
                item.IsLocked = _contextLockItem.IsChecked;
            }

            e.Handled = true;
        };
        _contextGroupItem = Item("Nhóm đối tượng", "Ctrl+G", GroupObjects_Click);
        _contextUngroupItem = Item("Bỏ nhóm", "Ctrl+Shift+G", UngroupObjects_Click);
        var arrange = new MenuItem { Header = "Sắp xếp" };
        arrange.Items.Add(Item("Đưa lên trên cùng", "Ctrl+PgUp", Run(_viewModel.BringToFrontCommand)));
        arrange.Items.Add(Item("Đưa lên một lớp", "PgUp", Run(_viewModel.BringForwardCommand)));
        arrange.Items.Add(Item("Đưa xuống một lớp", "PgDn", Run(_viewModel.SendBackwardCommand)));
        arrange.Items.Add(Item("Đưa xuống dưới cùng", "Ctrl+PgDn", Run(_viewModel.SendToBackCommand)));
        var align = new MenuItem { Header = "Căn chỉnh" };
        foreach (var (header, tag) in new[]
                 {
                     ("Căn trái", "Left"), ("Căn giữa theo chiều ngang", "HorizontalCenter"), ("Căn phải", "Right"),
                     ("Căn trên", "Top"), ("Căn giữa theo chiều dọc", "VerticalCenter"), ("Căn dưới", "Bottom")
                 })
        {
            align.Items.Add(Item(header, string.Empty, ArrangeAlign_Click, tag));
        }

        align.Items.Add(new Separator());
        align.Items.Add(Item("Phân bố đều theo chiều ngang", string.Empty, ArrangeDistribute_Click, "HorizontalGaps"));
        align.Items.Add(Item("Phân bố đều theo chiều dọc", string.Empty, ArrangeDistribute_Click, "VerticalGaps"));

        var labelProperties = Item("Thuộc tính tem", string.Empty, PrinterSetup_Click);
        _contextSelectAllItem = Item("Chọn tất cả", "Ctrl+A", (_, e) =>
        {
            DesignerCanvas.SelectAllObjects();
            DesignerCanvas.Focus();
            e.Handled = true;
        });
        var zoomLabel = Item("Vừa tài liệu", string.Empty, ZoomToDocument_Click);
        var zoomObjects = Item("Vừa đối tượng", string.Empty, ZoomToObjects_Click);
        var grid = new MenuItem { Header = "Căn và lưới" };
        foreach (var item in canvasItems)
        {
            grid.Items.Add(item);
        }

        // NiceLabel 10's label menu: Object markers after Alignment and gridlines; the View tab's toggles.
        var markers = new MenuItem { Header = "Dấu đối tượng" };
        foreach (var (header, property) in new[]
                 {
                     ("Tên đối tượng", ANLAbel.App.Controls.LabelDesignerCanvas.ShowObjectNamesProperty),
                     ("Nguồn dữ liệu", ANLAbel.App.Controls.LabelDesignerCanvas.ShowDataSourceMarkersProperty),
                     ("Đối tượng khóa", ANLAbel.App.Controls.LabelDesignerCanvas.ShowLockedMarkersProperty)
                 })
        {
            var marker = new MenuItem { Header = header, IsCheckable = true };
            marker.SetBinding(MenuItem.IsCheckedProperty, new System.Windows.Data.Binding { Source = DesignerCanvas, Path = new PropertyPath(property), Mode = System.Windows.Data.BindingMode.TwoWay });
            markers.Items.Add(marker);
        }

        var objectHead = new Separator();
        var objectTail = new Separator();
        var labelTail = new Separator();
        foreach (var item in new Control[] { properties, _contextEditTextItem, labelProperties, objectHead, cut, copy, _contextPasteItem, _contextSelectAllItem, delete, objectTail, _contextLockItem, _contextGroupItem, _contextUngroupItem, arrange, align, zoomLabel, zoomObjects, labelTail, grid, markers })
        {
            menu.Items.Add(item);
        }

        _objectMenuItems.AddRange(new Control[] { properties, cut, copy, delete, objectTail, _contextLockItem, _contextGroupItem, _contextUngroupItem, arrange, align });
        _labelMenuItems.AddRange(new Control[] { labelProperties, _contextSelectAllItem, zoomLabel, zoomObjects, markers });
        DesignerCanvas.PreviewMouseRightButtonDown += (_, e) => SelectForContextMenu(e.GetPosition(DesignerCanvas));
        // The menu key or Shift+F10 opens it with no pointer (CursorLeft and CursorTop are -1).
        DesignerCanvas.ContextMenuOpening += (_, e) => PrepareDesignerContextMenu(e.CursorLeft < 0 && e.CursorTop < 0);
    }

    /// <summary>
    /// NiceLabel 10 Object browser: a right-click on an object selects it and offers its
    /// commands (SelectOnRightClick, the selector's context menu). A hidden object, which the
    /// label does not draw, is reached only here: Hiển thị brings it back.
    /// </summary>
    private void BuildObjectBrowserContextMenu()
    {
        MenuItem Item(string header, string gesture, RoutedEventHandler click)
        {
            var item = new MenuItem { Header = header, InputGestureText = gesture };
            item.Click += click;
            return item;
        }

        RoutedEventHandler Run(ICommand command) => (_, e) =>
        {
            command.Execute(null);
            e.Handled = true;
        };

        // Ticked when every selected object is shown or locked; a click sets them all the other way.
        var shown = Item("Hiển thị", string.Empty, (_, e) =>
        {
            _viewModel.SelectionShown = _viewModel.SelectionShown != true;
            e.Handled = true;
        });
        var locked = Item("Khóa vị trí", string.Empty, (_, e) =>
        {
            _viewModel.SelectionLocked = _viewModel.SelectionLocked != true;
            e.Handled = true;
        });
        var arrange = new MenuItem { Header = "Sắp xếp" };
        arrange.Items.Add(Item("Đưa lên trên cùng", "Ctrl+PgUp", Run(_viewModel.BringToFrontCommand)));
        arrange.Items.Add(Item("Đưa lên một lớp", "PgUp", Run(_viewModel.BringForwardCommand)));
        arrange.Items.Add(Item("Đưa xuống một lớp", "PgDn", Run(_viewModel.SendBackwardCommand)));
        arrange.Items.Add(Item("Đưa xuống dưới cùng", "Ctrl+PgDn", Run(_viewModel.SendToBackCommand)));
        var menu = new ContextMenu();
        foreach (var item in new Control[]
                 {
                     Item("Thuộc tính", "F2", (_, e) =>
                     {
                         _viewModel.IsPropertiesVisible = true;
                         if (!_viewModel.IsPropertiesPinned)
                         {
                             OpenFloatingProperties();
                         }

                         e.Handled = true;
                     }),
                     new Separator(),
                     Item("Cắt", "Ctrl+X", Cut_Click),
                     Item("Sao chép", "Ctrl+C", Copy_Click),
                     Item("Xóa", "Delete", (_, e) =>
                     {
                         ANLAbel.App.Controls.LabelDesignerCanvas.DeleteSelectionCommand.Execute(null, DesignerCanvas);
                         e.Handled = true;
                     }),
                     new Separator(),
                     shown,
                     locked,
                     arrange
                 })
        {
            menu.Items.Add(item);
        }

        menu.Opened += (_, _) =>
        {
            shown.IsChecked = _viewModel.SelectionShown == true;
            locked.IsChecked = _viewModel.SelectionLocked == true;
        };
        ObjectBrowserList.ContextMenu = menu;
        // Only on an object: the list's empty space has no commands.
        ObjectBrowserList.ContextMenuOpening += (_, e) =>
        {
            if (ItemsControl.ContainerFromElement(ObjectBrowserList, e.OriginalSource as DependencyObject) is not ListBoxItem)
            {
                e.Handled = true;
            }
        };
    }

    /// <summary>
    /// A right-click selects the object under the pointer, as NiceLabel does;
    /// one already in a multi-selection keeps the group. Empty label space
    /// leaves the selection and offers the label's menu.
    /// </summary>
    private void SelectForContextMenu(Point point)
    {
        _contextMenuObject = DesignerCanvas.DrawingTool is null ? ObjectAt(point) : null;
        if (_contextMenuObject is { } item && !DesignerCanvas.SetKeyObject(item))
        {
            DesignerCanvas.SelectedObject = item;
        }
    }

    /// <summary>
    /// From the keyboard the menu is the selection's, as NiceLabel applies its context commands to the
    /// selected objects (the label's when nothing is selected); a right-click has picked the object under
    /// the pointer already. Before, the keyboard menu kept whatever the last right-click had picked.
    /// </summary>
    private void PrepareDesignerContextMenu(bool byKeyboard)
    {
        if (byKeyboard)
        {
            _contextMenuObject = _viewModel.SelectedObject;
        }

        UpdateDesignerContextMenu();
    }

    private void UpdateDesignerContextMenu()
    {
        var onObject = _contextMenuObject is not null;
        foreach (var item in _objectMenuItems)
        {
            item.Visibility = onObject ? Visibility.Visible : Visibility.Collapsed;
        }

        foreach (var item in _labelMenuItems)
        {
            item.Visibility = onObject ? Visibility.Collapsed : Visibility.Visible;
        }

        if (_contextPasteItem is not null)
        {
            _contextPasteItem.IsEnabled = DesignerCanvas.HasCopiedObjects;
        }

        if (_contextEditTextItem is not null)
        {
            // Only a Text or Text Box with fixed content is edited on the label.
            _contextEditTextItem.Visibility = onObject && CanEditInPlace(_contextMenuObject) ? Visibility.Visible : Visibility.Collapsed;
        }

        if (_contextSelectAllItem is not null)
        {
            _contextSelectAllItem.IsEnabled = _viewModel.Template.Objects.Any(item => item.IsVisible);
        }

        if (_contextLockItem is not null)
        {
            // Ticked when every selected object is locked; a click locks or frees them all.
            _contextLockItem.IsChecked = _viewModel.SelectionLocked == true;
        }

        if (_contextGroupItem is not null)
        {
            _contextGroupItem.IsEnabled = DesignerCanvas.SelectedObjectCount > 1;
        }

        if (_contextUngroupItem is not null)
        {
            _contextUngroupItem.IsEnabled = !string.IsNullOrEmpty(_viewModel.SelectedObject?.GroupId);
        }
    }

    /// <summary>The topmost visible object under a canvas point, locked ones and lines included.</summary>
    private LabelObject? ObjectAt(Point point)
    {
        if (_viewModel.Template is not { } template || DesignerCanvas.Zoom <= 0)
        {
            return null;
        }

        var xMm = ANLAbel.Core.Geometry.MmConverter.DipToMm(point.X / DesignerCanvas.Zoom);
        var yMm = ANLAbel.Core.Geometry.MmConverter.DipToMm(point.Y / DesignerCanvas.Zoom);
        // A hairline still offers a 1.5 mm target.
        const double minimumTargetMm = 1.5;
        // With equal ZIndex the later object is drawn on top, as the canvas adds them.
        return template.Objects
            .Select((item, index) => (Item: item, Index: index))
            .Where(entry => entry.Item.IsVisible)
            .OrderByDescending(entry => entry.Item.ZIndex)
            .ThenByDescending(entry => entry.Index)
            .Select(entry => entry.Item)
            .FirstOrDefault(item =>
            {
                var bounds = ANLAbel.Core.Geometry.TransformedBoundsContract.GetBounds(item);
                var padX = Math.Max(0, (minimumTargetMm - (bounds.Right - bounds.Left)) / 2);
                var padY = Math.Max(0, (minimumTargetMm - (bounds.Bottom - bounds.Top)) / 2);
                return xMm >= bounds.Left - padX && xMm <= bounds.Right + padX && yMm >= bounds.Top - padY && yMm <= bounds.Bottom + padY;
            });
    }

    private void GroupObjects_Click(object sender, RoutedEventArgs e)
    {
        DesignerCanvas.GroupSelectedObjects();
        DesignerCanvas.Focus();
        e.Handled = true;
    }

    private void UngroupObjects_Click(object sender, RoutedEventArgs e)
    {
        DesignerCanvas.UngroupSelectedObjects();
        DesignerCanvas.Focus();
        e.Handled = true;
    }

    /// <summary>
    /// Designer keys act on the label, as NiceLabel 10's ribbon keys do: not while a box
    /// takes the typing (it keeps its own Ctrl+A, Ctrl+X), nor behind the File backstage.
    /// </summary>
    private bool DesignerKeysApply()
        => BackstagePanel.Visibility != Visibility.Visible && !IsTextInputTarget(Keyboard.FocusedElement);

    /// <summary>
    /// A designer key that changes the label does nothing while the label is protected. An
    /// arrow-key move still open is finished first, so Esc cannot take this edit back with it.
    /// </summary>
    private void RunUnlessLocked(Func<bool> edit)
    {
        if (DesignerKeysApply() && !_viewModel.IsEditingLocked)
        {
            DesignerCanvas.CommitNudgeGesture();
            edit();
            DesignerCanvas.Focus();
        }
    }

    /// <summary>
    /// Ctrl+A: every object, and the label takes the keys so the arrows move them. An armed
    /// Format Painter is put down first: it would paint only the object Ctrl+A makes the key.
    /// </summary>
    private void SelectAllByKey()
    {
        if (!DesignerKeysApply())
        {
            return;
        }

        DesignerCanvas.CommitNudgeGesture();
        FormatPainterToggle.IsChecked = false;
        if (DesignerCanvas.SelectAllObjects())
        {
            DesignerCanvas.Focus();
        }
    }

    /// <summary>Ctrl+B, Ctrl+I, Ctrl+U: the ribbon's Đậm, Nghiêng and Gạch chân.</summary>
    private void ToggleTextStyle(Action<ObjectStyle> toggle)
    {
        if (DesignerKeysApply()
            && !_viewModel.IsEditingLocked
            && _viewModel.SelectedObject is { Type: ObjectType.Text or ObjectType.TextBox } item)
        {
            DesignerCanvas.CommitNudgeGesture();
            // The key text's change reaches every selected text, as the ribbon toggle's does.
            toggle(item.Style);
        }
    }

    /// <summary>
    /// PgUp/PgDn move the selected object one layer up or down, Ctrl+PgUp/PgDn to the top
    /// or the bottom (NiceLabel 10's Arrange keys). False when there is nothing to order.
    /// </summary>
    private bool ArrangeByKey(Key key, ModifierKeys modifiers)
    {
        if (key is not (Key.PageUp or Key.PageDown)
            || modifiers is not (ModifierKeys.None or ModifierKeys.Control)
            || _viewModel.IsEditingLocked
            || _viewModel.SelectedObject is null)
        {
            return false;
        }

        var toEnd = modifiers == ModifierKeys.Control;
        var command = key == Key.PageUp
            ? toEnd ? _viewModel.BringToFrontCommand : _viewModel.BringForwardCommand
            : toEnd ? _viewModel.SendToBackCommand : _viewModel.SendBackwardCommand;
        DesignerCanvas.CommitNudgeGesture();
        command.Execute(null);
        return true;
    }

    /// <summary>
    /// Tab and Shift+Tab select the next or the previous object in the label's paint order,
    /// round to the start, as on NiceLabel 10's design surface; a group is one stop, selected
    /// whole as a click selects it. False when there is none.
    /// </summary>
    private bool SelectNextByKey(Key key, ModifierKeys modifiers)
    {
        if (key != Key.Tab || modifiers is not (ModifierKeys.None or ModifierKeys.Shift) || DesignerCanvas.DrawingTool is not null)
        {
            return false;
        }

        var order = _viewModel.Template.Objects
            .Select((item, index) => (item, index))
            .Where(entry => entry.item.IsVisible)
            .OrderBy(entry => entry.item.ZIndex)
            .ThenBy(entry => entry.index)
            .Select(entry => entry.item)
            .ToList();
        if (order.Count == 0)
        {
            return false;
        }

        var backwards = modifiers == ModifierKeys.Shift;
        var current = _viewModel.SelectedObject is { } selected ? order.IndexOf(selected) : -1;
        var group = current < 0 ? string.Empty : order[current].GroupId;
        for (var step = 1; step <= order.Count; step++)
        {
            var next = current < 0
                ? (backwards ? order.Count - step : step - 1)
                : ((current + (backwards ? -step : step)) % order.Count + order.Count) % order.Count;
            if (group.Length > 0 && order[next].GroupId == group)
            {
                continue;
            }

            // A group is one stop, at its first member in paint order.
            if (order[next].GroupId.Length > 0 && !ReferenceEquals(order.First(item => item.GroupId == order[next].GroupId), order[next]))
            {
                continue;
            }

            // An arrow-key move still open is finished first, as a click does.
            DesignerCanvas.CommitNudgeGesture();
            DesignerCanvas.SelectAsClicked(order[next]);
            return true;
        }

        return false;
    }

    /// <summary>
    /// F2 opens the selected object's properties, as a double-click does and as NiceLabel 10's
    /// design surface opens the property editor; for a text the cursor goes to its content,
    /// the words selected to type over.
    /// </summary>
    private void OpenPropertiesByKey()
    {
        if (!DesignerKeysApply() || _viewModel.SelectedObject is not { } item)
        {
            return;
        }

        DesignerCanvas.CommitNudgeGesture();
        // NiceLabel 10: F2 edits an item that supports in-place editing, else opens its properties.
        if (BeginInPlaceEdit())
        {
            return;
        }

        _viewModel.IsPropertiesVisible = true;
        if (!_viewModel.IsPropertiesPinned)
        {
            OpenFloatingProperties();
        }

        if (item.Type is ObjectType.Text or ObjectType.TextBox)
        {
            Dispatcher.BeginInvoke(() =>
            {
                if (ObjectTextBox.IsVisible && ObjectTextBox.Focus())
                {
                    ObjectTextBox.SelectAll();
                }
            }, System.Windows.Threading.DispatcherPriority.Input);
        }
    }

    /// <summary>Ctrl+F, NiceLabel's object browser search: the Đối tượng tab's search box.</summary>
    private void FocusObjectSearch()
    {
        if (BackstagePanel.Visibility == Visibility.Visible)
        {
            return;
        }

        // The search sits in the left panel: Ctrl+F shows the panel when it was hidden.
        _viewModel.IsToolboxVisible = true;
        DependencyObject? node = ObjectBrowserSearch;
        while (node is not null and not TabItem)
        {
            node = LogicalTreeHelper.GetParent(node);
        }

        if (node is TabItem tab)
        {
            tab.IsSelected = true;
        }

        Dispatcher.BeginInvoke(() =>
        {
            ObjectBrowserSearch.Focus();
            ObjectBrowserSearch.SelectAll();
        }, System.Windows.Threading.DispatcherPriority.Input);
    }

    private LabelObject? _richTextEditorObject;

    /// <summary>Loads the selected Rich Text Box into the Properties editor.</summary>
    private void LoadRichTextEditor()
    {
        _richTextEditorObject = _viewModel.SelectedObject?.Type == ObjectType.RichTextBox ? _viewModel.SelectedObject : null;
        RichTextEditor.Document = RichTextLayout.CreateDocument(_richTextEditorObject?.RichText, 260);
        SyncRichFontPickers();
    }

    /// <summary>The ribbon's rich-text font and size show the editor selection (or the whole content).</summary>
    private void SyncRichFontPickers()
    {
        if (_richTextEditorObject is null)
        {
            return;
        }

        var range = RichTextEditor.Selection.IsEmpty
            ? new TextRange(RichTextEditor.Document.ContentStart, RichTextEditor.Document.ContentEnd)
            : new TextRange(RichTextEditor.Selection.Start, RichTextEditor.Selection.End);
        var family = (range.GetPropertyValue(TextElement.FontFamilyProperty) as FontFamily)?.Source;
        RichFontFamilyBox.SelectedItem = family is not null && _viewModel.FontFamilies.Contains(family) ? family : null;
        var size = range.GetPropertyValue(TextElement.FontSizeProperty) is double dip ? Math.Round(dip * 72.0 / 96.0, 1) : (double?)null;
        RichFontSizeBox.SelectedItem = size is { } pt && _viewModel.FontSizes.Contains(pt) ? pt : null;
    }

    private void RichTextEditor_SelectionChanged(object sender, RoutedEventArgs e) => SyncRichFontPickers();

    private void RichTextEditor_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        => SaveRichTextEditor();

    /// <summary>
    /// Ribbon Font group on a Rich Text Box (NiceLabel 10): formats the text
    /// selected in the Properties editor, or the whole content when nothing is
    /// selected, then stores the RTF as one undoable edit.
    /// </summary>
    private void ApplyRichFormat(Action<TextRange> format)
    {
        if (_richTextEditorObject is null || _viewModel.SelectedObject != _richTextEditorObject)
        {
            return;
        }

        var range = RichTextEditor.Selection.IsEmpty
            ? new TextRange(RichTextEditor.Document.ContentStart, RichTextEditor.Document.ContentEnd)
            : new TextRange(RichTextEditor.Selection.Start, RichTextEditor.Selection.End);
        format(range);
        SaveRichTextEditor();
        SyncRichFontPickers();
    }

    private void SaveRichTextEditor()
    {
        if (_richTextEditorObject is not { } item)
        {
            return;
        }

        var rtf = RichTextLayout.ToRtf(RichTextEditor.Document);
        if (!string.Equals(rtf, item.RichText, StringComparison.Ordinal))
        {
            _viewModel.BeginTemplateEditGesture();
            item.RichText = rtf;
            _viewModel.CommitTemplateEditGesture();
        }
    }

    private static double RichFontSizePt(TextRange range)
        => range.GetPropertyValue(TextElement.FontSizeProperty) is double dip ? dip * 72.0 / 96.0 : 9;

    private static void ToggleRichDecoration(TextRange range, TextDecorationCollection decoration)
    {
        var current = range.GetPropertyValue(Inline.TextDecorationsProperty) as TextDecorationCollection;
        var has = current is not null && decoration.All(d => current.Any(c => c.Location == d.Location));
        var next = new TextDecorationCollection(current?.Where(c => decoration.All(d => d.Location != c.Location)) ?? []);
        if (!has)
        {
            foreach (var d in decoration)
            {
                next.Add(d);
            }
        }

        range.ApplyPropertyValue(Inline.TextDecorationsProperty, next);
    }

    private void RichFormat_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: string action })
        {
            return;
        }

        ApplyRichFormat(range =>
        {
            switch (action)
            {
                case "Bold":
                    var bold = range.GetPropertyValue(TextElement.FontWeightProperty) is FontWeight weight && weight == FontWeights.Bold;
                    range.ApplyPropertyValue(TextElement.FontWeightProperty, bold ? FontWeights.Normal : FontWeights.Bold);
                    break;
                case "Italic":
                    var italic = range.GetPropertyValue(TextElement.FontStyleProperty) is FontStyle style && style == FontStyles.Italic;
                    range.ApplyPropertyValue(TextElement.FontStyleProperty, italic ? FontStyles.Normal : FontStyles.Italic);
                    break;
                case "Underline":
                    ToggleRichDecoration(range, TextDecorations.Underline);
                    break;
                case "Strikethrough":
                    ToggleRichDecoration(range, TextDecorations.Strikethrough);
                    break;
                case "Grow":
                case "Shrink":
                    var size = Math.Clamp(RichFontSizePt(range) + (action == "Grow" ? 1 : -1), 4, 200);
                    range.ApplyPropertyValue(TextElement.FontSizeProperty, size * 96.0 / 72.0);
                    break;
                case "Left":
                    range.ApplyPropertyValue(Paragraph.TextAlignmentProperty, TextAlignment.Left);
                    break;
                case "Center":
                    range.ApplyPropertyValue(Paragraph.TextAlignmentProperty, TextAlignment.Center);
                    break;
                case "Right":
                    range.ApplyPropertyValue(Paragraph.TextAlignmentProperty, TextAlignment.Right);
                    break;
                case "Justify":
                    range.ApplyPropertyValue(Paragraph.TextAlignmentProperty, TextAlignment.Justify);
                    break;
            }
        });
        e.Handled = true;
    }

    private void RichFontFamily_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (RichFontFamilyBox.SelectedItem is string family && RichFontFamilyBox.IsDropDownOpen | RichFontFamilyBox.IsKeyboardFocusWithin)
        {
            ApplyRichFormat(range => range.ApplyPropertyValue(TextElement.FontFamilyProperty, new FontFamily(family)));
        }
    }

    private void RichFontSize_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (RichFontSizeBox.SelectedItem is double size && RichFontSizeBox.IsDropDownOpen | RichFontSizeBox.IsKeyboardFocusWithin)
        {
            ApplyRichFormat(range => range.ApplyPropertyValue(TextElement.FontSizeProperty, size * 96.0 / 72.0));
        }
    }

    private void RichColor_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (RichColorBox.SelectedItem is string color && RichColorBox.IsDropDownOpen | RichColorBox.IsKeyboardFocusWithin)
        {
            ApplyRichFormat(range => range.ApplyPropertyValue(TextElement.ForegroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString(color))));
        }
    }

    private LabelObject? _formatPainterSource;

    private void FormatPainter_Checked(object sender, RoutedEventArgs e)
    {
        // NiceLabel 10 Format Painter: the selected object is the style source.
        _formatPainterSource = _viewModel.SelectedObject;
        if (_formatPainterSource is null)
        {
            FormatPainterToggle.IsChecked = false;
        }
    }

    private void ApplyFormatPainter(LabelObject source, LabelObject target)
    {
        _viewModel.BeginTemplateEditGesture($"Chép định dạng sang “{target.Name}”");
        LabelObjectCloner.CopyStyle(source, target);
        _viewModel.CommitTemplateEditGesture();
    }

    private void HistoryList_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button)
        {
            var menu = BuildHistoryMenu(Equals(button.Tag, "Redo"));
            menu.PlacementTarget = button;
            menu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            menu.IsOpen = true;
        }
    }

    /// <summary>
    /// NiceLabel 10's Undo ▾ and Redo ▾: the named steps, newest first. A choice
    /// takes back (or puts back) that step with every step above it.
    /// </summary>
    private ContextMenu BuildHistoryMenu(bool redo)
    {
        // An edit still being recorded is a step too: the counts below are what UndoSteps takes back.
        _viewModel.CloseOpenHistoryStep();
        var menu = new ContextMenu();
        var steps = redo ? _viewModel.RedoHistory : _viewModel.UndoHistory;
        for (var index = 0; index < Math.Min(steps.Count, 20); index++)
        {
            var count = index + 1;
            // A TextBlock header: an underscore in an object name is not an access key.
            var item = new MenuItem { Header = new TextBlock { Text = steps[index] }, InputGestureText = count == 1 ? string.Empty : $"{count} bước" };
            System.Windows.Automation.AutomationProperties.SetName(item, steps[index]);
            item.Click += (_, _) =>
            {
                if (redo)
                {
                    _viewModel.RedoSteps(count);
                }
                else
                {
                    _viewModel.UndoSteps(count);
                }
            };
            menu.Items.Add(item);
        }

        return menu;
    }

    private void Paste_Click(object sender, RoutedEventArgs e)
    {
        DesignerCanvas.PasteCopiedObjects();
        DesignerCanvas.Focus();
        e.Handled = true;
    }

    private void Cut_Click(object sender, RoutedEventArgs e)
    {
        DesignerCanvas.CutSelectedObjects();
        DesignerCanvas.Focus();
        e.Handled = true;
    }

    private void Copy_Click(object sender, RoutedEventArgs e)
    {
        DesignerCanvas.CopySelectedObjects();
        DesignerCanvas.Focus();
        e.Handled = true;
    }

    private void DataSources_Click(object sender, RoutedEventArgs e)
    {
        ShowDataManager(null);
        e.Handled = true;
    }

    private void ZoomToDocument_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel viewModel)
        {
            return;
        }

        // Fit the label and the canvas margins into the visible surface.
        var labelWidthDip = MmConverter.MmToDip(viewModel.Template.WidthMm) + 212;
        var labelHeightDip = MmConverter.MmToDip(viewModel.Template.HeightMm) + 172;
        var fit = Math.Min(
            DesignerScrollViewer.ViewportWidth / Math.Max(1, labelWidthDip),
            DesignerScrollViewer.ViewportHeight / Math.Max(1, labelHeightDip));
        if (double.IsFinite(fit) && fit > 0)
        {
            viewModel.Zoom = Math.Clamp(fit, 0.25, 4);
        }

        e.Handled = true;
    }

    private void RotateView_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel viewModel)
        {
            viewModel.ViewRotation += 90;
        }

        e.Handled = true;
    }

    private void FontGrow_Click(object sender, RoutedEventArgs e) => StepFontSize(1);

    private void FontShrink_Click(object sender, RoutedEventArgs e) => StepFontSize(-1);

    /// <summary>NiceLabel 10 Grow/Shrink Font: one point per click, for every selected text.</summary>
    private void StepFontSize(double stepPt) => _viewModel.StepSelectedFontSize(stepPt);

    private void DoNotAlign_Click(object sender, RoutedEventArgs e)
    {
        DesignerCanvas.IsSnapToObjectsEnabled = false;
        DesignerCanvas.IsSnapToGridEnabled = false;
        e.Handled = true;
    }

    private void ZoomToObjects_Click(object sender, RoutedEventArgs e)
    {
        var items = _viewModel.Template.Objects.Where(item => item.IsVisible).ToArray();
        if (items.Length == 0)
        {
            ZoomToDocument_Click(sender, e);
            return;
        }

        var widthMm = Math.Max(1, items.Max(item => item.XMm + item.WidthMm) - Math.Min(0, items.Min(item => item.XMm)));
        var heightMm = Math.Max(1, items.Max(item => item.YMm + item.HeightMm) - Math.Min(0, items.Min(item => item.YMm)));
        var fit = Math.Min(
            DesignerScrollViewer.ViewportWidth / (MmConverter.MmToDip(widthMm) + 212),
            DesignerScrollViewer.ViewportHeight / (MmConverter.MmToDip(heightMm) + 172));
        if (double.IsFinite(fit) && fit > 0)
        {
            _viewModel.Zoom = Math.Clamp(fit, 0.25, 4);
        }

        e.Handled = true;
    }

    private void Zoom100_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel viewModel)
        {
            viewModel.Zoom = 1;
        }

        e.Handled = true;
    }

    private void ArrangeAlign_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: string alignmentText }
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
        if (sender is not FrameworkElement { Tag: string distributionText }
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

    private void ResetSerialCounter_Click(object sender, RoutedEventArgs e)
    {
        var answer = OperatorDialog.Ask(
            this,
            "Đặt lại bộ đếm serial",
            ["Đặt lại bộ đếm sẽ bắt đầu một dãy số mới từ giá trị Bắt đầu. Các số đã in trước đó có thể bị in lại (trùng số). Tiếp tục?"],
            null,
            OperatorDialogTone.Warning,
            new OperatorDialogChoice("reset", "Đặt lại"),
            new OperatorDialogChoice("cancel", "Không đặt lại", IsDefault: true, IsCancel: true));
        if (answer == "reset")
        {
            _viewModel.ResetSerialCounter();
        }
    }

    private void ManageDataSources_Click(object sender, RoutedEventArgs e)
    {
        ShowDatabaseManager();
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
        if (!await ConfirmDocumentCanBeReplacedAsync("tạo mẫu mới"))
        {
            return;
        }

        var dialog = new NewTemplateWindow { Owner = this };
        if (dialog.ShowDialog() == true)
        {
            _viewModel.NewTemplate(dialog.Request);
            if (dialog.OpenLabelPropertiesNext)
            {
                // NiceLabel 10 New Label Setup Wizard: printer, label dimensions, paper.
                await ShowPrinterSetupDialogAsync(guided: true);
            }

            await _viewModel.RefreshPrinterQueueStatusAsync();
        }
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        await SaveDocumentAsync(saveAs: false);
    }

    private async void Open_Click(object sender, RoutedEventArgs e)
    {
        if (!await ConfirmDocumentCanBeReplacedAsync("mở mẫu khác"))
        {
            return;
        }

        var dialog = new OpenFileDialog
        {
            Title = "Mở mẫu tem ANLAbel",
            Filter = "Mẫu tem ANLAbel (*.anlabel)|*.anlabel|Tem NiceLabel (*.lbl;*.nlbl)|*.lbl;*.nlbl|Tệp JSON (*.json)|*.json|Tất cả tệp (*.*)|*.*"
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        await OpenDocumentPathAsync(dialog.FileName);
    }

    private async void RevisionHistory_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(_viewModel.CurrentFilePath)
            && !await ConfirmDocumentCanBeReplacedAsync("xem và khôi phục phiên bản"))
        {
            return;
        }

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

    private void ImportExcel_Click(object sender, RoutedEventArgs e)
    {
        new ExcelImportWindow(_viewModel, ExcelImportStart.BrowseWorkbook) { Owner = this }.ShowDialog();
    }

    private async void PrinterSetup_Click(object sender, RoutedEventArgs e)
    {
        await ShowPrinterSetupDialogAsync();
        await _viewModel.RefreshPrinterQueueStatusAsync();
    }

    private void PrintPreview_Click(object sender, RoutedEventArgs e)
    {
        OpenPrintPreview();
    }

    private async void PrintCurrent_Click(object sender, RoutedEventArgs e)
    {
        if (!await TryEnsurePrinterSelectedAsync())
        {
            return;
        }

        await RunQuickPrintAsync(_viewModel.PrintCurrentRowCommand, "Không có dòng nào để in.");
    }

    private async void PrintAllRows_Click(object sender, RoutedEventArgs e)
    {
        if (!await TryEnsurePrinterSelectedAsync())
        {
            return;
        }

        await RunQuickPrintAsync(_viewModel.PrintAllRowsCommand, "Không có dòng Excel nào để in.");
    }

    /// <summary>
    /// Runs a quick-print command to its end with a wait cursor, and says why a
    /// click did nothing instead of ignoring it (audit 2026-10-13).
    /// </summary>
    private async Task RunQuickPrintAsync(System.Windows.Input.ICommand command, string nothingToPrint)
    {
        if (!command.CanExecute(null))
        {
            _viewModel.StatusText = _viewModel.IsQuickPrinting ? "Đang gửi lệnh in trước, vui lòng đợi." : nothingToPrint;
            return;
        }

        Mouse.OverrideCursor = Cursors.Wait;
        try
        {
            if (command is AsyncRelayCommand asyncCommand)
            {
                await asyncCommand.ExecuteAsync(null);
            }
            else
            {
                command.Execute(null);
            }
        }
        finally
        {
            Mouse.OverrideCursor = null;
        }
    }

    private async Task<bool> TryEnsurePrinterSelectedAsync()
    {
        if (!string.IsNullOrWhiteSpace(_viewModel.Template.PrinterProfile.PrinterName))
        {
            return true;
        }

        await ShowPrinterSetupDialogAsync();
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
            var openReport = OperatorDialog.Ask(
                this,
                "Xuất thành công",
                [$"Đã xuất ra tệp {dialog.FileName}.", "Bạn có muốn mở ngay bây giờ?"],
                null,
                OperatorDialogTone.Normal,
                new OperatorDialogChoice("open", "Mở ngay", IsDefault: true),
                new OperatorDialogChoice("cancel", "Để sau", IsCancel: true)) == "open";
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
            MessageBox.Show(this, ANLAbel.App.Services.OperationFailureExplainer.StatusLine(ex), "Xuất thất bại", MessageBoxButton.OK, MessageBoxImage.Error);
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
            if (!await ConfirmDocumentCanBeReplacedAsync("mở mẫu từ thư viện"))
            {
                return;
            }

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
            MessageBox.Show(this, ANLAbel.App.Services.OperationFailureExplainer.WithTechnicalIfGeneric(ANLAbel.App.Services.TemplateFailureExplainer.Explain(ex)), "Thư viện mẫu tem", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void UnlinkExcel_Click(object sender, RoutedEventArgs e)
    {
        var confirmed = OperatorDialog.Ask(
            this,
            "Hủy liên kết Excel",
            ["Hủy liên kết Excel khỏi mẫu tem này?", "Các đối tượng vẫn giữ trường liên kết nhưng sẽ hiển thị nội dung mẫu cho đến khi bạn nhập dữ liệu lại."],
            null,
            OperatorDialogTone.Warning,
            new OperatorDialogChoice("unlink", "Hủy liên kết", IsDefault: true),
            new OperatorDialogChoice("cancel", "Giữ liên kết", IsCancel: true)) == "unlink";
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
                MessageBox.Show(this, $"Xem trước bị chặn: lỗi ở hàm của Quản lý dữ liệu. {transformError}", "Xem trước bị chặn", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // A new job asks for its {?Name} values once, before preview; a
            // reprint takes the original job's values from the record instead.
            IReadOnlyDictionary<string, string>? promptValues = null;
            var prompts = JobVariableContract.RequiredPrompts(_viewModel.Template);
            if (approvedReprintJobId is null && prompts.Count > 0)
            {
                promptValues = PromptValuesWindow.Ask(this, prompts, _viewModel.LastPromptValues);
                if (promptValues is null)
                {
                    return;
                }

                _viewModel.LastPromptValues = promptValues;
            }

            var dialog = new PrintPreviewWindow(_viewModel.Template, _viewModel.PreviewRow, _viewModel.ExcelDataView, _viewModel.PrintService, _viewModel.PrintLogService, _viewModel.CurrentFilePath, approvedReprintJobId, approvedReprintManifest, preparedRows, _viewModel.ImportedExcelDataSourceIdentity, promptValues) { Owner = this };
            dialog.ShowDialog();
            if (promptValues is not null)
            {
                // Answers changed in the preview are offered again next time.
                _viewModel.LastPromptValues = dialog.PromptValues;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Print preview open error: {ex}");
            MessageBox.Show(
                this,
                $"Không thể mở Xem trước khi in. {ANLAbel.App.Services.OperationFailureExplainer.StatusLine(ex)}",
                "Xem trước không khả dụng",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private async void PrinterQueueWarning_Click(object sender, RoutedEventArgs e)
    {
        var choice = OperatorDialog.Ask(
            this,
            "Hàng đợi máy in không khả dụng",
            [_viewModel.PrinterQueueStatusMessage, "Mở Cài đặt máy in ngay bây giờ để chọn hàng đợi đã xác thực?"],
            null,
            OperatorDialogTone.Warning,
            new OperatorDialogChoice("open", "Mở Cài đặt máy in", IsDefault: true),
            new OperatorDialogChoice("cancel", "Để sau", IsCancel: true));
        if (choice != "open")
        {
            return;
        }

        await ShowPrinterSetupDialogAsync();
        await _viewModel.RefreshPrinterQueueStatusAsync();
    }

    private async Task ShowPrinterSetupDialogAsync(bool guided = false)
    {
        try
        {
            var printers = await ANLAbel.App.Services.PrinterQueueLoader.LoadForDialogAsync(this, _viewModel.DiscoverInstalledPrinters);
            if (printers is null)
            {
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
                printService: _viewModel.PrintService,
                initialLayout: PageLayoutSelection.From(profile)) { Owner = this, IsGuided = guided };
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
                    dialog.SelectedScaleY,
                    dialog.SelectedLayout);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Không thể đọc danh sách máy in đã cài đặt.\n\n{ANLAbel.App.Services.OperationFailureExplainer.StatusLine(ex)}", "Cài đặt máy in", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private Point? _dataDragStart;

    private void DataTree_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        => _dataDragStart = e.GetPosition(null);

    /// <summary>
    /// NiceLabel 10 Data explorer: a prompt or field dragged onto the label
    /// becomes a bound Text there, or binds the object it is dropped on.
    /// </summary>
    /// <summary>The binding an object dropped from a Data panel item gets; null for a heading.</summary>
    private static string? DataTreeBinding(object? item) => item switch
    {
        ANLAbel.Core.Models.PromptVariableDefinition prompt when prompt.Name.Length > 0 => "{?" + prompt.Name + "}",
        ANLAbel.Core.Data.DataTransformDefinition function when !string.IsNullOrWhiteSpace(function.Name) => "{" + function.Name.Trim() + "}",
        ANLAbel.App.ViewModels.DataTreeField field => field.Binding,
        string field when field.Length > 0 => "{" + field + "}",
        _ => null
    };

    private void DataTree_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (_dataDragStart is not { } start || e.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        var moved = e.GetPosition(null) - start;
        if (Math.Abs(moved.X) < SystemParameters.MinimumHorizontalDragDistance
            && Math.Abs(moved.Y) < SystemParameters.MinimumVerticalDragDistance)
        {
            return;
        }

        _dataDragStart = null;
        var binding = DataTreeBinding((e.OriginalSource as FrameworkElement)?.DataContext);
        if (binding is not null)
        {
            DragDrop.DoDragDrop((DependencyObject)sender, new DataObject(ANLAbel.App.Controls.LabelDesignerCanvas.DataSourceDragFormat, binding), DragDropEffects.Copy);
        }
    }
}
