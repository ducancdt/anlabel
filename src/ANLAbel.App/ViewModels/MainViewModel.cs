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
using ANLAbel.Barcode.Options;
using ANLAbel.Barcode.Renderers;
using ANLAbel.Core.Barcode;
using ANLAbel.Core.Enums;
using ANLAbel.Core.Expressions;
using ANLAbel.Core.Expressions.Formulas;
using ANLAbel.Core.Geometry;
using ANLAbel.Core.Models;
using ANLAbel.Core.Mvvm;
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

    private static readonly JsonSerializerOptions HistoryJsonOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new JsonStringEnumConverter() }
    };

    public MainViewModel()
        : this(new ProjectFileService(), new ExcelDataService(), new PrintService(), new PrinterDiscoveryService(), new PrintLogService())
    {
    }

    public MainViewModel(IProjectFileService projectFileService, ExcelDataService excelDataService, PrintService printService, PrinterDiscoveryService printerDiscoveryService, PrintLogService printLogService)
    {
        _projectFileService = projectFileService;
        _excelDataService = excelDataService;
        _printService = printService;
        _printerDiscoveryService = printerDiscoveryService;
        _printLogService = printLogService;
        AddTextCommand = new RelayCommand(AddText);
        AddTextBoxCommand = new RelayCommand(AddTextBox);
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
                ((RelayCommand)BindSelectedAsExcelFieldCommand).RaiseCanExecuteChanged();
                ((RelayCommand)ClearSelectedBindingCommand).RaiseCanExecuteChanged();
                ((RelayCommand)DeleteSelectedCommand).RaiseCanExecuteChanged();
                ((RelayCommand)InsertFunctionFormulaCommand).RaiseCanExecuteChanged();
                ((RelayCommand)BindSelectedAsExcelFieldCommand).RaiseCanExecuteChanged();
                ((RelayCommand)ApplyFormulaBuilderCommand).RaiseCanExecuteChanged();
                OnPropertyChanged(nameof(SelectedObjectTypeText));
                OnPropertyChanged(nameof(SelectedObjectSummaryText));
                RaiseFormulaPreviewChanged();
                OnPropertyChanged(nameof(BarcodeValidationMessage));
                OnPropertyChanged(nameof(TextBoxValidationMessage));
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
    public string LinkedExcelSourceText => HasLinkedExcelSource
        ? $"{Path.GetFileName(Template.DatabaseConfig.FilePath)} / {Template.DatabaseConfig.SheetName}"
        : "No Excel file linked";
    public string CurrentExcelRowText => ExcelDataView is null || ExcelDataView.Count == 0 || SelectedDataItem is not DataRowView rowView
        ? "No Excel row selected"
        : $"Row {GetDataRowViewIndex(rowView) + 1} of {ExcelDataView.Count}";
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

    private void ShowAllPanels()
    {
        IsToolboxVisible = true;
        IsPropertiesVisible = true;
        StatusText = "Workspace panels restored";
    }

    public void NewTemplate(NewTemplateRequest? request)
    {
        request ??= new NewTemplateRequest("Untitled Label", 100, 50, 203);
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

    public IReadOnlyList<string> GetExcelSheetNames(string filePath)
    {
        return _excelDataService.GetSheetNames(filePath);
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

    public async Task ImportExcelAsync(string filePath, string sheetName)
    {
        var table = await _excelDataService.LoadSheetAsync(filePath, sheetName);
        ExcelDataView = table.DefaultView;
        ExcelHeaders.Clear();
        foreach (DataColumn column in table.Columns)
        {
            ExcelHeaders.Add(column.ColumnName);
        }

        Template.DatabaseConfig.FilePath = filePath;
        Template.DatabaseConfig.SheetName = sheetName;
        Template.DatabaseConfig.HeaderRowIndex = 1;
        SyncDatabaseFieldsFromTable(table);
        if (ExcelDataView.Count > 0)
        {
            var targetRowIndex = Math.Max(0, Math.Min(ExcelDataView.Count - 1, Template.DatabaseConfig.LastSelectedRow));
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
        StatusText = $"Imported {table.Rows.Count} rows from {Path.GetFileName(filePath)} / {sheetName}";
    }

    public async Task RefreshExcelDataAsync()
    {
        if (!CanRefreshExcelData())
        {
            StatusText = "No Excel database is linked yet";
            return;
        }

        await ImportExcelAsync(Template.DatabaseConfig.FilePath, Template.DatabaseConfig.SheetName);
        StatusText = $"Excel data refreshed: {Path.GetFileName(Template.DatabaseConfig.FilePath)} / {Template.DatabaseConfig.SheetName}";
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
            StatusText = $"Opened: {Path.GetFileName(CurrentFilePath)}";
            return;
        }

        if (!File.Exists(Template.DatabaseConfig.FilePath))
        {
            ExcelDataView = null;
            PreviewRow = null;
            SelectedDataItem = null;
            ExcelHeaders.Clear();
            OnPropertyChanged(nameof(CurrentExcelRowText));
            StatusText = $"Opened: {Path.GetFileName(CurrentFilePath)}. Linked Excel file not found: {Template.DatabaseConfig.FilePath}";
            return;
        }

        try
        {
            await ImportExcelAsync(Template.DatabaseConfig.FilePath, Template.DatabaseConfig.SheetName);
            StatusText = $"Opened: {Path.GetFileName(CurrentFilePath)}. Excel link restored: {Path.GetFileName(Template.DatabaseConfig.FilePath)} / {Template.DatabaseConfig.SheetName}";
        }
        catch (Exception ex)
        {
            ExcelDataView = null;
            PreviewRow = null;
            SelectedDataItem = null;
            ExcelHeaders.Clear();
            OnPropertyChanged(nameof(CurrentExcelRowText));
            StatusText = $"Opened: {Path.GetFileName(CurrentFilePath)}. Excel link could not be restored: {ex.Message}";
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
                OpenPrintHistoryFile();
            }
        }
        catch (Exception ex)
        {
            StatusText = $"Print failed: {ex.Message}";
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
                OpenPrintHistoryFile();
            }
        }
        catch (Exception ex)
        {
            StatusText = $"Print failed: {ex.Message}";
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
        AddBarcodeWithLinkedText(barcode);
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
        AddBarcodeWithLinkedText(qr);
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
        AddBarcodeWithLinkedText(dm);
    }

    /// <summary>
    /// Adds a barcode/QR object along with a linked text object positioned just below it.
    /// The text object mirrors the barcode's content and moves together with it.
    /// </summary>
    private void AddBarcodeWithLinkedText(LabelObject barcode)
    {
        barcode.ShowBarcodeText = false; // Use linked text instead of built-in text
        barcode.HasLinkedText = true;
        barcode.ZIndex = Template.Objects.Count == 0 ? 1 : Template.Objects.Max(item => item.ZIndex) + 1;
        Template.Objects.Add(barcode);

        var linkedText = new LabelObject
        {
            Type = ObjectType.Text,
            Name = $"{barcode.Name} Text",
            Text = barcode.Text,
            BindingExpression = barcode.BindingExpression,
            XMm = barcode.XMm,
            YMm = barcode.YMm + barcode.HeightMm + 1,
            WidthMm = barcode.WidthMm,
            HeightMm = 5,
            LinkedToId = barcode.Id,
            Style = { FontSizePt = 7, BorderThicknessMm = 0, Alignment = TextAlignmentMode.Center }
        };
        linkedText.ZIndex = barcode.ZIndex + 1;
        Template.Objects.Add(linkedText);

        ObserveObject(linkedText);
        SelectedObject = barcode;
        StatusText = $"Added {barcode.Name} with linked text";
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

        // If this is a barcode, also remove its linked text object
        if (toDelete.Type is ObjectType.BarcodeCode128 or ObjectType.QRCode or ObjectType.DataMatrix)
        {
            var linkedText = Template.Objects.FirstOrDefault(o => o.LinkedToId == toDelete.Id);
            if (linkedText is not null)
            {
                Template.Objects.Remove(linkedText);
            }
        }

        // If this is a linked text, also remove its barcode parent
        if (!string.IsNullOrWhiteSpace(toDelete.LinkedToId))
        {
            var parent = Template.Objects.FirstOrDefault(o => o.Id == toDelete.LinkedToId);
            if (parent is not null)
            {
                Template.Objects.Remove(parent);
            }
        }

        Template.Objects.Remove(toDelete);
        SelectedObject = null;
        StatusText = "Deleted selected object";
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
        }

        // Toggle linked text on/off when HasLinkedText checkbox changes
        if (sender is LabelObject toggleObj
            && toggleObj.Type is ObjectType.BarcodeCode128 or ObjectType.QRCode or ObjectType.DataMatrix
            && e.PropertyName is nameof(LabelObject.HasLinkedText))
        {
            var existingLinked = Template.Objects.FirstOrDefault(o => o.LinkedToId == toggleObj.Id);
            if (toggleObj.HasLinkedText && existingLinked is null)
            {
                // Create linked text
                var linkedText = new LabelObject
                {
                    Type = ObjectType.Text,
                    Name = $"{toggleObj.Name} Text",
                    Text = toggleObj.Text,
                    BindingExpression = toggleObj.BindingExpression,
                    XMm = toggleObj.XMm,
                    YMm = toggleObj.YMm + toggleObj.HeightMm + 1,
                    WidthMm = toggleObj.WidthMm,
                    HeightMm = 5,
                    LinkedToId = toggleObj.Id,
                    Style = { FontSizePt = 7, BorderThicknessMm = 0, Alignment = TextAlignmentMode.Center }
                };
                linkedText.ZIndex = toggleObj.ZIndex + 1;
                Template.Objects.Add(linkedText);
                ObserveObject(linkedText);
                StatusText = "Linked text created";
            }
            else if (!toggleObj.HasLinkedText && existingLinked is not null)
            {
                // Remove linked text
                Template.Objects.Remove(existingLinked);
                StatusText = "Linked text removed";
            }
        }

        // Sync linked text when barcode data changes
        if (sender is LabelObject barcodeObj
            && barcodeObj.Type is ObjectType.BarcodeCode128 or ObjectType.QRCode or ObjectType.DataMatrix
            && e.PropertyName is nameof(LabelObject.Text) or nameof(LabelObject.BindingExpression))
        {
            var linked = Template.Objects.FirstOrDefault(o => o.LinkedToId == barcodeObj.Id);
            if (linked is not null)
            {
                if (e.PropertyName is nameof(LabelObject.Text))
                    linked.Text = barcodeObj.Text;
                if (e.PropertyName is nameof(LabelObject.BindingExpression))
                    linked.BindingExpression = barcodeObj.BindingExpression;
            }
        }

        // Note: linked text position is NOT auto-synced when barcode moves.
        // The user can freely position the linked text independently.
        // Content sync (Text/BindingExpression) is handled above.

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
