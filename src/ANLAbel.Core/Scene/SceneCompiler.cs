using System.Collections.Immutable;
using ANLAbel.Core.Enums;
using ANLAbel.Core.Geometry;

namespace ANLAbel.Core.Scene;

/// <summary>
/// WPF-free geometry compiler skeleton.  It resolves deterministic physical bounds
/// and anchors from a <see cref="DocumentSnapshot"/>; text glyph metrics and device
/// dot quantization are deliberately separate later stages.
/// </summary>
public static class SceneCompiler
{
    public static SceneCompilationResult Compile(DocumentSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var diagnostics = ImmutableArray.CreateBuilder<SceneDiagnostic>();
        if (!IsFinitePositive(snapshot.WidthMm) || !IsFinitePositive(snapshot.HeightMm))
        {
            diagnostics.Add(SceneDiagnostic.Error("SCN001", "Label width and height must be finite positive values."));
        }

        var ordered = snapshot.Objects
            .OrderBy(item => item.ZIndex)
            .ThenBy(item => item.Id, StringComparer.Ordinal)
            .ToArray();
        var duplicateIds = ordered
            .Where(item => !string.IsNullOrWhiteSpace(item.Id))
            .GroupBy(item => item.Id, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();
        foreach (var duplicateId in duplicateIds)
        {
            diagnostics.Add(SceneDiagnostic.Error("SCN002", $"Object ID '{duplicateId}' is duplicated."));
        }

        var nodes = ImmutableArray.CreateBuilder<CompiledSceneNode>();
        foreach (var item in ordered)
        {
            if (string.IsNullOrWhiteSpace(item.Id))
            {
                diagnostics.Add(SceneDiagnostic.Error("SCN003", "Every scene object must have a stable non-empty ID."));
                continue;
            }

            if (!Enum.IsDefined(item.Type))
            {
                diagnostics.Add(SceneDiagnostic.Error("SCN007", $"Object '{item.Id}' has an unsupported object type '{item.Type}'."));
                continue;
            }

            if (!IsFinite(item.XMm) || !IsFinite(item.YMm) || !IsFinite(item.WidthMm) || !IsFinite(item.HeightMm))
            {
                diagnostics.Add(SceneDiagnostic.Error("SCN004", $"Object '{item.Id}' has non-finite geometry."));
                continue;
            }

            var lineEnd = item.Type == ObjectType.Line ? ResolveLineEnd(item) : (ScenePoint?)null;
            if (item.Type == ObjectType.Line)
            {
                if (lineEnd is not { } end
                    || !IsFinite(end.XMm)
                    || !IsFinite(end.YMm)
                    || (end.XMm == item.XMm && end.YMm == item.YMm))
                {
                    diagnostics.Add(SceneDiagnostic.Error("SCN005", $"Line '{item.Id}' must have finite distinct endpoints."));
                    continue;
                }
            }
            else if (item.WidthMm <= 0 || item.HeightMm <= 0)
            {
                diagnostics.Add(SceneDiagnostic.Error("SCN005", $"Object '{item.Id}' must have positive layout dimensions."));
                continue;
            }

            if (item.Rotation is not (0 or 90 or 180 or 270))
            {
                diagnostics.Add(SceneDiagnostic.Error("SCN006", $"Object '{item.Id}' has unsupported rotation {item.Rotation}."));
                continue;
            }

            var layoutBounds = ResolveLayoutBounds(item);
            var visualBounds = ResolveVisualBounds(item, layoutBounds);
            var anchors = SceneAnchorSet.FromBounds(visualBounds);
            nodes.Add(new CompiledSceneNode
            {
                Id = item.Id,
                Type = item.Type,
                Name = item.Name,
                ZIndex = item.ZIndex,
                IsLocked = item.IsLocked,
                IsVisible = item.IsVisible,
                Rotation = item.Rotation,
                LayoutBoundsMm = layoutBounds,
                VisualBoundsMm = visualBounds,
                Anchors = anchors,
                LineStartMm = item.Type == ObjectType.Line ? new ScenePoint(item.XMm, item.YMm) : null,
                LineEndMm = lineEnd,
                Text = item.Type is ObjectType.Text or ObjectType.TextBox ? item.Text : string.Empty,
                BindingExpression = item.BindingExpression,
                Style = item.Style
            });
        }

        var immutableNodes = nodes.ToImmutable();
        var immutableDiagnostics = diagnostics.ToImmutable();
        var hash = SceneHash.ComputeSceneHash(snapshot, immutableNodes);
        return new SceneCompilationResult(snapshot.DocumentHash, hash, immutableNodes, immutableDiagnostics)
        {
            Snapshot = snapshot
        };
    }

    private static SceneBounds ResolveLayoutBounds(SceneObjectSnapshot item)
    {
        if (item.Type == ObjectType.Line)
        {
            var end = ResolveLineEnd(item);
            return SceneBounds.FromPoints(new ScenePoint(item.XMm, item.YMm), end);
        }

        return new SceneBounds(item.XMm, item.YMm, item.WidthMm, item.HeightMm);
    }

    private static SceneBounds ResolveVisualBounds(SceneObjectSnapshot item, SceneBounds layoutBounds)
    {
        if (item.Type == ObjectType.Line)
        {
            var bounds = LineBoundsContract.GetBounds(item);
            return new SceneBounds(bounds.Left, bounds.Top, bounds.Width, bounds.Height);
        }

        if (item.Rotation == 0)
        {
            return layoutBounds;
        }

        var center = new ScenePoint(
            layoutBounds.LeftMm + layoutBounds.WidthMm / 2,
            layoutBounds.TopMm + layoutBounds.HeightMm / 2);
        var corners = new[]
        {
            new ScenePoint(layoutBounds.LeftMm, layoutBounds.TopMm),
            new ScenePoint(layoutBounds.RightMm, layoutBounds.TopMm),
            new ScenePoint(layoutBounds.RightMm, layoutBounds.BottomMm),
            new ScenePoint(layoutBounds.LeftMm, layoutBounds.BottomMm)
        };
        var rotated = corners.Select(point => Rotate(point, center, item.Rotation)).ToArray();
        return SceneBounds.FromPoints(rotated);
    }

    private static ScenePoint Rotate(ScenePoint point, ScenePoint center, int degrees)
    {
        var radians = degrees * Math.PI / 180;
        var cos = Math.Cos(radians);
        var sin = Math.Sin(radians);
        var dx = point.XMm - center.XMm;
        var dy = point.YMm - center.YMm;
        return new ScenePoint(
            center.XMm + dx * cos - dy * sin,
            center.YMm + dx * sin + dy * cos);
    }

    private static ScenePoint ResolveLineEnd(SceneObjectSnapshot item)
    {
        return item.LineEndXMm == 0 && item.LineEndYMm == 0
            ? new ScenePoint(item.XMm + item.WidthMm, item.YMm + item.HeightMm)
            : new ScenePoint(item.LineEndXMm, item.LineEndYMm);
    }

    private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);

    private static bool IsFinitePositive(double value) => IsFinite(value) && value > 0;
}

public sealed record SceneCompilationResult(
    string DocumentHash,
    string SceneHash,
    ImmutableArray<CompiledSceneNode> Nodes,
    ImmutableArray<SceneDiagnostic> Diagnostics)
{
    /// <summary>
    /// The immutable value boundary that produced this compilation.  It is kept
    /// with the result so a presenter can render exactly the compiled state,
    /// even if the WPF authoring model changes after plan creation.
    /// </summary>
    public DocumentSnapshot Snapshot { get; init; } = new();

    public bool Succeeded => Diagnostics.All(item => item.Severity != SceneDiagnosticSeverity.Error);
}

public sealed record CompiledSceneNode
{
    public string Id { get; init; } = string.Empty;
    public ObjectType Type { get; init; }
    public string Name { get; init; } = string.Empty;
    public int ZIndex { get; init; }
    public bool IsLocked { get; init; }
    public bool IsVisible { get; init; }
    public int Rotation { get; init; }
    public SceneBounds LayoutBoundsMm { get; init; }
    public SceneBounds VisualBoundsMm { get; init; }
    public SceneAnchorSet Anchors { get; init; }
    public ScenePoint? LineStartMm { get; init; }
    public ScenePoint? LineEndMm { get; init; }
    public string Text { get; init; } = string.Empty;
    public string BindingExpression { get; init; } = string.Empty;
    public ObjectStyleSnapshot Style { get; init; } = new();
}

public readonly record struct ScenePoint(double XMm, double YMm);

public readonly record struct SceneBounds(double LeftMm, double TopMm, double WidthMm, double HeightMm)
{
    public double RightMm => LeftMm + WidthMm;
    public double BottomMm => TopMm + HeightMm;
    public double CenterXMm => LeftMm + WidthMm / 2;
    public double CenterYMm => TopMm + HeightMm / 2;

    public static SceneBounds FromPoints(params ScenePoint[] points)
    {
        ArgumentNullException.ThrowIfNull(points);
        if (points.Length == 0)
        {
            return new SceneBounds(0, 0, 0, 0);
        }

        var left = points.Min(point => point.XMm);
        var top = points.Min(point => point.YMm);
        var right = points.Max(point => point.XMm);
        var bottom = points.Max(point => point.YMm);
        return new SceneBounds(left, top, right - left, bottom - top);
    }

    public static SceneBounds FromPoints(ScenePoint first, ScenePoint second)
        => FromPoints(new[] { first, second });
}

public readonly record struct SceneAnchorSet(
    double LeftMm,
    double CenterXMm,
    double RightMm,
    double TopMm,
    double CenterYMm,
    double BottomMm)
{
    public static SceneAnchorSet FromBounds(SceneBounds bounds)
        => new(bounds.LeftMm, bounds.CenterXMm, bounds.RightMm, bounds.TopMm, bounds.CenterYMm, bounds.BottomMm);
}

public enum SceneDiagnosticSeverity
{
    Warning,
    Error
}

public sealed record SceneDiagnostic(SceneDiagnosticSeverity Severity, string Code, string Message)
{
    public static SceneDiagnostic Error(string code, string message) => new(SceneDiagnosticSeverity.Error, code, message);
    public static SceneDiagnostic Warning(string code, string message) => new(SceneDiagnosticSeverity.Warning, code, message);
}
