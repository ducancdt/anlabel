namespace ANLAbel.Project.NiceLabel10;

/// <summary>
/// Printer and media facts a NiceLabel 10 label stores, read from the
/// format XML (see <c>docs/NICELABEL_10_REVERSE_ENGINEERING.md</c> §10).
/// Lengths are millimetres.
/// </summary>
/// <param name="PageWidthMm"><c>Media/Width</c>: the page or roll width.</param>
/// <param name="PageHeightMm"><c>Media/Height</c>: the page or roll length.</param>
/// <param name="PaperFormatId">
/// <c>Media/PaperFormat</c>: the Windows <c>DMPAPER_*</c> id (1 Letter, 9 A4,
/// 256 user-defined), equal to the DEVMODE <c>dmPaperSize</c> whenever the
/// DEVMODE carries one.
/// </param>
/// <param name="PaperName"><c>Media/PaperDetails/Name</c>, as the driver named the paper.</param>
/// <param name="MediaKind">Vendor <c>MediaType</c>: Unknown 0, Sheet 1, Roll 2 (default Roll).</param>
/// <param name="OffsetXMm"><c>PageLayout/HorizontalOffset</c>: left margin of the first label on the page.</param>
/// <param name="OffsetYMm"><c>PageLayout/VerticalOffset</c>: top margin of the first label on the page.</param>
/// <param name="LabelsAcross"><c>PageLayout/HorizontalCount</c> (1 for a single layout).</param>
/// <param name="LabelsDown"><c>PageLayout/VerticalCount</c> (1 for a single layout).</param>
/// <param name="StockName"><c>PageLayout/StockName</c>: a vendor label-stock preset, when chosen.</param>
/// <param name="PrinterName"><c>PrintScenario/Printer/Name</c>: the authoring machine's queue.</param>
/// <param name="DriverName"><c>PrintScenario/Printer/DriverName</c>.</param>
/// <param name="UseDefaultPrinter"><c>PrintScenario/UseDefaultPrinter</c>: print to the Windows default printer.</param>
/// <param name="UseDriverSettings">
/// <c>Printer/UseDefaultPrinterSettings</c>: take paper size and resolution
/// from the printer driver rather than from the label.
/// </param>
/// <param name="PrintUpsideDown"><c>PrintScenario/PrintRotation</c> is UpsideDown (vendor <c>PrintRotationType</c> 2).</param>
/// <param name="DriverDpi">
/// DEVMODE <c>dmPrintQuality</c> (with <c>DM_PRINTQUALITY</c> set): the
/// driver resolution the label was authored for; 0 when not stored.
/// </param>
/// <param name="HorizontalGapMm"><c>PageLayout/HorizontalGap</c>: space between label columns.</param>
/// <param name="VerticalGapMm"><c>PageLayout/VerticalGap</c>: space between label rows.</param>
/// <param name="WebSettings">
/// <c>PageLayout/WebSettings</c> flags (vendor <c>WebSettings</c>): Horizontal 1,
/// Vertical 2 (fill order), Top 4, Bottom 8, Left 0x10, Right 0x20 (start corner).
/// </param>
public sealed record NiceLabel10PrintSetup(
    double PageWidthMm,
    double PageHeightMm,
    int PaperFormatId,
    string? PaperName,
    int MediaKind,
    double OffsetXMm,
    double OffsetYMm,
    int LabelsAcross,
    int LabelsDown,
    string? StockName,
    string? PrinterName,
    string? DriverName,
    bool UseDefaultPrinter,
    bool UseDriverSettings,
    bool PrintUpsideDown,
    int DriverDpi,
    double HorizontalGapMm = 0,
    double VerticalGapMm = 0,
    int WebSettings = 0)
{
    /// <summary>Labels the vendor places on one page or roll section.</summary>
    public int LabelsPerPage => Math.Max(1, LabelsAcross) * Math.Max(1, LabelsDown);
}

/// <summary>How an imported NiceLabel 10 label's media was carried into ANLAbel.</summary>
public enum NiceLabelMediaSetup
{
    /// <summary>No printer/media facts were stored.</summary>
    None,

    /// <summary>One label per page on label stock: the physical stock and DPI were applied.</summary>
    LabelStock,

    /// <summary>The label takes paper size and resolution from the printer driver, as in NiceLabel.</summary>
    DriverSettings,

    /// <summary>
    /// One label on an office sheet (A4/Letter/Legal/A3): the sheet was
    /// applied as the physical media, as in NiceLabel.
    /// </summary>
    OfficeSheet,

    /// <summary>
    /// Several labels per page: the sheet and the label grid were applied,
    /// as in NiceLabel.
    /// </summary>
    MultipleLabelsPerPage
}

/// <summary>
/// What the importer did with the stored printer/media facts, for the
/// application to explain to the operator.
/// </summary>
public sealed record NiceLabelPrinterSetupResult(
    NiceLabelMediaSetup Setup,
    string? AuthoredPrinterName,
    double PageWidthMm,
    double PageHeightMm,
    int LabelsAcross,
    int LabelsDown,
    int AppliedDpi);
