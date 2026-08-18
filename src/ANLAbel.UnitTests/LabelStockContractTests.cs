using ANLAbel.Core.Enums;
using ANLAbel.Core.Geometry;
using ANLAbel.Core.Printing;
using Xunit;

namespace ANLAbel.UnitTests;

public sealed class LabelStockContractTests
{
    [Fact]
    public void OfficeSheetsAreRejectedInBothOrientations()
    {
        Assert.True(LabelStockContract.IsOfficeSheet(210, 297));
        Assert.True(LabelStockContract.IsOfficeSheet(297, 210));
        Assert.True(LabelStockContract.IsOfficeSheet(215.9, 279.4));
        Assert.True(LabelStockContract.IsOfficeSheet(279.4, 215.9));
        Assert.True(LabelStockContract.IsOfficeSheet(215.9, 355.6));
        Assert.True(LabelStockContract.IsOfficeSheet(297, 420));
        Assert.False(LabelStockContract.IsOfficeSheet(100, 50));
        Assert.False(LabelStockContract.IsOfficeSheet(102, 152));
        Assert.False(LabelStockContract.IsOfficeSheet(210, 148));
    }

    [Fact]
    public void NameClaimsOfficeSheetOnlyForA4FullSheet()
    {
        Assert.True(LabelStockContract.NameClaimsOfficeSheet("A4 210 × 297 mm"));
        Assert.False(LabelStockContract.NameClaimsOfficeSheet("210 × 148 mm (A5)"));
        Assert.False(LabelStockContract.NameClaimsOfficeSheet("100 × 150 mm shipping"));
        Assert.False(LabelStockContract.NameClaimsOfficeSheet(null));
    }

    [Fact]
    public void AuthoredLabelMaySwapPhysicalOrientation()
    {
        var swapped = LabelGeometry.OrientSize(100, 50, LabelOrientation.Portrait);
        Assert.True(LabelStockContract.MatchesAuthoredLabel(100, 50, 100, 50));
        Assert.True(LabelStockContract.MatchesAuthoredLabel(100, 50, swapped.WidthMm, swapped.HeightMm));
        Assert.False(LabelStockContract.MatchesAuthoredLabel(100, 50, 210, 297));
        Assert.False(LabelStockContract.MatchesAuthoredLabel(0, 50, 50, 50));
    }

    [Fact]
    public void EvaluateBlocksOfficeIncompleteAndMismatchedStock()
    {
        Assert.True(LabelStockContract.Evaluate(100, 50, 0, 0).IsAllowed);
        Assert.True(LabelStockContract.Evaluate(50, 100, 100, 50).IsAllowed);

        var office = LabelStockContract.Evaluate(50, 30, 210, 297);
        Assert.False(office.IsAllowed);
        Assert.Contains("office sheets", office.Diagnostic, StringComparison.OrdinalIgnoreCase);

        var namedOffice = LabelStockContract.Evaluate(100, 50, 0, 0, "A4 210 × 297 mm");
        Assert.False(namedOffice.IsAllowed);

        var incomplete = LabelStockContract.Evaluate(100, 50, 100, 0);
        Assert.False(incomplete.IsAllowed);
        Assert.Contains("incomplete", incomplete.Diagnostic, StringComparison.OrdinalIgnoreCase);

        var mismatch = LabelStockContract.Evaluate(50, 30, 100, 150);
        Assert.False(mismatch.IsAllowed);
        Assert.Contains("does not match", mismatch.Diagnostic, StringComparison.OrdinalIgnoreCase);

        var invalid = LabelStockContract.Evaluate(0, 30, 0, 0);
        Assert.False(invalid.IsAllowed);

        var tooSmall = LabelStockContract.Evaluate(5, 20, 5, 20);
        Assert.False(tooSmall.IsAllowed);
        Assert.Contains("8", tooSmall.Diagnostic, StringComparison.Ordinal);

        var tooLarge = LabelStockContract.Evaluate(50, 500, 50, 500);
        Assert.False(tooLarge.IsAllowed);
        Assert.Contains("400", tooLarge.Diagnostic, StringComparison.Ordinal);
    }

    [Fact]
    public void OperatorStockIsNeverDriverAutomatic()
    {
        Assert.Equal(PaperSizeSource.Manual, LabelStockContract.SourceForOperatorStock());
        Assert.NotEqual(PaperSizeSource.DriverAutomatic, LabelStockContract.SourceForOperatorStock());
    }
}
