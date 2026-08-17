using ANLAbel.Core.Geometry;
using Xunit;

namespace ANLAbel.UnitTests;

public sealed class OpticalAlignmentContractTests
{
    [Fact]
    public void CenterAlignmentUsesVisibleInkInsteadOfFrameOrigin()
    {
        var result = OpticalAlignmentContract.Align(
            new OpticalBounds(2, 1, 12, 9),
            new OpticalBounds(20, 5, 34, 15),
            OpticalAlignmentAxis.Horizontal);

        Assert.True(result.Succeeded);
        Assert.Equal(20, result.DeltaX, precision: 6);
        Assert.Equal(0, result.DeltaY, precision: 6);
    }

    [Fact]
    public void LeadingAndTrailingAnchorsAreDeterministicOnBothAxes()
    {
        var source = new OpticalBounds(2, 4, 12, 14);
        var target = new OpticalBounds(20, 30, 34, 46);

        var leading = OpticalAlignmentContract.Align(
            source,
            target,
            OpticalAlignmentAxis.Both,
            OpticalAlignmentAnchor.Leading);
        var trailing = OpticalAlignmentContract.Align(
            source,
            target,
            OpticalAlignmentAxis.Both,
            OpticalAlignmentAnchor.Trailing);

        Assert.Equal(18, leading.DeltaX, precision: 6);
        Assert.Equal(26, leading.DeltaY, precision: 6);
        Assert.Equal(22, trailing.DeltaX, precision: 6);
        Assert.Equal(32, trailing.DeltaY, precision: 6);
    }

    [Fact]
    public void InvalidOrEmptyInkFailsClosed()
    {
        var result = OpticalAlignmentContract.Align(
            new OpticalBounds(1, 1, 1, 5),
            new OpticalBounds(0, 0, 5, 5),
            OpticalAlignmentAxis.Both);

        Assert.False(result.Succeeded);
        Assert.Contains("finite visible ink", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, result.DeltaX, precision: 6);
        Assert.Equal(0, result.DeltaY, precision: 6);
    }

    [Fact]
    public void AxisSelectionNeverMovesTheOtherAxis()
    {
        var result = OpticalAlignmentContract.Align(
            new OpticalBounds(2, 4, 12, 14),
            new OpticalBounds(20, 30, 34, 46),
            OpticalAlignmentAxis.Vertical,
            OpticalAlignmentAnchor.Center);

        Assert.Equal(0, result.DeltaX, precision: 6);
        Assert.Equal(29, result.DeltaY, precision: 6);
    }
}
