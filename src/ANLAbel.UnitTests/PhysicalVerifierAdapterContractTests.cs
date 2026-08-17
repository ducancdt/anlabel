using ANLAbel.Core.Barcode;
using ANLAbel.Core.Enums;
using ANLAbel.Core.Printing;
using Xunit;

namespace ANLAbel.UnitTests;

public sealed class PhysicalVerifierAdapterContractTests
{
    [Fact]
    public void ScannerObservationMapsToHashOnlyEvidenceWithAdapterDeviceIdentity()
    {
        var manifest = CreateManifest("job-map");
        var request = new PhysicalOutputVerificationRequest(
            "job-map",
            manifest,
            "PAYLOAD-1",
            PhysicalVerificationMethod.Scanner,
            CorrelationToken: "corr-1");
        var observation = new PhysicalVerifierAdapterObservation(
            "Vendor.Scanner",
            "1.4",
            "corr-1",
            PhysicalVerificationMethod.Scanner,
            PhysicalVerificationOutcome.Pass,
            "PAYLOAD-1",
            "scan-01",
            "",
            DateTimeOffset.UtcNow);

        var result = PhysicalVerifierAdapterContract.Map(request, observation);

        Assert.True(result.IsAccepted);
        Assert.Equal(manifest.Fingerprint, result.Evidence?.ManifestFingerprint);
        Assert.Equal("Vendor.Scanner@1.4/scan-01", result.Evidence?.DeviceId);
        Assert.True(result.Evidence?.IsFingerprintValid);
    }

    [Fact]
    public void CorrelationMismatchIsRejectedBeforeEvidenceCreation()
    {
        var request = new PhysicalOutputVerificationRequest(
            "job-correlation",
            CreateManifest("job-correlation"),
            "PAYLOAD",
            PhysicalVerificationMethod.Scanner,
            CorrelationToken: "expected");
        var observation = new PhysicalVerifierAdapterObservation(
            "scanner",
            "1",
            "different",
            PhysicalVerificationMethod.Scanner,
            PhysicalVerificationOutcome.Pass,
            "PAYLOAD",
            "device",
            "",
            DateTimeOffset.UtcNow);

        var result = PhysicalVerifierAdapterContract.Map(request, observation);

        Assert.False(result.IsAccepted);
        Assert.Equal("correlation-mismatch", result.Code);
    }

    [Fact]
    public void BarcodeObservationRequiresSha256ContentFingerprint()
    {
        var expectation = BarcodeVerificationContract.CreateExpectation(
            BarcodeSymbology.Code128,
            BarcodeApplicationProfile.General,
            "ABC-123",
            BarcodeVerificationGradeScale.Ansi,
            "A");
        var request = new PhysicalOutputVerificationRequest(
            "job-barcode-map",
            CreateManifest("job-barcode-map"),
            expectation.ExpectedContentFingerprint,
            PhysicalVerificationMethod.BarcodeVerifier,
            BarcodeExpectation: expectation);
        var observation = new PhysicalVerifierAdapterObservation(
            "vendor-verifier",
            "2",
            "",
            PhysicalVerificationMethod.BarcodeVerifier,
            PhysicalVerificationOutcome.Pass,
            "not-a-digest",
            "verifier-01",
            "A",
            DateTimeOffset.UtcNow);

        var result = PhysicalVerifierAdapterContract.Map(request, observation);

        Assert.False(result.IsAccepted);
        Assert.Equal("observed-content-invalid", result.Code);
    }

    [Fact]
    public async Task BarcodeObservationMapsAndCoordinatorValidatesGrade()
    {
        var expectation = BarcodeVerificationContract.CreateExpectation(
            BarcodeSymbology.Code128,
            BarcodeApplicationProfile.General,
            "ABC-123",
            BarcodeVerificationGradeScale.Ansi,
            "A");
        var manifest = CreateManifest("job-barcode-valid");
        var request = new PhysicalOutputVerificationRequest(
            "job-barcode-valid",
            manifest,
            expectation.ExpectedContentFingerprint,
            PhysicalVerificationMethod.BarcodeVerifier,
            BarcodeExpectation: expectation);
        var observation = new PhysicalVerifierAdapterObservation(
            "vendor-verifier",
            "2",
            "",
            PhysicalVerificationMethod.BarcodeVerifier,
            PhysicalVerificationOutcome.Pass,
            expectation.ExpectedContentFingerprint,
            "verifier-01",
            "A",
            DateTimeOffset.UtcNow);
        var mapped = PhysicalVerifierAdapterContract.Map(request, observation);

        Assert.True(mapped.IsAccepted);
        var coordinator = new PhysicalOutputVerifierCoordinator(
            new PhysicalVerifierAdapter(new ObservationAdapter(observation)));
        var result = await coordinator.VerifyAsync(request);

        Assert.True(result.IsAccepted);
        Assert.Equal("accepted", result.Code);
    }

    [Fact]
    public void ThermalGoldenManifestRequiresMatchingRequestContext()
    {
        var thermal = CreateThermalBinding();
        var manifest = CreateManifest("job-thermal", thermal.Binding.Fingerprint);
        var request = new PhysicalOutputVerificationRequest(
            "job-thermal",
            manifest,
            "PAYLOAD",
            PhysicalVerificationMethod.Scanner,
            ThermalProfile: thermal.Profile,
            RasterGolden: thermal.Raster,
            ThermalRasterGolden: thermal.Binding);

        Assert.True(request.ThermalGoldenIsValid);
        Assert.True(request.IsValid);
    }

    [Fact]
    public async Task ThermalGoldenDriftStopsCoordinatorBeforeAdapterCall()
    {
        var thermal = CreateThermalBinding();
        var manifest = CreateManifest("job-thermal-drift", thermal.Binding.Fingerprint);
        var request = new PhysicalOutputVerificationRequest(
            "job-thermal-drift",
            manifest,
            "PAYLOAD",
            PhysicalVerificationMethod.Scanner,
            ThermalProfile: CreateThermalBinding("CAL-DRIFT").Profile,
            RasterGolden: thermal.Raster,
            ThermalRasterGolden: thermal.Binding);
        var adapter = new CountingVerifier();

        var result = await new PhysicalOutputVerifierCoordinator(adapter).VerifyAsync(request);

        Assert.False(result.IsAccepted);
        Assert.Equal("profile-mismatch", result.Code);
        Assert.Equal(0, adapter.CallCount);
    }

    [Fact]
    public async Task AdapterTimeoutIsReturnedAsExplicitFailureCode()
    {
        var request = CreateScannerRequest("job-timeout");
        var adapter = new CancellationAwareDelayAdapter();
        var verifier = new PhysicalVerifierAdapter(
            adapter,
            new PhysicalVerifierAdapterOptions { Timeout = TimeSpan.FromMilliseconds(40) });

        var result = await new PhysicalOutputVerifierCoordinator(verifier).VerifyAsync(request);

        Assert.False(result.IsAccepted);
        Assert.Equal("adapter-timeout", result.Code);
        Assert.Equal(1, adapter.CallCount);
    }

    [Fact]
    public async Task CallerCancellationRemainsCancellationInsteadOfTimeoutFailure()
    {
        var request = CreateScannerRequest("job-cancel");
        var adapter = new CancellationAwareDelayAdapter();
        var verifier = new PhysicalVerifierAdapter(
            adapter,
            new PhysicalVerifierAdapterOptions { Timeout = TimeSpan.FromSeconds(2) });
        using var cancellation = new CancellationTokenSource();

        var verification = new PhysicalOutputVerifierCoordinator(verifier)
            .VerifyAsync(request, cancellation.Token)
            .AsTask();
        await adapter.Started.Task;
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await verification);
    }

    [Fact]
    public async Task ConcurrentObservationIsRejectedUntilTheFirstCallFinishes()
    {
        var request = CreateScannerRequest("job-busy");
        var adapter = new BlockingObservationAdapter();
        var verifier = new PhysicalVerifierAdapter(
            adapter,
            new PhysicalVerifierAdapterOptions { Timeout = TimeSpan.FromSeconds(2) });
        var coordinator = new PhysicalOutputVerifierCoordinator(verifier);

        var first = coordinator.VerifyAsync(request).AsTask();
        await adapter.Started.Task;

        var second = await coordinator.VerifyAsync(request);

        Assert.False(second.IsAccepted);
        Assert.Equal("adapter-busy", second.Code);

        adapter.Release.TrySetResult(true);
        var firstResult = await first;
        Assert.True(firstResult.IsAccepted);
        Assert.Equal("accepted", firstResult.Code);
    }

    private static PrintJobManifest CreateManifest(string jobId, string thermalGoldenFingerprint = "")
        => PrintJobManifest.Create(
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
            outputContractHash: "contract",
            thermalRasterGoldenFingerprint: thermalGoldenFingerprint);

    private static PhysicalOutputVerificationRequest CreateScannerRequest(string jobId)
        => new(
            jobId,
            CreateManifest(jobId),
            "PAYLOAD",
            PhysicalVerificationMethod.Scanner);

    private static (ThermalRasterProfile Profile, RasterGoldenIdentity Raster, ThermalRasterGoldenBinding Binding) CreateThermalBinding(string calibrationIdentity = "CAL-1")
    {
        var profile = ThermalRasterGoldenContract.CreateProfile(
            "Zebra Test",
            "Driver",
            "1",
            "FW-1",
            "STOCK-A",
            "RIBBON-NONE",
            calibrationIdentity,
            LabelMediaType.Gap,
            FeedDirection.TopToBottom,
            false,
            203,
            203,
            100,
            50,
            2,
            0,
            0,
            1,
            1);
        var raster = RasterGoldenContract.Describe(4, 2, 203, 203, 16, "Pbgra32", new byte[32]);
        return (profile, raster, ThermalRasterGoldenContract.CreateBinding("golden-1", profile, raster));
    }

    private sealed class ObservationAdapter : IPhysicalVerifierPayloadAdapter
    {
        private readonly PhysicalVerifierAdapterObservation _observation;

        public ObservationAdapter(PhysicalVerifierAdapterObservation observation)
        {
            _observation = observation;
        }

        public ValueTask<PhysicalVerifierAdapterObservation?> ObserveAsync(
            PhysicalOutputVerificationRequest request,
            CancellationToken cancellationToken = default)
            => ValueTask.FromResult<PhysicalVerifierAdapterObservation?>(_observation);
    }

    private sealed class CancellationAwareDelayAdapter : IPhysicalVerifierPayloadAdapter
    {
        public TaskCompletionSource<bool> Started { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int CallCount { get; private set; }

        public async ValueTask<PhysicalVerifierAdapterObservation?> ObserveAsync(
            PhysicalOutputVerificationRequest request,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            Started.TrySetResult(true);
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return null;
        }
    }

    private sealed class BlockingObservationAdapter : IPhysicalVerifierPayloadAdapter
    {
        public TaskCompletionSource<bool> Started { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource<bool> Release { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async ValueTask<PhysicalVerifierAdapterObservation?> ObserveAsync(
            PhysicalOutputVerificationRequest request,
            CancellationToken cancellationToken = default)
        {
            Started.TrySetResult(true);
            await Release.Task.WaitAsync(cancellationToken);
            return new PhysicalVerifierAdapterObservation(
                "blocking-adapter",
                "1",
                "",
                PhysicalVerificationMethod.Scanner,
                PhysicalVerificationOutcome.Pass,
                request.ExpectedContentFingerprint,
                "device-1",
                "",
                DateTimeOffset.UtcNow);
        }
    }

    private sealed class CountingVerifier : IPhysicalOutputVerifier
    {
        public int CallCount { get; private set; }

        public ValueTask<PhysicalOutputVerificationEvidence?> VerifyAsync(
            PhysicalOutputVerificationRequest request,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            return ValueTask.FromResult<PhysicalOutputVerificationEvidence?>(null);
        }
    }
}
