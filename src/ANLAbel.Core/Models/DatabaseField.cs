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
}