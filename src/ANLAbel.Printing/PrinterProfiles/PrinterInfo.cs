namespace ANLAbel.Printing.PrinterProfiles;

public sealed class PrinterInfo
{
    public string Name { get; init; } = string.Empty;
    public string DriverName { get; init; } = string.Empty;
    public bool IsDefault { get; init; }
    public IReadOnlyList<PrinterPaperInfo> PaperSizes { get; init; } = Array.Empty<PrinterPaperInfo>();

    public string DisplayName
    {
        get
        {
            var suffix = IsDefault ? " (Default)" : string.Empty;
            return $"{Name}{suffix}";
        }
    }
}
