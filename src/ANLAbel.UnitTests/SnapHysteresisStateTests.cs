using ANLAbel.Core.Geometry;
using Xunit;

namespace ANLAbel.UnitTests;

public sealed class SnapHysteresisStateTests
{
    [Fact]
    public void Resolve_AcquiresCandidateAndHoldsInsideReleaseWindow()
    {
        var state = new SnapHysteresisState();
        Assert.Null(state.LockedTarget);

        Assert.Equal(10, state.Resolve(9.5, 10, releaseTolerance: 1));
        Assert.Equal(10, state.LockedTarget);

        Assert.Equal(10, state.Resolve(10.8, candidateTarget: null, releaseTolerance: 1));
        Assert.Equal(10, state.Resolve(10.8, candidateTarget: 20, releaseTolerance: 1));
        Assert.Equal(10, state.Resolve(11, candidateTarget: null, releaseTolerance: 1));
        Assert.Equal(10, state.LockedTarget);
    }

    [Fact]
    public void Resolve_ReleasesOutsideWindowAndCanAcquireNewTarget()
    {
        var state = new SnapHysteresisState();
        state.Resolve(10, 10, releaseTolerance: 1);

        Assert.Null(state.Resolve(12, candidateTarget: null, releaseTolerance: 1));
        Assert.Null(state.LockedTarget);
        Assert.Equal(20, state.Resolve(19.5, 20, releaseTolerance: 1));
        Assert.Equal(20, state.LockedTarget);
    }

    [Fact]
    public void Resolve_ZeroToleranceHoldsOnlyTheExactLockedPosition()
    {
        var state = new SnapHysteresisState();
        Assert.Equal(10, state.Resolve(10, 10, releaseTolerance: 0));
        Assert.Equal(10, state.Resolve(10, candidateTarget: null, releaseTolerance: 0));
        Assert.Null(state.Resolve(10.01, candidateTarget: null, releaseTolerance: 0));
        Assert.Null(state.LockedTarget);
    }

    [Fact]
    public void Resolve_WithoutCandidateDoesNotAcquire()
    {
        var state = new SnapHysteresisState();
        Assert.Null(state.Resolve(10, candidateTarget: null, releaseTolerance: 1));
        Assert.Null(state.LockedTarget);
    }

    [Fact]
    public void Resolve_NegativeToleranceFailsClosed()
    {
        var state = new SnapHysteresisState();
        Assert.Throws<ArgumentOutOfRangeException>(() => state.Resolve(10, 10, releaseTolerance: -0.01));
        Assert.Null(state.LockedTarget);
    }

    [Fact]
    public void Reset_ClearsLockSoLaterResolveDoesNotHold()
    {
        var state = new SnapHysteresisState();
        state.Resolve(10, 10, releaseTolerance: 1);
        state.Reset();

        Assert.Null(state.LockedTarget);
        Assert.Null(state.Resolve(10.2, candidateTarget: null, releaseTolerance: 1));
    }
}
