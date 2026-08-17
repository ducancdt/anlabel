using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using ANLAbel.Core.Enums;
using ANLAbel.Core.Data;
using ANLAbel.Core.Mvvm;

namespace ANLAbel.Core.Models;

public sealed class LabelTemplate : ObservableObject
{
    private string _id = Guid.NewGuid().ToString("N");
    private string _name = "Untitled Label";
    private double _widthMm = 100;
    private double _heightMm = 50;
    private int _dpi = 203;
    private double _gapMm;
    private double _marginMm = 1;
    private LabelOrientation _orientation = LabelOrientation.Portrait;
    private PrinterProfile _printerProfile = new();
    private DatabaseConfig _databaseConfig = new();

    public string Id
    {
        get => _id;
        set => SetProperty(ref _id, value);
    }

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public double WidthMm
    {
        get => _widthMm;
        set => SetProperty(ref _widthMm, Math.Max(1, Math.Round(value, 2)));
    }

    public double HeightMm
    {
        get => _heightMm;
        set => SetProperty(ref _heightMm, Math.Max(1, Math.Round(value, 2)));
    }

    public int Dpi
    {
        get => _dpi;
        set => SetProperty(ref _dpi, value);
    }

    public double GapMm
    {
        get => _gapMm;
        set => SetProperty(ref _gapMm, Math.Max(0, Math.Round(value, 2)));
    }

    public double MarginMm
    {
        get => _marginMm;
        set => SetProperty(ref _marginMm, Math.Max(0, Math.Round(value, 2)));
    }

    public LabelOrientation Orientation
    {
        get => _orientation;
        set => SetProperty(ref _orientation, value);
    }

    public ObservableCollection<LabelObject> Objects { get; set; } = new();

    /// <summary>
    /// Authoring-only ruler guides. They are saved with the template and may
    /// participate in designer snapping, but never become printable scene
    /// nodes or affect rendered geometry.
    /// </summary>
    public ObservableCollection<LabelGuide> Guides { get; set; } = new();

    /// <summary>Persisted typed-record transforms applied by the data workflow.</summary>
    public ObservableCollection<DataTransformDefinition> DataTransforms { get; set; } = new();

    public PrinterProfile PrinterProfile
    {
        get => _printerProfile;
        set => SetProperty(ref _printerProfile, value);
    }

    public DatabaseConfig DatabaseConfig
    {
        get => _databaseConfig;
        set => SetProperty(ref _databaseConfig, value);
    }

    /// <summary>
    /// Forward-compatible template metadata owned by a newer feature or an
    /// extension. Unknown members are retained on a load/save round trip
    /// instead of being silently discarded by an older desktop build.
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, JsonElement> ExtensionData { get; set; } = new(StringComparer.Ordinal);
}
