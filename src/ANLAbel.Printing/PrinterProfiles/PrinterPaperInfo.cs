namespace ANLAbel.Printing.PrinterProfiles;

using ANLAbel.Core.Enums;

public sealed class PrinterPaperInfo
{
    public string Name { get; init; } = string.Empty;
    public double WidthMm { get; init; }
    public double HeightMm { get; init; }
    public string Category { get; init; } = string.Empty;
    public PaperSizeSourceKind Source { get; init; } = PaperSizeSourceKind.StandardCatalog;
    public string RawIdentity { get; init; } = string.Empty;

    public PrinterMediaProvenance Provenance => Source switch
    {
        PaperSizeSourceKind.DriverWpf => PrinterMediaProvenance.DriverWpf,
        PaperSizeSourceKind.DriverWin32 => PrinterMediaProvenance.DriverWin32,
        PaperSizeSourceKind.VendorStock => PrinterMediaProvenance.VendorStock,
        PaperSizeSourceKind.StandardCatalog => PrinterMediaProvenance.BuiltInStock,
        PaperSizeSourceKind.UserCustom => PrinterMediaProvenance.UserCustom,
        _ => PrinterMediaProvenance.Unknown
    };

    public string DisplayName => WidthMm > 0 && HeightMm > 0
        ? $"{Name} - {WidthMm:0.##} x {HeightMm:0.##} mm"
        : Name;
}

public enum PaperSizeSourceKind
{
    Unknown,
    DriverWpf,
    DriverWin32,
    VendorStock,
    StandardCatalog,
    UserCustom
}
