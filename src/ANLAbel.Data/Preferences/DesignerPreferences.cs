namespace ANLAbel.Data.Preferences;

public sealed class DesignerPreferences
{
    public bool SnapToObjects { get; set; } = true;
    public bool SnapToGrid { get; set; }
    public double GridStepMm { get; set; } = 1.0;
    public bool ShowGrid { get; set; } = true;
}
