using ANLAbel.Core.Printing;
using Xunit;

namespace ANLAbel.UnitTests;

public sealed class SpoolJobIdentityResolverTests
{
    [Fact]
    public void ResolvesOneNewJob()
    {
        var before = new[] { new SpoolJobIdentityCandidate(10, "older") };
        var after = new[]
        {
            new SpoolJobIdentityCandidate(10, "older"),
            new SpoolJobIdentityCandidate(11, "ANLAbel labels")
        };

        Assert.Equal(11, SpoolJobIdentityResolver.TryResolve(before, after, "ANLAbel labels"));
    }

    [Fact]
    public void DoesNotGuessWhenSeveralNewJobsExist()
    {
        var before = new[] { new SpoolJobIdentityCandidate(10, "older") };
        var after = new[]
        {
            new SpoolJobIdentityCandidate(10, "older"),
            new SpoolJobIdentityCandidate(11, "other application"),
            new SpoolJobIdentityCandidate(12, "another application")
        };

        Assert.Null(SpoolJobIdentityResolver.TryResolve(before, after));
    }

    [Fact]
    public void JobNameCanDisambiguateConcurrentQueueEntries()
    {
        var before = Array.Empty<SpoolJobIdentityCandidate>();
        var after = new[]
        {
            new SpoolJobIdentityCandidate(11, "other application"),
            new SpoolJobIdentityCandidate(12, "ANLAbel labels")
        };

        Assert.Equal(12, SpoolJobIdentityResolver.TryResolve(before, after, "ANLAbel labels"));
    }

    [Fact]
    public void DuplicateMatchingNamesRemainAmbiguous()
    {
        var after = new[]
        {
            new SpoolJobIdentityCandidate(11, "ANLAbel labels"),
            new SpoolJobIdentityCandidate(12, "ANLAbel labels")
        };

        Assert.Null(SpoolJobIdentityResolver.TryResolve(
            Array.Empty<SpoolJobIdentityCandidate>(),
            after,
            "ANLAbel labels"));
    }

    [Fact]
    public void MissingSnapshotNeverCreatesFalseCorrelation()
    {
        var after = new[] { new SpoolJobIdentityCandidate(11, "ANLAbel labels") };

        Assert.Null(SpoolJobIdentityResolver.TryResolve(null, after));
        Assert.Null(SpoolJobIdentityResolver.TryResolve(Array.Empty<SpoolJobIdentityCandidate>(), null));
    }
}
