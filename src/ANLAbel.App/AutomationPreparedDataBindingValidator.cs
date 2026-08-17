using ANLAbel.Core.Automation;
using ANLAbel.Core.Data;
using ANLAbel.Core.Expressions;
using ANLAbel.Project.SaveLoad;
using System.IO;

namespace ANLAbel.App;

/// <summary>
/// Validates that prepared automation records can satisfy the configured
/// template's explicit binding expressions. This creates no manifest, queue
/// request, printer call or durable copy of the record payload.
/// </summary>
public sealed class AutomationPreparedDataBindingValidator
{
    private readonly IProjectFileService _files;
    private readonly AutomationTemplateBindingValidator _templatePolicy;

    public AutomationPreparedDataBindingValidator(
        IProjectFileService? files = null,
        AutomationTemplateBindingValidator? templatePolicy = null)
    {
        _files = files ?? new ProjectFileService();
        _templatePolicy = templatePolicy ?? new AutomationTemplateBindingValidator(_files);
    }

    public async Task<AutomationPreparedDataBindingResult> ValidateAsync(
        FileDropTriggerConfiguration configuration,
        IReadOnlyList<DataRecord> preparedRecords,
        CancellationToken cancellationToken = default)
    {
        if (preparedRecords.Count == 0)
            return new(false, [], "Automation data binding requires at least one prepared record.");

        var policy = await _templatePolicy.ValidateAsync(configuration, cancellationToken);
        if (!policy.Allowed)
            return new(false, [], $"Template policy blocked prepared records: {policy.Diagnostic}");

        try
        {
            var template = await _files.LoadAsync(configuration.TargetTemplatePath, cancellationToken);
            var fields = template.Objects
                .SelectMany(item => BindingExpressionEvaluator.GetFields(item.BindingExpression))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Order(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            if (fields.Length == 0)
                return new(true, fields, "Template has no data bindings; prepared records are policy-valid but no manifest, queue or print was created.");

            for (var index = 0; index < preparedRecords.Count; index++)
            {
                var missing = fields.Where(field => !preparedRecords[index].TryGetValue(field, out _)).ToArray();
                if (missing.Length != 0)
                    return new(false, fields, $"Prepared record {index + 1} is missing template field(s): {string.Join(", ", missing)}.");
            }
            return new(true, fields, $"Prepared {preparedRecords.Count} record(s) satisfy {fields.Length} template binding field(s). No manifest, queue or print was created.");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            return new(false, [], $"Configured target template cannot be checked against prepared records: {ex.Message}");
        }
    }
}

public sealed record AutomationPreparedDataBindingResult(bool Allowed, IReadOnlyList<string> RequiredFields, string Diagnostic);
