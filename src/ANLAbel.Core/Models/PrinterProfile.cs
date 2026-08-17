using ANLAbel.Core.Mvvm;
using ANLAbel.Core.Enums;

namespace ANLAbel.Core.Models;

public sealed class PrinterProfile : ObservableObject
{
    private string _printerName = string.Empty;
    private string _paperName = string.Empty;
    private PrinterSettingsSource _settingsSource = PrinterSettingsSource.Label;
    private PaperSizeSource _paperSizeSource = PaperSizeSource.DriverAutomatic;
    private LabelMediaType _mediaType = LabelMediaType.Gap;
    private FeedDirection _feedDirection = FeedDirection.TopToBottom;
    private bool _rotated180;
    private int _dpi = 203;
    private double _labelWidthMm = 100;
    private double _labelHeightMm = 50;
    private double _gapMm;
    private double _offsetXMm;
    private double _offsetYMm;
    private double _scaleX = 1;
    private double _scaleY = 1;
    private double _physicalWidthMm;
    private double _physicalHeightMm;
    private PrintMethod _printMethod = PrintMethod.ApplicationGraphic;

    public string PrinterName
    {
        get => _printerName;
        set => SetProperty(ref _printerName, value);
    }

    public string PaperName
    {
        get => _paperName;
        set => SetProperty(ref _paperName, value);
    }

    public PrinterSettingsSource SettingsSource
    {
        get => _settingsSource;
        set => SetProperty(ref _settingsSource, value);
    }

    public PaperSizeSource PaperSizeSource
    {
        get => _paperSizeSource;
        set => SetProperty(ref _paperSizeSource, value);
    }

    public LabelMediaType MediaType
    {
        get => _mediaType;
        set => SetProperty(ref _mediaType, value);
    }

    public FeedDirection FeedDirection
    {
        get => _feedDirection;
        set => SetProperty(ref _feedDirection, value);
    }

    public bool Rotated180
    {
        get => _rotated180;
        set => SetProperty(ref _rotated180, value);
    }

    public int Dpi
    {
        get => _dpi;
        set => SetProperty(ref _dpi, value);
    }

    public double LabelWidthMm
    {
        get => _labelWidthMm;
        set => SetProperty(ref _labelWidthMm, Math.Max(1, Math.Round(value, 2)));
    }

    public double LabelHeightMm
    {
        get => _labelHeightMm;
        set => SetProperty(ref _labelHeightMm, Math.Max(1, Math.Round(value, 2)));
    }

    public double GapMm
    {
        get => _gapMm;
        set => SetProperty(ref _gapMm, Math.Max(0, Math.Round(value, 2)));
    }

    public double OffsetXMm
    {
        get => _offsetXMm;
        set => SetProperty(ref _offsetXMm, Math.Round(value, 2));
    }

    public double OffsetYMm
    {
        get => _offsetYMm;
        set => SetProperty(ref _offsetYMm, Math.Round(value, 2));
    }

    public double ScaleX
    {
        get => _scaleX;
        set => SetProperty(ref _scaleX, value);
    }

    public double ScaleY
    {
        get => _scaleY;
        set => SetProperty(ref _scaleY, value);
    }

    /// <summary>
    /// Physical paper width in mm (the original size from the label catalog before any orientation swap).
    /// Used by PrintService to set PageMediaSize so the printer driver receives exact paper dimensions.
    /// </summary>
    public double PhysicalWidthMm
    {
        get => _physicalWidthMm;
        set => SetProperty(ref _physicalWidthMm, Math.Max(0, Math.Round(value, 2)));
    }

    /// <summary>
    /// Physical paper height in mm (the original size from the label catalog before any orientation swap).
    /// Used by PrintService to set PageMediaSize so the printer driver receives exact paper dimensions.
    /// </summary>
    public double PhysicalHeightMm
    {
        get => _physicalHeightMm;
        set => SetProperty(ref _physicalHeightMm, Math.Max(0, Math.Round(value, 2)));
    }

    /// <summary>
    /// Print method: ApplicationGraphic (default vector/raster pipeline) or PrinterNative (vendor command stream).
    /// </summary>
    public PrintMethod PrintMethod
    {
        get => _printMethod;
        set => SetProperty(ref _printMethod, value);
    }
}
