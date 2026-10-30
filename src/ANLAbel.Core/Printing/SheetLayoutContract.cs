using ANLAbel.Core.Enums;
using ANLAbel.Core.Models;

namespace ANLAbel.Core.Printing;

/// <summary>
/// Several labels on one physical sheet, as NiceLabel 10 prints them
/// (<c>WebPageLayout</c>: counts, gaps, fill order and start corner). The
/// grid starts at the profile offset; every cell must lie on the sheet.
/// </summary>
public static class SheetLayoutContract
{
    private const double FitToleranceMm = 0.5;

    public static bool IsSheet(int labelsAcross, int labelsDown)
        => Math.Max(1, labelsAcross) * Math.Max(1, labelsDown) > 1;

    public static LabelStockDecision Evaluate(
        double labelWidthMm,
        double labelHeightMm,
        double physicalWidthMm,
        double physicalHeightMm,
        double offsetXMm,
        double offsetYMm,
        int labelsAcross,
        int labelsDown,
        double horizontalGapMm,
        double verticalGapMm)
    {
        if (!IsSheet(labelsAcross, labelsDown))
        {
            return LabelStockDecision.Allowed;
        }

        return EvaluatePlacement(
            labelWidthMm,
            labelHeightMm,
            physicalWidthMm,
            physicalHeightMm,
            offsetXMm,
            offsetYMm,
            labelsAcross,
            labelsDown,
            horizontalGapMm,
            verticalGapMm);
    }

    /// <summary>
    /// The labels of one page — a grid, or a single label on a larger sheet
    /// such as A5 — must lie on the sheet from the left/top margins
    /// (NiceLabel 10 Label Dimensions › Margins and Labels Across).
    /// </summary>
    public static LabelStockDecision EvaluatePlacement(
        double labelWidthMm,
        double labelHeightMm,
        double physicalWidthMm,
        double physicalHeightMm,
        double offsetXMm,
        double offsetYMm,
        int labelsAcross,
        int labelsDown,
        double horizontalGapMm,
        double verticalGapMm)
    {
        labelsAcross = Math.Max(1, labelsAcross);
        labelsDown = Math.Max(1, labelsDown);
        if (!LabelStockContract.IsFinitePositive(physicalWidthMm, physicalHeightMm))
        {
            return LabelStockDecision.Blocked(
                $"A sheet of {labelsAcross} × {labelsDown} labels needs the physical sheet size. Enter the sheet width and height in millimetres.");
        }

        if (!double.IsFinite(horizontalGapMm) || !double.IsFinite(verticalGapMm)
            || horizontalGapMm < 0 || verticalGapMm < 0)
        {
            return LabelStockDecision.Blocked("The gaps between labels must be zero or more millimetres.");
        }

        var gridWidth = ExtentMm(offsetXMm, labelWidthMm, labelsAcross, horizontalGapMm);
        var gridHeight = ExtentMm(offsetYMm, labelHeightMm, labelsDown, verticalGapMm);
        if (offsetXMm < -FitToleranceMm
            || offsetYMm < -FitToleranceMm
            || gridWidth > physicalWidthMm + FitToleranceMm
            || gridHeight > physicalHeightMm + FitToleranceMm)
        {
            return LabelStockDecision.Blocked(
                $"The sheet layout of {labelsAcross} × {labelsDown} labels needs {gridWidth:0.##} × {gridHeight:0.##} mm but the sheet is {physicalWidthMm:0.##} × {physicalHeightMm:0.##} mm. Reduce the label count, gaps or margins.");
        }

        return LabelStockDecision.Allowed;
    }

    /// <summary>
    /// Extent of the margin plus <paramref name="count"/> labels and the gaps
    /// between them along one axis; the sheet minus this is the right or
    /// bottom margin NiceLabel shows.
    /// </summary>
    public static double ExtentMm(double marginMm, double labelMm, int count, double gapMm)
    {
        count = Math.Max(1, count);
        return marginMm + count * labelMm + (count - 1) * gapMm;
    }

    /// <summary>
    /// Cells left empty at the start of the first sheet (NiceLabel 10 print
    /// "Number of skipped labels"), so a partly used sheet of labels can be
    /// printed again: never negative and always leaving one cell to print on.
    /// A label per page has no cells to skip.
    /// </summary>
    public static int ClampSkippedLabels(int skippedLabels, int labelsPerPage)
        => labelsPerPage <= 1 ? 0 : Math.Clamp(skippedLabels, 0, labelsPerPage - 1);

    /// <summary>Sheets needed for <paramref name="labels"/> after the skipped cells.</summary>
    public static int SheetCount(int labels, int labelsPerPage, int skippedLabels)
    {
        labelsPerPage = Math.Max(1, labelsPerPage);
        var skip = ClampSkippedLabels(skippedLabels, labelsPerPage);
        return labels <= 0 ? 0 : (labels + skip + labelsPerPage - 1) / labelsPerPage;
    }

    /// <summary>
    /// How many labels fit along one axis of the sheet after the margin
    /// (NiceLabel 10 "Automatic labels across"); at least one.
    /// </summary>
    public static int FitCount(double sheetMm, double marginMm, double labelMm, double gapMm)
    {
        if (!double.IsFinite(sheetMm) || !double.IsFinite(marginMm) || !double.IsFinite(labelMm) || !double.IsFinite(gapMm)
            || labelMm <= 0 || gapMm < 0)
        {
            return 1;
        }

        var count = (int)Math.Floor((sheetMm - marginMm + gapMm + FitToleranceMm) / (labelMm + gapMm));
        return Math.Max(1, count);
    }

    /// <summary>
    /// True when each page sent to the driver is the whole physical sheet: a
    /// grid of labels, or one label on a larger sheet chosen manually (an
    /// office sheet such as A5 or A4 with margins), as NiceLabel 10 prints a
    /// sheet. A page of only the label's size would leave the driver to place
    /// or scale it and would cut off content moved by the margins. Label
    /// stock of the label's own size (also turned 90°) keeps label pages.
    /// </summary>
    public static bool PrintsWholeSheet(PrinterProfile profile, double labelWidthMm, double labelHeightMm)
    {
        ArgumentNullException.ThrowIfNull(profile);
        if (IsSheet(profile.LabelsAcross, profile.LabelsDown))
        {
            return true;
        }

        if (profile.SettingsSource != PrinterSettingsSource.Label
            || profile.PaperSizeSource != PaperSizeSource.Manual
            || !LabelStockContract.IsFinitePositive(profile.PhysicalWidthMm, profile.PhysicalHeightMm)
            || !LabelStockContract.IsFinitePositive(labelWidthMm, labelHeightMm))
        {
            return false;
        }

        var width = profile.PhysicalWidthMm;
        var height = profile.PhysicalHeightMm;
        return width >= labelWidthMm - FitToleranceMm
            && height >= labelHeightMm - FitToleranceMm
            && (width > labelWidthMm + FitToleranceMm || height > labelHeightMm + FitToleranceMm);
    }

    /// <summary>
    /// Top-left of the label at <paramref name="index"/> on its page,
    /// relative to the first cell (the profile offset is applied by the
    /// renderer).
    /// </summary>
    public static (double XMm, double YMm) CellOrigin(
        int index,
        double labelWidthMm,
        double labelHeightMm,
        int labelsAcross,
        int labelsDown,
        double horizontalGapMm,
        double verticalGapMm,
        bool fillDownFirst,
        bool startFromRight,
        bool startFromBottom)
    {
        labelsAcross = Math.Max(1, labelsAcross);
        labelsDown = Math.Max(1, labelsDown);
        var slot = index % (labelsAcross * labelsDown);
        var column = fillDownFirst ? slot / labelsDown : slot % labelsAcross;
        var row = fillDownFirst ? slot % labelsDown : slot / labelsAcross;
        if (startFromRight)
        {
            column = labelsAcross - 1 - column;
        }

        if (startFromBottom)
        {
            row = labelsDown - 1 - row;
        }

        return (column * (labelWidthMm + horizontalGapMm), row * (labelHeightMm + verticalGapMm));
    }
}
