using ANLAbel.Core.Enums;
using ANLAbel.Core.Printing;
using Xunit;

namespace ANLAbel.UnitTests;

public sealed class DispatchRevalidationContractTests
{
    [Fact]
    public void StableContractsAllowSubmission()
    {
        var prepared = NewContract();
        var result = DispatchRevalidationContract.Evaluate(
            "doc-1",
            prepared,
            preparedTicketVerified: true,
            "doc-1",
            prepared,
            finalTicketVerified: true,
            expectedOutputContractHash: prepared.Fingerprint);

        Assert.True(result.Allowed);
        Assert.True(result.SubmissionAllowed);
        Assert.Empty(result.ChangedFields);
        Assert.Equal(string.Empty, result.Diagnostic);
    }

    [Fact]
    public void DpiDriftBlocksWithNamedFieldAndNoSubmission()
    {
        var prepared = NewContract();
        var final = prepared with { DpiX = 300 };
        var result = DispatchRevalidationContract.Evaluate(
            "doc-1",
            prepared,
            preparedTicketVerified: true,
            "doc-1",
            final,
            finalTicketVerified: true,
            expectedOutputContractHash: prepared.Fingerprint);

        Assert.False(result.SubmissionAllowed);
        Assert.Contains("dpi", result.ChangedFields);
        Assert.StartsWith(DispatchRevalidationContract.BlockDiagnosticPrefix, result.Diagnostic, StringComparison.Ordinal);
        Assert.Contains("dpi", result.Diagnostic, StringComparison.Ordinal);
        Assert.Contains("no label was submitted", result.Diagnostic, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void MediaAndImageableAreaDriftAreNamedSeparately()
    {
        var prepared = NewContract();
        var media = prepared with { LabelWidthMm = prepared.LabelWidthMm + 1 };
        var imageable = prepared with { PrintableWidthDots = prepared.PrintableWidthDots + 4 };

        var mediaResult = DispatchRevalidationContract.Evaluate(
            "doc", prepared, true, "doc", media, true, prepared.Fingerprint);
        var imageableResult = DispatchRevalidationContract.Evaluate(
            "doc", prepared, true, "doc", imageable, true, prepared.Fingerprint);

        Assert.Contains("media", mediaResult.ChangedFields);
        Assert.Contains("imageable-area", imageableResult.ChangedFields);
        Assert.False(mediaResult.SubmissionAllowed);
        Assert.False(imageableResult.SubmissionAllowed);
    }

    [Fact]
    public void TicketEvidenceFlipBlocksEvenWhenHashesMatch()
    {
        var prepared = NewContract();
        var result = DispatchRevalidationContract.Evaluate(
            "doc-1",
            prepared,
            preparedTicketVerified: true,
            "doc-1",
            prepared,
            finalTicketVerified: false,
            expectedOutputContractHash: prepared.Fingerprint);

        Assert.False(result.SubmissionAllowed);
        Assert.Contains("ticket-evidence", result.ChangedFields);
    }

    [Fact]
    public void FingerprintHelperBlocksOutputContractDriftWithoutSubmission()
    {
        var result = DispatchRevalidationContract.EvaluateFingerprints(
            "doc-1",
            "output-a",
            preparedTicketVerified: true,
            "doc-1",
            "output-b",
            finalTicketVerified: true,
            expectedOutputContractHash: "output-a");

        Assert.False(result.Allowed);
        Assert.False(result.SubmissionAllowed);
        Assert.Contains("output-contract-fingerprint", result.ChangedFields);
        Assert.Contains(DispatchRevalidationContract.BlockDiagnosticPrefix, result.Diagnostic, StringComparison.Ordinal);
    }

    [Fact]
    public void FingerprintHelperAllowsStablePreparedDispatch()
    {
        var result = DispatchRevalidationContract.EvaluateFingerprints(
            "doc-1",
            "output-1",
            preparedTicketVerified: true,
            "doc-1",
            "output-1",
            finalTicketVerified: true,
            expectedOutputContractHash: "output-1");

        Assert.True(result.SubmissionAllowed);
        Assert.Empty(result.ChangedFields);
    }

    [Fact]
    public void UnverifiedTicketsDoNotAuthorizeFingerprintDispatch()
    {
        Assert.False(PrintContractGuard.MatchesDispatchSnapshot(
            "doc-1", "output-1", preparedTicketVerified: false,
            "doc-1", "output-1", finalTicketVerified: false));

        var result = DispatchRevalidationContract.EvaluateFingerprints(
            "doc-1",
            "output-1",
            preparedTicketVerified: false,
            "doc-1",
            "output-1",
            finalTicketVerified: false);

        Assert.False(result.Allowed);
        Assert.False(result.SubmissionAllowed);
        Assert.StartsWith(DispatchRevalidationContract.BlockDiagnosticPrefix, result.Diagnostic, StringComparison.Ordinal);
        Assert.Contains("no label was submitted", result.Diagnostic, StringComparison.OrdinalIgnoreCase);
    }

    private static EffectiveOutputContract NewContract() => new()
    {
        PrinterName = "Industrial-Queue",
        RequestedTicketHash = "requested-a",
        EffectiveTicketHash = "effective-a",
        DpiX = 203,
        DpiY = 203,
        LabelWidthMm = 50,
        LabelHeightMm = 30,
        GapMm = 2,
        MarginMm = 1,
        OffsetXMm = 0,
        OffsetYMm = 0,
        ScaleX = 1,
        ScaleY = 1,
        MediaType = LabelMediaType.Gap,
        FeedDirection = FeedDirection.TopToBottom,
        PrintableOriginXDots = 0,
        PrintableOriginYDots = 0,
        PrintableWidthDots = 400,
        PrintableHeightDots = 240,
        PrintableOriginXDip = 0,
        PrintableOriginYDip = 0,
        PrintableWidthDip = 189,
        PrintableHeightDip = 113,
        PrintableAreaVerified = true
    };
}
