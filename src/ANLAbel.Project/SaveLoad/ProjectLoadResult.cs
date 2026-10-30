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

    /// <summary>
    /// For a NiceLabel import (<c>.lbl</c> or <c>.nlbl</c>), how many
    /// object records the source file stores in total, converted or not;
    /// null for native <c>.anlabel</c> files. Lets the UI state honestly
    /// how much of the label was carried over.
    /// </summary>
    public int? NiceLabelTotalObjectCount { get; init; }

    /// <summary>
    /// For a NiceLabel 10 import: the names of the stored items that did not
    /// become ANLAbel objects (empty when all did); null for other files.
    /// </summary>
    public IReadOnlyList<string>? NiceLabelNotConverted { get; init; }

    /// <summary>
    /// For a NiceLabel 10 import: what was done with the printer and media
    /// the label was authored for; null otherwise.
    /// </summary>
    public ANLAbel.Project.NiceLabel10.NiceLabelPrinterSetupResult? NiceLabelPrinterSetup { get; init; }
}
