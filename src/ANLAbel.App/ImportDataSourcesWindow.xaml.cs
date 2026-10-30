using System.IO;
using System.Windows;
using System.Windows.Input;
using ANLAbel.App.Services;
using ANLAbel.App.ViewModels;
using ANLAbel.Core.Data;
using ANLAbel.Core.Models;
using Microsoft.Win32;

namespace ANLAbel.App;

/// <summary>
/// Reads another label and merges its prompt variables and functions into the given
/// lists (<see cref="DataDefinitionImport"/>). After "Nhập", <see cref="Prompts"/> and
/// <see cref="Transforms"/> hold the merged lists and <see cref="Summary"/> says what changed.
/// </summary>
public partial class ImportDataSourcesWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly IReadOnlyList<PromptVariableDefinition> _currentPrompts;
    private readonly IReadOnlyList<DataTransformDefinition> _currentTransforms;
    private LabelTemplate? _source;

    public ImportDataSourcesWindow(
        MainViewModel viewModel,
        IReadOnlyList<PromptVariableDefinition> currentPrompts,
        IReadOnlyList<DataTransformDefinition> currentTransforms)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _currentPrompts = currentPrompts;
        _currentTransforms = currentTransforms;
        Prompts = currentPrompts;
        Transforms = currentTransforms;
    }

    public IReadOnlyList<PromptVariableDefinition> Prompts { get; private set; }

    public IReadOnlyList<DataTransformDefinition> Transforms { get; private set; }

    public string Summary { get; private set; } = string.Empty;

    private async void Browse_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Chọn tem để nhập nguồn dữ liệu",
            Filter = "Tem ANLAbel hoặc NiceLabel (*.anlabel;*.json;*.nlbl;*.lbl)|*.anlabel;*.json;*.nlbl;*.lbl|Tất cả tệp (*.*)|*.*"
        };
        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        Mouse.OverrideCursor = Cursors.Wait;
        try
        {
            _source = await _viewModel.ReadTemplateForImportAsync(dialog.FileName);
            SourcePathBox.Text = dialog.FileName;
            var prompts = _source.PromptVariables.Count;
            var functions = _source.DataTransforms.Count;
            SourceSummaryText.Text = prompts == 0 && functions == 0
                ? $"“{Path.GetFileName(dialog.FileName)}” không có biến nhập khi in hay hàm nào để nhập."
                : $"“{Path.GetFileName(dialog.FileName)}” có {prompts} biến nhập khi in và {functions} hàm.";
            OkButton.IsEnabled = prompts + functions > 0;
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException or NotSupportedException)
        {
            _source = null;
            OkButton.IsEnabled = false;
            var explained = TemplateFailureExplainer.Explain(ex);
            OperatorDialog.Ask(
                this,
                "Không đọc được tem",
                [$"Không đọc được “{Path.GetFileName(dialog.FileName)}”: {explained.Problem}", explained.NextAction],
                $"Chi tiết kỹ thuật: {explained.Technical}",
                OperatorDialogTone.Warning,
                new OperatorDialogChoice("ok", "Đóng", IsDefault: true, IsCancel: true));
        }
        finally
        {
            Mouse.OverrideCursor = null;
        }
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        if (_source is null)
        {
            return;
        }

        var overwrite = OverwriteRadio.IsChecked == true;
        var parts = new List<string>();
        if (ImportPromptsBox.IsChecked == true)
        {
            var prompts = DataDefinitionImport.MergePrompts(_currentPrompts, _source.PromptVariables.ToArray(), overwrite);
            Prompts = prompts.Items;
            parts.Add(Describe("biến nhập khi in", prompts.Added, prompts.Replaced, prompts.Kept));
        }

        if (ImportFunctionsBox.IsChecked == true)
        {
            var transforms = DataDefinitionImport.MergeTransforms(_currentTransforms, _source.DataTransforms.ToArray(), overwrite);
            Transforms = transforms.Items;
            parts.Add(Describe("hàm", transforms.Added, transforms.Replaced, transforms.Kept));
        }

        Summary = "Đã nhập " + string.Join("; ", parts) + ".";
        DialogResult = true;
    }

    private static string Describe(string what, int added, int replaced, int kept) =>
        $"{what}: thêm {added}, ghi đè {replaced}, giữ nguyên {kept}";
}
