using ANLAbel.Core.Geometry;
using Xunit;

namespace ANLAbel.UnitTests;

public sealed class SmartSpacingContractTests
{
    [Fact]
    public void GetAdjacentGaps_UsesSortedNonOverlappingNeighbors()
    {
        var gaps = SmartSpacingContract.GetAdjacentGaps(new[]
        {
            new SpacingInterval(20, 30, "b"),
            new SpacingInterval(0, 10, "a"),
            new SpacingInterval(8, 12, "overlap")
        });

        Assert.Single(gaps);
        Assert.Equal(8, gaps[0].Gap, precision: 6);
        Assert.Equal("overlap", gaps[0].BeforeKey);
        Assert.Equal("b", gaps[0].AfterKey);
    }

    [Fact]
    public void CandidateLeadingPositions_PreserveTheMeasuredGapBeforeAndAfter()
    {
        var gap = new SmartSpacingGap(10, 15, 5, "a", "b") { AfterTrailing = 20 };
        var candidates = SmartSpacingContract.CandidateLeadingPositions(4, gap);

        Assert.Equal(new[] { 25d, 1d }, candidates);
    }

    [Fact]
    public void GetAdjacentGaps_DoesNotCreateGapInsideAnOverlappingRun()
    {
        var gaps = SmartSpacingContract.GetAdjacentGaps(new[]
        {
            new SpacingInterval(0, 100, "wide"),
            new SpacingInterval(20, 30, "nested"),
            new SpacingInterval(40, 50, "inside"),
            new SpacingInterval(120, 130, "after")
        });

        Assert.Single(gaps);
        Assert.Equal(20, gaps[0].Gap, precision: 6);
        Assert.Equal("wide", gaps[0].BeforeKey);
        Assert.Equal("after", gaps[0].AfterKey);
    }

    [Fact]
    public void GetAdjacentGaps_NullSourceFailsClosed()
    {
        Assert.Throws<ArgumentNullException>(() => SmartSpacingContract.GetAdjacentGaps(null!));
    }

    [Fact]
    public void GetAdjacentGaps_DropsNonFiniteAndInvertedIntervals()
    {
        var gaps = SmartSpacingContract.GetAdjacentGaps(new[]
        {
            new SpacingInterval(0, 10, "a"),
            new SpacingInterval(double.NaN, 5, "nan"),
            new SpacingInterval(0, double.PositiveInfinity, "halfInfinite"),
            new SpacingInterval(50, 40, "inverted"),
            new SpacingInterval(20, 30, "b")
        });

        Assert.Single(gaps);
        Assert.Equal("a", gaps[0].BeforeKey);
        Assert.Equal("b", gaps[0].AfterKey);
        Assert.Equal(10, gaps[0].Gap, precision: 6);
        Assert.Equal(30, gaps[0].AfterTrailing, precision: 6);
    }

    [Fact]
    public void GetAdjacentGaps_KeepsZeroLengthAnchorInterval()
    {
        // A degenerate point interval (Trailing == Leading) is valid ink and must
        // survive the `Trailing >= Leading` filter, producing gaps on both sides.
        var gaps = SmartSpacingContract.GetAdjacentGaps(new[]
        {
            new SpacingInterval(0, 10, "a"),
            new SpacingInterval(20, 20, "point"),
            new SpacingInterval(40, 50, "b")
        });

        Assert.Equal(2, gaps.Count);
        Assert.Equal(10, gaps[0].Gap, precision: 6);
        Assert.Equal(20, gaps[1].Gap, precision: 6);
    }

    [Fact]
    public void GetAdjacentGaps_TieBreaksByStableKeyOrdinal()
    {
        // Same Leading and Trailing: ascending StableKey keeps "a" as the run
        // anchor, so the later gap must report "a" (not "b") as its before key.
        var gaps = SmartSpacingContract.GetAdjacentGaps(new[]
        {
            new SpacingInterval(0, 10, "b"),
            new SpacingInterval(0, 10, "a"),
            new SpacingInterval(50, 60, "c")
        });

        Assert.Single(gaps);
        Assert.Equal("a", gaps[0].BeforeKey);
        Assert.Equal("c", gaps[0].AfterKey);
    }

    [Fact]
    public void GetAdjacentGaps_EqualTrailingOverlapKeepsTheFirstAnchor()
    {
        // When the overlapping neighbour shares the same trailing edge, the
        // strict `>` keeps the first interval as the anchor.
        var gaps = SmartSpacingContract.GetAdjacentGaps(new[]
        {
            new SpacingInterval(0, 100, "wide"),
            new SpacingInterval(50, 100, "equalTrailing"),
            new SpacingInterval(200, 210, "after")
        });

        Assert.Single(gaps);
        Assert.Equal("wide", gaps[0].BeforeKey);
        Assert.Equal(100, gaps[0].Gap, precision: 6);
    }

    [Fact]
    public void GetAdjacentGaps_FewerThanTwoValidIntervalsYieldsNoGaps()
    {
        Assert.Empty(SmartSpacingContract.GetAdjacentGaps(new[]
        {
            new SpacingInterval(0, 10, "only"),
            new SpacingInterval(60, 50, "inverted-dropped")
        }));

        Assert.Empty(SmartSpacingContract.GetAdjacentGaps(Array.Empty<SpacingInterval>()));
    }

    [Fact]
    public void GetAdjacentGaps_KeepsFurthestTrailingRunAndIgnoresShorterNested()
    {
        // nested is fully inside wide (trailing 30 <= 100) so `before` must stay
        // wide; the surviving gap is measured from wide's trailing edge.
        var gaps = SmartSpacingContract.GetAdjacentGaps(new[]
        {
            new SpacingInterval(0, 100, "wide"),
            new SpacingInterval(20, 30, "nested"),
            new SpacingInterval(200, 210, "after")
        });

        Assert.Single(gaps);
        Assert.Equal(100, gaps[0].Gap, precision: 6);
        Assert.Equal("wide", gaps[0].BeforeKey);
        Assert.Equal("after", gaps[0].AfterKey);
    }

    [Fact]
    public void GetAdjacentGaps_ClampsSubEpsilonTouchToZero()
    {
        // Edges within EpsilonMm are treated as touching, not overlapping, and
        // the reported gap is clamped to exactly 0.
        var gaps = SmartSpacingContract.GetAdjacentGaps(new[]
        {
            new SpacingInterval(0, 10, "a"),
            new SpacingInterval(9.99995, 20, "b")
        });

        Assert.Single(gaps);
        Assert.Equal(0, gaps[0].Gap, precision: 6);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void CandidateLeadingPositions_FailsClosedOnBadObjectSize(double size)
    {
        var gap = new SmartSpacingGap(10, 15, 5, "a", "b") { AfterTrailing = 20 };
        Assert.Empty(SmartSpacingContract.CandidateLeadingPositions(size, gap));
    }

    [Fact]
    public void CandidateLeadingPositions_FailsClosedOnNonFiniteGapFields()
    {
        var nonFiniteGap = new SmartSpacingGap(10, 15, double.NaN, "a", "b") { AfterTrailing = 20 };
        Assert.Empty(SmartSpacingContract.CandidateLeadingPositions(4, nonFiniteGap));

        var nonFiniteTrailing = new SmartSpacingGap(10, 15, 5, "a", "b")
        {
            AfterTrailing = double.PositiveInfinity
        };
        Assert.Empty(SmartSpacingContract.CandidateLeadingPositions(4, nonFiniteTrailing));
    }
}
