using System.IO;
using ANLAbel.Core.Automation;
using ANLAbel.Core.Data;
using ANLAbel.Core.Scene;
using ANLAbel.Printing.PrinterProfiles;
using ANLAbel.Project.SaveLoad;

namespace ANLAbel.App;

/// <summary>
/// Runs the existing print preflight against one immutable prepared batch and
/// one exact saved queue. This is evidence-only: it creates no print manifest,
/// state transition, queue job or filesystem mutation.
/// </summary>
public sealed class AutomationPreparedBatchPreflightService
{
    private readonly IProjectFileService _files;
    private readonly PrintPreflightValidator _validator;
    private readonly IPrinterQueueLookup _queueLookup;

    public AutomationPreparedBatchPreflightService(
        IProjectFileService? files = null,
        PrintPreflightValidator? validator = null,
        IPrinterQueueLookup? queueLookup = null)
    {
        _files = files ?? new ProjectFileService();
        _validator = validator ?? new PrintPreflightValidator();
        _queueLookup = queueLookup ?? new WindowsPrinterQueueLookup();
    }

    public async Task<AutomationPreparedBatchPreflightResult> ValidateAsync(
        FileDropPreparedBatchIdentity batch,
        FileDropTriggerConfiguration configuration,
        IReadOnlyList<DataRecord> records,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(batch);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(records);
        if (!FileDropTriggerConfigurationContract.TryValidateDispatchBinding(configuration, out var configurationError))
            return Block(configurationError);
        if (!string.Equals(batch.TriggerId, configuration.TriggerId, StringComparison.Ordinal)
            || !string.Equals(batch.ConfigurationFingerprint, configuration.ConfigurationFingerprint, StringComparison.Ordinal)
            || batch.RecordCount != records.Count
            || !string.Equals(batch.DataFingerprint, FileDropPreparedBatchContract.ComputeDataFingerprint(records), StringComparison.Ordinal))
            return Block("Prepared batch no longer matches the exact trigger configuration or ordered records.");

        var queue = _queueLookup.Resolve(configuration.QueueName);
        if (!queue.IsAvailable || !string.Equals(queue.CanonicalName, configuration.QueueName, StringComparison.OrdinalIgnoreCase))
            return Block(string.IsNullOrWhiteSpace(queue.ErrorMessage) ? "The configured named queue is unavailable." : queue.ErrorMessage);

        try
        {
            var template = await _files.LoadAsync(configuration.TargetTemplatePath, cancellationToken);
            var templateFingerprint = DocumentSnapshot.Capture(template).DocumentHash;
            if (!string.Equals(templateFingerprint, batch.TemplateFingerprint, StringComparison.Ordinal))
                return Block("Configured template changed after prepared-batch identity was created.");
            if (!string.Equals(template.PrinterProfile.PrinterName, queue.CanonicalName, StringComparison.OrdinalIgnoreCase))
                return Block("Configured queue does not exactly match the template's saved printer profile.");

            var rows = records
                .Select(record => (IReadOnlyDictionary<string, string>)record.Values.ToDictionary(item => item.Key, item => item.Value ?? string.Empty, StringComparer.OrdinalIgnoreCase))
                .ToArray();
            var dpi = template.PrinterProfile.Dpi > 0 ? template.PrinterProfile.Dpi : template.Dpi;
            var result = _validator.Validate(template, rows, dpi, dpi, cancellationToken);
            return new(result.IsSuccess, queue.CanonicalName, result.Issues,
                result.IsSuccess
                    ? "Prepared batch passed shared preflight against its exact template and named queue; no manifest, job or dispatch was created."
                    : result.ToUserMessage());
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            return Block($"Prepared batch preflight could not load the configured template: {ex.Message}");
        }
    }

    private static AutomationPreparedBatchPreflightResult Block(string diagnostic) =>
        new(false, string.Empty, [], diagnostic);
}

public sealed record AutomationPreparedBatchPreflightResult(
    bool Passed,
    string CanonicalQueueName,
    IReadOnlyList<PrintPreflightIssue> Issues,
    string Diagnostic);
