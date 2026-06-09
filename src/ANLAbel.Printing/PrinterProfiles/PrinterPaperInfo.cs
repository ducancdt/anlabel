namespace ANLAbel.Printing.PrinterProfiles;

public sealed class PrinterPaperInfo
{
    public string Name { get; init; } = string.Empty;
    public double WidthMm { get; init; }
    public double HeightMm { get; init; }
    public string Category { get; init; } = string.Empty;
    public PaperSizeSourceKind Source { get; init; } = PaperSizeSourceKind.StandardCatalog;

    public string DisplayName => WidthMm > 0 && HeightMm > 0
        ? $"{Name} - {WidthMm:0.##} x {HeightMm:0.##} mm"
        : Name;
}

public enum PaperSizeSourceKind
{
    StandardCatalog,
    UserCustom
}