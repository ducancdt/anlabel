using ANLAbel.Core.Barcode;

namespace ANLAbel.Core.Printing;

/// <summary>
/// Immutable request passed to a physical scanner/verifier adapter. The
/// expected label content is represented by a digest so adapters never need to
/// persist raw production data in the lifecycle log.
/// </summary>
public sealed record PhysicalOutputVerificationRequest(
    string JobId,
    PrintJobManifest Manifest,
    string ExpectedContentFingerprint,
    PhysicalVerificationMethod Method,
    string CorrelationToken = "",
    BarcodeVerificationExpectation? BarcodeExpectation = null,
    ThermalRasterProfile? ThermalProfile = null,
    RasterGoldenIdentity? RasterGolden = null,
    ThermalRasterGoldenBinding? ThermalRasterGolden = null)
{
    public bool ThermalGoldenIsValid
    {
        get
        {
            var manifestFingerprint = Manifest?.ThermalRasterGoldenFingerprint ?? string.Empty;
            var hasRequestGolden = ThermalProfile is not null || RasterGolden is not null || ThermalRasterGolden is not null;
            if (string.IsNullOrWhiteSpace(manifestFingerprint))
            {
                return !hasRequestGolden;
            }

            return hasRequestGolden
                && ThermalRasterGoldenContract.Validate(ThermalRasterGolden, ThermalProfile, RasterGolden).IsAccepted
                && string.Equals(manifestFingerprint, ThermalRasterGolden?.Fingerprint, StringComparison.Ordinal);
        }
    }

    public bool IsValid => Manifest is not null
        && Manifest.IsFingerprintValid
        && !string.IsNullOrWhiteSpace(JobId)
        && !string.IsNullOrWhiteSpace(ExpectedContentFingerprint)
        && Method is PhysicalVerificationMethod.Scanner or PhysicalVerificationMethod.BarcodeVerifier
        && (Method != PhysicalVerificationMethod.BarcodeVerifier
            || BarcodeExpectation is not null
                && BarcodeExpectation.IsValid
                && string.Equals(
                    ExpectedContentFingerprint,
                    BarcodeExpectation.ExpectedContentFingerprint,
                    StringComparison.Ordinal))
        && ThermalGoldenIsValid;
}

/// <summary>
/// Hardware-neutral adapter boundary. Production implementations may use a USB
/// scanner, camera/verifier SDK or line controller; none of those dependencies
/// leak into Core or the durable event store.
/// </summary>
public interface IPhysicalOutputVerifier
{
    ValueTask<PhysicalOutputVerificationEvidence?> VerifyAsync(
        PhysicalOutputVerificationRequest request,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Validates adapter output before a caller appends a Completed transition.
/// Adapter errors and null output become an explicit failed validation instead
/// of being interpreted as a successful print.
/// </summary>
public sealed class PhysicalOutputVerifierCoordinator
{
    private readonly IPhysicalOutputVerifier _verifier;

    public PhysicalOutputVerifierCoordinator(IPhysicalOutputVerifier verifier)
    {
        _verifier = verifier ?? throw new ArgumentNullException(nameof(verifier));
    }

    public async ValueTask<PhysicalVerificationValidation> VerifyAsync(
        PhysicalOutputVerificationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!request.ThermalGoldenIsValid)
        {
            if (request.Manifest is not null
                && !string.IsNullOrWhiteSpace(request.Manifest.ThermalRasterGoldenFingerprint))
            {
                var thermalValidation = ThermalRasterGoldenContract.Validate(
                    request.ThermalRasterGolden,
                    request.ThermalProfile,
                    request.RasterGolden);
                if (!thermalValidation.IsAccepted)
                {
                    return PhysicalVerificationValidation.Fail(thermalValidation.Code, thermalValidation.Message);
                }

                return PhysicalVerificationValidation.Fail(
                    "thermal-golden-manifest-mismatch",
                    "The manifest thermal golden fingerprint does not match the reviewed profile/frame binding.");
            }

            return PhysicalVerificationValidation.Fail(
                "thermal-golden-unbound",
                "Thermal golden context was supplied without a matching manifest fingerprint.");
        }

        if (!request.IsValid)
        {
            return PhysicalVerificationValidation.Fail(
                "request-invalid",
                "The physical-verification request is missing a valid manifest, job identity, content fingerprint or supported method.");
        }

        PhysicalOutputVerificationEvidence? evidence;
        try
        {
            evidence = await _verifier.VerifyAsync(request, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (PhysicalVerifierAdapterException ex)
        {
            return PhysicalVerificationValidation.Fail(ex.Code, ex.Message);
        }
        catch (Exception ex)
        {
            return PhysicalVerificationValidation.Fail(
                "adapter-error",
                $"The physical verifier adapter failed; output remains unverified ({ex.Message}).");
        }

        if (evidence is null)
        {
            return PhysicalVerificationValidation.Fail(
                "no-evidence",
                "The physical verifier returned no evidence; output remains unverified.");
        }

        var lifecycleValidation = PhysicalOutputVerificationEvidence.Validate(
            request.Manifest,
            request.JobId,
            evidence);
        if (!lifecycleValidation.IsAccepted || request.Method != PhysicalVerificationMethod.BarcodeVerifier)
        {
            return lifecycleValidation;
        }

        var barcodeValidation = BarcodeVerificationContract.ValidateObserved(
            request.BarcodeExpectation,
            evidence.ExpectedContentFingerprint,
            evidence.ObservedContentFingerprint,
            evidence.Grade);
        return barcodeValidation.IsAccepted
            ? lifecycleValidation
            : PhysicalVerificationValidation.Fail(barcodeValidation.Code, barcodeValidation.Message);
    }
}
