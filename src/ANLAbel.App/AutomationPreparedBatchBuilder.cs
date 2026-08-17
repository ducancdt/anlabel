using ANLAbel.Core.Automation;
using ANLAbel.Core.Data;
using ANLAbel.Core.Scene;
using ANLAbel.Project.SaveLoad;
using System.IO;

namespace ANLAbel.App;

/// <summary>
/// Combines already-validated prepared records with one exact local template
/// snapshot. It emits only a fingerprint identity and never creates a queue,
/// manifest, persistent record payload or printer request.
/// </summary>
public sealed class AutomationPreparedBatchBuilder
{
    private readonly IProjectFileService _files;
    private readonly AutomationPreparedDataBindingValidator _bindingValidator;

    public AutomationPreparedBatchBuilder(
        IProjectFileService? files = null,
        AutomationPreparedDataBindingValidator? bindingValidator = null)
    {
        _files = files ?? new ProjectFileService();
        _bindingValidator = bindingValidator ?? new AutomationPreparedDataBindingValidator(_files);
    }

    public async Task<AutomationPreparedBatchBuildResult> BuildAsync(
        FileDropEventIdentity eventIdentity,
        FileDropTriggerConfiguration configuration,
        IReadOnlyList<DataRecord> preparedRecords,
        CancellationToken cancellationToken = default)
    {
        var validation = await _bindingValidator.ValidateAsync(configuration, preparedRecords, cancellationToken);
        if (!validation.Allowed)
            return new(false, null, validation.Diagnostic);

        try
        {
            var template = await _files.LoadAsync(configuration.TargetTemplatePath, cancellationToken);
            var templateFingerprint = DocumentSnapshot.Capture(template).DocumentHash;
            var identity = FileDropPreparedBatchContract.Create(eventIdentity, templateFingerprint, preparedRecords);
            return new(true, identity,
                $"Prepared batch {identity.PreparedBatchId[..12]} binds {identity.RecordCount} record(s) to exact source, configuration and template fingerprints; no manifest, queue or print was created.");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            return new(false, null, $"Prepared batch identity could not capture the configured template: {ex.Message}");
        }
    }
}

public sealed record AutomationPreparedBatchBuildResult(
    bool Allowed,
    FileDropPreparedBatchIdentity? Identity,
    string Diagnostic);
