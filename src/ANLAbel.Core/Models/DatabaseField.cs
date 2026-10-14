using ANLAbel.Core.Mvvm;

namespace ANLAbel.Core.Models;

public sealed class DatabaseField : ObservableObject
{
    private string _name = string.Empty;
    private string _displayName = string.Empty;
    private string _sampleValue = string.Empty;
    private int _sourceColumn;

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public string DisplayName
    {
        get => string.IsNullOrWhiteSpace(_displayName) ? Name : _displayName;
        set => SetProperty(ref _displayName, value);
    }

    public string SampleValue
    {
        get => _sampleValue;
        set => SetProperty(ref _sampleValue, value);
    }

    /// <summary>
    /// The field's 1-based column in the data file at the last import, so the next
    /// import can name the column a missing field had (DB03); columns with neither
    /// a header nor data are not fields, so a field's place in the list is not its
    /// column. 0 when unknown (saved by an older ANLAbel); not written then, so an
    /// older template still loads and saves byte for byte.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault)]
    public int SourceColumn
    {
        get => _sourceColumn;
        set => SetProperty(ref _sourceColumn, value);
    }

    /// <summary>
    /// Members written by a newer ANLAbel that this version does not know. They
    /// are kept on a load/save round trip instead of being dropped (audit
    /// 2026-10-13); the template-level bag alone did not cover nested parts.
    /// </summary>
    [System.Text.Json.Serialization.JsonExtensionData]
    public Dictionary<string, System.Text.Json.JsonElement> ExtensionData { get; set; } = new(StringComparer.Ordinal);
}
