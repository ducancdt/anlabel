namespace ANLAbel.Core.Barcode;

public sealed record QrSizingResult
{
    public bool IsValid { get; init; }
    public string? ErrorMessage { get; init; }
    public int Version { get; init; }
    public int ModuleCount { get; init; }
    public int TotalModules { get; init; }
    public int ModuleSizePx { get; init; }
    public int FinalSizePx { get; init; }
    public double FinalSizeMm { get; init; }
    public double WidthMm { get; init; }
    public double HeightMm { get; init; }

    public static QrSizingResult Invalid(string errorMessage) => new()
    {
        IsValid = false,
        ErrorMessage = errorMessage
    };
}