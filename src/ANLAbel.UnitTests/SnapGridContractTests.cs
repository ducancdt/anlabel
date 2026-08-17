using ANLAbel.Core.Geometry;
using Xunit;

namespace ANLAbel.UnitTests;

public sealed class SnapGridContractTests
{
    [Fact]
    public void PublicStepBoundsAreTheAuthoredConstants()
    {
        Assert.Equal(1.0, SnapGridContract.DefaultStepMm);
        Assert.Equal(0.25, SnapGridContract.MinimumStepMm);
        Assert.Equal(20.0, SnapGridContract.MaximumStepMm);
        Assert.True(SnapGridContract.MinimumStepMm < SnapGridContract.DefaultStepMm);
        Assert.True(SnapGridContract.DefaultStepMm < SnapGridContract.MaximumStepMm);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    [InlineData(0)]
    [InlineData(-1)]
    public void NormalizeStep_FallsBackToDefaultForNonPositiveOrNonFinite(double stepMm)
    {
        Assert.Equal(SnapGridContract.DefaultStepMm, SnapGridContract.NormalizeStep(stepMm));
    }

    [Fact]
    public void NormalizeStep_ClampsAndPreservesTheAuthoredRange()
    {
        Assert.Equal(SnapGridContract.MinimumStepMm, SnapGridContract.NormalizeStep(0.01));
        Assert.Equal(SnapGridContract.MinimumStepMm, SnapGridContract.NormalizeStep(SnapGridContract.MinimumStepMm));
        Assert.Equal(SnapGridContract.DefaultStepMm, SnapGridContract.NormalizeStep(SnapGridContract.DefaultStepMm));
        Assert.Equal(SnapGridContract.MaximumStepMm, SnapGridContract.NormalizeStep(SnapGridContract.MaximumStepMm));
        Assert.Equal(SnapGridContract.MaximumStepMm, SnapGridContract.NormalizeStep(999));
    }

    [Fact]
    public void Snap_UsesPhysicalStepAndAwayFromZero()
    {
        Assert.Equal(12, SnapGridContract.Snap(12, 1));
        Assert.Equal(12, SnapGridContract.Snap(12.4, 1));
        Assert.Equal(13, SnapGridContract.Snap(12.6, 1));
        Assert.Equal(13, SnapGridContract.Snap(12.5, 1));
        Assert.Equal(-13, SnapGridContract.Snap(-12.5, 1));
        Assert.Equal(12.5, SnapGridContract.Snap(12.26, 0.5));
        Assert.Equal(-0.5, SnapGridContract.Snap(-0.26, 0.5));
    }

    [Fact]
    public void Snap_NonFinitePositionIsOrigin()
    {
        Assert.Equal(0, SnapGridContract.Snap(double.NaN, 1));
        Assert.Equal(0, SnapGridContract.Snap(double.PositiveInfinity, 1));
        Assert.Equal(0, SnapGridContract.Snap(double.NegativeInfinity, 1));
    }

    [Fact]
    public void Snap_UnsafeStepUsesNormalizedDefault()
    {
        var onDefault = SnapGridContract.Snap(12.6, SnapGridContract.DefaultStepMm);
        Assert.Equal(onDefault, SnapGridContract.Snap(12.6, 0));
        Assert.Equal(onDefault, SnapGridContract.Snap(12.6, double.NaN));
        Assert.Equal(onDefault, SnapGridContract.Snap(12.6, -4));
    }

    [Fact]
    public void TrySnap_RequiresTheTargetInsideTheInteractionTolerance()
    {
        Assert.True(SnapGridContract.TrySnap(10.4, 1, 0.5, out var accepted));
        Assert.Equal(SnapGridContract.Snap(10.4, 1), accepted);

        Assert.False(SnapGridContract.TrySnap(10.4, 1, 0.2, out var rejected));
        Assert.Equal(SnapGridContract.Snap(10.4, 1), rejected);

        var distance = Math.Abs(SnapGridContract.Snap(10.4, 1) - 10.4);
        Assert.True(SnapGridContract.TrySnap(10.4, 1, distance, out _));
        Assert.True(SnapGridContract.TrySnap(10, 1, 0, out var exact));
        Assert.Equal(10, exact);
        Assert.False(SnapGridContract.TrySnap(10.1, 1, 0, out _));
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.NegativeInfinity)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(-0.01)]
    public void TrySnap_InvalidToleranceNeverAcquires(double toleranceMm)
    {
        Assert.False(SnapGridContract.TrySnap(10.4, 1, toleranceMm, out var target));
        Assert.Equal(SnapGridContract.Snap(10.4, 1), target);
    }
}
