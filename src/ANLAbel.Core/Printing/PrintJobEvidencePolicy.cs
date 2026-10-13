namespace ANLAbel.Core.Printing;

/// <summary>
/// Distinguishes required pre-dispatch job evidence from best-effort post-dispatch
/// audit (CORE P08). Missing required evidence must block send; a log I/O failure
/// after spool accept must not invent Failed or trigger automatic retry.
/// </summary>
public static class PrintJobEvidencePolicy
{
    public static bool IsRequiredBeforeDispatch(PrintJobLifecycleState toState) =>
        toState is PrintJobLifecycleState.Preparing
            or PrintJobLifecycleState.PreflightPassed
            or PrintJobLifecycleState.Dispatching;

    /// <summary>
    /// Expanded print rows are label instances; distinct source rows are the operator
    /// selection. Callers must not swap these when building <see cref="PrintJobManifest"/>.
    /// </summary>
    public static (int LabelCount, int SourceRowCount) ResolveManifestCounts(
        int expandedLabelCount,
        int? distinctSourceRowCount)
    {
        var labelCount = Math.Max(0, expandedLabelCount);
        var sourceRowCount = distinctSourceRowCount is > 0
            ? distinctSourceRowCount.Value
            : labelCount;
        return (labelCount, sourceRowCount);
    }
}
