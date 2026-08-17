using ANLAbel.Core.Enums;
using ANLAbel.Core.Barcode;
using ANLAbel.Core.Models;
using Xunit;

namespace ANLAbel.UnitTests;

public sealed class LabelObjectClonerTests
{
    [Fact]
    public void Clone_PreservesAllPersistedTextAndResourceProperties_WithoutSharingStyle()
    {
        var source = new LabelObject
        {
            Id = "source",
            Type = ObjectType.Text,
            Name = "Part description",
            XMm = 3.25,
            YMm = 4.5,
            WidthMm = 42,
            HeightMm = 12,
            LineEndXMm = 47.5,
            LineEndYMm = 16.25,
            Rotation = 90,
            ZIndex = 7,
            IsLocked = true,
            IsVisible = false,
            BindingExpression = "{Description}",
            Text = "Fallback",
            BarcodeSymbology = BarcodeSymbology.Code128,
            BarcodeApplicationProfile = BarcodeApplicationProfile.Gs1,
            QrSizingMode = QrSizingMode.FixedVersionAndModuleSize,
            QrErrorCorrection = QrErrorCorrection.Q,
            QrFixedVersion = 12,
            QrModuleSizePx = 9,
            QrQuietZoneModules = 5,
            QrDpi = 305,
            ImageDataBase64 = "embedded-image",
            ImageRasterMode = ImageRasterMode.MonochromeOrderedDither,
            ImagePixelWidth = 640,
            ImagePixelHeight = 480,
            BarcodeHriPlacement = BarcodeHriPlacement.Above,
            BarcodeTextFontSizePt = 8.5,
            BarcodeModuleWidthMm = 0.33,
            BarcodeWidthMode = BarcodeWidthMode.SizedFromX,
            Style = new ObjectStyle
            {
                FontFamily = "Bahnschrift",
                FontSizePt = 13,
                LineHeightPt = 18,
                Bold = true,
                Italic = true,
                Underline = true,
                Alignment = TextAlignmentMode.Right,
                TextDirection = TextDirectionMode.RightToLeft,
                TextSizing = TextSizingMode.FixedFrame,
                TextOverflow = TextOverflowMode.Clip,
                VerticalAlignment = TextVerticalAlignmentMode.Bottom,
                TextPaddingLeftMm = 1.25,
                TextPaddingRightMm = 2.5,
                TextPaddingTopMm = 0.75,
                TextPaddingBottomMm = 3,
                BorderThicknessMm = 0.4,
                OutlineStyle = OutlineStyle.Dash,
                FillStyle = FillStyle.None,
                CornerRadiusMm = 1.2,
                FillColor = "#FF102030",
                StrokeColor = "#FF405060"
            }
        };

        var clone = LabelObjectCloner.Clone(source);

        Assert.NotEqual(source.Id, clone.Id);
        Assert.Equal(source.Type, clone.Type);
        Assert.Equal(source.Name, clone.Name);
        Assert.Equal(source.XMm, clone.XMm, precision: 2);
        Assert.Equal(source.YMm, clone.YMm, precision: 2);
        Assert.Equal(source.WidthMm, clone.WidthMm, precision: 2);
        Assert.Equal(source.HeightMm, clone.HeightMm, precision: 2);
        Assert.Equal(source.LineEndXMm, clone.LineEndXMm, precision: 2);
        Assert.Equal(source.LineEndYMm, clone.LineEndYMm, precision: 2);
        Assert.Equal(source.Rotation, clone.Rotation);
        Assert.Equal(source.ZIndex, clone.ZIndex);
        Assert.Equal(source.IsLocked, clone.IsLocked);
        Assert.Equal(source.IsVisible, clone.IsVisible);
        Assert.Equal(source.BindingExpression, clone.BindingExpression);
        Assert.Equal(source.Text, clone.Text);
        Assert.Equal(source.BarcodeSymbology, clone.BarcodeSymbology);
        Assert.Equal(source.BarcodeApplicationProfile, clone.BarcodeApplicationProfile);
        Assert.Equal(source.QrSizingMode, clone.QrSizingMode);
        Assert.Equal(source.QrErrorCorrection, clone.QrErrorCorrection);
        Assert.Equal(source.QrFixedVersion, clone.QrFixedVersion);
        Assert.Equal(source.QrModuleSizePx, clone.QrModuleSizePx);
        Assert.Equal(source.QrQuietZoneModules, clone.QrQuietZoneModules);
        Assert.Equal(source.QrDpi, clone.QrDpi);
        Assert.Equal(source.ImageDataBase64, clone.ImageDataBase64);
        Assert.Equal(source.ImageRasterMode, clone.ImageRasterMode);
        Assert.Equal(source.ImagePixelWidth, clone.ImagePixelWidth);
        Assert.Equal(source.ImagePixelHeight, clone.ImagePixelHeight);
        Assert.Equal(source.ShowBarcodeText, clone.ShowBarcodeText);
        Assert.Equal(source.BarcodeHriPlacement, clone.BarcodeHriPlacement);
        Assert.Equal(source.BarcodeTextFontSizePt, clone.BarcodeTextFontSizePt, precision: 6);
        Assert.Equal(source.BarcodeModuleWidthMm, clone.BarcodeModuleWidthMm, precision: 2);
        Assert.Equal(source.BarcodeWidthMode, clone.BarcodeWidthMode);

        Assert.NotSame(source.Style, clone.Style);
        Assert.Equal(source.Style.FontFamily, clone.Style.FontFamily);
        Assert.Equal(source.Style.FontSizePt, clone.Style.FontSizePt, precision: 6);
        Assert.Equal(source.Style.LineHeightPt, clone.Style.LineHeightPt, precision: 6);
        Assert.Equal(source.Style.Alignment, clone.Style.Alignment);
        Assert.Equal(source.Style.TextDirection, clone.Style.TextDirection);
        Assert.Equal(source.Style.TextSizing, clone.Style.TextSizing);
        Assert.Equal(source.Style.TextOverflow, clone.Style.TextOverflow);
        Assert.Equal(source.Style.VerticalAlignment, clone.Style.VerticalAlignment);
        Assert.Equal(source.Style.TextPaddingMm, clone.Style.TextPaddingMm, precision: 6);
        Assert.Equal(source.Style.TextPaddingLeftMm, clone.Style.TextPaddingLeftMm, precision: 6);
        Assert.Equal(source.Style.TextPaddingRightMm, clone.Style.TextPaddingRightMm, precision: 6);
        Assert.Equal(source.Style.TextPaddingTopMm, clone.Style.TextPaddingTopMm, precision: 6);
        Assert.Equal(source.Style.TextPaddingBottomMm, clone.Style.TextPaddingBottomMm, precision: 6);
        Assert.Equal(source.Style.OutlineStyle, clone.Style.OutlineStyle);
        Assert.Equal(source.Style.FillStyle, clone.Style.FillStyle);
        Assert.Equal(source.Style.FillColor, clone.Style.FillColor);
        Assert.Equal(source.Style.StrokeColor, clone.Style.StrokeColor);

        source.Style.TextDirection = TextDirectionMode.LeftToRight;
        source.Style.LineHeightPt = 22;
        source.ImageDataBase64 = "changed";
        Assert.Equal(TextDirectionMode.RightToLeft, clone.Style.TextDirection);
        Assert.Equal(18, clone.Style.LineHeightPt, precision: 6);
        Assert.Equal("embedded-image", clone.ImageDataBase64);
    }

    [Fact]
    public void Clone_BoundMatrixPreservesPersistedGeometry_WhenTypeAutoSizeHookRuns()
    {
        var source = new LabelObject
        {
            Type = ObjectType.QRCode,
            BindingExpression = "{PartNumber}",
            WidthMm = 28.37,
            HeightMm = 28.37,
            QrSizingMode = QrSizingMode.AutoSizeByData,
            QrErrorCorrection = QrErrorCorrection.H,
            QrModuleSizePx = 8,
            QrQuietZoneModules = 4,
            QrDpi = 305
        };

        var clone = LabelObjectCloner.Clone(source);

        Assert.Equal(source.WidthMm, clone.WidthMm, precision: 2);
        Assert.Equal(source.HeightMm, clone.HeightMm, precision: 2);
        Assert.Equal(source.BindingExpression, clone.BindingExpression);
        Assert.Equal(source.QrErrorCorrection, clone.QrErrorCorrection);
        Assert.Equal(source.QrModuleSizePx, clone.QrModuleSizePx);
        Assert.Equal(source.QrDpi, clone.QrDpi);
    }
}
