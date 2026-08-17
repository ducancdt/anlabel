using ANLAbel.Core.Enums;
using ANLAbel.Core.Mvvm;

namespace ANLAbel.Core.Models;

/// <summary>
/// Persistent authoring metadata for one ruler guide. Guides are useful for
/// snapping/alignment but are deliberately not printable scene objects.
/// </summary>
public sealed class LabelGuide : ObservableObject
{
    private string _id = Guid.NewGuid().ToString("N");
    private LabelGuideOrientation _orientation = LabelGuideOrientation.Vertical;
    private double _positionMm;
    private bool _isLocked;
    private bool _isVisible = true;

    public string Id
    {
        get => _id;
        set => SetProperty(ref _id, string.IsNullOrWhiteSpace(value) ? Guid.NewGuid().ToString("N") : value);
    }

    public LabelGuideOrientation Orientation
    {
        get => _orientation;
        set => SetProperty(ref _orientation, value);
    }

    public double PositionMm
    {
        get => _positionMm;
        set => SetProperty(ref _positionMm, NormalizePosition(value));
    }

    public bool IsLocked
    {
        get => _isLocked;
        set => SetProperty(ref _isLocked, value);
    }

    public bool IsVisible
    {
        get => _isVisible;
        set => SetProperty(ref _isVisible, value);
    }

    private static double NormalizePosition(double value)
        => double.IsFinite(value) ? Math.Round(Math.Max(0, value), 3, MidpointRounding.AwayFromZero) : 0;
}
