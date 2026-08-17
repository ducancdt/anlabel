using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ANLAbel.Core.Data;

namespace ANLAbel.App.ViewModels;

/// <summary>Draft-only transform editor; Core remains the formula/evaluation owner.</summary>
public sealed class DataWorkspaceViewModel : INotifyPropertyChanged
{
    private readonly Func<DataRecord?> _sample;
    private readonly Action<IReadOnlyList<DataTransformDefinition>> _apply;
    private DataTransformDefinition? _selected;
    private string _outputName = "";
    private string _formula = "";
    private string _status = "Select a sample row, then add or edit a transform.";
    private string _result = "";
    private string _lineage = "";
    public DataWorkspaceViewModel(Func<DataRecord?> sample, IEnumerable<DataTransformDefinition> committed, Action<IReadOnlyList<DataTransformDefinition>> apply)
    {
        _sample = sample; _apply = apply;
        Drafts = new ObservableCollection<DataTransformDefinition>(committed);
        Fields = new ObservableCollection<string>();
        RefreshSample();
    }
    public event PropertyChangedEventHandler? PropertyChanged;
    public ObservableCollection<DataTransformDefinition> Drafts { get; }
    public ObservableCollection<string> Fields { get; }
    public DataTransformDefinition? Selected { get => _selected; set { if (Set(ref _selected, value) && value is not null) { OutputName = value.Name; Formula = value.Formula; } } }
    public string OutputName { get => _outputName; set => Set(ref _outputName, value); }
    public string Formula { get => _formula; set => Set(ref _formula, value); }
    public string Status { get => _status; private set => Set(ref _status, value); }
    public string Result { get => _result; private set => Set(ref _result, value); }
    public string Lineage { get => _lineage; private set => Set(ref _lineage, value); }
    public bool CanApply => _sample() is not null && ValidateInternal(out _);
    public void RefreshSample()
    {
        Fields.Clear(); var sample = _sample();
        if (sample is null) { Status = "No sample row is selected. Select an imported Excel/CSV row first."; Result = Lineage = ""; Changed(nameof(CanApply)); return; }
        foreach (var field in sample.Values.Keys.OrderBy(x => x, StringComparer.OrdinalIgnoreCase)) Fields.Add(field);
        Validate();
    }
    public void Add() { var draft = new DataTransformDefinition("DerivedField", "CONCAT(FIELD(\"Field\"))"); Drafts.Add(draft); Selected = draft; }
    public void Remove() { if (Selected is null) return; Drafts.Remove(Selected); Selected = null; Validate(); }
    public void CommitEditor() { if (Selected is null) { Add(); } if (Selected is not null) { var index = Drafts.IndexOf(Selected); var updated = new DataTransformDefinition(OutputName.Trim(), Formula.Trim()); Drafts[index] = updated; Selected = updated; } Validate(); }
    public bool Validate()
    {
        var valid = ValidateInternal(out var evaluation);
        if (evaluation is null) { Result = Lineage = ""; Changed(nameof(CanApply)); return valid; }
        Status = valid ? "Valid draft. Apply commits all definitions atomically." : string.Join(" ", evaluation.Errors);
        Result = valid && Selected is not null && evaluation.Record.TryGetValue(Selected.Name, out var value) ? value ?? "" : "";
        Lineage = valid ? string.Join("; ", evaluation.Lineage.Select(x => $"{x.OutputField} ← {string.Join(", ", x.InputFields)}")) : "";
        Changed(nameof(CanApply)); return valid;
    }
    public bool Apply() { if (!ValidateInternal(out var evaluation) || evaluation is null) { Validate(); return false; } _apply(Drafts.ToArray()); Status = "Applied to the current template. Save the template to persist it."; Changed(nameof(CanApply)); return true; }
    private bool ValidateInternal(out DataTransformResult? evaluation)
    {
        var sample = _sample(); if (sample is null) { evaluation = null; return false; }
        evaluation = DataTransformPipeline.Evaluate(sample, Drafts); return evaluation.IsValid;
    }
    private bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null) { if (EqualityComparer<T>.Default.Equals(field, value)) return false; field = value; Changed(name); return true; }
    private void Changed([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
