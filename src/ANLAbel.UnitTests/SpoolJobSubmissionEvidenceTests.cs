using ANLAbel.Core.Printing;
using Xunit;

namespace ANLAbel.UnitTests;

public sealed class SpoolJobSubmissionEvidenceTests
{
    [Fact]
    public void ValidEvidenceRequiresAStablePrinterAndPositiveSnapshotIds()
    {
        var evidence = new SpoolJobSubmissionEvidence(
            "Zebra Test",
            "ANLAbel labels",
            new[] { new SpoolJobIdentityCandidate(17, "existing") },
            DateTimeOffset.UtcNow);

        Assert.True(evidence.IsValid);
    }

    [Fact]
    public void InvalidEvidenceDisablesDelayedCorrelation()
    {
        var evidence = new SpoolJobSubmissionEvidence(
            "",
            "ANLAbel labels",
            new[] { new SpoolJobIdentityCandidate(0, "unknown") },
            DateTimeOffset.UtcNow);

        Assert.False(evidence.IsValid);
    }
}
