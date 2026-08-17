using ANLAbel.Core.Enums;
using ANLAbel.Core.Mvvm;

namespace ANLAbel.Core.Models;

public sealed class ObjectStyle : ObservableObject
{
    private string _fontFamily = "Arial";
    private double _fontSizePt = 10;
    private double _lineHeightPt;
    private bool _bold;
    private bool _italic;
    private bool _underline;
    private TextAlignmentMode _alignment = TextAlignmentMode.Left;
    private TextDirectionMode _textDirection = TextDirectionMode.Auto;
    private TextSizingMode _textSizing = TextSizingMode.AutoFit;
    private TextOverflowMode _textOverflow = TextOverflowMode.Error;
    private double _textFitMinimumFontSizePt = 4;
    private double _textFitMaximumFontSizePt;
    private double _textFitMinimumScale = 0.5;
    private double _textFitMaximumScale = 1.0;
    private TextVerticalAlignmentMode? _verticalAlignment;
    private double _textPaddingMm;
    private double _textPaddingLeftMm;
    private double _textPaddingRightMm;
    private double _textPaddingTopMm;
    private double _textPaddingBottomMm;
    private double _borderThicknessMm = 0.2;
    private OutlineStyle _outlineStyle = OutlineStyle.Solid;
    private FillStyle _fillStyle = FillStyle.Solid;
    private double _cornerRadiusMm;
    private string _fillColor = "#FFFFFFFF";
    private string _strokeColor = "#FF1F2937";

    public string FontFamily
    {
        get => _fontFamily;
        set => SetProperty(ref _fontFamily, value);
    }

    public double FontSizePt
    {
        get => _fontSizePt;
        set => SetProperty(ref _fontSizePt, value);
    }

    /// <summary>
    /// Requested paragraph line height in points. Zero means Auto and preserves
    /// the historical WPF font metrics. Positive values are treated as a
    /// minimum line box height so glyph ink is never clipped by a too-small
    /// setting.
    /// </summary>
    public double LineHeightPt
    {
        get => _lineHeightPt;
        set => SetProperty(ref _lineHeightPt, value <= 0 ? 0 : Math.Max(1, value));
    }

    public bool Bold
    {
        get => _bold;
        set => SetProperty(ref _bold, value);
    }

    public bool Italic
    {
        get => _italic;
        set => SetProperty(ref _italic, value);
    }

    public bool Underline
    {
        get => _underline;
        set => SetProperty(ref _underline, value);
    }

    public TextAlignmentMode Alignment
    {
        get => _alignment;
        set => SetProperty(ref _alignment, value);
    }

    /// <summary>
    /// Explicit paragraph direction for mixed-language labels. Auto resolves
    /// from the first strong Unicode letter and keeps legacy LTR behavior when
    /// the value contains only numbers or punctuation.
    /// </summary>
    public TextDirectionMode TextDirection
    {
        get => _textDirection;
        set => SetProperty(ref _textDirection, value);
    }

    /// <summary>
    /// Controls whether static Text owns measured content bounds (AutoFit) or
    /// stays inside a user-authored TextBox frame. FixedFrame, ShrinkFont and
    /// ScaleWidth all preserve Width/Height; only glyph layout is remediated.
    /// </summary>
    public TextSizingMode TextSizing
    {
        get => _textSizing;
        set => SetProperty(ref _textSizing, Enum.IsDefined(value) ? value : TextSizingMode.AutoFit);
    }

    /// <summary>
    /// Explicit policy for content that exceeds a bounded text frame. Error is
    /// the fail-closed production default; other values require an intentional
    /// authoring choice and are retained through save/load, clone and scene
    /// identity.
    /// </summary>
    public TextOverflowMode TextOverflow
    {
        get => _textOverflow;
        set => SetProperty(ref _textOverflow, Enum.IsDefined(value) ? value : TextOverflowMode.Error);
    }

    /// <summary>
    /// Lower point-size bound used by TextBox font-size fitting. NiceLabel
    /// exposes this as "Minimum size". Values are normalized independently;
    /// the layout resolver orders the effective min/max pair.
    /// </summary>
    public double TextFitMinimumFontSizePt
    {
        get => _textFitMinimumFontSizePt;
        set => SetProperty(ref _textFitMinimumFontSizePt, NormalizeFitFontSize(value, 4));
    }

    /// <summary>
    /// Upper point-size bound used by TextBox font-size fitting. Zero is the
    /// compatibility sentinel for the authored FontSizePt in older templates.
    /// </summary>
    public double TextFitMaximumFontSizePt
    {
        get => _textFitMaximumFontSizePt;
        set => SetProperty(ref _textFitMaximumFontSizePt, value <= 0 ? 0 : NormalizeFitFontSize(value, 10));
    }

    /// <summary>Minimum horizontal font scale (0.1 = 10%, 1 = 100%).</summary>
    public double TextFitMinimumScale
    {
        get => _textFitMinimumScale;
        set => SetProperty(ref _textFitMinimumScale, NormalizeFitScale(value, 0.5));
    }

    /// <summary>Maximum horizontal font scale (1 = 100%, 2 = 200%).</summary>
    public double TextFitMaximumScale
    {
        get => _textFitMaximumScale;
        set => SetProperty(ref _textFitMaximumScale, NormalizeFitScale(value, 1.0));
    }

    /// <summary>
    /// Explicit vertical text alignment. Null preserves the legacy semantic: static
    /// text is centered while a bounded Text Box starts at the top.
    /// </summary>
    public TextVerticalAlignmentMode? VerticalAlignment
    {
        get => _verticalAlignment;
        set => SetProperty(ref _verticalAlignment, value);
    }

    /// <summary>
    /// Uniform inner padding for text content, expressed in physical
    /// millimetres. Setting this property is a convenient shorthand that sets
    /// all four edge values. A non-uniform edge edit projects this value to
    /// zero, so old templates and the compact UI remain backward-compatible.
    /// </summary>
    public double TextPaddingMm
    {
        get => _textPaddingMm;
        set
        {
            var normalized = NormalizeTextPadding(value);
            SetProperty(ref _textPaddingMm, normalized);
            SetPaddingEdge(ref _textPaddingLeftMm, normalized, nameof(TextPaddingLeftMm), projectUniform: false);
            SetPaddingEdge(ref _textPaddingRightMm, normalized, nameof(TextPaddingRightMm), projectUniform: false);
            SetPaddingEdge(ref _textPaddingTopMm, normalized, nameof(TextPaddingTopMm), projectUniform: false);
            SetPaddingEdge(ref _textPaddingBottomMm, normalized, nameof(TextPaddingBottomMm), projectUniform: false);
        }
    }

    public double TextPaddingLeftMm
    {
        get => _textPaddingLeftMm;
        set => SetPaddingEdge(ref _textPaddingLeftMm, value, nameof(TextPaddingLeftMm));
    }

    public double TextPaddingRightMm
    {
        get => _textPaddingRightMm;
        set => SetPaddingEdge(ref _textPaddingRightMm, value, nameof(TextPaddingRightMm));
    }

    public double TextPaddingTopMm
    {
        get => _textPaddingTopMm;
        set => SetPaddingEdge(ref _textPaddingTopMm, value, nameof(TextPaddingTopMm));
    }

    public double TextPaddingBottomMm
    {
        get => _textPaddingBottomMm;
        set => SetPaddingEdge(ref _textPaddingBottomMm, value, nameof(TextPaddingBottomMm));
    }

    private bool SetPaddingEdge(ref double field, double value, string propertyName, bool projectUniform = true)
    {
        var changed = SetProperty(ref field, NormalizeTextPadding(value), propertyName);
        if (changed && projectUniform)
        {
            ProjectUniformPadding();
        }

        return changed;
    }

    private void ProjectUniformPadding()
    {
        var projected = _textPaddingLeftMm == _textPaddingRightMm
            && _textPaddingLeftMm == _textPaddingTopMm
            && _textPaddingLeftMm == _textPaddingBottomMm
            ? _textPaddingLeftMm
            : 0;
        if (Math.Abs(_textPaddingMm - projected) > 0.000001)
        {
            _textPaddingMm = projected;
            OnPropertyChanged(nameof(TextPaddingMm));
        }
    }

    private static double NormalizeTextPadding(double value)
        => Math.Clamp(double.IsFinite(value) ? value : 0, 0, 20);

    private static double NormalizeFitFontSize(double value, double fallback)
        => Math.Clamp(double.IsFinite(value) ? value : fallback, 1, 200);

    private static double NormalizeFitScale(double value, double fallback)
        => Math.Clamp(double.IsFinite(value) ? value : fallback, 0.1, 4.0);

    public double BorderThicknessMm
    {
        get => _borderThicknessMm;
        set => SetProperty(ref _borderThicknessMm, Math.Max(0, value));
    }

    public OutlineStyle OutlineStyle
    {
        get => _outlineStyle;
        set => SetProperty(ref _outlineStyle, value);
    }

    public FillStyle FillStyle
    {
        get => _fillStyle;
        set => SetProperty(ref _fillStyle, value);
    }

    public double CornerRadiusMm
    {
        get => _cornerRadiusMm;
        set => SetProperty(ref _cornerRadiusMm, Math.Max(0, value));
    }

    public string FillColor
    {
        get => _fillColor;
        set => SetProperty(ref _fillColor, value);
    }

    public string StrokeColor
    {
        get => _strokeColor;
        set => SetProperty(ref _strokeColor, value);
    }
}
