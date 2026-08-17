using ANLAbel.Core.Barcode;
using ANLAbel.Core.Enums;
using Xunit;

namespace ANLAbel.UnitTests;

public sealed class BarcodeHriLayoutTests
{
    [Fact]
    public void HiddenHriLeavesTheWholeFrameForTheSymbol()
    {
        var result = BarcodeHriLayoutContract.Create(
            supportsHri: true,
            showHri: false,
            frameWidthMm: 40,
            frameHeightMm: 10,
            hriTextWidthMm: 20,
            hriTextHeightMm: 3,
            hriFontSizePt: 7);

        Assert.True(result.IsValid);
        Assert.False(result.IsEnabled);
        Assert.Equal(BarcodeHriPlacement.None, result.Placement);
        Assert.Equal(0, result.SymbolTopMm, precision: 6);
        Assert.Equal(10, result.SymbolHeightMm, precision: 6);
    }

    [Fact]
    public void PlacementNoneUsesFullFrameHeight()
    {
        var result = BarcodeHriLayoutContract.Create(
            supportsHri: true,
            BarcodeHriPlacement.None,
            frameWidthMm: 40,
            frameHeightMm: 12,
            hriTextWidthMm: 20,
            hriTextHeightMm: 3,
            hriFontSizePt: 7);

        Assert.True(result.IsValid);
        Assert.False(result.IsEnabled);
        Assert.Equal(0, result.SymbolTopMm, precision: 6);
        Assert.Equal(12, result.SymbolHeightMm, precision: 6);
        Assert.Equal(0, result.HriHeightMm, precision: 6);
    }

    [Fact]
    public void ValidHriReservesGapAndTextStripWithoutStretchingSymbol()
    {
        var result = BarcodeHriLayoutContract.Create(
            supportsHri: true,
            showHri: true,
            frameWidthMm: 50,
            frameHeightMm: 12,
            hriTextWidthMm: 30,
            hriTextHeightMm: 2.8,
            hriFontSizePt: 7);

        Assert.True(result.IsValid);
        Assert.True(result.IsEnabled);
        Assert.Equal(BarcodeHriPlacement.Below, result.Placement);
        Assert.Equal(0, result.SymbolTopMm, precision: 6);
        Assert.Equal(3.2, result.HriHeightMm, precision: 6);
        Assert.Equal(8.3, result.SymbolHeightMm, precision: 6);
        Assert.Equal(8.8, result.HriTopMm, precision: 6);
    }

    [Fact]
    public void PlacementAboveReservesTopStripAndShiftsSymbolDown()
    {
        var result = BarcodeHriLayoutContract.Create(
            supportsHri: true,
            BarcodeHriPlacement.Above,
            frameWidthMm: 50,
            frameHeightMm: 12,
            hriTextWidthMm: 30,
            hriTextHeightMm: 2.8,
            hriFontSizePt: 7);

        Assert.True(result.IsValid);
        Assert.True(result.IsEnabled);
        Assert.Equal(BarcodeHriPlacement.Above, result.Placement);
        Assert.Equal(3.2, result.HriHeightMm, precision: 6);
        Assert.Equal(0, result.HriTopMm, precision: 6);
        Assert.Equal(8.3, result.SymbolHeightMm, precision: 6);
        Assert.Equal(3.7, result.SymbolTopMm, precision: 6); // hri 3.2 + gap 0.5
        Assert.Equal(12, result.SymbolTopMm + result.SymbolHeightMm, precision: 6);
    }

    [Fact]
    public void PlacementBelowMatchesLegacyBoolShowPath()
    {
        var viaBool = BarcodeHriLayoutContract.Create(
            supportsHri: true,
            showHri: true,
            frameWidthMm: 40,
            frameHeightMm: 11,
            hriTextWidthMm: 25,
            hriTextHeightMm: 2.5,
            hriFontSizePt: 7);
        var viaEnum = BarcodeHriLayoutContract.Create(
            supportsHri: true,
            BarcodeHriPlacement.Below,
            frameWidthMm: 40,
            frameHeightMm: 11,
            hriTextWidthMm: 25,
            hriTextHeightMm: 2.5,
            hriFontSizePt: 7);

        Assert.Equal(viaBool.SymbolTopMm, viaEnum.SymbolTopMm, precision: 6);
        Assert.Equal(viaBool.SymbolHeightMm, viaEnum.SymbolHeightMm, precision: 6);
        Assert.Equal(viaBool.HriTopMm, viaEnum.HriTopMm, precision: 6);
        Assert.Equal(viaBool.HriHeightMm, viaEnum.HriHeightMm, precision: 6);
        Assert.Equal(BarcodeHriPlacement.Below, viaEnum.Placement);
    }

    [Fact]
    public void HriWidthOverflowFailsClosed()
    {
        var result = BarcodeHriLayoutContract.Create(
            supportsHri: true,
            showHri: true,
            frameWidthMm: 20,
            frameHeightMm: 10,
            hriTextWidthMm: 20.1,
            hriTextHeightMm: 2,
            hriFontSizePt: 7);

        Assert.False(result.IsValid);
        Assert.Contains("frame", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ShortFrameFailsClosedInsteadOfCompressingBars()
    {
        var result = BarcodeHriLayoutContract.Create(
            supportsHri: true,
            showHri: true,
            frameWidthMm: 20,
            frameHeightMm: 3.5,
            hriTextWidthMm: 10,
            hriTextHeightMm: 2.5,
            hriFontSizePt: 7);

        Assert.False(result.IsValid);
        Assert.Contains("too short", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void UnsupportedHriPathLeavesMatrixFrameUntouched()
    {
        var result = BarcodeHriLayoutContract.Create(
            supportsHri: false,
            showHri: true,
            frameWidthMm: 20,
            frameHeightMm: 20,
            hriTextWidthMm: 10,
            hriTextHeightMm: 2,
            hriFontSizePt: 7);

        Assert.True(result.IsValid);
        Assert.False(result.IsEnabled);
        Assert.Equal(20, result.SymbolHeightMm, precision: 6);
    }

    [Fact]
    public void LegacyShowBarcodeTextMapsToPlacement()
    {
        var item = new ANLAbel.Core.Models.LabelObject
        {
            Type = ANLAbel.Core.Enums.ObjectType.BarcodeCode128,
            ShowBarcodeText = true
        };
        Assert.Equal(BarcodeHriPlacement.Below, item.BarcodeHriPlacement);

        item.ShowBarcodeText = false;
        Assert.Equal(BarcodeHriPlacement.None, item.BarcodeHriPlacement);
        Assert.False(item.ShowBarcodeText);

        item.BarcodeHriPlacement = BarcodeHriPlacement.Above;
        Assert.True(item.ShowBarcodeText);

        item.ShowBarcodeText = true; // already shown Above — must keep Above
        Assert.Equal(BarcodeHriPlacement.Above, item.BarcodeHriPlacement);
    }

    [Fact]
    public void PublicGapAndPaddingConstantsAreTheAuthoredValues()
    {
        Assert.Equal(0.5, BarcodeHriLayoutContract.GapMm);
        Assert.Equal(0.2, BarcodeHriLayoutContract.VerticalPaddingMm);
    }

    [Fact]
    public void ValidBelowAndAbove_OccupyTheAuthoredFrameWithoutOverlap()
    {
        const double frameHeightMm = 12;
        var below = BarcodeHriLayoutContract.Create(
            supportsHri: true,
            BarcodeHriPlacement.Below,
            frameWidthMm: 50,
            frameHeightMm,
            hriTextWidthMm: 30,
            hriTextHeightMm: 2.8,
            hriFontSizePt: 7);
        var above = BarcodeHriLayoutContract.Create(
            supportsHri: true,
            BarcodeHriPlacement.Above,
            frameWidthMm: 50,
            frameHeightMm,
            hriTextWidthMm: 30,
            hriTextHeightMm: 2.8,
            hriFontSizePt: 7);

        Assert.True(below.IsValid);
        Assert.True(above.IsValid);
        Assert.Equal(BarcodeHriLayoutContract.GapMm, below.GapMm, precision: 9);
        Assert.Equal(BarcodeHriLayoutContract.GapMm, above.GapMm, precision: 9);
        Assert.Equal(below.HriHeightMm, above.HriHeightMm, precision: 9);
        Assert.Equal(below.SymbolHeightMm, above.SymbolHeightMm, precision: 9);
        Assert.Equal(
            frameHeightMm,
            below.SymbolHeightMm + below.GapMm + below.HriHeightMm,
            precision: 9);
        Assert.Equal(
            frameHeightMm,
            above.HriHeightMm + above.GapMm + above.SymbolHeightMm,
            precision: 9);
        Assert.Equal(0, below.SymbolTopMm, precision: 9);
        Assert.Equal(0, above.HriTopMm, precision: 9);
        Assert.True(below.HriTopMm > below.SymbolTopMm + below.SymbolHeightMm);
        Assert.True(above.SymbolTopMm > above.HriTopMm + above.HriHeightMm);
    }

    [Fact]
    public void Disabled_ClampsNonPositiveFrameHeightToZero()
    {
        var disabled = BarcodeHriLayout.Disabled(-4);
        Assert.True(disabled.IsValid);
        Assert.False(disabled.IsEnabled);
        Assert.Equal(0, disabled.SymbolHeightMm);
        Assert.Equal(0, disabled.HriTopMm);
        Assert.Equal(0, disabled.GapMm);
        Assert.Null(disabled.ErrorMessage);

        var viaCreate = BarcodeHriLayoutContract.Create(
            supportsHri: false,
            BarcodeHriPlacement.Below,
            frameWidthMm: 10,
            frameHeightMm: -4,
            hriTextWidthMm: 4,
            hriTextHeightMm: 2,
            hriFontSizePt: 7);
        Assert.Equal(0, viaCreate.SymbolHeightMm);
        Assert.False(viaCreate.IsEnabled);
    }

    [Fact]
    public void UnsupportedPlacement_FailsClosed()
    {
        var result = BarcodeHriLayoutContract.Create(
            supportsHri: true,
            (BarcodeHriPlacement)99,
            frameWidthMm: 40,
            frameHeightMm: 12,
            hriTextWidthMm: 20,
            hriTextHeightMm: 2,
            hriFontSizePt: 7);

        Assert.False(result.IsValid);
        Assert.True(result.IsEnabled);
        Assert.Equal(BarcodeHriPlacement.None, result.Placement);
        Assert.Equal(0, result.SymbolHeightMm);
        Assert.Equal(BarcodeHriLayoutContract.GapMm, result.GapMm);
        Assert.Contains("Unsupported HRI placement", result.ErrorMessage, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(-1, 10)]
    [InlineData(double.NaN, 10)]
    [InlineData(double.PositiveInfinity, 10)]
    [InlineData(10, 0)]
    [InlineData(10, -1)]
    [InlineData(10, double.NaN)]
    [InlineData(10, double.NegativeInfinity)]
    public void NonPositiveOrNonFiniteFrame_FailsClosed(double widthMm, double heightMm)
    {
        var result = BarcodeHriLayoutContract.Create(
            supportsHri: true,
            BarcodeHriPlacement.Below,
            widthMm,
            heightMm,
            hriTextWidthMm: 5,
            hriTextHeightMm: 2,
            hriFontSizePt: 7);

        Assert.False(result.IsValid);
        Assert.Contains("positive finite", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(0, 2)]
    [InlineData(-1, 2)]
    [InlineData(double.NaN, 2)]
    [InlineData(5, 0)]
    [InlineData(5, -0.1)]
    [InlineData(5, double.PositiveInfinity)]
    public void NonPositiveOrNonFiniteHriInk_FailsClosed(double textWidthMm, double textHeightMm)
    {
        var result = BarcodeHriLayoutContract.Create(
            supportsHri: true,
            BarcodeHriPlacement.Above,
            frameWidthMm: 40,
            frameHeightMm: 12,
            textWidthMm,
            textHeightMm,
            hriFontSizePt: 7);

        Assert.False(result.IsValid);
        Assert.Contains("no measurable", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void HriFontSize_UsesApplicationContractBounds()
    {
        var min = BarcodeApplicationContract.MinimumHriFontSizePt;
        var max = BarcodeApplicationContract.MaximumHriFontSizePt;

        Assert.True(CreateAtFont(min).IsValid);
        Assert.True(CreateAtFont(max).IsValid);
        Assert.False(CreateAtFont(min - 1e-6).IsValid);
        Assert.False(CreateAtFont(max + 1e-6).IsValid);
        Assert.False(CreateAtFont(double.NaN).IsValid);
        Assert.False(CreateAtFont(double.PositiveInfinity).IsValid);
        Assert.Contains("5", CreateAtFont(4).ErrorMessage, StringComparison.Ordinal);
        Assert.Contains("20", CreateAtFont(21).ErrorMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void HriWidth_AllowsExactFrameAndOneThousandthSlack()
    {
        const double frameWidthMm = 20;
        var exact = BarcodeHriLayoutContract.Create(
            supportsHri: true,
            BarcodeHriPlacement.Below,
            frameWidthMm,
            frameHeightMm: 12,
            hriTextWidthMm: frameWidthMm,
            hriTextHeightMm: 2,
            hriFontSizePt: 7);
        var slack = BarcodeHriLayoutContract.Create(
            supportsHri: true,
            BarcodeHriPlacement.Below,
            frameWidthMm,
            frameHeightMm: 12,
            hriTextWidthMm: frameWidthMm + 0.001,
            hriTextHeightMm: 2,
            hriFontSizePt: 7);
        var overflow = BarcodeHriLayoutContract.Create(
            supportsHri: true,
            BarcodeHriPlacement.Below,
            frameWidthMm,
            frameHeightMm: 12,
            hriTextWidthMm: frameWidthMm + 0.001 + 1e-6,
            hriTextHeightMm: 2,
            hriFontSizePt: 7);

        Assert.True(exact.IsValid);
        Assert.True(slack.IsValid);
        Assert.False(overflow.IsValid);
        Assert.Contains("20", overflow.ErrorMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void SymbolHeightAtHalfMillimetre_FailsClosed()
    {
        // 2 mm ink + 0.4 mm pad + 0.5 mm gap + 0.5 mm bars = 3.4 mm frame.
        var atLimit = BarcodeHriLayoutContract.Create(
            supportsHri: true,
            BarcodeHriPlacement.Below,
            frameWidthMm: 30,
            frameHeightMm: 3.4,
            hriTextWidthMm: 10,
            hriTextHeightMm: 2,
            hriFontSizePt: 7);
        var justAbove = BarcodeHriLayoutContract.Create(
            supportsHri: true,
            BarcodeHriPlacement.Below,
            frameWidthMm: 30,
            frameHeightMm: 3.41,
            hriTextWidthMm: 10,
            hriTextHeightMm: 2,
            hriFontSizePt: 7);

        Assert.False(atLimit.IsValid);
        Assert.Contains("too short", atLimit.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        Assert.True(justAbove.IsValid);
        Assert.True(justAbove.SymbolHeightMm > 0.5);
    }

    [Fact]
    public void BoolShowHriFalse_MatchesPlacementNone()
    {
        var viaBool = BarcodeHriLayoutContract.Create(
            supportsHri: true,
            showHri: false,
            frameWidthMm: 40,
            frameHeightMm: 11,
            hriTextWidthMm: 25,
            hriTextHeightMm: 2.5,
            hriFontSizePt: 7);
        var viaEnum = BarcodeHriLayoutContract.Create(
            supportsHri: true,
            BarcodeHriPlacement.None,
            frameWidthMm: 40,
            frameHeightMm: 11,
            hriTextWidthMm: 25,
            hriTextHeightMm: 2.5,
            hriFontSizePt: 7);

        Assert.Equal(viaEnum.IsEnabled, viaBool.IsEnabled);
        Assert.Equal(viaEnum.SymbolHeightMm, viaBool.SymbolHeightMm, precision: 9);
        Assert.Equal(viaEnum.HriTopMm, viaBool.HriTopMm, precision: 9);
        Assert.Equal(11, viaBool.SymbolHeightMm, precision: 9);
    }

    private static BarcodeHriLayout CreateAtFont(double fontPt)
        => BarcodeHriLayoutContract.Create(
            supportsHri: true,
            BarcodeHriPlacement.Below,
            frameWidthMm: 40,
            frameHeightMm: 12,
            hriTextWidthMm: 20,
            hriTextHeightMm: 2,
            hriFontSizePt: fontPt);
}
