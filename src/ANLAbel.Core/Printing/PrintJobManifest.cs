using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using ANLAbel.Core.Automation;

namespace ANLAbel.Core.Printing;

/// <summary>
/// Immutable identity of the inputs that were approved for one print dispatch.
/// It deliberately stores metadata and hashes only; raw label values never enter
/// the durable lifecycle or operation logs.
/// </summary>
public sealed record PrintJobManifest(
    string ContractVersion,
    string TemplateName,
    string TemplateFilePath,
    string PrintMode,
    string PrinterName,
    double LabelWidthMm,
    double LabelHeightMm,
    int DpiX,
    int DpiY,
    int LabelCount,
    int SourceRowCount,
    string RowsFingerprint,
    string DocumentHash,
    string TextResourceFingerprint,
    string SceneHash,
    string OutputContractHash,
    string Fingerprint)
{
    public const string CurrentContractVersion = "print-manifest-v3";
    public const string PreviousContractVersion = "print-manifest-v2";
    public const string LegacyContractVersion = "print-manifest-v1";
    public string ImageRasterFingerprint { get; init; } = string.Empty;
    public string PrintMethod { get; init; } = "ApplicationGraphic";
    public bool NativeCommandsUsed { get; init; }
    /// <summary>
    /// Optional fingerprint of the thermal driver/firmware/media/calibration
    /// golden bound to this dispatch. Empty explicitly means no thermal golden
    /// was approved; it is not physical-output verification.
    /// </summary>
    public string ThermalRasterGoldenFingerprint { get; init; } = string.Empty;
    /// <summary>
    /// Optional local file-drop provenance. These are opaque stable identities,
    /// never source paths or record payloads, and are empty for interactive jobs.
    /// </summary>
    public string AutomationEventId { get; init; } = string.Empty;
    public string AutomationTriggerId { get; init; } = string.Empty;
    public string AutomationConfigurationFingerprint { get; init; } = string.Empty;
    public string AutomationSourceFingerprint { get; init; } = string.Empty;
    public string AutomationPreparedBatchId { get; init; } = string.Empty;

    /// <summary>
    /// Verifies that the stored fingerprint still matches every immutable
    /// metadata field. This catches tampered JSON metadata, not just a changed
    /// fingerprint string.
    /// </summary>
    public bool IsFingerprintValid => !string.IsNullOrWhiteSpace(Fingerprint)
        && ((string.Equals(ContractVersion, CurrentContractVersion, StringComparison.Ordinal)
                && string.Equals(Fingerprint, ComputeManifestFingerprint(this), StringComparison.Ordinal))
            || (string.Equals(ContractVersion, PreviousContractVersion, StringComparison.Ordinal)
                && HasNoAutomationProvenance(this)
                && string.Equals(Fingerprint, ComputePreviousManifestFingerprint(this), StringComparison.Ordinal))
            || (string.Equals(ContractVersion, LegacyContractVersion, StringComparison.Ordinal)
                && string.IsNullOrWhiteSpace(ThermalRasterGoldenFingerprint)
                && HasNoAutomationProvenance(this)
                && string.Equals(Fingerprint, ComputeLegacyManifestFingerprint(this), StringComparison.Ordinal)));

    /// <summary>
    /// Computes the v1 fingerprint for a legacy manifest read from disk. New
    /// manifests must use <see cref="Create"/>; this helper exists only for
    /// migration/replay validation of the previous schema.
    /// </summary>
    public static string ComputeLegacyFingerprint(PrintJobManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        return ComputeLegacyManifestFingerprint(manifest);
    }

    /// <summary>Computes the v2 fingerprint for replay validation of manifests written before automation provenance existed.</summary>
    public static string ComputePreviousFingerprint(PrintJobManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        return ComputePreviousManifestFingerprint(manifest);
    }

    /// <summary>
    /// Creates a manifest from the immutable design and selected rows. The rows
    /// are enumerated once and reduced to a canonical digest; no row dictionary
    /// is retained by the resulting value.
    /// </summary>
    public static PrintJobManifest Create(
        string templateName,
        string templateFilePath,
        string printMode,
        string printerName,
        double labelWidthMm,
        double labelHeightMm,
        int dpiX,
        int dpiY,
        int labelCount,
        int sourceRowCount,
        IEnumerable<IReadOnlyDictionary<string, string>?>? rows,
        string documentHash = "",
        string textResourceFingerprint = "",
        string sceneHash = "",
        string outputContractHash = "",
        string imageRasterFingerprint = "",
        string thermalRasterGoldenFingerprint = "",
        FileDropPreparedBatchIdentity? automationBatch = null,
        string printMethod = "ApplicationGraphic",
        bool nativeCommandsUsed = false)
    {
        var rowList = rows?.ToArray() ?? Array.Empty<IReadOnlyDictionary<string, string>?>();
        var normalized = new PrintJobManifest(
            CurrentContractVersion,
            NormalizeText(templateName),
            NormalizePath(templateFilePath),
            NormalizeText(printMode),
            NormalizeText(printerName),
            NormalizeDimension(labelWidthMm),
            NormalizeDimension(labelHeightMm),
            Math.Max(0, dpiX),
            Math.Max(0, dpiY),
            Math.Max(0, labelCount),
            Math.Max(0, sourceRowCount),
            ComputeRowsFingerprint(rowList),
            NormalizeFingerprint(documentHash),
            NormalizeFingerprint(textResourceFingerprint),
            NormalizeFingerprint(sceneHash),
            NormalizeFingerprint(outputContractHash),
            string.Empty)
        {
            ImageRasterFingerprint = NormalizeFingerprint(imageRasterFingerprint),
            ThermalRasterGoldenFingerprint = NormalizeFingerprint(thermalRasterGoldenFingerprint),
            AutomationEventId = NormalizeFingerprint(automationBatch?.EventId),
            AutomationTriggerId = NormalizeText(automationBatch?.TriggerId),
            AutomationConfigurationFingerprint = NormalizeFingerprint(automationBatch?.ConfigurationFingerprint),
            AutomationSourceFingerprint = NormalizeFingerprint(automationBatch?.SourceFingerprint),
            AutomationPreparedBatchId = NormalizeFingerprint(automationBatch?.PreparedBatchId),
            PrintMethod = NormalizeText(printMethod),
            NativeCommandsUsed = nativeCommandsUsed
        };

        return normalized with { Fingerprint = ComputeManifestFingerprint(normalized) };
    }

    /// <summary>
    /// Canonical row digest. Dictionary ordering is ignored, row ordering is
    /// significant, null rows are explicit, and each string is length-prefixed
    /// to prevent delimiter-based collisions.
    /// </summary>
    public static string ComputeRowsFingerprint(IEnumerable<IReadOnlyDictionary<string, string>?>? rows)
    {
        var rowList = rows?.ToArray() ?? Array.Empty<IReadOnlyDictionary<string, string>?>();
        var canonical = new StringBuilder();
        AppendInteger(canonical, rowList.Length);
        for (var rowIndex = 0; rowIndex < rowList.Length; rowIndex++)
        {
            var row = rowList[rowIndex];
            AppendInteger(canonical, rowIndex);
            if (row is null)
            {
                AppendString(canonical, "<null-row>");
                continue;
            }

            AppendInteger(canonical, row.Count);
            foreach (var pair in row.OrderBy(item => item.Key, StringComparer.Ordinal))
            {
                AppendString(canonical, NormalizeText(pair.Key));
                AppendString(canonical, pair.Value ?? string.Empty);
            }
        }

        return HashCanonical(canonical);
    }

    private static string ComputeManifestFingerprint(PrintJobManifest manifest)
        => ComputeManifestFingerprint(manifest, includeThermalRasterGolden: true, includeAutomationProvenance: true);

    private static string ComputePreviousManifestFingerprint(PrintJobManifest manifest)
        => ComputeManifestFingerprint(manifest, includeThermalRasterGolden: true, includeAutomationProvenance: false);

    private static string ComputeLegacyManifestFingerprint(PrintJobManifest manifest)
        => ComputeManifestFingerprint(manifest, includeThermalRasterGolden: false, includeAutomationProvenance: false);

    private static string ComputeManifestFingerprint(PrintJobManifest manifest, bool includeThermalRasterGolden, bool includeAutomationProvenance)
    {
        var canonical = new StringBuilder();
        AppendString(canonical, manifest.ContractVersion);
        AppendString(canonical, manifest.TemplateName);
        AppendString(canonical, manifest.TemplateFilePath);
        AppendString(canonical, manifest.PrintMode);
        AppendString(canonical, manifest.PrinterName);
        AppendNumber(canonical, manifest.LabelWidthMm);
        AppendNumber(canonical, manifest.LabelHeightMm);
        AppendInteger(canonical, manifest.DpiX);
        AppendInteger(canonical, manifest.DpiY);
        AppendInteger(canonical, manifest.LabelCount);
        AppendInteger(canonical, manifest.SourceRowCount);
        AppendString(canonical, manifest.RowsFingerprint);
        AppendString(canonical, manifest.DocumentHash);
        AppendString(canonical, manifest.TextResourceFingerprint);
        AppendString(canonical, manifest.SceneHash);
        AppendString(canonical, manifest.OutputContractHash);
        AppendString(canonical, manifest.ImageRasterFingerprint);
        if (includeThermalRasterGolden)
        {
            AppendString(canonical, manifest.ThermalRasterGoldenFingerprint);
        }
        if (includeAutomationProvenance)
        {
            AppendString(canonical, manifest.AutomationEventId);
            AppendString(canonical, manifest.AutomationTriggerId);
            AppendString(canonical, manifest.AutomationConfigurationFingerprint);
            AppendString(canonical, manifest.AutomationSourceFingerprint);
            AppendString(canonical, manifest.AutomationPreparedBatchId);
        }

        return HashCanonical(canonical);
    }

    private static string NormalizeText(string? value)
    {
        return (value ?? string.Empty).Normalize(NormalizationForm.FormC).Trim();
    }

    private static string NormalizePath(string? value)
    {
        var path = NormalizeText(value);
        return path.Length == 0 ? string.Empty : path.Replace('/', '\\');
    }

    private static string NormalizeFingerprint(string? value)
    {
        return NormalizeText(value).ToUpperInvariant();
    }

    private static bool HasNoAutomationProvenance(PrintJobManifest manifest) =>
        string.IsNullOrWhiteSpace(manifest.AutomationEventId)
        && string.IsNullOrWhiteSpace(manifest.AutomationTriggerId)
        && string.IsNullOrWhiteSpace(manifest.AutomationConfigurationFingerprint)
        && string.IsNullOrWhiteSpace(manifest.AutomationSourceFingerprint)
        && string.IsNullOrWhiteSpace(manifest.AutomationPreparedBatchId);

    private static double NormalizeDimension(double value)
    {
        return double.IsFinite(value) ? Math.Round(Math.Max(0, value), 6, MidpointRounding.ToEven) : 0;
    }

    private static void AppendString(StringBuilder canonical, string? value)
    {
        var normalized = value ?? string.Empty;
        canonical.Append(Encoding.UTF8.GetByteCount(normalized).ToString(CultureInfo.InvariantCulture));
        canonical.Append(':');
        canonical.Append(normalized);
        canonical.Append(';');
    }

    private static void AppendInteger(StringBuilder canonical, int value)
    {
        AppendString(canonical, value.ToString(CultureInfo.InvariantCulture));
    }

    private static void AppendNumber(StringBuilder canonical, double value)
    {
        AppendString(canonical, value.ToString("R", CultureInfo.InvariantCulture));
    }

    private static string HashCanonical(StringBuilder canonical)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical.ToString())));
    }
}
