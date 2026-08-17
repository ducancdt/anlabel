using ANLAbel.Core.Enums;
using ANLAbel.Core.Barcode;
using ANLAbel.Core.Mvvm;
using ANLAbel.Core.Printing;
using System.Text.Json.Serialization;

namespace ANLAbel.Core.Models;

public sealed class LabelObject : ObservableObject
{
    private string _id = Guid.NewGuid().ToString("N");
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
    private BarcodeApplicationProfile _barcodeApplicationProfile = BarcodeApplicationProfile.General;
    private QrSizingMode _qrSizingMode = QrSizingMode.AutoSizeByData;
    private QrErrorCorrection _qrErrorCorrection = QrErrorCorrection.M;
    private int _qrFixedVersion = 1;
    private int _qrModuleSizePx = 6;
    private int _qrQuietZoneModules = 2;
    private int _qrDpi = 300;
    private ObjectStyle _style = new();
    private BarcodeHriPlacement _barcodeHriPlacement = BarcodeHriPlacement.Below;
    private double _barcodeTextFontSizePt = 7;
    private BarcodeCheckDigitPolicy _barcodeCheckDigitPolicy = BarcodeCheckDigitPolicy.None;
    private bool _barcodeHriShowCheckDigit = true;
    /// <summary>0 = legacy: derive module width from frame / module count.</summary>
    private double _barcodeModuleWidthMm;
    private BarcodeWidthMode _barcodeWidthMode = BarcodeWidthMode.FrameOwned;
    private bool _applyingQrAutoSize;
    private bool _hasBindingIssue;
    private string _bindingStateDisplayText = string.Empty;
    private string _imageDataBase64 = string.Empty;
    private ImageRasterMode _imageRasterMode = ImageRasterMode.DriverManaged;
    private int _imagePixelWidth;
    private int _imagePixelHeight;

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

    /// <summary>
    /// Optional production application contract for this barcode.  General keeps
    /// the historical authoring behavior; Industrial and GS1 opt into fail-closed
    /// quiet-zone/HRI/data checks during print preflight.
    /// </summary>
    public BarcodeApplicationProfile BarcodeApplicationProfile
    {
        get => _barcodeApplicationProfile;
        set => SetProperty(ref _barcodeApplicationProfile, value);
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
    /// Base64-encoded bytes of an inserted picture (PNG/JPEG/BMP). Embedded directly so
    /// templates stay standalone files, consistent with the rest of the template format.
    /// </summary>
    public string ImageDataBase64
    {
        get => _imageDataBase64;
        set => SetProperty(ref _imageDataBase64, value);
    }

    /// <summary>
    /// Explicit colour/monochrome policy for this embedded image. The default
    /// preserves the historical driver-managed behaviour; selecting an
    /// application mode makes the same deterministic transform run in the
    /// designer, preview and print presenter.
    /// </summary>
    public ImageRasterMode ImageRasterMode
    {
        get => _imageRasterMode;
        set => SetProperty(ref _imageRasterMode, ImageRasterContract.IsSupported(value)
            ? value
            : ImageRasterMode.DriverManaged);
    }

    /// <summary>
    /// Decoder-observed source dimensions. They are persisted with the image
    /// resource so a snapshot can fingerprint the exact raster, while the
    /// print preflight still re-decodes and rejects a stale/mismatched value.
    /// Zero means the template predates this metadata or has not been decoded.
    /// </summary>
    public int ImagePixelWidth
    {
        get => _imagePixelWidth;
        set => SetProperty(ref _imagePixelWidth, Math.Max(0, value));
    }

    public int ImagePixelHeight
    {
        get => _imagePixelHeight;
        set => SetProperty(ref _imagePixelHeight, Math.Max(0, value));
    }

    [JsonIgnore]
    public bool HasImageData => !string.IsNullOrWhiteSpace(ImageDataBase64);

    /// <summary>
    /// Vertical placement of human-readable text relative to the linear symbol.
    /// Default is <see cref="BarcodeHriPlacement.Below"/> (industry common).
    /// </summary>
    public BarcodeHriPlacement BarcodeHriPlacement
    {
        get => _barcodeHriPlacement;
        set
        {
            var next = Enum.IsDefined(typeof(BarcodeHriPlacement), value)
                ? value
                : BarcodeHriPlacement.Below;
            if (SetProperty(ref _barcodeHriPlacement, next))
            {
                OnPropertyChanged(nameof(ShowBarcodeText));
            }
        }
    }

    /// <summary>
    /// Legacy visibility flag mapped onto <see cref="BarcodeHriPlacement"/>.
    /// True means HRI is shown (Below when enabling from None; preserves Above).
    /// False maps to <see cref="BarcodeHriPlacement.None"/>. Kept for save/load
    /// of templates that only store the bool.
    /// </summary>
    public bool ShowBarcodeText
    {
        get => _barcodeHriPlacement != BarcodeHriPlacement.None;
        set
        {
            if (value)
            {
                if (_barcodeHriPlacement == BarcodeHriPlacement.None)
                {
                    BarcodeHriPlacement = BarcodeHriPlacement.Below;
                }
            }
            else if (_barcodeHriPlacement != BarcodeHriPlacement.None)
            {
                BarcodeHriPlacement = BarcodeHriPlacement.None;
            }
        }
    }

    /// <summary>
    /// Font size (in points) for the text displayed with the barcode (HRI).
    /// </summary>
    public double BarcodeTextFontSizePt
    {
        get => _barcodeTextFontSizePt;
        set => SetProperty(ref _barcodeTextFontSizePt, Math.Max(4, Math.Min(20, value)));
    }

    /// <summary>
    /// Optional check-digit policy for Code 39 / ITF. Default <see cref="BarcodeCheckDigitPolicy.None"/>
    /// keeps legacy templates open. <see cref="BarcodeCheckDigitPolicy.Verify"/> fails closed in preflight.
    /// </summary>
    public BarcodeCheckDigitPolicy BarcodeCheckDigitPolicy
    {
        get => _barcodeCheckDigitPolicy;
        set
        {
            var next = Enum.IsDefined(typeof(BarcodeCheckDigitPolicy), value)
                ? value
                : BarcodeCheckDigitPolicy.None;
            SetProperty(ref _barcodeCheckDigitPolicy, next);
        }
    }

    /// <summary>
    /// When false, HRI omits a validated trailing check digit. Symbol encode always
    /// uses the full payload — this flag never changes module geometry.
    /// </summary>
    public bool BarcodeHriShowCheckDigit
    {
        get => _barcodeHriShowCheckDigit;
        set => SetProperty(ref _barcodeHriShowCheckDigit, value);
    }

    /// <summary>
    /// Authored 1D X-dimension (module width) in millimetres. Zero means legacy
    /// behaviour: effective module is estimated from the object frame width and
    /// the encoded module count. When set, print/preflight quantize this value
    /// to whole printer dots at the print-plan DPI.
    /// </summary>
    public double BarcodeModuleWidthMm
    {
        get => _barcodeModuleWidthMm;
        set
        {
            var normalized = double.IsFinite(value) ? Math.Clamp(value, 0, 5) : 0;
            // Persist at 0.01 mm so industrial X-dim edits stay stable in JSON.
            normalized = Math.Round(normalized, 2, MidpointRounding.AwayFromZero);
            SetProperty(ref _barcodeModuleWidthMm, normalized);
        }
    }

    private Code39WideNarrowRatio _code39WideNarrowRatio = Code39WideNarrowRatio.LegacyEngineDefault;

    /// <summary>
    /// Authored wide:narrow ratio for Code 39. Default <see cref="Code39WideNarrowRatio.LegacyEngineDefault"/>
    /// preserves historical ZXing behavior.
    /// </summary>
    public Code39WideNarrowRatio Code39WideNarrowRatio
    {
        get => _code39WideNarrowRatio;
        set
        {
            var next = Enum.IsDefined(typeof(Code39WideNarrowRatio), value)
                ? value
                : Code39WideNarrowRatio.LegacyEngineDefault;
            SetProperty(ref _code39WideNarrowRatio, next);
        }
    }

    /// <summary>
    /// Linear barcode horizontal sizing policy. Default <see cref="BarcodeWidthMode.FrameOwned"/>
    /// preserves legacy templates. <see cref="BarcodeWidthMode.SizedFromX"/> sets production
    /// width from quantized X × pure logical module count when X &gt; 0.
    /// </summary>
    public BarcodeWidthMode BarcodeWidthMode
    {
        get => _barcodeWidthMode;
        set => SetProperty(ref _barcodeWidthMode, value);
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

        var targetSizeMm = QrObjectGeometryContract.ResolveTargetSizeMm(
            this,
            string.IsNullOrWhiteSpace(BindingExpression) ? Text : null);
        if (targetSizeMm is null)
        {
            return;
        }

        if (!QrObjectGeometryContract.HasMeaningfulSizeDelta(this, targetSizeMm.Value))
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

    /// <summary>
    /// True for matrix-style barcodes (QR/DataMatrix, or Code128-typed objects whose
    /// symbology was switched to a 2D kind) — the shared predicate for "does this object
    /// behave like a square/matrix code" used across the designer, renderers, and preflight
    /// validation. Public so callers outside this assembly don't need their own copy.
    /// </summary>
    public bool IsSquare2DCodeLike()
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
