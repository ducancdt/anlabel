namespace ANLAbel.Core.Models;

/// <summary>
/// Creates an independent authoring copy of a label object for clipboard and
/// duplication flows. Keeping this in Core makes the persisted-property contract
/// testable without WPF and prevents the canvas from silently dropping newly
/// added text, barcode or resource fields.
/// </summary>
public static class LabelObjectCloner
{
    public static LabelObject Clone(LabelObject source)
    {
        ArgumentNullException.ThrowIfNull(source);

        return new LabelObject
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = source.Name,
            Rotation = source.Rotation,
            ZIndex = source.ZIndex,
            IsLocked = source.IsLocked,
            IsVisible = source.IsVisible,
            BindingExpression = source.BindingExpression,
            Text = source.Text,
            BarcodeSymbology = source.BarcodeSymbology,
            BarcodeApplicationProfile = source.BarcodeApplicationProfile,
            QrSizingMode = source.QrSizingMode,
            QrErrorCorrection = source.QrErrorCorrection,
            QrFixedVersion = source.QrFixedVersion,
            QrModuleSizePx = source.QrModuleSizePx,
            QrQuietZoneModules = source.QrQuietZoneModules,
            QrDpi = source.QrDpi,
            ImageDataBase64 = source.ImageDataBase64,
            ImageRasterMode = source.ImageRasterMode,
            ImagePixelWidth = source.ImagePixelWidth,
            ImagePixelHeight = source.ImagePixelHeight,
            BarcodeHriPlacement = source.BarcodeHriPlacement,
            BarcodeTextFontSizePt = source.BarcodeTextFontSizePt,
            BarcodeCheckDigitPolicy = source.BarcodeCheckDigitPolicy,
            BarcodeHriShowCheckDigit = source.BarcodeHriShowCheckDigit,
            BarcodeModuleWidthMm = source.BarcodeModuleWidthMm,
            BarcodeWidthMode = source.BarcodeWidthMode,
            Code39WideNarrowRatio = source.Code39WideNarrowRatio,
            Style = CloneStyle(source.Style),
            // Type is assigned after its dependent binding/QR fields so its
            // own auto-size hook sees the complete source configuration.
            Type = source.Type,
            // Restore authoring geometry after the type hook. A bound matrix
            // object intentionally owns its persisted frame; cloning must not
            // replace it with a transient auto-size computed from empty data.
            XMm = source.XMm,
            YMm = source.YMm,
            WidthMm = source.WidthMm,
            HeightMm = source.HeightMm,
            LineEndXMm = source.LineEndXMm,
            LineEndYMm = source.LineEndYMm
        };
    }

    private static ObjectStyle CloneStyle(ObjectStyle source)
    {
        ArgumentNullException.ThrowIfNull(source);

        return new ObjectStyle
        {
            FontFamily = source.FontFamily,
            FontSizePt = source.FontSizePt,
            LineHeightPt = source.LineHeightPt,
            Bold = source.Bold,
            Italic = source.Italic,
            Underline = source.Underline,
            Alignment = source.Alignment,
            TextDirection = source.TextDirection,
            TextSizing = source.TextSizing,
            TextOverflow = source.TextOverflow,
            TextFitMinimumFontSizePt = source.TextFitMinimumFontSizePt,
            TextFitMaximumFontSizePt = source.TextFitMaximumFontSizePt,
            TextFitMinimumScale = source.TextFitMinimumScale,
            TextFitMaximumScale = source.TextFitMaximumScale,
            VerticalAlignment = source.VerticalAlignment,
            TextPaddingMm = source.TextPaddingMm,
            TextPaddingLeftMm = source.TextPaddingLeftMm,
            TextPaddingRightMm = source.TextPaddingRightMm,
            TextPaddingTopMm = source.TextPaddingTopMm,
            TextPaddingBottomMm = source.TextPaddingBottomMm,
            BorderThicknessMm = source.BorderThicknessMm,
            OutlineStyle = source.OutlineStyle,
            FillStyle = source.FillStyle,
            CornerRadiusMm = source.CornerRadiusMm,
            FillColor = source.FillColor,
            StrokeColor = source.StrokeColor
        };
    }
}
