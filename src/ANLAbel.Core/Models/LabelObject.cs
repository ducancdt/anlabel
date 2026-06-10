using ANLAbel.Core.Enums;
using ANLAbel.Core.Barcode;
using ANLAbel.Core.Mvvm;
using System.Text.Json.Serialization;

namespace ANLAbel.Core.Models;

public sealed class LabelObject : ObservableObject
{
    private string _id = Guid.NewGuid().ToString("N");
    private string _linkedToId = string.Empty;
    private ObjectType _type;
    private string _name = "Object";
    private double _xMm;
    private double _yMm;
    private double _widthMm = 20;
    private double _heightMm = 8;
    private double _lineEndXMm;
    private double _lineEndYMm;
    private int _rotation;
    private int _zIndex;
    private bool _isLocked;
    private bool _isVisible = true;
    private string _bindingExpression = string.Empty;
    private string _text = string.Empty;
    private BarcodeSymbology _barcodeSymbology = BarcodeSymbology.Code128;
    private QrSizingMode _qrSizingMode = QrSizingMode.AutoSizeByData;
    private QrErrorCorrection _qrErrorCorrection = QrErrorCorrection.M;
    private int _qrFixedVersion = 1;
    private int _qrModuleSizePx = 6;
    private int _qrQuietZoneModules = 4;
    private int _qrDpi = 300;
    private ObjectStyle _style = new();
    private bool _showBarcodeText = true;
    private double _barcodeTextFontSizePt = 7;
    private bool _applyingQrAutoSize;
    private bool _hasBindingIssue;
    private string _bindingStateDisplayText = string.Empty;

    /// <summary>
    /// When non-empty, this object is linked to the barcode/QR object with the given Id.
    /// Linked text objects move with the barcode and mirror its data content.
    /// </summary>
    public string LinkedToId
    {
        get => _linkedToId;
        set => SetProperty(ref _linkedToId, value ?? string.Empty);
    }

    public string Id
    {
        get => _id;
        set => SetProperty(ref _id, value);
    }

    public ObjectType Type
    {
        get => _type;
        set
        {
            if (SetProperty(ref _type, value))
            {
                ApplyQrAutoSizeFromOwnData();
            }
        }
    }

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public double XMm
    {
        get => _xMm;
        set => SetProperty(ref _xMm, Math.Round(value, 2));
    }

    public double YMm
    {
        get => _yMm;
        set => SetProperty(ref _yMm, Math.Round(value, 2));
    }

    public double WidthMm
    {
        get => _widthMm;
        set => SetProperty(ref _widthMm, Math.Max(0.5, Math.Round(value, 2)));
    }

    public double HeightMm
    {
        get => _heightMm;
        set => SetProperty(ref _heightMm, Math.Max(0.5, Math.Round(value, 2)));
    }

    public double LineEndXMm
    {
        get => _lineEndXMm;
        set => SetProperty(ref _lineEndXMm, Math.Round(value, 2));
    }

    public double LineEndYMm
    {
        get => _lineEndYMm;
        set => SetProperty(ref _lineEndYMm, Math.Round(value, 2));
    }

    public int Rotation
    {
        get => _rotation;
        set => SetProperty(ref _rotation, NormalizeRotation(value));
    }

    public int ZIndex
    {
        get => _zIndex;
        set => SetProperty(ref _zIndex, value);
    }

    public bool IsLocked
    {
        get => _isLocked;
        set => SetProperty(ref _isLocked, value);
    }

    public bool IsVisible
    {
        get => _isVisible;
        set => SetProperty(ref _isVisible, value);
    }

    public string BindingExpression
    {
        get => _bindingExpression;
        set
        {
            if (EqualityComparer<string>.Default.Equals(_bindingExpression, value))
            {
                return;
            }

            _bindingExpression = value;
            ApplyQrAutoSizeFromOwnData();
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasBindingExpression));
        }
    }

    [JsonIgnore]
    public bool HasBindingExpression => !string.IsNullOrWhiteSpace(BindingExpression);

    [JsonIgnore]
    public bool HasBindingIssue
    {
        get => _hasBindingIssue;
        set => SetProperty(ref _hasBindingIssue, value);
    }

    [JsonIgnore]
    public string BindingStateDisplayText
    {
        get => _bindingStateDisplayText;
        set => SetProperty(ref _bindingStateDisplayText, value);
    }

    public string Text
    {
        get => _text;
        set
        {
            if (EqualityComparer<string>.Default.Equals(_text, value))
            {
                return;
            }

            _text = value;
            ApplyQrAutoSizeFromOwnData();
            OnPropertyChanged();
        }
    }

    public BarcodeSymbology BarcodeSymbology
    {
        get => _barcodeSymbology;
        set
        {
            if (SetProperty(ref _barcodeSymbology, value))
            {
                ApplyQrAutoSizeFromOwnData();
            }
        }
    }

    public QrSizingMode QrSizingMode
    {
        get => _qrSizingMode;
        set
        {
            if (EqualityComparer<QrSizingMode>.Default.Equals(_qrSizingMode, value))
            {
                return;
            }

            _qrSizingMode = value;
            ApplyQrAutoSizeFromOwnData();
            OnPropertyChanged();
        }
    }

    public QrErrorCorrection QrErrorCorrection
    {
        get => _qrErrorCorrection;
        set
        {
            if (EqualityComparer<QrErrorCorrection>.Default.Equals(_qrErrorCorrection, value))
            {
                return;
            }

            _qrErrorCorrection = value;
            ApplyQrAutoSizeFromOwnData();
            OnPropertyChanged();
        }
    }

    public int QrFixedVersion
    {
        get => _qrFixedVersion;
        set
        {
            if (SetProperty(ref _qrFixedVersion, Math.Clamp(value, 1, 40)))
            {
                ApplyQrAutoSizeFromOwnData();
            }
        }
    }

    public int QrModuleSizePx
    {
        get => _qrModuleSizePx;
        set
        {
            var normalized = Math.Max(1, value);
            if (_qrModuleSizePx == normalized)
            {
                return;
            }

            _qrModuleSizePx = normalized;
            ApplyQrAutoSizeFromOwnData();
            OnPropertyChanged();
        }
    }

    public int QrQuietZoneModules
    {
        get => _qrQuietZoneModules;
        set
        {
            var normalized = Math.Max(0, value);
            if (_qrQuietZoneModules == normalized)
            {
                return;
            }

            _qrQuietZoneModules = normalized;
            ApplyQrAutoSizeFromOwnData();
            OnPropertyChanged();
        }
    }

    public int QrDpi
    {
        get => _qrDpi;
        set
        {
            var normalized = Math.Max(1, value);
            if (_qrDpi == normalized)
            {
                return;
            }

            _qrDpi = normalized;
            ApplyQrAutoSizeFromOwnData();
            OnPropertyChanged();
        }
    }

    public ObjectStyle Style
    {
        get => _style;
        set => SetProperty(ref _style, value);
    }

    /// <summary>
    /// When true, displays human-readable text content below the barcode.
    /// Default is true (industry standard for 1D barcodes).
    /// </summary>
    public bool ShowBarcodeText
    {
        get => _showBarcodeText;
        set => SetProperty(ref _showBarcodeText, value);
    }

    /// <summary>
    /// Font size (in points) for the text displayed below the barcode.
    /// </summary>
    public double BarcodeTextFontSizePt
    {
        get => _barcodeTextFontSizePt;
        set => SetProperty(ref _barcodeTextFontSizePt, Math.Max(4, Math.Min(20, value)));
    }

    private static int NormalizeRotation(int value)
    {
        var normalized = ((value % 360) + 360) % 360;
        return normalized is 0 or 90 or 180 or 270 ? normalized : 0;
    }

    private void ApplyQrAutoSizeFromOwnData()
    {
        if (_applyingQrAutoSize || !IsSquare2DCodeLike())
        {
            return;
        }

        var targetSizeMm = QrSizingMode == QrSizingMode.FixedVersionAndModuleSize
            ? QrAutoSizeHelper.CalculateFixedSizeMm(QrFixedVersion, QrModuleSizePx, QrQuietZoneModules, QrDpi)
            : string.IsNullOrWhiteSpace(BindingExpression)
                ? QrAutoSizeHelper.CalculateRequiredSizeMm(
                    Text,
                    WidthMm,
                    HeightMm,
                    QrErrorCorrection,
                    QrModuleSizePx,
                    QrQuietZoneModules,
                    QrDpi)
                : null;
        if (targetSizeMm is null)
        {
            return;
        }

        if (Math.Abs(WidthMm - targetSizeMm.Value) <= 0.05 && Math.Abs(HeightMm - targetSizeMm.Value) <= 0.05)
        {
            return;
        }

        _applyingQrAutoSize = true;
        try
        {
            WidthMm = targetSizeMm.Value;
            HeightMm = targetSizeMm.Value;
        }
        finally
        {
            _applyingQrAutoSize = false;
        }
    }

    private bool IsSquare2DCodeLike()
    {
        return Type == ObjectType.QRCode
            || Type == ObjectType.DataMatrix
            || Type == ObjectType.BarcodeCode128
                && BarcodeSymbology is BarcodeSymbology.QRCode
                    or BarcodeSymbology.DataMatrix
                    or BarcodeSymbology.Aztec
                    or BarcodeSymbology.Pdf417;
    }
}
