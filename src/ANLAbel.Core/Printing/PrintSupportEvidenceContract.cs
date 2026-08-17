using System.Collections.ObjectModel;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ANLAbel.Core.Printing;

/// <summary>
/// Builds a redacted, reconstructable support evidence record for one print
/// job.  The bundle keeps correlation identities and fingerprints so an
/// operator or maintainer can diagnose preparation→dispatch→queue outcomes
/// without exporting raw label field values from the production data source.
/// </summary>
public static class PrintSupportEvidenceContract
{
    public const string ContractVersion = "print-support-evidence/v1";
    private static readonly Regex SensitiveKeyPattern = new(
        @"pass(word)?|secret|token|api[_-]?key|authorization|rawvalue|payload|labelvalue|cellvalue",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    /// <summary>
    /// Builds a redacted support bundle from durable recovery/job evidence
    /// already stored after preparation→dispatch.  Recovery never sets
    /// physical verification; operators export this for support without raw
    /// label payloads.
    /// </summary>
    public static PrintSupportEvidenceBundle BuildFromDurableJob(
        string jobId,
        string? printerName,
        int? spoolJobId,
        string? queueState,
        string? documentHash,
        string? sceneHash,
        string? outputContractHash,
        string? manifestFingerprint,
        string lifecycleState,
        string? operatorAction = null,
        string? relatedJobId = null,
        string? reason = null)
    {
        var states = new List<string>();
        if (!string.IsNullOrWhiteSpace(lifecycleState))
        {
            states.Add(lifecycleState.Trim());
        }

        if (!string.IsNullOrWhiteSpace(operatorAction)
            && !string.Equals(operatorAction, "None", StringComparison.OrdinalIgnoreCase))
        {
            states.Add(operatorAction.Trim());
        }

        return Build(
            jobId: jobId,
            queueName: printerName,
            spoolJobId: spoolJobId?.ToString(CultureInfo.InvariantCulture),
            documentHash: documentHash,
            sceneHash: sceneHash,
            outputContractHash: outputContractHash,
            manifestFingerprint: manifestFingerprint,
            textResourceFingerprint: null,
            imageRasterFingerprint: null,
            thermalGoldenFingerprint: null,
            outcome: string.IsNullOrWhiteSpace(lifecycleState) ? "Unknown" : lifecycleState.Trim(),
            physicalOutputVerified: false,
            metadata: new[]
            {
                new KeyValuePair<string, string?>("queueState", queueState),
                new KeyValuePair<string, string?>("operatorAction", operatorAction),
                new KeyValuePair<string, string?>("relatedJobId", relatedJobId),
                new KeyValuePair<string, string?>("reason", reason)
            },
            lifecycleStates: states);
    }

    /// <summary>
    /// Writes the canonical redacted JSON atomically to <paramref name="filePath"/>.
    /// The path must end with <c>.json</c>; partial writes are cleaned up.
    /// </summary>
    public static async Task WriteJsonAsync(
        PrintSupportEvidenceBundle bundle,
        string filePath,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(bundle);
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("A support evidence file path is required.", nameof(filePath));
        }

        var fullPath = Path.GetFullPath(filePath);
        if (!fullPath.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Support evidence must be exported as a .json file.", nameof(filePath));
        }

        var directory = Path.GetDirectoryName(fullPath)
            ?? throw new InvalidOperationException("The support evidence path has no parent directory.");
        Directory.CreateDirectory(directory);

        var json = ToCanonicalJson(bundle);
        var temporaryPath = Path.Combine(directory, $".{Path.GetFileName(fullPath)}.{Guid.NewGuid():N}.tmp");
        try
        {
            await using (var stream = new FileStream(
                temporaryPath,
                new FileStreamOptions
                {
                    Mode = FileMode.CreateNew,
                    Access = FileAccess.Write,
                    Share = FileShare.None,
                    Options = FileOptions.Asynchronous | FileOptions.WriteThrough
                }))
            await using (var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)))
            {
                await writer.WriteAsync(json.AsMemory(), cancellationToken).ConfigureAwait(false);
                await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
            }

            File.Move(temporaryPath, fullPath, overwrite: true);
        }
        catch
        {
            try
            {
                if (File.Exists(temporaryPath))
                {
                    File.Delete(temporaryPath);
                }
            }
            catch
            {
                // Preserve the original write/cancellation exception.
            }

            throw;
        }
    }

    public static PrintSupportEvidenceBundle Build(
        string jobId,
        string? queueName,
        string? spoolJobId,
        string? documentHash,
        string? sceneHash,
        string? outputContractHash,
        string? manifestFingerprint,
        string? textResourceFingerprint,
        string? imageRasterFingerprint,
        string? thermalGoldenFingerprint,
        string outcome,
        bool physicalOutputVerified,
        IEnumerable<KeyValuePair<string, string?>>? metadata = null,
        IEnumerable<string>? lifecycleStates = null)
    {
        if (string.IsNullOrWhiteSpace(jobId))
        {
            throw new ArgumentException("A durable job identity is required for support evidence.", nameof(jobId));
        }

        var redactedMetadata = new List<KeyValuePair<string, string>>();
        if (metadata is not null)
        {
            foreach (var pair in metadata)
            {
                var key = (pair.Key ?? string.Empty).Trim();
                if (key.Length == 0)
                {
                    continue;
                }

                if (IsSensitiveKey(key))
                {
                    redactedMetadata.Add(new KeyValuePair<string, string>(key, "[redacted]"));
                    continue;
                }

                redactedMetadata.Add(new KeyValuePair<string, string>(
                    key,
                    RedactValue(pair.Value)));
            }
        }

        var states = lifecycleStates?
            .Where(static s => !string.IsNullOrWhiteSpace(s))
            .Select(static s => s.Trim())
            .ToArray()
            ?? Array.Empty<string>();

        var bundle = new PrintSupportEvidenceBundle(
            ContractVersion,
            jobId.Trim(),
            Normalize(queueName),
            Normalize(spoolJobId),
            Normalize(documentHash),
            Normalize(sceneHash),
            Normalize(outputContractHash),
            Normalize(manifestFingerprint),
            Normalize(textResourceFingerprint),
            Normalize(imageRasterFingerprint),
            Normalize(thermalGoldenFingerprint),
            Normalize(outcome),
            physicalOutputVerified,
            new ReadOnlyCollection<KeyValuePair<string, string>>(redactedMetadata),
            new ReadOnlyCollection<string>(states));

        return bundle with { EvidenceFingerprint = bundle.ComputeFingerprint() };
    }

    public static string ToCanonicalJson(PrintSupportEvidenceBundle bundle)
    {
        ArgumentNullException.ThrowIfNull(bundle);
        // Deterministic property order for cross-machine support comparison.
        var payload = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["contractVersion"] = bundle.ContractVersion,
            ["jobId"] = bundle.JobId,
            ["queueName"] = bundle.QueueName,
            ["spoolJobId"] = bundle.SpoolJobId,
            ["documentHash"] = bundle.DocumentHash,
            ["sceneHash"] = bundle.SceneHash,
            ["outputContractHash"] = bundle.OutputContractHash,
            ["manifestFingerprint"] = bundle.ManifestFingerprint,
            ["textResourceFingerprint"] = bundle.TextResourceFingerprint,
            ["imageRasterFingerprint"] = bundle.ImageRasterFingerprint,
            ["thermalGoldenFingerprint"] = bundle.ThermalGoldenFingerprint,
            ["outcome"] = bundle.Outcome,
            ["physicalOutputVerified"] = bundle.PhysicalOutputVerified ? 1 : 0,
            ["lifecycleStates"] = bundle.LifecycleStates.ToArray(),
            ["metadata"] = bundle.Metadata
                .OrderBy(static p => p.Key, StringComparer.Ordinal)
                .Select(static p => new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["key"] = p.Key,
                    ["value"] = p.Value
                })
                .ToArray()
        };
        return JsonSerializer.Serialize(payload);
    }

    public static bool ContainsRawPayloadLeak(PrintSupportEvidenceBundle bundle, params string[] forbiddenSubstrings)
    {
        ArgumentNullException.ThrowIfNull(bundle);
        var json = ToCanonicalJson(bundle);
        foreach (var fragment in forbiddenSubstrings)
        {
            if (string.IsNullOrEmpty(fragment))
            {
                continue;
            }

            if (json.Contains(fragment, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsSensitiveKey(string key)
        => SensitiveKeyPattern.IsMatch(key);

    private static string RedactValue(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        // Never keep multi-line or long free-text values that could hold row
        // payloads; keep short machine codes and fingerprints as-is.
        if (value.Length > 128 || value.Contains('\n', StringComparison.Ordinal) || value.Contains('\r', StringComparison.Ordinal))
        {
            return "[redacted-long-value]";
        }

        return value.Trim();
    }

    private static string Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
}

public sealed record PrintSupportEvidenceBundle(
    string ContractVersion,
    string JobId,
    string QueueName,
    string SpoolJobId,
    string DocumentHash,
    string SceneHash,
    string OutputContractHash,
    string ManifestFingerprint,
    string TextResourceFingerprint,
    string ImageRasterFingerprint,
    string ThermalGoldenFingerprint,
    string Outcome,
    bool PhysicalOutputVerified,
    IReadOnlyList<KeyValuePair<string, string>> Metadata,
    IReadOnlyList<string> LifecycleStates,
    string EvidenceFingerprint = "")
{
    public string ComputeFingerprint()
    {
        var canonical = PrintSupportEvidenceContract.ToCanonicalJson(this with { EvidenceFingerprint = string.Empty });
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant();
    }

    public string Summarize()
        => string.Create(
            CultureInfo.InvariantCulture,
            $"{ContractVersion}|job={JobId}|queue={QueueName}|outcome={Outcome}|physical={PhysicalOutputVerified}|fp={EvidenceFingerprint}");
}
