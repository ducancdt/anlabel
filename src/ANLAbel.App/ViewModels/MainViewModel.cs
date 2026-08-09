using System.Collections.Specialized;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ANLAbel.Barcode.Options;
using ANLAbel.Barcode.Renderers;
using ANLAbel.Core.Barcode;
using ANLAbel.Core.Enums;
using ANLAbel.Core.Expressions;
using ANLAbel.Core.Expressions.Formulas;
using ANLAbel.Core.Geometry;
using ANLAbel.Core.Models;
using ANLAbel.Core.Mvvm;
using ANLAbel.Data;
using ANLAbel.Data.DataLogs;
using ANLAbel.Data.Excel;
using ANLAbel.Data.PrintLogs;
using ANLAbel.Project.SaveLoad;
using ANLAbel.Printing.PrinterProfiles;
using ANLAbel.Printing.RenderPipeline;

namespace ANLAbel.App.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private static readonly string[] PreferredIndustrialFonts =
    [
        "Arial",
        "Arial Narrow",
        "Bahnschrift",
        "Calibri",
        "Consolas",
        "Courier New",
        "Lucida Console",
        "Segoe UI Semibold",
        "Tahoma",
        "Verdana"
    ];

    private readonly IProjectFileService _projectFileService;
    private readonly ExcelDataService _excelDataService;
    private readonly PrintService _printService;
    private readonly PrinterDiscoveryService _printerDiscoveryService;
    private readonly PrintLogService _printLogService;
    private readonly DataOperationLogService _dataOperationLogService;
    private readonly PrintOperationLogService _printOperationLogService;
    private readonly DataSourceRegistry _dataSourceRegistry;
    private readonly IBarcodeRenderer _barcodeValidator = new ZxingBarcodeRenderer();
    private readonly QrCapacityTable _qrCapacityTable = new();
    private readonly HashSet<LabelObject> _applyingQrAutoSize = new();
    private readonly Stack<string> _undoStack = new();
    private readonly Stack<string> _redoStack = new();
    private System.Windows.Threading.DispatcherTimer? _debounceTimer;
    private bool _debounceActive;
    private string _pendingSnapshot = string.Empty;
    private LabelTemplate _template = CreateDefaultTemplate();
    private LabelObject? _selectedObject;
    private DataView? _excelDataView;
    private object? _selectedDataItem;
    private IReadOnlyDictionary<string, string>? _previewRow;
    private string? _selectedExcelField;
    private DatabaseField? _selectedAvailableDatabaseField;
    private DatabaseField? _selectedLabelDatabaseField;
    private FormulaBuilderPart? _selectedFormulaBuilderPart;
    private string _formulaBuilderText = string.Empty;
    private ObjectType? _drawingTool;
    private string _drawingCommandText = string.Empty;
    private double _zoom = 1.25;
    private int _printCopies = 1;
    private string _printCopiesField = string.Empty;
    private string _currentFilePath = string.Empty;
    private string _statusText = "Ready";
    private string _lastTemplateSnapshot = string.Empty;
    private bool _isRestoringHistory;
    private bool _syncingTemplatePrinterSize;
    private bool _isToolboxVisible = true;
    private bool _isPropertiesVisible = true;
    private BindingIssueSummary? _selectedBindingIssue;
    private bool _isExcelLinkBroken;
    private DateTime? _excelDataReadAtLocal;
    private DateTime? _excelDataSourceWriteTimeUtc;
    private FileSystemWatcher? _excelFileWatcher;
    private readonly object _excelStaleDebounceLock = new();
    private System.Threading.Timer? _excelStaleDebounceTimer;
    private bool _isExcelDataStale;

    private static readonly JsonSerializerOptions HistoryJsonOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new JsonStringEnumConverter() }
    };

    public MainViewModel()
        : this(new ProjectFileService(), new ExcelDataService(), new PrintService(), new PrinterDiscoveryService(), new PrintLogService(), new DataOperationLogService())
    {
    }

    public MainViewModel(IProjectFileService projectFileService, ExcelDataService excelDataService, PrintService printService, PrinterDiscoveryService printerDiscoveryService, PrintLogService printLogService, DataOperationLogService? dataOperationLogService = null, DataSourceRegistry? dataSourceRegistry = null, PrintOperationLogService? printOperationLogService = null)
    {
        _projectFileService = projectFileService;
        _excelDataService = excelDataService;
        _printService = printService;
        _printerDiscoveryService = printerDiscoveryService;
        _printLogService = printLogService;
        _printOperationLogService = printOperationLogService ?? new PrintOperationLogService();
        _dataOperationLogService = dataOperationLogService ?? new DataOperationLogService();
        _dataSourceRegistry = dataSourceRegistry ?? new DataSourceRegistry();
        _dataSourceRegistry.Load();
        DataSources = new ObservableCollection<DataSource>(_dataSourceRegistry.Sources);
        AddTextCommand = new RelayCommand(AddText);
        AddTextBoxCommand = new RelayCommand(AddTextBox);
        AddImageCommand = new RelayCommand(AddImage);
        ReplaceSelectedImageCommand = new RelayCommand(ReplaceSelectedImage, () => SelectedObject is not null && SelectedObject.Type == ObjectType.Image);
        AddExcelFieldCommand = new RelayCommand(parameter => AddExcelField(GetFieldName(parameter)), parameter => !string.IsNullOrWhiteSpace(GetFieldName(parameter)));
        BindSelectedAsExcelFieldCommand = new RelayCommand(parameter => BindSelectedAsExcelField(GetFieldName(parameter)), parameter => SelectedObject is not null && !string.IsNullOrWhiteSpace(GetFieldName(parameter)));
        ClearSelectedBindingCommand = new RelayCommand(ClearSelectedBinding, () => SelectedObject is not null);
        AddDatabaseFieldCommand = new RelayCommand(_ => AddDatabaseField(), _ => SelectedAvailableDatabaseField is not null);
        AddAllDatabaseFieldsCommand = new RelayCommand(AddAllDatabaseFields, () => AvailableDatabaseFields.Count > 0);
        RemoveDatabaseFieldCommand = new RelayCommand(_ => RemoveDatabaseField(), _ => SelectedLabelDatabaseField is not null);
        ClearDatabaseFieldsCommand = new RelayCommand(ClearDatabaseFields, () => LabelDatabaseFields.Count > 0);
        AddFormulaFieldPartCommand = new RelayCommand(parameter => AddFormulaFieldPart(parameter as DatabaseField), parameter => parameter is DatabaseField);
        AddFormulaTextPartCommand = new RelayCommand(AddFormulaTextPart, () => !string.IsNullOrEmpty(FormulaBuilderText));
        AddFormulaSeparatorPartCommand = new RelayCommand(parameter => AddFormulaTextPart(parameter?.ToString()), parameter => !string.IsNullOrEmpty(parameter?.ToString()));
        RemoveFormulaPartCommand = new RelayCommand(RemoveFormulaPart, () => SelectedFormulaBuilderPart is not null);
        ClearFormulaBuilderCommand = new RelayCommand(ClearFormulaBuilder, () => FormulaBuilderParts.Count > 0);
        ApplyFormulaBuilderCommand = new RelayCommand(ApplyFormulaBuilder, () => SelectedObject is not null && FormulaBuilderParts.Count > 0);
        AddRectangleCommand = new RelayCommand(() => StartDrawingTool(ObjectType.Rectangle));
        AddEllipseCommand = new RelayCommand(() => StartDrawingTool(ObjectType.Ellipse));
        AddLineCommand = new RelayCommand(() => StartDrawingTool(ObjectType.Line));
        AddBarcodeCommand = new RelayCommand(AddBarcode);
        AddCode128Command = new RelayCommand(AddBarcode);
        AddQrCodeCommand = new RelayCommand(AddQrCode);
        AddDataMatrixCommand = new RelayCommand(AddDataMatrix);
        DeleteSelectedCommand = new RelayCommand(DeleteSelected, () => SelectedObject is not null);
        BringToFrontCommand = new RelayCommand(BringToFront, () => SelectedObject is not null);
        SendToBackCommand = new RelayCommand(SendToBack, () => SelectedObject is not null);
        BringForwardCommand = new RelayCommand(BringForward, () => SelectedObject is not null);
        SendBackwardCommand = new RelayCommand(SendBackward, () => SelectedObject is not null);
        SetRotationCommand = new RelayCommand(parameter => SetRotation(parameter), _ => SelectedObject is not null);
        UndoCommand = new RelayCommand(Undo, () => _undoStack.Count > 0);
        RedoCommand = new RelayCommand(Redo, () => _redoStack.Count > 0);
        ZoomInCommand = new RelayCommand(() => Zoom = Math.Min(4, Zoom + 0.1));
        ZoomOutCommand = new RelayCommand(() => Zoom = Math.Max(0.25, Zoom - 0.1));
        NewTemplateCommand = new RelayCommand(parameter => NewTemplate(parameter as NewTemplateRequest));
        RefreshExcelDataCommand = new RelayCommand(async () => await RefreshExcelDataAsync(), CanRefreshExcelData);
        PrintCurrentRowCommand = new RelayCommand(PrintCurrentRow);
        PrintAllRowsCommand = new RelayCommand(PrintAllRows, () => ExcelDataView is not null && ExcelDataView.Count > 0);
        PrintCalibrationCommand = new RelayCommand(PrintCalibration);
        HideToolboxCommand = new RelayCommand(() => IsToolboxVisible = false);
        HidePropertiesCommand = new RelayCommand(() => IsPropertiesVisible = false);
        ShowAllPanelsCommand = new RelayCommand(ShowAllPanels);
        InsertFunctionFormulaCommand = new RelayCommand(parameter => InsertFunctionFormula(GetFormulaText(parameter)), _ => SelectedObject is not null);
        SelectBindingIssueCommand = new RelayCommand(parameter => SelectBindingIssue(parameter as BindingIssueSummary), parameter => parameter is BindingIssueSummary);
        RelinkExcelCommand = new RelayCommand(async () => await RelinkExcelAsync(), () => HasLinkedExcelSource && IsExcelLinkBroken);
        AddCurrentAsDataSourceCommand = new RelayCommand(AddCurrentAsDataSource, () => HasLinkedExcelSource && !IsExcelLinkBroken);
        UseDataSourceCommand = new RelayCommand(async parameter => { if (parameter is DataSource source) { await UseDataSourceAsync(source); } }, parameter => parameter is DataSource);
        RemoveDataSourceCommand = new RelayCommand(parameter => RemoveDataSource(parameter as DataSource), parameter => parameter is DataSource);
        RelinkDataSourceCommand = new RelayCommand(async parameter => { if (parameter is DataSource source) { await RelinkDataSourceAsync(source); } }, parameter => parameter is DataSource);
        ObserveTemplate(Template);
        _lastTemplateSnapshot = CaptureTemplateSnapshot();
    }

    public LabelTemplate Template
    {
        get => _template;
        private set
        {
            var oldTemplate = _template;
            if (SetProperty(ref _template, value))
            {
                UnobserveTemplate(oldTemplate);
                ObserveTemplate(value);
                _lastTemplateSnapshot = CaptureTemplateSnapshot();
                OnPropertyChanged(nameof(SelectedKeyFieldName));
                OnPropertyChanged(nameof(SelectedCopiesFieldName));
            }
        }
    }

    public LabelObject? SelectedObject
    {
        get => _selectedObject;
        set
        {
            if (SetProperty(ref _selectedObject, value))
            {
                ((RelayCommand)DeleteSelectedCommand).RaiseCanExecuteChanged();
                ((RelayCommand)BringToFrontCommand).RaiseCanExecuteChanged();
                ((RelayCommand)SendToBackCommand).RaiseCanExecuteChanged();
                ((RelayCommand)BringForwardCommand).RaiseCanExecuteChanged();
                ((RelayCommand)SendBackwardCommand).RaiseCanExecuteChanged();
                ((RelayCommand)BindSelectedAsExcelFieldCommand).RaiseCanExecuteChanged();
                ((RelayCommand)ClearSelectedBindingCommand).RaiseCanExecuteChanged();
                ((RelayCommand)InsertFunctionFormulaCommand).RaiseCanExecuteChanged();
                ((RelayCommand)BindSelectedAsExcelFieldCommand).RaiseCanExecuteChanged();
                ((RelayCommand)ApplyFormulaBuilderCommand).RaiseCanExecuteChanged();
                ((RelayCommand)ReplaceSelectedImageCommand).RaiseCanExecuteChanged();
                OnPropertyChanged(nameof(SelectedObjectTypeText));
                OnPropertyChanged(nameof(SelectedObjectSummaryText));
                RaiseFormulaPreviewChanged();
                OnPropertyChanged(nameof(BarcodeValidationMessage));
                OnPropertyChanged(nameof(TextBoxValidationMessage));
                OnPropertyChanged(nameof(BarcodeModuleSizeWarningText));
            }
        }
    }

    public DataView? ExcelDataView
    {
        get => _excelDataView;
        private set
        {
            if (SetProperty(ref _excelDataView, value))
            {
                OnPropertyChanged(nameof(HasExcelData));
                ((RelayCommand)PrintAllRowsCommand).RaiseCanExecuteChanged();
            }
        }
    }

    public bool HasExcelData => ExcelDataView is not null;
    public PrintService PrintService => _printService;
    public PrintLogService PrintLogService => _printLogService;
    public string PrintHistoryFilePath => _printLogService.LogFilePath;
    public ExcelDataService ExcelDataService => _excelDataService;

    public object? SelectedDataItem
    {
        get => _selectedDataItem;
        set
        {
            if (SetProperty(ref _selectedDataItem, value))
            {
                if (value is DataRowView rowView)
                {
                    Template.DatabaseConfig.LastSelectedRow = Math.Max(0, GetDataRowViewIndex(rowView));
                    if (!string.IsNullOrWhiteSpace(Template.DatabaseConfig.KeyField)
                        && rowView.Row.Table.Columns.Contains(Template.DatabaseConfig.KeyField))
                    {
                        Template.DatabaseConfig.KeyValue = rowView.Row[Template.DatabaseConfig.KeyField]?.ToString() ?? string.Empty;
                    }
                }

                PreviewRow = CreatePreviewRow(value);
                OnPropertyChanged(nameof(CurrentExcelRowText));
            }
        }
    }

    public IReadOnlyDictionary<string, string>? PreviewRow
    {
        get => _previewRow;
        private set
        {
            if (SetProperty(ref _previewRow, value))
            {
                RaiseFormulaPreviewChanged();
                OnPropertyChanged(nameof(TextBoxValidationMessage));
            }
        }
    }

    public ObservableCollection<string> ExcelHeaders { get; } = new();
    public ObservableCollection<DatabaseField> AvailableDatabaseFields => Template.DatabaseConfig.AvailableFields;
    public ObservableCollection<DatabaseField> LabelDatabaseFields => Template.DatabaseConfig.LabelFields;
    public ObservableCollection<FormulaBuilderPart> FormulaBuilderParts { get; } = new();
    public IReadOnlyList<BarcodeSymbology> BarcodeSymbologies { get; } = Enum.GetValues<BarcodeSymbology>();
    public IReadOnlyList<BarcodeSymbologyOption> BarcodeSymbologyOptions { get; } =
    [
        new(BarcodeSymbology.Code128, "1D Barcode", "Code 128"),
        new(BarcodeSymbology.Code39, "1D Barcode", "Code 39"),
        new(BarcodeSymbology.Code93, "1D Barcode", "Code 93"),
        new(BarcodeSymbology.Ean13, "1D Barcode", "EAN-13"),
        new(BarcodeSymbology.Ean8, "1D Barcode", "EAN-8"),
        new(BarcodeSymbology.UpcA, "1D Barcode", "UPC-A"),
        new(BarcodeSymbology.UpcE, "1D Barcode", "UPC-E"),
        new(BarcodeSymbology.ITF, "1D Barcode", "ITF"),
        new(BarcodeSymbology.Codabar, "1D Barcode", "Codabar"),
        new(BarcodeSymbology.MSI, "1D Barcode", "MSI"),
        new(BarcodeSymbology.Plessey, "1D Barcode", "Plessey"),
        new(BarcodeSymbology.QRCode, "2D QR / Matrix", "QR Code"),
        new(BarcodeSymbology.DataMatrix, "2D QR / Matrix", "Data Matrix"),
        new(BarcodeSymbology.Aztec, "2D QR / Matrix", "Aztec"),
        new(BarcodeSymbology.Pdf417, "2D QR / Matrix", "PDF417")
    ];
    public IReadOnlyList<string> FontFamilies { get; } = GetIndustrialFontFamilies();
    public IReadOnlyList<double> FontSizes { get; } = new double[] { 4, 5, 6, 7, 8, 9, 10, 11, 12, 14, 16, 18, 20, 24, 28, 32 };
    public IReadOnlyList<QrOptionItem<QrSizingMode>> QrSizingModeOptions { get; } = QrOptionLists.SizingModes;
    public IReadOnlyList<QrOptionItem<QrErrorCorrection>> QrErrorCorrectionOptions { get; } = QrOptionLists.ErrorCorrections;
    public IReadOnlyList<QrOptionItem<int>> QrVersionOptions { get; } = QrOptionLists.Versions;
    public IReadOnlyList<int> QrModuleSizePxOptions { get; } = QrOptionLists.ModuleSizesPx;
    public IReadOnlyList<int> QrQuietZoneModuleOptions { get; } = QrOptionLists.QuietZoneModules;
    public IReadOnlyList<TextAlignmentMode> TextAlignments { get; } = Enum.GetValues<TextAlignmentMode>();
    public IReadOnlyList<OutlineStyle> OutlineStyles { get; } = Enum.GetValues<OutlineStyle>();
    public IReadOnlyList<FillStyle> FillStyles { get; } = Enum.GetValues<FillStyle>();
    public IReadOnlyList<FormulaFunctionTemplate> FormulaFunctions { get; } =
    [
        new("QR/Barcode Production", "CONCAT(FIELD(\"part\"), FIELD(\"partname\"), FIELD(\"lot\"))", "Required fields: part + partname + lot. Output example: P001Product AL01."),
        new("QR/Barcode Production - labeled", "CONCAT(\"Part: \", FIELD(\"part\"), \" | Name: \", FIELD(\"partname\"), \" | Lot: \", FIELD(\"lot\"))", "Readable output with labels and separators."),
        new("FIELD", "FIELD(\"Name\")", "Get value from one Excel column."),
        new("CONCAT", "CONCAT(\"Tên: \", FIELD(\"Name\"), \", Mã: \", FIELD(\"Code\"))", "Join fixed text and fields.")
    ];

    public string FormulaPreviewValue => EvaluateSelectedFormula().Value;
    public string FormulaPreviewErrors => string.Join(Environment.NewLine, EvaluateSelectedFormula().Errors);
    public string FormulaPreviewUsedFields => string.Join(", ", EvaluateSelectedFormula().UsedFields);
    public string FormulaBuilderExpression => BuildFormulaExpression();
    public string FormulaBuilderPreviewValue => EvaluateFormulaBuilder().Value;
    public string FormulaBuilderPreviewErrors => string.Join(Environment.NewLine, EvaluateFormulaBuilder().Errors);
    public string BarcodeValidationMessage => ValidateSelectedBarcode();

    /// <summary>
    /// Warns when a fixed-size matrix barcode's module would print at less than ~2
    /// physical dots on this label's configured print DPI (properties-panel-plan Đợt C /
    /// print-preview-reliability-plan R5) — modules that small are unreliable to scan on
    /// industrial thermal printers. Only meaningful for
    /// <see cref="QrSizingMode.FixedVersionAndModuleSize"/>, where the module size in
    /// pixels is an explicit design choice rather than computed to fit the label.
    /// </summary>
    public string BarcodeModuleSizeWarningText
    {
        get
        {
            if (SelectedObject is not { } item || !IsSquare2DCodeLike(item) || item.QrSizingMode != QrSizingMode.FixedVersionAndModuleSize)
            {
                return string.Empty;
            }

            // Match PrintService.CreatePlan's DPI resolution (PrinterProfile.Dpi first,
            // then Template.Dpi) so this Designer-side warning agrees with the DPI the
            // preflight check (PrintPreflightValidator.ValidateBarcodeModuleSizeAtPrintDpi)
            // will actually enforce at print time. Using Template.Dpi alone would disagree
            // once PrinterProfile.Dpi is set independently (e.g. via the "Label printer
            // setup..." dialog in Print Preview, which only updates PrinterProfile.Dpi).
            var printDpi = Template.PrinterProfile.Dpi > 0 ? Template.PrinterProfile.Dpi : Template.Dpi > 0 ? Template.Dpi : item.QrDpi;
            var effectiveDots = item.QrModuleSizePx * (double)printDpi / item.QrDpi;
            return effectiveDots < 2
                ? $"⚠ Module is only ~{effectiveDots:0.#} dot(s) at {printDpi} DPI — likely to fail scanning. Increase Module px or DPI."
                : string.Empty;
        }
    }
    public string TextBoxValidationMessage => ValidateSelectedTextBox();
    public string SelectedObjectTypeText => SelectedObject?.Type switch
    {
        ObjectType.Text => "Text",
        ObjectType.TextBox => "Text Box",
        ObjectType.BarcodeCode128 => "Barcode",
        ObjectType.QRCode => "QR Code",
        ObjectType.DataMatrix => "Data Matrix",
        ObjectType.Rectangle => "Rectangle",
        ObjectType.Ellipse => "Ellipse",
        ObjectType.Line => "Line",
        _ => "Object"
    };
    public string SelectedObjectSummaryText => SelectedObject is null
        ? string.Empty
        : $"{SelectedObject.Name} | {SelectedObjectTypeText} | {SelectedObject.WidthMm:0.##} x {SelectedObject.HeightMm:0.##} mm";
    public bool HasLinkedExcelSource => !string.IsNullOrWhiteSpace(Template.DatabaseConfig.FilePath) && !string.IsNullOrWhiteSpace(Template.DatabaseConfig.SheetName);
    public bool IsExcelLinkBroken
    {
        get => _isExcelLinkBroken;
        private set
        {
            if (SetProperty(ref _isExcelLinkBroken, value))
            {
                OnPropertyChanged(nameof(ExcelLinkStatusText));
                ((RelayCommand)RelinkExcelCommand).RaiseCanExecuteChanged();
            }
        }
    }

    public string LinkedExcelSourceText => HasLinkedExcelSource
        ? $"{Path.GetFileName(Template.DatabaseConfig.FilePath)} / {Template.DatabaseConfig.SheetName}"
        : "No Excel file linked";
    public string ExcelLinkStatusText => IsExcelLinkBroken
        ? "⚠ Excel link broken — click Relink to locate the file"
        : string.Empty;
    public string ExcelDataFreshnessText => HasLinkedExcelSource && !IsExcelLinkBroken && _excelDataReadAtLocal is not null
        ? $"Data read at {_excelDataReadAtLocal.Value:HH:mm:ss}"
        : string.Empty;
    public string CurrentExcelRowText => ExcelDataView is null || ExcelDataView.Count == 0 || SelectedDataItem is not DataRowView rowView
        ? "No Excel row selected"
        : $"Row {GetDataRowViewIndex(rowView) + 1} of {ExcelDataView.Count}";

    /// <summary>
    /// True when the linked Excel file changed on disk since the last import/refresh
    /// (database-plan GĐ2 item 5). The app never reloads automatically — an in-progress
    /// design/print session must not have its data swapped out from under it.
    /// </summary>
    public bool IsExcelDataStale
    {
        get => _isExcelDataStale;
        private set
        {
            if (SetProperty(ref _isExcelDataStale, value))
            {
                OnPropertyChanged(nameof(ExcelStaleNoticeText));
            }
        }
    }

    public string ExcelStaleNoticeText => IsExcelDataStale
        ? "⚠ Excel file changed on disk — click Update Excel to refresh"
        : string.Empty;

    /// <summary>
    /// Column names the user can pick as the row-tracking key (database-plan TC5).
    /// The empty first entry lets the user clear the key back to index-based tracking.
    /// </summary>
    public IReadOnlyList<string> KeyFieldOptions => new[] { string.Empty }.Concat(ExcelHeaders).ToArray();

    public string SelectedKeyFieldName
    {
        get => Template.DatabaseConfig.KeyField;
        set
        {
            var normalized = value ?? string.Empty;
            if (Template.DatabaseConfig.KeyField == normalized)
            {
                return;
            }

            Template.DatabaseConfig.KeyField = normalized;
            Template.DatabaseConfig.KeyValue = TryGetCurrentRowValue(normalized);
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Optional Excel column that sets how many copies to print per row in Print Preview
    /// (database-manager-module-plan.md M4, NiceLabel-style "label copies per record").
    /// Reuses <see cref="KeyFieldOptions"/> for the picker — same "empty first entry clears
    /// it" pattern as <see cref="SelectedKeyFieldName"/>.
    /// </summary>
    public string SelectedCopiesFieldName
    {
        get => Template.DatabaseConfig.CopiesField;
        set
        {
            var normalized = value ?? string.Empty;
            if (Template.DatabaseConfig.CopiesField == normalized)
            {
                return;
            }

            Template.DatabaseConfig.CopiesField = normalized;
            OnPropertyChanged();
        }
    }

    private string TryGetCurrentRowValue(string fieldName)
    {
        if (string.IsNullOrWhiteSpace(fieldName) || SelectedDataItem is not DataRowView rowView || !rowView.Row.Table.Columns.Contains(fieldName))
        {
            return string.Empty;
        }

        return rowView.Row[fieldName]?.ToString() ?? string.Empty;
    }
    public bool HasSelectedBinding => SelectedObject is not null && !string.IsNullOrWhiteSpace(SelectedObject.BindingExpression);
    public bool IsSelectedBindingFormula => SelectedObject is not null && FormulaBindingEvaluator.LooksLikeFormula(SelectedObject.BindingExpression);
    public string SelectedBindingKindText => EvaluateSelectedBinding().KindText;
    public string SelectedBindingPreviewValue => EvaluateSelectedBinding().PreviewValue;
    public string SelectedBindingUsedFieldsText => string.Join(", ", EvaluateSelectedBinding().UsedFields);
    public string SelectedBindingMissingFieldsText => string.Join(", ", EvaluateSelectedBinding().MissingFields);
    public string SelectedBindingUsedFieldsSummary => string.IsNullOrWhiteSpace(SelectedBindingUsedFieldsText) ? string.Empty : $"Linked fields: {SelectedBindingUsedFieldsText}";
    public string SelectedBindingMissingFieldsSummary => string.IsNullOrWhiteSpace(SelectedBindingMissingFieldsText) ? string.Empty : $"Missing fields: {SelectedBindingMissingFieldsText}";
    public string SelectedBindingStatusText => EvaluateSelectedBinding().StatusText;
    public string SelectedBindingErrorsText => string.Join(Environment.NewLine, EvaluateSelectedBinding().Errors);
    public IReadOnlyList<BindingIssueSummary> BindingIssues => GetBindingIssues();
    public bool HasBindingIssues => BindingIssues.Count > 0;
    public string BindingIssuesSummary => HasBindingIssues
        ? $"{BindingIssues.Count} object(s) have broken or incomplete Excel bindings."
        : "All Excel bindings match the current workbook.";
    public IReadOnlyList<string> FormulaSeparators { get; } = [" - ", " | ", " / ", "_", " ", ": "];

    public ObjectType? DrawingTool
    {
        get => _drawingTool;
        set
        {
            if (SetProperty(ref _drawingTool, value))
            {
                OnPropertyChanged(nameof(HasDrawingTool));
            }
        }
    }

    public bool HasDrawingTool => DrawingTool is not null;

    public string DrawingCommandText
    {
        get => _drawingCommandText;
        set => SetProperty(ref _drawingCommandText, value);
    }

    public string? SelectedExcelField
    {
        get => _selectedExcelField;
        set
        {
            if (SetProperty(ref _selectedExcelField, value))
            {
                ((RelayCommand)AddExcelFieldCommand).RaiseCanExecuteChanged();
                ((RelayCommand)BindSelectedAsExcelFieldCommand).RaiseCanExecuteChanged();
            }
        }
    }

    public DatabaseField? SelectedAvailableDatabaseField
    {
        get => _selectedAvailableDatabaseField;
        set
        {
            if (SetProperty(ref _selectedAvailableDatabaseField, value))
            {
                ((RelayCommand)AddDatabaseFieldCommand).RaiseCanExecuteChanged();
            }
        }
    }

    public DatabaseField? SelectedLabelDatabaseField
    {
        get => _selectedLabelDatabaseField;
        set
        {
            if (SetProperty(ref _selectedLabelDatabaseField, value))
            {
                SelectedExcelField = value?.Name;
                ((RelayCommand)RemoveDatabaseFieldCommand).RaiseCanExecuteChanged();
            }
        }
    }

    public FormulaBuilderPart? SelectedFormulaBuilderPart
    {
        get => _selectedFormulaBuilderPart;
        set
        {
            if (SetProperty(ref _selectedFormulaBuilderPart, value))
            {
                ((RelayCommand)RemoveFormulaPartCommand).RaiseCanExecuteChanged();
            }
        }
    }

    public string FormulaBuilderText
    {
        get => _formulaBuilderText;
        set
        {
            if (SetProperty(ref _formulaBuilderText, value))
            {
                ((RelayCommand)AddFormulaTextPartCommand).RaiseCanExecuteChanged();
            }
        }
    }

    public double Zoom
    {
        get => _zoom;
        set
        {
            if (SetProperty(ref _zoom, Math.Round(value, 2)))
            {
                OnPropertyChanged(nameof(ZoomPercent));
            }
        }
    }

    public int ZoomPercent => (int)Math.Round(Zoom * 100);

    public int PrintCopies
    {
        get => _printCopies;
        set => SetProperty(ref _printCopies, Math.Max(1, value));
    }

    public string PrintCopiesField
    {
        get => _printCopiesField;
        set => SetProperty(ref _printCopiesField, value);
    }

    public string CurrentFilePath
    {
        get => _currentFilePath;
        private set => SetProperty(ref _currentFilePath, value);
    }

    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    public bool IsToolboxVisible
    {
        get => _isToolboxVisible;
        set => SetProperty(ref _isToolboxVisible, value);
    }

    public bool IsPropertiesVisible
    {
        get => _isPropertiesVisible;
        set => SetProperty(ref _isPropertiesVisible, value);
    }

    public BindingIssueSummary? SelectedBindingIssue
    {
        get => _selectedBindingIssue;
        set => SetProperty(ref _selectedBindingIssue, value);
    }

    public ICommand AddTextCommand { get; }
    public ICommand AddImageCommand { get; }
    public ICommand ReplaceSelectedImageCommand { get; }
    public ICommand AddTextBoxCommand { get; }
    public ICommand AddExcelFieldCommand { get; }
    public ICommand BindSelectedAsExcelFieldCommand { get; }
    public ICommand ClearSelectedBindingCommand { get; }
    public ICommand AddDatabaseFieldCommand { get; }
    public ICommand AddAllDatabaseFieldsCommand { get; }
    public ICommand RemoveDatabaseFieldCommand { get; }
    public ICommand ClearDatabaseFieldsCommand { get; }
    public ICommand AddFormulaFieldPartCommand { get; }
    public ICommand AddFormulaTextPartCommand { get; }
    public ICommand AddFormulaSeparatorPartCommand { get; }
    public ICommand RemoveFormulaPartCommand { get; }
    public ICommand ClearFormulaBuilderCommand { get; }
    public ICommand ApplyFormulaBuilderCommand { get; }
    public ICommand AddRectangleCommand { get; }
    public ICommand AddEllipseCommand { get; }
    public ICommand AddLineCommand { get; }
    public ICommand AddBarcodeCommand { get; }
    public ICommand AddCode128Command { get; }
    public ICommand AddQrCodeCommand { get; }
    public ICommand AddDataMatrixCommand { get; }
    public ICommand DeleteSelectedCommand { get; }
    public ICommand BringToFrontCommand { get; }
    public ICommand SendToBackCommand { get; }
    public ICommand BringForwardCommand { get; }
    public ICommand SendBackwardCommand { get; }
    public ICommand SetRotationCommand { get; }
    public ICommand UndoCommand { get; }
    public ICommand RedoCommand { get; }
    public ICommand ZoomInCommand { get; }
    public ICommand ZoomOutCommand { get; }
    public ICommand NewTemplateCommand { get; }
    public ICommand RefreshExcelDataCommand { get; }
    public ICommand PrintCurrentRowCommand { get; }
    public ICommand PrintAllRowsCommand { get; }
    public ICommand PrintCalibrationCommand { get; }
    public ICommand HideToolboxCommand { get; }
    public ICommand HidePropertiesCommand { get; }
    public ICommand ShowAllPanelsCommand { get; }
    public ICommand InsertFunctionFormulaCommand { get; }
    public ICommand SelectBindingIssueCommand { get; }
    public ICommand RelinkExcelCommand { get; }
    public ICommand AddCurrentAsDataSourceCommand { get; }
    public ICommand UseDataSourceCommand { get; }
    public ICommand RemoveDataSourceCommand { get; }
    public ICommand RelinkDataSourceCommand { get; }

    /// <summary>
    /// Shared Excel data sources (database-plan GĐ2 item 4), stored machine-wide at
    /// <c>%AppData%\ANLAbel\data-sources.json</c>. Templates reference an entry by
    /// <see cref="DataSource.Id"/> via <see cref="DatabaseConfig.DataSourceId"/> so that
    /// relinking the shared file once fixes every template that uses it.
    /// </summary>
    public ObservableCollection<DataSource> DataSources { get; }

    private void ShowAllPanels()
    {
        IsToolboxVisible = true;
        IsPropertiesVisible = true;
        StatusText = "Workspace panels restored";
    }

    public void NewTemplate(NewTemplateRequest? request)
    {
        request ??= new NewTemplateRequest("Untitled Label", 100, 50, 203);
        StopWatchingExcelFile();
        IsExcelDataStale = false;
        UnobserveTemplate(Template);
        Template = new LabelTemplate
        {
            Name = request.Name,
            WidthMm = request.WidthMm,
            HeightMm = request.HeightMm,
            Dpi = request.Dpi,
            Orientation = LabelGeometry.ResolveOrientation(request.WidthMm, request.HeightMm),
            PrinterProfile = new PrinterProfile
            {
                LabelWidthMm = request.WidthMm,
                LabelHeightMm = request.HeightMm,
                Dpi = request.Dpi
            }
        };
        ResetHistory();
        SelectedObject = null;
        CurrentFilePath = string.Empty;
        StatusText = $"New template: {Template.WidthMm} x {Template.HeightMm} mm";
    }

    public async Task SaveAsync(string filePath)
    {
        UpdateRelativePath(filePath);
        await _projectFileService.SaveAsync(Template, filePath);
        CurrentFilePath = filePath;
        StatusText = $"Saved: {Path.GetFileName(filePath)}";
    }

    public async Task OpenAsync(string filePath)
    {
        UnobserveTemplate(Template);
        Template = await _projectFileService.LoadAsync(filePath);
        ResetHistory();
        CurrentFilePath = filePath;
        SelectedObject = Template.Objects.OrderByDescending(item => item.ZIndex).FirstOrDefault();
        await RestoreLinkedExcelDataAsync();
    }

    /// <summary>
    /// Loads a template picked from the built-in Template Library. The template is a
    /// fresh in-memory copy (not a file), so the current file path is cleared and the
    /// user is prompted for a destination on first Save.
    /// </summary>
    public async Task LoadTemplateFromLibraryAsync(LabelTemplate template)
    {
        ArgumentNullException.ThrowIfNull(template);
        UnobserveTemplate(Template);
        Template = template;
        ResetHistory();
        CurrentFilePath = string.Empty;
        SelectedObject = Template.Objects.OrderByDescending(item => item.ZIndex).FirstOrDefault();
        await RestoreLinkedExcelDataAsync();
        StatusText = $"Đã mở mẫu từ thư viện: {template.Name}";
    }

    public Task<IReadOnlyList<string>> GetExcelSheetNamesAsync(string filePath, CancellationToken cancellationToken = default)
    {
        return _excelDataService.GetSheetNamesAsync(filePath, cancellationToken);
    }

    public IReadOnlyList<PrinterInfo> GetInstalledPrinters()
    {
        return _printerDiscoveryService.GetInstalledPrinters();
    }

    public void ApplyPrinterSelection(PrinterInfo printer, PrinterPaperInfo paper, int dpi, LabelOrientation orientation)
    {
        // OrientSize swaps dimensions for Landscape so the design canvas shows
        // the label in landscape view (like NiceLabel). The physical paper dimensions
        // are sent to the printer driver via PageMediaSize — no PageOrientation is set,
        // so the driver prints content on the exact physical dimensions without rotation.
        var (widthMm, heightMm) = LabelGeometry.OrientSize(paper.WidthMm, paper.HeightMm, orientation);
        Template.WidthMm = widthMm;
        Template.HeightMm = heightMm;
        Template.Dpi = dpi;
        Template.Orientation = orientation;
        Template.PrinterProfile.PrinterName = printer.Name;
        Template.PrinterProfile.PaperName = paper.Name;
        Template.PrinterProfile.SettingsSource = PrinterSettingsSource.Label;
        Template.PrinterProfile.PaperSizeSource = PaperSizeSource.DriverAutomatic;
        Template.PrinterProfile.LabelWidthMm = widthMm;
        Template.PrinterProfile.LabelHeightMm = heightMm;
        // Store original physical dimensions (before orient swap) for the printer driver PageMediaSize
        Template.PrinterProfile.PhysicalWidthMm = paper.WidthMm;
        Template.PrinterProfile.PhysicalHeightMm = paper.HeightMm;
        Template.PrinterProfile.Dpi = dpi;
        Template.PrinterProfile.ScaleX = Template.PrinterProfile.ScaleX == 0 ? 1 : Template.PrinterProfile.ScaleX;
        Template.PrinterProfile.ScaleY = Template.PrinterProfile.ScaleY == 0 ? 1 : Template.PrinterProfile.ScaleY;
        StatusText = $"Printer: {printer.Name}, paper: {paper.Name} ({widthMm:0.##} x {heightMm:0.##} mm), orientation: {orientation}";
    }

    public async Task ImportExcelAsync(string filePath, string sheetName, CancellationToken cancellationToken = default)
    {
        await ImportExcelAsync(filePath, sheetName, "Import", cancellationToken);
    }

    private async Task ImportExcelAsync(string filePath, string sheetName, string operation, CancellationToken cancellationToken = default)
    {
        DataTable table;
        try
        {
            table = await _excelDataService.LoadSheetAsync(filePath, sheetName, Template.DatabaseConfig.HeaderRowIndex, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogDataOperation(operation, filePath, sheetName, rowCount: 0, columnCount: 0, success: false, ex.Message);
            throw;
        }

        ExcelDataView = table.DefaultView;
        ExcelHeaders.Clear();
        foreach (DataColumn column in table.Columns)
        {
            ExcelHeaders.Add(column.ColumnName);
        }

        Template.DatabaseConfig.FilePath = filePath;
        Template.DatabaseConfig.SheetName = sheetName;
        // Deliberately NOT resetting HeaderRowIndex here (bug fixed 2026-07-03): the read
        // above already succeeded using Template.DatabaseConfig.HeaderRowIndex, so that
        // value is exactly what correctly describes this data. Forcing it back to 1 used
        // to be harmless when every source's header was always row 1, but now that shared
        // sources can have a different header row (Database Manager M2), clobbering it here
        // silently broke every subsequent Refresh/Open for that source — it would try to
        // re-read with header row 1 even though the file's real header is elsewhere.

        // Update RelativePath if template has been saved
        if (!string.IsNullOrWhiteSpace(CurrentFilePath))
        {
            UpdateRelativePath(CurrentFilePath);
        }

        SyncDatabaseFieldsFromTable(table);
        if (ExcelDataView.Count > 0)
        {
            var targetRowIndex = FindRowIndexByKeyField(table) ?? Math.Max(0, Math.Min(ExcelDataView.Count - 1, Template.DatabaseConfig.LastSelectedRow));
            SelectedDataItem = ExcelDataView[targetRowIndex];
        }
        else
        {
            SelectedDataItem = null;
            Template.DatabaseConfig.LastSelectedRow = 0;
        }
        OnPropertyChanged(nameof(HasLinkedExcelSource));
        OnPropertyChanged(nameof(LinkedExcelSourceText));
        OnPropertyChanged(nameof(CurrentExcelRowText));
        OnPropertyChanged(nameof(KeyFieldOptions));
        OnPropertyChanged(nameof(SelectedKeyFieldName));
        OnPropertyChanged(nameof(SelectedCopiesFieldName));

        _excelDataReadAtLocal = DateTime.Now;
        _excelDataSourceWriteTimeUtc = TryGetFileWriteTimeUtc(filePath);
        OnPropertyChanged(nameof(ExcelDataFreshnessText));
        IsExcelDataStale = false;
        StartWatchingExcelFile(filePath);

        var issueCount = GetBindingIssues().Count;
        var issueSuffix = issueCount > 0 ? $" — {issueCount} object(s) have missing/broken bindings" : string.Empty;
        StatusText = $"Imported {table.Rows.Count} rows from {Path.GetFileName(filePath)} / {sheetName}{issueSuffix}";
        LogDataOperation(operation, filePath, sheetName, table.Rows.Count, table.Columns.Count, success: true, errorMessage: string.Empty);
    }

    /// <summary>
    /// Fire-and-forget append to the local data-operation log (database-plan.md TC6).
    /// Never awaited and never allowed to throw — a logging failure must not affect
    /// the data operation that triggered it.
    /// </summary>
    private void LogDataOperation(string operation, string excelFilePath, string sheetName, int rowCount, int columnCount, bool success, string errorMessage)
    {
        var entry = new DataOperationLogEntry
        {
            Operation = operation,
            TemplateFilePath = CurrentFilePath,
            ExcelFilePath = excelFilePath,
            SheetName = sheetName,
            RowCount = rowCount,
            ColumnCount = columnCount,
            Success = success,
            ErrorMessage = errorMessage
        };
        _ = _dataOperationLogService.AppendAsync(entry);
    }

    public async Task RefreshExcelDataAsync()
    {
        if (!CanRefreshExcelData())
        {
            StatusText = "No Excel database is linked yet";
            return;
        }

        var currentWriteTimeUtc = TryGetFileWriteTimeUtc(Template.DatabaseConfig.FilePath);
        if (currentWriteTimeUtc is not null && currentWriteTimeUtc == _excelDataSourceWriteTimeUtc)
        {
            StatusText = $"Excel data already up to date ({ExcelDataFreshnessText}) — file has not changed since last read";
            return;
        }

        await ImportExcelAsync(Template.DatabaseConfig.FilePath, Template.DatabaseConfig.SheetName, "Refresh");
    }

    /// <summary>
    /// Reads the file's last-write time for freshness/cache comparisons. Returns null
    /// if the file is missing or inaccessible rather than throwing — this is a best-effort
    /// metadata check, not a critical read path.
    /// </summary>
    private static DateTime? TryGetFileWriteTimeUtc(string filePath)
    {
        try
        {
            return File.Exists(filePath) ? File.GetLastWriteTimeUtc(filePath) : null;
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }

    /// <summary>
    /// (Re)starts a <see cref="FileSystemWatcher"/> on the linked Excel file so the user
    /// gets a "data changed" notice without the app silently reloading data mid-session
    /// (database-plan GĐ2 item 5). Failures to watch (e.g. unreachable network share)
    /// are swallowed — this is a convenience notice, not a critical path.
    /// </summary>
    private void StartWatchingExcelFile(string filePath)
    {
        StopWatchingExcelFile();

        try
        {
            var directory = Path.GetDirectoryName(filePath);
            var fileName = Path.GetFileName(filePath);
            if (string.IsNullOrWhiteSpace(directory) || string.IsNullOrWhiteSpace(fileName) || !Directory.Exists(directory))
            {
                return;
            }

            var watcher = new FileSystemWatcher(directory, fileName)
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size,
                EnableRaisingEvents = true
            };
            watcher.Changed += OnLinkedExcelFileChanged;
            watcher.Renamed += OnLinkedExcelFileChanged;
            _excelFileWatcher = watcher;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            // Watching is a convenience notice; if the share is unreachable, the
            // existing Cancel/timeout/relink flows already cover the hard failure.
        }
    }

    private void StopWatchingExcelFile()
    {
        if (_excelFileWatcher is null)
        {
            return;
        }

        _excelFileWatcher.EnableRaisingEvents = false;
        _excelFileWatcher.Changed -= OnLinkedExcelFileChanged;
        _excelFileWatcher.Renamed -= OnLinkedExcelFileChanged;
        _excelFileWatcher.Dispose();
        _excelFileWatcher = null;
        lock (_excelStaleDebounceLock)
        {
            _excelStaleDebounceTimer?.Dispose();
            _excelStaleDebounceTimer = null;
        }
    }

    private void OnLinkedExcelFileChanged(object sender, FileSystemEventArgs e)
    {
        // A single Excel save can raise several Changed/Renamed events in quick
        // succession, and FileSystemWatcher can deliver them on more than one
        // thread-pool thread concurrently — lock the dispose+replace so two overlapping
        // events cannot each create a Timer that overwrites the other's field reference
        // (which would leave one Timer orphaned: unreferenced, silently GC-eligible, or
        // firing an extra redundant check).
        lock (_excelStaleDebounceLock)
        {
            _excelStaleDebounceTimer?.Dispose();
            _excelStaleDebounceTimer = new System.Threading.Timer(
                _ => MarkExcelDataStaleIfActuallyChanged(),
                null,
                TimeSpan.FromSeconds(1),
                Timeout.InfiniteTimeSpan);
        }
    }

    private void MarkExcelDataStaleIfActuallyChanged()
    {
        // Ignore the notice if the write time actually matches what we already
        // read (e.g. the app itself just wrote the print log next to it, or the
        // save re-touched the file without changing content-relevant data).
        var currentWriteTimeUtc = TryGetFileWriteTimeUtc(Template.DatabaseConfig.FilePath);
        if (currentWriteTimeUtc is null || currentWriteTimeUtc == _excelDataSourceWriteTimeUtc)
        {
            return;
        }

        // FileSystemWatcher/timer callbacks run on a thread-pool thread; marshal to
        // the UI thread before touching bindable state when a WPF Dispatcher exists.
        var dispatcher = System.Windows.Application.Current?.Dispatcher;
        if (dispatcher is null || dispatcher.CheckAccess())
        {
            IsExcelDataStale = true;
        }
        else
        {
            dispatcher.BeginInvoke(() => IsExcelDataStale = true);
        }
    }

    /// <summary>
    /// Finds the row index matching the stored KeyField/KeyValue pair.
    /// Returns the matching index, or null if not found (caller should fallback to LastSelectedRow).
    /// </summary>
    private int? FindRowIndexByKeyField(DataTable table)
    {
        var keyField = Template.DatabaseConfig.KeyField;
        var keyValue = Template.DatabaseConfig.KeyValue;
        if (string.IsNullOrWhiteSpace(keyField) || string.IsNullOrWhiteSpace(keyValue) || !table.Columns.Contains(keyField))
        {
            return null;
        }

        for (var i = 0; i < table.Rows.Count; i++)
        {
            var cellValue = table.Rows[i][keyField]?.ToString() ?? string.Empty;
            if (string.Equals(cellValue, keyValue, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        // Key not found — data may have changed. Keep LastSelectedRow but warn.
        StatusText = $"Key row \"{keyValue}\" not found in column \"{keyField}\" — row may have been removed or changed";
        return null;
    }

    private bool CanRefreshExcelData()
    {
        return !string.IsNullOrWhiteSpace(Template.DatabaseConfig.FilePath)
            && !string.IsNullOrWhiteSpace(Template.DatabaseConfig.SheetName)
            && File.Exists(Template.DatabaseConfig.FilePath);
    }

    private async Task RestoreLinkedExcelDataAsync()
    {
        OnPropertyChanged(nameof(HasLinkedExcelSource));
        OnPropertyChanged(nameof(LinkedExcelSourceText));

        if (!HasLinkedExcelSource)
        {
            StopWatchingExcelFile();
            IsExcelDataStale = false;
            IsExcelLinkBroken = false;
            StatusText = $"Opened: {Path.GetFileName(CurrentFilePath)}";
            return;
        }

        // If this template references a shared data source, pick up whatever path/sheet
        // it currently points to first — relinking the shared source once then fixes
        // every template that references it, instead of each template needing its own
        // relink (database-plan GĐ2 item 4).
        DataSource? linkedSource = null;
        if (!string.IsNullOrWhiteSpace(Template.DatabaseConfig.DataSourceId))
        {
            linkedSource = _dataSourceRegistry.GetById(Template.DatabaseConfig.DataSourceId);
            if (linkedSource is not null)
            {
                Template.DatabaseConfig.FilePath = linkedSource.FilePath;
                Template.DatabaseConfig.SheetName = linkedSource.SheetName;
                Template.DatabaseConfig.HeaderRowIndex = linkedSource.HeaderRowIndex;
            }
        }

        // Resolve Excel path: absolute → relative → same directory
        var resolvedPath = ResolveExcelPath();
        if (resolvedPath is null)
        {
            StopWatchingExcelFile();
            IsExcelDataStale = false;
            ExcelDataView = null;
            PreviewRow = null;
            SelectedDataItem = null;
            ExcelHeaders.Clear();
            OnPropertyChanged(nameof(CurrentExcelRowText));
            OnPropertyChanged(nameof(KeyFieldOptions));
            OnPropertyChanged(nameof(SelectedKeyFieldName));
            OnPropertyChanged(nameof(SelectedCopiesFieldName));
            IsExcelLinkBroken = true;
            StatusText = $"Opened: {Path.GetFileName(CurrentFilePath)}. Linked Excel file not found: {Template.DatabaseConfig.FilePath}";
            return;
        }

        // Update FilePath to the resolved path so it works from the new location
        Template.DatabaseConfig.FilePath = resolvedPath;

        try
        {
            await ImportExcelAsync(Template.DatabaseConfig.FilePath, Template.DatabaseConfig.SheetName, "Open");
            if (linkedSource is not null)
            {
                RecordDataSourceUsage(linkedSource);
            }

            IsExcelLinkBroken = false;
            StatusText = $"Opened: {Path.GetFileName(CurrentFilePath)}. Excel link restored: {Path.GetFileName(Template.DatabaseConfig.FilePath)} / {Template.DatabaseConfig.SheetName}";
        }
        catch (Exception ex)
        {
            ExcelDataView = null;
            PreviewRow = null;
            SelectedDataItem = null;
            ExcelHeaders.Clear();
            OnPropertyChanged(nameof(CurrentExcelRowText));
            IsExcelLinkBroken = true;
            StatusText = $"Opened: {Path.GetFileName(CurrentFilePath)}. Excel link could not be restored: {ex.Message}";
        }
    }

    /// <summary>
    /// Prompts the user to locate the linked Excel file and restores the connection.
    /// </summary>
    private async Task RelinkExcelAsync()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Locate Excel file",
            Filter = "Excel Files (*.xlsx;*.xlsm)|*.xlsx;*.xlsm|All Files (*.*)|*.*",
            FileName = Path.GetFileName(Template.DatabaseConfig.FilePath)
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        var filePath = dialog.FileName;

        // If the Excel file has sheets, prompt the user to pick one
        IReadOnlyList<string> sheets;
        try
        {
            sheets = await _excelDataService.GetSheetNamesAsync(filePath);
        }
        catch (Exception ex)
        {
            StatusText = $"Cannot read Excel file: {ex.Message}";
            return;
        }

        if (sheets.Count == 0)
        {
            StatusText = $"No sheets found in {Path.GetFileName(filePath)}";
            return;
        }

        var sheetName = sheets.Contains(Template.DatabaseConfig.SheetName)
            ? Template.DatabaseConfig.SheetName
            : sheets[0];

        Template.DatabaseConfig.FilePath = filePath;

        if (!string.IsNullOrWhiteSpace(CurrentFilePath))
        {
            UpdateRelativePath(CurrentFilePath);
        }

        try
        {
            await ImportExcelAsync(filePath, sheetName, "Relink");
            IsExcelLinkBroken = false;
            StatusText = $"Excel re-linked: {Path.GetFileName(filePath)} / {sheetName}";
        }
        catch (Exception ex)
        {
            IsExcelLinkBroken = true;
            StatusText = $"Re-link failed: {ex.Message}";
        }
    }

    /// <summary>
    /// Removes the Excel link from this template (database-manager-module-plan.md M1).
    /// Clears the file/sheet/rows/fields so the template goes back to standalone, but
    /// deliberately leaves every object's <see cref="LabelObject.BindingExpression"/>
    /// untouched — importing a file with the same column names resumes without having
    /// to re-bind objects. This is the only way to escape a permanently broken link
    /// short of hand-editing the .anlabel file.
    /// Confirmation is the caller's responsibility (shown in code-behind, not here) so
    /// this stays callable from automated tests without popping a real dialog.
    /// </summary>
    public void UnlinkExcel()
    {
        if (!HasLinkedExcelSource)
        {
            return;
        }

        var previousFilePath = Template.DatabaseConfig.FilePath;
        var previousSheetName = Template.DatabaseConfig.SheetName;

        StopWatchingExcelFile();
        ExcelDataView = null;
        ExcelHeaders.Clear();
        SelectedDataItem = null;
        SelectedAvailableDatabaseField = null;
        SelectedLabelDatabaseField = null;
        SelectedExcelField = null;
        Template.DatabaseConfig = new DatabaseConfig();
        IsExcelLinkBroken = false;
        IsExcelDataStale = false;
        _excelDataReadAtLocal = null;
        _excelDataSourceWriteTimeUtc = null;

        OnPropertyChanged(nameof(HasLinkedExcelSource));
        OnPropertyChanged(nameof(LinkedExcelSourceText));
        OnPropertyChanged(nameof(ExcelLinkStatusText));
        OnPropertyChanged(nameof(ExcelDataFreshnessText));
        OnPropertyChanged(nameof(CurrentExcelRowText));
        OnPropertyChanged(nameof(KeyFieldOptions));
        OnPropertyChanged(nameof(SelectedKeyFieldName));
        OnPropertyChanged(nameof(SelectedCopiesFieldName));
        RaiseDatabaseFieldStateChanged();

        StatusText = "Excel link removed. Objects keep their bindings — import Excel again to resume printing.";
        LogDataOperation("Unlink", previousFilePath, previousSheetName, rowCount: 0, columnCount: 0, success: true, errorMessage: string.Empty);
    }

    /// <summary>
    /// Saves the currently linked Excel file/sheet as a reusable shared data source
    /// (database-plan GĐ2 item 4) and points this template at it.
    /// </summary>
    private void AddCurrentAsDataSource()
    {
        if (!HasLinkedExcelSource || IsExcelLinkBroken)
        {
            return;
        }

        // Avoid creating a duplicate registry entry if the user clicks this more than
        // once (or the current file/sheet was already saved as a source earlier) — just
        // point the template at the existing match instead of piling up look-alike rows.
        var existing = DataSources.FirstOrDefault(s =>
            string.Equals(s.FilePath, Template.DatabaseConfig.FilePath, StringComparison.OrdinalIgnoreCase)
            && string.Equals(s.SheetName, Template.DatabaseConfig.SheetName, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            Template.DatabaseConfig.DataSourceId = existing.Id;
            StatusText = $"Already a shared data source: {existing.DisplayName}";
            return;
        }

        var source = new DataSource
        {
            FilePath = Template.DatabaseConfig.FilePath,
            SheetName = Template.DatabaseConfig.SheetName,
            HeaderRowIndex = Template.DatabaseConfig.HeaderRowIndex
        };
        _dataSourceRegistry.Upsert(source);
        _dataSourceRegistry.Save();
        DataSources.Add(source);
        Template.DatabaseConfig.DataSourceId = source.Id;
        StatusText = $"Added shared data source: {source.DisplayName}";
    }

    /// <summary>
    /// Points this template at a shared data source and imports its data.
    /// </summary>
    private async Task UseDataSourceAsync(DataSource source)
    {
        Template.DatabaseConfig.DataSourceId = source.Id;
        Template.DatabaseConfig.HeaderRowIndex = source.HeaderRowIndex;
        try
        {
            await ImportExcelAsync(source.FilePath, source.SheetName, "Import");
            RecordDataSourceUsage(source);
            StatusText = $"Using shared data source: {source.DisplayName}";
        }
        catch (Exception ex)
        {
            IsExcelLinkBroken = true;
            StatusText = $"Could not use shared data source '{source.DisplayName}': {ex.Message}";
        }
    }

    /// <summary>
    /// Records that <paramref name="source"/> was just successfully loaded by the current
    /// template (database-manager-module-plan.md M3): updates <see cref="DataSource.LastUsedUtc"/>
    /// and pushes <see cref="CurrentFilePath"/> to the front of <see cref="DataSource.RecentTemplates"/>
    /// (deduplicated, capped at 10, most-recent-first). Skipped for an unsaved template
    /// (empty <see cref="CurrentFilePath"/>) since there is no path to record yet.
    /// </summary>
    private void RecordDataSourceUsage(DataSource source)
    {
        source.LastUsedUtc = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(CurrentFilePath))
        {
            source.RecentTemplates.RemoveAll(path => string.Equals(path, CurrentFilePath, StringComparison.OrdinalIgnoreCase));
            source.RecentTemplates.Insert(0, CurrentFilePath);
            if (source.RecentTemplates.Count > 10)
            {
                source.RecentTemplates.RemoveRange(10, source.RecentTemplates.Count - 10);
            }
        }

        _dataSourceRegistry.Upsert(source);
        _dataSourceRegistry.Save();
    }

    private void RemoveDataSource(DataSource? source)
    {
        if (source is null)
        {
            return;
        }

        if (string.Equals(Template.DatabaseConfig.DataSourceId, source.Id, StringComparison.OrdinalIgnoreCase))
        {
            // Clear the shared-source reference but keep the template's own FilePath/SheetName
            // (fallback path already in DatabaseConfig) so it keeps working standalone.
            Template.DatabaseConfig.DataSourceId = string.Empty;
        }

        _dataSourceRegistry.Remove(source.Id);
        _dataSourceRegistry.Save();
        DataSources.Remove(source);
        StatusText = $"Removed shared data source: {source.DisplayName}";
    }

    /// <summary>
    /// Points a shared data source at a new file location. Every template that
    /// references this source's Id picks up the new path the next time it is opened
    /// or refreshed — this is the main payoff of using a shared source over a
    /// per-template link.
    /// </summary>
    private async Task RelinkDataSourceAsync(DataSource source)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Locate Excel file for shared data source",
            Filter = "Excel Files (*.xlsx;*.xlsm)|*.xlsx;*.xlsm|All Files (*.*)|*.*",
            FileName = Path.GetFileName(source.FilePath)
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        IReadOnlyList<string> sheets;
        try
        {
            sheets = await _excelDataService.GetSheetNamesAsync(dialog.FileName);
        }
        catch (Exception ex)
        {
            StatusText = $"Cannot read Excel file: {ex.Message}";
            return;
        }

        if (sheets.Count == 0)
        {
            StatusText = $"No sheets found in {Path.GetFileName(dialog.FileName)}";
            return;
        }

        source.FilePath = dialog.FileName;
        source.SheetName = sheets.Contains(source.SheetName) ? source.SheetName : sheets[0];
        _dataSourceRegistry.Upsert(source);
        _dataSourceRegistry.Save();
        StatusText = $"Relinked shared data source: {source.DisplayName}";

        if (string.Equals(Template.DatabaseConfig.DataSourceId, source.Id, StringComparison.OrdinalIgnoreCase))
        {
            await UseDataSourceAsync(source);
        }
    }

    /// <summary>
    /// Persists a shared data source's <see cref="DataSource.Name"/> after inline
    /// editing in the Data Sources panel (invoked from the TextBox LostFocus handler).
    /// </summary>
    public void PersistDataSources()
    {
        _dataSourceRegistry.Save();
    }

    /// <summary>
    /// Tries to find the linked Excel file by checking (in order):
    /// 1. Absolute path (FilePath)
    /// 2. Relative to the .anlabel file (RelativePath)
    /// 3. Same directory as the .anlabel file (filename only)
    /// Returns the resolved absolute path, or null if not found.
    /// </summary>
    private string? ResolveExcelPath()
    {
        var config = Template.DatabaseConfig;
        var fileName = Path.GetFileName(config.FilePath);

        // 1. Try absolute path
        if (!string.IsNullOrWhiteSpace(config.FilePath) && File.Exists(config.FilePath))
        {
            return config.FilePath;
        }

        // 2. Try relative path (relative to the .anlabel file location)
        if (!string.IsNullOrWhiteSpace(config.RelativePath) && !string.IsNullOrWhiteSpace(CurrentFilePath))
        {
            var templateDir = Path.GetDirectoryName(CurrentFilePath);
            if (templateDir is not null)
            {
                var candidate = Path.GetFullPath(Path.Combine(templateDir, config.RelativePath));
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }
        }

        // 3. Try same directory as .anlabel file (filename only)
        if (!string.IsNullOrWhiteSpace(fileName) && !string.IsNullOrWhiteSpace(CurrentFilePath))
        {
            var templateDir = Path.GetDirectoryName(CurrentFilePath);
            if (templateDir is not null)
            {
                var candidate = Path.Combine(templateDir, fileName);
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Updates DatabaseConfig.RelativePath based on the current FilePath and the template's save location.
    /// </summary>
    private void UpdateRelativePath(string templateFilePath)
    {
        var excelPath = Template.DatabaseConfig.FilePath;
        if (string.IsNullOrWhiteSpace(excelPath) || string.IsNullOrWhiteSpace(templateFilePath))
        {
            Template.DatabaseConfig.RelativePath = string.Empty;
            return;
        }

        var templateDir = Path.GetDirectoryName(Path.GetFullPath(templateFilePath));
        if (templateDir is null)
        {
            Template.DatabaseConfig.RelativePath = string.Empty;
            return;
        }

        try
        {
            var relativeUri = new Uri(templateDir + Path.DirectorySeparatorChar).MakeRelativeUri(new Uri(Path.GetFullPath(excelPath)));
            Template.DatabaseConfig.RelativePath = Uri.UnescapeDataString(relativeUri.ToString().Replace('/', Path.DirectorySeparatorChar));
        }
        catch
        {
            Template.DatabaseConfig.RelativePath = string.Empty;
        }
    }

    private void SyncDatabaseFieldsFromTable(DataTable table)
    {
        var existingLabelFields = LabelDatabaseFields
            .Select(field => CloneDatabaseField(field))
            .ToArray();
        var shouldSeedLabelFields = existingLabelFields.Length == 0 && !Template.Objects.Any(item => !string.IsNullOrWhiteSpace(item.BindingExpression));
        AvailableDatabaseFields.Clear();
        foreach (DataColumn column in table.Columns)
        {
            AvailableDatabaseFields.Add(CreateDatabaseField(column.ColumnName, table));
        }

        var requestedFieldNames = existingLabelFields
            .Select(field => field.Name)
            .Concat(GetReferencedTemplateFieldNames());
        var resolvedFieldMap = BuildResolvedFieldMap(requestedFieldNames, AvailableDatabaseFields.Select(field => field.Name));

        for (var i = LabelDatabaseFields.Count - 1; i >= 0; i--)
        {
            if (!FieldNameResolver.TryResolveFieldName(LabelDatabaseFields[i].Name, ExcelHeaders, out _))
            {
                LabelDatabaseFields.RemoveAt(i);
            }
        }

        foreach (var field in existingLabelFields)
        {
            if (!resolvedFieldMap.TryGetValue(field.Name, out var resolvedFieldName))
            {
                continue;
            }

            var availableField = AvailableDatabaseFields.FirstOrDefault(candidate => string.Equals(candidate.Name, resolvedFieldName, StringComparison.OrdinalIgnoreCase));
            if (availableField is not null)
            {
                var existing = LabelDatabaseFields.FirstOrDefault(candidate => string.Equals(candidate.Name, availableField.Name, StringComparison.OrdinalIgnoreCase));
                if (existing is null)
                {
                    LabelDatabaseFields.Add(CloneDatabaseField(availableField));
                }
                else
                {
                    existing.Name = availableField.Name;
                    existing.DisplayName = availableField.DisplayName;
                    existing.SampleValue = availableField.SampleValue;
                }
            }
        }

        if (shouldSeedLabelFields)
        {
            foreach (var field in AvailableDatabaseFields)
            {
                LabelDatabaseFields.Add(CloneDatabaseField(field));
            }
        }

        RepairTemplateFieldReferences(resolvedFieldMap);
        SelectedAvailableDatabaseField = AvailableDatabaseFields.FirstOrDefault();
        SelectedLabelDatabaseField = LabelDatabaseFields.FirstOrDefault();
        SelectedExcelField = SelectedLabelDatabaseField?.Name;
        RaiseDatabaseFieldStateChanged();
    }

    private static DatabaseField CreateDatabaseField(string fieldName, DataTable table)
    {
        var sampleValue = table.Rows.Count == 0 ? string.Empty : table.Rows[0][fieldName]?.ToString() ?? string.Empty;
        return new DatabaseField { Name = fieldName, DisplayName = fieldName, SampleValue = sampleValue };
    }

    private static DatabaseField CloneDatabaseField(DatabaseField field)
    {
        return new DatabaseField { Name = field.Name, DisplayName = field.DisplayName, SampleValue = field.SampleValue };
    }

    private static Dictionary<string, string> BuildResolvedFieldMap(IEnumerable<string> requestedFieldNames, IEnumerable<string> availableFieldNames)
    {
        var resolved = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var available = availableFieldNames
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        foreach (var requested in requestedFieldNames.Where(name => !string.IsNullOrWhiteSpace(name)).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (FieldNameResolver.TryResolveFieldName(requested, available, out var resolvedName))
            {
                resolved[requested] = resolvedName;
            }
        }

        return resolved;
    }

    private IEnumerable<string> GetReferencedTemplateFieldNames()
    {
        foreach (var item in Template.Objects)
        {
            if (string.IsNullOrWhiteSpace(item.BindingExpression))
            {
                continue;
            }

            if (FormulaBindingEvaluator.LooksLikeFormula(item.BindingExpression))
            {
                foreach (var field in FormulaBindingEvaluator.Evaluate(item.BindingExpression, PreviewRow ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)).UsedFields)
                {
                    if (!string.IsNullOrWhiteSpace(field))
                    {
                        yield return field;
                    }
                }

                continue;
            }

            foreach (var field in BindingExpressionEvaluator.GetFields(item.BindingExpression))
            {
                if (!string.IsNullOrWhiteSpace(field))
                {
                    yield return field;
                }
            }
        }
    }

    private void RepairTemplateFieldReferences(IReadOnlyDictionary<string, string> resolvedFieldMap)
    {
        if (resolvedFieldMap.Count == 0)
        {
            return;
        }

        foreach (var item in Template.Objects)
        {
            if (string.IsNullOrWhiteSpace(item.BindingExpression))
            {
                continue;
            }

            var repaired = RepairBindingExpression(item.BindingExpression, resolvedFieldMap);
            if (!string.Equals(repaired, item.BindingExpression, StringComparison.Ordinal))
            {
                item.BindingExpression = repaired;
            }
        }
    }

    private static string RepairBindingExpression(string expression, IReadOnlyDictionary<string, string> resolvedFieldMap)
    {
        if (FormulaBindingEvaluator.LooksLikeFormula(expression))
        {
            return Regex.Replace(
                expression,
                "FIELD\\(\\s*\"((?:\\\\.|[^\"])*)\"\\s*\\)",
                match =>
                {
                    var requestedField = Regex.Unescape(match.Groups[1].Value);
                    if (!TryResolveMappedFieldName(requestedField, resolvedFieldMap, out var resolvedField))
                    {
                        return match.Value;
                    }

                    return $"FIELD(\"{EscapeFormulaString(resolvedField)}\")";
                },
                RegexOptions.IgnoreCase);
        }

        return Regex.Replace(
            expression,
            "\\{([^{}]+)\\}",
            match =>
            {
                var requestedField = match.Groups[1].Value;
                return TryResolveMappedFieldName(requestedField, resolvedFieldMap, out var resolvedField)
                    ? $"{{{resolvedField}}}"
                    : match.Value;
            });
    }

    private static bool TryResolveMappedFieldName(string requestedField, IReadOnlyDictionary<string, string> resolvedFieldMap, out string resolvedField)
    {
        if (resolvedFieldMap.TryGetValue(requestedField, out var mappedField) && !string.IsNullOrWhiteSpace(mappedField))
        {
            resolvedField = mappedField;
            return true;
        }

        var normalizedRequested = FieldNameResolver.Normalize(requestedField);
        foreach (var pair in resolvedFieldMap)
        {
            if (string.Equals(FieldNameResolver.Normalize(pair.Key), normalizedRequested, StringComparison.OrdinalIgnoreCase))
            {
                resolvedField = pair.Value;
                return true;
            }
        }

        resolvedField = string.Empty;
        return false;
    }

    private void AddDatabaseField()
    {
        if (SelectedAvailableDatabaseField is null)
        {
            return;
        }

        AddLabelDatabaseField(SelectedAvailableDatabaseField);
    }

    private void AddAllDatabaseFields()
    {
        foreach (var field in AvailableDatabaseFields)
        {
            AddLabelDatabaseField(field, updateStatus: false);
        }

        StatusText = $"Added {LabelDatabaseFields.Count} field(s) for label design";
        RaiseDatabaseFieldStateChanged();
    }

    private void AddLabelDatabaseField(DatabaseField field, bool updateStatus = true)
    {
        if (LabelDatabaseFields.Any(item => string.Equals(item.Name, field.Name, StringComparison.OrdinalIgnoreCase)))
        {
            if (updateStatus)
            {
                StatusText = $"Field already added: {field.Name}";
            }
            return;
        }

        var clone = CloneDatabaseField(field);
        LabelDatabaseFields.Add(clone);
        SelectedLabelDatabaseField = clone;
        SelectedExcelField = clone.Name;
        if (updateStatus)
        {
            StatusText = $"Added label field: {field.Name}";
        }

        RaiseDatabaseFieldStateChanged();
    }

    private void RemoveDatabaseField()
    {
        if (SelectedLabelDatabaseField is null)
        {
            return;
        }

        var removedName = SelectedLabelDatabaseField.Name;
        LabelDatabaseFields.Remove(SelectedLabelDatabaseField);
        SelectedLabelDatabaseField = LabelDatabaseFields.FirstOrDefault();
        SelectedExcelField = SelectedLabelDatabaseField?.Name;
        StatusText = $"Removed label field: {removedName}";
        RaiseDatabaseFieldStateChanged();
    }

    private void ClearDatabaseFields()
    {
        LabelDatabaseFields.Clear();
        SelectedLabelDatabaseField = null;
        SelectedExcelField = null;
        StatusText = "Cleared label fields";
        RaiseDatabaseFieldStateChanged();
    }

    private void RaiseDatabaseFieldStateChanged()
    {
        RefreshObjectTreeBindingStates();
        OnPropertyChanged(nameof(AvailableDatabaseFields));
        OnPropertyChanged(nameof(LabelDatabaseFields));
        OnPropertyChanged(nameof(BindingIssues));
        OnPropertyChanged(nameof(HasBindingIssues));
        OnPropertyChanged(nameof(BindingIssuesSummary));
        ((RelayCommand)AddDatabaseFieldCommand).RaiseCanExecuteChanged();
        ((RelayCommand)AddAllDatabaseFieldsCommand).RaiseCanExecuteChanged();
        ((RelayCommand)RemoveDatabaseFieldCommand).RaiseCanExecuteChanged();
        ((RelayCommand)ClearDatabaseFieldsCommand).RaiseCanExecuteChanged();
        ((RelayCommand)AddExcelFieldCommand).RaiseCanExecuteChanged();
        ((RelayCommand)BindSelectedAsExcelFieldCommand).RaiseCanExecuteChanged();
        ((RelayCommand)RefreshExcelDataCommand).RaiseCanExecuteChanged();
    }

    private async void PrintCurrentRow()
    {
        try
        {
            if (IsExcelDataStale)
            {
                StatusText = "Print blocked: the linked Excel file changed since it was last read. Click Update Excel first (or use Print Preview, which lets you confirm and print with the current data).";
                return;
            }

            var rows = ExpandRowsForCopies(PreviewRow is null ? new IReadOnlyDictionary<string, string>?[] { null } : new IReadOnlyDictionary<string, string>?[] { PreviewRow }).ToArray();
            var validationError = ValidatePrintableContent(rows);
            if (validationError is not null)
            {
                StatusText = validationError;
                return;
            }

            if (_printService.PrintRows(Template, rows, $"{Template.Name} label"))
            {
                StatusText = $"Print job sent: {rows.Length} label(s)";
                await WritePrintLogAsync("Current row", rows, PreviewRow is null ? 0 : 1, rows.Length);
            }
        }
        catch (Exception ex)
        {
            StatusText = $"Print failed: {ex.Message}";
            LogPrintOperation("Current row", PreviewRow is null ? 0 : 1, 0, success: false, ex.Message);
        }
    }

    private async void PrintAllRows()
    {
        try
        {
            if (ExcelDataView is null || ExcelDataView.Count == 0)
            {
                StatusText = "No Excel rows to print";
                return;
            }

            if (IsExcelDataStale)
            {
                StatusText = "Print blocked: the linked Excel file changed since it was last read. Click Update Excel first (or use Print Preview, which lets you confirm and print with the current data).";
                return;
            }

            var rows = ExpandRowsForCopies(ExcelDataView
                .Cast<DataRowView>()
                .Select(CreatePreviewRow)
                .Where(row => row is not null)
                .Cast<IReadOnlyDictionary<string, string>>()
                .Cast<IReadOnlyDictionary<string, string>?>()).ToArray();

            var validationError = ValidatePrintableContent(rows);
            if (validationError is not null)
            {
                StatusText = validationError;
                return;
            }

            if (_printService.PrintRows(Template, rows, $"{Template.Name} labels"))
            {
                StatusText = $"Print job sent: {rows.Length} labels";
                await WritePrintLogAsync("All rows", rows, ExcelDataView.Count, rows.Length);
            }
        }
        catch (Exception ex)
        {
            StatusText = $"Print failed: {ex.Message}";
            LogPrintOperation("All rows", ExcelDataView?.Count ?? 0, 0, success: false, ex.Message);
        }
    }

    private void PrintCalibration()
    {
        try
        {
            if (_printService.PrintCalibration(Template))
            {
                StatusText = "Calibration print job sent";
            }
        }
        catch (Exception ex)
        {
            StatusText = $"Calibration print failed: {ex.Message}";
        }
    }

    private void AddText()
    {
        AddObject(new LabelObject
        {
            Type = ObjectType.Text,
            Name = "Text",
            Text = "Long text can overflow horizontally",
            BindingExpression = string.Empty,
            XMm = 5,
            YMm = 5,
            WidthMm = 35,
            HeightMm = 10,
            Style = { FontSizePt = 11, BorderThicknessMm = 0 }
        });
    }

    private void AddTextBox()
    {
        AddObject(new LabelObject
        {
            Type = ObjectType.TextBox,
            Name = "Text Box",
            Text = "Text box keeps content inside its bounds and wraps long lines.",
            BindingExpression = string.Empty,
            XMm = 5,
            YMm = 18,
            WidthMm = 42,
            HeightMm = 16,
            Style = { FontSizePt = 9, BorderThicknessMm = 0, OutlineStyle = OutlineStyle.None }
        });
    }

    private void ReplaceSelectedImage()
    {
        if (SelectedObject is null || SelectedObject.Type != ObjectType.Image)
        {
            return;
        }

        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Chọn hình ảnh",
            Filter = "Image Files (*.png;*.jpg;*.jpeg;*.bmp;*.gif)|*.png;*.jpg;*.jpeg;*.bmp;*.gif|All Files (*.*)|*.*"
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            SelectedObject.ImageDataBase64 = Convert.ToBase64String(File.ReadAllBytes(dialog.FileName));
            SelectedObject.Name = Path.GetFileNameWithoutExtension(dialog.FileName);
            StatusText = $"Replaced image: {SelectedObject.Name}";
        }
        catch (Exception ex)
        {
            StatusText = $"Cannot read image file: {ex.Message}";
        }
    }

    private void AddImage()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Chọn hình ảnh",
            Filter = "Image Files (*.png;*.jpg;*.jpeg;*.bmp;*.gif)|*.png;*.jpg;*.jpeg;*.bmp;*.gif|All Files (*.*)|*.*"
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        byte[] bytes;
        try
        {
            bytes = File.ReadAllBytes(dialog.FileName);
        }
        catch (Exception ex)
        {
            StatusText = $"Cannot read image file: {ex.Message}";
            return;
        }

        var (widthMm, heightMm) = GetDefaultImageSizeMm(bytes);

        AddObject(new LabelObject
        {
            Type = ObjectType.Image,
            Name = Path.GetFileNameWithoutExtension(dialog.FileName),
            ImageDataBase64 = Convert.ToBase64String(bytes),
            XMm = 5,
            YMm = 5,
            WidthMm = widthMm,
            HeightMm = heightMm,
            Style = { BorderThicknessMm = 0, OutlineStyle = OutlineStyle.None }
        });
    }

    private static (double WidthMm, double HeightMm) GetDefaultImageSizeMm(byte[] imageBytes)
    {
        const double defaultMm = 25;
        try
        {
            using var stream = new MemoryStream(imageBytes);
            var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.None, BitmapCacheOption.Default);
            var frame = decoder.Frames[0];
            if (frame.PixelWidth <= 0 || frame.PixelHeight <= 0)
            {
                return (defaultMm, defaultMm);
            }

            var aspect = (double)frame.PixelWidth / frame.PixelHeight;
            return aspect >= 1
                ? (defaultMm, defaultMm / aspect)
                : (defaultMm * aspect, defaultMm);
        }
        catch
        {
            return (defaultMm, defaultMm);
        }
    }

    private void AddExcelField(string? fieldName)
    {
        if (string.IsNullOrWhiteSpace(fieldName))
        {
            StatusText = "Select an Excel field first";
            return;
        }

        AddObject(new LabelObject
        {
            Type = ObjectType.Text,
            Name = $"Field: {fieldName}",
            Text = fieldName,
            BindingExpression = $"{{{fieldName}}}",
            XMm = 5,
            YMm = 5,
            WidthMm = 38,
            HeightMm = 10,
            Style = { FontSizePt = 11, BorderThicknessMm = 0 }
        });
    }

    private void BindSelectedAsExcelField(string? fieldName)
    {
        if (SelectedObject is null || string.IsNullOrWhiteSpace(fieldName))
        {
            return;
        }

        if (SelectedObject.Type is not (ObjectType.Text or ObjectType.TextBox or ObjectType.BarcodeCode128 or ObjectType.QRCode or ObjectType.DataMatrix))
        {
            SelectedObject.Type = ObjectType.Text;
        }

        SelectedObject.Name = SelectedObject.Type is ObjectType.BarcodeCode128 or ObjectType.QRCode or ObjectType.DataMatrix
            ? $"{SelectedObject.Type}: {fieldName}"
            : $"Field: {fieldName}";
        SelectedObject.Text = fieldName;
        SelectedObject.BindingExpression = $"{{{fieldName}}}";
        StatusText = $"Bound selected object to {{{fieldName}}}";
        RaiseFormulaPreviewChanged();
    }

    private void ClearSelectedBinding()
    {
        if (SelectedObject is null)
        {
            return;
        }

        SelectedObject.BindingExpression = string.Empty;
        if (string.IsNullOrWhiteSpace(SelectedObject.Text))
        {
            SelectedObject.Text = "Text";
        }

        if (SelectedObject.Type is ObjectType.Text or ObjectType.TextBox)
        {
            SelectedObject.Name = "Text";
        }
        StatusText = "Selected object changed to static text";
        RaiseFormulaPreviewChanged();
    }

    private void InsertFunctionFormula(string? formula)
    {
        if (SelectedObject is null || string.IsNullOrWhiteSpace(formula))
        {
            StatusText = "Select an object before inserting a function";
            return;
        }

        SelectedObject.BindingExpression = formula;
        if (SelectedObject.Type is ObjectType.BarcodeCode128 or ObjectType.QRCode or ObjectType.DataMatrix)
        {
            SelectedObject.Text = string.Empty;
        }

        StatusText = $"Inserted formula: {formula}";
        RaiseFormulaPreviewChanged();
    }

    private void AddFormulaFieldPart(DatabaseField? field)
    {
        if (field is null)
        {
            return;
        }

        AddFormulaPart(new FormulaBuilderPart(FormulaBuilderPartKind.Field, field.Name, field.DisplayName));
    }

    private void AddFormulaTextPart()
    {
        AddFormulaTextPart(FormulaBuilderText);
        FormulaBuilderText = string.Empty;
    }

    private void AddFormulaTextPart(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        AddFormulaPart(new FormulaBuilderPart(FormulaBuilderPartKind.Text, text, text));
    }

    private void AddFormulaPart(FormulaBuilderPart part)
    {
        FormulaBuilderParts.Add(part);
        SelectedFormulaBuilderPart = part;
        RaiseFormulaBuilderChanged();
    }

    private void RemoveFormulaPart()
    {
        if (SelectedFormulaBuilderPart is null)
        {
            return;
        }

        var index = FormulaBuilderParts.IndexOf(SelectedFormulaBuilderPart);
        FormulaBuilderParts.Remove(SelectedFormulaBuilderPart);
        SelectedFormulaBuilderPart = FormulaBuilderParts.Count == 0 ? null : FormulaBuilderParts[Math.Clamp(index, 0, FormulaBuilderParts.Count - 1)];
        RaiseFormulaBuilderChanged();
    }

    private void ClearFormulaBuilder()
    {
        FormulaBuilderParts.Clear();
        SelectedFormulaBuilderPart = null;
        RaiseFormulaBuilderChanged();
    }

    private void ApplyFormulaBuilder()
    {
        if (SelectedObject is null || FormulaBuilderParts.Count == 0)
        {
            StatusText = "Select an object and add formula parts first";
            return;
        }

        var expression = BuildFormulaExpression();
        InsertFunctionFormula(expression);
        StatusText = $"Applied formula builder: {expression}";
    }

    private string BuildFormulaExpression()
    {
        if (FormulaBuilderParts.Count == 0)
        {
            return string.Empty;
        }

        var arguments = FormulaBuilderParts.Select(part => part.Kind == FormulaBuilderPartKind.Field
            ? $"FIELD(\"{EscapeFormulaString(part.Value)}\")"
            : $"\"{EscapeFormulaString(part.Value)}\"");
        return $"CONCAT({string.Join(", ", arguments)})";
    }

    private FormulaEvaluationResult EvaluateFormulaBuilder()
    {
        var expression = BuildFormulaExpression();
        if (string.IsNullOrWhiteSpace(expression))
        {
            return new FormulaEvaluationResult(string.Empty, Array.Empty<string>(), Array.Empty<string>());
        }

        return PreviewRow is null
            ? new FormulaEvaluationResult(expression, Array.Empty<string>(), Array.Empty<string>())
            : FormulaBindingEvaluator.Evaluate(expression, PreviewRow);
    }

    private void RaiseFormulaBuilderChanged()
    {
        OnPropertyChanged(nameof(FormulaBuilderExpression));
        OnPropertyChanged(nameof(FormulaBuilderPreviewValue));
        OnPropertyChanged(nameof(FormulaBuilderPreviewErrors));
        ((RelayCommand)RemoveFormulaPartCommand).RaiseCanExecuteChanged();
        ((RelayCommand)ClearFormulaBuilderCommand).RaiseCanExecuteChanged();
        ((RelayCommand)ApplyFormulaBuilderCommand).RaiseCanExecuteChanged();
    }

    private static string? GetFieldName(object? parameter)
    {
        return parameter switch
        {
            DatabaseField field => field.Name,
            string text => text,
            _ => parameter?.ToString()
        };
    }

    private static string? GetFormulaText(object? parameter)
    {
        return parameter switch
        {
            FormulaFunctionTemplate template => template.Template,
            DatabaseField field => $"FIELD(\"{EscapeFormulaString(field.Name)}\")",
            string text => text,
            _ => parameter?.ToString()
        };
    }

    private static string EscapeFormulaString(string value)
    {
        return value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal);
    }

    private void StartDrawingTool(ObjectType tool)
    {
        DrawingTool = tool;
        DrawingCommandText = tool switch
        {
            ObjectType.Line => "Line: specify first point",
            ObjectType.Ellipse => "Ellipse/Circle: specify first corner",
            _ => "Rectangle: specify first corner"
        };
        StatusText = tool switch
        {
            ObjectType.Line => "Line: click first point, move pointer, click next point or type length + Enter, Esc to cancel",
            ObjectType.Ellipse => "Ellipse/Circle: click first corner, move pointer, click opposite corner or type width,height + Enter, Esc to cancel",
            _ => "Rectangle: click first corner, move pointer, click opposite corner or type width,height + Enter, Esc to cancel"
        };
    }

    public void CompleteDrawingTool(LabelObject labelObject)
    {
        DrawingTool = null;
        SelectedObject = labelObject;
        StatusText = $"Added {labelObject.Name}";
    }

    private void AddBarcode()
    {
        var barcode = new LabelObject
        {
            Type = ObjectType.BarcodeCode128,
            Name = "Barcode",
            Text = "123456789012",
            BindingExpression = string.Empty,
            XMm = 8,
            YMm = 8,
            WidthMm = 32,
            HeightMm = 10,
            BarcodeSymbology = BarcodeSymbology.Code128,
            Style = { BorderThicknessMm = 0 }
        };
        AddBarcodeObject(barcode);
    }

    private void AddQrCode()
    {
        var qr = new LabelObject
        {
            Type = ObjectType.QRCode,
            Name = "QR Code",
            Text = "QR Code",
            BindingExpression = string.Empty,
            XMm = 8,
            YMm = 8,
            WidthMm = 8,
            HeightMm = 8,
            Style = { BorderThicknessMm = 0 }
        };
        AddBarcodeObject(qr);
    }

    private void AddDataMatrix()
    {
        var dm = new LabelObject
        {
            Type = ObjectType.DataMatrix,
            Name = "Data Matrix",
            Text = "Data Matrix",
            BindingExpression = string.Empty,
            XMm = 8,
            YMm = 8,
            WidthMm = 18,
            HeightMm = 18,
            Style = { BorderThicknessMm = 0 }
        };
        AddBarcodeObject(dm);
    }

    /// <summary>
    /// Adds a barcode/QR object along with a linked text object positioned just below it.
    /// The text object mirrors the barcode's content and moves together with it.
    /// </summary>
    private void AddBarcodeObject(LabelObject barcode)
    {
        barcode.ZIndex = Template.Objects.Count == 0 ? 1 : Template.Objects.Max(item => item.ZIndex) + 1;
        Template.Objects.Add(barcode);
        SelectedObject = barcode;
        StatusText = $"Added {barcode.Name}";
        RecordTemplateChange();
    }

    private void AddObject(LabelObject labelObject)
    {
        labelObject.ZIndex = Template.Objects.Count == 0 ? 1 : Template.Objects.Max(item => item.ZIndex) + 1;
        Template.Objects.Add(labelObject);
        SelectedObject = labelObject;
        StatusText = $"Added {labelObject.Name}";
    }

    private void DeleteSelected()
    {
        if (SelectedObject is null)
        {
            return;
        }

        var toDelete = SelectedObject;
        Template.Objects.Remove(toDelete);
        SelectedObject = null;
        StatusText = "Deleted selected object";
    }

    private void BringToFront()
    {
        if (SelectedObject is null || Template.Objects.Count == 0) return;
        var maxZ = Template.Objects.Max(o => o.ZIndex);
        if (SelectedObject.ZIndex != maxZ)
            SelectedObject.ZIndex = maxZ + 1;
    }

    private void SendToBack()
    {
        if (SelectedObject is null || Template.Objects.Count == 0) return;
        var minZ = Template.Objects.Min(o => o.ZIndex);
        if (SelectedObject.ZIndex != minZ)
            SelectedObject.ZIndex = minZ - 1;
    }

    /// <summary>Swaps ZIndex with the next object above (properties-panel-plan Đợt C).</summary>
    private void BringForward()
    {
        if (SelectedObject is null) return;
        var current = SelectedObject;
        var next = Template.Objects
            .Where(o => o.ZIndex > current.ZIndex)
            .OrderBy(o => o.ZIndex)
            .FirstOrDefault();
        if (next is null) return;
        (current.ZIndex, next.ZIndex) = (next.ZIndex, current.ZIndex);
    }

    /// <summary>Swaps ZIndex with the next object below (properties-panel-plan Đợt C).</summary>
    private void SendBackward()
    {
        if (SelectedObject is null) return;
        var current = SelectedObject;
        var previous = Template.Objects
            .Where(o => o.ZIndex < current.ZIndex)
            .OrderByDescending(o => o.ZIndex)
            .FirstOrDefault();
        if (previous is null) return;
        (current.ZIndex, previous.ZIndex) = (previous.ZIndex, current.ZIndex);
    }

    /// <summary>Sets rotation from a "0"/"90"/"180"/"270" command parameter (properties-panel-plan Đợt C: 4 quick buttons instead of a ComboBox).</summary>
    private void SetRotation(object? parameter)
    {
        if (SelectedObject is null || parameter is not string text || !int.TryParse(text, out var degrees))
        {
            return;
        }

        SelectedObject.Rotation = degrees;
    }

    private void Undo()
    {
        if (_undoStack.Count == 0)
        {
            return;
        }

        var currentSnapshot = CaptureTemplateSnapshot();
        var previousSnapshot = _undoStack.Pop();
        _redoStack.Push(currentSnapshot);
        RestoreTemplateSnapshot(previousSnapshot);
        StatusText = "Undo";
        RaiseHistoryCanExecuteChanged();
    }

    private void Redo()
    {
        if (_redoStack.Count == 0)
        {
            return;
        }

        var currentSnapshot = CaptureTemplateSnapshot();
        var nextSnapshot = _redoStack.Pop();
        _undoStack.Push(currentSnapshot);
        RestoreTemplateSnapshot(nextSnapshot);
        StatusText = "Redo";
        RaiseHistoryCanExecuteChanged();
    }

    private void ObserveTemplate(LabelTemplate template)
    {
        template.PropertyChanged += TemplateOnPropertyChanged;
        template.PrinterProfile.PropertyChanged += PrinterProfileOnPropertyChanged;
        template.Objects.CollectionChanged += ObjectsOnCollectionChanged;
        foreach (var item in template.Objects)
        {
            ObserveObject(item);
        }

        RefreshObjectTreeBindingStates();
    }

    private void UnobserveTemplate(LabelTemplate template)
    {
        template.PropertyChanged -= TemplateOnPropertyChanged;
        template.PrinterProfile.PropertyChanged -= PrinterProfileOnPropertyChanged;
        template.Objects.CollectionChanged -= ObjectsOnCollectionChanged;
        foreach (var item in template.Objects)
        {
            UnobserveObject(item);
        }
    }

    private void ObserveObject(LabelObject item)
    {
        item.PropertyChanged -= ObjectOnPropertyChanged;
        item.Style.PropertyChanged -= ObjectStyleOnPropertyChanged;
        item.PropertyChanged += ObjectOnPropertyChanged;
        item.Style.PropertyChanged += ObjectStyleOnPropertyChanged;
    }

    private void UnobserveObject(LabelObject item)
    {
        item.PropertyChanged -= ObjectOnPropertyChanged;
        item.Style.PropertyChanged -= ObjectStyleOnPropertyChanged;
    }

    private void TemplateOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (!_syncingTemplatePrinterSize && e.PropertyName is nameof(LabelTemplate.WidthMm) or nameof(LabelTemplate.HeightMm))
        {
            _syncingTemplatePrinterSize = true;
            Template.PrinterProfile.LabelWidthMm = Template.WidthMm;
            Template.PrinterProfile.LabelHeightMm = Template.HeightMm;
            Template.Orientation = LabelGeometry.ResolveOrientation(Template.WidthMm, Template.HeightMm);
            _syncingTemplatePrinterSize = false;
        }

        RecordTemplateChange();
    }

    private void PrinterProfileOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (!_syncingTemplatePrinterSize && e.PropertyName is nameof(PrinterProfile.LabelWidthMm) or nameof(PrinterProfile.LabelHeightMm))
        {
            _syncingTemplatePrinterSize = true;
            Template.WidthMm = Template.PrinterProfile.LabelWidthMm;
            Template.HeightMm = Template.PrinterProfile.LabelHeightMm;
            Template.Orientation = LabelGeometry.ResolveOrientation(Template.WidthMm, Template.HeightMm);
            _syncingTemplatePrinterSize = false;
        }

        RecordTemplateChange();
    }

    private void ObjectStyleOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        RecordTemplateChange();
        if (SelectedObject is not null && ReferenceEquals(SelectedObject.Style, sender))
        {
            OnPropertyChanged(nameof(SelectedObject));
            OnPropertyChanged(nameof(TextBoxValidationMessage));
        }
    }

    private void ObjectsOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
        {
            foreach (LabelObject item in e.OldItems)
            {
                UnobserveObject(item);
            }
        }

        if (e.NewItems is not null)
        {
            foreach (LabelObject item in e.NewItems)
            {
                ObserveObject(item);
            }
        }

        RefreshObjectTreeBindingStates();
        RecordTemplateChange();
    }

    private void ObjectOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(LabelObject.HasBindingExpression)
            or nameof(LabelObject.HasBindingIssue)
            or nameof(LabelObject.BindingStateDisplayText))
        {
            return;
        }

        if (sender is LabelObject changedObject)
        {
            ApplyQrAutoSizeFromModel(changedObject, e.PropertyName);
        }

        RecordTemplateChange();
        if (ReferenceEquals(sender, SelectedObject))
        {
            OnPropertyChanged(nameof(SelectedObject));
            if (e.PropertyName is nameof(LabelObject.BindingExpression) or nameof(LabelObject.Text))
            {
                RaiseFormulaPreviewChanged();
            }

            if (e.PropertyName is nameof(LabelObject.Text) or nameof(LabelObject.BindingExpression) or nameof(LabelObject.BarcodeSymbology) or nameof(LabelObject.Type))
            {
                OnPropertyChanged(nameof(BarcodeValidationMessage));
            }

            if (e.PropertyName is nameof(LabelObject.Text)
                or nameof(LabelObject.BindingExpression)
                or nameof(LabelObject.Type)
                or nameof(LabelObject.WidthMm)
                or nameof(LabelObject.HeightMm))
            {
                OnPropertyChanged(nameof(TextBoxValidationMessage));
            }

            if (e.PropertyName is nameof(LabelObject.Type)
                or nameof(LabelObject.BarcodeSymbology)
                or nameof(LabelObject.QrSizingMode)
                or nameof(LabelObject.QrModuleSizePx)
                or nameof(LabelObject.QrDpi))
            {
                OnPropertyChanged(nameof(BarcodeModuleSizeWarningText));
            }
        }

        if (e.PropertyName is nameof(LabelObject.BindingExpression)
            or nameof(LabelObject.Text)
            or nameof(LabelObject.Type)
            or nameof(LabelObject.IsVisible))
        {
            RefreshObjectTreeBindingStates();
        }
    }

    private void ApplyQrAutoSizeFromModel(LabelObject item, string? propertyName)
    {
        if (_applyingQrAutoSize.Contains(item)
            || !IsSquare2DCodeLike(item)
            || propertyName is not (nameof(LabelObject.Text)
                or nameof(LabelObject.BindingExpression)
                or nameof(LabelObject.BarcodeSymbology)
                or nameof(LabelObject.Type)
                or nameof(LabelObject.QrSizingMode)
                or nameof(LabelObject.QrErrorCorrection)
                or nameof(LabelObject.QrFixedVersion)
                or nameof(LabelObject.QrModuleSizePx)
                or nameof(LabelObject.QrQuietZoneModules)
                or nameof(LabelObject.QrDpi)))
        {
            return;
        }

        var targetSizeMm = item.QrSizingMode == QrSizingMode.FixedVersionAndModuleSize
            ? QrAutoSizeHelper.CalculateFixedSizeMm(item.QrFixedVersion, item.QrModuleSizePx, item.QrQuietZoneModules, item.QrDpi, GetAvailableQrSizeMm(item))
            : QrAutoSizeHelper.CalculateRequiredSizeMm(
                string.IsNullOrWhiteSpace(item.BindingExpression) ? item.Text : ResolveExpression(item.BindingExpression, PreviewRow),
                item.WidthMm,
                item.HeightMm,
                item.QrErrorCorrection,
                item.QrModuleSizePx,
                item.QrQuietZoneModules,
                item.QrDpi,
                _qrCapacityTable,
                GetAvailableQrSizeMm(item));
        if (targetSizeMm is null)
        {
            return;
        }

        if (Math.Abs(item.WidthMm - targetSizeMm.Value) <= 0.05 && Math.Abs(item.HeightMm - targetSizeMm.Value) <= 0.05)
        {
            return;
        }

        _applyingQrAutoSize.Add(item);
        try
        {
            item.WidthMm = targetSizeMm.Value;
            item.HeightMm = targetSizeMm.Value;
            OnPropertyChanged(nameof(SelectedObject));
        }
        finally
        {
            _applyingQrAutoSize.Remove(item);
        }
    }

    private static bool IsSquare2DCodeLike(LabelObject item)
    {
        return item.Type == ObjectType.QRCode
            || item.Type == ObjectType.DataMatrix
            || item.Type == ObjectType.BarcodeCode128
                && item.BarcodeSymbology is BarcodeSymbology.QRCode
                    or BarcodeSymbology.DataMatrix
                    or BarcodeSymbology.Aztec
                    or BarcodeSymbology.Pdf417;
    }

    private double GetAvailableQrSizeMm(LabelObject item)
    {
        var availableWidthMm = Template.WidthMm - item.XMm;
        var availableHeightMm = Template.HeightMm - item.YMm;
        return Math.Max(1, Math.Min(availableWidthMm, availableHeightMm));
    }

    private string ValidateSelectedBarcode()
    {
        if (SelectedObject is not { Type: ObjectType.BarcodeCode128 or ObjectType.QRCode or ObjectType.DataMatrix } item)
        {
            return string.Empty;
        }

        var data = string.IsNullOrWhiteSpace(item.BindingExpression) ? item.Text : ResolveExpression(item.BindingExpression, PreviewRow);
        var type = item.Type switch
        {
            ObjectType.QRCode => BarcodeType.QRCode,
            ObjectType.DataMatrix => BarcodeType.DataMatrix,
            _ => ANLAbel.App.Controls.BarcodeTypeMapper.ToRendererType(item.BarcodeSymbology)
        };

        var renderError = ValidateBarcodeCanRender(item, data, type);
        return renderError is null
            ? string.Empty
            : $"Invalid {type} data. {renderError}";
    }

    private string ValidateSelectedTextBox()
    {
        if (SelectedObject is not { Type: ObjectType.TextBox } item)
        {
            return string.Empty;
        }

        var data = string.IsNullOrWhiteSpace(item.BindingExpression) ? item.Text : ResolveExpression(item.BindingExpression, PreviewRow);
        return IsTextBoxOverflowing(item, data)
            ? "Text box overflow: increase the object size or reduce text/font size."
            : string.Empty;
    }

    public string? ValidatePrintPreviewContent()
    {
        var rows = ExcelDataView is null || ExcelDataView.Count == 0
            ? new IReadOnlyDictionary<string, string>?[] { PreviewRow }
            : ExcelDataView
                .Cast<DataRowView>()
                .Select(CreatePreviewRow)
                .Cast<IReadOnlyDictionary<string, string>?>()
                .ToArray();
        return ValidatePrintableContent(rows);
    }

    private string? ValidatePrintableContent(IReadOnlyList<IReadOnlyDictionary<string, string>?> rows)
    {
        foreach (var item in Template.Objects.Where(item => item.IsVisible && item.Type is ObjectType.BarcodeCode128 or ObjectType.QRCode or ObjectType.DataMatrix))
        {
            for (var i = 0; i < rows.Count; i++)
            {
                var data = string.IsNullOrWhiteSpace(item.BindingExpression) ? item.Text : ResolveExpression(item.BindingExpression, rows[i]);
                var type = item.Type switch
                {
                    ObjectType.QRCode => BarcodeType.QRCode,
                    ObjectType.DataMatrix => BarcodeType.DataMatrix,
                    _ => ANLAbel.App.Controls.BarcodeTypeMapper.ToRendererType(item.BarcodeSymbology)
                };

                var renderError = ValidateBarcodeCanRender(item, data, type);
                if (renderError is not null)
                {
                    return $"Print blocked: row {i + 1}, {item.Name} has invalid {type} data. {renderError}";
                }

                if (item.Type == ObjectType.QRCode && item.QrSizingMode == QrSizingMode.FixedVersionAndModuleSize)
                {
                    var byteCount = Encoding.UTF8.GetByteCount(data);
                    var capacity = EstimateFixedQrCapacity(item.WidthMm, item.HeightMm, item.QrErrorCorrection);
                    if (byteCount > capacity)
                    {
                        return $"Print blocked: row {i + 1}, {item.Name} has {byteCount} bytes but fixed QR {item.WidthMm:0.#}x{item.HeightMm:0.#}mm allows about {capacity}. Increase size or use Auto size.";
                    }
                }
            }
        }

        foreach (var item in Template.Objects.Where(item => item.IsVisible && item.Type == ObjectType.TextBox))
        {
            for (var i = 0; i < rows.Count; i++)
            {
                var data = string.IsNullOrWhiteSpace(item.BindingExpression) ? item.Text : ResolveExpression(item.BindingExpression, rows[i]);
                if (IsTextBoxOverflowing(item, data))
                {
                    return $"Print blocked: row {i + 1}, {item.Name} text overflows its text box. Increase object size or reduce text/font size.";
                }
            }
        }

        return null;
    }

    private static bool IsTextBoxOverflowing(LabelObject item, string data)
    {
        return TextBoxOverflowDetector.IsOverflowing(
            item,
            data,
            ANLAbel.Core.Geometry.MmConverter.MmToDip(item.WidthMm),
            ANLAbel.Core.Geometry.MmConverter.MmToDip(item.HeightMm));
    }

    private string? ValidateBarcodeCanRender(LabelObject item, string data, BarcodeType type)
    {
        if (!_barcodeValidator.ValidateData(data, type))
        {
            return "Check empty text, unsupported characters, or required length.";
        }

        try
        {
            _barcodeValidator.RenderBarcode(data, type, item.WidthMm, item.HeightMm, item.QrDpi, CreateBarcodeRenderOptions(item));
            return null;
        }
        catch (ArgumentException ex)
        {
            return ex.Message;
        }
        catch (InvalidOperationException ex)
        {
            return ex.Message;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            return ex.Message;
        }
    }

    private static BarcodeRenderOptions CreateBarcodeRenderOptions(LabelObject item)
    {
        return new BarcodeRenderOptions
        {
            ErrorCorrection = item.QrErrorCorrection.ToString(),
            QuietZoneModules = item.QrQuietZoneModules
        };
    }

    private static int EstimateFixedQrCapacity(double widthMm, double heightMm, QrErrorCorrection errorCorrection)
    {
        var safeSize = Math.Max(1, Math.Min(widthMm, heightMm));
        var baseline = Math.Floor(safeSize * safeSize);
        var factor = errorCorrection switch
        {
            QrErrorCorrection.L => 1.15,
            QrErrorCorrection.M => 1.0,
            QrErrorCorrection.Q => 0.8,
            QrErrorCorrection.H => 0.65,
            _ => 1.0
        };

        return Math.Max(1, (int)Math.Floor(baseline * factor));
    }

    private FormulaEvaluationResult EvaluateSelectedFormula()
    {
        if (SelectedObject is null || string.IsNullOrWhiteSpace(SelectedObject.BindingExpression) || !FormulaBindingEvaluator.LooksLikeFormula(SelectedObject.BindingExpression))
        {
            return new FormulaEvaluationResult(string.Empty, Array.Empty<string>(), Array.Empty<string>());
        }

        return PreviewRow is null
            ? new FormulaEvaluationResult(SelectedObject.BindingExpression, Array.Empty<string>(), Array.Empty<string>())
            : FormulaBindingEvaluator.Evaluate(SelectedObject.BindingExpression, PreviewRow);
    }

    private BindingPreviewResult EvaluateSelectedBinding()
    {
        if (SelectedObject is null || string.IsNullOrWhiteSpace(SelectedObject.BindingExpression))
        {
            return BindingPreviewResult.Empty;
        }

        return EvaluateBinding(SelectedObject);
    }

    private BindingPreviewResult EvaluateBinding(LabelObject item)
    {
        if (string.IsNullOrWhiteSpace(item.BindingExpression))
        {
            return BindingPreviewResult.Empty;
        }

        var expression = item.BindingExpression;
        var knownFields = AvailableDatabaseFields.Select(field => field.Name).ToArray();
        if (FormulaBindingEvaluator.LooksLikeFormula(expression))
        {
            var analysisRow = CreateBindingAnalysisRow();
            var analysis = FormulaBindingEvaluator.Evaluate(expression, analysisRow);
            var previewEvaluation = PreviewRow is null
                ? new FormulaEvaluationResult(string.Empty, Array.Empty<string>(), analysis.UsedFields)
                : FormulaBindingEvaluator.Evaluate(expression, PreviewRow);
            var missingFields = analysis.UsedFields
                .Where(field => !FieldNameResolver.TryResolveFieldName(field, knownFields, out _))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            var errors = analysis.Errors
                .Concat(previewEvaluation.Errors)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            return new BindingPreviewResult(
                "Formula",
                PreviewRow is null ? string.Empty : previewEvaluation.Value,
                analysis.UsedFields,
                missingFields,
                errors,
                BuildBindingStatusText(missingFields, errors, PreviewRow is not null));
        }

        var usedFields = BindingExpressionEvaluator.GetFields(expression);
        var missingPlaceholderFields = usedFields
            .Where(field => !FieldNameResolver.TryResolveFieldName(field, knownFields, out _))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        return new BindingPreviewResult(
            "Field placeholders",
            PreviewRow is null ? string.Empty : BindingExpressionEvaluator.Evaluate(expression, PreviewRow),
            usedFields,
            missingPlaceholderFields,
            Array.Empty<string>(),
            BuildBindingStatusText(missingPlaceholderFields, Array.Empty<string>(), PreviewRow is not null));
    }

    private IReadOnlyList<BindingIssueSummary> GetBindingIssues()
    {
        return Template.Objects
            .Where(item => item.IsVisible && !string.IsNullOrWhiteSpace(item.BindingExpression))
            .Select(item => new { Item = item, Binding = EvaluateBinding(item) })
            .Where(result => result.Binding.MissingFields.Count > 0 || result.Binding.Errors.Count > 0)
            .Select(result => new BindingIssueSummary(
                result.Item.Id,
                result.Item.Name,
                result.Item.Type.ToString(),
                result.Binding.KindText,
                result.Binding.StatusText,
                result.Binding.MissingFields,
                result.Binding.Errors))
            .ToArray();
    }

    private void SelectBindingIssue(BindingIssueSummary? issue)
    {
        if (issue is null)
        {
            return;
        }

        SelectedBindingIssue = issue;
        SelectedObject = Template.Objects.FirstOrDefault(item => item.Id == issue.ObjectId);
        if (SelectedObject is not null)
        {
            StatusText = $"Selected binding issue: {SelectedObject.Name}";
        }
    }

    private Dictionary<string, string> CreateBindingAnalysisRow()
    {
        var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var field in AvailableDatabaseFields)
        {
            if (!row.ContainsKey(field.Name))
            {
                row[field.Name] = PreviewRow is not null && FieldNameResolver.TryGetValue(PreviewRow, field.Name, out var value, out _)
                    ? value
                    : string.Empty;
            }
        }

        if (PreviewRow is not null)
        {
            foreach (var pair in PreviewRow)
            {
                if (!row.ContainsKey(pair.Key))
                {
                    row[pair.Key] = pair.Value;
                }
            }
        }

        return row;
    }

    private static string BuildBindingStatusText(IReadOnlyList<string> missingFields, IReadOnlyList<string> errors, bool hasPreviewRow)
    {
        if (missingFields.Count > 0)
        {
            return missingFields.Count == 1
                ? "Binding is missing 1 field in the current workbook."
                : $"Binding is missing {missingFields.Count} fields in the current workbook.";
        }

        if (errors.Count > 0)
        {
            return "Binding has validation errors.";
        }

        return hasPreviewRow
            ? "Binding is linked to the current Excel preview row."
            : "Binding is valid. Import or select an Excel row to preview output.";
    }

    private void RaiseFormulaPreviewChanged()
    {
        RefreshObjectTreeBindingStates();
        OnPropertyChanged(nameof(HasSelectedBinding));
        OnPropertyChanged(nameof(IsSelectedBindingFormula));
        OnPropertyChanged(nameof(SelectedBindingKindText));
        OnPropertyChanged(nameof(SelectedBindingPreviewValue));
        OnPropertyChanged(nameof(SelectedBindingUsedFieldsText));
        OnPropertyChanged(nameof(SelectedBindingMissingFieldsText));
        OnPropertyChanged(nameof(SelectedBindingUsedFieldsSummary));
        OnPropertyChanged(nameof(SelectedBindingMissingFieldsSummary));
        OnPropertyChanged(nameof(SelectedBindingStatusText));
        OnPropertyChanged(nameof(SelectedBindingErrorsText));
        OnPropertyChanged(nameof(BindingIssues));
        OnPropertyChanged(nameof(HasBindingIssues));
        OnPropertyChanged(nameof(BindingIssuesSummary));
        OnPropertyChanged(nameof(FormulaPreviewValue));
        OnPropertyChanged(nameof(FormulaPreviewErrors));
        OnPropertyChanged(nameof(FormulaPreviewUsedFields));
        RaiseFormulaBuilderChanged();
    }

    private void RefreshObjectTreeBindingStates()
    {
        var issuesByObjectId = GetBindingIssues()
            .ToDictionary(issue => issue.ObjectId, StringComparer.OrdinalIgnoreCase);

        foreach (var item in Template.Objects)
        {
            if (string.IsNullOrWhiteSpace(item.BindingExpression))
            {
                item.HasBindingIssue = false;
                item.BindingStateDisplayText = string.Empty;
                continue;
            }

            if (issuesByObjectId.TryGetValue(item.Id, out var issue))
            {
                item.HasBindingIssue = true;
                item.BindingStateDisplayText = BuildObjectTreeBindingIssueText(issue);
                continue;
            }

            item.HasBindingIssue = false;
            item.BindingStateDisplayText = FormulaBindingEvaluator.LooksLikeFormula(item.BindingExpression)
                ? "Formula linked"
                : "Linked Excel";
        }
    }

    private static string BuildObjectTreeBindingIssueText(BindingIssueSummary issue)
    {
        if (issue.MissingFields.Count > 0)
        {
            return issue.MissingFields.Count == 1
                ? $"Missing: {issue.MissingFields[0]}"
                : $"Missing {issue.MissingFields.Count} fields";
        }

        return issue.Errors.Count > 0
            ? "Formula error"
            : issue.StatusText;
    }

    private void RecordTemplateChange()
    {
        if (_isRestoringHistory)
        {
            return;
        }

        var currentSnapshot = CaptureTemplateSnapshot();
        if (currentSnapshot == _lastTemplateSnapshot)
        {
            return;
        }

        // Debounce: accumulate rapid changes into one undo step
        if (_debounceActive)
        {
            // Already waiting for debounce timer, just update pending snapshot
            _pendingSnapshot = currentSnapshot;
            return;
        }

        // First change in a burst: save the pre-change state and start debounce
        _debounceActive = true;
        var preChangeSnapshot = _lastTemplateSnapshot;
        _pendingSnapshot = currentSnapshot;
        _lastTemplateSnapshot = currentSnapshot;

        // Start timer - when it fires, commit the undo step
        _debounceTimer?.Stop();
        _debounceTimer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(300)
        };
        _debounceTimer.Tick += (_, _) =>
        {
            _debounceTimer.Stop();
            _debounceTimer = null;
            _debounceActive = false;

            // Push the pre-change snapshot (the state before the drag started)
            if (!string.IsNullOrEmpty(preChangeSnapshot))
            {
                // Only push if it's different from current
                var finalSnapshot = CaptureTemplateSnapshot();
                if (preChangeSnapshot != finalSnapshot)
                {
                    _undoStack.Push(preChangeSnapshot);
                    TrimUndoStack();
                    _redoStack.Clear();
                    _lastTemplateSnapshot = finalSnapshot;
                }
            }

            RaiseHistoryCanExecuteChanged();
        };
        _debounceTimer.Start();
    }

    private void RestoreTemplateSnapshot(string snapshot)
    {
        var selectedId = SelectedObject?.Id;
        var restored = JsonSerializer.Deserialize<LabelTemplate>(snapshot, HistoryJsonOptions);
        if (restored is null)
        {
            return;
        }

        _isRestoringHistory = true;
        try
        {
            UnobserveTemplate(Template);
            Template = restored;
            SelectedObject = string.IsNullOrWhiteSpace(selectedId)
                ? null
                : Template.Objects.FirstOrDefault(item => item.Id == selectedId);
            _lastTemplateSnapshot = snapshot;
        }
        finally
        {
            _isRestoringHistory = false;
        }
    }

    private string CaptureTemplateSnapshot()
    {
        return JsonSerializer.Serialize(Template, HistoryJsonOptions);
    }

    private void ResetHistory()
    {
        _undoStack.Clear();
        _redoStack.Clear();
        _lastTemplateSnapshot = CaptureTemplateSnapshot();
        RaiseHistoryCanExecuteChanged();
    }

    private void RaiseHistoryCanExecuteChanged()
    {
        ((RelayCommand)UndoCommand).RaiseCanExecuteChanged();
        ((RelayCommand)RedoCommand).RaiseCanExecuteChanged();
    }

    private void TrimUndoStack()
    {
        const int maxUndoSteps = 200;
        if (_undoStack.Count <= maxUndoSteps)
        {
            return;
        }

        var keep = _undoStack.Take(maxUndoSteps).Reverse().ToArray();
        _undoStack.Clear();
        foreach (var snapshot in keep)
        {
            _undoStack.Push(snapshot);
        }
    }

    private static LabelTemplate CreateDefaultTemplate()
    {
        return new LabelTemplate
        {
            Name = "ANLAbel Template",
            WidthMm = 100,
            HeightMm = 50,
            Dpi = 203,
            PrinterProfile = new PrinterProfile
            {
                Dpi = 203,
                LabelWidthMm = 100,
                LabelHeightMm = 50,
                ScaleX = 1,
                ScaleY = 1
            }
        };
    }

    private static IReadOnlyList<string> GetIndustrialFontFamilies()
    {
        var installedFonts = Fonts.SystemFontFamilies.Select(font => font.Source).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var fonts = PreferredIndustrialFonts.Where(installedFonts.Contains).ToList();
        if (fonts.Count == 0)
        {
            fonts.Add("Segoe UI");
        }

        return fonts;
    }

    private static IReadOnlyDictionary<string, string>? CreatePreviewRow(object? item)
    {
        if (item is not DataRowView rowView)
        {
            return null;
        }

        return rowView.Row.Table.Columns
            .Cast<DataColumn>()
            .ToDictionary(column => column.ColumnName, column => rowView.Row[column]?.ToString() ?? string.Empty, StringComparer.OrdinalIgnoreCase);
    }

    private static int GetDataRowViewIndex(DataRowView rowView)
    {
        for (var i = 0; i < rowView.DataView.Count; i++)
        {
            if (ReferenceEquals(rowView.DataView[i], rowView))
            {
                return i;
            }
        }

        return 0;
    }

    private IEnumerable<IReadOnlyDictionary<string, string>?> ExpandRowsForCopies(IEnumerable<IReadOnlyDictionary<string, string>?> rows)
    {
        foreach (var row in rows)
        {
            var copies = GetCopyCount(row);
            for (var i = 0; i < copies; i++)
            {
                yield return row;
            }
        }
    }

    private int GetCopyCount(IReadOnlyDictionary<string, string>? row)
    {
        if (row is not null && !string.IsNullOrWhiteSpace(PrintCopiesField))
        {
            var field = PrintCopiesField.Trim();
            if (field.StartsWith('{') && field.EndsWith('}') && field.Length > 2)
            {
                field = field[1..^1];
            }

            if (row.TryGetValue(field, out var value) && int.TryParse(value, out var fieldCopies) && fieldCopies > 0)
            {
                return Math.Min(999, fieldCopies);
            }
        }

        return Math.Min(999, Math.Max(1, PrintCopies));
    }

    public async Task WritePrintLogAsync(string printMode, IEnumerable<IReadOnlyDictionary<string, string>?> rows, int rowCount, int labelCount, string notes = "")
    {
        LogPrintOperation(printMode, rowCount, labelCount, success: true, errorMessage: string.Empty);
        try
        {
            var printedAt = DateTime.Now;
            var entries = rows.Select((row, index) => CreatePrintLogEntry(printMode, row, rowCount, labelCount, index + 1, printedAt, notes)).ToArray();
            await _printLogService.AppendManyAsync(entries);
        }
        catch (Exception ex)
        {
            StatusText = $"Print sent, but log failed: {ex.Message}";
        }
    }

    /// <summary>
    /// Fire-and-forget job-level print trace (print-preview-reliability-plan.md item 3).
    /// Separate from the per-label print-history.csv log written above — this is a
    /// machine-parseable JSON trace, never awaited and never allowed to affect the print
    /// job or the (already saved) human-facing history.
    /// </summary>
    private void LogPrintOperation(string printMode, int rowsSelected, int labelsPrinted, bool success, string errorMessage)
    {
        var entry = new PrintOperationLogEntry
        {
            TemplateName = Template.Name,
            TemplateFilePath = CurrentFilePath,
            PrinterName = Template.PrinterProfile.PrinterName,
            LabelWidthMm = Template.PrinterProfile.LabelWidthMm,
            LabelHeightMm = Template.PrinterProfile.LabelHeightMm,
            Dpi = Template.PrinterProfile.Dpi,
            PrintMode = printMode,
            RowsSelected = rowsSelected,
            LabelsPrinted = labelsPrinted,
            Success = success,
            ErrorMessage = errorMessage
        };
        _ = _printOperationLogService.AppendAsync(entry);
    }

    private PrintLogEntry CreatePrintLogEntry(string printMode, IReadOnlyDictionary<string, string>? row, int rowCount, int labelCount, int labelIndex, DateTime printedAt, string notes)
    {
        return new PrintLogEntry
        {
            PrintedAt = printedAt,
            TemplateName = Template.Name,
            TemplateFilePath = CurrentFilePath,
            PrinterName = Template.PrinterProfile.PrinterName,
            LabelWidthMm = Template.PrinterProfile.LabelWidthMm,
            LabelHeightMm = Template.PrinterProfile.LabelHeightMm,
            Dpi = Template.PrinterProfile.Dpi,
            PrintMode = printMode,
            RowCount = rowCount,
            LabelCount = labelCount,
            LabelIndex = labelIndex,
            ExcelFilePath = Template.DatabaseConfig.FilePath,
            ExcelSheetName = Template.DatabaseConfig.SheetName,
            PartNo = GetRowValue(row, "PartNo", "Part No", "PN", "MaHang", "Ma Hang", "Mã hàng"),
            ItemName = GetRowValue(row, "Name", "ItemName", "Item Name", "Ten", "Tên", "TenHang", "Ten Hang", "Tên hàng"),
            Lot = GetRowValue(row, "Lot", "LotNo", "Lot No", "Batch"),
            Quantity = GetRowValue(row, "Qty", "Quantity", "SoLuong", "So Luong", "Số lượng"),
            LabelContent = CreateLabelContent(row),
            RowData = row is null ? string.Empty : string.Join("; ", row.Select(pair => $"{pair.Key}={pair.Value}")),
            Notes = notes
        };
    }

    private string CreateLabelContent(IReadOnlyDictionary<string, string>? row)
    {
        return string.Join(" | ", Template.Objects
            .Where(item => item.IsVisible)
            .OrderBy(item => item.ZIndex)
            .Select(item => CreateObjectContent(item, row))
            .Where(value => !string.IsNullOrWhiteSpace(value)));
    }

    private static string CreateObjectContent(LabelObject item, IReadOnlyDictionary<string, string>? row)
    {
        var value = string.IsNullOrWhiteSpace(item.BindingExpression)
            ? item.Text
            : ResolveExpression(item.BindingExpression, row);
        return string.IsNullOrWhiteSpace(value) ? string.Empty : $"{item.Name}={value}";
    }

    private static string ResolveExpression(string expression, IReadOnlyDictionary<string, string>? row)
    {
        if (row is null)
        {
            return expression;
        }

        return FormulaBindingEvaluator.LooksLikeFormula(expression)
            ? FormulaBindingEvaluator.Evaluate(expression, row).Value
            : BindingExpressionEvaluator.Evaluate(expression, row);
    }

    private static string GetRowValue(IReadOnlyDictionary<string, string>? row, params string[] names)
    {
        if (row is null)
        {
            return string.Empty;
        }

        foreach (var name in names)
        {
            if (row.TryGetValue(name, out var value))
            {
                return value;
            }
        }

        var normalizedNames = names.Select(NormalizeKey).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in row)
        {
            if (normalizedNames.Contains(NormalizeKey(pair.Key)))
            {
                return pair.Value;
            }
        }

        return string.Empty;
    }

    private static string NormalizeKey(string value)
    {
        return new string(value.Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant).ToArray());
    }

    public void OpenPrintHistoryFile()
    {
        try
        {
            if (!File.Exists(_printLogService.LogFilePath))
            {
                StatusText = "Print history file has not been created yet";
                return;
            }

            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = _printLogService.LogFilePath,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            StatusText = $"Cannot open print history: {ex.Message}";
        }
    }
}

public sealed record NewTemplateRequest(string Name, double WidthMm, double HeightMm, int Dpi);

public sealed record FormulaFunctionTemplate(string Name, string Template, string Description);

public enum FormulaBuilderPartKind
{
    Field,
    Text
}

public sealed record FormulaBuilderPart(FormulaBuilderPartKind Kind, string Value, string DisplayText)
{
    public string KindText => Kind == FormulaBuilderPartKind.Field ? "Field" : "Text";
}

public sealed record BarcodeSymbologyOption(BarcodeSymbology Value, string GroupName, string DisplayName);

public sealed record BindingPreviewResult(
    string KindText,
    string PreviewValue,
    IReadOnlyList<string> UsedFields,
    IReadOnlyList<string> MissingFields,
    IReadOnlyList<string> Errors,
    string StatusText)
{
    public static BindingPreviewResult Empty { get; } = new(string.Empty, string.Empty, Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>(), string.Empty);
}

public sealed record BindingIssueSummary(
    string ObjectId,
    string ObjectName,
    string ObjectType,
    string BindingKind,
    string StatusText,
    IReadOnlyList<string> MissingFields,
    IReadOnlyList<string> Errors)
{
    public string Title => $"{ObjectName} ({ObjectType})";
    public string MissingFieldsSummary => MissingFields.Count == 0 ? string.Empty : $"Missing: {string.Join(", ", MissingFields)}";
    public string ErrorsSummary => Errors.Count == 0 ? string.Empty : string.Join(Environment.NewLine, Errors);
}
