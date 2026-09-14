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
        // A named field already covers the fingerprint drift; the catch-all must
        // not add the generic marker.
        Assert.DoesNotContain("output-contract-fingerprint", result.ChangedFields);
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
        Assert.DoesNotContain("output-contract-fingerprint", mediaResult.ChangedFields);
        Assert.DoesNotContain("output-contract-fingerprint", imageableResult.ChangedFields);
        Assert.False(mediaResult.SubmissionAllowed);
        Assert.False(imageableResult.SubmissionAllowed);
    }

    [Fact]
    public void ImageableOriginDriftIsNamed()
    {
        var prepared = NewContract();
        var final = prepared with { PrintableOriginXDots = prepared.PrintableOriginXDots + 2 };
        var result = DispatchRevalidationContract.Evaluate(
            "doc", prepared, true, "doc", final, true, prepared.Fingerprint);

        Assert.Contains("imageable-area", result.ChangedFields);
        Assert.DoesNotContain("output-contract-fingerprint", result.ChangedFields);
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

    [Fact]
    public void DocumentHashDriftBlocksWithNamedField()
    {
        var prepared = NewContract();
        var result = DispatchRevalidationContract.Evaluate(
            "doc-1", prepared, true, "doc-2", prepared, true, prepared.Fingerprint);

        Assert.False(result.SubmissionAllowed);
        Assert.Contains("document-hash", result.ChangedFields);
    }

    [Fact]
    public void MissingOutputContractBlocksAndShortCircuits()
    {
        var prepared = NewContract();

        var noFinal = DispatchRevalidationContract.Evaluate(
            "doc-1", prepared, true, "doc-1", null, true, prepared.Fingerprint);
        var noPrepared = DispatchRevalidationContract.Evaluate(
            "doc-1", null, true, "doc-1", prepared, true, prepared.Fingerprint);

        Assert.False(noFinal.SubmissionAllowed);
        Assert.Contains("output-contract-missing", noFinal.ChangedFields);
        Assert.False(noPrepared.SubmissionAllowed);
        Assert.Contains("output-contract-missing", noPrepared.ChangedFields);
    }

    [Fact]
    public void RequestedAndEffectiveTicketDriftAreNamedSeparately()
    {
        var prepared = NewContract();
        var requested = prepared with { RequestedTicketHash = "requested-b" };
        var effective = prepared with { EffectiveTicketHash = "effective-b" };

        var requestedResult = DispatchRevalidationContract.Evaluate(
            "doc", prepared, true, "doc", requested, true, prepared.Fingerprint);
        var effectiveResult = DispatchRevalidationContract.Evaluate(
            "doc", prepared, true, "doc", effective, true, prepared.Fingerprint);

        Assert.Contains("requested-ticket", requestedResult.ChangedFields);
        Assert.DoesNotContain("output-contract-fingerprint", requestedResult.ChangedFields);
        Assert.Contains("effective-ticket", effectiveResult.ChangedFields);
        Assert.DoesNotContain("output-contract-fingerprint", effectiveResult.ChangedFields);
    }

    [Fact]
    public void QueueNameDriftIsNamed()
    {
        var prepared = NewContract();
        var final = prepared with { PrinterName = "Different-Queue" };
        var result = DispatchRevalidationContract.Evaluate(
            "doc", prepared, true, "doc", final, true, prepared.Fingerprint);

        Assert.Contains("queue-name", result.ChangedFields);
        Assert.DoesNotContain("output-contract-fingerprint", result.ChangedFields);
    }

    [Fact]
    public void TicketEvidenceWithFingerprintDriftDoesNotDoubleCountFingerprint()
    {
        // Fingerprint drifts (offset) AND the ticket-evidence bit flips: the
        // catch-all must defer to the named ticket-evidence field.
        var prepared = NewContract();
        var final = prepared with { OffsetXMm = 7 };
        var result = DispatchRevalidationContract.Evaluate(
            "doc", prepared, true, "doc", final, finalTicketVerified: false, prepared.Fingerprint);

        Assert.Contains("ticket-evidence", result.ChangedFields);
        Assert.DoesNotContain("output-contract-fingerprint", result.ChangedFields);
    }

    [Fact]
    public void FingerprintOnlyDriftFallsBackToTheCatchAllField()
    {
        // OffsetX is part of the fingerprint but not any named field check, so
        // the catch-all must surface it as output-contract-fingerprint.
        var prepared = NewContract();
        var final = prepared with { OffsetXMm = 5 };
        var result = DispatchRevalidationContract.Evaluate(
            "doc", prepared, true, "doc", final, true, prepared.Fingerprint);

        Assert.False(result.SubmissionAllowed);
        Assert.Contains("output-contract-fingerprint", result.ChangedFields);
        Assert.DoesNotContain("dpi", result.ChangedFields);
        Assert.DoesNotContain("media", result.ChangedFields);
    }

    [Fact]
    public void ExpectationMismatchIsTheOnlyChangeWhenContractsAreOtherwiseStable()
    {
        var prepared = NewContract();
        var result = DispatchRevalidationContract.Evaluate(
            "doc-1", prepared, true, "doc-1", prepared, true,
            expectedOutputContractHash: "unexpected-hash");

        Assert.False(result.SubmissionAllowed);
        Assert.Contains("prepared-expectation", result.ChangedFields);
    }

    [Fact]
    public void NanMediaFieldCountsAsDrift()
    {
        var prepared = NewContract();
        var final = prepared with { LabelWidthMm = double.NaN };

        Assert.Contains("media", DispatchRevalidationContract.Evaluate(
            "doc", prepared, true, "doc", final, true, prepared.Fingerprint).ChangedFields);
    }

    [Fact]
    public void BlockedFieldsAreDistinctAndSortedAscending()
    {
        var prepared = NewContract();
        var final = prepared with { DpiX = 300, LabelWidthMm = prepared.LabelWidthMm + 1 };
        var result = DispatchRevalidationContract.Evaluate(
            "doc", prepared, true, "doc", final, true, prepared.Fingerprint);

        var fields = result.ChangedFields;
        Assert.Contains("dpi", fields);
        Assert.Contains("media", fields);
        // Ascending ordinal order: "dpi" precedes "media".
        Assert.True(
            fields.ToList().IndexOf("dpi") < fields.ToList().IndexOf("media"));
    }

    [Fact]
    public void FingerprintHelperNamesDocumentHashDrift()
    {
        var result = DispatchRevalidationContract.EvaluateFingerprints(
            "doc-1", "output-1", true, "doc-2", "output-1", true, "output-1");

        Assert.False(result.SubmissionAllowed);
        Assert.Contains("document-hash", result.ChangedFields);
    }

    [Fact]
    public void FingerprintHelperNamesMissingOutputHash()
    {
        var result = DispatchRevalidationContract.EvaluateFingerprints(
            "doc-1", "output-1", true, "doc-1", "   ", true, "output-1");

        Assert.False(result.SubmissionAllowed);
        Assert.Contains("output-contract-fingerprint", result.ChangedFields);
    }

    [Fact]
    public void FingerprintHelperNamesTicketEvidenceFlip()
    {
        var result = DispatchRevalidationContract.EvaluateFingerprints(
            "doc-1", "output-1", true, "doc-1", "output-1", false, "output-1");

        Assert.False(result.SubmissionAllowed);
        Assert.Contains("ticket-evidence", result.ChangedFields);
    }

    [Fact]
    public void FingerprintHelperNamesExpectationMismatch()
    {
        var result = DispatchRevalidationContract.EvaluateFingerprints(
            "doc-1", "output-1", true, "doc-1", "output-1", true,
            expectedOutputContractHash: "wrong");

        Assert.False(result.SubmissionAllowed);
        Assert.Contains("prepared-expectation", result.ChangedFields);
    }

    [Fact]
    public void FingerprintHelperNamesDispatchSnapshotWhenNoFieldChanged()
    {
        // Unverified tickets fail the snapshot with every explicit field equal,
        // so the residual reason is the generic dispatch-snapshot marker.
        var result = DispatchRevalidationContract.EvaluateFingerprints(
            "doc-1", "output-1", false, "doc-1", "output-1", false);

        Assert.Contains("dispatch-snapshot", result.ChangedFields);
    }

    [Fact]
    public void FingerprintHelperAllowPathReportsAllowedAndEmptyDiagnostic()
    {
        var result = DispatchRevalidationContract.EvaluateFingerprints(
            "doc-1", "output-1", true, "doc-1", "output-1", true, "output-1");

        Assert.True(result.Allowed);
        Assert.True(result.SubmissionAllowed);
        Assert.Equal(string.Empty, result.Diagnostic);
    }

    [Fact]
    public void NullPreparedDocumentHashBlocks()
    {
        var prepared = NewContract();
        var result = DispatchRevalidationContract.Evaluate(
            null, prepared, true, "doc", prepared, true, prepared.Fingerprint);

        Assert.False(result.SubmissionAllowed);
        Assert.Contains("document-hash", result.ChangedFields);
    }

    [Fact]
    public void ExpectationGateHonoursFinalTicketValidationBit()
    {
        // finalTicketVerified is false but the validation bit is true (NewContract
        // carries an effective ticket); the expectation gate must AND them, so the
        // expectation still fails closed.
        var contract = NewContract();
        var result = DispatchRevalidationContract.Evaluate(
            "doc", contract, false, "doc", contract, false,
            expectedOutputContractHash: contract.Fingerprint);

        Assert.False(result.SubmissionAllowed);
        Assert.Contains("prepared-expectation", result.ChangedFields);
    }

    [Fact]
    public void UnvalidatedTicketBlocksViaDispatchSnapshotEvenWithVerifiedFlagSet()
    {
        // Both verified flags are true but the output was never ticket-validated
        // (empty effective ticket), so the dispatch snapshot must fail closed.
        var contract = NewContract() with { EffectiveTicketHash = "" };
        var result = DispatchRevalidationContract.Evaluate(
            "doc", contract, true, "doc", contract, true,
            expectedOutputContractHash: null);

        Assert.False(result.SubmissionAllowed);
        Assert.Contains("dispatch-snapshot", result.ChangedFields);
    }

    [Fact]
    public void FingerprintHelperNamesNullPreparedDocument()
    {
        var result = DispatchRevalidationContract.EvaluateFingerprints(
            null, "output-1", true, "doc-1", "output-1", true, "output-1");

        Assert.False(result.SubmissionAllowed);
        Assert.Contains("document-hash", result.ChangedFields);
    }

    [Fact]
    public void FingerprintHelperNamesNullPreparedOutput()
    {
        var result = DispatchRevalidationContract.EvaluateFingerprints(
            "doc-1", null, true, "doc-1", "output-1", true, "output-1");

        Assert.False(result.SubmissionAllowed);
        Assert.Contains("output-contract-fingerprint", result.ChangedFields);
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
