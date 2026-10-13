using ANLAbel.Core.Mvvm;
using ANLAbel.Core.Enums;

namespace ANLAbel.Core.Models;

public sealed class PrinterProfile : ObservableObject
{
    private string _printerName = string.Empty;
    private string _paperName = string.Empty;
    private string _labelStockName = string.Empty;
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
    private PrinterMediaProvenance _mediaProvenance = PrinterMediaProvenance.Unknown;
    private string _driverMediaIdentity = string.Empty;
    private string _driverIdentity = string.Empty;
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

    /// <summary>
    /// Name of the software/vendor stock preset used for authored label
    /// geometry. PaperName remains the physical driver media name.
    /// </summary>
    public string LabelStockName
    {
        get => _labelStockName;
        set => SetProperty(ref _labelStockName, value ?? string.Empty);
    }

    public PrinterSettingsSource SettingsSource
    {
        get => _settingsSource;
        set
        {
            if (SetProperty(ref _settingsSource, value))
            {
                if (value == PrinterSettingsSource.Driver && _paperSizeSource != PaperSizeSource.DriverAutomatic)
                {
                    PaperSizeSource = PaperSizeSource.DriverAutomatic;
                }
            }
        }
    }

    public PaperSizeSource PaperSizeSource
    {
        get => _settingsSource == PrinterSettingsSource.Driver ? PaperSizeSource.DriverAutomatic : _paperSizeSource;
        set
        {
            var effective = _settingsSource == PrinterSettingsSource.Driver ? PaperSizeSource.DriverAutomatic : value;
            SetProperty(ref _paperSizeSource, effective);
        }
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
    /// Records who supplied the physical media definition. Software stock
    /// presets must not be mistaken for media advertised by a printer driver.
    /// </summary>
    public PrinterMediaProvenance MediaProvenance
    {
        get => _mediaProvenance;
        set => SetProperty(ref _mediaProvenance, value);
    }

    public string DriverMediaIdentity
    {
        get => _driverMediaIdentity;
        set => SetProperty(ref _driverMediaIdentity, value ?? string.Empty);
    }

    public string DriverIdentity
    {
        get => _driverIdentity;
        set => SetProperty(ref _driverIdentity, value ?? string.Empty);
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
