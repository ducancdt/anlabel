using ANLAbel.Core.Geometry;
using Xunit;

namespace ANLAbel.UnitTests;

public sealed class SnapPathMatrixContractTests
{
    [Theory]
    [InlineData(SnapPathKind.SingleMove)]
    [InlineData(SnapPathKind.GroupMove)]
    [InlineData(SnapPathKind.Resize)]
    [InlineData(SnapPathKind.Draw)]
    public void ExactAcquireBoundaryUsesSameWinnerAcrossZoomLadder(SnapPathKind pathKind)
    {
        var sameWinner = SnapPathMatrixContract.SameWinnerAcrossZooms(
            pathKind,
            SnapPathMatrixContract.SoftwareZoomLadder,
            zoom =>
            {
                // A candidate sitting exactly on the 6 DIP acquire boundary
                // must still win at every zoom once converted to document mm.
                var acquireMm = SnapPathMatrixContract.AcquireToleranceMm(zoom);
                return new[]
                {
                    new SnapCandidate(10, 10 + acquireMm, acquireMm, Priority: 80, StableKey: "edge"),
                    new SnapCandidate(10, 10 + acquireMm * 0.5, acquireMm * 0.5, Priority: 60, StableKey: "near-grid")
                };
            },
            proposedPositionMm: 10);

        Assert.True(sameWinner);
    }

    [Theory]
    [InlineData(SnapPathKind.SingleMove)]
    [InlineData(SnapPathKind.GroupMove)]
    [InlineData(SnapPathKind.Resize)]
    [InlineData(SnapPathKind.Draw)]
    public void SemanticPriorityBeatsCloserDistanceOnEveryPath(SnapPathKind pathKind)
    {
        var hysteresis = new SnapHysteresisState();
        var decision = SnapPathMatrixContract.Resolve(
            pathKind,
            zoom: 1,
            candidates: new[]
            {
                new SnapCandidate(10, 10.2, 0.2, Priority: 90, StableKey: "same-edge"),
                new SnapCandidate(10, 10.05, 0.05, Priority: 50, StableKey: "cross-anchor")
            },
            hysteresis,
            proposedPositionMm: 10);

        Assert.True(decision.Snapped);
        Assert.Equal("same-edge", decision.WinnerKey);
        Assert.Equal(pathKind, decision.PathKind);
        Assert.False(decision.Bypassed);
    }

    [Fact]
    public void ReleaseHysteresisHoldsThenReleasesOutsideWindow()
    {
        var hysteresis = new SnapHysteresisState();
        var first = SnapPathMatrixContract.Resolve(
            SnapPathKind.SingleMove,
            zoom: 1,
            candidates: new[] { new SnapCandidate(10, 10, 0, Priority: 80, StableKey: "lock") },
            hysteresis,
            proposedPositionMm: 10);
        Assert.True(first.Snapped);

        var held = SnapPathMatrixContract.Resolve(
            SnapPathKind.GroupMove,
            zoom: 1,
            candidates: Array.Empty<SnapCandidate>(),
            hysteresis,
            proposedPositionMm: 10 + SnapPathMatrixContract.ReleaseToleranceMm(1) * 0.5);
        Assert.True(held.Snapped);
        Assert.Equal(10, held.TargetMm);

        var released = SnapPathMatrixContract.Resolve(
            SnapPathKind.Resize,
            zoom: 1,
            candidates: Array.Empty<SnapCandidate>(),
            hysteresis,
            proposedPositionMm: 10 + SnapPathMatrixContract.ReleaseToleranceMm(1) + 0.01);
        Assert.False(released.Snapped);
        Assert.Null(hysteresis.LockedTarget);
    }

    [Fact]
    public void AltBypassReturnsNoSnapAndLeavesHysteresisIdle()
    {
        var hysteresis = new SnapHysteresisState();
        var decision = SnapPathMatrixContract.Resolve(
            SnapPathKind.Draw,
            zoom: 2,
            candidates: new[] { new SnapCandidate(5, 5, 0, Priority: 100, StableKey: "would-win") },
            hysteresis,
            proposedPositionMm: 5,
            bypassSnap: true);

        Assert.True(decision.Bypassed);
        Assert.False(decision.Snapped);
        Assert.Null(decision.TargetMm);
        Assert.Null(hysteresis.LockedTarget);
        Assert.Equal(2, decision.NormalizedZoom);
    }

    [Fact]
    public void ScreenDipDistanceMapsToEqualDecisionAtOppositeZooms()
    {
        // 3 DIP of pointer error at 100% and 400% must still acquire the same
        // semantic key when the document-space distances are converted with
        // the shared tolerance contract.
        const double screenDip = 3;
        string? firstKey = null;
        foreach (var zoom in new[] { 1.0, 4.0 })
        {
            var distanceMm = SnapToleranceContract.ToDocumentMm(screenDip, zoom);
            var hysteresis = new SnapHysteresisState();
            var decision = SnapPathMatrixContract.Resolve(
                SnapPathKind.SingleMove,
                zoom,
                new[]
                {
                    new SnapCandidate(0, distanceMm, distanceMm, Priority: 80, StableKey: "peer-edge"),
                    new SnapCandidate(0, distanceMm * 2, distanceMm * 2, Priority: 80, StableKey: "far-edge")
                },
                hysteresis,
                proposedPositionMm: 0);
            Assert.True(decision.Snapped);
            firstKey ??= decision.WinnerKey;
            Assert.Equal(firstKey, decision.WinnerKey);
        }
    }
}
