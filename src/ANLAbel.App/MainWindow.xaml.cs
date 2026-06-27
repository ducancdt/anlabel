using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ANLAbel.App.ViewModels;
using ANLAbel.Core.Enums;
using ANLAbel.Core.Geometry;
using ANLAbel.Core.Models;
using ANLAbel.Printing.RenderPipeline;
using Microsoft.Win32;

namespace ANLAbel.App;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel = new();
    private bool _syncingContentSource;
    private bool _syncingExcelField;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = _viewModel;
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

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        Loaded -= MainWindow_Loaded;

        // Ensure window fits within the working area (handles high DPI / small screens)
        var workArea = SystemParameters.WorkArea;
        Width = Math.Min(Width, workArea.Width);
        Height = Math.Min(Height, workArea.Height);
        if (Left + Width > workArea.Right)
            Left = Math.Max(workArea.Left, workArea.Right - Width);
        if (Top + Height > workArea.Bottom)
            Top = Math.Max(workArea.Top, workArea.Bottom - Height);
        if (Left < workArea.Left) Left = workArea.Left;
        if (Top < workArea.Top) Top = workArea.Top;

        UpdateContentSourceSelection();
        ShowPrinterSetupDialog();
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
        return originalSource is TextBox or PasswordBox or ComboBox;
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
        return !IsTextBoxOverflowing(item, candidate);
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

    private void New_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new NewTemplateWindow { Owner = this };
        if (dialog.ShowDialog() == true)
        {
            _viewModel.NewTemplate(dialog.Request);
        }
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        var filePath = _viewModel.CurrentFilePath;
        if (string.IsNullOrWhiteSpace(filePath))
        {
            var dialog = new SaveFileDialog
            {
                Title = "Save ANLAbel template",
                Filter = "ANLAbel Template (*.anlabel)|*.anlabel|JSON (*.json)|*.json",
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
            MessageBox.Show(this, ex.Message, "Save failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void Open_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Open ANLAbel template",
            Filter = "ANLAbel Template (*.anlabel)|*.anlabel|JSON (*.json)|*.json|All files (*.*)|*.*"
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        try
        {
            await _viewModel.OpenAsync(dialog.FileName);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Open failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void ImportExcel_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ExcelImportWindow(_viewModel) { Owner = this };
        dialog.ShowDialog();
    }

    private void PrinterSetup_Click(object sender, RoutedEventArgs e)
    {
        ShowPrinterSetupDialog();
    }

    private void PrintPreview_Click(object sender, RoutedEventArgs e)
    {
        OpenPrintPreview();
    }

    private void PrintHistory_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.OpenPrintHistoryFile();
    }

    private void Help_Click(object sender, RoutedEventArgs e)
    {
        new HelpWindow { Owner = this }.ShowDialog();
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
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Template Library", MessageBoxButton.OK, MessageBoxImage.Error);
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
        var validationError = _viewModel.ValidatePrintPreviewContent();
        if (validationError is not null)
        {
            MessageBox.Show(this, validationError, "Preview blocked", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var dialog = new PrintPreviewWindow(_viewModel.Template, _viewModel.PreviewRow, _viewModel.ExcelDataView, _viewModel.PrintService, _viewModel.PrintLogService, _viewModel.CurrentFilePath)
        {
            Owner = this
        };
        dialog.ShowDialog();
    }

    private void ShowPrinterSetupDialog()
    {
        try
        {
            var printers = _viewModel.GetInstalledPrinters();
            if (printers.Count == 0)
            {
                MessageBox.Show(this, "No Windows printers were found.", "Printer setup", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dialog = new PrinterSetupWindow(printers) { Owner = this };
            if (dialog.ShowDialog() == true && dialog.SelectedPrinter is not null && dialog.SelectedPaper is not null)
            {
                _viewModel.ApplyPrinterSelection(dialog.SelectedPrinter, dialog.SelectedPaper, dialog.SelectedDpi, dialog.SelectedOrientation);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Cannot read installed printers.\n\n{ex.Message}", "Printer setup", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
