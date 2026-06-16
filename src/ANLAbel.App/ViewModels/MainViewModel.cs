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

public sealed partial class MainViewModel : ObservableObject
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

        if (e.PropertyName is nameof(LabelObject.BindingExpression)
            or nameof(LabelObject.Text)
            or nameof(LabelObject.Type)
            or nameof(LabelObject.IsVisible))
        {
            RefreshObjectTreeBindingStates();
        }
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
