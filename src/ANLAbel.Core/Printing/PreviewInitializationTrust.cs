namespace ANLAbel.Core.Printing;

/// <summary>
/// Print-preview initialization trust. A verified ticket is not Ready when the
/// compiled scene failed: preview must not claim an output print will reject.
/// </summary>
public static class PreviewInitializationTrust
{
    public static string SceneBlockMessage(string? sceneDiagnostics)
    {
        var detail = string.IsNullOrWhiteSpace(sceneDiagnostics)
            ? "the scene compiler did not produce a verified scene"
            : sceneDiagnostics;
        return $"Printing is blocked because the label design is invalid ({detail}). Fix the design and refresh preview.";
    }

    public static PreviewTrustLevel Combine(
        PreviewTrustLevel planTrust,
        bool sceneCompilationVerified,
        bool contentPreflightSucceeded)
    {
        var outputReady = sceneCompilationVerified && contentPreflightSucceeded;
        if (!outputReady)
        {
            return planTrust == PreviewTrustLevel.DesignOnly
                ? PreviewTrustLevel.DesignOnly
                : PreviewTrustLevel.EffectiveBlocked;
        }

        return planTrust;
    }

    /// <summary>
    /// Ready names one prepared snapshot. A later quantity, data, or ticket
    /// change must not keep that mark; every other trust level stays as it is.
    /// </summary>
    public static PreviewTrustLevel RetainForCurrentSnapshot(
        PreviewTrustLevel preparedTrust,
        bool snapshotStillCurrent)
    {
        if (snapshotStillCurrent || preparedTrust != PreviewTrustLevel.EffectiveReady)
        {
            return preparedTrust;
        }

        return PreviewTrustLevel.Stale;
    }

    /// <summary>
    /// Selected records still describe the prepared label sequence. Copies of
    /// zero are not part of the sequence. A repeated source row is not a match.
    /// </summary>
    public static bool QuantitySnapshotMatches(
        IReadOnlyList<LabelInstance> preparedInstances,
        IEnumerable<(int SourceRowNumber, int Copies)> selectedRecords)
    {
        ArgumentNullException.ThrowIfNull(preparedInstances);
        ArgumentNullException.ThrowIfNull(selectedRecords);

        var prepared = new Dictionary<int, int>();
        foreach (var instance in preparedInstances)
        {
            prepared.TryGetValue(instance.SourceRowNumber, out var copies);
            prepared[instance.SourceRowNumber] = copies + 1;
        }

        var selected = new Dictionary<int, int>();
        foreach (var record in selectedRecords)
        {
            if (record.Copies <= 0)
            {
                continue;
            }

            if (!selected.TryAdd(record.SourceRowNumber, record.Copies))
            {
                return false;
            }
        }

        if (prepared.Count != selected.Count)
        {
            return false;
        }

        foreach (var pair in prepared)
        {
            if (!selected.TryGetValue(pair.Key, out var copies) || copies != pair.Value)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// The prepared document, output-contract fingerprint, and ticket-evidence
    /// bit are still the ones Ready was computed from. An empty fingerprint
    /// matches only another empty fingerprint; verification flipping is a change.
    /// </summary>
    public static bool OutputSnapshotUnchanged(
        string? preparedDocumentHash,
        string? preparedOutputContractHash,
        bool preparedTicketVerified,
        string? currentDocumentHash,
        string? currentOutputContractHash,
        bool currentTicketVerified)
    {
        return string.Equals(NormalizeFingerprint(preparedDocumentHash), NormalizeFingerprint(currentDocumentHash), StringComparison.Ordinal)
            && string.Equals(NormalizeFingerprint(preparedOutputContractHash), NormalizeFingerprint(currentOutputContractHash), StringComparison.OrdinalIgnoreCase)
            && preparedTicketVerified == currentTicketVerified;
    }

    private static string NormalizeFingerprint(string? value)
        => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
}
