namespace ANLAbel.Core.Enums;

/// <summary>
/// Identifies who owns a media definition. Driver media and software stocks
/// are deliberately separate because a stock preset is not proof that a
/// Windows printer queue can accept that physical page size.
/// </summary>
public enum PrinterMediaProvenance
{
    Unknown,
    DriverWpf,
    DriverWin32,
    VendorStock,
    BuiltInStock,
    UserCustom
}
