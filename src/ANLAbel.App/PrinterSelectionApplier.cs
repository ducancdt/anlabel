using ANLAbel.Core.Enums;
using ANLAbel.Core.Geometry;
using ANLAbel.Core.Models;
using ANLAbel.Core.Printing;
using ANLAbel.Printing.PrinterProfiles;

namespace ANLAbel.App;

/// <summary>
/// Single apply outcome for designer Printer Setup and Print Preview.
/// Queue, page-source, media identity and authored millimetres are written together.
/// </summary>
public static class PrinterSelectionApplier
{
    public static void Apply(
        LabelTemplate template,
        PrinterInfo printer,
        PrinterPaperInfo paper,
        int dpi,
        LabelOrientation orientation,
        PrinterPaperInfo? physicalMedia = null,
        PaperSizeSource pageSizeSource = PaperSizeSource.Manual,
        string driverIdentity = "",
        PrinterSettingsSource settingsSource = PrinterSettingsSource.Label,
        double? offsetXMm = null,
        double? offsetYMm = null,
        double? scaleX = null,
        double? scaleY = null)
    {
        ArgumentNullException.ThrowIfNull(template);
        ArgumentNullException.ThrowIfNull(printer);
        ArgumentNullException.ThrowIfNull(paper);

        var (widthMm, heightMm) = LabelGeometry.OrientSize(paper.WidthMm, paper.HeightMm, orientation);
        var media = physicalMedia ?? paper;
        var stock = LabelStockContract.Evaluate(
            widthMm,
            heightMm,
            media.WidthMm,
            media.HeightMm,
            media.Name);
        if (!stock.IsAllowed)
        {
            throw new InvalidOperationException(stock.Diagnostic);
        }

        var dpiDecision = IndustrialPrintDpiContract.Evaluate(dpi, dpi);
        if (!dpiDecision.IsAllowed)
        {
            throw new InvalidOperationException(dpiDecision.Diagnostic);
        }

        var (nextScaleX, nextScaleY) = ResolvePrintScale(
            scaleX,
            scaleY,
            template.PrinterProfile.ScaleX,
            template.PrinterProfile.ScaleY);
        var scaleDecision = PrintScaleContract.Evaluate(nextScaleX, nextScaleY);
        if (!scaleDecision.IsAllowed)
        {
            throw new InvalidOperationException(scaleDecision.Diagnostic);
        }

        template.WidthMm = widthMm;
        template.HeightMm = heightMm;
        template.Dpi = dpi;
        template.Orientation = orientation;
        template.PrinterProfile.PrinterName = printer.Name;
        template.PrinterProfile.LabelStockName = paper.Name;
        template.PrinterProfile.PaperName = media.Name;
        template.PrinterProfile.SettingsSource = settingsSource;
        template.PrinterProfile.PaperSizeSource = pageSizeSource;
        template.PrinterProfile.LabelWidthMm = widthMm;
        template.PrinterProfile.LabelHeightMm = heightMm;
        template.PrinterProfile.PhysicalWidthMm = media.WidthMm;
        template.PrinterProfile.PhysicalHeightMm = media.HeightMm;
        template.PrinterProfile.MediaProvenance = pageSizeSource == PaperSizeSource.DriverAutomatic && physicalMedia is null
            ? PrinterMediaProvenance.Unknown
            : media.Provenance;
        template.PrinterProfile.DriverMediaIdentity = media.RawIdentity;
        template.PrinterProfile.DriverIdentity = driverIdentity;
        template.PrinterProfile.Dpi = dpi;
        if (offsetXMm is double ox)
        {
            template.PrinterProfile.OffsetXMm = ox;
        }

        if (offsetYMm is double oy)
        {
            template.PrinterProfile.OffsetYMm = oy;
        }

        template.PrinterProfile.ScaleX = nextScaleX;
        template.PrinterProfile.ScaleY = nextScaleY;
    }

    /// <summary>
    /// A missing request keeps the stored axis. Both axes unset means 1:1.
    /// One unset axis is left unset so <see cref="PrintScaleContract"/> can
    /// fail closed instead of inventing the other factor.
    /// </summary>
    private static (double ScaleX, double ScaleY) ResolvePrintScale(
        double? requestedX,
        double? requestedY,
        double currentX,
        double currentY)
    {
        var scaleX = requestedX is > 0 ? requestedX.Value : currentX;
        var scaleY = requestedY is > 0 ? requestedY.Value : currentY;
        if (scaleX == 0 && scaleY == 0)
        {
            return (PrintScaleContract.Identity, PrintScaleContract.Identity);
        }

        return (scaleX, scaleY);
    }

    public static PrinterSelectionOutcome Read(LabelTemplate template)
    {
        ArgumentNullException.ThrowIfNull(template);
        var profile = template.PrinterProfile;
        return new PrinterSelectionOutcome(
            profile.PrinterName,
            profile.PaperSizeSource,
            profile.DriverMediaIdentity ?? string.Empty,
            template.WidthMm,
            template.HeightMm,
            profile.PaperName,
            profile.SettingsSource,
            profile.PhysicalWidthMm,
            profile.PhysicalHeightMm,
            profile.MediaProvenance,
            profile.LabelStockName,
            profile.OffsetXMm,
            profile.OffsetYMm,
            profile.ScaleX,
            profile.ScaleY);
    }
}

public readonly record struct PrinterSelectionOutcome(
    string QueueName,
    PaperSizeSource PageSizeSource,
    string MediaIdentity,
    double WidthMm,
    double HeightMm,
    string PaperName,
    PrinterSettingsSource SettingsSource,
    double PhysicalWidthMm,
    double PhysicalHeightMm,
    PrinterMediaProvenance MediaProvenance,
    string LabelStockName,
    double OffsetXMm,
    double OffsetYMm,
    double ScaleX,
    double ScaleY);
