using ANLAbel.Core.Automation;
using ANLAbel.Core.Scene;
using ANLAbel.Core.Workflow;
using ANLAbel.Data.Workflow;
using ANLAbel.Project.SaveLoad;
using System.IO;
using System.Text.Json;

namespace ANLAbel.App;

/// <summary>Validates the configured template/revision/policy before any future automation manifest is created.</summary>
public sealed class AutomationTemplateBindingValidator
{
    private readonly IProjectFileService _files;
    public AutomationTemplateBindingValidator(IProjectFileService? files = null) => _files = files ?? new ProjectFileService();

    public async Task<DocumentWorkflowPrintPolicyResult> ValidateAsync(FileDropTriggerConfiguration configuration, CancellationToken cancellationToken = default)
    {
        if (!FileDropTriggerConfigurationContract.TryValidateDispatchBinding(configuration, out var readiness)) return new(false, readiness);
        try
        {
            var template = await _files.LoadAsync(configuration.TargetTemplatePath, cancellationToken);
            var hash = DocumentSnapshot.Capture(template).DocumentHash;
            var store = DocumentWorkflowSidecar.Open(configuration.TargetTemplatePath);
            var events = store.ReadValid(out var diagnostics);
            var latest = events.LastOrDefault();
            var state = latest is not null && latest.DocumentHash == hash ? latest.To : DocumentWorkflowState.Draft;
            return DocumentWorkflowPrintPolicy.Evaluate(configuration.PrintPolicyMode, true, diagnostics.Count == 0, latest is not null && latest.DocumentHash == hash, state);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException or JsonException)
        {
            return new(false, $"Configured target template cannot be validated: {ex.Message}");
        }
    }
}
