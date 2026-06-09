using ANLAbel.Core.Enums;
using ANLAbel.Core.Mvvm;

namespace ANLAbel.Core.Models;

public sealed class ObjectStyle : ObservableObject
{
    private string _fontFamily = "Arial";
    private double _fontSizePt = 10;
    private bool _bold;
    private bool _italic;
    private bool _underline;
    private TextAlignmentMode _alignment = TextAlignmentMode.Left;
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
