using ANLAbel.Barcode.Options;
using ANLAbel.Core.Enums;
using ANLAbel.Core.Models;
using Xunit;

namespace ANLAbel.UnitTests;

public sealed class BearerBarsContractTests
{
    [Fact]
    public void DefaultBearerBarSettingsAreNoneAndOneMillimetre()
    {
        var obj = new LabelObject();
        Assert.Equal(BearerBarStyle.None, obj.BearerBarStyle);
        Assert.Equal(1.0, obj.BearerBarThicknessMm);
    }

    [Fact]
    public void LabelObjectClonerPreservesBearerBarSettings()
    {
        var source = new LabelObject
        {
            Name = "ITF14",
            Type = ObjectType.BarcodeCode128,
            BarcodeSymbology = BarcodeSymbology.ITF,
            Text = "12345678901231",
            BearerBarStyle = BearerBarStyle.Frame,
            BearerBarThicknessMm = 1.25
        };

        var clone = LabelObjectCloner.Clone(source);
        Assert.Equal(BearerBarStyle.Frame, clone.BearerBarStyle);
        Assert.Equal(1.25, clone.BearerBarThicknessMm);
    }

    [Theory]
    [InlineData(BearerBarStyle.None)]
    [InlineData(BearerBarStyle.TopBottom)]
    [InlineData(BearerBarStyle.Frame)]
    public void BarcodeRenderOptionsCarriesBearerBars(BearerBarStyle style)
    {
        var options = new BarcodeRenderOptions
        {
            BearerBarStyle = style,
            BearerBarThicknessMm = 1.5
        };
        Assert.Equal(style, options.BearerBarStyle);
        Assert.Equal(1.5, options.BearerBarThicknessMm);
    }
}
