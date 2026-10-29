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
