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

    [Fact]
    public void HorizontalAxisNeverMovesTheVerticalAxis()
    {
        var result = OpticalAlignmentContract.Align(
            new OpticalBounds(2, 4, 12, 14),
            new OpticalBounds(20, 30, 34, 46),
            OpticalAlignmentAxis.Horizontal,
            OpticalAlignmentAnchor.Center);

        // Horizontal-only: X centers move, Y stays put.
        Assert.Equal(20, result.DeltaX, precision: 6);
        Assert.Equal(0, result.DeltaY, precision: 6);
    }

    [Theory]
    [InlineData(99, (int)OpticalAlignmentAnchor.Center)]
    [InlineData((int)OpticalAlignmentAxis.Both, 99)]
    public void UnknownAxisOrAnchorFailsClosed(int axis, int anchor)
    {
        var result = OpticalAlignmentContract.Align(
            new OpticalBounds(2, 4, 12, 14),
            new OpticalBounds(20, 30, 34, 46),
            (OpticalAlignmentAxis)axis,
            (OpticalAlignmentAnchor)anchor);

        Assert.False(result.Succeeded);
        Assert.Contains("unknown axis or anchor", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, result.DeltaX, precision: 6);
        Assert.Equal(0, result.DeltaY, precision: 6);
    }

    [Fact]
    public void TargetWithoutInkFailsClosed()
    {
        var result = OpticalAlignmentContract.Align(
            new OpticalBounds(0, 0, 5, 5),
            new OpticalBounds(1, 1, 1, 5),
            OpticalAlignmentAxis.Both);

        Assert.False(result.Succeeded);
        Assert.Contains("finite visible ink", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void NonFiniteInkBoundsFailClosed()
    {
        var result = OpticalAlignmentContract.Align(
            new OpticalBounds(0, 0, double.PositiveInfinity, 5),
            new OpticalBounds(0, 0, 5, 5),
            OpticalAlignmentAxis.Both);

        Assert.False(result.Succeeded);
        Assert.Contains("finite visible ink", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void InkExactlyAtMinimumExtentIsNotEnoughInk()
    {
        // Width == MinimumExtentMm must fail the strict `> MinimumExtentMm` gate.
        var atFloor = new OpticalBounds(0, 0, OpticalBounds.MinimumExtentMm, 5);
        Assert.False(atFloor.HasInk);

        var justAbove = new OpticalBounds(0, 0, OpticalBounds.MinimumExtentMm * 2, 5);
        Assert.True(justAbove.HasInk);
    }

    [Fact]
    public void HeightBelowMinimumExtentIsNotEnoughInk()
    {
        var flat = new OpticalBounds(0, 0, 5, OpticalBounds.MinimumExtentMm);
        Assert.False(flat.HasInk);
    }
}
