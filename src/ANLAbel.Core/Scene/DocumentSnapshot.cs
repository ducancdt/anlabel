using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using ANLAbel.Core.Barcode;
using ANLAbel.Core.Data;
using ANLAbel.Core.Enums;
using ANLAbel.Core.Models;
using ANLAbel.Core.Printing;
using ANLAbel.Core.Text;

namespace ANLAbel.Core.Scene;

/// <summary>
/// Immutable, UI-independent input for the scene compiler.  The existing WPF model
/// remains the authoring model for now; this adapter gives preview, print and the
/// future retained viewport a stable value boundary without sharing ObservableObjects.
/// </summary>
public sealed record DocumentSnapshot
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public double WidthMm { get; init; }
    public double HeightMm { get; init; }
    public double GapMm { get; init; }
    public double MarginMm { get; init; }
    public LabelOrientation Orientation { get; init; }
    public int Dpi { get; init; }
    public PrinterProfileSnapshot PrinterProfile { get; init; } = new();
    public DatabaseConfigSnapshot DatabaseConfig { get; init; } = new();
    public ImmutableArray<SceneObjectSnapshot> Objects { get; init; } = ImmutableArray<SceneObjectSnapshot>.Empty;
    /// <summary>
    /// Persistent authoring guides are part of the document identity for
    /// save/history/review, but are intentionally not compiled into scene
    /// nodes or emitted to preview/print output.
    /// </summary>
    public ImmutableArray<LabelGuideSnapshot> Guides { get; init; } = ImmutableArray<LabelGuideSnapshot>.Empty;
    public string ExtensionFingerprint { get; init; } = string.Empty;
    public string DataTransformFingerprint { get; init; } = string.Empty;

    public string DocumentHash => SceneHash.ComputeDocumentHash(this);

    /// <summary>
    /// Stable aggregate identity for all text resources in the immutable
    /// document. It is empty when the design contains no text-capable objects.
    /// </summary>
    public string TextResourceFingerprint => SceneHash.ComputeTextResourceFingerprint(this);

    /// <summary>
    /// Aggregate identity of all embedded-image payloads, dimensions and raster
    /// policies. Empty means the design contains no image objects.
    /// </summary>
    public string ImageRasterFingerprint => SceneHash.ComputeImageRasterFingerprint(this);

    public static DocumentSnapshot Capture(LabelTemplate template)
    {
        ArgumentNullException.ThrowIfNull(template);

        return new DocumentSnapshot
        {
            Id = template.Id,
            Name = template.Name,
            WidthMm = template.WidthMm,
            HeightMm = template.HeightMm,
            GapMm = template.GapMm,
            MarginMm = template.MarginMm,
            Orientation = template.Orientation,
            Dpi = template.Dpi,
            PrinterProfile = PrinterProfileSnapshot.Capture(template.PrinterProfile),
            DatabaseConfig = DatabaseConfigSnapshot.Capture(template.DatabaseConfig),
            Objects = template.Objects
                .Select(SceneObjectSnapshot.Capture)
                .ToImmutableArray(),
            Guides = (template.Guides ?? new())
                .Select(LabelGuideSnapshot.Capture)
                .ToImmutableArray(),
            ExtensionFingerprint = TemplateExtensionContract.ComputeFingerprint(template.ExtensionData),
            DataTransformFingerprint = DataTransformPipeline.ComputeFingerprint(template.DataTransforms)
        };
    }
}

public sealed record LabelGuideSnapshot
{
    public string Id { get; init; } = string.Empty;
    public LabelGuideOrientation Orientation { get; init; }
    public double PositionMm { get; init; }
    public bool IsLocked { get; init; }
    public bool IsVisible { get; init; }

    public static LabelGuideSnapshot Capture(LabelGuide guide)
    {
        ArgumentNullException.ThrowIfNull(guide);
        return new LabelGuideSnapshot
        {
            Id = guide.Id,
            Orientation = guide.Orientation,
            PositionMm = guide.PositionMm,
            IsLocked = guide.IsLocked,
            IsVisible = guide.IsVisible
        };
    }
}

public sealed record PrinterProfileSnapshot
{
    public string PrinterName { get; init; } = string.Empty;
    public string PaperName { get; init; } = string.Empty;
    public PrinterSettingsSource SettingsSource { get; init; }
    public PaperSizeSource PaperSizeSource { get; init; }
    public LabelMediaType MediaType { get; init; }
    public FeedDirection FeedDirection { get; init; }
    public bool Rotated180 { get; init; }
    public int Dpi { get; init; }
    public double LabelWidthMm { get; init; }
    public double LabelHeightMm { get; init; }
    public double GapMm { get; init; }
    public double OffsetXMm { get; init; }
    public double OffsetYMm { get; init; }
    public double ScaleX { get; init; }
    public double ScaleY { get; init; }
    public double PhysicalWidthMm { get; init; }
    public double PhysicalHeightMm { get; init; }

    public static PrinterProfileSnapshot Capture(PrinterProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        return new PrinterProfileSnapshot
        {
            PrinterName = profile.PrinterName,
            PaperName = profile.PaperName,
            SettingsSource = profile.SettingsSource,
            PaperSizeSource = profile.PaperSizeSource,
            MediaType = profile.MediaType,
            FeedDirection = profile.FeedDirection,
            Rotated180 = profile.Rotated180,
            Dpi = profile.Dpi,
            LabelWidthMm = profile.LabelWidthMm,
            LabelHeightMm = profile.LabelHeightMm,
            GapMm = profile.GapMm,
            OffsetXMm = profile.OffsetXMm,
            OffsetYMm = profile.OffsetYMm,
            ScaleX = profile.ScaleX,
            ScaleY = profile.ScaleY,
            PhysicalWidthMm = profile.PhysicalWidthMm,
            PhysicalHeightMm = profile.PhysicalHeightMm
        };
    }
}

public sealed record DatabaseConfigSnapshot
{
    public string DataSourceId { get; init; } = string.Empty;
    public string FilePath { get; init; } = string.Empty;
    public string RelativePath { get; init; } = string.Empty;
    public string SheetName { get; init; } = string.Empty;
    public int HeaderRowIndex { get; init; }
    public string KeyField { get; init; } = string.Empty;
    public string KeyValue { get; init; } = string.Empty;
    public int LastSelectedRow { get; init; }
    public string CopiesField { get; init; } = string.Empty;
    public ImmutableArray<DatabaseFieldSnapshot> AvailableFields { get; init; } = ImmutableArray<DatabaseFieldSnapshot>.Empty;
    public ImmutableArray<DatabaseFieldSnapshot> LabelFields { get; init; } = ImmutableArray<DatabaseFieldSnapshot>.Empty;

    public static DatabaseConfigSnapshot Capture(DatabaseConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);
        return new DatabaseConfigSnapshot
        {
            DataSourceId = config.DataSourceId,
            FilePath = config.FilePath,
            RelativePath = config.RelativePath,
            SheetName = config.SheetName,
            HeaderRowIndex = config.HeaderRowIndex,
            KeyField = config.KeyField,
            KeyValue = config.KeyValue,
            LastSelectedRow = config.LastSelectedRow,
            CopiesField = config.CopiesField,
            AvailableFields = config.AvailableFields.Select(DatabaseFieldSnapshot.Capture).ToImmutableArray(),
            LabelFields = config.LabelFields.Select(DatabaseFieldSnapshot.Capture).ToImmutableArray()
        };
    }
}

public sealed record DatabaseFieldSnapshot
{
    public string Name { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string SampleValue { get; init; } = string.Empty;

    public static DatabaseFieldSnapshot Capture(DatabaseField field)
    {
        ArgumentNullException.ThrowIfNull(field);
        return new DatabaseFieldSnapshot
        {
            Name = field.Name,
            DisplayName = field.DisplayName,
            SampleValue = field.SampleValue
        };
    }
}

public sealed record SceneObjectSnapshot
{
    public string Id { get; init; } = string.Empty;
    public ObjectType Type { get; init; }
    public string Name { get; init; } = string.Empty;
    public double XMm { get; init; }
    public double YMm { get; init; }
    public double WidthMm { get; init; }
    public double HeightMm { get; init; }
    public double LineEndXMm { get; init; }
    public double LineEndYMm { get; init; }
    public int Rotation { get; init; }
    public int ZIndex { get; init; }
    public bool IsLocked { get; init; }
    public bool IsVisible { get; init; }
    public string BindingExpression { get; init; } = string.Empty;
    public string Text { get; init; } = string.Empty;
    public BarcodeSymbology BarcodeSymbology { get; init; }
    public BarcodeApplicationProfile BarcodeApplicationProfile { get; init; }
    public QrSizingMode QrSizingMode { get; init; }
    public QrErrorCorrection QrErrorCorrection { get; init; }
    public int QrFixedVersion { get; init; }
    public int QrModuleSizePx { get; init; }
    public int QrQuietZoneModules { get; init; }
    public int QrDpi { get; init; }
    public bool ShowBarcodeText { get; init; }
    public BarcodeHriPlacement BarcodeHriPlacement { get; init; }
    public double BarcodeTextFontSizePt { get; init; }
    public BarcodeCheckDigitPolicy BarcodeCheckDigitPolicy { get; init; }
    public bool BarcodeHriShowCheckDigit { get; init; }
    public double BarcodeModuleWidthMm { get; init; }
    public BarcodeWidthMode BarcodeWidthMode { get; init; }
    public Code39WideNarrowRatio Code39WideNarrowRatio { get; init; }
    /// <summary>
    /// Embedded image bytes are retained in the immutable render snapshot so a
    /// compiled presenter never has to read the mutable authoring object again.
    /// The canonical document hash uses the fingerprint below rather than the
    /// raw payload, keeping hashes compact and free of label contents.
    /// </summary>
    public string ImageDataBase64 { get; init; } = string.Empty;
    public string ImageDataFingerprint { get; init; } = string.Empty;
    public int ImageDataLength { get; init; }
    public ImageRasterMode ImageRasterMode { get; init; } = ImageRasterMode.DriverManaged;
    public int ImagePixelWidth { get; init; }
    public int ImagePixelHeight { get; init; }
    public string ImageRasterFingerprint { get; init; } = string.Empty;
    public ObjectStyleSnapshot Style { get; init; } = new();

    public static SceneObjectSnapshot Capture(LabelObject item)
    {
        ArgumentNullException.ThrowIfNull(item);
        var imageData = item.ImageDataBase64 ?? string.Empty;
        var imageFingerprint = Fingerprint(imageData);
        return new SceneObjectSnapshot
        {
            Id = item.Id,
            Type = item.Type,
            Name = item.Name,
            XMm = item.XMm,
            YMm = item.YMm,
            WidthMm = item.WidthMm,
            HeightMm = item.HeightMm,
            LineEndXMm = item.LineEndXMm,
            LineEndYMm = item.LineEndYMm,
            Rotation = item.Rotation,
            ZIndex = item.ZIndex,
            IsLocked = item.IsLocked,
            IsVisible = item.IsVisible,
            BindingExpression = item.BindingExpression,
            Text = item.Text,
            BarcodeSymbology = item.BarcodeSymbology,
            BarcodeApplicationProfile = item.BarcodeApplicationProfile,
            QrSizingMode = item.QrSizingMode,
            QrErrorCorrection = item.QrErrorCorrection,
            QrFixedVersion = item.QrFixedVersion,
            QrModuleSizePx = item.QrModuleSizePx,
            QrQuietZoneModules = item.QrQuietZoneModules,
            QrDpi = item.QrDpi,
            ShowBarcodeText = item.ShowBarcodeText,
            BarcodeHriPlacement = item.BarcodeHriPlacement,
            BarcodeTextFontSizePt = item.BarcodeTextFontSizePt,
            BarcodeCheckDigitPolicy = item.BarcodeCheckDigitPolicy,
            BarcodeHriShowCheckDigit = item.BarcodeHriShowCheckDigit,
            BarcodeModuleWidthMm = item.BarcodeModuleWidthMm,
            BarcodeWidthMode = item.BarcodeWidthMode,
            Code39WideNarrowRatio = item.Code39WideNarrowRatio,
            ImageDataBase64 = imageData,
            ImageDataFingerprint = imageFingerprint,
            ImageDataLength = imageData.Length,
            ImageRasterMode = item.ImageRasterMode,
            ImagePixelWidth = item.ImagePixelWidth,
            ImagePixelHeight = item.ImagePixelHeight,
            ImageRasterFingerprint = ImageRasterContract.ComputeFingerprint(
                imageFingerprint,
                imageData.Length,
                item.ImageRasterMode,
                item.ImagePixelWidth,
                item.ImagePixelHeight),
            Style = ObjectStyleSnapshot.Capture(item.Style)
        };
    }

    private static string Fingerprint(string value)
    {
        if (value.Length == 0)
        {
            return string.Empty;
        }

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    }
}

public sealed record ObjectStyleSnapshot
{
    public string FontFamily { get; init; } = string.Empty;
    public double FontSizePt { get; init; }
    public double LineHeightPt { get; init; }
    public bool Bold { get; init; }
    public bool Italic { get; init; }
    public bool Underline { get; init; }
    public TextAlignmentMode Alignment { get; init; }
    public TextDirectionMode TextDirection { get; init; }
    public TextSizingMode TextSizing { get; init; }
    public TextOverflowMode TextOverflow { get; init; }
    public double TextFitMinimumFontSizePt { get; init; }
    public double TextFitMaximumFontSizePt { get; init; }
    public double TextFitMinimumScale { get; init; }
    public double TextFitMaximumScale { get; init; }
    public TextVerticalAlignmentMode? VerticalAlignment { get; init; }
    public double TextPaddingMm { get; init; }
    public double TextPaddingLeftMm { get; init; }
    public double TextPaddingRightMm { get; init; }
    public double TextPaddingTopMm { get; init; }
    public double TextPaddingBottomMm { get; init; }
    public double BorderThicknessMm { get; init; }
    public OutlineStyle OutlineStyle { get; init; }
    public FillStyle FillStyle { get; init; }
    public double CornerRadiusMm { get; init; }
    public string FillColor { get; init; } = string.Empty;
    public string StrokeColor { get; init; } = string.Empty;
    /// <summary>
    /// Canonical requested font/style/fallback identity. This is not an
    /// assertion that the font is installed; preflight supplies that runtime
    /// observation separately.
    /// </summary>
    public string TextResourceFingerprint { get; init; } = string.Empty;

    public static ObjectStyleSnapshot Capture(ObjectStyle style)
    {
        ArgumentNullException.ThrowIfNull(style);
        return new ObjectStyleSnapshot
        {
            FontFamily = style.FontFamily,
            FontSizePt = style.FontSizePt,
            LineHeightPt = style.LineHeightPt,
            Bold = style.Bold,
            Italic = style.Italic,
            Underline = style.Underline,
            Alignment = style.Alignment,
            TextDirection = style.TextDirection,
            TextSizing = style.TextSizing,
            TextOverflow = style.TextOverflow,
            TextFitMinimumFontSizePt = style.TextFitMinimumFontSizePt,
            TextFitMaximumFontSizePt = style.TextFitMaximumFontSizePt,
            TextFitMinimumScale = style.TextFitMinimumScale,
            TextFitMaximumScale = style.TextFitMaximumScale,
            VerticalAlignment = style.VerticalAlignment,
            TextPaddingMm = style.TextPaddingMm,
            TextPaddingLeftMm = style.TextPaddingLeftMm,
            TextPaddingRightMm = style.TextPaddingRightMm,
            TextPaddingTopMm = style.TextPaddingTopMm,
            TextPaddingBottomMm = style.TextPaddingBottomMm,
            BorderThicknessMm = style.BorderThicknessMm,
            OutlineStyle = style.OutlineStyle,
            FillStyle = style.FillStyle,
            CornerRadiusMm = style.CornerRadiusMm,
            FillColor = style.FillColor,
            StrokeColor = style.StrokeColor,
            TextResourceFingerprint = TextResourceContract.Describe(style).Fingerprint
        };
    }
}

internal static class SceneHash
{
    public static string ComputeTextResourceFingerprint(DocumentSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        var textObjects = snapshot.Objects
            .Where(item => item.Type is ObjectType.Text or ObjectType.TextBox)
            .OrderBy(item => item.ZIndex)
            .ThenBy(item => item.Id, StringComparer.Ordinal)
            .ToArray();
        if (textObjects.Length == 0)
        {
            return string.Empty;
        }

        var canonical = new StringBuilder();
        Append(canonical, TextResourceContract.ContractVersion);
        foreach (var item in textObjects)
        {
            Append(canonical, item.Id);
            Append(canonical, item.Style.TextResourceFingerprint);
        }

        return Hash(canonical);
    }

    public static string ComputeImageRasterFingerprint(DocumentSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        var imageObjects = snapshot.Objects
            .Where(item => item.Type == ObjectType.Image)
            .OrderBy(item => item.ZIndex)
            .ThenBy(item => item.Id, StringComparer.Ordinal)
            .ToArray();
        if (imageObjects.Length == 0)
        {
            return string.Empty;
        }

        var canonical = new StringBuilder();
        Append(canonical, ImageRasterContract.ContractVersion);
        foreach (var item in imageObjects)
        {
            Append(canonical, item.Id);
            Append(canonical, item.ImageDataFingerprint);
            Append(canonical, item.ImageDataLength);
            Append(canonical, item.ImageRasterMode.ToString());
            Append(canonical, item.ImagePixelWidth);
            Append(canonical, item.ImagePixelHeight);
            Append(canonical, item.ImageRasterFingerprint);
        }

        return Hash(canonical);
    }

    public static string ComputeDocumentHash(DocumentSnapshot snapshot)
    {
        var canonical = new StringBuilder();
        AppendDocument(canonical, snapshot, includeAuthoringGuides: true);
        return Hash(canonical);
    }

    public static string ComputeSceneHash(DocumentSnapshot snapshot, IEnumerable<CompiledSceneNode> nodes)
    {
        var canonical = new StringBuilder();
        AppendDocument(canonical, snapshot, includeAuthoringGuides: false);
        foreach (var node in nodes.OrderBy(item => item.ZIndex).ThenBy(item => item.Id, StringComparer.Ordinal))
        {
            Append(canonical, node.Id);
            Append(canonical, node.Type.ToString());
            AppendBounds(canonical, node.LayoutBoundsMm);
            AppendBounds(canonical, node.VisualBoundsMm);
            Append(canonical, node.Rotation);
            Append(canonical, node.IsVisible);
            Append(canonical, node.ZIndex);
        }

        return Hash(canonical);
    }

    private static void AppendDocument(StringBuilder builder, DocumentSnapshot snapshot, bool includeAuthoringGuides)
    {
        Append(builder, snapshot.Id);
        Append(builder, snapshot.Name);
        Append(builder, snapshot.WidthMm);
        Append(builder, snapshot.HeightMm);
        Append(builder, snapshot.GapMm);
        Append(builder, snapshot.MarginMm);
        Append(builder, snapshot.Orientation.ToString());
        Append(builder, snapshot.Dpi);
        Append(builder, snapshot.PrinterProfile.PrinterName);
        Append(builder, snapshot.PrinterProfile.PaperName);
        Append(builder, snapshot.PrinterProfile.Dpi);
        Append(builder, snapshot.PrinterProfile.LabelWidthMm);
        Append(builder, snapshot.PrinterProfile.LabelHeightMm);
        Append(builder, snapshot.PrinterProfile.MediaType.ToString());
        Append(builder, snapshot.PrinterProfile.FeedDirection.ToString());
        Append(builder, snapshot.DatabaseConfig.DataSourceId);
        Append(builder, snapshot.DatabaseConfig.FilePath);
        Append(builder, snapshot.DatabaseConfig.RelativePath);
        Append(builder, snapshot.ExtensionFingerprint);
        Append(builder, snapshot.DataTransformFingerprint);

        if (includeAuthoringGuides)
        {
            foreach (var guide in snapshot.Guides
                         .OrderBy(item => item.Orientation)
                         .ThenBy(item => item.PositionMm)
                         .ThenBy(item => item.Id, StringComparer.Ordinal))
            {
                Append(builder, guide.Id);
                Append(builder, guide.Orientation.ToString());
                Append(builder, guide.PositionMm);
                Append(builder, guide.IsLocked);
                Append(builder, guide.IsVisible);
            }
        }

        foreach (var item in snapshot.Objects
                     .OrderBy(item => item.ZIndex)
                     .ThenBy(item => item.Id, StringComparer.Ordinal))
        {
            AppendObject(builder, item);
        }
    }

    private static void AppendObject(StringBuilder builder, SceneObjectSnapshot item)
    {
        Append(builder, item.Id);
        Append(builder, item.Type.ToString());
        Append(builder, item.Name);
        Append(builder, item.XMm);
        Append(builder, item.YMm);
        Append(builder, item.WidthMm);
        Append(builder, item.HeightMm);
        Append(builder, item.LineEndXMm);
        Append(builder, item.LineEndYMm);
        Append(builder, item.Rotation);
        Append(builder, item.ZIndex);
        Append(builder, item.IsLocked);
        Append(builder, item.IsVisible);
        Append(builder, item.BindingExpression);
        Append(builder, item.Text);
        Append(builder, item.BarcodeSymbology.ToString());
        Append(builder, item.BarcodeApplicationProfile.ToString());
        Append(builder, item.QrSizingMode.ToString());
        Append(builder, item.QrErrorCorrection.ToString());
        Append(builder, item.QrFixedVersion);
        Append(builder, item.QrModuleSizePx);
        Append(builder, item.QrQuietZoneModules);
        Append(builder, item.QrDpi);
        Append(builder, item.ShowBarcodeText);
        Append(builder, item.BarcodeHriPlacement.ToString());
        Append(builder, item.BarcodeTextFontSizePt);
        Append(builder, item.BarcodeCheckDigitPolicy.ToString());
        Append(builder, item.BarcodeHriShowCheckDigit);
        Append(builder, item.BarcodeModuleWidthMm);
        Append(builder, item.BarcodeWidthMode.ToString());
        Append(builder, item.Code39WideNarrowRatio.ToString());
        Append(builder, item.ImageDataFingerprint);
        Append(builder, item.ImageDataLength);
        Append(builder, item.ImageRasterMode.ToString());
        Append(builder, item.ImagePixelWidth);
        Append(builder, item.ImagePixelHeight);
        Append(builder, item.ImageRasterFingerprint);
        Append(builder, item.Style.FontFamily);
        Append(builder, item.Style.TextResourceFingerprint);
        Append(builder, item.Style.FontSizePt);
        Append(builder, item.Style.LineHeightPt);
        Append(builder, item.Style.Bold);
        Append(builder, item.Style.Italic);
        Append(builder, item.Style.Underline);
        Append(builder, item.Style.Alignment.ToString());
        Append(builder, item.Style.TextDirection.ToString());
        Append(builder, item.Style.TextSizing.ToString());
        Append(builder, item.Style.TextOverflow.ToString());
        Append(builder, item.Style.TextFitMinimumFontSizePt);
        Append(builder, item.Style.TextFitMaximumFontSizePt);
        Append(builder, item.Style.TextFitMinimumScale);
        Append(builder, item.Style.TextFitMaximumScale);
        Append(builder, item.Style.VerticalAlignment?.ToString());
        Append(builder, item.Style.TextPaddingMm);
        Append(builder, item.Style.TextPaddingLeftMm);
        Append(builder, item.Style.TextPaddingRightMm);
        Append(builder, item.Style.TextPaddingTopMm);
        Append(builder, item.Style.TextPaddingBottomMm);
        Append(builder, item.Style.BorderThicknessMm);
        Append(builder, item.Style.OutlineStyle.ToString());
        Append(builder, item.Style.FillStyle.ToString());
        Append(builder, item.Style.CornerRadiusMm);
        Append(builder, item.Style.FillColor);
        Append(builder, item.Style.StrokeColor);
    }

    private static void AppendBounds(StringBuilder builder, SceneBounds bounds)
    {
        Append(builder, bounds.LeftMm);
        Append(builder, bounds.TopMm);
        Append(builder, bounds.WidthMm);
        Append(builder, bounds.HeightMm);
    }

    private static void Append(StringBuilder builder, string? value)
    {
        if (value is null)
        {
            builder.Append("-1:");
            return;
        }

        builder.Append(value.Length.ToString(System.Globalization.CultureInfo.InvariantCulture))
            .Append(':')
            .Append(value);
    }

    private static void Append(StringBuilder builder, double value)
        => Append(builder, value.ToString("R", System.Globalization.CultureInfo.InvariantCulture));

    private static void Append(StringBuilder builder, int value)
        => Append(builder, value.ToString(System.Globalization.CultureInfo.InvariantCulture));

    private static void Append(StringBuilder builder, bool value)
        => Append(builder, value ? "1" : "0");

    private static string Hash(StringBuilder builder)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString())));
}
