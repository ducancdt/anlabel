namespace ANLAbel.Core.Data;

/// <summary>
/// Refresh short-circuit policy (CORE DB06). Matching last-write time alone is
/// never enough — only a matching content identity may skip re-read.
/// </summary>
public static class ExcelDataFreshnessPolicy
{
    public static bool IsUnchanged(FileSourceIdentity? known, FileSourceIdentity? current) =>
        known is not null && current is not null && Equals(known, current);
}
