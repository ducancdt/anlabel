using ANLAbel.Core.Enums;
using ANLAbel.Core.Models;
using ANLAbel.Core.Printing;
using Xunit;

namespace ANLAbel.UnitTests;

public sealed class SheetLayoutContractTests
{
    [Fact]
    public void SingleLabelPerPageIsAlwaysAllowed()
    {
        Assert.False(SheetLayoutContract.IsSheet(1, 1));
        Assert.True(SheetLayoutContract.Evaluate(100, 50, 0, 0, 0, 0, 1, 1, 0, 0).IsAllowed);
    }

    [Fact]
    public void GridMustFitOnTheSheet()
    {
        // 2 x 7 labels of 99.1 x 38.1 mm on A4 with 4.65/15.15 mm margins.
        Assert.True(SheetLayoutContract.Evaluate(99.1, 38.1, 210, 297, 4.65, 15.15, 2, 7, 2.5, 0).IsAllowed);

        var tooMany = SheetLayoutContract.Evaluate(99.1, 38.1, 210, 297, 4.65, 15.15, 2, 8, 2.5, 0);
        Assert.False(tooMany.IsAllowed);
        Assert.Contains("2 × 8", tooMany.Diagnostic, StringComparison.Ordinal);

        var noSheet = SheetLayoutContract.Evaluate(50, 30, 0, 0, 0, 0, 2, 2, 0, 0);
        Assert.False(noSheet.IsAllowed);
        Assert.Contains("physical sheet size", noSheet.Diagnostic, StringComparison.Ordinal);
    }

    [Fact]
    public void OneLabelOnALargerSheetMustLieOnTheSheet()
    {
        // A 100 x 50 mm label on A5 (148 x 210 mm): margins move it, never off the sheet.
        Assert.True(SheetLayoutContract.EvaluatePlacement(100, 50, 148, 210, 24, 30, 1, 1, 0, 0).IsAllowed);
        var tooFarRight = SheetLayoutContract.EvaluatePlacement(100, 50, 148, 210, 60, 30, 1, 1, 0, 0);
        Assert.False(tooFarRight.IsAllowed);
        Assert.Contains("1 × 1", tooFarRight.Diagnostic, StringComparison.Ordinal);
        Assert.False(SheetLayoutContract.EvaluatePlacement(50, 30, 148, 210, 0, 0, 2, 1, -1, 0).IsAllowed);
        Assert.False(SheetLayoutContract.EvaluatePlacement(50, 30, 148, 210, -2, 0, 1, 1, 0, 0).IsAllowed);
    }

    [Fact]
    public void RightAndBottomMarginsAndFitCountFollowNiceLabel()
    {
        // NiceLabel: right margin = paper width - (left margin + labels + gaps).
        Assert.Equal(20, 148 - SheetLayoutContract.ExtentMm(5, 60, 2, 3), 6);
        Assert.Equal(36, 210 - SheetLayoutContract.ExtentMm(5, 40, 4, 3), 6);
        Assert.Equal(2, SheetLayoutContract.FitCount(148, 5, 60, 3));
        Assert.Equal(4, SheetLayoutContract.FitCount(210, 5, 40, 3));
        Assert.Equal(1, SheetLayoutContract.FitCount(50, 0, 100, 0));
        // 2 x 7 labels of 99.1 x 38.1 mm on A4 from 4.65 / 15.15 mm margins.
        Assert.Equal(2, SheetLayoutContract.FitCount(210, 4.65, 99.1, 2.5));
        Assert.Equal(7, SheetLayoutContract.FitCount(297, 15.15, 38.1, 0));
    }

    [Fact]
    public void GridsAndOneLabelOnALargerSheetPrintWholeSheets()
    {
        static PrinterProfile Paper(double width, double height, int across = 1,
            PaperSizeSource source = PaperSizeSource.Manual, PrinterSettingsSource settings = PrinterSettingsSource.Label)
            => new() { PhysicalWidthMm = width, PhysicalHeightMm = height, LabelsAcross = across, PaperSizeSource = source, SettingsSource = settings };

        Assert.True(SheetLayoutContract.PrintsWholeSheet(Paper(148, 210), 100, 50));
        Assert.True(SheetLayoutContract.PrintsWholeSheet(Paper(210, 297, across: 2), 99.1, 38.1));
        // Label stock of the label's size, also the same die turned 90°, keeps label pages.
        Assert.False(SheetLayoutContract.PrintsWholeSheet(Paper(100, 50), 100, 50));
        Assert.False(SheetLayoutContract.PrintsWholeSheet(Paper(50, 100), 100, 50));
        // The driver owns the page size.
        Assert.False(SheetLayoutContract.PrintsWholeSheet(Paper(148, 210, source: PaperSizeSource.DriverAutomatic), 100, 50));
        Assert.False(SheetLayoutContract.PrintsWholeSheet(Paper(148, 210, settings: PrinterSettingsSource.Driver), 100, 50));
    }

    [Theory]
    // Across then down from the top-left (NiceLabel WebSettings 21).
    [InlineData(0, false, false, false, 0, 0)]
    [InlineData(1, false, false, false, 52, 0)]
    [InlineData(2, false, false, false, 0, 32)]
    [InlineData(4, false, false, false, 0, 0)]
    // Down then across.
    [InlineData(1, true, false, false, 0, 32)]
    [InlineData(2, true, false, false, 52, 0)]
    // Start corner.
    [InlineData(0, false, true, false, 52, 0)]
    [InlineData(0, false, false, true, 0, 32)]
    public void CellOriginFollowsFillOrderAndStartCorner(
        int index, bool downFirst, bool fromRight, bool fromBottom, double x, double y)
    {
        // 2 across x 2 down, 50 x 30 mm labels, 2 mm gaps; index 4 wraps to the next page.
        var cell = SheetLayoutContract.CellOrigin(index, 50, 30, 2, 2, 2, 2, downFirst, fromRight, fromBottom);
        Assert.Equal((x, y), cell);
    }
}
