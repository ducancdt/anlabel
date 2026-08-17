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
}
