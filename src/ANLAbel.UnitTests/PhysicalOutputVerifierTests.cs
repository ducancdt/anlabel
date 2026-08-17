using ANLAbel.Core.Barcode;
using ANLAbel.Core.Enums;
using ANLAbel.Core.Printing;
using Xunit;

namespace ANLAbel.UnitTests;

public sealed class PhysicalOutputVerifierTests
{
    [Fact]
    public async Task CoordinatorAcceptsOnlyAdapterEvidenceBoundToRequest()
    {
        var manifest = CreateManifest("job-adapter");
        var request = new PhysicalOutputVerificationRequest(
            "job-adapter",
            manifest,
            "PAYLOAD-1",
            PhysicalVerificationMethod.Scanner);
        var adapter = new FakeVerifier(_ => PhysicalOutputVerificationEvidence.Create(
            "job-adapter",
            manifest.Fingerprint,
            PhysicalVerificationMethod.Scanner,
            PhysicalVerificationOutcome.Pass,
            "PAYLOAD-1",
            "PAYLOAD-1",
            "scanner-01"));

        var result = await new PhysicalOutputVerifierCoordinator(adapter).VerifyAsync(request);

        Assert.True(result.IsAccepted);
        Assert.Equal("accepted", result.Code);
        Assert.Equal(1, adapter.CallCount);
    }

    [Fact]
    public async Task NullAdapterOutputRemainsUnverified()
    {
        var manifest = CreateManifest("job-no-evidence");
        var expectation = CreateBarcodeExpectation("PAYLOAD-2");
        var request = new PhysicalOutputVerificationRequest(
            "job-no-evidence",
            manifest,
            expectation.ExpectedContentFingerprint,
            PhysicalVerificationMethod.BarcodeVerifier,
            BarcodeExpectation: expectation);

        var result = await new PhysicalOutputVerifierCoordinator(
            new FakeVerifier(_ => null)).VerifyAsync(request);

        Assert.False(result.IsAccepted);
        Assert.Equal("no-evidence", result.Code);
    }

    [Fact]
    public async Task BarcodeVerifierRejectsPayloadOutsideTheReviewedExpectation()
    {
        var manifest = CreateManifest("job-barcode-payload");
        var expectation = CreateBarcodeExpectation("PAYLOAD-EXPECTED");
        var request = new PhysicalOutputVerificationRequest(
            "job-barcode-payload",
            manifest,
            expectation.ExpectedContentFingerprint,
            PhysicalVerificationMethod.BarcodeVerifier,
            BarcodeExpectation: expectation);
        var adapter = new FakeVerifier(_ => PhysicalOutputVerificationEvidence.Create(
            "job-barcode-payload",
            manifest.Fingerprint,
            PhysicalVerificationMethod.BarcodeVerifier,
            PhysicalVerificationOutcome.Pass,
            "WRONG-CONTENT-FINGERPRINT",
            "WRONG-CONTENT-FINGERPRINT",
            "verifier-01",
            "A"));

        var result = await new PhysicalOutputVerifierCoordinator(adapter).VerifyAsync(request);

        Assert.False(result.IsAccepted);
        Assert.Equal("expected-content-mismatch", result.Code);
    }

    [Fact]
    public async Task BarcodeVerifierRejectsGradeBelowTheConfiguredThreshold()
    {
        var manifest = CreateManifest("job-barcode-grade");
        var expectation = CreateBarcodeExpectation("PAYLOAD-GRADE");
        var request = new PhysicalOutputVerificationRequest(
            "job-barcode-grade",
            manifest,
            expectation.ExpectedContentFingerprint,
            PhysicalVerificationMethod.BarcodeVerifier,
            BarcodeExpectation: expectation);
        var adapter = new FakeVerifier(_ => PhysicalOutputVerificationEvidence.Create(
            "job-barcode-grade",
            manifest.Fingerprint,
            PhysicalVerificationMethod.BarcodeVerifier,
            PhysicalVerificationOutcome.Pass,
            expectation.ExpectedContentFingerprint,
            expectation.ExpectedContentFingerprint,
            "verifier-02",
            "B"));

        var result = await new PhysicalOutputVerifierCoordinator(adapter).VerifyAsync(request);

        Assert.False(result.IsAccepted);
        Assert.Equal("grade-below-threshold", result.Code);
    }

    [Fact]
    public async Task AdapterExceptionIsFailClosedAndCancellationEscapes()
    {
        var manifest = CreateManifest("job-adapter-error");
        var request = new PhysicalOutputVerificationRequest(
            "job-adapter-error",
            manifest,
            "PAYLOAD-3",
            PhysicalVerificationMethod.Scanner);

        var failed = await new PhysicalOutputVerifierCoordinator(
            new FakeVerifier(_ => throw new InvalidOperationException("device offline"))).VerifyAsync(request);
        Assert.False(failed.IsAccepted);
        Assert.Equal("adapter-error", failed.Code);

        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new PhysicalOutputVerifierCoordinator(new FakeVerifier((_, token) =>
            {
                token.ThrowIfCancellationRequested();
                return PhysicalOutputVerificationEvidence.Create(
                    "job-adapter-error",
                    manifest.Fingerprint,
                    PhysicalVerificationMethod.Scanner,
                    PhysicalVerificationOutcome.Pass,
                    "PAYLOAD-3",
                    "PAYLOAD-3",
                    "scanner-03");
            })).VerifyAsync(request, cancellation.Token).AsTask());
    }

    [Fact]
    public async Task InvalidRequestDoesNotCallHardwareAdapter()
    {
        var adapter = new FakeVerifier(_ => throw new Xunit.Sdk.XunitException("adapter should not be called"));
        var unsupportedRequest = new PhysicalOutputVerificationRequest(
            "",
            CreateManifest("job-invalid-request"),
            "",
            PhysicalVerificationMethod.OperatorVisualInspection);

        var unsupportedResult = await new PhysicalOutputVerifierCoordinator(adapter).VerifyAsync(unsupportedRequest);

        Assert.False(unsupportedResult.IsAccepted);
        Assert.Equal("request-invalid", unsupportedResult.Code);

        var missingExpectationRequest = new PhysicalOutputVerificationRequest(
            "job-missing-barcode-expectation",
            CreateManifest("job-missing-barcode-expectation"),
            "CONTENT-FINGERPRINT",
            PhysicalVerificationMethod.BarcodeVerifier);
        var missingExpectationResult = await new PhysicalOutputVerifierCoordinator(adapter).VerifyAsync(missingExpectationRequest);

        Assert.False(missingExpectationResult.IsAccepted);
        Assert.Equal("request-invalid", missingExpectationResult.Code);
        Assert.Equal(0, adapter.CallCount);
    }

    private static PrintJobManifest CreateManifest(string jobId)
    {
        return PrintJobManifest.Create(
            "Adapter label",
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

    private static BarcodeVerificationExpectation CreateBarcodeExpectation(string payload)
        => BarcodeVerificationContract.CreateExpectation(
            BarcodeSymbology.Code128,
            BarcodeApplicationProfile.General,
            payload,
            BarcodeVerificationGradeScale.Ansi,
            "A");

    private sealed class FakeVerifier : IPhysicalOutputVerifier
    {
        private readonly Func<PhysicalOutputVerificationRequest, CancellationToken, PhysicalOutputVerificationEvidence?> _handler;

        public FakeVerifier(Func<PhysicalOutputVerificationRequest, PhysicalOutputVerificationEvidence?> handler)
            : this((request, _) => handler(request))
        {
        }

        public FakeVerifier(Func<PhysicalOutputVerificationRequest, CancellationToken, PhysicalOutputVerificationEvidence?> handler)
        {
            _handler = handler;
        }

        public int CallCount { get; private set; }

        public ValueTask<PhysicalOutputVerificationEvidence?> VerifyAsync(
            PhysicalOutputVerificationRequest request,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(_handler(request, cancellationToken));
        }
    }
}
