using ANLAbel.Core.Enums;
using ANLAbel.Core.Models;
using Xunit;

namespace ANLAbel.UnitTests;

/// <summary>NiceLabel 10 Format Painter (LabelObjectCloner.CopyStyle).</summary>
public sealed class FormatPainterTests
{
    [Fact]
    public void CopiesTheWholeStyleBetweenObjectsOfOneType()
    {
        var source = new LabelObject { Type = ObjectType.Text, Style = { FontFamily = "Arial", FontSizePt = 18, Bold = true, StrokeColor = "#FFFF0000" } };
        var target = new LabelObject { Type = ObjectType.Text };
        var targetStyle = target.Style;

        LabelObjectCloner.CopyStyle(source, target);

        Assert.Same(targetStyle, target.Style);
        Assert.Equal(("Arial", 18.0, true, "#FFFF0000"), (target.Style.FontFamily, target.Style.FontSizePt, target.Style.Bold, target.Style.StrokeColor));
    }

    [Fact]
    public void KeepsTheTargetsFitSettingsAcrossTypes()
    {
        var source = new LabelObject { Type = ObjectType.Text, Style = { FontSizePt = 14, TextSizing = TextSizingMode.AutoFit } };
        var target = new LabelObject { Type = ObjectType.TextBox, Style = { TextSizing = TextSizingMode.ShrinkFont } };

        LabelObjectCloner.CopyStyle(source, target);

        Assert.Equal(14, target.Style.FontSizePt);
        Assert.Equal(TextSizingMode.ShrinkFont, target.Style.TextSizing);
    }
}
