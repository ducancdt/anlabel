using ANLAbel.Core.Printing;
using ANLAbel.Data.PrintLogs;
using Xunit;

namespace ANLAbel.UnitTests;

public sealed class PrintRecoveryCandidateFilterTests
{
    [Fact]
    public void BlankQueryReturnsAllCandidatesInSourceOrder()
    {
        var source = new[]
        {
            Candidate("job-a", "Zebra-A", 11, "Printing"),
            Candidate("job-b", "Zebra-B", 12, "Offline")
        };

        var result = PrintRecoveryCandidateFilter.Apply(source, "  ");

        Assert.Equal(new[] { "job-a", "job-b" }, result.Select(candidate => candidate.JobId));
    }

    [Fact]
    public void ExactJobIdMatchWinsForScannerInput()
    {
        var source = new[]
        {
            Candidate("job-123", "Zebra-A", 11, "Printing"),
            Candidate("job-123-reprint", "Zebra-A", 12, "Printing")
        };

        var result = PrintRecoveryCandidateFilter.Apply(source, " JOB-123 ");

        var candidate = Assert.Single(result);
        Assert.Equal("job-123", candidate.JobId);
    }

    [Fact]
    public void PartialSearchCoversPrinterQueueAndManifestWithoutMutatingSource()
    {
        var source = new[]
        {
            Candidate("job-a", "Zebra-A", 11, "Printing", manifest: "manifest-alpha"),
            Candidate("job-b", "TSC-B", 12, "Offline", manifest: "manifest-beta")
        };

        var printer = PrintRecoveryCandidateFilter.Apply(source, "tsc");
        var spool = PrintRecoveryCandidateFilter.Apply(source, "11");
        var manifest = PrintRecoveryCandidateFilter.Apply(source, "BETA");

        Assert.Equal("job-b", Assert.Single(printer).JobId);
        Assert.Equal("job-a", Assert.Single(spool).JobId);
        Assert.Equal("job-b", Assert.Single(manifest).JobId);
        Assert.Equal(new[] { "job-a", "job-b" }, source.Select(candidate => candidate.JobId));
    }

    private static PrintJobRecoveryCandidate Candidate(
        string jobId,
        string printerName,
        int spoolJobId,
        string queueState,
        string manifest = "manifest")
    {
        return new PrintJobRecoveryCandidate(
            jobId,
            PrintJobLifecycleState.SpoolAccepted,
            PrintJobRecoveryAction.OperatorDecision,
            DateTimeOffset.UtcNow,
            printerName,
            spoolJobId,
            queueState,
            "document",
            "scene",
            "contract",
            "Needs operator review.",
            ManifestFingerprint: manifest);
    }
}
