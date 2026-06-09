using System.Text;

namespace ANLAbel.Core.Barcode;

public static class QrAutoSizeHelper
{
    private const int ShortDataBytes = 16;
    private const double MinimumReadableSizeMm = 6;
    private const double MaximumShortDataSizeMm = 10;
    private const double MinimumReadableModuleMm = 0.25;

    public static double? CalculateRequiredSizeMm(
        string? data,
        double currentWidthMm,
        double currentHeightMm,
        QrErrorCorrection errorCorrection,
        int moduleSizePx,
        int quietZoneModules,
        int dpi,
        IQrCapacityProvider? capacityProvider = null,
        double? maxSizeMm = null)
    {
        if (moduleSizePx <= 0 || dpi <= 0 || quietZoneModules < 0)
        {
            return null;
        }

        var value = data ?? string.Empty;
        capacityProvider ??= new QrCapacityTable();
        var version = Enumerable.Range(QrVersionHelper.MinVersion, QrVersionHelper.MaxVersion)
            .FirstOrDefault(candidate => capacityProvider.CanEncodeByteMode(value, candidate, errorCorrection));
        if (version == 0)
        {
            return null;
        }

        var byteCount = Math.Max(1, Encoding.UTF8.GetByteCount(value));
        var totalModules = QrVersionHelper.GetModuleCount(version) + quietZoneModules * 2;
        var floorSizeMm = Math.Max(CalculateMinimumReadableSizeMm(maxSizeMm), totalModules * MinimumReadableModuleMm);
        var maxUsableSizeMm = maxSizeMm is > 0
            ? Math.Max(1, maxSizeMm.Value)
            : Math.Max(Math.Max(currentWidthMm, currentHeightMm), floorSizeMm + 24);

        var maxCapacity = Math.Max(ShortDataBytes + 1, capacityProvider.GetByteModeCapacity(QrVersionHelper.MaxVersion, errorCorrection));
        var adjustedBytes = Math.Max(0, byteCount - ShortDataBytes);
        var adjustedCapacity = Math.Max(1, maxCapacity - ShortDataBytes);
        var dataRatio = Math.Sqrt(Math.Min(1, adjustedBytes / (double)adjustedCapacity));
        var versionRatio = (version - QrVersionHelper.MinVersion) / (double)(QrVersionHelper.MaxVersion - QrVersionHelper.MinVersion);
        var growthRatio = Math.Max(dataRatio, versionRatio);

        var targetSizeMm = floorSizeMm + growthRatio * Math.Max(0, maxUsableSizeMm - floorSizeMm);
        targetSizeMm = Math.Min(targetSizeMm, maxUsableSizeMm);

        return Math.Round(Math.Max(1, targetSizeMm), 2);
    }

    public static double? CalculateFixedSizeMm(
        int fixedVersion,
        int moduleSizePx,
        int quietZoneModules,
        int dpi,
        double? maxSizeMm = null)
    {
        if (!QrVersionHelper.IsValidVersion(fixedVersion) || moduleSizePx <= 0 || dpi <= 0 || quietZoneModules < 0)
        {
            return null;
        }

        var totalModules = QrVersionHelper.GetModuleCount(fixedVersion) + quietZoneModules * 2;
        var targetSizeMm = totalModules * moduleSizePx / (double)dpi * 25.4;
        if (maxSizeMm is > 0)
        {
            targetSizeMm = Math.Min(targetSizeMm, maxSizeMm.Value);
        }
        return Math.Round(Math.Max(1, targetSizeMm), 2);
    }

    private static double CalculateMinimumReadableSizeMm(double? maxSizeMm)
    {
        if (maxSizeMm is not > 0)
        {
            return 8;
        }

        return Math.Clamp(maxSizeMm.Value * 0.16, MinimumReadableSizeMm, MaximumShortDataSizeMm);
    }
}
