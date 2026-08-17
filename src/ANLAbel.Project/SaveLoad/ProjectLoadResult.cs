using ANLAbel.Core.Models;

namespace ANLAbel.Project.SaveLoad;

/// <summary>
/// Describes where an opened template came from.  Recovery is intentionally
/// explicit so the UI can warn the operator instead of silently replacing a
/// damaged primary file.
/// </summary>
public sealed record ProjectLoadResult(
    LabelTemplate Template,
    string SourcePath,
    bool RecoveredFromBackup,
    string? BackupPath,
    string? PrimaryError)
{
    public bool IsPrimary => !RecoveredFromBackup;
}
