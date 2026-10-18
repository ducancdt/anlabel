using ANLAbel.Core.Models;

namespace ANLAbel.Core.Barcode;

/// <summary>
/// Bearer bar thickness: a multiple of the effective (whole-dot) narrow bar
/// when <see cref="LabelObject.BearerBarThicknessModules"/> is set, so it
/// follows the symbol's X at any DPI; otherwise the authored millimetres.
/// </summary>
public static class BearerBarContract
{
    public const double DefaultThicknessMm = 1.0;

    public static double ResolveThicknessMm(LabelObject item, double? effectiveModuleWidthMm)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (item.BearerBarThicknessModules > 0 && effectiveModuleWidthMm is > 0)
        {
            return item.BearerBarThicknessModules * effectiveModuleWidthMm.Value;
        }

        return item.BearerBarThicknessMm > 0 ? item.BearerBarThicknessMm : DefaultThicknessMm;
    }
}
