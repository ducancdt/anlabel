using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace ANLAbel.Core.Printing;

/// <summary>
/// A physical-output evidence source. Queue completion is intentionally not a
/// member: the Windows spooler cannot prove that media was marked or decoded.
/// </summary>
public enum PhysicalVerificationMethod
{
    Scanner,
    BarcodeVerifier,
    OperatorVisualInspection
}

public enum PhysicalVerificationOutcome
{
    Pass,
    Fail
}

/// <summary>
/// Hash-only evidence produced by a scanner/verifier adapter. Raw barcode data
/// and images stay outside the durable lifecycle; only their canonical digests
/// are persisted. Operator visual inspection is retained as an audit signal but
/// is never eligible to mark a job physically complete.
/// </summary>
public sealed record PhysicalOutputVerificationEvidence(
    string ContractVersion,
    string JobId,
    string ManifestFingerprint,
    PhysicalVerificationMethod Method,
    PhysicalVerificationOutcome Outcome,
    string ExpectedContentFingerprint,
    string ObservedContentFingerprint,
    string DeviceId,
    string Grade,
    DateTimeOffset VerifiedAtUtc,
    string Fingerprint)
{
    public const string CurrentContractVersion = "physical-output-verification-v1";

    public bool IsFingerprintValid => !string.IsNullOrWhiteSpace(Fingerprint)
        && string.Equals(Fingerprint, ComputeFingerprint(this), StringComparison.Ordinal);

    public bool IsEligibleForCompletion => IsFingerprintValid
        && string.Equals(ContractVersion, CurrentContractVersion, StringComparison.Ordinal)
        && Outcome == PhysicalVerificationOutcome.Pass
        && Method is PhysicalVerificationMethod.Scanner or PhysicalVerificationMethod.BarcodeVerifier
        && !string.IsNullOrWhiteSpace(JobId)
        && !string.IsNullOrWhiteSpace(ManifestFingerprint)
        && !string.IsNullOrWhiteSpace(ExpectedContentFingerprint)
        && string.Equals(ExpectedContentFingerprint, ObservedContentFingerprint, StringComparison.Ordinal)
        && !string.IsNullOrWhiteSpace(DeviceId);

    public static PhysicalOutputVerificationEvidence Create(
        string jobId,
        string manifestFingerprint,
        PhysicalVerificationMethod method,
        PhysicalVerificationOutcome outcome,
        string expectedContentFingerprint,
        string observedContentFingerprint,
        string deviceId,
        string grade = "",
        DateTimeOffset? verifiedAtUtc = null)
    {
        var normalized = new PhysicalOutputVerificationEvidence(
            CurrentContractVersion,
            Normalize(jobId),
            NormalizeFingerprint(manifestFingerprint),
            method,
            outcome,
            NormalizeFingerprint(expectedContentFingerprint),
            NormalizeFingerprint(observedContentFingerprint),
            Normalize(deviceId),
            Normalize(grade),
            (verifiedAtUtc ?? DateTimeOffset.UtcNow).ToUniversalTime(),
            string.Empty);
        return normalized with { Fingerprint = ComputeFingerprint(normalized) };
    }

    public static PhysicalVerificationValidation Validate(
        PrintJobManifest? manifest,
        string jobId,
        PhysicalOutputVerificationEvidence? evidence)
    {
        if (manifest is null || !manifest.IsFingerprintValid)
        {
            return PhysicalVerificationValidation.Fail("manifest-invalid", "A valid print manifest is required before physical verification.");
        }

        if (evidence is null || !evidence.IsFingerprintValid)
        {
            return PhysicalVerificationValidation.Fail("evidence-invalid", "The scanner/verifier evidence is missing or its fingerprint is invalid.");
        }

        if (!string.Equals(evidence.ContractVersion, CurrentContractVersion, StringComparison.Ordinal)
            || !string.Equals(evidence.JobId, Normalize(jobId), StringComparison.Ordinal))
        {
            return PhysicalVerificationValidation.Fail("identity-mismatch", "The physical evidence does not belong to this print job.");
        }

        if (!string.Equals(evidence.ManifestFingerprint, manifest.Fingerprint, StringComparison.Ordinal))
        {
            return PhysicalVerificationValidation.Fail("manifest-mismatch", "The scanned label was not verified against the reviewed print manifest.");
        }

        if (evidence.Method == PhysicalVerificationMethod.OperatorVisualInspection)
        {
            return PhysicalVerificationValidation.Fail("visual-only", "Visual inspection is recorded for audit but cannot mark physical completion.");
        }

        if (evidence.Outcome != PhysicalVerificationOutcome.Pass)
        {
            return PhysicalVerificationValidation.Fail("verification-failed", "The scanner/verifier reported a failed physical check.");
        }

        if (string.IsNullOrWhiteSpace(evidence.ExpectedContentFingerprint)
            || !string.Equals(evidence.ExpectedContentFingerprint, evidence.ObservedContentFingerprint, StringComparison.Ordinal))
        {
            return PhysicalVerificationValidation.Fail("content-mismatch", "The observed payload does not match the expected label content.");
        }

        if (string.IsNullOrWhiteSpace(evidence.DeviceId))
        {
            return PhysicalVerificationValidation.Fail("device-identity-missing", "A scanner/verifier device identity is required for physical evidence.");
        }

        return PhysicalVerificationValidation.Pass(evidence);
    }

    private static string ComputeFingerprint(PhysicalOutputVerificationEvidence evidence)
    {
        var canonical = string.Join('|', new[]
        {
            evidence.ContractVersion,
            evidence.JobId,
            evidence.ManifestFingerprint,
            evidence.Method.ToString(),
            evidence.Outcome.ToString(),
            evidence.ExpectedContentFingerprint,
            evidence.ObservedContentFingerprint,
            evidence.DeviceId,
            evidence.Grade,
            evidence.VerifiedAtUtc.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture)
        });
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }

    private static string Normalize(string? value)
        => (value ?? string.Empty).Normalize(NormalizationForm.FormC).Trim();

    private static string NormalizeFingerprint(string? value)
        => Normalize(value).ToUpperInvariant();
}

public sealed record PhysicalVerificationValidation(
    bool IsAccepted,
    string Code,
    string Message,
    PhysicalOutputVerificationEvidence? Evidence)
{
    public static PhysicalVerificationValidation Pass(PhysicalOutputVerificationEvidence evidence)
        => new(true, "accepted", "Physical output was verified against the reviewed manifest.", evidence);

    public static PhysicalVerificationValidation Fail(string code, string message)
        => new(false, code, message, null);
}
