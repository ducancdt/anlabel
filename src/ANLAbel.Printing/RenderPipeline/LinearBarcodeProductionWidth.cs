using ANLAbel.Barcode.Options;
using ANLAbel.Barcode.Renderers;
using ANLAbel.Core.Barcode;
using ANLAbel.Core.Enums;
using ANLAbel.Core.Models;

namespace ANLAbel.Printing.RenderPipeline;

/// <summary>
/// Shared production width for linear barcodes: FrameOwned uses authored
/// <see cref="LabelObject.WidthMm"/>; SizedFromX uses quantized X × pure
/// logical module count via the shipped encoder/preflight seams.
/// </summary>
public static class LinearBarcodeProductionWidth
{
    public static bool IsLinearBarcodeObject(LabelObject item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return item.Type == ObjectType.BarcodeCode128 && !item.IsSquare2DCodeLike();
    }

    public static double ResolveSymbolWidthMm(
        LabelObject item,
        IBarcodeRenderer renderer,
        int planDpi,
        string? resolvedData = null,
        BarcodeRenderOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(renderer);

        if (!IsLinearBarcodeObject(item)
            || !LinearBarcodeModuleContract.UsesSizedFromX(item.BarcodeWidthMode, item.BarcodeModuleWidthMm)
            || planDpi <= 0)
        {
            return item.WidthMm;
        }

        var data = resolvedData;
        if (string.IsNullOrEmpty(data))
        {
            data = string.IsNullOrEmpty(item.Text) ? "0" : item.Text;
        }

        var type = BarcodeTypeMapper.ToRendererType(item.BarcodeSymbology);
        options ??= CreateOptions(item);
        if (!renderer.ValidateData(data, type))
        {
            return item.WidthMm;
        }

        var modules = renderer.CountLinearModules(data, type, options);
        if (modules is null or <= 0)
        {
            return item.WidthMm;
        }

        try
        {
            return LinearBarcodeModuleContract.SizedFromXWidthMm(
                item.BarcodeModuleWidthMm,
                modules.Value,
                planDpi);
        }
        catch
        {
            return item.WidthMm;
        }
    }

    /// <summary>
    /// Applies SizedFromX width onto the object frame so selection/render share geometry.
    /// Returns true when WidthMm was changed.
    /// </summary>
    public static bool TryApplySizedFromXWidth(
        LabelObject item,
        IBarcodeRenderer renderer,
        int planDpi,
        string? resolvedData = null)
    {
        if (!IsLinearBarcodeObject(item)
            || !LinearBarcodeModuleContract.UsesSizedFromX(item.BarcodeWidthMode, item.BarcodeModuleWidthMm)
            || planDpi <= 0)
        {
            return false;
        }

        var width = ResolveSymbolWidthMm(item, renderer, planDpi, resolvedData);
        var normalized = Math.Round(Math.Max(0.5, width), 2, MidpointRounding.AwayFromZero);
        if (Math.Abs(item.WidthMm - normalized) < 0.005)
        {
            return false;
        }

        item.WidthMm = normalized;
        return true;
    }

    private static BarcodeRenderOptions CreateOptions(LabelObject item)
        => new()
        {
            QuietZoneModules = Math.Max(0, item.QrQuietZoneModules),
            IsGs1 = item.BarcodeApplicationProfile == BarcodeApplicationProfile.Gs1
        };
}
