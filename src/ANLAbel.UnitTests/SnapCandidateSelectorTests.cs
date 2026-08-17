using ANLAbel.Core.Geometry;
using Xunit;

namespace ANLAbel.UnitTests;

public sealed class SnapCandidateSelectorTests
{
    [Fact]
    public void DeltaIsTargetMinusSource()
    {
        var right = new SnapCandidate(10, 12.5, 2.5, Priority: 80, StableKey: "right");
        var left = new SnapCandidate(10, 8, 2, Priority: 80, StableKey: "left");

        Assert.Equal(2.5, right.Delta);
        Assert.Equal(-2, left.Delta);
    }

    [Fact]
    public void Choose_PrefersSemanticPriorityOverSlightlyCloserDistance()
    {
        var winner = SnapCandidateSelector.Choose(
            new[]
            {
                new SnapCandidate(10, 10.2, 0.2, Priority: 85, StableKey: "same-edge"),
                new SnapCandidate(10, 10.05, 0.05, Priority: 65, StableKey: "cross-anchor")
            },
            acquireTolerance: 1);

        Assert.Equal("same-edge", winner?.StableKey);
    }

    [Fact]
    public void Choose_UsesCloserDistanceThenOrdinalKeyOnPriorityTie()
    {
        var closer = SnapCandidateSelector.Choose(
            new[]
            {
                new SnapCandidate(10, 10.3, 0.3, Priority: 80, StableKey: "farther"),
                new SnapCandidate(10, 10.1, 0.1, Priority: 80, StableKey: "closer")
            },
            acquireTolerance: 1);

        Assert.Equal("closer", closer?.StableKey);

        var byKey = SnapCandidateSelector.Choose(
            new[]
            {
                new SnapCandidate(10, 10.2, 0.2, Priority: 80, StableKey: "z-target"),
                new SnapCandidate(10, 9.8, 0.2, Priority: 80, StableKey: "a-target")
            },
            acquireTolerance: 1);

        Assert.Equal("a-target", byKey?.StableKey);
    }

    [Fact]
    public void Choose_AcceptsZeroDistanceAndExactAcquireBoundary()
    {
        var onGrid = SnapCandidateSelector.Choose(
            new[] { new SnapCandidate(10, 10, 0, Priority: 80, StableKey: "on") },
            acquireTolerance: 1);

        Assert.Equal("on", onGrid?.StableKey);

        var boundary = SnapCandidateSelector.Choose(
            new[] { new SnapCandidate(10, 10.5, 0.5, Priority: 80, StableKey: "boundary") },
            acquireTolerance: 0.5);

        Assert.Equal("boundary", boundary?.StableKey);
    }

    [Fact]
    public void Choose_RejectsNegativeDistanceAndOutsideWindow()
    {
        Assert.Null(SnapCandidateSelector.Choose(
            new[] { new SnapCandidate(10, 9.9, -0.1, Priority: 100, StableKey: "behind") },
            acquireTolerance: 1));

        Assert.Null(SnapCandidateSelector.Choose(
            new[] { new SnapCandidate(10, 11.1, 1.1, Priority: 100, StableKey: "far") },
            acquireTolerance: 1));

        Assert.Null(SnapCandidateSelector.Choose(Array.Empty<SnapCandidate>(), acquireTolerance: 1));
    }

    [Fact]
    public void Choose_RejectsNullCandidates()
    {
        Assert.Throws<ArgumentNullException>(() =>
            SnapCandidateSelector.Choose(null!, acquireTolerance: 1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-0.01)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Choose_RejectsNonPositiveOrNonFiniteAcquireTolerance(double acquireTolerance)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            SnapCandidateSelector.Choose(Array.Empty<SnapCandidate>(), acquireTolerance));
    }
}
