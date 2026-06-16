using System.Data;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Input;
using ANLAbel.Core.Expressions;
using ANLAbel.Core.Expressions.Formulas;
using ANLAbel.Core.Models;
using ANLAbel.Core.Mvvm;

namespace ANLAbel.App.ViewModels;

public sealed partial class MainViewModel
{
    public async Task ImportExcelAsync(string filePath, string sheetName)
    {
        var table = await _excelDataService.LoadSheetAsync(filePath, sheetName);
        ExcelDataView = table.DefaultView;
        ExcelHeaders.Clear();
        foreach (DataColumn column in table.Columns)
        {
            ExcelHeaders.Add(column.ColumnName);
        }

        Template.DatabaseConfig.FilePath = filePath;
        Template.DatabaseConfig.SheetName = sheetName;
        Template.DatabaseConfig.HeaderRowIndex = 1;
        SyncDatabaseFieldsFromTable(table);
        if (ExcelDataView.Count > 0)
        {
            var targetRowIndex = Math.Max(0, Math.Min(ExcelDataView.Count - 1, Template.DatabaseConfig.LastSelectedRow));
            SelectedDataItem = ExcelDataView[targetRowIndex];
        }
        else
        {
            SelectedDataItem = null;
            Template.DatabaseConfig.LastSelectedRow = 0;
        }
        OnPropertyChanged(nameof(HasLinkedExcelSource));
        OnPropertyChanged(nameof(LinkedExcelSourceText));
        OnPropertyChanged(nameof(CurrentExcelRowText));
        StatusText = $"Imported {table.Rows.Count} rows from {Path.GetFileName(filePath)} / {sheetName}";
    }

    public async Task RefreshExcelDataAsync()
    {
        if (!CanRefreshExcelData())
        {
            StatusText = "No Excel database is linked yet";
            return;
        }

        await ImportExcelAsync(Template.DatabaseConfig.FilePath, Template.DatabaseConfig.SheetName);
        StatusText = $"Excel data refreshed: {Path.GetFileName(Template.DatabaseConfig.FilePath)} / {Template.DatabaseConfig.SheetName}";
    }

    private bool CanRefreshExcelData()
    {
        return !string.IsNullOrWhiteSpace(Template.DatabaseConfig.FilePath)
            && !string.IsNullOrWhiteSpace(Template.DatabaseConfig.SheetName)
            && File.Exists(Template.DatabaseConfig.FilePath);
    }

    private async Task RestoreLinkedExcelDataAsync()
    {
        OnPropertyChanged(nameof(HasLinkedExcelSource));
        OnPropertyChanged(nameof(LinkedExcelSourceText));

        if (!HasLinkedExcelSource)
        {
            StatusText = $"Opened: {Path.GetFileName(CurrentFilePath)}";
            return;
        }

        if (!File.Exists(Template.DatabaseConfig.FilePath))
        {
            ExcelDataView = null;
            PreviewRow = null;
            SelectedDataItem = null;
            ExcelHeaders.Clear();
            OnPropertyChanged(nameof(CurrentExcelRowText));
            StatusText = $"Opened: {Path.GetFileName(CurrentFilePath)}. Linked Excel file not found: {Template.DatabaseConfig.FilePath}";
            return;
        }

        try
        {
            await ImportExcelAsync(Template.DatabaseConfig.FilePath, Template.DatabaseConfig.SheetName);
            StatusText = $"Opened: {Path.GetFileName(CurrentFilePath)}. Excel link restored: {Path.GetFileName(Template.DatabaseConfig.FilePath)} / {Template.DatabaseConfig.SheetName}";
        }
        catch (Exception ex)
        {
            ExcelDataView = null;
            PreviewRow = null;
            SelectedDataItem = null;
            ExcelHeaders.Clear();
            OnPropertyChanged(nameof(CurrentExcelRowText));
            StatusText = $"Opened: {Path.GetFileName(CurrentFilePath)}. Excel link could not be restored: {ex.Message}";
        }
    }

    private void SyncDatabaseFieldsFromTable(DataTable table)
    {
        var existingLabelFields = LabelDatabaseFields
            .Select(field => CloneDatabaseField(field))
            .ToArray();
        var shouldSeedLabelFields = existingLabelFields.Length == 0 && !Template.Objects.Any(item => !string.IsNullOrWhiteSpace(item.BindingExpression));
        AvailableDatabaseFields.Clear();
        foreach (DataColumn column in table.Columns)
        {
            AvailableDatabaseFields.Add(CreateDatabaseField(column.ColumnName, table));
        }

        var requestedFieldNames = existingLabelFields
            .Select(field => field.Name)
            .Concat(GetReferencedTemplateFieldNames());
        var resolvedFieldMap = BuildResolvedFieldMap(requestedFieldNames, AvailableDatabaseFields.Select(field => field.Name));

        for (var i = LabelDatabaseFields.Count - 1; i >= 0; i--)
        {
            if (!FieldNameResolver.TryResolveFieldName(LabelDatabaseFields[i].Name, ExcelHeaders, out _))
            {
                LabelDatabaseFields.RemoveAt(i);
            }
        }

        foreach (var field in existingLabelFields)
        {
            if (!resolvedFieldMap.TryGetValue(field.Name, out var resolvedFieldName))
            {
                continue;
            }

            var availableField = AvailableDatabaseFields.FirstOrDefault(candidate => string.Equals(candidate.Name, resolvedFieldName, StringComparison.OrdinalIgnoreCase));
            if (availableField is not null)
            {
                var existing = LabelDatabaseFields.FirstOrDefault(candidate => string.Equals(candidate.Name, availableField.Name, StringComparison.OrdinalIgnoreCase));
                if (existing is null)
                {
                    LabelDatabaseFields.Add(CloneDatabaseField(availableField));
                }
                else
                {
                    existing.Name = availableField.Name;
                    existing.DisplayName = availableField.DisplayName;
                    existing.SampleValue = availableField.SampleValue;
                }
            }
        }

        if (shouldSeedLabelFields)
        {
            foreach (var field in AvailableDatabaseFields)
            {
                LabelDatabaseFields.Add(CloneDatabaseField(field));
            }
        }

        RepairTemplateFieldReferences(resolvedFieldMap);
        SelectedAvailableDatabaseField = AvailableDatabaseFields.FirstOrDefault();
        SelectedLabelDatabaseField = LabelDatabaseFields.FirstOrDefault();
        SelectedExcelField = SelectedLabelDatabaseField?.Name;
        RaiseDatabaseFieldStateChanged();
    }

    private static DatabaseField CreateDatabaseField(string fieldName, DataTable table)
    {
        var sampleValue = table.Rows.Count == 0 ? string.Empty : table.Rows[0][fieldName]?.ToString() ?? string.Empty;
        return new DatabaseField { Name = fieldName, DisplayName = fieldName, SampleValue = sampleValue };
    }

    private static DatabaseField CloneDatabaseField(DatabaseField field)
    {
        return new DatabaseField { Name = field.Name, DisplayName = field.DisplayName, SampleValue = field.SampleValue };
    }

    private static Dictionary<string, string> BuildResolvedFieldMap(IEnumerable<string> requestedFieldNames, IEnumerable<string> availableFieldNames)
    {
        var resolved = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var available = availableFieldNames
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        foreach (var requested in requestedFieldNames.Where(name => !string.IsNullOrWhiteSpace(name)).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (FieldNameResolver.TryResolveFieldName(requested, available, out var resolvedName))
            {
                resolved[requested] = resolvedName;
            }
        }

        return resolved;
    }

    private IEnumerable<string> GetReferencedTemplateFieldNames()
    {
        foreach (var item in Template.Objects)
        {
            if (string.IsNullOrWhiteSpace(item.BindingExpression))
            {
                continue;
            }

            if (FormulaBindingEvaluator.LooksLikeFormula(item.BindingExpression))
            {
                foreach (var field in FormulaBindingEvaluator.Evaluate(item.BindingExpression, PreviewRow ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)).UsedFields)
                {
                    if (!string.IsNullOrWhiteSpace(field))
                    {
                        yield return field;
                    }
                }

                continue;
            }

            foreach (var field in BindingExpressionEvaluator.GetFields(item.BindingExpression))
            {
                if (!string.IsNullOrWhiteSpace(field))
                {
                    yield return field;
                }
            }
        }
    }

    private void RepairTemplateFieldReferences(IReadOnlyDictionary<string, string> resolvedFieldMap)
    {
        if (resolvedFieldMap.Count == 0)
        {
            return;
        }

        foreach (var item in Template.Objects)
        {
            if (string.IsNullOrWhiteSpace(item.BindingExpression))
            {
                continue;
            }

            var repaired = RepairBindingExpression(item.BindingExpression, resolvedFieldMap);
            if (!string.Equals(repaired, item.BindingExpression, StringComparison.Ordinal))
            {
                item.BindingExpression = repaired;
            }
        }
    }

    private static string RepairBindingExpression(string expression, IReadOnlyDictionary<string, string> resolvedFieldMap)
    {
        if (FormulaBindingEvaluator.LooksLikeFormula(expression))
        {
            return Regex.Replace(
                expression,
                "FIELD\\(\\s*\"((?:\\\\.|[^\"])*)\"\\s*\\)",
                match =>
                {
                    var requestedField = Regex.Unescape(match.Groups[1].Value);
                    if (!TryResolveMappedFieldName(requestedField, resolvedFieldMap, out var resolvedField))
                    {
                        return match.Value;
                    }

                    return $"FIELD(\"{EscapeFormulaString(resolvedField)}\")";
                },
                RegexOptions.IgnoreCase);
        }

        return Regex.Replace(
            expression,
            "\\{([^{}]+)\\}",
            match =>
            {
                var requestedField = match.Groups[1].Value;
                return TryResolveMappedFieldName(requestedField, resolvedFieldMap, out var resolvedField)
                    ? $"{{{resolvedField}}}"
                    : match.Value;
            });
    }

    private static bool TryResolveMappedFieldName(string requestedField, IReadOnlyDictionary<string, string> resolvedFieldMap, out string resolvedField)
    {
        if (resolvedFieldMap.TryGetValue(requestedField, out var mappedField) && !string.IsNullOrWhiteSpace(mappedField))
        {
            resolvedField = mappedField;
            return true;
        }

        var normalizedRequested = FieldNameResolver.Normalize(requestedField);
        foreach (var pair in resolvedFieldMap)
        {
            if (string.Equals(FieldNameResolver.Normalize(pair.Key), normalizedRequested, StringComparison.OrdinalIgnoreCase))
            {
                resolvedField = pair.Value;
                return true;
            }
        }

        resolvedField = string.Empty;
        return false;
    }

    private void AddDatabaseField()
    {
        if (SelectedAvailableDatabaseField is null)
        {
            return;
        }

        AddLabelDatabaseField(SelectedAvailableDatabaseField);
    }

    private void AddAllDatabaseFields()
    {
        foreach (var field in AvailableDatabaseFields)
        {
            AddLabelDatabaseField(field, updateStatus: false);
        }

        StatusText = $"Added {LabelDatabaseFields.Count} field(s) for label design";
        RaiseDatabaseFieldStateChanged();
    }

    private void AddLabelDatabaseField(DatabaseField field, bool updateStatus = true)
    {
        if (LabelDatabaseFields.Any(item => string.Equals(item.Name, field.Name, StringComparison.OrdinalIgnoreCase)))
        {
            if (updateStatus)
            {
                StatusText = $"Field already added: {field.Name}";
            }
            return;
        }

        var clone = CloneDatabaseField(field);
        LabelDatabaseFields.Add(clone);
        SelectedLabelDatabaseField = clone;
        SelectedExcelField = clone.Name;
        if (updateStatus)
        {
            StatusText = $"Added label field: {field.Name}";
        }

        RaiseDatabaseFieldStateChanged();
    }

    private void RemoveDatabaseField()
    {
        if (SelectedLabelDatabaseField is null)
        {
            return;
        }

        var removedName = SelectedLabelDatabaseField.Name;
        LabelDatabaseFields.Remove(SelectedLabelDatabaseField);
        SelectedLabelDatabaseField = LabelDatabaseFields.FirstOrDefault();
        SelectedExcelField = SelectedLabelDatabaseField?.Name;
        StatusText = $"Removed label field: {removedName}";
        RaiseDatabaseFieldStateChanged();
    }

    private void ClearDatabaseFields()
    {
        LabelDatabaseFields.Clear();
        SelectedLabelDatabaseField = null;
        SelectedExcelField = null;
        StatusText = "Cleared label fields";
        RaiseDatabaseFieldStateChanged();
    }

    private void RaiseDatabaseFieldStateChanged()
    {
        RefreshObjectTreeBindingStates();
        OnPropertyChanged(nameof(AvailableDatabaseFields));
        OnPropertyChanged(nameof(LabelDatabaseFields));
        OnPropertyChanged(nameof(BindingIssues));
        OnPropertyChanged(nameof(HasBindingIssues));
        OnPropertyChanged(nameof(BindingIssuesSummary));
        ((RelayCommand)AddDatabaseFieldCommand).RaiseCanExecuteChanged();
        ((RelayCommand)AddAllDatabaseFieldsCommand).RaiseCanExecuteChanged();
        ((RelayCommand)RemoveDatabaseFieldCommand).RaiseCanExecuteChanged();
        ((RelayCommand)ClearDatabaseFieldsCommand).RaiseCanExecuteChanged();
        ((RelayCommand)AddExcelFieldCommand).RaiseCanExecuteChanged();
        ((RelayCommand)BindSelectedAsExcelFieldCommand).RaiseCanExecuteChanged();
        ((RelayCommand)RefreshExcelDataCommand).RaiseCanExecuteChanged();
    }
}
