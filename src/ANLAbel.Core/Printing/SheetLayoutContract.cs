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

        if (!LabelStockContract.IsFinitePositive(physicalWidthMm, physicalHeightMm))
        {
            return LabelStockDecision.Blocked(
                $"A sheet of {labelsAcross} × {labelsDown} labels needs the physical sheet size. Enter the sheet width and height in millimetres.");
        }

        var gridWidth = offsetXMm + labelsAcross * labelWidthMm + (labelsAcross - 1) * horizontalGapMm;
        var gridHeight = offsetYMm + labelsDown * labelHeightMm + (labelsDown - 1) * verticalGapMm;
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
