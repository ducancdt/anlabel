using ANLAbel.Core.Mvvm;

namespace ANLAbel.Core.Models;

public sealed class DatabaseField : ObservableObject
{
    private string _name = string.Empty;
    private string _displayName = string.Empty;
    private string _sampleValue = string.Empty;

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
    /// Members written by a newer ANLAbel that this version does not know. They
    /// are kept on a load/save round trip instead of being dropped (audit
    /// 2026-10-13); the template-level bag alone did not cover nested parts.
    /// </summary>
    [System.Text.Json.Serialization.JsonExtensionData]
    public Dictionary<string, System.Text.Json.JsonElement> ExtensionData { get; set; } = new(StringComparer.Ordinal);
}
