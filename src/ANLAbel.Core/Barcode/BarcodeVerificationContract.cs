using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using ANLAbel.Core.Enums;

namespace ANLAbel.Core.Barcode;

/// <summary>
/// Grade scales emitted by production barcode verifiers. Vendor-specific
/// scales are intentionally not represented: an unknown scale cannot satisfy
/// a release gate without an explicit adapter policy.
/// </summary>
public enum BarcodeVerificationGradeScale
{
    Ansi,
    Iso15415,
    Iso15416
}

/// <summary>
/// Immutable expectation passed to a barcode-verifier adapter. The payload is
/// never persisted; only its canonical fingerprint and the minimum accepted
/// grade are carried across the hardware boundary.
/// </summary>
public sealed record BarcodeVerificationExpectation(
    string ContractVersion,
    BarcodeSymbology Symbology,
    BarcodeApplicationProfile ApplicationProfile,
    BarcodeVerificationGradeScale GradeScale,
    string MinimumGrade,
    string ExpectedContentFingerprint,
    string Fingerprint)
{
    public const string CurrentContractVersion = "barcode-verification-v1";

    public bool IsValid => string.Equals(ContractVersion, CurrentContractVersion, StringComparison.Ordinal)
        && Enum.IsDefined(Symbology)
        && Enum.IsDefined(ApplicationProfile)
        && Enum.IsDefined(GradeScale)
        && BarcodeVerificationContract.IsGradeValid(GradeScale, MinimumGrade)
        && BarcodeVerificationContract.IsFingerprint(ExpectedContentFingerprint)
        && !string.IsNullOrWhiteSpace(Fingerprint)
        && string.Equals(Fingerprint, BarcodeVerificationContract.ComputeExpectationFingerprint(this), StringComparison.Ordinal);
}

/// <summary>
/// Shared barcode-content and grade policy. It is deliberately independent of
/// a scanner SDK so the same expected digest and threshold are used by preview,
/// preflight, a future adapter and the durable physical-output gate.
/// </summary>
public static class BarcodeVerificationContract
{
    public const string ContentAlgorithmRevision = "barcode-content-v1";

    public static BarcodeVerificationExpectation CreateExpectation(
        BarcodeSymbology symbology,
        BarcodeApplicationProfile applicationProfile,
        string payload,
        BarcodeVerificationGradeScale gradeScale,
        string minimumGrade)
    {
        if (!TryCreateExpectation(
                symbology,
                applicationProfile,
                payload,
                gradeScale,
                minimumGrade,
                out var expectation,
                out var error))
        {
            throw new ArgumentException(error, nameof(payload));
        }

        return expectation;
    }

    public static bool TryCreateExpectation(
        BarcodeSymbology symbology,
        BarcodeApplicationProfile applicationProfile,
        string? payload,
        BarcodeVerificationGradeScale gradeScale,
        string? minimumGrade,
        out BarcodeVerificationExpectation expectation,
        out string error)
    {
        expectation = new BarcodeVerificationExpectation(
            BarcodeVerificationExpectation.CurrentContractVersion,
            symbology,
            applicationProfile,
            gradeScale,
            string.Empty,
            string.Empty,
            string.Empty);
        error = string.Empty;

        if (!Enum.IsDefined(symbology) || !Enum.IsDefined(applicationProfile) || !Enum.IsDefined(gradeScale))
        {
            error = "Barcode symbology, application profile and grade scale must be supported values.";
            return false;
        }

        if (!TryNormalizePayload(applicationProfile, payload, out var normalizedPayload, out error))
        {
            return false;
        }

        if (!TryNormalizeGrade(gradeScale, minimumGrade, out var normalizedGrade, out error))
        {
            return false;
        }

        var contentFingerprint = ComputeContentFingerprint(symbology, applicationProfile, normalizedPayload);
        var normalized = new BarcodeVerificationExpectation(
            BarcodeVerificationExpectation.CurrentContractVersion,
            symbology,
            applicationProfile,
            gradeScale,
            normalizedGrade,
            contentFingerprint,
            string.Empty);
        expectation = normalized with { Fingerprint = ComputeExpectationFingerprint(normalized) };
        return true;
    }

    public static bool TryNormalizePayload(
        BarcodeApplicationProfile applicationProfile,
        string? payload,
        out string normalized,
        out string error)
    {
        normalized = string.Empty;
        error = string.Empty;
        if (string.IsNullOrWhiteSpace(payload))
        {
            error = "Barcode payload is empty.";
            return false;
        }

        if (payload.Contains('\0'))
        {
            error = "Barcode payload contains a NUL character.";
            return false;
        }

        if (applicationProfile == BarcodeApplicationProfile.Gs1)
        {
            if (!BarcodeApplicationContract.TryNormalizeGs1Data(payload, out normalized, out var errors))
            {
                error = string.Join(" ", errors);
                return false;
            }

            return true;
        }

        normalized = payload.Normalize(NormalizationForm.FormC);
        return true;
    }

    public static BarcodeVerificationValidation ValidateObserved(
        BarcodeVerificationExpectation? expectation,
        string? expectedContentFingerprint,
        string? observedContentFingerprint,
        string? observedGrade)
    {
        if (expectation is null || !expectation.IsValid)
        {
            return BarcodeVerificationValidation.Fail(
                "expectation-invalid",
                "The barcode-verifier expectation is missing or tampered.");
        }

        if (!string.Equals(expectedContentFingerprint, expectation.ExpectedContentFingerprint, StringComparison.Ordinal))
        {
            return BarcodeVerificationValidation.Fail(
                "expected-content-mismatch",
                "The adapter did not verify the content fingerprint from the reviewed barcode expectation.");
        }

        if (!string.Equals(observedContentFingerprint, expectation.ExpectedContentFingerprint, StringComparison.Ordinal))
        {
            return BarcodeVerificationValidation.Fail(
                "payload-mismatch",
                "The verifier decoded a barcode payload different from the reviewed content.");
        }

        if (!TryNormalizeGrade(expectation.GradeScale, observedGrade, out var normalizedGrade, out _))
        {
            return BarcodeVerificationValidation.Fail(
                "grade-invalid",
                "The verifier grade is missing or is not valid for the requested grade scale.");
        }

        if (!MeetsMinimumGrade(expectation.GradeScale, normalizedGrade, expectation.MinimumGrade))
        {
            return BarcodeVerificationValidation.Fail(
                "grade-below-threshold",
                $"The verifier grade {normalizedGrade} is below the required {expectation.MinimumGrade} threshold.");
        }

        return BarcodeVerificationValidation.Pass();
    }

    public static string ComputeContentFingerprint(
        BarcodeSymbology symbology,
        BarcodeApplicationProfile applicationProfile,
        string normalizedPayload)
    {
        var canonical = new StringBuilder();
        AppendString(canonical, ContentAlgorithmRevision);
        AppendString(canonical, symbology.ToString());
        AppendString(canonical, applicationProfile.ToString());
        AppendString(canonical, normalizedPayload.Normalize(NormalizationForm.FormC));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical.ToString())));
    }

    internal static string ComputeExpectationFingerprint(BarcodeVerificationExpectation expectation)
    {
        var canonical = new StringBuilder();
        AppendString(canonical, expectation.ContractVersion);
        AppendString(canonical, expectation.Symbology.ToString());
        AppendString(canonical, expectation.ApplicationProfile.ToString());
        AppendString(canonical, expectation.GradeScale.ToString());
        AppendString(canonical, expectation.MinimumGrade);
        AppendString(canonical, expectation.ExpectedContentFingerprint);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical.ToString())));
    }

    internal static bool IsGradeValid(BarcodeVerificationGradeScale scale, string? grade)
        => TryNormalizeGrade(scale, grade, out _, out _);

    internal static bool IsFingerprint(string? fingerprint)
        => !string.IsNullOrWhiteSpace(fingerprint)
            && fingerprint.Length == 64
            && fingerprint.All(Uri.IsHexDigit);

    private static bool TryNormalizeGrade(
        BarcodeVerificationGradeScale scale,
        string? grade,
        out string normalized,
        out string error)
    {
        normalized = string.Empty;
        error = string.Empty;
        var value = (grade ?? string.Empty).Trim().ToUpperInvariant();
        var prefix = scale switch
        {
            BarcodeVerificationGradeScale.Ansi => "ANSI:",
            BarcodeVerificationGradeScale.Iso15415 => "ISO15415:",
            BarcodeVerificationGradeScale.Iso15416 => "ISO15416:",
            _ => string.Empty
        };

        if (value.StartsWith(prefix, StringComparison.Ordinal))
        {
            value = value[prefix.Length..];
        }
        else if (value.Contains(':'))
        {
            error = "The verifier grade scale does not match the requested policy.";
            return false;
        }

        if (scale == BarcodeVerificationGradeScale.Ansi)
        {
            if (value is not ("A" or "B" or "C" or "D" or "F"))
            {
                error = "ANSI grades must be A, B, C, D or F.";
                return false;
            }

            normalized = value;
            return true;
        }

        if (value is not ("0" or "1" or "2" or "3" or "4"))
        {
            error = "ISO grades must be an integer from 0 to 4.";
            return false;
        }

        normalized = value;
        return true;
    }

    private static bool MeetsMinimumGrade(
        BarcodeVerificationGradeScale scale,
        string observed,
        string minimum)
    {
        if (scale == BarcodeVerificationGradeScale.Ansi)
        {
            return AnsiRank(observed) >= AnsiRank(minimum);
        }

        return int.TryParse(observed, NumberStyles.None, CultureInfo.InvariantCulture, out var observedValue)
            && int.TryParse(minimum, NumberStyles.None, CultureInfo.InvariantCulture, out var minimumValue)
            && observedValue >= minimumValue;
    }

    private static int AnsiRank(string grade)
        => grade switch
        {
            "A" => 4,
            "B" => 3,
            "C" => 2,
            "D" => 1,
            "F" => 0,
            _ => -1
        };

    private static void AppendString(StringBuilder builder, string? value)
    {
        var normalized = value ?? string.Empty;
        builder.Append(Encoding.UTF8.GetByteCount(normalized).ToString(CultureInfo.InvariantCulture));
        builder.Append(':').Append(normalized).Append(';');
    }
}

public sealed record BarcodeVerificationValidation(
    bool IsAccepted,
    string Code,
    string Message)
{
    public static BarcodeVerificationValidation Pass()
        => new(true, "accepted", "Barcode content and verifier grade satisfy the production policy.");

    public static BarcodeVerificationValidation Fail(string code, string message)
        => new(false, code, message);
}
