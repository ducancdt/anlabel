using ANLAbel.Core.Enums;
using ANLAbel.Core.Models;

namespace ANLAbel.Core.Geometry;

/// <summary>
/// Deterministic, model-only arrange operations used by the WPF designer.
/// Geometry is persisted in millimeters and this class never depends on a
/// viewport, zoom, DPI, or WPF visual.  That keeps align/distribute behavior
/// identical after save/load and makes it directly testable.
/// </summary>
public static class LabelArrangeEngine
{
    public static LabelArrangeResult Align(
        IReadOnlyList<LabelObject> selection,
        LabelObject? keyObject,
        LabelAlignmentMode alignment,
        LabelArrangeReferenceMode reference,
        double canvasWidthMm = 0,
        double canvasHeightMm = 0)
    {
        var validation = ValidateSelection(selection, minimumCount: 1, keyObject, reference);
        if (!validation.Succeeded)
        {
            return validation;
        }

        var items = selection.ToArray();
        var isHorizontal = alignment is LabelAlignmentMode.Left or LabelAlignmentMode.HorizontalCenter or LabelAlignmentMode.Right;
        var targetBounds = reference switch
        {
            LabelArrangeReferenceMode.SelectionBounds => GetAggregateBounds(items),
            LabelArrangeReferenceMode.KeyObject => keyObject is null ? default : GetBounds(keyObject),
            LabelArrangeReferenceMode.Canvas => new LabelLayoutBounds(0, 0, canvasWidthMm, canvasHeightMm),
            _ => default
        };

        if (reference == LabelArrangeReferenceMode.Canvas
            && ((isHorizontal && canvasWidthMm <= 0) || (!isHorizontal && canvasHeightMm <= 0)))
        {
            return LabelArrangeResult.Failure("A positive canvas dimension is required for canvas alignment.");
        }

        var target = isHorizontal
            ? alignment switch
            {
                LabelAlignmentMode.Left => targetBounds.Left,
                LabelAlignmentMode.HorizontalCenter => targetBounds.CenterX,
                LabelAlignmentMode.Right => targetBounds.Right,
                _ => 0
            }
            : alignment switch
            {
                LabelAlignmentMode.Top => targetBounds.Top,
                LabelAlignmentMode.VerticalCenter => targetBounds.CenterY,
                LabelAlignmentMode.Bottom => targetBounds.Bottom,
                _ => 0
            };

        var changed = 0;
        foreach (var item in items)
        {
            // A key object is the reference, not a second target to mutate.
            if (reference == LabelArrangeReferenceMode.KeyObject && ReferenceEquals(item, keyObject))
            {
                continue;
            }

            var bounds = GetBounds(item);
            var source = isHorizontal
                ? alignment switch
                {
                    LabelAlignmentMode.Left => bounds.Left,
                    LabelAlignmentMode.HorizontalCenter => bounds.CenterX,
                    LabelAlignmentMode.Right => bounds.Right,
                    _ => 0
                }
                : alignment switch
                {
                    LabelAlignmentMode.Top => bounds.Top,
                    LabelAlignmentMode.VerticalCenter => bounds.CenterY,
                    LabelAlignmentMode.Bottom => bounds.Bottom,
                    _ => 0
                };
            var delta = target - source;
            if (Math.Abs(delta) <= 0.004)
            {
                continue;
            }

            Move(item, isHorizontal ? delta : 0, isHorizontal ? 0 : delta);
            changed++;
        }

        return LabelArrangeResult.Success(changed);
    }

    public static LabelArrangeResult Distribute(
        IReadOnlyList<LabelObject> selection,
        LabelDistributionMode distribution)
    {
        var validation = ValidateSelection(selection, minimumCount: 3, keyObject: null, LabelArrangeReferenceMode.SelectionBounds);
        if (!validation.Succeeded)
        {
            return validation;
        }

        var horizontal = distribution is LabelDistributionMode.HorizontalCenters or LabelDistributionMode.HorizontalGaps;
        var useGaps = distribution is LabelDistributionMode.HorizontalGaps or LabelDistributionMode.VerticalGaps;
        var ordered = selection
            .Select(item => (Item: item, Bounds: GetBounds(item)))
            .OrderBy(pair => horizontal ? pair.Bounds.Left : pair.Bounds.Top)
            .ThenBy(pair => horizontal ? pair.Bounds.CenterX : pair.Bounds.CenterY)
            .ThenBy(pair => pair.Item.ZIndex)
            .ThenBy(pair => pair.Item.Id, StringComparer.Ordinal)
            .ToArray();

        var first = ordered[0].Bounds;
        var last = ordered[^1].Bounds;
        var changed = 0;

        if (useGaps)
        {
            var start = horizontal ? first.Left : first.Top;
            var end = horizontal ? last.Right : last.Bottom;
            var totalSize = ordered.Sum(pair => horizontal ? pair.Bounds.Width : pair.Bounds.Height);
            var gap = (end - start - totalSize) / (ordered.Length - 1);
            var cursor = start;

            foreach (var pair in ordered)
            {
                var currentStart = horizontal ? pair.Bounds.Left : pair.Bounds.Top;
                var delta = cursor - currentStart;
                if (Math.Abs(delta) > 0.004)
                {
                    Move(pair.Item, horizontal ? delta : 0, horizontal ? 0 : delta);
                    changed++;
                }

                cursor += (horizontal ? pair.Bounds.Width : pair.Bounds.Height) + gap;
            }
        }
        else
        {
            var firstCenter = horizontal ? first.CenterX : first.CenterY;
            var lastCenter = horizontal ? last.CenterX : last.CenterY;
            var step = (lastCenter - firstCenter) / (ordered.Length - 1);

            for (var index = 1; index < ordered.Length - 1; index++)
            {
                var pair = ordered[index];
                var currentCenter = horizontal ? pair.Bounds.CenterX : pair.Bounds.CenterY;
                var delta = firstCenter + step * index - currentCenter;
                if (Math.Abs(delta) <= 0.004)
                {
                    continue;
                }

                Move(pair.Item, horizontal ? delta : 0, horizontal ? 0 : delta);
                changed++;
            }
        }

        return LabelArrangeResult.Success(changed);
    }

    public static LabelLayoutBounds GetBounds(LabelObject item)
    {
        if (item.Type == ObjectType.Line)
        {
            return LineBoundsContract.GetBounds(item);
        }

        return TransformedBoundsContract.GetBounds(item);
    }

    private static LabelLayoutBounds GetAggregateBounds(IEnumerable<LabelObject> items)
    {
        var bounds = items.Select(GetBounds).ToArray();
        return new LabelLayoutBounds(
            bounds.Min(item => item.Left),
            bounds.Min(item => item.Top),
            bounds.Max(item => item.Right),
            bounds.Max(item => item.Bottom));
    }

    private static LabelArrangeResult ValidateSelection(
        IReadOnlyList<LabelObject> selection,
        int minimumCount,
        LabelObject? keyObject,
        LabelArrangeReferenceMode reference)
    {
        if (selection is null || selection.Count < minimumCount)
        {
            return LabelArrangeResult.Failure($"Select at least {minimumCount} objects for this operation.");
        }

        if (selection.Any(item => item is null))
        {
            return LabelArrangeResult.Failure("The selection contains an invalid object.");
        }

        if (selection.Any(item => item.IsLocked))
        {
            return LabelArrangeResult.Failure("Unlock every selected object before arranging.");
        }

        if (selection.Any(item => !item.IsVisible))
        {
            return LabelArrangeResult.Failure("Show every selected object before arranging.");
        }

        if (reference == LabelArrangeReferenceMode.KeyObject
            && (keyObject is null || !selection.Contains(keyObject)))
        {
            return LabelArrangeResult.Failure("Choose a selected key object first.");
        }

        return LabelArrangeResult.Success(0);
    }

    private static void Move(LabelObject item, double deltaX, double deltaY)
    {
        if (Math.Abs(deltaX) <= 0.0001 && Math.Abs(deltaY) <= 0.0001)
        {
            return;
        }

        item.XMm += deltaX;
        item.YMm += deltaY;
        if (item.Type == ObjectType.Line)
        {
            var endX = item.LineEndXMm == 0 && item.LineEndYMm == 0
                ? item.XMm + item.WidthMm - deltaX
                : item.LineEndXMm;
            var endY = item.LineEndXMm == 0 && item.LineEndYMm == 0
                ? item.YMm + item.HeightMm - deltaY
                : item.LineEndYMm;
            item.LineEndXMm = endX + deltaX;
            item.LineEndYMm = endY + deltaY;
        }
    }
}

public readonly record struct LabelLayoutBounds(double Left, double Top, double Right, double Bottom)
{
    public double Width => Right - Left;
    public double Height => Bottom - Top;
    public double CenterX => (Left + Right) / 2;
    public double CenterY => (Top + Bottom) / 2;
}

public readonly record struct LabelArrangeResult(bool Succeeded, bool Changed, int AffectedCount, string? ErrorMessage)
{
    public static LabelArrangeResult Success(int changed) => new(true, changed > 0, changed, null);
    public static LabelArrangeResult Failure(string message) => new(false, false, 0, message);
}
