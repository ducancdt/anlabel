using ANLAbel.Core.Enums;

namespace ANLAbel.Core.Geometry;

[Flags]
public enum ResizeModifierFlags
{
    None = 0,
    PreserveAspectRatio = 1,
    ResizeFromCenter = 2
}

/// <summary>
/// Applies keyboard-modifier semantics to a resize frame before snapping and
/// artboard clamping.  The contract is deliberately WPF-free so the same
/// rules can be exercised in tests and reused by a non-WPF editor.
/// </summary>
public static class ResizeModifierContract
{
    public static ResizeFrame Apply(
        ResizeFrame source,
        ResizeFrame proposed,
        ResizeHandle handle,
        ResizeModifierFlags modifiers,
        double minimumWidthMm = 1,
        double minimumHeightMm = 1)
    {
        if (!IsValid(source) || !IsValid(proposed))
        {
            return source;
        }

        var adjusted = proposed;
        if ((modifiers & ResizeModifierFlags.PreserveAspectRatio) != 0)
        {
            adjusted = ApplyAspectRatio(source, adjusted, handle, minimumWidthMm, minimumHeightMm);
        }

        if ((modifiers & ResizeModifierFlags.ResizeFromCenter) != 0)
        {
            adjusted = ApplyCenterAnchor(source, adjusted, handle);
        }

        return EnforceMinimum(adjusted, handle, minimumWidthMm, minimumHeightMm);
    }

    public static GroupResizeFrame Apply(
        GroupResizeFrame source,
        GroupResizeFrame proposed,
        ResizeHandle handle,
        ResizeModifierFlags modifiers,
        double minimumWidthMm = 1,
        double minimumHeightMm = 1)
    {
        if (!GroupResizeGeometryContract.IsValid(source)
            || !GroupResizeGeometryContract.IsValid(proposed))
        {
            return source;
        }

        var adjusted = proposed;
        if ((modifiers & ResizeModifierFlags.PreserveAspectRatio) != 0)
        {
            adjusted = ApplyAspectRatio(source, adjusted, handle, minimumWidthMm, minimumHeightMm);
        }

        if ((modifiers & ResizeModifierFlags.ResizeFromCenter) != 0)
        {
            adjusted = ApplyCenterAnchor(source, adjusted, handle);
        }

        return EnforceMinimum(adjusted, handle, minimumWidthMm, minimumHeightMm);
    }

    private static ResizeFrame ApplyCenterAnchor(
        ResizeFrame source,
        ResizeFrame proposed,
        ResizeHandle handle)
    {
        var centerX = source.XMm + source.WidthMm / 2;
        var centerY = source.YMm + source.HeightMm / 2;
        var x = proposed.XMm;
        var y = proposed.YMm;
        var width = proposed.WidthMm;
        var height = proposed.HeightMm;

        if (MovesHorizontalLeadingEdge(handle))
        {
            width = Math.Max(0, source.WidthMm + 2 * (source.XMm - proposed.XMm));
            x = centerX - width / 2;
        }
        else if (MovesHorizontalTrailingEdge(handle))
        {
            width = Math.Max(0, source.WidthMm + 2 * (proposed.RightMm - source.RightMm));
            x = centerX - width / 2;
        }

        if (MovesVerticalLeadingEdge(handle))
        {
            height = Math.Max(0, source.HeightMm + 2 * (source.YMm - proposed.YMm));
            y = centerY - height / 2;
        }
        else if (MovesVerticalTrailingEdge(handle))
        {
            height = Math.Max(0, source.HeightMm + 2 * (proposed.BottomMm - source.BottomMm));
            y = centerY - height / 2;
        }

        return new ResizeFrame(x, y, width, height);
    }

    private static GroupResizeFrame ApplyCenterAnchor(
        GroupResizeFrame source,
        GroupResizeFrame proposed,
        ResizeHandle handle)
    {
        var centerX = source.XMm + source.WidthMm / 2;
        var centerY = source.YMm + source.HeightMm / 2;
        var x = proposed.XMm;
        var y = proposed.YMm;
        var width = proposed.WidthMm;
        var height = proposed.HeightMm;

        if (MovesHorizontalLeadingEdge(handle))
        {
            width = Math.Max(0, source.WidthMm + 2 * (source.XMm - proposed.XMm));
            x = centerX - width / 2;
        }
        else if (MovesHorizontalTrailingEdge(handle))
        {
            width = Math.Max(0, source.WidthMm + 2 * (proposed.RightMm - source.RightMm));
            x = centerX - width / 2;
        }

        if (MovesVerticalLeadingEdge(handle))
        {
            height = Math.Max(0, source.HeightMm + 2 * (source.YMm - proposed.YMm));
            y = centerY - height / 2;
        }
        else if (MovesVerticalTrailingEdge(handle))
        {
            height = Math.Max(0, source.HeightMm + 2 * (proposed.BottomMm - source.BottomMm));
            y = centerY - height / 2;
        }

        return new GroupResizeFrame(x, y, width, height);
    }

    private static ResizeFrame ApplyAspectRatio(
        ResizeFrame source,
        ResizeFrame proposed,
        ResizeHandle handle,
        double minimumWidthMm,
        double minimumHeightMm)
    {
        var ratio = source.WidthMm / source.HeightMm;
        if (!double.IsFinite(ratio) || ratio <= 0)
        {
            return proposed;
        }

        var widthChange = Math.Abs(proposed.WidthMm - source.WidthMm);
        var heightChange = Math.Abs(proposed.HeightMm - source.HeightMm);
        var width = Math.Max(Math.Max(0.001, minimumWidthMm), proposed.WidthMm);
        var height = Math.Max(Math.Max(0.001, minimumHeightMm), proposed.HeightMm);

        if (IsHorizontalOnly(handle))
        {
            height = Math.Max(Math.Max(0.001, minimumHeightMm), width / ratio);
        }
        else if (IsVerticalOnly(handle))
        {
            width = Math.Max(Math.Max(0.001, minimumWidthMm), height * ratio);
        }
        else if (widthChange / Math.Max(source.WidthMm, 0.001)
                 >= heightChange / Math.Max(source.HeightMm, 0.001))
        {
            height = Math.Max(Math.Max(0.001, minimumHeightMm), width / ratio);
        }
        else
        {
            width = Math.Max(Math.Max(0.001, minimumWidthMm), height * ratio);
        }

        return PlaceWithAnchors(source, proposed, handle, width, height);
    }

    private static GroupResizeFrame ApplyAspectRatio(
        GroupResizeFrame source,
        GroupResizeFrame proposed,
        ResizeHandle handle,
        double minimumWidthMm,
        double minimumHeightMm)
    {
        var ratio = source.WidthMm / source.HeightMm;
        if (!double.IsFinite(ratio) || ratio <= 0)
        {
            return proposed;
        }

        var widthChange = Math.Abs(proposed.WidthMm - source.WidthMm);
        var heightChange = Math.Abs(proposed.HeightMm - source.HeightMm);
        var width = Math.Max(Math.Max(0.001, minimumWidthMm), proposed.WidthMm);
        var height = Math.Max(Math.Max(0.001, minimumHeightMm), proposed.HeightMm);

        if (IsHorizontalOnly(handle))
        {
            height = Math.Max(Math.Max(0.001, minimumHeightMm), width / ratio);
        }
        else if (IsVerticalOnly(handle))
        {
            width = Math.Max(Math.Max(0.001, minimumWidthMm), height * ratio);
        }
        else if (widthChange / Math.Max(source.WidthMm, 0.001)
                 >= heightChange / Math.Max(source.HeightMm, 0.001))
        {
            height = Math.Max(Math.Max(0.001, minimumHeightMm), width / ratio);
        }
        else
        {
            width = Math.Max(Math.Max(0.001, minimumWidthMm), height * ratio);
        }

        return PlaceWithAnchors(source, proposed, handle, width, height);
    }

    private static ResizeFrame PlaceWithAnchors(
        ResizeFrame source,
        ResizeFrame proposed,
        ResizeHandle handle,
        double width,
        double height)
    {
        var x = MovesHorizontalLeadingEdge(handle)
            ? source.RightMm - width
            : MovesHorizontalTrailingEdge(handle)
                ? source.XMm
                : proposed.XMm + (proposed.WidthMm - width) / 2;
        var y = MovesVerticalLeadingEdge(handle)
            ? source.BottomMm - height
            : MovesVerticalTrailingEdge(handle)
                ? source.YMm
                : proposed.YMm + (proposed.HeightMm - height) / 2;

        // A centre resize has already placed the frame around the source
        // centre.  For a corner resize this midpoint is the desired anchor;
        // use the proposed frame when neither axis is actively resized.
        return new ResizeFrame(x, y, width, height);
    }

    private static GroupResizeFrame PlaceWithAnchors(
        GroupResizeFrame source,
        GroupResizeFrame proposed,
        ResizeHandle handle,
        double width,
        double height)
    {
        var x = MovesHorizontalLeadingEdge(handle)
            ? source.RightMm - width
            : MovesHorizontalTrailingEdge(handle)
                ? source.XMm
                : proposed.XMm + (proposed.WidthMm - width) / 2;
        var y = MovesVerticalLeadingEdge(handle)
            ? source.BottomMm - height
            : MovesVerticalTrailingEdge(handle)
                ? source.YMm
                : proposed.YMm + (proposed.HeightMm - height) / 2;
        return new GroupResizeFrame(x, y, width, height);
    }

    private static ResizeFrame EnforceMinimum(
        ResizeFrame frame,
        ResizeHandle handle,
        double minimumWidthMm,
        double minimumHeightMm)
    {
        var width = Math.Max(Math.Max(0.001, minimumWidthMm), frame.WidthMm);
        var height = Math.Max(Math.Max(0.001, minimumHeightMm), frame.HeightMm);
        var x = frame.XMm;
        var y = frame.YMm;
        if (MovesHorizontalLeadingEdge(handle))
        {
            x = frame.RightMm - width;
        }
        if (MovesVerticalLeadingEdge(handle))
        {
            y = frame.BottomMm - height;
        }
        return new ResizeFrame(x, y, width, height);
    }

    private static GroupResizeFrame EnforceMinimum(
        GroupResizeFrame frame,
        ResizeHandle handle,
        double minimumWidthMm,
        double minimumHeightMm)
    {
        var width = Math.Max(Math.Max(0.001, minimumWidthMm), frame.WidthMm);
        var height = Math.Max(Math.Max(0.001, minimumHeightMm), frame.HeightMm);
        var x = frame.XMm;
        var y = frame.YMm;
        if (MovesHorizontalLeadingEdge(handle))
        {
            x = frame.RightMm - width;
        }
        if (MovesVerticalLeadingEdge(handle))
        {
            y = frame.BottomMm - height;
        }
        return new GroupResizeFrame(x, y, width, height);
    }

    private static bool IsHorizontalOnly(ResizeHandle handle) => handle is ResizeHandle.Left or ResizeHandle.Right;

    private static bool IsVerticalOnly(ResizeHandle handle) => handle is ResizeHandle.Top or ResizeHandle.Bottom;

    private static bool MovesHorizontalLeadingEdge(ResizeHandle handle) => handle is ResizeHandle.TopLeft or ResizeHandle.Left or ResizeHandle.BottomLeft;

    private static bool MovesHorizontalTrailingEdge(ResizeHandle handle) => handle is ResizeHandle.TopRight or ResizeHandle.Right or ResizeHandle.BottomRight;

    private static bool MovesVerticalLeadingEdge(ResizeHandle handle) => handle is ResizeHandle.TopLeft or ResizeHandle.Top or ResizeHandle.TopRight;

    private static bool MovesVerticalTrailingEdge(ResizeHandle handle) => handle is ResizeHandle.BottomLeft or ResizeHandle.Bottom or ResizeHandle.BottomRight;

    private static bool IsValid(ResizeFrame frame) =>
        double.IsFinite(frame.XMm)
        && double.IsFinite(frame.YMm)
        && double.IsFinite(frame.WidthMm)
        && double.IsFinite(frame.HeightMm)
        && frame.WidthMm > 0
        && frame.HeightMm > 0;
}
