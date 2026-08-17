using ANLAbel.Core.Printing;
using Xunit;

namespace ANLAbel.UnitTests;

public sealed class PhysicalOutputVerificationTests
{
    [Fact]
    public void ScannerPassMustMatchManifestAndContent()
    {
        var manifest = CreateManifest("job-verifier");
        var evidence = PhysicalOutputVerificationEvidence.Create(
            "job-verifier",
            manifest.Fingerprint,
            PhysicalVerificationMethod.Scanner,
            PhysicalVerificationOutcome.Pass,
            expectedContentFingerprint: "payload-abc",
            observedContentFingerprint: "payload-abc",
            deviceId: "scanner-01");

        var result = PhysicalOutputVerificationEvidence.Validate(manifest, "job-verifier", evidence);

        Assert.True(result.IsAccepted);
        Assert.True(evidence.IsEligibleForCompletion);
        Assert.Equal("accepted", result.Code);
    }

    [Fact]
    public void ManifestMismatchCannotCompleteTheJob()
    {
        var manifest = CreateManifest("job-verifier");
        var otherManifest = CreateManifest("other-job");
        var evidence = PhysicalOutputVerificationEvidence.Create(
            "job-verifier",
            otherManifest.Fingerprint,
            PhysicalVerificationMethod.BarcodeVerifier,
            PhysicalVerificationOutcome.Pass,
            "payload-abc",
            "payload-abc",
            "verifier-01",
            grade: "A");

        var result = PhysicalOutputVerificationEvidence.Validate(manifest, "job-verifier", evidence);

        Assert.False(result.IsAccepted);
        Assert.Equal("manifest-mismatch", result.Code);
    }

    [Fact]
    public void ContentMismatchIsRejectedEvenWhenDeviceReportsPass()
    {
        var manifest = CreateManifest("job-content");
        var evidence = PhysicalOutputVerificationEvidence.Create(
            "job-content",
            manifest.Fingerprint,
            PhysicalVerificationMethod.Scanner,
            PhysicalVerificationOutcome.Pass,
            "payload-expected",
            "payload-observed",
            "scanner-02");

        var result = PhysicalOutputVerificationEvidence.Validate(manifest, "job-content", evidence);

        Assert.False(result.IsAccepted);
        Assert.Equal("content-mismatch", result.Code);
    }

    [Fact]
    public void VisualInspectionIsAuditOnly()
    {
        var manifest = CreateManifest("job-visual");
        var evidence = PhysicalOutputVerificationEvidence.Create(
            "job-visual",
            manifest.Fingerprint,
            PhysicalVerificationMethod.OperatorVisualInspection,
            PhysicalVerificationOutcome.Pass,
            "payload",
            "payload",
            "operator-screen");

        var result = PhysicalOutputVerificationEvidence.Validate(manifest, "job-visual", evidence);

        Assert.False(result.IsAccepted);
        Assert.Equal("visual-only", result.Code);
        Assert.False(evidence.IsEligibleForCompletion);
    }

    [Fact]
    public void TamperedEvidenceFingerprintFailsClosed()
    {
        var manifest = CreateManifest("job-tamper");
        var evidence = PhysicalOutputVerificationEvidence.Create(
            "job-tamper",
            manifest.Fingerprint,
            PhysicalVerificationMethod.Scanner,
            PhysicalVerificationOutcome.Pass,
            "payload",
            "payload",
            "scanner-03") with { DeviceId = "attacker" };

        var result = PhysicalOutputVerificationEvidence.Validate(manifest, "job-tamper", evidence);

        Assert.False(evidence.IsFingerprintValid);
        Assert.False(result.IsAccepted);
        Assert.Equal("evidence-invalid", result.Code);
    }

    [Fact]
    public void LifecycleCompletedRequiresAcceptedEvidence()
    {
        var manifest = CreateManifest("job-lifecycle");
        var invalid = new PrintJobStateTransition(
            "job-lifecycle",
            PrintJobLifecycleState.QueueObserved,
            PrintJobLifecycleState.Completed,
            DateTimeOffset.UtcNow,
            "queue completed",
            PrinterName: "Zebra Test",
            ManifestFingerprint: manifest.Fingerprint,
            Manifest: manifest,
            PhysicalOutputVerified: true);

        Assert.Throws<InvalidOperationException>(() =>
            PrintJobStateMachine.ValidateTransition(invalid, PrintJobLifecycleState.QueueObserved));

        var evidence = PhysicalOutputVerificationEvidence.Create(
            "job-lifecycle",
            manifest.Fingerprint,
            PhysicalVerificationMethod.Scanner,
            PhysicalVerificationOutcome.Pass,
            "payload",
            "payload",
            "scanner-lifecycle");
        var valid = invalid with { VerificationEvidence = evidence };

        PrintJobStateMachine.ValidateTransition(valid, PrintJobLifecycleState.QueueObserved);
    }

    private static PrintJobManifest CreateManifest(string jobId)
    {
        return PrintJobManifest.Create(
            "Verifier label",
            $"{jobId}.anlabel",
            "Print",
            "Zebra Test",
            100,
            50,
            203,
            203,
            1,
            1,
            new IReadOnlyDictionary<string, string>?[]
            {
                new Dictionary<string, string> { ["PartNo"] = jobId }
            },
            documentHash: "doc",
            textResourceFingerprint: "text",
            sceneHash: "scene",
            outputContractHash: "contract");
    }
}
