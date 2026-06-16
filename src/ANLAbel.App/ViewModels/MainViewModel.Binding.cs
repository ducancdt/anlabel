using ANLAbel.Core.Expressions;
using ANLAbel.Core.Expressions.Formulas;
using ANLAbel.Core.Models;
using ANLAbel.Core.Mvvm;

namespace ANLAbel.App.ViewModels;

public sealed partial class MainViewModel
{
    private FormulaEvaluationResult EvaluateSelectedFormula()
    {
        if (SelectedObject is null || string.IsNullOrWhiteSpace(SelectedObject.BindingExpression) || !FormulaBindingEvaluator.LooksLikeFormula(SelectedObject.BindingExpression))
        {
            return new FormulaEvaluationResult(string.Empty, Array.Empty<string>(), Array.Empty<string>());
        }

        return PreviewRow is null
            ? new FormulaEvaluationResult(SelectedObject.BindingExpression, Array.Empty<string>(), Array.Empty<string>())
            : FormulaBindingEvaluator.Evaluate(SelectedObject.BindingExpression, PreviewRow);
    }

    private BindingPreviewResult EvaluateSelectedBinding()
    {
        if (SelectedObject is null || string.IsNullOrWhiteSpace(SelectedObject.BindingExpression))
        {
            return BindingPreviewResult.Empty;
        }

        return EvaluateBinding(SelectedObject);
    }

    private BindingPreviewResult EvaluateBinding(LabelObject item)
    {
        if (string.IsNullOrWhiteSpace(item.BindingExpression))
        {
            return BindingPreviewResult.Empty;
        }

        var expression = item.BindingExpression;
        var knownFields = AvailableDatabaseFields.Select(field => field.Name).ToArray();
        if (FormulaBindingEvaluator.LooksLikeFormula(expression))
        {
            var analysisRow = CreateBindingAnalysisRow();
            var analysis = FormulaBindingEvaluator.Evaluate(expression, analysisRow);
            var previewEvaluation = PreviewRow is null
                ? new FormulaEvaluationResult(string.Empty, Array.Empty<string>(), analysis.UsedFields)
                : FormulaBindingEvaluator.Evaluate(expression, PreviewRow);
            var missingFields = analysis.UsedFields
                .Where(field => !FieldNameResolver.TryResolveFieldName(field, knownFields, out _))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            var errors = analysis.Errors
                .Concat(previewEvaluation.Errors)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            return new BindingPreviewResult(
                "Formula",
                PreviewRow is null ? string.Empty : previewEvaluation.Value,
                analysis.UsedFields,
                missingFields,
                errors,
                BuildBindingStatusText(missingFields, errors, PreviewRow is not null));
        }

        var usedFields = BindingExpressionEvaluator.GetFields(expression);
        var missingPlaceholderFields = usedFields
            .Where(field => !FieldNameResolver.TryResolveFieldName(field, knownFields, out _))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        return new BindingPreviewResult(
            "Field placeholders",
            PreviewRow is null ? string.Empty : BindingExpressionEvaluator.Evaluate(expression, PreviewRow),
            usedFields,
            missingPlaceholderFields,
            Array.Empty<string>(),
            BuildBindingStatusText(missingPlaceholderFields, Array.Empty<string>(), PreviewRow is not null));
    }

    private IReadOnlyList<BindingIssueSummary> GetBindingIssues()
    {
        return Template.Objects
            .Where(item => item.IsVisible && !string.IsNullOrWhiteSpace(item.BindingExpression))
            .Select(item => new { Item = item, Binding = EvaluateBinding(item) })
            .Where(result => result.Binding.MissingFields.Count > 0 || result.Binding.Errors.Count > 0)
            .Select(result => new BindingIssueSummary(
                result.Item.Id,
                result.Item.Name,
                result.Item.Type.ToString(),
                result.Binding.KindText,
                result.Binding.StatusText,
                result.Binding.MissingFields,
                result.Binding.Errors))
            .ToArray();
    }

    private void SelectBindingIssue(BindingIssueSummary? issue)
    {
        if (issue is null)
        {
            return;
        }

        SelectedBindingIssue = issue;
        SelectedObject = Template.Objects.FirstOrDefault(item => item.Id == issue.ObjectId);
        if (SelectedObject is not null)
        {
            StatusText = $"Selected binding issue: {SelectedObject.Name}";
        }
    }

    private Dictionary<string, string> CreateBindingAnalysisRow()
    {
        var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var field in AvailableDatabaseFields)
        {
            if (!row.ContainsKey(field.Name))
            {
                row[field.Name] = PreviewRow is not null && FieldNameResolver.TryGetValue(PreviewRow, field.Name, out var value, out _)
                    ? value
                    : string.Empty;
            }
        }

        if (PreviewRow is not null)
        {
            foreach (var pair in PreviewRow)
            {
                if (!row.ContainsKey(pair.Key))
                {
                    row[pair.Key] = pair.Value;
                }
            }
        }

        return row;
    }

    private static string BuildBindingStatusText(IReadOnlyList<string> missingFields, IReadOnlyList<string> errors, bool hasPreviewRow)
    {
        if (missingFields.Count > 0)
        {
            return missingFields.Count == 1
                ? "Binding is missing 1 field in the current workbook."
                : $"Binding is missing {missingFields.Count} fields in the current workbook.";
        }

        if (errors.Count > 0)
        {
            return "Binding has validation errors.";
        }

        return hasPreviewRow
            ? "Binding is linked to the current Excel preview row."
            : "Binding is valid. Import or select an Excel row to preview output.";
    }

    private void RaiseFormulaPreviewChanged()
    {
        RefreshObjectTreeBindingStates();
        OnPropertyChanged(nameof(HasSelectedBinding));
        OnPropertyChanged(nameof(IsSelectedBindingFormula));
        OnPropertyChanged(nameof(SelectedBindingKindText));
        OnPropertyChanged(nameof(SelectedBindingPreviewValue));
        OnPropertyChanged(nameof(SelectedBindingUsedFieldsText));
        OnPropertyChanged(nameof(SelectedBindingMissingFieldsText));
        OnPropertyChanged(nameof(SelectedBindingUsedFieldsSummary));
        OnPropertyChanged(nameof(SelectedBindingMissingFieldsSummary));
        OnPropertyChanged(nameof(SelectedBindingStatusText));
        OnPropertyChanged(nameof(SelectedBindingErrorsText));
        OnPropertyChanged(nameof(BindingIssues));
        OnPropertyChanged(nameof(HasBindingIssues));
        OnPropertyChanged(nameof(BindingIssuesSummary));
        OnPropertyChanged(nameof(FormulaPreviewValue));
        OnPropertyChanged(nameof(FormulaPreviewErrors));
        OnPropertyChanged(nameof(FormulaPreviewUsedFields));
        RaiseFormulaBuilderChanged();
    }

    private void RefreshObjectTreeBindingStates()
    {
        var issuesByObjectId = GetBindingIssues()
            .ToDictionary(issue => issue.ObjectId, StringComparer.OrdinalIgnoreCase);

        foreach (var item in Template.Objects)
        {
            if (string.IsNullOrWhiteSpace(item.BindingExpression))
            {
                item.HasBindingIssue = false;
                item.BindingStateDisplayText = string.Empty;
                continue;
            }

            if (issuesByObjectId.TryGetValue(item.Id, out var issue))
            {
                item.HasBindingIssue = true;
                item.BindingStateDisplayText = BuildObjectTreeBindingIssueText(issue);
                continue;
            }

            item.HasBindingIssue = false;
            item.BindingStateDisplayText = FormulaBindingEvaluator.LooksLikeFormula(item.BindingExpression)
                ? "Formula linked"
                : "Linked Excel";
        }
    }

    private static string BuildObjectTreeBindingIssueText(BindingIssueSummary issue)
    {
        if (issue.MissingFields.Count > 0)
        {
            return issue.MissingFields.Count == 1
                ? $"Missing: {issue.MissingFields[0]}"
                : $"Missing {issue.MissingFields.Count} fields";
        }

        return issue.Errors.Count > 0
            ? "Formula error"
            : issue.StatusText;
    }
}
