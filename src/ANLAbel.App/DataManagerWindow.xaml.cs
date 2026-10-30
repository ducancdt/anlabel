using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ANLAbel.App.Services;
using ANLAbel.App.ViewModels;
using ANLAbel.Core.Data;
using ANLAbel.Core.Enums;
using ANLAbel.Core.Models;
using ANLAbel.Core.Printing;

namespace ANLAbel.App;

/// <summary>
/// Quản lý dữ liệu — NiceLabel 10's Dynamic Data Manager (docs/NICELABEL_10_UI_LAYOUT.md,
/// "Quản lý dữ liệu"). Prompt variables, functions and the serial counter are edited on a
/// draft; Đồng ý applies it to the label as one undo step (<see cref="MainViewModel.ApplyDataDefinitions"/>),
/// Hủy drops it. Database commands act at once, as on the Data tab.
/// </summary>
public partial class DataManagerWindow : Window
{
    private enum NodeKind
    {
        PromptCategory,
        Prompt,
        FunctionCategory,
        Function,
        Counter,
        Database,
        InternalCategory,
        DateTimeVariable,
        SerialVariable
    }

    private sealed record Node(NodeKind Kind, object? Item);

    /// <summary>A function on the draft; keeps unknown members of the definition it came from.</summary>
    private sealed class FunctionDraft : INotifyPropertyChanged
    {
        private string _name;
        private string _formula;

        public FunctionDraft(DataTransformDefinition? source, string name, string formula)
        {
            Source = source;
            _name = name;
            _formula = formula;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public DataTransformDefinition? Source { get; }

        public string Name
        {
            get => _name;
            set => Set(ref _name, (value ?? string.Empty).Trim());
        }

        public string Formula
        {
            get => _formula;
            set => Set(ref _formula, value ?? string.Empty);
        }

        public DataTransformDefinition ToDefinition() =>
            Source is null ? new DataTransformDefinition(Name, Formula) : Source with { Name = Name, Formula = Formula };

        private void Set(ref string field, string value, [CallerMemberName] string? property = null)
        {
            if (field != value)
            {
                field = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
            }
        }
    }

    private readonly MainViewModel _viewModel;
    private readonly ObservableCollection<PromptVariableDefinition> _prompts;
    private readonly ObservableCollection<FunctionDraft> _functions;
    private readonly SerialCounterDefinition _counter;
    private readonly Dictionary<object, TextBlock> _headers = new();
    private bool _changed;
    private bool _closingAccepted;

    public DataManagerWindow(MainViewModel viewModel, string? start = null)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _prompts = new ObservableCollection<PromptVariableDefinition>(viewModel.Template.PromptVariables.Select(prompt => prompt.Clone()));
        _functions = new ObservableCollection<FunctionDraft>(viewModel.Template.DataTransforms.Select(transform => new FunctionDraft(transform, transform.Name, transform.Formula)));
        _counter = viewModel.Template.SerialCounter.Clone();
        foreach (var prompt in _prompts)
        {
            prompt.PropertyChanged += Draft_PropertyChanged;
        }

        foreach (var function in _functions)
        {
            function.PropertyChanged += Draft_PropertyChanged;
        }

        _counter.PropertyChanged += Draft_PropertyChanged;
        CounterAlphabetBox.ItemsSource = viewModel.SerialCounterAlphabetOptions;
        Closing += DataManagerWindow_Closing;
        RebuildTree(start switch
        {
            "Counter" => _counter,
            "Database" => NodeKind.Database,
            "Functions" => NodeKind.FunctionCategory,
            _ => NodeKind.PromptCategory
        });
    }

    /// <summary>True after Đồng ý applied the draft to the label.</summary>
    public bool Accepted { get; private set; }

    // ---- tree ------------------------------------------------------------------------------

    private void RebuildTree(object? select)
    {
        _headers.Clear();
        DataTree.Items.Clear();
        var prompts = Item($"Biến nhập khi in ({_prompts.Count})", "Icon.Variable16", new Node(NodeKind.PromptCategory, null), bold: true);
        foreach (var prompt in _prompts)
        {
            prompts.Items.Add(Item(PromptHeader(prompt), "Icon.Variable16", new Node(NodeKind.Prompt, prompt), key: prompt));
        }

        var functions = Item($"Hàm ({_functions.Count})", "Icon.Function16", new Node(NodeKind.FunctionCategory, null), bold: true);
        foreach (var function in _functions)
        {
            functions.Items.Add(Item(FunctionHeader(function), "Icon.Function16", new Node(NodeKind.Function, function), key: function));
        }

        var counter = Item(CounterHeader(), "Icon.Counter16", new Node(NodeKind.Counter, _counter), bold: true, key: _counter);
        var database = Item(DatabaseHeader(), "Icon.Database16", new Node(NodeKind.Database, null), bold: true);
        var internals = Item("Biến nội bộ", "Icon.DateTime16", new Node(NodeKind.InternalCategory, null), bold: true);
        internals.Items.Add(Item("Ngày giờ in  {#NOW}", "Icon.DateTime16", new Node(NodeKind.DateTimeVariable, null)));
        internals.Items.Add(Item("Số Serial  {#SERIAL}", "Icon.Counter16", new Node(NodeKind.SerialVariable, null)));
        foreach (var item in new[] { prompts, functions, counter, database, internals })
        {
            item.IsExpanded = true;
            DataTree.Items.Add(item);
        }

        var target = FindItem(select) ?? prompts;
        target.IsSelected = true;
        target.BringIntoView();
    }

    private TreeViewItem? FindItem(object? select)
    {
        foreach (var root in DataTree.Items.OfType<TreeViewItem>())
        {
            foreach (var item in new[] { root }.Concat(root.Items.OfType<TreeViewItem>()))
            {
                if (item.Tag is Node node && (ReferenceEquals(node.Item, select) && select is not null || select is NodeKind kind && node.Kind == kind && node.Item is null))
                {
                    return item;
                }
            }
        }

        return null;
    }

    private TreeViewItem Item(string text, string iconKey, Node node, bool bold = false, object? key = null)
    {
        var label = new TextBlock { Text = text, VerticalAlignment = VerticalAlignment.Center, Foreground = new SolidColorBrush(Color.FromRgb(0x26, 0x26, 0x26)) };
        if (bold)
        {
            label.FontWeight = FontWeights.SemiBold;
        }

        if (key is not null)
        {
            _headers[key] = label;
        }

        var header = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 2) };
        header.Children.Add(new Image { Source = (ImageSource)FindResource(iconKey), Width = 16, Height = 16, Margin = new Thickness(0, 0, 6, 0) });
        header.Children.Add(label);
        var item = new TreeViewItem { Header = header, Tag = node };
        System.Windows.Automation.AutomationProperties.SetName(item, text);
        return item;
    }

    private static string PromptHeader(PromptVariableDefinition prompt) =>
        string.IsNullOrWhiteSpace(prompt.Name) ? "(chưa đặt tên)" : prompt.Name;

    private static string FunctionHeader(FunctionDraft function) =>
        string.IsNullOrWhiteSpace(function.Name) ? "(chưa đặt tên)" : function.Name;

    private string CounterHeader() => _counter.Enabled ? "Bộ đếm Serial" : "Bộ đếm Serial (tắt)";

    private string DatabaseHeader() => _viewModel.HasLinkedExcelSource
        ? "Cơ sở dữ liệu: " + System.IO.Path.GetFileName(_viewModel.Template.DatabaseConfig.FilePath)
        : "Cơ sở dữ liệu (chưa liên kết)";

    private Node? SelectedNode => (DataTree.SelectedItem as TreeViewItem)?.Tag as Node;

    private void DataTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e) => ShowEditor(SelectedNode);

    // ---- editors -----------------------------------------------------------------------------

    private void ShowEditor(Node? node)
    {
        foreach (var editor in new FrameworkElement[] { CategoryEditor, PromptEditor, FunctionEditor, CounterEditor, DatabaseEditor, InternalEditor })
        {
            editor.Visibility = Visibility.Collapsed;
        }

        DeleteButton.IsEnabled = node?.Kind is NodeKind.Prompt or NodeKind.Function || node?.Kind == NodeKind.Counter && _counter.Enabled;
        EditDatabaseButton.IsEnabled = _viewModel.HasLinkedExcelSource;
        switch (node?.Kind)
        {
            case NodeKind.Prompt:
                PromptEditor.DataContext = node.Item;
                PromptEditor.Visibility = Visibility.Visible;
                ShowPromptKind((PromptVariableDefinition)node.Item!);
                UpdatePromptUsage();
                break;
            case NodeKind.Function:
                FunctionEditor.DataContext = node.Item;
                FunctionEditor.Visibility = Visibility.Visible;
                UpdateFunctionResult();
                break;
            case NodeKind.Counter:
                CounterEditor.DataContext = _counter;
                CounterEditor.Visibility = Visibility.Visible;
                UpdateCounterStatus();
                break;
            case NodeKind.Database:
                DatabaseEditor.Visibility = Visibility.Visible;
                UpdateDatabaseSummary();
                break;
            case NodeKind.DateTimeVariable:
                ShowInternal(
                    "Ngày giờ in",
                    "Ngày giờ lúc in, ghi cùng lệnh in để in lại đúng ngày cũ. Thêm định dạng sau dấu hai chấm; cộng hoặc trừ thời gian bằng d (ngày), w (tuần), mo (tháng), y (năm), h (giờ).",
                    "{#NOW:dd/MM/yyyy}",
                    $"Bây giờ: {DateTime.Now:dd/MM/yyyy HH:mm}. Ví dụ hạn dùng 30 ngày: {{#NOW+30d:dd/MM/yyyy}} = {DateTime.Now.AddDays(30):dd/MM/yyyy}.");
                break;
            case NodeKind.SerialVariable:
                ShowInternal(
                    "Số Serial",
                    "Số tiếp theo của Bộ đếm Serial; mỗi tem in ra một số, không bao giờ dùng lại. Bật và đặt bộ đếm ở mục Bộ đếm Serial.",
                    "{#SERIAL}",
                    _viewModel.DescribeSerialCounter(_counter));
                break;
            default:
                ShowCategory(node?.Kind ?? NodeKind.PromptCategory);
                break;
        }
    }

    private void ShowCategory(NodeKind kind)
    {
        CategoryEditor.Visibility = Visibility.Visible;
        CategoryAddButton.Visibility = kind is NodeKind.PromptCategory or NodeKind.FunctionCategory ? Visibility.Visible : Visibility.Collapsed;
        CategoryAddButton.Tag = kind;
        (CategoryTitle.Text, CategoryHint.Text, CategoryAddButton.Content) = kind switch
        {
            NodeKind.FunctionCategory => (
                "Hàm",
                $"Trường tính từ dữ liệu khác trước khi xem trước và in, như hàm của NiceLabel. Tem có {_functions.Count} hàm.",
                (object)"Thêm hàm"),
            NodeKind.InternalCategory => (
                "Biến nội bộ",
                "Giá trị ANLAbel tự điền khi in: ngày giờ in và số Serial. Chọn một mục để xem cách viết.",
                (object)string.Empty),
            _ => (
                "Biến nhập khi in",
                $"Giá trị người in nhập mỗi lần in, hỏi theo thứ tự của danh sách này (Thứ tự nhắc). Tem có {_prompts.Count} biến.",
                (object)"Thêm biến nhập khi in")
        };
    }

    private void ShowInternal(string title, string hint, string token, string value)
    {
        InternalEditor.Visibility = Visibility.Visible;
        InternalTitle.Text = title;
        InternalHint.Text = hint;
        InternalTokenBox.Text = token;
        InternalValueText.Text = value;
    }

    private bool _showingPromptKind;

    private void ShowPromptKind(PromptVariableDefinition prompt)
    {
        _showingPromptKind = true;
        var isDate = JobVariableContract.IsDatePrompt(prompt);
        PromptKindDate.IsChecked = isDate;
        PromptKindText.IsChecked = !isDate;
        _showingPromptKind = false;
    }

    /// <summary>
    /// NiceLabel 10 variable data type. A date prints in its date format (dd/MM/yyyy
    /// to start with); back to text, the date formats are cleared.
    /// </summary>
    private void PromptKind_Checked(object sender, RoutedEventArgs e)
    {
        if (_showingPromptKind || PromptEditor.DataContext is not PromptVariableDefinition prompt)
        {
            return;
        }

        if (ReferenceEquals(sender, PromptKindDate))
        {
            if (prompt.DateFormat.Length == 0)
            {
                prompt.DateFormat = "dd/MM/yyyy";
            }
        }
        else
        {
            prompt.DateFormat = string.Empty;
            prompt.DateInputFormat = string.Empty;
        }
    }

    private void UpdatePromptUsage()
    {
        if (PromptEditor.DataContext is not PromptVariableDefinition prompt)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(prompt.Name))
        {
            PromptUsageText.Text = "Đặt tên cho biến; đối tượng dùng nó qua {?Tên}.";
            return;
        }

        var users = _viewModel.Template.Objects
            .Where(item => Mentions(item.BindingExpression, prompt.Name) || Mentions(item.VisibilityCondition, prompt.Name))
            .Select(item => item.Name)
            .ToArray();
        var original = _viewModel.Template.PromptVariables.Any(item => string.Equals(item.Name, prompt.Name, StringComparison.OrdinalIgnoreCase));
        PromptUsageText.Text = users.Length == 0
            ? $"Chưa đối tượng nào dùng {{?{prompt.Name}}}. Gõ {{?{prompt.Name}}} vào nguồn dữ liệu của một đối tượng để dùng biến này."
            : $"Dùng ở {users.Length} đối tượng: {string.Join(", ", users)}."
              + (original ? string.Empty : " Tên này mới: đối tượng đang dùng tên cũ sẽ báo lỗi liên kết cho tới khi sửa.");
    }

    private static bool Mentions(string? expression, string name) =>
        !string.IsNullOrEmpty(expression)
        && (expression.Contains("{?" + name + "}", StringComparison.OrdinalIgnoreCase)
            || expression.Contains("\"?" + name + "\"", StringComparison.OrdinalIgnoreCase));

    private void UpdateFunctionResult()
    {
        if (FunctionEditor.DataContext is not FunctionDraft draft)
        {
            return;
        }

        var fields = _viewModel.ExcelHeaders.ToArray();
        FunctionFieldsText.Text = fields.Length == 0
            ? "Tem chưa liên kết cơ sở dữ liệu: FIELD chỉ đọc được biến nhập khi in ({?Tên} viết là FIELD(\"?Tên\")) và hàm khác."
            : "Trường của cơ sở dữ liệu: " + string.Join(", ", fields) + ".";
        if (string.IsNullOrWhiteSpace(draft.Name) || string.IsNullOrWhiteSpace(draft.Formula))
        {
            FunctionResultText.Text = "Đặt tên và viết công thức để xem kết quả.";
            return;
        }

        var source = fields.Select(field => new KeyValuePair<string, string?>(
            field,
            _viewModel.PreviewRow is { } row && row.TryGetValue(field, out var value) ? value : string.Empty));
        var result = DataTransformPipeline.Evaluate(
            DataRecord.Create(source),
            _functions.Where(function => !string.IsNullOrWhiteSpace(function.Name)).Select(function => function.ToDefinition()).ToArray());
        FunctionResultText.Text = result.IsValid
            ? (result.Record.Values.TryGetValue(draft.Name, out var output) ? output ?? string.Empty : string.Empty) is { Length: > 0 } text ? text : "(rỗng)"
            : "Lỗi: " + string.Join(" ", result.Errors.Select(FormulaErrorText.Translate));
    }

    private void UpdateCounterStatus()
    {
        CustomAlphabetPanel.Visibility = _counter.Alphabet == SerialCounterAlphabet.Custom ? Visibility.Visible : Visibility.Collapsed;
        CounterStatusText.Text = _counter.CounterId != _viewModel.Template.SerialCounter.CounterId && _counter.Enabled
            ? $"Dãy số mới: bắt đầu từ {_counter.StartValue} khi bấm Đồng ý."
            : _viewModel.DescribeSerialCounter(_counter);
    }

    private void UpdateDatabaseSummary()
    {
        var config = _viewModel.Template.DatabaseConfig;
        if (!_viewModel.HasLinkedExcelSource)
        {
            DatabaseSummaryText.Text = "Tem chưa liên kết cơ sở dữ liệu. Dùng Excel hoặc Tệp văn bản ở ribbon để liên kết một tệp.";
            DatabaseFieldsText.Text = "—";
            return;
        }

        DatabaseSummaryText.Text = $"{_viewModel.LinkedExcelSourceText} {_viewModel.ExcelDataFreshnessText}".Trim();
        var fields = config.LabelFields.Select(field => field.Name).ToArray();
        DatabaseFieldsText.Text = fields.Length == 0 ? "Chưa chọn trường nào; Sửa cơ sở dữ liệu để chọn." : string.Join(", ", fields);
    }

    // ---- draft changes -------------------------------------------------------------------------

    private void Draft_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        _changed = true;
        if (sender is not null && _headers.TryGetValue(sender, out var header))
        {
            header.Text = sender switch
            {
                PromptVariableDefinition prompt => PromptHeader(prompt),
                FunctionDraft function => FunctionHeader(function),
                _ => CounterHeader()
            };
        }

        switch (sender)
        {
            case PromptVariableDefinition:
                UpdatePromptUsage();
                break;
            case FunctionDraft:
                UpdateFunctionResult();
                break;
            case SerialCounterDefinition:
                UpdateCounterStatus();
                DeleteButton.IsEnabled = SelectedNode?.Kind == NodeKind.Counter && _counter.Enabled;
                break;
        }
    }

    private void AddVariable_Click(object sender, RoutedEventArgs e) => AddVariable();

    private void AddFunction_Click(object sender, RoutedEventArgs e) => AddFunction();

    private void CategoryAdd_Click(object sender, RoutedEventArgs e)
    {
        if (CategoryAddButton.Tag is NodeKind.FunctionCategory)
        {
            AddFunction();
        }
        else
        {
            AddVariable();
        }
    }

    private void AddVariable()
    {
        var prompt = new PromptVariableDefinition { Name = UniqueName("Biến", _prompts.Select(item => item.Name)), Required = true };
        prompt.PropertyChanged += Draft_PropertyChanged;
        _prompts.Add(prompt);
        _changed = true;
        RebuildTree(prompt);
    }

    private void AddFunction()
    {
        var function = new FunctionDraft(null, UniqueName("Hàm", _functions.Select(item => item.Name)), string.Empty);
        function.PropertyChanged += Draft_PropertyChanged;
        _functions.Add(function);
        _changed = true;
        RebuildTree(function);
    }

    private static string UniqueName(string stem, IEnumerable<string> taken)
    {
        var names = taken.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var number = 1;
        while (names.Contains($"{stem}{number}"))
        {
            number++;
        }

        return $"{stem}{number}";
    }

    private void Counter_Click(object sender, RoutedEventArgs e)
    {
        // One counter per label, as {#SERIAL}: the button turns it on and shows it.
        if (!_counter.Enabled)
        {
            _counter.Enabled = true;
        }

        RebuildTree(_counter);
    }

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        switch (SelectedNode)
        {
            case { Kind: NodeKind.Prompt, Item: PromptVariableDefinition prompt }:
                prompt.PropertyChanged -= Draft_PropertyChanged;
                _prompts.Remove(prompt);
                _changed = true;
                RebuildTree(NodeKind.PromptCategory);
                break;
            case { Kind: NodeKind.Function, Item: FunctionDraft function }:
                function.PropertyChanged -= Draft_PropertyChanged;
                _functions.Remove(function);
                _changed = true;
                RebuildTree(NodeKind.FunctionCategory);
                break;
            case { Kind: NodeKind.Counter }:
                _counter.Enabled = false;
                RebuildTree(_counter);
                break;
        }
    }

    private void CounterReset_Click(object sender, RoutedEventArgs e)
    {
        var reset = OperatorDialog.Ask(
            this,
            "Bắt đầu lại dãy số",
            [$"Bộ đếm sẽ bắt đầu lại từ {_counter.StartValue} sau khi bấm Đồng ý.", "Số đã in vẫn nằm trong lịch sử in; nếu in lại cùng khoảng số, tem có thể trùng số với tem đã in."],
            null,
            OperatorDialogTone.Warning,
            new OperatorDialogChoice("reset", "Bắt đầu lại", IsDefault: false),
            new OperatorDialogChoice("cancel", "Giữ dãy số", IsDefault: true, IsCancel: true)) == "reset";
        if (reset)
        {
            _counter.Reset();
        }
    }

    // ---- prompt order and import -----------------------------------------------------------------

    private void PromptOrder_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new PromptOrderWindow(_prompts) { Owner = this };
        if (dialog.ShowDialog() != true)
        {
            return;
        }

        var ordered = dialog.Order.ToArray();
        if (!ordered.SequenceEqual(_prompts))
        {
            _prompts.Clear();
            foreach (var prompt in ordered)
            {
                _prompts.Add(prompt);
            }

            _changed = true;
        }

        RebuildTree(NodeKind.PromptCategory);
    }

    private void Import_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ImportDataSourcesWindow(_viewModel, _prompts.ToArray(), _functions.Select(function => function.ToDefinition()).ToArray()) { Owner = this };
        if (dialog.ShowDialog() != true)
        {
            return;
        }

        foreach (var prompt in _prompts)
        {
            prompt.PropertyChanged -= Draft_PropertyChanged;
        }

        _prompts.Clear();
        foreach (var prompt in dialog.Prompts)
        {
            prompt.PropertyChanged += Draft_PropertyChanged;
            _prompts.Add(prompt);
        }

        foreach (var function in _functions)
        {
            function.PropertyChanged -= Draft_PropertyChanged;
        }

        _functions.Clear();
        foreach (var transform in dialog.Transforms)
        {
            var function = new FunctionDraft(transform, transform.Name, transform.Formula);
            function.PropertyChanged += Draft_PropertyChanged;
            _functions.Add(function);
        }

        _changed = true;
        FooterText.Text = dialog.Summary + " Bấm Đồng ý để lưu vào tem.";
        RebuildTree(NodeKind.PromptCategory);
    }

    // ---- database (acts at once) ----------------------------------------------------------------

    private void Excel_Click(object sender, RoutedEventArgs e) => OpenDatabaseWindow(ExcelImportStart.BrowseWorkbook);

    private void TextFile_Click(object sender, RoutedEventArgs e) => OpenDatabaseWindow(ExcelImportStart.BrowseTextFile);

    private void EditDatabase_Click(object sender, RoutedEventArgs e) => OpenDatabaseWindow(ExcelImportStart.Review);

    private void OpenDatabaseWindow(ExcelImportStart start)
    {
        new ExcelImportWindow(_viewModel, start) { Owner = this }.ShowDialog();
        RebuildTree(NodeKind.Database);
    }

    private void ReloadDatabase_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel.RefreshExcelDataCommand.CanExecute(null))
        {
            _viewModel.RefreshExcelDataCommand.Execute(null);
        }

        UpdateDatabaseSummary();
    }

    private void UnlinkDatabase_Click(object sender, RoutedEventArgs e)
    {
        var confirmed = OperatorDialog.Ask(
            this,
            "Bỏ liên kết cơ sở dữ liệu",
            ["Bỏ liên kết cơ sở dữ liệu khỏi tem này?", "Các đối tượng vẫn giữ trường đã gắn nhưng hiện nội dung mẫu cho tới khi liên kết lại."],
            null,
            OperatorDialogTone.Warning,
            new OperatorDialogChoice("unlink", "Bỏ liên kết", IsDefault: true),
            new OperatorDialogChoice("cancel", "Giữ liên kết", IsCancel: true)) == "unlink";
        if (confirmed)
        {
            _viewModel.UnlinkExcel();
            RebuildTree(NodeKind.Database);
        }
    }

    // ---- OK / Cancel ---------------------------------------------------------------------------------

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        CommitFocusedText();
        var problems = new List<string>();
        CheckNames(_prompts.Select(prompt => prompt.Name), "Biến nhập khi in", problems);
        problems.AddRange(_prompts.SelectMany(JobVariableContract.ValidateDefinition).Select(problem => JobVariableText.Message(problem)));
        CheckNames(_functions.Select(function => function.Name), "Hàm", problems);
        var fields = _viewModel.ExcelHeaders.ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var clash in _functions.Where(function => fields.Contains(function.Name)))
        {
            problems.Add($"Hàm “{clash.Name}” trùng tên một trường của cơ sở dữ liệu; đặt tên khác.");
        }

        if (problems.Count > 0)
        {
            OperatorDialog.Ask(
                this,
                "Chưa lưu được",
                ["Sửa các điểm sau rồi bấm Đồng ý lại:", .. problems],
                null,
                OperatorDialogTone.Warning,
                new OperatorDialogChoice("ok", "Sửa lại", IsDefault: true, IsCancel: true));
            return;
        }

        var warnings = new List<string>();
        var definitions = _functions.Select(function => function.ToDefinition()).ToArray();
        if (definitions.Length > 0)
        {
            var check = DataTransformPipeline.Evaluate(
                DataRecord.Create(_viewModel.ExcelHeaders.Select(field => new KeyValuePair<string, string?>(field, string.Empty))),
                definitions);
            warnings.AddRange(check.Errors.Select(FormulaErrorText.Translate));
        }

        if (_counter.Enabled)
        {
            warnings.AddRange(SerialCounterContract.ValidateDefinition(_counter).Select(SerialCounterText.Message));
        }

        if (warnings.Count > 0 && OperatorDialog.Ask(
                this,
                "Còn lỗi khi in",
                ["Tem sẽ bị chặn in cho tới khi sửa:", .. warnings],
                null,
                OperatorDialogTone.Warning,
                new OperatorDialogChoice("fix", "Sửa lại", IsDefault: true, IsCancel: true),
                new OperatorDialogChoice("save", "Vẫn lưu")) != "save")
        {
            return;
        }

        _viewModel.ApplyDataDefinitions(_prompts.ToArray(), definitions, _counter);
        Accepted = true;
        _closingAccepted = true;
        Close();
    }

    private static void CheckNames(IEnumerable<string> names, string what, List<string> problems)
    {
        var list = names.ToArray();
        if (list.Any(string.IsNullOrWhiteSpace))
        {
            problems.Add($"{what}: có mục chưa đặt tên.");
        }

        foreach (var name in list.Where(name => name.IndexOfAny(['{', '}', '"']) >= 0))
        {
            problems.Add($"{what}: tên “{name}” không được chứa {{, }} hay dấu ngoặc kép.");
        }

        foreach (var duplicate in list.Where(name => !string.IsNullOrWhiteSpace(name))
                     .GroupBy(name => name, StringComparer.OrdinalIgnoreCase)
                     .Where(group => group.Count() > 1))
        {
            problems.Add($"{what}: tên “{duplicate.Key}” dùng {duplicate.Count()} lần.");
        }
    }

    /// <summary>A text box with UpdateSourceTrigger=LostFocus writes its value before OK reads the draft.</summary>
    private void CommitFocusedText()
    {
        if (System.Windows.Input.Keyboard.FocusedElement is TextBox box)
        {
            box.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
        }
    }

    private void DataManagerWindow_Closing(object? sender, CancelEventArgs e)
    {
        if (_closingAccepted || !_changed)
        {
            return;
        }

        var discard = OperatorDialog.Ask(
            this,
            "Bỏ các thay đổi",
            ["Đóng mà không lưu biến, hàm và bộ đếm vừa sửa?"],
            null,
            OperatorDialogTone.Normal,
            new OperatorDialogChoice("discard", "Bỏ thay đổi"),
            new OperatorDialogChoice("keep", "Tiếp tục sửa", IsDefault: true, IsCancel: true)) == "discard";
        e.Cancel = !discard;
    }
}
