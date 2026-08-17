using ANLAbel.Core.Models;

namespace ANLAbel.Core.Barcode;

/// <summary>
/// Single, side-effect-free geometry decision for square 2D barcode objects.
/// The model, designer and data-bound preview can all ask the same question
/// without reimplementing QR/Data Matrix sizing rules or tolerances.
/// </summary>
public static class QrObjectGeometryContract
{
    public const double SizeToleranceMm = 0.05;

    /// <summary>
    /// Resolves the authored square size for the current object settings.
    /// For an unbound object <paramref name="data"/> is its static text. For a
    /// bound object callers pass the resolved preview-row value; passing null
    /// intentionally leaves AutoSizeByData unchanged until a row is available.
    /// </summary>
    public static double? ResolveTargetSizeMm(
        LabelObject item,
        string? data,
        double? maxSizeMm = null,
        IQrCapacityProvider? capacityProvider = null)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (!item.IsSquare2DCodeLike())
        {
            return null;
        }

        if (item.QrSizingMode == QrSizingMode.FixedVersionAndModuleSize)
        {
            return QrAutoSizeHelper.CalculateFixedSizeMm(
                item.QrFixedVersion,
                item.QrModuleSizePx,
                item.QrQuietZoneModules,
                item.QrDpi,
                maxSizeMm);
        }

        if (data is null)
        {
            return null;
        }

        return QrAutoSizeHelper.CalculateRequiredSizeMm(
            data,
            item.WidthMm,
            item.HeightMm,
            item.QrErrorCorrection,
            item.QrModuleSizePx,
            item.QrQuietZoneModules,
            item.QrDpi,
            capacityProvider,
            maxSizeMm);
    }

    public static bool HasMeaningfulSizeDelta(LabelObject item, double targetSizeMm)
    {
        ArgumentNullException.ThrowIfNull(item);
        return !double.IsFinite(targetSizeMm)
            || targetSizeMm <= 0
            || Math.Abs(item.WidthMm - targetSizeMm) > SizeToleranceMm
            || Math.Abs(item.HeightMm - targetSizeMm) > SizeToleranceMm;
    }
}
