using System.Globalization;
using System.Windows;
using System.Windows.Media;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using FontFamily = System.Windows.Media.FontFamily;
using FlowDirection = System.Windows.FlowDirection;
using ANLAbel.Core.Enums;
using ANLAbel.Core.Geometry;
using ANLAbel.Core.Models;
using ANLAbel.Core.Text;

namespace ANLAbel.Printing.RenderPipeline;

public static class TextBoxOverflowDetector
{
    public const double HorizontalPaddingDip = 0;
    // WPF rounds glyph metrics at the device boundary. Keep a small tolerance
    // so a value that is only a fraction of a DIP over the frame is not
    // reported differently by designer, preview and print.
    private const double HorizontalOverflowToleranceDip = 0.2;
    private const double StaticTextHorizontalPaddingDip = 2;
    private const string DefaultFontFamilyName = "Arial";
    public const double MinimumShrinkFontSizePt = 4.0;
    public const double MinimumScaleWidthFactor = 0.5;
    private const int ShrinkFontIterations = 18;
    private static readonly Lazy<IReadOnlySet<string>> InstalledFontFamilyNames = new(BuildInstalledFontFamilyNames);

    /// <summary>
    /// Returns whether the renderer/preflight must keep text inside the
    /// authored frame. Object type owns this contract: Text is content-owned
    /// and remains free-flowing, while TextBox is frame-owned and is always
    /// constrained. Persisted sizing/overflow values must never blur that
    /// boundary, including values loaded from older project files.
    /// </summary>
    public static bool ShouldConstrainToBox(LabelObject item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return item.Type == ObjectType.TextBox;
    }

    public static bool ShouldBlockOverflow(LabelObject item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return item.Type == ObjectType.TextBox
            && ResolveTextBoxOverflow(item.Style.TextOverflow) == TextOverflowMode.Error;
    }

    public static bool UsesEllipsis(LabelObject item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return item.Type == ObjectType.TextBox
            && ResolveTextBoxOverflow(item.Style.TextOverflow) == TextOverflowMode.Ellipsis;
    }

    public static bool UsesShrinkFont(LabelObject item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return item.Type == ObjectType.TextBox
            && item.Style.TextSizing == TextSizingMode.ShrinkFont;
    }

    public static bool UsesScaleWidth(LabelObject item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return item.Type == ObjectType.TextBox
            && item.Style.TextSizing == TextSizingMode.ScaleWidth;
    }

    /// <summary>
    /// Free <see cref="ObjectType.Text"/> compresses glyphs into an undersized
    /// authored selection frame (border-drag WYSIWYG). This is not TextBox
    /// ownership: no wrap-as-field, no overflow Error block, and content AutoFit
    /// may still grow the frame when content/style changes.
    /// </summary>
    public static bool UsesTextFrameFitCompress(LabelObject item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return item.Type == ObjectType.Text;
    }

    /// <summary>
    /// Computes free-Text frame-fit scales so natural ink fits the content box.
    /// Values are clamped to (0.01, 1]: compress only, never expand above 100%.
    /// Horizontal and vertical factors are independent (distortion allowed).
    /// </summary>
    public static (double ScaleX, double ScaleY) ResolveTextFrameFitScale(
        double naturalWidthDip,
        double naturalHeightDip,
        double contentWidthDip,
        double contentHeightDip,
        double lineHeightDip = 1.0)
    {
        naturalWidthDip = Math.Max(0.01, naturalWidthDip);
        naturalHeightDip = Math.Max(0.01, naturalHeightDip);
        contentWidthDip = Math.Max(1, contentWidthDip);
        contentHeightDip = Math.Max(1, contentHeightDip);
        lineHeightDip = Math.Max(0.01, lineHeightDip);

        var scaleX = 1.0;
        var scaleY = 1.0;
        if (naturalWidthDip > contentWidthDip + HorizontalOverflowToleranceDip)
        {
            scaleX = Math.Clamp(contentWidthDip / naturalWidthDip, 0.01, 1.0);
        }

        if (naturalHeightDip > contentHeightDip + lineHeightDip * 0.2)
        {
            scaleY = Math.Clamp(contentHeightDip / naturalHeightDip, 0.01, 1.0);
        }

        return (scaleX, scaleY);
    }

    /// <summary>
    /// AllowOverflow was available to early TextBox builds, but it contradicts
    /// the industrial-label safety contract. Treat it as Error at runtime so a
    /// legacy file is clipped and blocked by preflight instead of leaking glyphs
    /// into adjacent objects or outside the printed label.
    /// </summary>
    public static TextOverflowMode ResolveTextBoxOverflow(TextOverflowMode value)
        => value == TextOverflowMode.AllowOverflow ? TextOverflowMode.Error : value;

    public static double GetHorizontalOriginDip(LabelObject item, bool constrainToBox)
    {
        ArgumentNullException.ThrowIfNull(item);
        return GetLeftPaddingDip(item, constrainToBox);
    }

    /// <summary>
    /// Returns the combined effective left/right inset in WPF DIP. The legacy
    /// static text inset remains part of the unconstrained path; authored edge
    /// padding is physical and is added consistently to constrained and
    /// unconstrained text so the persisted model owns the frame.
    /// </summary>
    public static double GetHorizontalPaddingDip(LabelObject item, bool constrainToBox)
    {
        ArgumentNullException.ThrowIfNull(item);
        return GetLeftPaddingDip(item, constrainToBox) + GetRightPaddingDip(item, constrainToBox);
    }

    public static double GetVerticalPaddingDip(LabelObject item, bool constrainToBox)
    {
        ArgumentNullException.ThrowIfNull(item);
        return GetTopPaddingDip(item, constrainToBox) + GetBottomPaddingDip(item, constrainToBox);
    }

    public static double GetLeftPaddingDip(LabelObject item, bool constrainToBox)
    {
        ArgumentNullException.ThrowIfNull(item);
        var legacy = constrainToBox ? HorizontalPaddingDip : StaticTextHorizontalPaddingDip;
        return legacy + MmConverter.MmToDip(Math.Clamp(item.Style.TextPaddingLeftMm, 0, 20));
    }

    public static double GetRightPaddingDip(LabelObject item, bool constrainToBox)
    {
        ArgumentNullException.ThrowIfNull(item);
        var legacy = constrainToBox ? HorizontalPaddingDip : StaticTextHorizontalPaddingDip;
        return legacy + MmConverter.MmToDip(Math.Clamp(item.Style.TextPaddingRightMm, 0, 20));
    }

    public static double GetTopPaddingDip(LabelObject item, bool constrainToBox)
    {
        ArgumentNullException.ThrowIfNull(item);
        return MmConverter.MmToDip(Math.Clamp(item.Style.TextPaddingTopMm, 0, 20));
    }

    public static double GetBottomPaddingDip(LabelObject item, bool constrainToBox)
    {
        ArgumentNullException.ThrowIfNull(item);
        return MmConverter.MmToDip(Math.Clamp(item.Style.TextPaddingBottomMm, 0, 20));
    }

    /// <summary>
    /// Measures the physical frame for static-Text AutoFit (content-owned).
    /// TextBox is user-owned and must not apply these results to rewrite size
    /// from content; when called for TextBox, returns the authored frame.
    /// </summary>
    public static (double WidthMm, double HeightMm) MeasureAutoFitFrameMm(
        LabelObject item,
        string? displayText,
        double pixelsPerDip = 1.0)
    {
        ArgumentNullException.ThrowIfNull(item);
        const double minSizeMm = 1.0;
        const double slackMm = 0.6;
        var value = string.IsNullOrEmpty(displayText) ? " " : displayText;
        pixelsPerDip = pixelsPerDip > 0 && double.IsFinite(pixelsPerDip) ? pixelsPerDip : 1.0;

        if (item.Type == ObjectType.TextBox)
        {
            var widthMm = double.IsFinite(item.WidthMm) && item.WidthMm > 0 ? item.WidthMm : minSizeMm;
            var heightMm = double.IsFinite(item.HeightMm) && item.HeightMm > 0 ? item.HeightMm : minSizeMm;
            return (Math.Max(minSizeMm, widthMm), Math.Max(minSizeMm, heightMm));
        }

        var text = CreateFormattedText(item, value, Brushes.Black, pixelsPerDip);
        var horizontalPaddingDip = GetHorizontalPaddingDip(item, constrainToBox: false);
        var verticalPaddingDip = GetVerticalPaddingDip(item, constrainToBox: false);
        return (
            Math.Max(minSizeMm, MmConverter.DipToMm(Math.Ceiling(text.WidthIncludingTrailingWhitespace) + horizontalPaddingDip) + slackMm),
            Math.Max(minSizeMm, MmConverter.DipToMm(Math.Ceiling(text.Height) + verticalPaddingDip) + slackMm));
    }

    public static bool IsOverflowing(LabelObject item, string value, double widthDip, double heightDip, double pixelsPerDip = 1.0)
    {
        if (!ShouldConstrainToBox(item) || widthDip <= 0 || heightDip <= 0)
        {
            return false;
        }

        if (HasExplicitLineHeight(item) || UsesShrinkFont(item) || UsesScaleWidth(item))
        {
            return CreateTextLayout(item, value, widthDip, heightDip, constrainToBox: true, Brushes.Black, pixelsPerDip)
                .Metrics.IsOverflowing;
        }

        var contentWidth = GetContentWidthDip(item, widthDip, constrainToBox: true);
        var wrappedText = WrapTextToBox(item, value, contentWidth, pixelsPerDip);
        var measured = CreateFormattedText(item, wrappedText, Brushes.Black, pixelsPerDip);
        var horizontalOverflow = HasHorizontalOverflow(item, wrappedText, contentWidth, pixelsPerDip);
        // Measure the actual wrapped value rather than estimating line count
        // from "Ag". This preserves glyph shaping, combining marks, emoji
        // fallback and explicit newlines in the same metrics used to draw.
        measured.MaxTextWidth = contentWidth;
        return Measure(
            measured,
            item,
            widthDip,
            heightDip,
            constrainToBox: true,
            sourceValue: value,
            pixelsPerDip: pixelsPerDip,
            horizontalOverflow: horizontalOverflow).IsOverflowing;
    }

    public static FormattedText CreateFormattedText(
        LabelObject item,
        string value,
        Brush brush,
        double pixelsPerDip = 1.0,
        double? fontSizePtOverride = null)
    {
        var typeface = new Typeface(
            new FontFamily(ResolveFontFamilyName(item.Style.FontFamily)),
            item.Style.Italic ? FontStyles.Italic : FontStyles.Normal,
            item.Style.Bold ? FontWeights.Bold : FontWeights.Normal,
            FontStretches.Normal);

        var text = new FormattedText(
            value,
            CultureInfo.InvariantCulture,
            ResolveFlowDirection(item, value),
            typeface,
            ResolveFontSizePt(item.Style.FontSizePt, fontSizePtOverride) * 96.0 / 72.0,
            brush,
            pixelsPerDip)
        {
            TextAlignment = item.Style.Alignment switch
            {
                TextAlignmentMode.Center => TextAlignment.Center,
                TextAlignmentMode.Right => TextAlignment.Right,
                TextAlignmentMode.Justify => TextAlignment.Justify,
                _ => TextAlignment.Left
            }
        };

        if (item.Style.Underline)
        {
            text.SetTextDecorations(TextDecorations.Underline);
        }

        return text;
    }

    public static bool HasExplicitLineHeight(LabelObject item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return item.Style.LineHeightPt > 0;
    }

    /// <summary>
    /// Creates a line-by-line layout for an explicit line-height request. WPF's
    /// <see cref="FormattedText.LineHeight"/> is read-only, so drawing separate
    /// lines is the only way to add deterministic spacing without scaling glyphs.
    /// Auto line-height continues through the legacy single <see cref="FormattedText"/>
    /// path to preserve existing templates exactly.
    /// </summary>
    public static TextLayoutResult CreateTextLayout(
        LabelObject item,
        string value,
        double widthDip,
        double heightDip,
        bool constrainToBox,
        Brush brush,
        double pixelsPerDip = 1.0)
    {
        ArgumentNullException.ThrowIfNull(item);
        value ??= string.Empty;
        widthDip = Math.Max(1, widthDip);
        heightDip = Math.Max(1, heightDip);
        var contentWidth = GetContentWidthDip(item, widthDip, constrainToBox);
        var effectiveFontSizePt = ResolveEffectiveFontSizePt(
            item,
            value,
            widthDip,
            heightDip,
            constrainToBox,
            pixelsPerDip);
        var scaleWidth = constrainToBox && UsesScaleWidth(item);
        var displayValue = constrainToBox && !scaleWidth
            ? WrapTextToBox(item, value, contentWidth, pixelsPerDip, effectiveFontSizePt)
            : TextLayoutContract.NormalizeLineEndings(value);
        var lineValues = displayValue.Split('\n', StringSplitOptions.None);
        if (lineValues.Length == 0)
        {
            lineValues = new[] { string.Empty };
        }

        var horizontalScale = ResolveHorizontalScale(
            item,
            lineValues,
            contentWidth,
            pixelsPerDip,
            effectiveFontSizePt,
            scaleWidth);
        var unscaledContentWidth = horizontalScale > 0
            ? contentWidth / horizontalScale
            : contentWidth;

        var naturalLineHeight = 1.0;
        if (lineValues.Length > 0)
        {
            var firstLine = CreateFormattedText(
                item,
                string.IsNullOrEmpty(lineValues[0]) ? " " : lineValues[0],
                brush,
                pixelsPerDip,
                effectiveFontSizePt);
            naturalLineHeight = ResolveNaturalLineHeightDip(firstLine);
        }

        var lineHeight = TextLayoutContract.ResolveLineHeightDip(naturalLineHeight, item.Style.LineHeightPt);
        if (constrainToBox && UsesEllipsis(item))
        {
            var maxLineCount = Math.Max(
                1,
                (int)Math.Floor(
                    GetContentHeightDip(item, heightDip, constrainToBox) / lineHeight + 0.0001));
            if (lineValues.Length > maxLineCount)
            {
                lineValues = lineValues.Take(maxLineCount).ToArray();
                lineValues[^1] = TrimLineWithEllipsis(
                    item,
                    lineValues[^1],
                    unscaledContentWidth,
                    pixelsPerDip,
                    effectiveFontSizePt,
                    forceEllipsis: true);
            }
        }

        var lines = new List<FormattedText>(lineValues.Length);
        // Capture unscaled line widths BEFORE MaxTextWidth is applied. Setting
        // MaxTextWidth first can make WidthIncludingTrailingWhitespace report the
        // frame width and skip free-Text frame-fit compress.
        var naturalLineWidths = new List<double>(lineValues.Length);
        var width = 0.0;
        var inkExtent = 0.0;
        var baseline = 0.0;
        var horizontalOverflow = false;
        for (var lineIndex = 0; lineIndex < lineValues.Length; lineIndex++)
        {
            var lineValue = lineValues[lineIndex];
            if (constrainToBox && UsesEllipsis(item))
            {
                lineValue = TrimLineWithEllipsis(item, lineValue, unscaledContentWidth, pixelsPerDip, effectiveFontSizePt);
            }

            var line = CreateFormattedText(
                item,
                string.IsNullOrEmpty(lineValue) ? " " : lineValue,
                brush,
                pixelsPerDip,
                effectiveFontSizePt);
            var naturalLineWidth = string.IsNullOrEmpty(lineValue)
                ? 0
                : line.WidthIncludingTrailingWhitespace;
            naturalLineWidths.Add(naturalLineWidth);
            var effectiveLineWidth = naturalLineWidth * horizontalScale;
            horizontalOverflow |= constrainToBox
                && effectiveLineWidth > contentWidth + HorizontalOverflowToleranceDip;
            if (constrainToBox || item.Style.Alignment != TextAlignmentMode.Left)
            {
                line.MaxTextWidth = contentWidth;
            }

            lines.Add(line);
            naturalLineHeight = Math.Max(naturalLineHeight, ResolveNaturalLineHeightDip(line));
            width = Math.Max(width, effectiveLineWidth);
            inkExtent = Math.Max(inkExtent, line.Extent);
            if (lines.Count == 1)
            {
                baseline = line.Baseline;
            }
        }

        var totalHeight = lineHeight * lines.Count;
        // Free Text frame-fit compress (ANLAbel WYSIWYG). When the authored
        // selection is smaller than natural ink (border-drag shrink), scale
        // glyphs into the frame. Independent X/Y scales allow distortion so the
        // design frame stays filled — not TextBox wrap/clip ownership.
        var verticalScale = 1.0;
        if (UsesTextFrameFitCompress(item) && !scaleWidth)
        {
            var naturalWidth = 0.0;
            foreach (var naturalLineWidth in naturalLineWidths)
            {
                naturalWidth = Math.Max(naturalWidth, naturalLineWidth);
            }

            if (naturalWidth <= HorizontalOverflowToleranceDip)
            {
                naturalWidth = Math.Max(width, 0.01);
            }

            var naturalHeight = Math.Max(totalHeight, 0.01);
            var contentHeight = GetContentHeightDip(item, heightDip, constrainToBox: false);
            var fit = ResolveTextFrameFitScale(naturalWidth, naturalHeight, contentWidth, contentHeight, lineHeight);
            horizontalScale = fit.ScaleX;
            verticalScale = fit.ScaleY;
            width = naturalWidth * horizontalScale;
            totalHeight = naturalHeight * verticalScale;
            horizontalOverflow = false;
        }

        var metrics = new TextLayoutMetrics(
            WidthDip: width,
            HeightDip: totalHeight,
            InkExtentDip: inkExtent * verticalScale,
            BaselineDip: baseline * verticalScale,
            LineHeightDip: lineHeight,
            LineCount: lines.Count,
            ContentWidthDip: contentWidth,
            VerticalOffsetDip: ResolveVerticalOffset(item, totalHeight, heightDip, constrainToBox),
            IsOverflowing: constrainToBox
                && (horizontalOverflow
                    || totalHeight > GetContentHeightDip(item, heightDip, constrainToBox) + lineHeight * 0.2));
        metrics = metrics with
        {
            EffectiveFontSizePt = effectiveFontSizePt,
            HorizontalScale = horizontalScale,
            VerticalScale = verticalScale,
            HorizontalScaleAnchorFraction = ResolveHorizontalScaleAnchorFraction(item)
        };

        metrics = WithIdentity(metrics, item, value, widthDip, heightDip, constrainToBox, pixelsPerDip);

        return new TextLayoutResult { Lines = lines, Metrics = metrics };
    }

    public static void DrawTextLayout(System.Windows.Media.DrawingContext drawingContext, TextLayoutResult layout, System.Windows.Point origin)
    {
        ArgumentNullException.ThrowIfNull(drawingContext);
        ArgumentNullException.ThrowIfNull(layout);
        var scaleX = double.IsFinite(layout.Metrics.HorizontalScale)
            ? Math.Clamp(layout.Metrics.HorizontalScale, 0.01, 4.0)
            : 1.0;
        var scaleY = double.IsFinite(layout.Metrics.VerticalScale)
            ? Math.Clamp(layout.Metrics.VerticalScale, 0.01, 4.0)
            : 1.0;
        var transformed = Math.Abs(scaleX - 1.0) > 0.000001 || Math.Abs(scaleY - 1.0) > 0.000001;
        if (transformed)
        {
            var anchorX = origin.X + layout.Metrics.ContentWidthDip * layout.Metrics.HorizontalScaleAnchorFraction;
            // Vertical offset already places the block; scale from the top of
            // the first line so top/center/bottom alignment stay consistent.
            drawingContext.PushTransform(new ScaleTransform(scaleX, scaleY, anchorX, origin.Y));
        }

        for (var index = 0; index < layout.Lines.Count; index++)
        {
            drawingContext.DrawText(
                layout.Lines[index],
                new System.Windows.Point(origin.X, origin.Y + index * layout.Metrics.LineHeightDip));
        }

        if (transformed)
        {
            drawingContext.Pop();
        }
    }

    /// <summary>
    /// Returns the bounds of the actual WPF glyph/decorations geometry at the
    /// supplied origin. This is deliberately separate from <see cref="Measure"/>
    /// which reports layout/frame metrics and is therefore suitable for the
    /// explicit optical-alignment command only.
    /// </summary>
    public static Rect GetInkBoundsDip(FormattedText text, System.Windows.Point origin)
    {
        ArgumentNullException.ThrowIfNull(text);
        return text.BuildGeometry(origin).Bounds;
    }

    /// <summary>Combines the visible ink bounds of an explicit line layout.</summary>
    public static Rect GetInkBoundsDip(TextLayoutResult layout, System.Windows.Point origin)
    {
        ArgumentNullException.ThrowIfNull(layout);
        var scaleX = double.IsFinite(layout.Metrics.HorizontalScale) ? layout.Metrics.HorizontalScale : 1.0;
        var scaleY = double.IsFinite(layout.Metrics.VerticalScale) ? layout.Metrics.VerticalScale : 1.0;
        var transformed = Math.Abs(scaleX - 1.0) > 0.000001 || Math.Abs(scaleY - 1.0) > 0.000001;
        var anchorX = origin.X + layout.Metrics.ContentWidthDip * layout.Metrics.HorizontalScaleAnchorFraction;
        var combined = Rect.Empty;
        for (var index = 0; index < layout.Lines.Count; index++)
        {
            var bounds = GetInkBoundsDip(
                layout.Lines[index],
                new System.Windows.Point(origin.X, origin.Y + index * layout.Metrics.LineHeightDip));
            if (!bounds.IsEmpty && transformed)
            {
                bounds = ScaleBounds(bounds, anchorX, origin.Y, scaleX, scaleY);
            }
            if (bounds.IsEmpty)
            {
                continue;
            }

            combined = combined.IsEmpty ? bounds : Rect.Union(combined, bounds);
        }

        return combined;
    }

    private static Rect ScaleBounds(Rect bounds, double anchorX, double anchorY, double scaleX, double scaleY)
    {
        var left = anchorX + (bounds.Left - anchorX) * scaleX;
        var right = anchorX + (bounds.Right - anchorX) * scaleX;
        var top = anchorY + (bounds.Top - anchorY) * scaleY;
        var bottom = anchorY + (bounds.Bottom - anchorY) * scaleY;
        return new Rect(
            Math.Min(left, right),
            Math.Min(top, bottom),
            Math.Abs(right - left),
            Math.Abs(bottom - top));
    }

    /// <summary>
    /// Returns whether WPF can resolve the requested family on this machine.
    /// A missing family is not allowed to fail the render path: the shared
    /// formatter chooses a deterministic fallback, while print preflight can
    /// surface the mismatch before a production batch is submitted.
    /// </summary>
    public static bool IsFontAvailable(string? fontFamily, IReadOnlySet<string>? installedFontFamilyNames = null)
    {
        return string.IsNullOrWhiteSpace(fontFamily)
            || FindAvailableFontFamily(fontFamily.Trim(), installedFontFamilyNames) is not null;
    }

    /// <summary>
    /// Observes the requested family and glyph coverage for one resolved value.
    /// WPF may silently choose another fallback family for a missing code point;
    /// production preflight uses this observation to fail closed instead of
    /// allowing a preview glyph and a printer glyph to diverge.
    /// </summary>
    public static TextFontObservation ObserveFont(
        LabelObject item,
        string? value,
        IReadOnlySet<string>? installedFontFamilyNames = null)
    {
        ArgumentNullException.ThrowIfNull(item);

        var requested = string.IsNullOrWhiteSpace(item.Style.FontFamily)
            ? DefaultFontFamilyName
            : item.Style.FontFamily.Trim();
        var requestedAvailable = IsFontAvailable(requested, installedFontFamilyNames);
        var resolved = ResolveFontFamilyName(requested, installedFontFamilyNames);
        var missing = new SortedSet<int>();
        var glyphMapAvailable = false;

        try
        {
            var typeface = new Typeface(
                new FontFamily(resolved),
                item.Style.Italic ? FontStyles.Italic : FontStyles.Normal,
                item.Style.Bold ? FontWeights.Bold : FontWeights.Normal,
                FontStretches.Normal);
            if (typeface.TryGetGlyphTypeface(out var glyphTypeface))
            {
                glyphMapAvailable = true;
                foreach (var codePoint in EnumerateCodePoints(value))
                {
                    if (!glyphTypeface.CharacterToGlyphMap.TryGetValue(codePoint, out var glyphIndex)
                        || glyphIndex == 0)
                    {
                        missing.Add(codePoint);
                    }
                }
            }
        }
        catch
        {
            // Font observation is diagnostic; the formatter still owns the
            // deterministic fallback path and preflight reports family errors.
        }

        return new TextFontObservation(
            requested,
            resolved,
            requestedAvailable,
            glyphMapAvailable,
            missing.ToArray());
    }

    public static string ResolveFontFamilyName(string? fontFamily, IReadOnlySet<string>? installedFontFamilyNames = null)
    {
        var requested = fontFamily?.Trim();
        if (string.IsNullOrWhiteSpace(requested))
        {
            return DefaultFontFamilyName;
        }

        var availableFamily = FindAvailableFontFamily(requested, installedFontFamilyNames);
        if (availableFamily is not null)
        {
            return availableFamily;
        }

        // Arial is part of the supported baseline font set for the current
        // desktop build. Keep the fallback stable so preview and print do not
        // resolve different metrics just because the original family is absent.
        return DefaultFontFamilyName;
    }

    private static string? FindAvailableFontFamily(string requested, IReadOnlySet<string>? installedFontFamilyNames)
    {
        var availableNames = installedFontFamilyNames ?? InstalledFontFamilyNames.Value;
        var exactMatch = availableNames.FirstOrDefault(name => string.Equals(name, requested, StringComparison.Ordinal));
        if (exactMatch is not null)
        {
            return exactMatch;
        }

        // Test/catalog adapters are allowed to use a case-sensitive set. Match
        // family names with the same case-insensitive policy as Windows, then
        // choose a stable spelling so resource fingerprints do not depend on
        // enumeration order.
        return availableNames
            .Where(name => string.Equals(name, requested, StringComparison.OrdinalIgnoreCase))
            .OrderBy(name => name, StringComparer.Ordinal)
            .FirstOrDefault();
    }

    /// <summary>
    /// Resolves only the paragraph base direction. WPF still performs the
    /// mixed LTR/RTL run ordering and glyph shaping for the value itself.
    /// Keeping this in the shared detector prevents designer/preview/print
    /// from silently disagreeing for Arabic, Hebrew or mixed identifiers.
    /// </summary>
    public static FlowDirection ResolveFlowDirection(LabelObject item, string? value)
    {
        ArgumentNullException.ThrowIfNull(item);
        return ResolveFlowDirection(item.Style.TextDirection, value);
    }

    public static FlowDirection ResolveFlowDirection(TextDirectionMode mode, string? value)
    {
        return TextLayoutContract.ResolveDirection(mode, value) == TextDirectionMode.RightToLeft
            ? FlowDirection.RightToLeft
            : FlowDirection.LeftToRight;
    }

    /// <summary>
    /// Applies the same width/height contract used by both the WPF designer
    /// and the print renderer. Text objects remain content-owned/free-flowing;
    /// TextBox objects always use their authored frame.
    /// </summary>
    public static void ApplyLayoutBounds(
        FormattedText text,
        LabelObject item,
        double widthDip,
        double heightDip,
        bool constrainToBox)
    {
        if (constrainToBox || item.Style.Alignment != TextAlignmentMode.Left)
        {
            text.MaxTextWidth = GetContentWidthDip(item, widthDip, constrainToBox);
        }

        if (constrainToBox)
        {
            text.MaxTextHeight = GetContentHeightDip(item, heightDip, constrainToBox);
            if (UsesEllipsis(item))
            {
                text.Trimming = TextTrimming.CharacterEllipsis;
            }
        }
    }

    public static double GetContentWidthDip(LabelObject item, double widthDip, bool constrainToBox)
    {
        return Math.Max(1, widthDip - GetLeftPaddingDip(item, constrainToBox) - GetRightPaddingDip(item, constrainToBox));
    }

    public static double GetContentHeightDip(LabelObject item, double heightDip, bool constrainToBox)
    {
        return Math.Max(1, heightDip - GetTopPaddingDip(item, constrainToBox) - GetBottomPaddingDip(item, constrainToBox));
    }

    public static double ResolveVerticalOffset(
        LabelObject item,
        double textHeight,
        double frameHeight,
        bool constrainToBox)
    {
        var top = GetTopPaddingDip(item, constrainToBox);
        var bottom = GetBottomPaddingDip(item, constrainToBox);
        var contentHeight = Math.Max(1, frameHeight - top - bottom);
        return top + TextLayoutContract.ResolveVerticalOffset(item.Style.VerticalAlignment, textHeight, contentHeight, constrainToBox);
    }

    /// <summary>
    /// Extracts the WPF metrics that matter to alignment and overflow before
    /// callers optionally apply a clipping height.  Callers that need an
    /// overflow decision should set MaxTextWidth first but defer MaxTextHeight
    /// until after this method returns.
    /// </summary>
    public static TextLayoutMetrics Measure(
        FormattedText text,
        LabelObject item,
        double widthDip,
        double heightDip,
        bool constrainToBox,
        string? sourceValue = null,
        double pixelsPerDip = 1.0,
        bool? horizontalOverflow = null)
    {
        var lineCount = Math.Max(
            1,
            TextLayoutContract.NormalizeLineEndings(text.Text).Count(character => character == '\n') + 1);
        var lineHeight = Math.Max(1, text.Height / lineCount);
        var contentWidth = GetContentWidthDip(item, widthDip, constrainToBox);
        var verticalOffset = ResolveVerticalOffset(item, text.Height, heightDip, constrainToBox);
        var measuredHorizontalOverflow = horizontalOverflow
            ?? (constrainToBox
                && text.WidthIncludingTrailingWhitespace > contentWidth + HorizontalOverflowToleranceDip);
        var overflowing = constrainToBox
            && (measuredHorizontalOverflow
                || text.Height > GetContentHeightDip(item, heightDip, constrainToBox) + lineHeight * 0.2);
        var metrics = new TextLayoutMetrics(
            WidthDip: text.WidthIncludingTrailingWhitespace,
            HeightDip: text.Height,
            InkExtentDip: text.Extent,
            BaselineDip: text.Baseline,
            LineHeightDip: lineHeight,
            LineCount: lineCount,
            ContentWidthDip: contentWidth,
            VerticalOffsetDip: verticalOffset,
            IsOverflowing: overflowing)
        {
            EffectiveFontSizePt = ResolveFontSizePt(item.Style.FontSizePt, null)
        };
        return sourceValue is null
            ? metrics
            : WithIdentity(metrics, item, sourceValue, widthDip, heightDip, constrainToBox, pixelsPerDip);
    }

    public static TextLayoutMetrics WithIdentity(
        TextLayoutMetrics metrics,
        LabelObject item,
        string sourceValue,
        double frameWidthDip,
        double frameHeightDip,
        bool constrainToBox,
        double pixelsPerDip = 1.0)
    {
        ArgumentNullException.ThrowIfNull(item);
        var snapshot = TextLayoutContract.Capture(sourceValue, item.Style.TextDirection);
        var resource = TextResourceContract.Describe(item.Style);
        return metrics with
        {
            IdentityFingerprint = TextLayoutIdentityContract.ComputeFingerprint(
                new TextLayoutIdentityInput(
                    snapshot.ContentHash,
                    resource.Fingerprint,
                    snapshot.ResolvedDirection,
                    constrainToBox,
                    pixelsPerDip,
                    frameWidthDip,
                    frameHeightDip,
                    metrics.WidthDip,
                    metrics.HeightDip,
                    metrics.InkExtentDip,
                    metrics.BaselineDip,
                    metrics.LineHeightDip,
                    metrics.LineCount,
                    metrics.ContentWidthDip,
                    metrics.VerticalOffsetDip,
                    metrics.IsOverflowing)
                {
                    EffectiveFontSizePt = metrics.EffectiveFontSizePt,
                    HorizontalScale = metrics.HorizontalScale,
                    VerticalScale = metrics.VerticalScale,
                    HorizontalScaleAnchorFraction = metrics.HorizontalScaleAnchorFraction
                })
        };
    }

    public static string WrapTextToBox(
        LabelObject item,
        string value,
        double widthDip,
        double pixelsPerDip = 1.0,
        double? fontSizePtOverride = null)
    {
        return TextLayoutContract.WrapGraphemes(
            value,
            widthDip,
            line => MeasureLineWidth(item, line, pixelsPerDip, fontSizePtOverride));
    }

    private static double MeasureLineWidth(
        LabelObject item,
        string value,
        double pixelsPerDip,
        double? fontSizePtOverride = null)
    {
        if (string.IsNullOrEmpty(value))
        {
            return 0;
        }

        return CreateFormattedText(item, value, Brushes.Black, pixelsPerDip, fontSizePtOverride).WidthIncludingTrailingWhitespace;
    }

    private static double ResolveEffectiveFontSizePt(
        LabelObject item,
        string value,
        double widthDip,
        double heightDip,
        bool constrainToBox,
        double pixelsPerDip)
    {
        var authored = ResolveFontSizePt(item.Style.FontSizePt, null);
        if (!constrainToBox || !UsesShrinkFont(item))
        {
            return authored;
        }

        var configuredMaximum = item.Style.TextFitMaximumFontSizePt > 0
            ? item.Style.TextFitMaximumFontSizePt
            : authored;
        var minimum = Math.Min(item.Style.TextFitMinimumFontSizePt, configuredMaximum);
        var maximum = Math.Max(item.Style.TextFitMinimumFontSizePt, configuredMaximum);
        minimum = Math.Clamp(minimum, 1, 200);
        maximum = Math.Clamp(maximum, minimum, 200);

        if (!FitsAtFontSize(item, value, widthDip, heightDip, pixelsPerDip, minimum))
        {
            // The authored frame is too small even at the configured minimum.
            // Returning the minimum keeps all renderer paths deterministic and
            // lets the normal Error/Clip/Ellipsis policy decide the outcome.
            return minimum;
        }

        if (FitsAtFontSize(item, value, widthDip, heightDip, pixelsPerDip, maximum))
        {
            return maximum;
        }

        var low = minimum;
        var high = maximum;
        for (var iteration = 0; iteration < ShrinkFontIterations; iteration++)
        {
            var candidate = (low + high) / 2.0;
            if (FitsAtFontSize(item, value, widthDip, heightDip, pixelsPerDip, candidate))
            {
                low = candidate;
            }
            else
            {
                high = candidate;
            }
        }

        return Math.Round(low, 4, MidpointRounding.AwayFromZero);
    }

    private static double ResolveHorizontalScale(
        LabelObject item,
        IReadOnlyList<string> lineValues,
        double contentWidth,
        double pixelsPerDip,
        double effectiveFontSizePt,
        bool enabled)
    {
        if (!enabled || contentWidth <= 0)
        {
            return 1.0;
        }

        var widestLine = 0.0;
        foreach (var lineValue in lineValues)
        {
            widestLine = Math.Max(
                widestLine,
                MeasureLineWidth(item, lineValue, pixelsPerDip, effectiveFontSizePt));
        }

        var minimum = Math.Min(item.Style.TextFitMinimumScale, item.Style.TextFitMaximumScale);
        var maximum = Math.Max(item.Style.TextFitMinimumScale, item.Style.TextFitMaximumScale);
        minimum = Math.Clamp(minimum, 0.1, 4.0);
        maximum = Math.Clamp(maximum, minimum, 4.0);
        if (widestLine <= HorizontalOverflowToleranceDip)
        {
            return Math.Clamp(1.0, minimum, maximum);
        }

        return Math.Clamp(contentWidth / widestLine, minimum, maximum);
    }

    private static double ResolveHorizontalScaleAnchorFraction(LabelObject item)
    {
        return item.Style.Alignment switch
        {
            TextAlignmentMode.Center => 0.5,
            TextAlignmentMode.Right => 1.0,
            _ => 0.0
        };
    }

    private static bool FitsAtFontSize(
        LabelObject item,
        string value,
        double widthDip,
        double heightDip,
        double pixelsPerDip,
        double fontSizePt)
    {
        var contentWidth = GetContentWidthDip(item, widthDip, constrainToBox: true);
        var wrapped = WrapTextToBox(item, value, contentWidth, pixelsPerDip, fontSizePt);
        var lines = TextLayoutContract.NormalizeLineEndings(wrapped).Split('\n', StringSplitOptions.None);
        if (lines.Length == 0)
        {
            lines = new[] { string.Empty };
        }

        var maxWidth = 0.0;
        var naturalLineHeight = 1.0;
        foreach (var lineValue in lines)
        {
            var line = CreateFormattedText(
                item,
                string.IsNullOrEmpty(lineValue) ? " " : lineValue,
                Brushes.Black,
                pixelsPerDip,
                fontSizePt);
            maxWidth = Math.Max(maxWidth, string.IsNullOrEmpty(lineValue) ? 0 : line.WidthIncludingTrailingWhitespace);
            naturalLineHeight = Math.Max(naturalLineHeight, ResolveNaturalLineHeightDip(line));
        }

        var lineHeight = TextLayoutContract.ResolveLineHeightDip(naturalLineHeight, item.Style.LineHeightPt);
        var contentHeight = GetContentHeightDip(item, heightDip, constrainToBox: true);
        return maxWidth <= contentWidth + HorizontalOverflowToleranceDip
            && lineHeight * lines.Length <= contentHeight + lineHeight * 0.2;
    }

    private static double ResolveFontSizePt(double authoredFontSizePt, double? overrideFontSizePt)
    {
        var value = overrideFontSizePt ?? authoredFontSizePt;
        return double.IsFinite(value) ? Math.Clamp(value, 1, 200) : 10;
    }

    private static double ResolveNaturalLineHeightDip(FormattedText text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var lineCount = Math.Max(
            1,
            TextLayoutContract.NormalizeLineEndings(text.Text).Count(character => character == '\n') + 1);
        var heightPerLine = text.Height / lineCount;
        return Math.Max(1, Math.Max(text.LineHeight, heightPerLine));
    }

    private static string TrimLineWithEllipsis(
        LabelObject item,
        string value,
        double widthDip,
        double pixelsPerDip,
        double? fontSizePtOverride = null,
        bool forceEllipsis = false)
    {
        value = TextLayoutContract.NormalizeLineEndings(value).Replace("\n", string.Empty, StringComparison.Ordinal);
        if (!forceEllipsis
            && (string.IsNullOrEmpty(value)
                || MeasureLineWidth(item, value, pixelsPerDip, fontSizePtOverride) <= widthDip + HorizontalOverflowToleranceDip))
        {
            return value;
        }

        const string ellipsis = "…";
        var ellipsisWidth = MeasureLineWidth(item, ellipsis, pixelsPerDip, fontSizePtOverride);
        if (ellipsisWidth > widthDip + HorizontalOverflowToleranceDip)
        {
            return string.Empty;
        }

        var result = new System.Text.StringBuilder();
        foreach (var cluster in TextLayoutContract.SegmentGraphemes(value))
        {
            var candidate = result.ToString() + cluster + ellipsis;
            if (MeasureLineWidth(item, candidate, pixelsPerDip, fontSizePtOverride) > widthDip + HorizontalOverflowToleranceDip)
            {
                break;
            }

            result.Append(cluster);
        }

        while (result.Length > 0 && char.IsWhiteSpace(result[^1]))
        {
            result.Length--;
        }

        return result.ToString() + ellipsis;
    }

    private static bool HasHorizontalOverflow(
        LabelObject item,
        string value,
        double contentWidth,
        double pixelsPerDip)
    {
        foreach (var line in TextLayoutContract.NormalizeLineEndings(value).Split('\n', StringSplitOptions.None))
        {
            if (!string.IsNullOrEmpty(line)
                && MeasureLineWidth(item, line, pixelsPerDip) > contentWidth + HorizontalOverflowToleranceDip)
            {
                return true;
            }
        }

        return false;
    }

    private static IReadOnlySet<string> BuildInstalledFontFamilyNames()
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            foreach (var family in Fonts.SystemFontFamilies)
            {
                if (!string.IsNullOrWhiteSpace(family.Source))
                {
                    names.Add(family.Source);
                }

                foreach (var localizedName in family.FamilyNames.Values)
                {
                    if (!string.IsNullOrWhiteSpace(localizedName))
                    {
                        names.Add(localizedName);
                    }
                }
            }
        }
        catch
        {
            // Font enumeration is diagnostic-only. Rendering still has the
            // deterministic Arial fallback if the OS font service is busy.
        }

        return names;
    }

    private static IEnumerable<int> EnumerateCodePoints(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            yield break;
        }

        for (var index = 0; index < value.Length; index++)
        {
            var character = value[index];
            if (char.IsWhiteSpace(character) || char.IsControl(character))
            {
                continue;
            }

            if (char.IsLowSurrogate(character))
            {
                // An unpaired UTF-16 surrogate is itself invalid label data;
                // expose it as a missing code point instead of throwing while
                // the operator is trying to run preflight.
                yield return character;
                continue;
            }

            if (char.IsHighSurrogate(character))
            {
                if (index + 1 >= value.Length || !char.IsLowSurrogate(value[index + 1]))
                {
                    yield return character;
                    continue;
                }

                var codePoint = char.ConvertToUtf32(value, index);
                index++;
                yield return codePoint;
                continue;
            }

            yield return character;
        }
    }

}

public sealed record TextFontObservation(
    string RequestedFamily,
    string ResolvedFamily,
    bool RequestedFamilyAvailable,
    bool GlyphMapAvailable,
    IReadOnlyList<int> MissingGlyphCodePoints)
{
    public bool HasMissingGlyphs => MissingGlyphCodePoints.Count > 0;

    public string MissingGlyphSummary => string.Join(
        ", ",
        MissingGlyphCodePoints
            .Take(8)
            .Select(codePoint => $"U+{codePoint:X4}"));
}
