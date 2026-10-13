using System.Collections.Specialized;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ANLAbel.Barcode.Options;
using ANLAbel.Barcode.Renderers;
using ANLAbel.Core.Barcode;
using ANLAbel.Core.Data;
using ANLAbel.Core.Enums;
using ANLAbel.Core.Expressions;
using ANLAbel.Core.Expressions.Formulas;
using ANLAbel.Core.Geometry;
using ANLAbel.Core.Models;
using ANLAbel.Core.Mvvm;
using ANLAbel.Core.Printing;
using ANLAbel.Core.Text;
using ANLAbel.Data;
using ANLAbel.Data.DataLogs;
using ANLAbel.Data.Excel;
using ANLAbel.Data.PrintLogs;
using ANLAbel.Project.SaveLoad;
using ANLAbel.Printing.PrinterProfiles;
using ANLAbel.Printing.RenderPipeline;
using ANLAbel.App;

namespace ANLAbel.App.ViewModels;

public enum ExcelLinkVerificationState
{
    NotLinked,
    Checking,
    Verified,
    Stale,
    Failed
}

public sealed class MainViewModel : ObservableObject
{
    private readonly IProjectFileService _projectFileService;
    private readonly ExcelDataService _excelDataService;
    private readonly PrintService _printService;
    private readonly PrinterDiscoveryService _printerDiscoveryService;
    private readonly IPrinterQueueLookup _printerQueueLookup;
    private readonly PrintLogService _printLogService;
    private readonly DataOperationLogService _dataOperationLogService;
    private readonly PrintOperationLogService _printOperationLogService;
    private readonly PrintJobStateStore _printJobStateStore = new();
    private PrintJobRecoveryReport _printRecoveryReport = PrintJobRecoveryReport.Empty;
    private PrinterQueueLookupResult _printerQueueStatus = PrinterQueueLookupResult.Missing(
        string.Empty,
        "No verified printer queue is selected.");
    private readonly DataSourceRegistry _dataSourceRegistry;
    private readonly IBarcodeRenderer _barcodeValidator = new ZxingBarcodeRenderer();
    private readonly QrCapacityTable _qrCapacityTable = new();
    private readonly HashSet<LabelObject> _applyingQrAutoSize = new();
    private readonly Stack<string> _undoStack = new();
    private readonly Stack<string> _redoStack = new();
    // Plan 13 F01: the history snapshot of the document as last saved/opened.
    // IsDirty compares the current snapshot with it, so undoing back to the
    // saved state clears the unsaved marker.
    private string _savedTemplateSnapshot = string.Empty;
    private bool _isDirty;
    private bool _isSaving;
    private readonly List<string> _startupNotices = new();
    private System.Windows.Threading.DispatcherTimer? _debounceTimer;
    private bool _debounceActive;
    private string _pendingPreChangeSnapshot = string.Empty;
    private string _pendingSnapshot = string.Empty;
    private bool _explicitEditGestureActive;
    private string _explicitEditGesturePreChangeSnapshot = string.Empty;
    private LabelTemplate _template = CreateDefaultTemplate();
    private LabelObject? _selectedObject;
    private DataView? _excelDataView;
    private IDataConnector? _dataConnector;
    private string _dataTransformError = string.Empty;
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
    private FileSourceIdentity? _excelDataSourceIdentity;
    private FileSystemWatcher? _excelFileWatcher;
    private readonly object _excelStaleDebounceLock = new();
    private System.Threading.Timer? _excelStaleDebounceTimer;
    private bool _isExcelDataStale;
    private ExcelLinkVerificationState _excelLinkVerificationState = ExcelLinkVerificationState.NotLinked;
    private string _excelLinkVerificationFailureMessage = string.Empty;
    private DateTime? _excelLinkVerifiedAtLocal;
    private int _excelLinkVerifiedRowCount;
    private int _excelLinkVerifiedColumnCount;

    private static readonly JsonSerializerOptions HistoryJsonOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new JsonStringEnumConverter() }
    };

    public MainViewModel()
        : this(new ProjectFileService(), new ExcelDataService(), new PrintService(), new PrinterDiscoveryService(), new PrintLogService(), new DataOperationLogService())
    {
    }

    public MainViewModel(IProjectFileService projectFileService, ExcelDataService excelDataService, PrintService printService, PrinterDiscoveryService printerDiscoveryService, PrintLogService printLogService, DataOperationLogService? dataOperationLogService = null, DataSourceRegistry? dataSourceRegistry = null, PrintOperationLogService? printOperationLogService = null, IPrinterQueueLookup? printerQueueLookup = null)
    {
        _projectFileService = projectFileService;
        _excelDataService = excelDataService;
        _printService = printService;
        _printerDiscoveryService = printerDiscoveryService;
        _printerQueueLookup = printerQueueLookup ?? new WindowsPrinterQueueLookup();
        _printLogService = printLogService;
        _printOperationLogService = printOperationLogService ?? new PrintOperationLogService();
        _dataOperationLogService = dataOperationLogService ?? new DataOperationLogService();
        _dataSourceRegistry = dataSourceRegistry ?? new DataSourceRegistry();
        var registryOutcome = _dataSourceRegistry.LoadOrQuarantine();
        if (!registryOutcome.IsLoaded)
        {
            _startupNotices.Add(registryOutcome.QuarantinePath is { } quarantinePath
                ? $"Tệp cấu hình nguồn dữ liệu dùng chung bị hỏng nên không đọc được. ANLAbel đã cất tệp này thành {Path.GetFileName(quarantinePath)} và dùng danh sách trống. Mẫu tem không bị ảnh hưởng."
                : "Không đọc được tệp cấu hình nguồn dữ liệu dùng chung (tệp đang bị khóa hoặc không có quyền). Phiên này dùng danh sách trống; tệp không bị thay đổi.");
        }
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
        RefreshExcelDataCommand = new AsyncRelayCommand(() => RefreshExcelDataAsync(), CanRefreshExcelData);
        VerifyExcelLinkCommand = new AsyncRelayCommand(() => VerifyExcelLinkAsync());
        PrintCurrentRowCommand = new AsyncRelayCommand(PrintCurrentRowAsync);
        PrintAllRowsCommand = new AsyncRelayCommand(PrintAllRowsAsync, () => ExcelDataView is not null && ExcelDataView.Count > 0);
        PrintCalibrationCommand = new AsyncRelayCommand(PrintCalibrationAsync);
        HideToolboxCommand = new RelayCommand(() => IsToolboxVisible = false);
        HidePropertiesCommand = new RelayCommand(() => IsPropertiesVisible = false);
        ShowAllPanelsCommand = new RelayCommand(ShowAllPanels);
        InsertFunctionFormulaCommand = new RelayCommand(parameter => InsertFunctionFormula(GetFormulaText(parameter)), _ => SelectedObject is not null);
        SelectBindingIssueCommand = new RelayCommand(parameter => SelectBindingIssue(parameter as BindingIssueSummary), parameter => parameter is BindingIssueSummary);
        RelinkExcelCommand = new AsyncRelayCommand(() => RelinkExcelAsync(), () => HasLinkedExcelSource && IsExcelLinkBroken);
        AddCurrentAsDataSourceCommand = new RelayCommand(AddCurrentAsDataSource, () => HasLinkedExcelSource && !IsExcelLinkBroken);
        UseDataSourceCommand = new AsyncRelayCommand(async parameter => { if (parameter is DataSource source) { await UseDataSourceAsync(source); } }, parameter => parameter is DataSource);
        RemoveDataSourceCommand = new RelayCommand(parameter => RemoveDataSource(parameter as DataSource), parameter => parameter is DataSource);
        RelinkDataSourceCommand = new AsyncRelayCommand(async parameter => { if (parameter is DataSource source) { await RelinkDataSourceAsync(source); } }, parameter => parameter is DataSource);
        ObserveTemplate(Template);
        _lastTemplateSnapshot = CaptureTemplateSnapshot();
        _savedTemplateSnapshot = _lastTemplateSnapshot;
    }

    public LabelTemplate Template
    {
        get => _template;
        private set
        {
            var oldTemplate = _template;
            NormalizeTextObjectPolicies(value);
            if (SetProperty(ref _template, value))
            {
                UnobserveTemplate(oldTemplate);
                ObserveTemplate(value);
                _lastTemplateSnapshot = CaptureTemplateSnapshot();
                OnPropertyChanged(nameof(SelectedKeyFieldName));
                OnPropertyChanged(nameof(SelectedCopiesFieldName));
                OnPropertyChanged(nameof(DataTransforms));
            }
        }
    }


    public UiStrings Ui => UiStrings.Current;
    public IEnumerable<DataTransformDefinition> DataTransforms => Template.DataTransforms;
    public bool TryBuildPrintPreviewRows(
        out IReadOnlyList<IReadOnlyDictionary<string, string>?> rows,
        out string error)
    {
        error = string.Empty;
        if (ExcelDataView is null || ExcelDataView.Count == 0)
        {
            rows = new IReadOnlyDictionary<string, string>?[] { PreviewRow };
            return true;
        }

        var prepared = new List<IReadOnlyDictionary<string, string>?>();
        foreach (DataRowView rowView in ExcelDataView)
        {
            var row = CreatePreviewRow(rowView, out var rowError);
            if (!string.IsNullOrWhiteSpace(rowError))
            {
                rows = Array.Empty<IReadOnlyDictionary<string, string>?>();
                error = rowError;
                return false;
            }

            prepared.Add(row);
        }

        rows = prepared;
        return true;
    }

    public FileSourceIdentity? GetExcelDataSourceIdentity()
    {
        return FileSourceIdentity.TryCapture(Template.DatabaseConfig?.FilePath, out var identity)
            ? identity
            : null;
    }

    public async Task<HistorySnapshot> ReadPrintHistorySnapshotAsync(CancellationToken cancellationToken = default)
    {
        var state = await _printJobStateStore.ReadRecoverySnapshotAsync(cancellationToken).ConfigureAwait(true);
        var operations = await _printOperationLogService.ReadAllAsync(cancellationToken).ConfigureAwait(true);
        var csv = await _printLogService.ReadSummariesAsync(cancellationToken).ConfigureAwait(true);
        return new HistorySnapshot(state, operations.Entries, operations.Diagnostics, csv.Entries, csv.Diagnostics);
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
                OnPropertyChanged(nameof(SelectedObjectIconPath));
                RaiseFormulaPreviewChanged();
                OnPropertyChanged(nameof(BarcodeValidationMessage));
                OnPropertyChanged(nameof(TextBoxValidationMessage));
                OnPropertyChanged(nameof(BarcodeModuleSizeWarningText));
                OnPropertyChanged(nameof(BarcodeEffectiveModuleReadoutText));
                OnPropertyChanged(nameof(BarcodePhysicalQuietZoneText));
                OnPropertyChanged(nameof(SelectedObjectSizeFromX));
                TryApplySizedFromXWidth(SelectedObject);
            }
        }
    }

    /// <summary>
    /// Properties checkbox: size linear barcode width from quantized X × logical modules.
    /// Maps to <see cref="BarcodeWidthMode.SizedFromX"/> when checked.
    /// </summary>
    public bool SelectedObjectSizeFromX
    {
        get => SelectedObject is { } item
            && item.BarcodeWidthMode == BarcodeWidthMode.SizedFromX;
        set
        {
            if (SelectedObject is not { } item
                || item.Type != ObjectType.BarcodeCode128
                || item.IsSquare2DCodeLike())
            {
                return;
            }

            var mode = value ? BarcodeWidthMode.SizedFromX : BarcodeWidthMode.FrameOwned;
            if (item.BarcodeWidthMode == mode)
            {
                return;
            }

            item.BarcodeWidthMode = mode;
            if (value && item.BarcodeModuleWidthMm <= 0)
            {
                item.BarcodeModuleWidthMm = LinearBarcodeModuleContract.RecommendedDefaultXDimensionMm;
            }

            TryApplySizedFromXWidth(item);
            OnPropertyChanged(nameof(SelectedObjectSizeFromX));
            OnPropertyChanged(nameof(BarcodeEffectiveModuleReadoutText));
            OnPropertyChanged(nameof(BarcodePhysicalQuietZoneText));
            OnPropertyChanged(nameof(BarcodeModuleSizeWarningText));
        }
    }

    private void TryApplySizedFromXWidth(LabelObject? item)
    {
        if (item is null)
        {
            return;
        }

        var planDpi = Template.PrinterProfile.Dpi > 0
            ? Template.PrinterProfile.Dpi
            : Template.Dpi > 0 ? Template.Dpi : item.QrDpi;
        LinearBarcodeProductionWidth.TryApplySizedFromXWidth(item, _barcodeValidator, planDpi);
    }

    public DataView? ExcelDataView
    {
        get => _excelDataView;
        private set
        {
            if (SetProperty(ref _excelDataView, value))
            {
                OnPropertyChanged(nameof(HasExcelData));
                RaiseCommandCanExecuteChanged(PrintAllRowsCommand);
            }
        }
    }

    public bool HasExcelData => ExcelDataView is not null;
    /// <summary>
    /// Read-only typed view over the currently imported Excel/CSV data. The
    /// existing DataView remains for WPF compatibility during the R4 migration.
    /// </summary>
    public IDataConnector? DataConnector
    {
        get => _dataConnector;
        private set => SetProperty(ref _dataConnector, value);
    }

    public string DataTransformError
    {
        get => _dataTransformError;
        private set
        {
            if (SetProperty(ref _dataTransformError, value))
            {
                OnPropertyChanged(nameof(HasDataTransformError));
            }
        }
    }

    public bool HasDataTransformError => !string.IsNullOrWhiteSpace(DataTransformError);
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
    public IReadOnlyList<double> FontSizes { get; } = TextStylePickerCatalog.StandardSizesPt;
    public IReadOnlyList<QrOptionItem<QrSizingMode>> QrSizingModeOptions { get; } = QrOptionLists.SizingModes;
    public IReadOnlyList<QrOptionItem<QrErrorCorrection>> QrErrorCorrectionOptions { get; } = QrOptionLists.ErrorCorrections;
    public IReadOnlyList<QrOptionItem<int>> QrVersionOptions { get; } = QrOptionLists.Versions;
    public IReadOnlyList<int> QrModuleSizePxOptions { get; } = QrOptionLists.ModuleSizesPx;
    public IReadOnlyList<int> QrQuietZoneModuleOptions { get; } = QrOptionLists.QuietZoneModules;
    public IReadOnlyList<BarcodeApplicationProfile> BarcodeApplicationProfiles { get; } = Enum.GetValues<BarcodeApplicationProfile>();
    public IReadOnlyList<BarcodeHriPlacement> BarcodeHriPlacementOptions { get; } =
    [
        BarcodeHriPlacement.None,
        BarcodeHriPlacement.Below,
        BarcodeHriPlacement.Above
    ];
    public IReadOnlyList<BarcodeCheckDigitPolicy> BarcodeCheckDigitPolicyOptions { get; } =
    [
        BarcodeCheckDigitPolicy.None,
        BarcodeCheckDigitPolicy.Auto,
        BarcodeCheckDigitPolicy.Verify
    ];
    public IReadOnlyList<Code39WideNarrowRatio> Code39WideNarrowRatioOptions { get; } =
    [
        Code39WideNarrowRatio.LegacyEngineDefault,
        Code39WideNarrowRatio.Ratio2_0,
        Code39WideNarrowRatio.Ratio2_2,
        Code39WideNarrowRatio.Ratio2_5,
        Code39WideNarrowRatio.Ratio3_0
    ];
    public IReadOnlyList<BearerBarStyle> BearerBarStyleOptions { get; } =
    [
        BearerBarStyle.None,
        BearerBarStyle.TopBottom,
        BearerBarStyle.Frame
    ];
    public IReadOnlyList<ImageRasterMode> ImageRasterModes { get; } = Enum.GetValues<ImageRasterMode>();
    public IReadOnlyList<TextAlignmentMode> TextAlignments { get; } = Enum.GetValues<TextAlignmentMode>();
    public IReadOnlyList<TextDirectionMode> TextDirections { get; } = Enum.GetValues<TextDirectionMode>();
    public IReadOnlyList<TextSizingMode> TextBoxSizingModes { get; } =
    [
            TextSizingMode.FixedFrame,
            TextSizingMode.ShrinkFont,
        TextSizingMode.ScaleWidth
    ];
    public IReadOnlyList<TextOverflowMode> TextBoxOverflowModes { get; } =
    [
        TextOverflowMode.Error,
        TextOverflowMode.Clip,
        TextOverflowMode.Ellipsis
    ];
    public IReadOnlyList<TextVerticalAlignmentMode> TextVerticalAlignments { get; } = Enum.GetValues<TextVerticalAlignmentMode>();
    public IReadOnlyList<OutlineStyle> OutlineStyles { get; } = Enum.GetValues<OutlineStyle>();
    public IReadOnlyList<FillStyle> FillStyles { get; } = Enum.GetValues<FillStyle>();
    public IReadOnlyList<FormulaFunctionTemplate> FormulaFunctions { get; } =
    [
        new("Sản xuất QR/Mã vạch", "CONCAT(FIELD(\"part\"), FIELD(\"partname\"), FIELD(\"lot\"))", "Các trường bắt buộc: part + partname + lot. Ví dụ đầu ra: P001Product AL01."),
        new("Sản xuất QR/Mã vạch - có nhãn", "CONCAT(\"Mã: \", FIELD(\"part\"), \" | Tên: \", FIELD(\"partname\"), \" | Lô: \", FIELD(\"lot\"))", "Đầu ra dễ đọc kèm nhãn và dấu phân cách."),
        new("FIELD", "FIELD(\"Name\")", "Lấy giá trị từ một cột Excel."),
        new("CONCAT", "CONCAT(\"Tên: \", FIELD(\"Name\"), \", Mã: \", FIELD(\"Code\"))", "Ghép văn bản cố định và các trường.")
    ];

    public string FormulaPreviewValue => EvaluateSelectedFormula().Value;
    public string FormulaPreviewErrors => string.Join(Environment.NewLine, EvaluateSelectedFormula().Errors);
    public string FormulaPreviewUsedFields => string.Join(", ", EvaluateSelectedFormula().UsedFields);
    public string FormulaBuilderExpression => BuildFormulaExpression();
    public string FormulaBuilderPreviewValue => EvaluateFormulaBuilder().Value;
    public string FormulaBuilderPreviewErrors => string.Join(Environment.NewLine, EvaluateFormulaBuilder().Errors);
    public string BarcodeValidationMessage => ValidateSelectedBarcode();
    public string BarcodeApplicationValidationMessage => ValidateSelectedBarcodeApplication();

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
            if (SelectedObject is not { } item)
            {
                return string.Empty;
            }

            // Match PrintService.CreatePlan's DPI resolution (PrinterProfile.Dpi first,
            // then Template.Dpi) so this Designer-side warning agrees with the DPI the
            // preflight check will actually enforce at print time.
            var printDpi = Template.PrinterProfile.Dpi > 0
                ? Template.PrinterProfile.Dpi
                : Template.Dpi > 0 ? Template.Dpi : item.QrDpi;
            if (printDpi <= 0)
            {
                return string.Empty;
            }

            // Matrix fixed module (existing policy).
            if (IsSquare2DCodeLike(item) && item.QrSizingMode == QrSizingMode.FixedVersionAndModuleSize && item.QrDpi > 0)
            {
                var effectiveDots = item.QrModuleSizePx * (double)printDpi / item.QrDpi;
                return effectiveDots < 2
                    ? $"⚠ Kích thước module chỉ khoảng ~{effectiveDots:0.#} dot ở {printDpi} DPI — có nguy cơ khó quét. Hãy tăng px của Module hoặc tăng DPI."
                    : string.Empty;
            }

            // Linear 1D: authored X-dimension (mm) quantized at print DPI.
            if (item.Type == ObjectType.BarcodeCode128
                && !IsSquare2DCodeLike(item)
                && item.BarcodeModuleWidthMm > 0)
            {
                try
                {
                    var resolution = LinearBarcodeModuleContract.Resolve(item.BarcodeModuleWidthMm, printDpi);
                    var message = LinearBarcodeModuleContract.FormatIndustrialRiskMessage(resolution);
                    return string.IsNullOrEmpty(message) ? string.Empty : "⚠ " + message;
                }
                catch
                {
                    return string.Empty;
                }
            }

            return string.Empty;
        }
    }

    /// <summary>
    /// P1.b: read-only industrial module readout for linear barcodes — same
    /// <see cref="LinearBarcodeModuleContract.Resolve"/> math as preflight/warning.
    /// Empty when no linear selection or DPI is unavailable.
    /// </summary>
    public string BarcodeEffectiveModuleReadoutText
    {
        get
        {
            if (SelectedObject is not { } item
                || item.Type != ObjectType.BarcodeCode128
                || IsSquare2DCodeLike(item))
            {
                return string.Empty;
            }

            var printDpi = Template.PrinterProfile.Dpi > 0
                ? Template.PrinterProfile.Dpi
                : Template.Dpi > 0 ? Template.Dpi : item.QrDpi;
            if (printDpi <= 0)
            {
                return string.Empty;
            }

            try
            {
                LinearBarcodeModuleResolution resolution;
                if (item.BarcodeModuleWidthMm > 0)
                {
                    resolution = LinearBarcodeModuleContract.Resolve(item.BarcodeModuleWidthMm, printDpi);
                }
                else
                {
                    var sample = string.IsNullOrEmpty(item.Text) ? "0" : item.Text;
                    var type = ANLAbel.App.Controls.BarcodeTypeMapper.ToRendererType(item.BarcodeSymbology);
                    if (!_barcodeValidator.ValidateData(sample, type))
                    {
                        return string.Empty;
                    }

                    var options = new BarcodeRenderOptions
                    {
                        QuietZoneModules = Math.Max(0, item.QrQuietZoneModules),
                        IsGs1 = item.BarcodeApplicationProfile == BarcodeApplicationProfile.Gs1
                    };
                    var modules = _barcodeValidator.CountLinearModules(sample, type, options);
                    if (modules is null or <= 0 || item.WidthMm <= 0)
                    {
                        return string.Empty;
                    }

                    resolution = LinearBarcodeModuleContract.ResolveForObject(
                        authoredModuleWidthMm: 0,
                        frameWidthMm: item.WidthMm,
                        totalModules: modules.Value,
                        dpi: printDpi);
                }

                var mils = resolution.EffectiveModuleWidthMm / LinearBarcodeModuleContract.MillimetersPerInch * 1000.0;
                return $"Kích thước X hiệu dụng: {resolution.EffectiveModuleWidthMm:0.###} mm ({mils:0.#} mil) · {resolution.ModuleDots} dot @ {resolution.Dpi} DPI";
            }
            catch
            {
                return string.Empty;
            }
        }
    }

    /// <summary>
    /// P4.c: Physical quiet zone per side (mm) at current print DPI.
    /// </summary>
    public string BarcodePhysicalQuietZoneText
    {
        get
        {
            if (SelectedObject is not { } item
                || item.Type != ObjectType.BarcodeCode128
                || IsSquare2DCodeLike(item))
            {
                return string.Empty;
            }

            var printDpi = Template.PrinterProfile.Dpi > 0
                ? Template.PrinterProfile.Dpi
                : Template.Dpi > 0 ? Template.Dpi : item.QrDpi;
            if (printDpi <= 0)
            {
                return string.Empty;
            }

            try
            {
                if (item.BarcodeModuleWidthMm > 0)
                {
                    var resolution = LinearBarcodeModuleContract.Resolve(item.BarcodeModuleWidthMm, printDpi);
                    var observedQzMm = Code39RatioContract.ObservedQuietZoneMmPerSide(item.QrQuietZoneModules, resolution);
                    return $"{observedQzMm:0.##} mm ({item.QrQuietZoneModules} modules)";
                }

                return $"{item.QrQuietZoneModules} modules (legacy frame estimate)";
            }
            catch
            {
                return string.Empty;
            }
        }
    }

    public string TextBoxValidationMessage => ValidateSelectedTextBox();
    public string SelectedObjectIconPath => SelectedObject?.Type switch
    {
        ObjectType.Text => "Icons/static_text.png",
        ObjectType.TextBox => "Icons/text_box.png",
        ObjectType.BarcodeCode128 => "Icons/barcode.png",
        ObjectType.QRCode => "Icons/qr_code.png",
        ObjectType.DataMatrix => "Icons/data_matrix.png",
        ObjectType.Rectangle => "Icons/rectangle.png",
        ObjectType.Ellipse => "Icons/ellipse.png",
        ObjectType.Line => "Icons/line.png",
        ObjectType.Image => "Icons/image.png",
        _ => "Icons/cursor_select.png"
    };
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
                RaiseCommandCanExecuteChanged(RelinkExcelCommand);
            }
        }
    }

    public string LinkedExcelSourceText => HasLinkedExcelSource
        ? $"{Path.GetFileName(Template.DatabaseConfig.FilePath)} / {Template.DatabaseConfig.SheetName}"
        : "Chưa liên kết tệp Excel";
    public string ExcelLinkStatusText => IsExcelLinkBroken
        ? "⚠ Liên kết Excel bị hỏng — nhấn Liên kết lại để tìm tệp"
        : string.Empty;
    public string ExcelDataFreshnessText => HasLinkedExcelSource && !IsExcelLinkBroken && _excelDataReadAtLocal is not null
        ? $"Dữ liệu đọc lúc {_excelDataReadAtLocal.Value:HH:mm:ss}"
        : string.Empty;
    public ExcelLinkVerificationState ExcelLinkVerificationState
    {
        get => _excelLinkVerificationState;
        private set
        {
            if (SetProperty(ref _excelLinkVerificationState, value))
            {
                OnPropertyChanged(nameof(ExcelLinkVerificationTitle));
                OnPropertyChanged(nameof(ExcelLinkVerificationDetail));
                OnPropertyChanged(nameof(ExcelLinkVerificationActionText));
                OnPropertyChanged(nameof(ExcelLinkVerificationTrustText));
            }
        }
    }

    public string ExcelLinkVerificationTitle => ExcelLinkVerificationState switch
    {
        ExcelLinkVerificationState.Checking => "Đang kiểm tra liên kết Excel...",
        ExcelLinkVerificationState.Verified => "Liên kết Excel đã xác thực",
        ExcelLinkVerificationState.Stale => "Tệp Excel đã thay đổi sau khi xác thực",
        ExcelLinkVerificationState.Failed => "Xác thực liên kết Excel thất bại",
        _ => "Chưa liên kết nguồn Excel"
    };

    public string ExcelLinkVerificationDetail => ExcelLinkVerificationState switch
    {
        ExcelLinkVerificationState.Checking => "Đang mở sổ làm việc và kiểm tra trang tính đã chọn.",
        ExcelLinkVerificationState.Verified => $"{Path.GetFileName(Template.DatabaseConfig.FilePath)}  ·  Trang tính: {Template.DatabaseConfig.SheetName}",
        ExcelLinkVerificationState.Stale => "Dữ liệu dòng đã nạp có thể không còn khớp với sổ làm việc.",
        ExcelLinkVerificationState.Failed => string.IsNullOrWhiteSpace(_excelLinkVerificationFailureMessage)
            ? "Không thể kiểm tra tệp hoặc trang tính đã chọn."
            : _excelLinkVerificationFailureMessage,
        _ => "Liên kết sổ làm việc trước khi sử dụng dữ liệu dòng."
    };

    public string ExcelLinkVerificationActionText => ExcelLinkVerificationState switch
    {
        ExcelLinkVerificationState.Checking => "Đang kiểm tra...",
        ExcelLinkVerificationState.Stale => "Cập nhật & xác thực",
        ExcelLinkVerificationState.NotLinked => "Liên kết Excel...",
        _ => "Kiểm tra lại liên kết Excel"
    };

    public string ExcelLinkVerificationTrustText => ExcelLinkVerificationState switch
    {
        ExcelLinkVerificationState.Checking => "Vui lòng đợi — lệnh in tạm thời bị chặn.",
        ExcelLinkVerificationState.Verified => $"{_excelLinkVerifiedColumnCount} cột · {_excelLinkVerifiedRowCount} dòng · đã kiểm tra {_excelLinkVerifiedAtLocal:HH:mm:ss}",
        ExcelLinkVerificationState.Stale => "Lệnh in tiếp tục bị chặn cho đến khi dữ liệu được làm mới.",
        ExcelLinkVerificationState.Failed => "Liên kết lại sổ làm việc nếu đường dẫn hoặc trang tính bị thay đổi.",
        _ => "Kiểm tra tệp, trang tính và dòng tiêu đề trước khi dùng."
    };
    public string CurrentExcelRowText => ExcelDataView is null || ExcelDataView.Count == 0 || SelectedDataItem is not DataRowView rowView
        ? "Chưa chọn dòng Excel nào"
        : $"Dòng {GetDataRowViewIndex(rowView) + 1} / {ExcelDataView.Count}";

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
                if (value && HasLinkedExcelSource)
                {
                    ExcelLinkVerificationState = ExcelLinkVerificationState.Stale;
                }
            }
        }
    }

    public string ExcelStaleNoticeText => IsExcelDataStale
        ? "⚠ Tệp Excel trên đĩa đã thay đổi — nhấn Cập nhật Excel để làm mới"
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
    public string SelectedBindingUsedFieldsSummary => string.IsNullOrWhiteSpace(SelectedBindingUsedFieldsText) ? string.Empty : $"Trường liên kết: {SelectedBindingUsedFieldsText}";
    public string SelectedBindingMissingFieldsSummary => string.IsNullOrWhiteSpace(SelectedBindingMissingFieldsText) ? string.Empty : $"Trường còn thiếu: {SelectedBindingMissingFieldsText}";
    public string SelectedBindingStatusText => EvaluateSelectedBinding().StatusText;
    public string SelectedBindingErrorsText => string.Join(Environment.NewLine, EvaluateSelectedBinding().Errors);
    public IReadOnlyList<BindingIssueSummary> BindingIssues => GetBindingIssues();
    public bool HasBindingIssues => BindingIssues.Count > 0;
    public string BindingIssuesSummary => HasBindingIssues
        ? $"{BindingIssues.Count} đối tượng bị hỏng hoặc chưa hoàn thiện liên kết Excel."
        : "Tất cả liên kết Excel đều khớp với sổ làm việc hiện tại.";
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
        private set
        {
            if (SetProperty(ref _currentFilePath, value))
            {
                OnPropertyChanged(nameof(DocumentDisplayName));
                OnPropertyChanged(nameof(WindowTitle));
            }
        }
    }

    /// <summary>True when the document differs from its last saved/opened state (plan 13 F01).</summary>
    public bool IsDirty
    {
        get => _isDirty;
        private set
        {
            if (SetProperty(ref _isDirty, value))
            {
                OnPropertyChanged(nameof(WindowTitle));
            }
        }
    }

    /// <summary>True while a save is writing; a second save request is refused (plan 13 F07).</summary>
    public bool IsSaving
    {
        get => _isSaving;
        private set => SetProperty(ref _isSaving, value);
    }

    /// <summary>File name of the open document, or its label name when it was never saved.</summary>
    public string DocumentDisplayName => !string.IsNullOrWhiteSpace(CurrentFilePath)
        ? Path.GetFileNameWithoutExtension(CurrentFilePath)
        : string.IsNullOrWhiteSpace(Template?.Name) ? "Mẫu mới" : Template.Name;

    /// <summary>Window title, e.g. "Tem kho 100x50* — ANLAbel - Thiết kế tem v0.306".</summary>
    public string WindowTitle => $"{DocumentDisplayName}{(IsDirty ? "*" : string.Empty)} — {ApplicationTitle}";

    public static string ApplicationTitle { get; } = BuildApplicationTitle();

    /// <summary>Problems found while starting (e.g. a quarantined registry), shown once after the window opens.</summary>
    public IReadOnlyList<string> StartupNotices => _startupNotices;

    private static string BuildApplicationTitle()
    {
        var version = typeof(MainViewModel).Assembly.GetName().Version;
        return version is null
            ? "ANLAbel - Thiết kế tem"
            : string.Create(System.Globalization.CultureInfo.InvariantCulture, $"ANLAbel - Thiết kế tem v{version.Major}.{version.Minor:000}");
    }

    /// <summary>Marks the current document state as saved (after save, open, new or library load).</summary>
    public void MarkDocumentClean()
    {
        CommitPendingHistory();
        _savedTemplateSnapshot = CaptureTemplateSnapshot();
        IsDirty = false;
        OnPropertyChanged(nameof(DocumentDisplayName));
        OnPropertyChanged(nameof(WindowTitle));
    }

    /// <summary>Exact check used before New/Open/Close: compares a fresh snapshot with the saved one.</summary>
    public bool HasUnsavedChanges()
    {
        CommitPendingHistory();
        var dirty = !string.Equals(CaptureTemplateSnapshot(), _savedTemplateSnapshot, StringComparison.Ordinal);
        IsDirty = dirty;
        return dirty;
    }

    /// <summary>
    /// Immutable copy of the open document for the recovery store, or null when
    /// there is nothing unsaved to protect. Safe to serialize off the UI thread.
    /// </summary>
    public LabelTemplate? CaptureRecoveryCopy()
    {
        var snapshot = CaptureTemplateSnapshot();
        if (string.Equals(snapshot, _savedTemplateSnapshot, StringComparison.Ordinal))
        {
            return null;
        }

        return JsonSerializer.Deserialize<LabelTemplate>(snapshot, HistoryJsonOptions);
    }

    /// <summary>
    /// Opens a recovery copy (plan 13 F04) as an unsaved document. The original
    /// path is kept when it still exists, so Save writes back there (its previous
    /// content goes to .bak as usual); otherwise Save asks for a location.
    /// </summary>
    public async Task OpenRecoveredAsync(string recoveryDocumentPath, string? originalFilePath, string documentName)
    {
        var recovered = await _projectFileService.LoadAsync(recoveryDocumentPath);
        StopWatchingExcelFile();
        UnobserveTemplate(Template);
        Template = recovered;
        ResetHistory();
        CurrentFilePath = !string.IsNullOrWhiteSpace(originalFilePath) && File.Exists(originalFilePath)
            ? originalFilePath
            : string.Empty;
        SelectedObject = Template.Objects.OrderByDescending(item => item.ZIndex).FirstOrDefault();
        await RestoreLinkedExcelDataAsync();
        CommitPendingHistory();
        _savedTemplateSnapshot = string.Empty;
        UpdateDirtyState(CaptureTemplateSnapshot());
        StatusText = string.IsNullOrWhiteSpace(CurrentFilePath)
            ? $"Đã mở bản phục hồi của “{documentName}”. Bấm Lưu để chọn nơi lưu."
            : $"Đã mở bản phục hồi của “{documentName}”. Bấm Lưu để ghi lại {Path.GetFileName(CurrentFilePath)} (bản cũ giữ trong .bak).";
    }

    private void UpdateDirtyState(string currentSnapshot)
    {
        IsDirty = !string.Equals(currentSnapshot, _savedTemplateSnapshot, StringComparison.Ordinal);
        OnPropertyChanged(nameof(WindowTitle));
    }

    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    /// <summary>
    /// Read-only availability evidence for the queue saved in the current
    /// template. A missing named queue is a repair warning; it must never be
    /// silently replaced with the Windows default queue.
    /// </summary>
    public PrinterQueueLookupResult PrinterQueueStatus
    {
        get => _printerQueueStatus;
        private set
        {
            if (SetProperty(ref _printerQueueStatus, value))
            {
                OnPropertyChanged(nameof(HasPrinterQueueWarning));
                OnPropertyChanged(nameof(PrinterQueueStatusText));
                OnPropertyChanged(nameof(PrinterQueueStatusMessage));
            }
        }
    }

    public bool HasPrinterQueueWarning => !PrinterQueueStatus.CanSubmit;

    public string PrinterDisplayName => string.IsNullOrWhiteSpace(Template.PrinterProfile.PrinterName)
        ? "(Chưa chọn)"
        : Template.PrinterProfile.PrinterName;

    public string PrinterQueueStatusText => PrinterQueueStatus.CanSubmit
        ? string.Empty
        : string.IsNullOrWhiteSpace(Template.PrinterProfile.PrinterName)
            ? "Chọn máy in đã xác thực"
            : PrinterQueueStatus.IsAvailable ? "Máy in chưa sẵn sàng" : "Máy in không khả dụng";

    public string PrinterQueueStatusMessage => PrinterQueueStatus.CanSubmit
        ? string.Empty
        : string.IsNullOrWhiteSpace(PrinterQueueStatus.ErrorMessage)
            ? "Hàng đợi máy in đã lưu không khả dụng. Hãy mở Cài đặt máy in và chọn hàng đợi đã xác thực trước khi in."
            : $"{PrinterQueueStatus.ErrorMessage} Hãy mở Cài đặt máy in và chọn hàng đợi đã xác thực trước khi in.";

    /// <summary>
    /// Rechecks the saved queue off the WPF dispatcher. The result is applied
    /// only if the template still refers to the same queue, so a slow lookup
    /// cannot overwrite a newer printer selection.
    /// </summary>
    public async Task RefreshPrinterQueueStatusAsync(CancellationToken cancellationToken = default)
    {
        var requestedName = Template.PrinterProfile.PrinterName ?? string.Empty;
        PrinterQueueLookupResult result;
        try
        {
            result = await Task.Run(
                () => string.IsNullOrWhiteSpace(requestedName)
                    ? PrinterQueueLookupResult.Missing(
                        requestedName,
                        "No printer queue is selected. Choose a verified industrial printer before printing.")
                    : _printerQueueLookup.Resolve(requestedName),
                cancellationToken).ConfigureAwait(true);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            result = PrinterQueueLookupResult.Missing(
                requestedName,
                $"Printer queue lookup failed: {ex.Message}");
        }

        if (!string.Equals(
                Template.PrinterProfile.PrinterName ?? string.Empty,
                requestedName,
                StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        PrinterQueueStatus = result;
        if (!result.CanSubmit)
        {
            StatusText = $"Cảnh báo máy in: {PrinterQueueStatusMessage}";
        }
    }

    /// <summary>
    /// Read-only recovery evidence loaded from the durable print-job event log.
    /// A pending tail is a warning for reconciliation, never permission to retry.
    /// </summary>
    public PrintJobRecoveryReport PrintRecoveryReport
    {
        get => _printRecoveryReport;
        private set
        {
            if (SetProperty(ref _printRecoveryReport, value))
            {
                OnPropertyChanged(nameof(HasPendingPrintRecovery));
                OnPropertyChanged(nameof(PrintRecoveryStatusText));
            }
        }
    }

    public bool HasPendingPrintRecovery => PrintRecoveryReport.RequiresRepair || PrintRecoveryReport.HasPendingJobs;

    public string PrintRecoveryStatusText => PrintRecoveryReport.RequiresRepair
        ? "Nhật ký lệnh in bị hỏng — mở Trung tâm in"
        : PrintRecoveryReport.HasPendingJobs
            ? $"Xem lại {PrintRecoveryReport.Candidates.Count} lệnh in chưa rõ kết quả"
            : string.Empty;

    /// <summary>
    /// Operator action from the Print Center (plan 13 GĐ5, Figma 37:280): archives
    /// a damaged job log and continues with its valid part. Returns the archive
    /// path, or null when the log was not damaged.
    /// </summary>
    public async Task<string?> ArchiveDamagedPrintLogAsync(CancellationToken cancellationToken = default)
    {
        var archive = await _printJobStateStore.ArchiveDamagedLogAndStartNewAsync(cancellationToken).ConfigureAwait(true);
        await RefreshPrintRecoveryAsync(cancellationToken).ConfigureAwait(true);
        return archive;
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
    public ICommand VerifyExcelLinkCommand { get; }
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

    /// <summary>
    /// Replays the latest valid event per job. This method is intentionally
    /// explicit so startup/UI can refresh without coupling the event store to
    /// dispatch or automatic retry behavior.
    /// </summary>
    public async Task RefreshPrintRecoveryAsync(CancellationToken cancellationToken = default)
    {
        PrintRecoveryReport = await PrintJobRecoveryService
            .LoadAsync(_printJobStateStore, cancellationToken)
            .ConfigureAwait(true);

        if (PrintRecoveryReport.RequiresRepair || PrintRecoveryReport.HasPendingJobs)
        {
            StatusText = PrintRecoveryReport.UserFacingSummary;
        }
    }

    /// <summary>
    /// Re-queries one queue-backed recovery candidate. The operation only appends
    /// a queue observation; it never marks physical output or authorizes retry.
    /// </summary>
    public async Task<PrintJobReconciliationResult> ReconcilePrintJobAsync(
        string jobId,
        CancellationToken cancellationToken = default)
    {
        await RefreshPrintRecoveryAsync(cancellationToken).ConfigureAwait(true);
        var candidate = PrintRecoveryReport.Candidates
            .FirstOrDefault(item => string.Equals(item.JobId, jobId, StringComparison.Ordinal));
        if (candidate is null)
        {
            var missing = PrintJobRecoveryService.CreateInvalidResult(
                jobId,
                "The print job is no longer present in the valid recovery snapshot; refresh the report and do not retry automatically.");
            StatusText = missing.Summary;
            return missing;
        }

        var result = await PrintJobRecoveryService.ReconcileQueueAsync(
            candidate,
            new WindowsSpoolJobStatusReader(),
            timeout: TimeSpan.FromSeconds(3),
            pollInterval: TimeSpan.FromMilliseconds(250),
            cancellationToken).ConfigureAwait(true);

        if (result.QueueResult is not null)
        {
            var current = _printJobStateStore.GetCurrentState(candidate.JobId);
            if (current is PrintJobLifecycleState currentState
                && PrintJobStateMachine.CanTransition(currentState, PrintJobLifecycleState.QueueObserved))
            {
                await RecordPrintJobTransitionAsync(new PrintJobStateTransition(
                    candidate.JobId,
                    currentState,
                    PrintJobLifecycleState.QueueObserved,
                    result.QueueResult.FinalObservation.ObservedAtUtc ?? DateTimeOffset.UtcNow,
                    result.Summary,
                    PrinterName: candidate.PrinterName,
                    SpoolJobId: candidate.SpoolJobId,
                    QueueState: result.QueueResult.FinalObservation.State.ToString(),
                    DocumentHash: candidate.DocumentHash,
                    SceneHash: candidate.SceneHash,
                    OutputContractHash: candidate.OutputContractHash,
                    ManifestFingerprint: candidate.ManifestFingerprint,
                    Manifest: candidate.Manifest));
            }
        }

        await RefreshPrintRecoveryAsync(cancellationToken).ConfigureAwait(true);
        StatusText = result.Summary;
        return result;
    }

    /// <summary>
    /// Records that an operator reviewed an uncertain job. This is an audit-only
    /// action; it does not mark output as printed and does not submit anything.
    /// </summary>
    public async Task<PrintJobOperatorActionResult> AcknowledgePrintJobAsync(
        string jobId,
        string reason = "Operator acknowledged the uncertain print job.",
        CancellationToken cancellationToken = default)
    {
        var result = await PrintJobOperatorActionService.AcknowledgeAsync(
            _printJobStateStore,
            jobId,
            reason: reason,
            cancellationToken: cancellationToken).ConfigureAwait(true);
        LogOperatorAction(result);
        await RefreshPrintRecoveryAsync(cancellationToken).ConfigureAwait(true);
        StatusText = result.Summary;
        return result;
    }

    /// <summary>
    /// Voids an uncertain job in the durable lineage without sending a printer
    /// cancellation command. The action is explicit and terminal for recovery.
    /// </summary>
    public async Task<PrintJobOperatorActionResult> VoidPrintJobAsync(
        string jobId,
        string reason = "Operator voided the uncertain print job.",
        CancellationToken cancellationToken = default)
    {
        var result = await PrintJobOperatorActionService.VoidAsync(
            _printJobStateStore,
            jobId,
            reason: reason,
            cancellationToken: cancellationToken).ConfigureAwait(true);
        LogOperatorAction(result);
        await RefreshPrintRecoveryAsync(cancellationToken).ConfigureAwait(true);
        StatusText = result.Summary;
        return result;
    }

    /// <summary>
    /// Creates a linked Created child for an explicitly requested reprint. It
    /// never enters preparation or dispatch; the operator must start that flow
    /// separately after reviewing the lineage.
    /// </summary>
    public async Task<PrintJobOperatorActionResult> RequestPrintJobReprintAsync(
        string jobId,
        string reason = "Operator requested an explicitly linked reprint.",
        CancellationToken cancellationToken = default)
    {
        var result = await PrintJobOperatorActionService.RequestReprintAsync(
            _printJobStateStore,
            jobId,
            reason: reason,
            cancellationToken: cancellationToken).ConfigureAwait(true);
        LogOperatorAction(result);
        await RefreshPrintRecoveryAsync(cancellationToken).ConfigureAwait(true);
        StatusText = result.Summary;
        return result;
    }

    /// <summary>
    /// Approves a linked reprint only when the caller presents the exact
    /// manifest captured on the Created child. This records approval but does
    /// not dispatch anything.
    /// </summary>
    public async Task<PrintJobOperatorActionResult> ApprovePrintJobReprintAsync(
        string childJobId,
        PrintJobManifest expectedManifest,
        string reason = "Operator approved the linked reprint after manifest review.",
        CancellationToken cancellationToken = default)
    {
        var result = await PrintJobOperatorActionService.ApproveReprintAsync(
            _printJobStateStore,
            childJobId,
            expectedManifest,
            reason: reason,
            cancellationToken: cancellationToken).ConfigureAwait(true);
        LogOperatorAction(result);
        await RefreshPrintRecoveryAsync(cancellationToken).ConfigureAwait(true);
        StatusText = result.Summary;
        return result;
    }

    /// <summary>
    /// Dispatches an already approved child with caller-supplied current rows.
    /// The current template, queue, DPI and rows are rebuilt into a manifest and
    /// must match the approved identity before the normal preparation path starts.
    /// </summary>
    public async Task<TrackedPrintResult> DispatchApprovedPrintJobReprintAsync(
        string childJobId,
        IReadOnlyList<IReadOnlyDictionary<string, string>?> rows,
        int sourceRowCount,
        IReadOnlyList<PrintDispatchCopy>? sequence = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(rows);
        if (rows.Count == 0 || sourceRowCount <= 0)
        {
            throw new ArgumentException("An approved reprint requires at least one current row and a positive source-row count.", nameof(rows));
        }

        var current = await PrintJobOperatorActionService.ReadCurrentEventAsync(
            _printJobStateStore,
            childJobId,
            cancellationToken).ConfigureAwait(true);
        if (current.To != PrintJobLifecycleState.Created
            || current.OperatorAction != PrintJobOperatorAction.ReprintApproved
            || current.Manifest is null
            || !current.Manifest.IsFingerprintValid)
        {
            throw new InvalidOperationException(
                $"Reprint child '{childJobId}' is not approved with a complete immutable manifest; dispatch was blocked.");
        }

        var approvedManifest = current.Manifest;
        if (rows.Count != approvedManifest.LabelCount
            || sourceRowCount != approvedManifest.SourceRowCount)
        {
            throw new InvalidOperationException(
                "Reprint dispatch was blocked because the current row/label counts do not match the approved manifest.");
        }

        var printerName = Template.PrinterProfile.PrinterName;
        if (string.IsNullOrWhiteSpace(printerName))
        {
            throw new InvalidOperationException(
                "Reprint dispatch was blocked because no verified printer queue is selected.");
        }

        // Reprint approval includes the physical output contract captured by
        // preview/quick-print preparation. Rebuild that effective contract
        // before comparing manifests; a design-only plan cannot prove that the
        // same queue, media and DPI will be used again.
        var effectivePlan = _printService.CreateEffectivePlan(Template, printerName);
        var currentManifest = PrintJobManifest.Create(
            Template.Name,
            CurrentFilePath,
            approvedManifest.PrintMode,
            printerName,
            Template.WidthMm,
            Template.HeightMm,
            effectivePlan.DpiX > 0 ? effectivePlan.DpiX : Template.PrinterProfile.Dpi,
            effectivePlan.DpiY > 0 ? effectivePlan.DpiY : Template.PrinterProfile.Dpi,
            rows.Count,
            sourceRowCount,
            rows,
            effectivePlan.DocumentHash,
            effectivePlan.TextResourceFingerprint,
            effectivePlan.SceneHash,
            effectivePlan.OutputContractHash,
            imageRasterFingerprint: effectivePlan.ImageRasterFingerprint,
            thermalRasterGoldenFingerprint: effectivePlan.ThermalRasterGolden?.Fingerprint ?? string.Empty,
            sequence: sequence);
        if (!string.Equals(currentManifest.Fingerprint, approvedManifest.Fingerprint, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Reprint dispatch was blocked because the current template, printer, DPI or data no longer matches the approved manifest.");
        }

        var tracked = await DispatchTrackedPrintAsync(
            rows,
            approvedManifest.PrintMode,
            existingJobId: childJobId,
            approvedManifest: approvedManifest,
            sourceRowCount: sourceRowCount,
            sequence: sequence).ConfigureAwait(true);
        await RefreshPrintRecoveryAsync(cancellationToken).ConfigureAwait(true);
        return tracked;
    }

    private void ShowAllPanels()
    {
        IsToolboxVisible = true;
        IsPropertiesVisible = true;
        StatusText = "Đã khôi phục các bảng làm việc";
    }

    public void NewTemplate(NewTemplateRequest? request)
    {
        request ??= new NewTemplateRequest("Untitled Label", 100, 50, 203);
        StopWatchingExcelFile();
        IsExcelDataStale = false;
        ExcelDataView = null;
        DataConnector = null;
        ExcelHeaders.Clear();
        SelectedDataItem = null;
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
        MarkDocumentClean();
        StatusText = $"Mẫu tem mới: {Template.WidthMm} x {Template.HeightMm} mm";
    }

    public async Task SaveAsync(string filePath)
    {
        if (IsSaving)
        {
            throw new InvalidOperationException("Đang lưu mẫu tem. Hãy chờ lần lưu hiện tại hoàn tất.");
        }

        IsSaving = true;
        try
        {
            CommitPendingHistory();
            UpdateRelativePath(filePath);
            // Serialize an immutable snapshot, not the live object: the designer
            // stays editable while the asynchronous write runs (plan 13 F07).
            var snapshot = CaptureTemplateSnapshot();
            var copy = JsonSerializer.Deserialize<LabelTemplate>(snapshot, HistoryJsonOptions)
                ?? throw new InvalidOperationException("Không tạo được bản chụp của mẫu tem để lưu.");
            await _projectFileService.SaveAsync(copy, filePath);
            CurrentFilePath = filePath;
            _savedTemplateSnapshot = snapshot;
            UpdateDirtyState(CaptureTemplateSnapshot());
            StatusText = $"Đã lưu: {Path.GetFileName(filePath)}";
        }
        finally
        {
            IsSaving = false;
        }
    }

    public async Task<ProjectLoadResult> OpenAsync(string filePath)
    {
        // Parse before detaching the current document. A corrupt, foreign, or
        // future-schema file must leave the operator's current work and undo
        // history intact. A readable .bak is not installed in its place: that
        // second outcome used to replace the open session. The operator opens
        // the backup file explicitly. Explicit revision restore still writes a
        // validated primary first, then calls this method.
        var loadResult = await _projectFileService.LoadWithRecoveryAsync(filePath);
        if (loadResult.RecoveredFromBackup)
        {
            var backupPath = string.IsNullOrWhiteSpace(loadResult.BackupPath)
                ? loadResult.SourcePath
                : loadResult.BackupPath;
            var reason = string.IsNullOrWhiteSpace(loadResult.PrimaryError)
                ? "The template file is invalid."
                : loadResult.PrimaryError;
            throw new InvalidDataException(
                $"Không thể mở tệp mẫu ({reason}). Bản sao lưu còn tại '{backupPath}'. Tài liệu đang mở không bị thay. Hãy mở tệp sao lưu đó nếu muốn dùng nó.");
        }

        UnobserveTemplate(Template);
        Template = loadResult.Template;
        ResetHistory();
        // Keep the selected path while linked Excel data is restored so
        // relative data-source paths resolve beside the original document.
        CurrentFilePath = filePath;
        SelectedObject = Template.Objects.OrderByDescending(item => item.ZIndex).FirstOrDefault();
        await RestoreLinkedExcelDataAsync();
        MarkDocumentClean();
        return loadResult;
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
        MarkDocumentClean();
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

    public PrinterDiscoveryResult DiscoverInstalledPrinters()
    {
        return _printerDiscoveryService.DiscoverInstalledPrinters();
    }

    public void ApplyPrinterSelection(
        PrinterInfo printer,
        PrinterPaperInfo paper,
        int dpi,
        LabelOrientation orientation,
        PrinterPaperInfo? physicalMedia = null,
        PaperSizeSource pageSizeSource = PaperSizeSource.Manual,
        string driverIdentity = "",
        PrinterSettingsSource settingsSource = PrinterSettingsSource.Label,
        double? offsetXMm = null,
        double? offsetYMm = null,
        double? scaleX = null,
        double? scaleY = null)
    {
        PrinterSelectionApplier.Apply(
            Template,
            printer,
            paper,
            dpi,
            orientation,
            physicalMedia,
            pageSizeSource,
            driverIdentity,
            settingsSource,
            offsetXMm,
            offsetYMm,
            scaleX,
            scaleY);
        StatusText = $"Máy in: {printer.Name}, khổ tem: {paper.Name} ({Template.WidthMm:0.##} x {Template.HeightMm:0.##} mm), nguồn trang: {pageSizeSource}, hướng: {orientation}";
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
        DataConnector = new DataTableDataConnector(
            new DataConnectorDescriptor(
                string.IsNullOrWhiteSpace(Template.DatabaseConfig.DataSourceId) ? filePath : Template.DatabaseConfig.DataSourceId,
                Path.GetFileName(filePath),
                string.Equals(Path.GetExtension(filePath), ".csv", StringComparison.OrdinalIgnoreCase) ? "csv" : "excel",
                SupportsPaging: true,
                // This connector is an immutable import snapshot. The existing
                // UI refresh command creates a replacement connector; it does
                // not refresh this instance through IDataConnector.
                SupportsRefresh: false),
            table);
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
        // Capture content identity (SHA-256) so refresh cannot treat same-mtime
        // byte replacements as current (CORE DB06). Hash off the UI thread.
        var capturedPath = filePath;
        _excelDataSourceIdentity = await Task.Run(() =>
            FileSourceIdentity.TryCapture(capturedPath, out var identity) ? identity : null).ConfigureAwait(true);
        _excelDataSourceWriteTimeUtc = _excelDataSourceIdentity?.LastWriteTimeUtc ?? TryGetFileWriteTimeUtc(filePath);
        OnPropertyChanged(nameof(ExcelDataFreshnessText));
        IsExcelDataStale = false;
        MarkExcelLinkVerified(table.Rows.Count, table.Columns.Count);
        StartWatchingExcelFile(filePath);

        var issueCount = GetBindingIssues().Count;
        var issueSuffix = issueCount > 0 ? $" — {issueCount} đối tượng có liên kết bị thiếu/hỏng" : string.Empty;
        StatusText = $"Đã nhập {table.Rows.Count} dòng từ {Path.GetFileName(filePath)} / {sheetName}{issueSuffix}";
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
            StatusText = "Chưa có cơ sở dữ liệu Excel nào được liên kết";
            return;
        }

        var path = Template.DatabaseConfig.FilePath;
        var known = _excelDataSourceIdentity;
        // DB06: mtime alone is not freshness evidence. Same timestamp with
        // replaced bytes must still refresh. Hash off the UI thread.
        var current = await Task.Run(() =>
            FileSourceIdentity.TryCapture(path, out var identity) ? identity : null).ConfigureAwait(true);
        if (ExcelDataFreshnessPolicy.IsUnchanged(known, current))
        {
            StatusText = $"Dữ liệu Excel đã được cập nhật mới nhất ({ExcelDataFreshnessText}) — tệp không thay đổi kể từ lần đọc cuối";
            return;
        }

        await ImportExcelAsync(path, Template.DatabaseConfig.SheetName, "Refresh");
    }

    /// <summary>
    /// Performs an explicit trust check for the Properties panel. A green Verified
    /// state is issued only after the workbook opens and the selected sheet/header
    /// can be read. If the file changed since the current snapshot, verification
    /// refreshes the rows first so the UI cannot certify stale print data.
    /// </summary>
    public async Task VerifyExcelLinkAsync(CancellationToken cancellationToken = default)
    {
        if (!HasLinkedExcelSource)
        {
            ExcelLinkVerificationState = ExcelLinkVerificationState.NotLinked;
            StatusText = "Chưa có cơ sở dữ liệu Excel nào được liên kết";
            return;
        }

        ExcelLinkVerificationState = ExcelLinkVerificationState.Checking;
        _excelLinkVerificationFailureMessage = string.Empty;

        try
        {
            var path = Template.DatabaseConfig.FilePath;
            var known = _excelDataSourceIdentity;
            var current = await Task.Run(() =>
                    FileSourceIdentity.TryCapture(path, out var identity) ? identity : null,
                cancellationToken).ConfigureAwait(true);
            if (IsExcelDataStale || !ExcelDataFreshnessPolicy.IsUnchanged(known, current))
            {
                await ImportExcelAsync(
                    path,
                    Template.DatabaseConfig.SheetName,
                    "Verify",
                    cancellationToken);
                IsExcelLinkBroken = false;
                StatusText = $"Đã cập nhật và xác thực Excel: {LinkedExcelSourceText}";
                return;
            }

            var result = await _excelDataService.TestConnectionAsync(
                Template.DatabaseConfig.FilePath,
                Template.DatabaseConfig.SheetName,
                Template.DatabaseConfig.HeaderRowIndex,
                cancellationToken);

            if (!result.Ok)
            {
                MarkExcelLinkVerificationFailed(result.Message);
                return;
            }

            IsExcelLinkBroken = false;
            MarkExcelLinkVerified(ExcelDataView?.Count ?? 0, ExcelHeaders.Count);
            StatusText = $"Đã xác thực liên kết Excel: {LinkedExcelSourceText}. {result.Message}";
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            MarkExcelLinkVerificationFailed(ex.Message);
        }
    }

    private void MarkExcelLinkVerified(int rowCount, int columnCount)
    {
        _excelLinkVerifiedRowCount = rowCount;
        _excelLinkVerifiedColumnCount = columnCount;
        _excelLinkVerifiedAtLocal = DateTime.Now;
        _excelLinkVerificationFailureMessage = string.Empty;
        ExcelLinkVerificationState = ExcelLinkVerificationState.Verified;
        OnPropertyChanged(nameof(ExcelLinkVerificationDetail));
        OnPropertyChanged(nameof(ExcelLinkVerificationTrustText));
    }

    private void MarkExcelLinkVerificationFailed(string message)
    {
        _excelLinkVerificationFailureMessage = string.IsNullOrWhiteSpace(message)
            ? "Không thể xác thực sổ làm việc."
            : message;
        IsExcelLinkBroken = !File.Exists(Template.DatabaseConfig.FilePath);
        ExcelLinkVerificationState = ExcelLinkVerificationState.Failed;
        OnPropertyChanged(nameof(ExcelLinkVerificationDetail));
        StatusText = $"Xác thực liên kết Excel thất bại: {_excelLinkVerificationFailureMessage}";
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
        StatusText = $"Không tìm thấy dòng khóa \"{keyValue}\" trong cột \"{keyField}\" — dòng có thể đã bị xóa hoặc thay đổi";
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
            ExcelLinkVerificationState = ExcelLinkVerificationState.NotLinked;
            StatusText = $"Đã mở: {Path.GetFileName(CurrentFilePath)}";
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
            DataConnector = null;
            PreviewRow = null;
            SelectedDataItem = null;
            ExcelHeaders.Clear();
            OnPropertyChanged(nameof(CurrentExcelRowText));
            OnPropertyChanged(nameof(KeyFieldOptions));
            OnPropertyChanged(nameof(SelectedKeyFieldName));
            OnPropertyChanged(nameof(SelectedCopiesFieldName));
            IsExcelLinkBroken = true;
            MarkExcelLinkVerificationFailed($"File not found: {Template.DatabaseConfig.FilePath}");
            StatusText = $"Đã mở: {Path.GetFileName(CurrentFilePath)}. Không tìm thấy tệp Excel liên kết: {Template.DatabaseConfig.FilePath}";
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
            StatusText = $"Đã mở: {Path.GetFileName(CurrentFilePath)}. Đã khôi phục liên kết Excel: {Path.GetFileName(Template.DatabaseConfig.FilePath)} / {Template.DatabaseConfig.SheetName}";
        }
        catch (Exception ex)
        {
            ExcelDataView = null;
            DataConnector = null;
            PreviewRow = null;
            SelectedDataItem = null;
            ExcelHeaders.Clear();
            OnPropertyChanged(nameof(CurrentExcelRowText));
            IsExcelLinkBroken = true;
            MarkExcelLinkVerificationFailed(ex.Message);
            StatusText = $"Đã mở: {Path.GetFileName(CurrentFilePath)}. Không thể khôi phục liên kết Excel: {ex.Message}";
        }
    }

    /// <summary>
    /// Prompts the user to locate the linked Excel file and restores the connection.
    /// </summary>
    private async Task RelinkExcelAsync()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Tìm tệp Excel",
            Filter = "Tệp dữ liệu (*.xlsx;*.xlsm;*.csv)|*.xlsx;*.xlsm;*.csv|Tất cả tệp (*.*)|*.*",
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
            StatusText = $"Không thể đọc tệp Excel: {ex.Message}";
            return;
        }

        if (sheets.Count == 0)
        {
            StatusText = $"Không tìm thấy trang tính nào trong {Path.GetFileName(filePath)}";
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
            StatusText = $"Đã liên kết lại Excel: {Path.GetFileName(filePath)} / {sheetName}";
        }
        catch (Exception ex)
        {
            IsExcelLinkBroken = true;
            StatusText = $"Liên kết lại thất bại: {ex.Message}";
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
        DataConnector = null;
        ExcelHeaders.Clear();
        SelectedDataItem = null;
        SelectedAvailableDatabaseField = null;
        SelectedLabelDatabaseField = null;
        SelectedExcelField = null;
        Template.DatabaseConfig = new DatabaseConfig();
        IsExcelLinkBroken = false;
        IsExcelDataStale = false;
        ExcelLinkVerificationState = ExcelLinkVerificationState.NotLinked;
        _excelDataReadAtLocal = null;
        _excelDataSourceWriteTimeUtc = null;
        _excelDataSourceIdentity = null;

        OnPropertyChanged(nameof(HasLinkedExcelSource));
        OnPropertyChanged(nameof(LinkedExcelSourceText));
        OnPropertyChanged(nameof(ExcelLinkStatusText));
        OnPropertyChanged(nameof(ExcelDataFreshnessText));
        OnPropertyChanged(nameof(CurrentExcelRowText));
        OnPropertyChanged(nameof(KeyFieldOptions));
        OnPropertyChanged(nameof(SelectedKeyFieldName));
        OnPropertyChanged(nameof(SelectedCopiesFieldName));
        RaiseDatabaseFieldStateChanged();

        StatusText = "Đã gỡ liên kết Excel. Các đối tượng vẫn giữ liên kết — hãy nhập lại Excel để tiếp tục in.";
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
            StatusText = $"Đã là nguồn dữ liệu dùng chung: {existing.DisplayName}";
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
        StatusText = $"Đã thêm nguồn dữ liệu dùng chung: {source.DisplayName}";
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
            StatusText = $"Đang dùng nguồn dữ liệu dùng chung: {source.DisplayName}";
        }
        catch (Exception ex)
        {
            IsExcelLinkBroken = true;
            StatusText = $"Không thể dùng nguồn dữ liệu dùng chung '{source.DisplayName}': {ex.Message}";
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
        StatusText = $"Đã gỡ nguồn dữ liệu dùng chung: {source.DisplayName}";
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
            Title = "Tìm tệp Excel cho nguồn dữ liệu dùng chung",
            Filter = "Tệp dữ liệu (*.xlsx;*.xlsm;*.csv)|*.xlsx;*.xlsm;*.csv|Tất cả tệp (*.*)|*.*",
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
            StatusText = $"Không thể đọc tệp Excel: {ex.Message}";
            return;
        }

        if (sheets.Count == 0)
        {
            StatusText = $"Không tìm thấy trang tính nào trong {Path.GetFileName(dialog.FileName)}";
            return;
        }

        source.FilePath = dialog.FileName;
        source.SheetName = sheets.Contains(source.SheetName) ? source.SheetName : sheets[0];
        _dataSourceRegistry.Upsert(source);
        _dataSourceRegistry.Save();
        StatusText = $"Đã liên kết lại nguồn dữ liệu dùng chung: {source.DisplayName}";

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

        StatusText = $"Đã thêm {LabelDatabaseFields.Count} trường cho thiết kế tem";
        RaiseDatabaseFieldStateChanged();
    }

    private void AddLabelDatabaseField(DatabaseField field, bool updateStatus = true)
    {
        if (LabelDatabaseFields.Any(item => string.Equals(item.Name, field.Name, StringComparison.OrdinalIgnoreCase)))
        {
            if (updateStatus)
            {
                StatusText = $"Trường đã tồn tại: {field.Name}";
            }
            return;
        }

        var clone = CloneDatabaseField(field);
        LabelDatabaseFields.Add(clone);
        SelectedLabelDatabaseField = clone;
        SelectedExcelField = clone.Name;
        if (updateStatus)
        {
            StatusText = $"Đã thêm trường tem: {field.Name}";
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
        StatusText = $"Đã xóa trường tem: {removedName}";
        RaiseDatabaseFieldStateChanged();
    }

    private void ClearDatabaseFields()
    {
        LabelDatabaseFields.Clear();
        SelectedLabelDatabaseField = null;
        SelectedExcelField = null;
        StatusText = "Đã xóa sạch các trường tem";
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
        RaiseCommandCanExecuteChanged(RefreshExcelDataCommand);
    }

    private async Task PrintCurrentRowAsync()
    {
        try
        {
            if (IsExcelDataStale)
            {
                StatusText = "Đã chặn in: tệp Excel liên kết đã thay đổi kể từ lần đọc cuối. Hãy nhấn Cập nhật Excel trước (hoặc dùng Xem trước khi in để xác nhận và in với dữ liệu hiện tại).";
                return;
            }

            if (!string.IsNullOrWhiteSpace(DataTransformError))
            {
                StatusText = $"Đã chặn in: lỗi chuyển đổi dữ liệu. {DataTransformError}";
                return;
            }

            // Copies come from the template column preview uses. A one-row print
            // still reports that row's data-view number, not "Row 1".
            DatabaseConfig? copiesDatabase;
            IReadOnlyList<(int SourceRowNumber, IReadOnlyDictionary<string, string>? Row)> quantityRows;
            if (SelectedDataItem is DataRowView selectedRow)
            {
                copiesDatabase = Template.DatabaseConfig;
                quantityRows = new (int SourceRowNumber, IReadOnlyDictionary<string, string>? Row)[]
                {
                    (GetDataRowViewIndex(selectedRow) + 1, PreviewRow)
                };
            }
            else
            {
                copiesDatabase = null;
                quantityRows = new (int SourceRowNumber, IReadOnlyDictionary<string, string>? Row)[]
                {
                    (0, PreviewRow)
                };
            }

            if (!QuickPrintQuantity.TryExpand(copiesDatabase, PrintCopies, quantityRows, out var expandedRows, out var sequence, out var copiesError))
            {
                StatusText = copiesError;
                return;
            }

            var rows = expandedRows.ToArray();
            var tracked = await DispatchTrackedPrintAsync(rows, $"{Template.Name} label", sequence: sequence);
            var result = tracked.Result;
            if (result.IsAccepted)
            {
                StatusText = AppendQueueStatus(result.UserFacingStatus, tracked.SpoolStatus);
                _ = WritePrintLogAsync(
                    "Current row",
                    rows,
                    PreviewRow is null ? 0 : 1,
                    rows.Length,
                    result: result,
                    jobId: tracked.JobId,
                    spoolStatus: tracked.SpoolStatus);
            }
            else if (result.Outcome != PrintJobOutcome.Cancelled)
            {
                StatusText = AppendQueueStatus(result.UserFacingStatus, tracked.SpoolStatus);
                LogPrintOperation(
                    "Current row",
                    PreviewRow is null ? 0 : 1,
                    0,
                    success: false,
                    errorMessage: result.ErrorMessage,
                    result: result,
                    jobId: tracked.JobId,
                    spoolStatus: tracked.SpoolStatus);
            }
        }
        catch (Exception ex)
        {
            StatusText = $"In thất bại: {ex.Message}";
            LogPrintOperation("Current row", PreviewRow is null ? 0 : 1, 0, success: false, ex.Message);
        }
    }

    private async Task PrintAllRowsAsync()
    {
        try
        {
            if (ExcelDataView is null || ExcelDataView.Count == 0)
            {
                StatusText = "Không có dòng Excel nào để in";
                return;
            }

            if (IsExcelDataStale)
            {
                StatusText = "Đã chặn in: tệp Excel liên kết đã thay đổi kể từ lần đọc cuối. Hãy nhấn Cập nhật Excel trước (hoặc dùng Xem trước khi in để xác nhận và in với dữ liệu hiện tại).";
                return;
            }

            var quantityRows = new List<(int SourceRowNumber, IReadOnlyDictionary<string, string>? Row)>();
            for (var index = 0; index < ExcelDataView.Count; index++)
            {
                if (ExcelDataView[index] is not DataRowView rowView)
                {
                    continue;
                }

                var row = CreatePreviewRow(rowView, out var transformError);
                if (!string.IsNullOrWhiteSpace(transformError))
                {
                    DataTransformError = transformError;
                    StatusText = $"Đã chặn in: lỗi chuyển đổi dữ liệu. {transformError}";
                    return;
                }

                quantityRows.Add((index + 1, row));
            }

            DataTransformError = string.Empty;
            if (!QuickPrintQuantity.TryExpand(Template.DatabaseConfig, PrintCopies, quantityRows, out var expandedRows, out var sequence, out var copiesError))
            {
                StatusText = copiesError;
                return;
            }

            var rows = expandedRows.ToArray();

            var tracked = await DispatchTrackedPrintAsync(rows, $"{Template.Name} labels", sequence: sequence);
            var result = tracked.Result;
            if (result.IsAccepted)
            {
                StatusText = AppendQueueStatus(result.UserFacingStatus, tracked.SpoolStatus);
                _ = WritePrintLogAsync(
                    "All rows",
                    rows,
                    ExcelDataView.Count,
                    rows.Length,
                    result: result,
                    jobId: tracked.JobId,
                    spoolStatus: tracked.SpoolStatus);
            }
            else if (result.Outcome != PrintJobOutcome.Cancelled)
            {
                StatusText = AppendQueueStatus(result.UserFacingStatus, tracked.SpoolStatus);
                LogPrintOperation(
                    "All rows",
                    ExcelDataView?.Count ?? 0,
                    0,
                    success: false,
                    errorMessage: result.ErrorMessage,
                    result: result,
                    jobId: tracked.JobId,
                    spoolStatus: tracked.SpoolStatus);
            }
        }
        catch (Exception ex)
        {
            StatusText = $"In thất bại: {ex.Message}";
            LogPrintOperation("All rows", ExcelDataView?.Count ?? 0, 0, success: false, ex.Message);
        }
    }

    private async Task PrintCalibrationAsync()
    {
        try
        {
            var result = await _printService.PrintCalibrationWithResultAsync(Template);
            if (result.IsAccepted)
            {
                StatusText = result.UserFacingStatus;
            }
            else if (result.Outcome != PrintJobOutcome.Cancelled)
            {
                StatusText = result.UserFacingStatus;
            }
        }
        catch (Exception ex)
        {
            StatusText = $"In hiệu chỉnh thất bại: {ex.Message}";
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
            Style =
            {
                FontSizePt = 11,
                BorderThicknessMm = 0,
                VerticalAlignment = TextVerticalAlignmentMode.Center,
                TextSizing = TextSizingMode.AutoFit,
                TextOverflow = TextOverflowMode.AllowOverflow
            }
        });
    }

    private void AddTextBox()
    {
        // User-owned frame: drag sets size; text reflows/clips inside. No
        // content-owned AutoFit (object does not follow text). The initial
        // frame is compact and label-aware so it does not waste or leave the
        // bounds of small logistics labels.
        var marginMm = Math.Clamp(Math.Min(Template.WidthMm, Template.HeightMm) * 0.04, 0.5, 2.0);
        var availableWidthMm = Math.Max(1, Template.WidthMm - marginMm * 2);
        var availableHeightMm = Math.Max(1, Template.HeightMm - marginMm * 2);
        var widthMm = Math.Min(32, availableWidthMm);
        var heightMm = Math.Min(6, availableHeightMm);
        AddObject(new LabelObject
        {
            Type = ObjectType.TextBox,
            Name = "Text Box",
            Text = "Text Box",
            BindingExpression = string.Empty,
            XMm = marginMm,
            YMm = marginMm,
            WidthMm = widthMm,
            HeightMm = heightMm,
            Style =
            {
                FontSizePt = 9,
                BorderThicknessMm = 0,
                OutlineStyle = OutlineStyle.None,
                VerticalAlignment = TextVerticalAlignmentMode.Center,
                // Retains over 90% of a 20 x 6 mm frame as printable content.
                // Tight (0 mm) remains available for true edge-to-edge text.
                TextPaddingMm = 0.2,
                TextSizing = TextSizingMode.FixedFrame,
                TextOverflow = TextOverflowMode.Error,
                TextFitMinimumFontSizePt = 4,
                TextFitMaximumFontSizePt = 9,
                TextFitMinimumScale = 0.5,
                TextFitMaximumScale = 1.0
            }
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
            Filter = "Tệp hình ảnh (*.png;*.jpg;*.jpeg;*.bmp;*.gif)|*.png;*.jpg;*.jpeg;*.bmp;*.gif|Tất cả tệp (*.*)|*.*"
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            var base64 = Convert.ToBase64String(File.ReadAllBytes(dialog.FileName));
            SelectedObject.ImageDataBase64 = base64;
            if (ImageRasterizer.TryGetPixelDimensions(base64, out var pixelWidth, out var pixelHeight))
            {
                SelectedObject.ImagePixelWidth = pixelWidth;
                SelectedObject.ImagePixelHeight = pixelHeight;
            }
            else
            {
                SelectedObject.ImagePixelWidth = 0;
                SelectedObject.ImagePixelHeight = 0;
            }
            SelectedObject.Name = Path.GetFileNameWithoutExtension(dialog.FileName);
            StatusText = $"Đã thay đổi hình ảnh: {SelectedObject.Name}";
        }
        catch (Exception ex)
        {
            StatusText = $"Không thể đọc tệp hình ảnh: {ex.Message}";
        }
    }

    private void AddImage()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Chọn hình ảnh",
            Filter = "Tệp hình ảnh (*.png;*.jpg;*.jpeg;*.bmp;*.gif)|*.png;*.jpg;*.jpeg;*.bmp;*.gif|Tất cả tệp (*.*)|*.*"
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
            StatusText = $"Không thể đọc tệp hình ảnh: {ex.Message}";
            return;
        }

        var (widthMm, heightMm) = GetDefaultImageSizeMm(bytes);
        var imageBase64 = Convert.ToBase64String(bytes);
        ImageRasterizer.TryGetPixelDimensions(imageBase64, out var pixelWidth, out var pixelHeight);

        AddObject(new LabelObject
        {
            Type = ObjectType.Image,
            Name = Path.GetFileNameWithoutExtension(dialog.FileName),
            ImageDataBase64 = imageBase64,
            ImagePixelWidth = pixelWidth,
            ImagePixelHeight = pixelHeight,
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
            StatusText = "Hãy chọn một trường Excel trước";
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
        StatusText = $"Đã liên kết đối tượng được chọn với {{{fieldName}}}";
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
        StatusText = "Đối tượng được chọn đã chuyển thành văn bản tĩnh";
        RaiseFormulaPreviewChanged();
    }

    private void InsertFunctionFormula(string? formula)
    {
        if (SelectedObject is null || string.IsNullOrWhiteSpace(formula))
        {
            StatusText = "Hãy chọn một đối tượng trước khi chèn hàm";
            return;
        }

        SelectedObject.BindingExpression = formula;
        if (SelectedObject.Type is ObjectType.BarcodeCode128 or ObjectType.QRCode or ObjectType.DataMatrix)
        {
            SelectedObject.Text = string.Empty;
        }

        StatusText = $"Đã chèn công thức: {formula}";
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
            StatusText = "Hãy chọn một đối tượng và thêm các thành phần công thức trước";
            return;
        }

        var expression = BuildFormulaExpression();
        InsertFunctionFormula(expression);
        StatusText = $"Đã áp dụng công thức: {expression}";
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
            ObjectType.Line => "Đường kẻ: xác định điểm đầu tiên",
            ObjectType.Ellipse => "Hình elip/tròn: xác định góc đầu tiên",
            _ => "Hình chữ nhật: xác định góc đầu tiên"
        };
        StatusText = tool switch
        {
            ObjectType.Line => "Đường kẻ: nhấp điểm đầu, di chuyển con trỏ, nhấp điểm tiếp theo hoặc nhập chiều dài + Enter, Esc để hủy",
            ObjectType.Ellipse => "Hình elip/tròn: nhấp góc đầu, di chuyển con trỏ, nhấp góc đối diện hoặc nhập rộng,cao + Enter, Esc để hủy",
            _ => "Hình chữ nhật: nhấp góc đầu, di chuyển con trỏ, nhấp góc đối diện hoặc nhập rộng,cao + Enter, Esc để hủy"
        };
    }

    public void CompleteDrawingTool(LabelObject labelObject)
    {
        DrawingTool = null;
        SelectedObject = labelObject;
        StatusText = $"Đã thêm {labelObject.Name}";
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
        StatusText = $"Đã thêm {barcode.Name}";
        RecordTemplateChange();
    }

    private void AddObject(LabelObject labelObject)
    {
        labelObject.ZIndex = Template.Objects.Count == 0 ? 1 : Template.Objects.Max(item => item.ZIndex) + 1;
        Template.Objects.Add(labelObject);
        SelectedObject = labelObject;
        StatusText = $"Đã thêm {labelObject.Name}";
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
        StatusText = "Đã xóa đối tượng được chọn";
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
        CommitTemplateEditGesture();
        CommitPendingHistory();
        if (_undoStack.Count == 0)
        {
            return;
        }

        var currentSnapshot = CaptureTemplateSnapshot();
        var previousSnapshot = _undoStack.Pop();
        _redoStack.Push(currentSnapshot);
        RestoreTemplateSnapshot(previousSnapshot);
        StatusText = "Hoàn tác";
        RaiseHistoryCanExecuteChanged();
    }

    private void Redo()
    {
        CommitTemplateEditGesture();
        CommitPendingHistory();
        if (_redoStack.Count == 0)
        {
            return;
        }

        var currentSnapshot = CaptureTemplateSnapshot();
        var nextSnapshot = _redoStack.Pop();
        _undoStack.Push(currentSnapshot);
        RestoreTemplateSnapshot(nextSnapshot);
        StatusText = "Làm lại";
        RaiseHistoryCanExecuteChanged();
    }

    private void ObserveTemplate(LabelTemplate template)
    {
        template.PropertyChanged += TemplateOnPropertyChanged;
        template.PrinterProfile.PropertyChanged += PrinterProfileOnPropertyChanged;
        template.Objects.CollectionChanged += ObjectsOnCollectionChanged;
        template.Guides.CollectionChanged += GuidesOnCollectionChanged;
        foreach (var item in template.Objects)
        {
            ObserveObject(item);
        }

        foreach (var guide in template.Guides)
        {
            ObserveGuide(guide);
        }

        RefreshObjectTreeBindingStates();
    }

    private void UnobserveTemplate(LabelTemplate template)
    {
        template.PropertyChanged -= TemplateOnPropertyChanged;
        template.PrinterProfile.PropertyChanged -= PrinterProfileOnPropertyChanged;
        template.Objects.CollectionChanged -= ObjectsOnCollectionChanged;
        template.Guides.CollectionChanged -= GuidesOnCollectionChanged;
        foreach (var item in template.Objects)
        {
            UnobserveObject(item);
        }

        foreach (var guide in template.Guides)
        {
            UnobserveGuide(guide);
        }
    }

    private void ObserveObject(LabelObject item)
    {
        NormalizeTextObjectPolicy(item);
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

    private void ObserveGuide(LabelGuide guide)
    {
        guide.PropertyChanged -= GuideOnPropertyChanged;
        guide.PropertyChanged += GuideOnPropertyChanged;
    }

    private void UnobserveGuide(LabelGuide guide)
    {
        guide.PropertyChanged -= GuideOnPropertyChanged;
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

        if (e.PropertyName == nameof(PrinterProfile.PrinterName))
        {
            OnPropertyChanged(nameof(PrinterDisplayName));
            _ = RefreshPrinterQueueStatusAsync();
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

    private void GuidesOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
        {
            foreach (var guide in e.OldItems.OfType<LabelGuide>())
            {
                UnobserveGuide(guide);
            }
        }

        if (e.NewItems is not null)
        {
            foreach (var guide in e.NewItems.OfType<LabelGuide>())
            {
                ObserveGuide(guide);
            }
        }

        RecordTemplateChange();
    }

    private void GuideOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
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
            if (e.PropertyName == nameof(LabelObject.Type))
            {
                NormalizeTextObjectPolicy(changedObject);
            }

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
                OnPropertyChanged(nameof(BarcodeApplicationValidationMessage));
            }

            if (e.PropertyName is nameof(LabelObject.BarcodeApplicationProfile)
                or nameof(LabelObject.QrQuietZoneModules)
                or nameof(LabelObject.ShowBarcodeText)
                or nameof(LabelObject.BarcodeHriPlacement)
                or nameof(LabelObject.BarcodeTextFontSizePt)
                or nameof(LabelObject.BarcodeCheckDigitPolicy)
                or nameof(LabelObject.BarcodeHriShowCheckDigit))
            {
                OnPropertyChanged(nameof(BarcodeApplicationValidationMessage));
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
                or nameof(LabelObject.QrDpi)
                or nameof(LabelObject.BarcodeModuleWidthMm)
                or nameof(LabelObject.BarcodeWidthMode)
                or nameof(LabelObject.Code39WideNarrowRatio)
                or nameof(LabelObject.BearerBarStyle)
                or nameof(LabelObject.BearerBarThicknessMm)
                or nameof(LabelObject.WidthMm)
                or nameof(LabelObject.Text)
                or nameof(LabelObject.QrQuietZoneModules)
                or nameof(LabelObject.BarcodeApplicationProfile))
            {
                if (e.PropertyName is not nameof(LabelObject.WidthMm))
                {
                    TryApplySizedFromXWidth(SelectedObject);
                }

                OnPropertyChanged(nameof(BarcodeModuleSizeWarningText));
                OnPropertyChanged(nameof(BarcodeEffectiveModuleReadoutText));
                OnPropertyChanged(nameof(BarcodePhysicalQuietZoneText));
                OnPropertyChanged(nameof(SelectedObjectSizeFromX));
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

    private static void NormalizeTextObjectPolicies(LabelTemplate template)
    {
        ArgumentNullException.ThrowIfNull(template);
        foreach (var item in template.Objects)
        {
            NormalizeTextObjectPolicy(item);
        }
    }

    private static void NormalizeTextObjectPolicy(LabelObject item)
    {
        if (item.Type == ObjectType.Text)
        {
            // AutoFit: content grows the selection (NiceLabel Text default).
            // FixedFrame on Text: user locked the selection by border-drag;
            // glyphs still free-flow/compress via shared layout — never TextBox
            // wrap/clip ownership (ShouldConstrainToBox stays false).
            if (item.Style.TextSizing is not (TextSizingMode.AutoFit or TextSizingMode.FixedFrame))
            {
                item.Style.TextSizing = TextSizingMode.AutoFit;
            }

            item.Style.TextOverflow = TextOverflowMode.AllowOverflow;
            return;
        }

        if (item.Type != ObjectType.TextBox)
        {
            return;
        }

        if (item.Style.TextSizing is TextSizingMode.AutoFit or TextSizingMode.AdjustHeight)
        {
            item.Style.TextSizing = TextSizingMode.FixedFrame;
        }

        if (item.Style.TextOverflow == TextOverflowMode.AllowOverflow)
        {
            item.Style.TextOverflow = TextOverflowMode.Error;
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

        var targetSizeMm = QrObjectGeometryContract.ResolveTargetSizeMm(
            item,
            string.IsNullOrWhiteSpace(item.BindingExpression)
                ? item.Text
                : ResolveExpression(item.BindingExpression, PreviewRow),
            GetAvailableQrSizeMm(item),
            _qrCapacityTable);
        if (targetSizeMm is null)
        {
            return;
        }

        if (!QrObjectGeometryContract.HasMeaningfulSizeDelta(item, targetSizeMm.Value))
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
        return item.IsSquare2DCodeLike();
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

    private string ValidateSelectedBarcodeApplication()
    {
        if (SelectedObject is not { Type: ObjectType.BarcodeCode128 or ObjectType.QRCode or ObjectType.DataMatrix } item)
        {
            return string.Empty;
        }

        var symbology = item.Type switch
        {
            ObjectType.QRCode => BarcodeSymbology.QRCode,
            ObjectType.DataMatrix => BarcodeSymbology.DataMatrix,
            _ => item.BarcodeSymbology
        };
        var geometryErrors = BarcodeApplicationContract.ValidateGeometry(
            item.BarcodeApplicationProfile,
            symbology,
            item.QrQuietZoneModules,
            item.ShowBarcodeText,
            item.BarcodeTextFontSizePt);
        var data = string.IsNullOrWhiteSpace(item.BindingExpression) ? item.Text : ResolveExpression(item.BindingExpression, PreviewRow);
        var dataErrors = BarcodeApplicationContract.ValidateData(item.BarcodeApplicationProfile, symbology, data);
        var errors = geometryErrors.Concat(dataErrors).ToList();
        var type = item.Type switch
        {
            ObjectType.QRCode => BarcodeType.QRCode,
            ObjectType.DataMatrix => BarcodeType.DataMatrix,
            _ => ANLAbel.App.Controls.BarcodeTypeMapper.ToRendererType(item.BarcodeSymbology)
        };
        var hriLayout = BarcodeHriTextLayout.Measure(
            type,
            data,
            item.WidthMm,
            item.HeightMm,
            item.BarcodeHriPlacement,
            item.BarcodeTextFontSizePt);
        if (!hriLayout.IsValid && hriLayout.ErrorMessage is not null)
        {
            errors.Add(hriLayout.ErrorMessage);
        }

        return string.Join(Environment.NewLine, errors);
    }

    private string ValidateSelectedTextBox()
    {
        if (SelectedObject is not { } item || item.Type is not (ObjectType.Text or ObjectType.TextBox))
        {
            return string.Empty;
        }

        if (!string.IsNullOrWhiteSpace(item.Style.FontFamily)
            && !TextBoxOverflowDetector.IsFontAvailable(item.Style.FontFamily))
        {
            var fallback = TextBoxOverflowDetector.ResolveFontFamilyName(item.Style.FontFamily);
            return $"Font '{item.Style.FontFamily}' is unavailable; preview/print use '{fallback}'. Install it or choose an installed family.";
        }

        if (item.Type != ObjectType.TextBox)
        {
            return string.Empty;
        }

        var data = string.IsNullOrWhiteSpace(item.BindingExpression) ? item.Text : ResolveExpression(item.BindingExpression, PreviewRow);
        return TextBoxOverflowDetector.ShouldBlockOverflow(item)
            && IsTextBoxOverflowing(item, data)
            ? "Text box overflow: increase the object size or reduce text/font size."
            : string.Empty;
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

        var hriLayout = BarcodeHriTextLayout.Measure(
            type,
            data,
            item.WidthMm,
            item.HeightMm,
            item.BarcodeHriPlacement,
            item.BarcodeTextFontSizePt);
        if (!hriLayout.IsValid)
        {
            return hriLayout.ErrorMessage;
        }

        try
        {
            var symbolHeightMm = hriLayout.IsEnabled ? hriLayout.SymbolHeightMm : item.HeightMm;
            _barcodeValidator.RenderBarcode(data, type, item.WidthMm, symbolHeightMm, item.QrDpi, CreateBarcodeRenderOptions(item));
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
            QuietZoneModules = item.QrQuietZoneModules,
            IsGs1 = item.BarcodeApplicationProfile == BarcodeApplicationProfile.Gs1
        };
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
                "Công thức",
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
            "Trường giữ chỗ",
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
            StatusText = $"Vấn đề liên kết đối tượng: {SelectedObject.Name}";
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
                ? "Liên kết bị thiếu 1 trường trong bảng tính hiện tại."
                : $"Liên kết bị thiếu {missingFields.Count} trường trong bảng tính hiện tại.";
        }

        if (errors.Count > 0)
        {
            return "Liên kết có lỗi xác thực.";
        }

        return hasPreviewRow
            ? "Liên kết đã khớp với dòng xem trước Excel hiện tại."
            : "Liên kết hợp lệ. Hãy nhập hoặc chọn một dòng Excel để xem trước kết quả.";
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

        // A canvas drag/resize/draw explicitly owns one history transaction.
        // Property notifications during the gesture do not serialize the full
        // template at every pointer tick.  CommitTemplateEditGesture captures
        // the one final snapshot; cancel restores the one start snapshot.
        // This keeps barcode/image-heavy drags off the JSON/history hot path.
        if (_explicitEditGestureActive)
        {
            return;
        }

        var currentSnapshot = CaptureTemplateSnapshot();
        UpdateDirtyState(currentSnapshot);
        if (currentSnapshot == _lastTemplateSnapshot)
        {
            return;
        }

        // Debounce: accumulate rapid changes into one undo step
        if (_debounceActive)
        {
            // Extend the gesture window on every property tick.  A long drag
            // must become one undo step, not several 300 ms fragments.
            _pendingSnapshot = currentSnapshot;
            _debounceTimer?.Stop();
            _debounceTimer?.Start();
            return;
        }

        // First change in a burst: save the pre-change state and start debounce
        _debounceActive = true;
        _pendingPreChangeSnapshot = _lastTemplateSnapshot;
        _pendingSnapshot = currentSnapshot;
        _lastTemplateSnapshot = currentSnapshot;

        // Start timer - when it fires, commit the undo step
        _debounceTimer?.Stop();
        _debounceTimer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(300)
        };
        _debounceTimer.Tick += (_, _) => CommitPendingHistory();
        _debounceTimer.Start();
    }

    private void CommitPendingHistory()
    {
        if (!_debounceActive)
        {
            return;
        }

        _debounceTimer?.Stop();
        _debounceTimer = null;
        _debounceActive = false;

        var preChangeSnapshot = _pendingPreChangeSnapshot;
        var finalSnapshot = CaptureTemplateSnapshot();
        _pendingPreChangeSnapshot = string.Empty;
        _pendingSnapshot = string.Empty;

        // Push the state before the gesture started, not an intermediate
        // property tick.  This also makes Undo deterministic if invoked while
        // the debounce timer is still pending.
        if (!string.IsNullOrEmpty(preChangeSnapshot) && preChangeSnapshot != finalSnapshot)
        {
            _undoStack.Push(preChangeSnapshot);
            TrimUndoStack();
            _redoStack.Clear();
            _lastTemplateSnapshot = finalSnapshot;
        }

        UpdateDirtyState(finalSnapshot);
        RaiseHistoryCanExecuteChanged();
    }

    private void CancelPendingHistory()
    {
        _debounceTimer?.Stop();
        _debounceTimer = null;
        _debounceActive = false;
        _pendingPreChangeSnapshot = string.Empty;
        _pendingSnapshot = string.Empty;
    }

    /// <summary>
    /// Starts an explicit canvas gesture transaction. This is intentionally
    /// public so the retained WPF canvas can bracket move/resize/draw sessions
    /// without reaching into snapshot implementation details.
    /// </summary>
    public void BeginTemplateEditGesture()
    {
        if (_isRestoringHistory || _explicitEditGestureActive)
        {
            return;
        }

        CommitPendingHistory();
        _explicitEditGesturePreChangeSnapshot = CaptureTemplateSnapshot();
        _lastTemplateSnapshot = _explicitEditGesturePreChangeSnapshot;
        _explicitEditGestureActive = true;
    }

    /// <summary>
    /// Commits one explicit canvas gesture as one undo step, regardless of how
    /// many property ticks the pointer generated.
    /// </summary>
    public void CommitTemplateEditGesture()
    {
        if (!_explicitEditGestureActive)
        {
            return;
        }

        var preChangeSnapshot = _explicitEditGesturePreChangeSnapshot;
        var finalSnapshot = CaptureTemplateSnapshot();
        _explicitEditGesturePreChangeSnapshot = string.Empty;
        _explicitEditGestureActive = false;
        if (!string.IsNullOrEmpty(preChangeSnapshot) && preChangeSnapshot != finalSnapshot)
        {
            _undoStack.Push(preChangeSnapshot);
            TrimUndoStack();
            _redoStack.Clear();
        }

        _lastTemplateSnapshot = finalSnapshot;
        UpdateDirtyState(finalSnapshot);
        RaiseHistoryCanExecuteChanged();
    }

    /// <summary>
    /// Cancels a canvas gesture and restores the exact pre-gesture snapshot.
    /// Capture loss/Esc therefore cannot leave a half-applied edit in history.
    /// </summary>
    public void CancelTemplateEditGesture()
    {
        if (!_explicitEditGestureActive)
        {
            return;
        }

        var preChangeSnapshot = _explicitEditGesturePreChangeSnapshot;
        _explicitEditGesturePreChangeSnapshot = string.Empty;
        _explicitEditGestureActive = false;
        CancelPendingHistory();
        if (!string.IsNullOrEmpty(preChangeSnapshot) && preChangeSnapshot != CaptureTemplateSnapshot())
        {
            RestoreTemplateSnapshot(preChangeSnapshot);
        }

        _lastTemplateSnapshot = CaptureTemplateSnapshot();
        UpdateDirtyState(_lastTemplateSnapshot);
        RaiseHistoryCanExecuteChanged();
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

        UpdateDirtyState(snapshot);
    }

    private string CaptureTemplateSnapshot()
    {
        return JsonSerializer.Serialize(Template, HistoryJsonOptions);
    }

    private void ResetHistory()
    {
        _explicitEditGestureActive = false;
        _explicitEditGesturePreChangeSnapshot = string.Empty;
        CancelPendingHistory();
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

    private static void RaiseCommandCanExecuteChanged(ICommand command)
    {
        switch (command)
        {
            case RelayCommand relay:
                relay.RaiseCanExecuteChanged();
                break;
            case AsyncRelayCommand async:
                async.RaiseCanExecuteChanged();
                break;
        }
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
            Name = "Mẫu ANLAbel",
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
        => TextStylePickerCatalog.FilterInstalled(
            Fonts.SystemFontFamilies.Select(font => font.Source));

    private IReadOnlyDictionary<string, string>? CreatePreviewRow(object? item)
    {
        var row = CreatePreviewRow(item, out var error);
        DataTransformError = error;
        return row;
    }

    private IReadOnlyDictionary<string, string>? CreatePreviewRow(object? item, out string error)
    {
        error = string.Empty;
        if (item is not DataRowView rowView)
        {
            return null;
        }

        var dataRow = rowView.Row;
        if (dataRow.RowState is DataRowState.Deleted or DataRowState.Detached)
        {
            error = "A linked Excel/CSV row is no longer available.";
            return null;
        }

        var source = dataRow.Table.Columns
            .Cast<DataColumn>()
            .ToDictionary(column => column.ColumnName, column => dataRow[column]?.ToString() ?? string.Empty, StringComparer.OrdinalIgnoreCase);
        if (Template.DataTransforms.Count == 0)
        {
            return source;
        }

        var transformed = DataTransformPipeline.Evaluate(
            DataRecord.Create(source.Select(pair => new KeyValuePair<string, string?>(pair.Key, pair.Value))),
            Template.DataTransforms);
        if (!transformed.IsValid)
        {
            error = string.Join(" ", transformed.Errors);
            return source;
        }

        return transformed.Record.Values.ToDictionary(pair => pair.Key, pair => pair.Value ?? string.Empty, StringComparer.OrdinalIgnoreCase);
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

    private async Task<TrackedPrintResult> DispatchTrackedPrintAsync(
        IReadOnlyList<IReadOnlyDictionary<string, string>?> rows,
        string description,
        string? existingJobId = null,
        PrintJobManifest? approvedManifest = null,
        int? sourceRowCount = null,
        IReadOnlyList<PrintDispatchCopy>? sequence = null)
    {
        var jobId = string.IsNullOrWhiteSpace(existingJobId) ? Guid.NewGuid().ToString("N") : existingJobId;
        // Plan 13 GD5 (H17/H22, P03/P05): one immutable snapshot feeds the
        // printer contract, preflight, manifest and dispatch, so an edit made
        // while the driver or validation is busy cannot mix two label states.
        var printTemplate = PrintSessionTemplateContract.Capture(Template);
        var printerName = printTemplate.PrinterProfile.PrinterName;
        if (string.IsNullOrWhiteSpace(printerName))
        {
            throw new InvalidOperationException(
                "Print preparation stopped because no verified printer queue is selected. Choose the industrial queue saved in the template before using quick print.");
        }

        // Quick print is a durable dispatch path, not a design-only preview.
        // Resolve the queue's effective ticket first so the manifest and every
        // lifecycle event carry the same DPI/media/imageable-area contract that
        // the paginator will receive.  This also prevents the old overload that
        // opened a second print dialog and could send to a different queue than
        // the one named in the manifest.
        // Drivers can block while opening a queue; resolve the contract off the
        // UI thread like Print Preview does (H17).
        var effectivePlan = await _printService.CreateEffectivePlanAsync(printTemplate, printerName).ConfigureAwait(true);
        if (!effectivePlan.SceneCompilationVerified)
        {
            var detail = string.IsNullOrWhiteSpace(effectivePlan.SceneDiagnostics)
                ? "the scene compiler did not produce a verified scene hash"
                : effectivePlan.SceneDiagnostics;
            throw new InvalidOperationException(
                $"Print preparation stopped because the label design is invalid ({detail}). Fix the design and try again.");
        }

        var preflight = await _printService.ValidateRowsAsync(printTemplate, rows, effectivePlan).ConfigureAwait(true);
        if (!preflight.IsSuccess)
        {
            throw new InvalidOperationException(preflight.ToUserMessage());
        }

        var (labelCount, resolvedSourceRowCount) = PrintJobEvidencePolicy.ResolveManifestCounts(
            rows.Count,
            sourceRowCount);
        var manifest = PrintJobManifest.Create(
            printTemplate.Name,
            CurrentFilePath,
            description,
            printerName,
            printTemplate.WidthMm,
            printTemplate.HeightMm,
            effectivePlan.DpiX > 0 ? effectivePlan.DpiX : printTemplate.PrinterProfile.Dpi,
            effectivePlan.DpiY > 0 ? effectivePlan.DpiY : printTemplate.PrinterProfile.Dpi,
            labelCount,
            resolvedSourceRowCount,
            rows,
            effectivePlan.DocumentHash,
            effectivePlan.TextResourceFingerprint,
            effectivePlan.SceneHash,
            effectivePlan.OutputContractHash,
            imageRasterFingerprint: effectivePlan.ImageRasterFingerprint,
            thermalRasterGoldenFingerprint: effectivePlan.ThermalRasterGolden?.Fingerprint ?? string.Empty,
            sequence: sequence);
        if (approvedManifest is not null
            && !string.Equals(manifest.Fingerprint, approvedManifest.Fingerprint, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Print dispatch was blocked because the newly compiled inputs do not match the approved manifest.");
        }
        await RecordPrintJobTransitionAsync(new PrintJobStateTransition(
            jobId,
            PrintJobLifecycleState.Created,
            PrintJobLifecycleState.Preparing,
            DateTimeOffset.UtcNow,
            "Main designer accepted the batch for preparation.",
            PrinterName: printerName,
            DocumentHash: effectivePlan.DocumentHash,
            TextResourceFingerprint: effectivePlan.TextResourceFingerprint,
            SceneHash: effectivePlan.SceneHash,
            ManifestFingerprint: manifest.Fingerprint,
            Manifest: manifest));
        await RecordPrintJobTransitionAsync(new PrintJobStateTransition(
            jobId,
            PrintJobLifecycleState.Preparing,
            PrintJobLifecycleState.PreflightPassed,
            DateTimeOffset.UtcNow,
            "Effective printer-ticket validation and row preflight passed for the batch.",
            PrinterName: printerName,
            DocumentHash: effectivePlan.DocumentHash,
            TextResourceFingerprint: effectivePlan.TextResourceFingerprint,
            SceneHash: effectivePlan.SceneHash,
            OutputContractHash: effectivePlan.OutputContractHash,
            ManifestFingerprint: manifest.Fingerprint,
            Manifest: manifest));
        await RecordPrintJobTransitionAsync(new PrintJobStateTransition(
            jobId,
            PrintJobLifecycleState.PreflightPassed,
            PrintJobLifecycleState.Dispatching,
            DateTimeOffset.UtcNow,
            "Dispatching the validated batch.",
            PrinterName: printerName,
            DocumentHash: effectivePlan.DocumentHash,
            TextResourceFingerprint: effectivePlan.TextResourceFingerprint,
            SceneHash: effectivePlan.SceneHash,
            OutputContractHash: effectivePlan.OutputContractHash,
            ManifestFingerprint: manifest.Fingerprint,
            Manifest: manifest));

        PrintJobResult? dispatched = null;
        PrintJobResult result;
        try
        {
            dispatched = await _printService.PrintRowsWithResultAsync(
                printTemplate,
                rows,
                printerName,
                description,
                expectedOutputContractHash: effectivePlan.OutputContractHash);
            result = dispatched with { ManifestFingerprint = manifest.Fingerprint, Manifest = manifest };
            result = await _printService.ResolveSpoolJobIdentityAsync(
                result,
                timeout: TimeSpan.FromSeconds(1),
                pollInterval: TimeSpan.FromMilliseconds(100));
        }
        catch (Exception ex)
        {
            // P07: once the spooler accepted the job, a later failure (for
            // example while reading its queue identity) leaves the outcome
            // uncertain. Recording Failed would invite a duplicate reprint.
            var failureState = dispatched?.IsAccepted == true
                ? PrintJobLifecycleState.Unknown
                : PrintJobLifecycleState.Failed;
            var current = _printJobStateStore.GetCurrentState(jobId);
            if (current is PrintJobLifecycleState currentState
                && PrintJobStateMachine.CanTransition(currentState, failureState))
            {
                await RecordPrintJobTransitionAsync(new PrintJobStateTransition(
                    jobId,
                    currentState,
                    failureState,
                    DateTimeOffset.UtcNow,
                    ex.Message,
                    PrinterName: printerName,
                    ManifestFingerprint: manifest.Fingerprint,
                    Manifest: manifest));
            }

            throw;
        }

        var targetState = result.Outcome switch
        {
            PrintJobOutcome.Cancelled => PrintJobLifecycleState.Cancelled,
            PrintJobOutcome.Failed => PrintJobLifecycleState.Failed,
            PrintJobOutcome.Unknown => PrintJobLifecycleState.Unknown,
            PrintJobOutcome.Completed when result.IsPhysicalCompletionVerified => PrintJobLifecycleState.Completed,
            PrintJobOutcome.SpoolAccepted or PrintJobOutcome.DeviceAcknowledged => PrintJobLifecycleState.SpoolAccepted,
            _ => PrintJobLifecycleState.Unknown
        };
        await RecordPrintJobTransitionAsync(new PrintJobStateTransition(
            jobId,
            PrintJobLifecycleState.Dispatching,
            targetState,
            DateTimeOffset.UtcNow,
            result.UserFacingStatus,
            PrinterName: result.PrinterName,
            SpoolJobId: result.SpoolJobId,
            DocumentHash: result.DocumentHash,
            TextResourceFingerprint: result.TextResourceFingerprint,
            SceneHash: result.SceneHash,
            OutputContractHash: result.OutputContractHash,
            ManifestFingerprint: result.ManifestFingerprint,
            Manifest: result.Manifest,
            PhysicalOutputVerified: result.IsPhysicalCompletionVerified));

        SpoolJobMonitorResult? spoolStatus = null;
        if (result.IsAccepted && result.SpoolJobId is int)
        {
            // Quick-print used to stop at the submit return value. Observe the
            // queue here as well as in Print Preview so both operator paths have
            // the same bounded, truthful status semantics. This is read-only and
            // never upgrades a queue observation to physical completion.
            spoolStatus = await _printService.MonitorSpoolJobAsync(
                result,
                timeout: TimeSpan.FromSeconds(3),
                pollInterval: TimeSpan.FromMilliseconds(250));

            if (targetState == PrintJobLifecycleState.SpoolAccepted)
            {
                await RecordPrintJobTransitionAsync(new PrintJobStateTransition(
                    jobId,
                    PrintJobLifecycleState.SpoolAccepted,
                    PrintJobLifecycleState.QueueObserved,
                    spoolStatus.FinalObservation.ObservedAtUtc ?? DateTimeOffset.UtcNow,
                    spoolStatus.FinalObservation.Message,
                    PrinterName: result.PrinterName,
                    SpoolJobId: result.SpoolJobId,
                    QueueState: spoolStatus.FinalObservation.State.ToString(),
                    DocumentHash: result.DocumentHash,
                    TextResourceFingerprint: result.TextResourceFingerprint,
                    SceneHash: result.SceneHash,
                    OutputContractHash: result.OutputContractHash,
                    ManifestFingerprint: result.ManifestFingerprint,
                    Manifest: result.Manifest));
            }
        }

        return new TrackedPrintResult(jobId, result, spoolStatus);
    }

    private async Task RecordPrintJobTransitionAsync(PrintJobStateTransition transition)
    {
        await PrintJobEvidenceAppend.AppendAsync(_printJobStateStore, transition).ConfigureAwait(true);
    }

    public async Task WritePrintLogAsync(
        string printMode,
        IEnumerable<IReadOnlyDictionary<string, string>?> rows,
        int rowCount,
        int labelCount,
        string notes = "",
        PrintJobResult? result = null,
        string jobId = "",
        SpoolJobMonitorResult? spoolStatus = null)
    {
        LogPrintOperation(
            printMode,
            rowCount,
            labelCount,
            success: result?.IsAccepted ?? true,
            errorMessage: result?.ErrorMessage ?? string.Empty,
            result: result,
            jobId: jobId,
            spoolStatus: spoolStatus);
        var effectiveNotes = string.IsNullOrWhiteSpace(notes)
            ? AppendQueueStatus(result?.UserFacingStatus ?? "Đã ghi nhận yêu cầu in. Kết quả in vật lý chưa được xác minh độc lập.", spoolStatus)
            : notes;
        try
        {
            var printedAt = DateTime.Now;
            var entries = rows.Select((row, index) => CreatePrintLogEntry(printMode, row, rowCount, labelCount, index + 1, printedAt, effectiveNotes)).ToArray();
            await _printLogService.AppendManyAsync(entries);
        }
        catch (Exception ex)
        {
            StatusText = $"Đã gửi lệnh in, nhưng ghi nhật ký thất bại: {ex.Message}";
        }
    }

    /// <summary>
    /// Fire-and-forget job-level print trace (print-preview-reliability-plan.md item 3).
    /// Separate from the per-label print-history.csv log written above — this is a
    /// machine-parseable JSON trace, never awaited and never allowed to affect the print
    /// job or the (already saved) human-facing history.
    /// </summary>
    private void LogPrintOperation(
        string printMode,
        int rowsSelected,
        int labelsPrinted,
        bool success,
        string errorMessage,
        PrintJobResult? result = null,
        string jobId = "",
        SpoolJobMonitorResult? spoolStatus = null)
    {
        var entry = new PrintOperationLogEntry
        {
            JobId = jobId,
            TemplateName = Template.Name,
            TemplateFilePath = CurrentFilePath,
            PrinterName = string.IsNullOrWhiteSpace(result?.PrinterName) ? Template.PrinterProfile.PrinterName : result.PrinterName,
            LabelWidthMm = Template.PrinterProfile.LabelWidthMm,
            LabelHeightMm = Template.PrinterProfile.LabelHeightMm,
            Dpi = result?.DpiX > 0 ? result.DpiX : Template.PrinterProfile.Dpi,
            DpiX = result?.DpiX > 0 ? result.DpiX : Template.PrinterProfile.Dpi,
            DpiY = result?.DpiY > 0 ? result.DpiY : Template.PrinterProfile.Dpi,
            PrintMode = printMode,
            PrintMethod = Template.PrinterProfile.PrintMethod.ToString(),
            NativeCommandsUsed = Template.PrinterProfile.PrintMethod == PrintMethod.PrinterNative,
            Outcome = result?.Outcome.ToString() ?? (success ? PrintJobOutcome.SpoolAccepted.ToString() : PrintJobOutcome.Failed.ToString()),
            OutcomeEvidence = result?.IsPhysicalCompletionVerified == true
                ? "device-confirmed"
                : result?.OutputContractTicketVerified == true
                    ? result.PrintableAreaVerified ? "effective-ticket-and-imageable-area; physical-output-unverified" : "effective-ticket; printable-area-unverified; physical-output-unverified"
                    : "output-contract-ticket-unverified; physical-output-unverified",
            SpoolJobId = result?.SpoolJobId,
            SpoolState = spoolStatus?.FinalObservation.State.ToString() ?? string.Empty,
            SpoolStatusMessage = spoolStatus?.FinalObservation.Message ?? string.Empty,
            SpoolStatusPollCount = spoolStatus?.PollCount ?? 0,
            SpoolStatusTimedOut = spoolStatus?.TimedOut ?? false,
            SpoolStatusObservedAtUtc = spoolStatus?.FinalObservation.ObservedAtUtc,
            OutputContractHash = result?.OutputContractHash ?? string.Empty,
            OutputContractTicketVerified = result?.OutputContractTicketVerified == true,
            DocumentHash = result?.DocumentHash ?? string.Empty,
            TextResourceFingerprint = result?.TextResourceFingerprint ?? string.Empty,
            ImageRasterFingerprint = result?.ImageRasterFingerprint ?? string.Empty,
            ThermalRasterGoldenFingerprint = result?.ThermalRasterGoldenFingerprint ?? string.Empty,
            ManifestFingerprint = result?.ManifestFingerprint ?? string.Empty,
            Manifest = result?.Manifest,
            SupportEvidenceFingerprint = result?.SupportEvidenceFingerprint ?? string.Empty,
            SceneHash = result?.SceneHash ?? string.Empty,
            SceneCompilationVerified = result?.SceneCompilationVerified == true,
            RowsSelected = rowsSelected,
            LabelsPrinted = labelsPrinted,
            Success = success,
            ErrorMessage = errorMessage
        };
        _ = _printOperationLogService.AppendAsync(entry);
    }

    private void LogOperatorAction(PrintJobOperatorActionResult result)
    {
        var stateEvent = result.Event;
        var entry = new PrintOperationLogEntry
        {
            JobId = result.JobId,
            TimestampLocal = DateTime.Now,
            TemplateName = Template.Name,
            TemplateFilePath = CurrentFilePath,
            PrinterName = stateEvent?.PrinterName ?? Template.PrinterProfile.PrinterName,
            LabelWidthMm = Template.PrinterProfile.LabelWidthMm,
            LabelHeightMm = Template.PrinterProfile.LabelHeightMm,
            Dpi = Template.PrinterProfile.Dpi,
            DpiX = Template.PrinterProfile.Dpi,
            DpiY = Template.PrinterProfile.Dpi,
            PrintMode = "OperatorRecovery",
            Outcome = result.Action.ToString(),
            OutcomeEvidence = "lineage-only; physical-output-unverified; automatic-retry-disabled",
            SpoolJobId = stateEvent?.SpoolJobId,
            SpoolState = stateEvent?.QueueState ?? string.Empty,
            OutputContractHash = stateEvent?.OutputContractHash ?? string.Empty,
            DocumentHash = stateEvent?.DocumentHash ?? string.Empty,
            TextResourceFingerprint = stateEvent?.TextResourceFingerprint ?? string.Empty,
            ManifestFingerprint = stateEvent?.ManifestFingerprint ?? string.Empty,
            Manifest = stateEvent?.Manifest,
            SceneHash = stateEvent?.SceneHash ?? string.Empty,
            OperatorAction = result.Action.ToString(),
            RelatedJobId = result.RelatedJobId,
            OperatorActor = stateEvent?.Actor ?? string.Empty,
            Success = result.Succeeded,
            ErrorMessage = result.Succeeded ? string.Empty : result.Summary
        };
        _ = _printOperationLogService.AppendAsync(entry);
    }

    private static string AppendQueueStatus(string status, SpoolJobMonitorResult? spoolStatus)
    {
        if (spoolStatus is null)
        {
            return status;
        }

        return $"{status}\nTrạng thái hàng đợi: {spoolStatus.UserFacingStatus}";
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
                StatusText = "Tệp lịch sử in chưa được tạo";
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
            StatusText = $"Không thể mở lịch sử in: {ex.Message}";
        }
    }
}

public sealed record TrackedPrintResult(
    string JobId,
    PrintJobResult Result,
    SpoolJobMonitorResult? SpoolStatus = null);

public sealed record NewTemplateRequest(string Name, double WidthMm, double HeightMm, int Dpi);

public sealed record FormulaFunctionTemplate(string Name, string Template, string Description);

public enum FormulaBuilderPartKind
{
    Field,
    Text
}

public sealed record FormulaBuilderPart(FormulaBuilderPartKind Kind, string Value, string DisplayText)
{
    public string KindText => Kind == FormulaBuilderPartKind.Field ? "Trường" : "Văn bản";
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
    public string MissingFieldsSummary => MissingFields.Count == 0 ? string.Empty : $"Thiếu: {string.Join(", ", MissingFields)}";
    public string ErrorsSummary => Errors.Count == 0 ? string.Empty : string.Join(Environment.NewLine, Errors);
}
