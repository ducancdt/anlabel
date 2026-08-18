using ANLAbel.Core.Enums;
using ANLAbel.Core.Geometry;

namespace ANLAbel.Core.Printing;

/// <summary>
/// Value-only industrial stock policy. Catalog and typed custom sizes are
/// operator stock; office sheets and incomplete physical dimensions fail closed
/// without reading a printer driver.
/// </summary>
public static class LabelStockContract
{
    public const double OfficeSheetToleranceMm = 0.8;
    public const double MinimumEdgeMm = 8;
    public const double MaximumEdgeMm = 400;

    public static readonly (double WidthMm, double HeightMm) A4Mm = (210, 297);
    public static readonly (double WidthMm, double HeightMm) A3Mm = (297, 420);
    public static readonly (double WidthMm, double HeightMm) LetterMm = (215.9, 279.4);
    public static readonly (double WidthMm, double HeightMm) LegalMm = (215.9, 355.6);

    public static bool IsFinitePositive(double widthMm, double heightMm)
    {
        return double.IsFinite(widthMm)
            && double.IsFinite(heightMm)
            && widthMm > 0
            && heightMm > 0;
    }

    public static bool IsOfficeSheet(double widthMm, double heightMm)
    {
        if (!IsFinitePositive(widthMm, heightMm))
        {
            return false;
        }

        return MatchesSheet(widthMm, heightMm, A4Mm)
            || MatchesSheet(widthMm, heightMm, A3Mm)
            || MatchesSheet(widthMm, heightMm, LetterMm)
            || MatchesSheet(widthMm, heightMm, LegalMm);
    }

    public static bool NameClaimsOfficeSheet(string? paperName)
    {
        if (string.IsNullOrWhiteSpace(paperName))
        {
            return false;
        }

        return paperName.Contains("A4", StringComparison.OrdinalIgnoreCase)
            && paperName.Contains("297", StringComparison.Ordinal);
    }

    public static bool MatchesAuthoredLabel(
        double physicalWidthMm,
        double physicalHeightMm,
        double labelWidthMm,
        double labelHeightMm)
    {
        if (!IsFinitePositive(physicalWidthMm, physicalHeightMm)
            || !IsFinitePositive(labelWidthMm, labelHeightMm))
        {
            return false;
        }

        if (SameSize(physicalWidthMm, physicalHeightMm, labelWidthMm, labelHeightMm))
        {
            return true;
        }

        var portrait = LabelGeometry.OrientSize(physicalWidthMm, physicalHeightMm, LabelOrientation.Portrait);
        var landscape = LabelGeometry.OrientSize(physicalWidthMm, physicalHeightMm, LabelOrientation.Landscape);
        return SameSize(portrait.WidthMm, portrait.HeightMm, labelWidthMm, labelHeightMm)
            || SameSize(landscape.WidthMm, landscape.HeightMm, labelWidthMm, labelHeightMm);
    }

    public static PaperSizeSource SourceForOperatorStock() => PaperSizeSource.Manual;

    public static LabelStockDecision Evaluate(
        double labelWidthMm,
        double labelHeightMm,
        double physicalWidthMm,
        double physicalHeightMm,
        string? paperName = null)
    {
        if (!IsFinitePositive(labelWidthMm, labelHeightMm))
        {
            return LabelStockDecision.Blocked(
                "Label stock width/height must be finite and greater than zero.");
        }

        if (!IsIndustrialEdge(labelWidthMm) || !IsIndustrialEdge(labelHeightMm))
        {
            return LabelStockDecision.Blocked(
                $"Label stock must stay between {MinimumEdgeMm:0} and {MaximumEdgeMm:0} mm on each edge. Enter the physical die size, not a poster or office sheet.");
        }

        var physicalUnset = physicalWidthMm == 0 && physicalHeightMm == 0;
        if (!physicalUnset && (physicalWidthMm == 0 || physicalHeightMm == 0
            || !double.IsFinite(physicalWidthMm) || !double.IsFinite(physicalHeightMm)))
        {
            return LabelStockDecision.Blocked(
                "Physical stock is incomplete (only one axis is set). Enter both width and height in millimetres.");
        }

        if (!physicalUnset && (!IsIndustrialEdge(physicalWidthMm) || !IsIndustrialEdge(physicalHeightMm)))
        {
            return LabelStockDecision.Blocked(
                $"Physical stock must stay between {MinimumEdgeMm:0} and {MaximumEdgeMm:0} mm on each edge.");
        }

        if (IsOfficeSheet(labelWidthMm, labelHeightMm)
            || (!physicalUnset && IsOfficeSheet(physicalWidthMm, physicalHeightMm))
            || NameClaimsOfficeSheet(paperName))
        {
            return LabelStockDecision.Blocked(
                "A4/Letter/Legal/A3 office sheets are not thermal label stock. Choose a catalog label size or enter the physical die size in millimetres.");
        }

        if (!physicalUnset
            && !MatchesAuthoredLabel(physicalWidthMm, physicalHeightMm, labelWidthMm, labelHeightMm))
        {
            return LabelStockDecision.Blocked(
                $"Physical stock {physicalWidthMm:0.##} × {physicalHeightMm:0.##} mm does not match the authored label {labelWidthMm:0.##} × {labelHeightMm:0.##} mm (including orientation swap). Fix Printer Setup before printing.");
        }

        return LabelStockDecision.Allowed;
    }

    public static bool IsIndustrialEdge(double edgeMm)
    {
        return double.IsFinite(edgeMm)
            && edgeMm >= MinimumEdgeMm
            && edgeMm <= MaximumEdgeMm;
    }

    private static bool MatchesSheet(double widthMm, double heightMm, (double WidthMm, double HeightMm) sheet)
    {
        return (Near(widthMm, sheet.WidthMm) && Near(heightMm, sheet.HeightMm))
            || (Near(widthMm, sheet.HeightMm) && Near(heightMm, sheet.WidthMm));
    }

    private static bool SameSize(double widthMm, double heightMm, double otherWidthMm, double otherHeightMm)
    {
        return Near(widthMm, otherWidthMm) && Near(heightMm, otherHeightMm);
    }

    private static bool Near(double left, double right)
    {
        return Math.Abs(left - right) <= OfficeSheetToleranceMm;
    }
}

public readonly record struct LabelStockDecision(bool IsAllowed, string Diagnostic)
{
    public static LabelStockDecision Allowed { get; } = new(true, string.Empty);

    public static LabelStockDecision Blocked(string diagnostic)
        => new(false, diagnostic);
}
