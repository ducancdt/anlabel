using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using ANLAbel.Core.Enums;

namespace ANLAbel.Core.Printing;

/// <summary>
/// Value-only identity of the physical thermal context for which a raster
/// golden was approved. Empty/unknown driver, firmware, stock, ribbon or
/// calibration identifiers are deliberately invalid: a golden must not be
/// reused across two materially different print contexts.
/// </summary>
public sealed record ThermalRasterProfile(
    string ContractVersion,
    string QueueName,
    string DriverName,
    string DriverVersion,
    string FirmwareVersion,
    string MediaIdentity,
    string RibbonIdentity,
    string CalibrationIdentity,
    LabelMediaType MediaType,
    FeedDirection FeedDirection,
    bool Rotated180,
    int DpiX,
    int DpiY,
    double LabelWidthMm,
    double LabelHeightMm,
    double GapMm,
    double OffsetXMm,
    double OffsetYMm,
    double ScaleX,
    double ScaleY,
    string Fingerprint)
{
    public const string CurrentContractVersion = "thermal-raster-profile-v1";

    public bool IsValid => string.Equals(ContractVersion, CurrentContractVersion, StringComparison.Ordinal)
        && !string.IsNullOrWhiteSpace(QueueName)
        && !string.IsNullOrWhiteSpace(DriverName)
        && !string.IsNullOrWhiteSpace(DriverVersion)
        && !string.IsNullOrWhiteSpace(FirmwareVersion)
        && !string.IsNullOrWhiteSpace(MediaIdentity)
        && !string.IsNullOrWhiteSpace(RibbonIdentity)
        && !string.IsNullOrWhiteSpace(CalibrationIdentity)
        && Enum.IsDefined(MediaType)
        && Enum.IsDefined(FeedDirection)
        && DpiX > 0
        && DpiY > 0
        && IsPositive(LabelWidthMm)
        && IsPositive(LabelHeightMm)
        && IsFiniteNonNegative(GapMm)
        && IsFinite(OffsetXMm)
        && IsFinite(OffsetYMm)
        && IsPositive(ScaleX)
        && IsPositive(ScaleY)
        && IsFingerprintShape(Fingerprint)
        && string.Equals(Fingerprint, ThermalRasterGoldenContract.ComputeProfileFingerprint(this), StringComparison.Ordinal);

    private static bool IsFinite(double value) => double.IsFinite(value);
    private static bool IsPositive(double value) => double.IsFinite(value) && value > 0;
    private static bool IsFiniteNonNegative(double value) => double.IsFinite(value) && value >= 0;
    private static bool IsFingerprintShape(string value)
        => value.Length == 64 && value.All(Uri.IsHexDigit);
}

/// <summary>
/// Binds one exact raster golden to one thermal profile. The binding is
/// metadata/hash-only and intentionally cannot claim that the physical print
/// head reproduced the frame.
/// </summary>
public sealed record ThermalRasterGoldenBinding(
    string ContractVersion,
    string GoldenId,
    string ProfileFingerprint,
    string RasterFingerprint,
    string Fingerprint)
{
    public const string CurrentContractVersion = "thermal-raster-golden-binding-v1";

    public bool IsValid => string.Equals(ContractVersion, CurrentContractVersion, StringComparison.Ordinal)
        && !string.IsNullOrWhiteSpace(GoldenId)
        && IsFingerprintShape(ProfileFingerprint)
        && IsFingerprintShape(RasterFingerprint)
        && IsFingerprintShape(Fingerprint)
        && string.Equals(Fingerprint, ThermalRasterGoldenContract.ComputeBindingFingerprint(this), StringComparison.Ordinal);

    private static bool IsFingerprintShape(string value)
        => value.Length == 64 && value.All(Uri.IsHexDigit);
}

public sealed record ThermalRasterGoldenValidation(
    bool IsAccepted,
    string Code,
    string Message,
    ThermalRasterGoldenBinding? Binding)
{
    public static ThermalRasterGoldenValidation Pass(ThermalRasterGoldenBinding binding)
        => new(true, "accepted", "The raster golden matches the reviewed thermal profile.", binding);

    public static ThermalRasterGoldenValidation Fail(string code, string message)
        => new(false, code, message, null);
}

public static class ThermalRasterGoldenContract
{
    public static ThermalRasterProfile CreateProfile(
        string queueName,
        string driverName,
        string driverVersion,
        string firmwareVersion,
        string mediaIdentity,
        string ribbonIdentity,
        string calibrationIdentity,
        LabelMediaType mediaType,
        FeedDirection feedDirection,
        bool rotated180,
        int dpiX,
        int dpiY,
        double labelWidthMm,
        double labelHeightMm,
        double gapMm,
        double offsetXMm,
        double offsetYMm,
        double scaleX,
        double scaleY)
    {
        var profile = new ThermalRasterProfile(
            ThermalRasterProfile.CurrentContractVersion,
            NormalizeRequired(queueName),
            NormalizeRequired(driverName),
            NormalizeRequired(driverVersion),
            NormalizeRequired(firmwareVersion),
            NormalizeRequired(mediaIdentity),
            NormalizeRequired(ribbonIdentity),
            NormalizeRequired(calibrationIdentity),
            mediaType,
            feedDirection,
            rotated180,
            dpiX,
            dpiY,
            NormalizeNumber(labelWidthMm),
            NormalizeNumber(labelHeightMm),
            NormalizeNumber(gapMm),
            NormalizeNumber(offsetXMm),
            NormalizeNumber(offsetYMm),
            NormalizeNumber(scaleX),
            NormalizeNumber(scaleY),
            string.Empty);
        return profile with { Fingerprint = ComputeProfileFingerprint(profile) };
    }

    public static bool TryCreateProfile(
        string queueName,
        string driverName,
        string driverVersion,
        string firmwareVersion,
        string mediaIdentity,
        string ribbonIdentity,
        string calibrationIdentity,
        LabelMediaType mediaType,
        FeedDirection feedDirection,
        bool rotated180,
        int dpiX,
        int dpiY,
        double labelWidthMm,
        double labelHeightMm,
        double gapMm,
        double offsetXMm,
        double offsetYMm,
        double scaleX,
        double scaleY,
        out ThermalRasterProfile profile,
        out string error)
    {
        profile = CreateProfile(
            queueName,
            driverName,
            driverVersion,
            firmwareVersion,
            mediaIdentity,
            ribbonIdentity,
            calibrationIdentity,
            mediaType,
            feedDirection,
            rotated180,
            dpiX,
            dpiY,
            labelWidthMm,
            labelHeightMm,
            gapMm,
            offsetXMm,
            offsetYMm,
            scaleX,
            scaleY);
        if (profile.IsValid)
        {
            error = string.Empty;
            return true;
        }

        error = DescribeInvalidProfile(profile);
        profile = profile with { Fingerprint = string.Empty };
        return false;
    }

    public static ThermalRasterGoldenBinding CreateBinding(
        string goldenId,
        ThermalRasterProfile profile,
        RasterGoldenIdentity raster)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(raster);
        var binding = new ThermalRasterGoldenBinding(
            ThermalRasterGoldenBinding.CurrentContractVersion,
            NormalizeRequired(goldenId),
            profile.IsValid ? profile.Fingerprint : string.Empty,
            raster.IsValid ? raster.Fingerprint : string.Empty,
            string.Empty);
        return binding with { Fingerprint = ComputeBindingFingerprint(binding) };
    }

    public static bool TryCreateBinding(
        string goldenId,
        ThermalRasterProfile profile,
        RasterGoldenIdentity raster,
        out ThermalRasterGoldenBinding binding,
        out string error)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(raster);
        binding = CreateBinding(goldenId, profile, raster);
        if (!profile.IsValid)
        {
            error = "thermal-profile-invalid";
            binding = binding with { Fingerprint = string.Empty };
            return false;
        }

        if (!raster.IsValid)
        {
            error = "raster-golden-invalid";
            binding = binding with { Fingerprint = string.Empty };
            return false;
        }

        if (!binding.IsValid)
        {
            error = "binding-invalid";
            binding = binding with { Fingerprint = string.Empty };
            return false;
        }

        error = string.Empty;
        return true;
    }

    public static ThermalRasterGoldenValidation Validate(
        ThermalRasterGoldenBinding? binding,
        ThermalRasterProfile? profile,
        RasterGoldenIdentity? raster)
    {
        if (binding is null || !binding.IsValid)
        {
            return ThermalRasterGoldenValidation.Fail("binding-invalid", "The thermal raster golden binding is missing or tampered.");
        }

        if (profile is null || !profile.IsValid)
        {
            return ThermalRasterGoldenValidation.Fail("thermal-profile-invalid", "A complete queue/driver/firmware/media/ribbon/calibration profile is required.");
        }

        if (raster is null || !raster.IsValid)
        {
            return ThermalRasterGoldenValidation.Fail("raster-golden-invalid", "The exact raster frame identity is missing or invalid.");
        }

        if (!string.Equals(binding.ProfileFingerprint, profile.Fingerprint, StringComparison.Ordinal))
        {
            return ThermalRasterGoldenValidation.Fail("profile-mismatch", "The current thermal driver, firmware, media or calibration differs from the approved golden profile.");
        }

        if (!string.Equals(binding.RasterFingerprint, raster.Fingerprint, StringComparison.Ordinal))
        {
            return ThermalRasterGoldenValidation.Fail("raster-mismatch", "The current raster frame differs from the approved thermal golden.");
        }

        return ThermalRasterGoldenValidation.Pass(binding);
    }

    public static string ComputeProfileFingerprint(ThermalRasterProfile profile)
    {
        var canonical = string.Join('|', new[]
        {
            ThermalRasterProfile.CurrentContractVersion,
            NormalizeRequired(profile.QueueName),
            NormalizeRequired(profile.DriverName),
            NormalizeRequired(profile.DriverVersion),
            NormalizeRequired(profile.FirmwareVersion),
            NormalizeRequired(profile.MediaIdentity),
            NormalizeRequired(profile.RibbonIdentity),
            NormalizeRequired(profile.CalibrationIdentity),
            profile.MediaType.ToString(),
            profile.FeedDirection.ToString(),
            profile.Rotated180 ? "1" : "0",
            profile.DpiX.ToString(CultureInfo.InvariantCulture),
            profile.DpiY.ToString(CultureInfo.InvariantCulture),
            Number(profile.LabelWidthMm),
            Number(profile.LabelHeightMm),
            Number(profile.GapMm),
            Number(profile.OffsetXMm),
            Number(profile.OffsetYMm),
            Number(profile.ScaleX),
            Number(profile.ScaleY)
        });
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }

    public static string ComputeBindingFingerprint(ThermalRasterGoldenBinding binding)
    {
        var canonical = string.Join('|', new[]
        {
            ThermalRasterGoldenBinding.CurrentContractVersion,
            NormalizeRequired(binding.GoldenId),
            NormalizeRequired(binding.ProfileFingerprint).ToUpperInvariant(),
            NormalizeRequired(binding.RasterFingerprint).ToUpperInvariant()
        });
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }

    private static string DescribeInvalidProfile(ThermalRasterProfile profile)
    {
        if (string.IsNullOrWhiteSpace(profile.QueueName)
            || string.IsNullOrWhiteSpace(profile.DriverName)
            || string.IsNullOrWhiteSpace(profile.DriverVersion)
            || string.IsNullOrWhiteSpace(profile.FirmwareVersion))
        {
            return "thermal-profile-identifiers-missing";
        }

        if (string.IsNullOrWhiteSpace(profile.MediaIdentity)
            || string.IsNullOrWhiteSpace(profile.RibbonIdentity)
            || string.IsNullOrWhiteSpace(profile.CalibrationIdentity))
        {
            return "thermal-profile-material-identifiers-missing";
        }

        if (profile.DpiX <= 0 || profile.DpiY <= 0)
        {
            return "thermal-profile-dpi-invalid";
        }

        return "thermal-profile-geometry-invalid";
    }

    private static string NormalizeRequired(string? value)
        => (value ?? string.Empty).Normalize(NormalizationForm.FormC).Trim();

    private static double NormalizeNumber(double value)
        => double.IsFinite(value) ? Math.Round(value, 6, MidpointRounding.ToEven) : double.NaN;

    private static string Number(double value)
        => value.ToString("R", CultureInfo.InvariantCulture);
}
