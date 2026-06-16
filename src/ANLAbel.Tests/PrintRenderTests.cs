using ANLAbel.Barcode.Options;
using ANLAbel.Barcode.Renderers;
using ANLAbel.Core.Enums;
using ANLAbel.Core.Geometry;
using ANLAbel.Core.Models;
using ANLAbel.Printing.RenderPipeline;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Xunit;

namespace ANLAbel.Tests;

public sealed class PrintRenderTests
{
    [Fact]
    public void BarcodeRenderer_ValidatesAndRendersAllSupportedTypes()
    {
        var renderer = new ZxingBarcodeRenderer();
        Assert.True(renderer.ValidateData("ABC123", BarcodeType.Code128));
        Assert.False(renderer.ValidateData(string.Empty, BarcodeType.QRCode));

        var code128 = renderer.RenderBarcode("ABC123", BarcodeType.Code128, 40, 12, 300);
        var qr = renderer.RenderBarcode("Tiếng Việt", BarcodeType.QRCode, 20, 20, 300);
        var dm = renderer.RenderBarcode("PN-001", BarcodeType.DataMatrix, 18, 18, 300);
        var ean13 = renderer.RenderBarcode("893850597419", BarcodeType.Ean13, 35, 12, 300);
        var pdf417 = renderer.RenderBarcode("PN-001 LOT-01", BarcodeType.Pdf417, 42, 16, 300);

        Assert.True(code128.WidthPixels > 100, "Code 128 should render enough pixels at 300 DPI");
        Assert.True(qr.WidthPixels == qr.HeightPixels, "QR should render square image");
        Assert.True(dm.WidthPixels == dm.HeightPixels, "Data Matrix should render square image");
        Assert.True(ean13.WidthPixels > 100, "EAN-13 should render enough pixels at 300 DPI");
        Assert.True(pdf417.WidthPixels > 100, "PDF417 should render enough pixels at 300 DPI");
    }

    [UIFact]
    public void PrintVisualRenderer_ProducesLabelAndCalibrationVisual()
    {
        var template = new LabelTemplate { Name = "Print Test", WidthMm = 60, HeightMm = 30, Dpi = 300 };
        template.Objects.Add(new LabelObject
        {
            Type = ObjectType.Text,
            Text = "PN {PartNo}",
            BindingExpression = "PN {PartNo}",
            XMm = 2, YMm = 2, WidthMm = 30, HeightMm = 8
        });
        template.Objects.Add(new LabelObject
        {
            Type = ObjectType.QRCode,
            BindingExpression = "{PartNo}",
            XMm = 35, YMm = 2, WidthMm = 18, HeightMm = 18
        });

        var renderer = new LabelVisualRenderer();
        var row = new Dictionary<string, string> { ["PartNo"] = "PN-001" };
        var plan = new PrintRenderPlan { Dpi = 300, LabelWidthMm = 60, LabelHeightMm = 30 };

        Assert.NotNull(renderer.Render(template, row, plan));
        Assert.NotNull(renderer.RenderCalibration(plan));
    }

    [UIFact]
    public void PrintPreview_UsesDesignLabelSize_NotPrinterProfileSize()
    {
        var template = new LabelTemplate
        {
            Name = "Design Size Test",
            WidthMm = 60,
            HeightMm = 30,
            Dpi = 300,
            PrinterProfile = { LabelWidthMm = 100, LabelHeightMm = 50 }
        };

        var page = new PrintService()
            .CreatePreviewPages(template, new IReadOnlyDictionary<string, string>?[] { null })
            .Single();

        Assert.Equal(MmConverter.MmToDip(60), page.WidthDip, precision: 3);
        Assert.Equal(MmConverter.MmToDip(30), page.HeightDip, precision: 3);
    }

    [UIFact]
    public void PrintBarcodeObject_UsesObjectDpi_NotLabelDpi()
    {
        var template = new LabelTemplate { Name = "Barcode DPI Test", WidthMm = 40, HeightMm = 25, Dpi = 203 };
        template.Objects.Add(new LabelObject
        {
            Type = ObjectType.QRCode,
            Text = "DPI-CHECK",
            XMm = 2, YMm = 2, WidthMm = 12, HeightMm = 12,
            QrDpi = 300
        });

        var fakeRenderer = new CapturingBarcodeRenderer();
        var renderer = new LabelVisualRenderer(fakeRenderer);
        var plan = new PrintRenderPlan { Dpi = 203, LabelWidthMm = 40, LabelHeightMm = 25 };
        renderer.Render(template, null, plan);

        Assert.Equal(300, fakeRenderer.LastDpi);
    }

    [UIFact]
    public void PrintRenderer_DoesNotClipContentAtLabelEdge()
    {
        var template = new LabelTemplate { Name = "Edge Content Test", WidthMm = 30, HeightMm = 20, MarginMm = 5, Dpi = 300 };
        template.Objects.Add(new LabelObject
        {
            Type = ObjectType.Rectangle,
            XMm = 0, YMm = 0, WidthMm = 5, HeightMm = 5,
            Style = { FillStyle = FillStyle.Solid, FillColor = "#000000", OutlineStyle = OutlineStyle.None }
        });

        var renderer = new LabelVisualRenderer();
        var plan = new PrintRenderPlan { Dpi = 300, LabelWidthMm = 30, LabelHeightMm = 20, MarginMm = 5 };
        var visual = renderer.Render(template, null, plan);
        var bitmap = RenderToBitmap(visual, MmConverter.MmToDip(30), MmConverter.MmToDip(20));
        var pixels = new byte[bitmap.PixelWidth * bitmap.PixelHeight * 4];
        bitmap.CopyPixels(pixels, bitmap.PixelWidth * 4, 0);

        var hasBlackNearOrigin = false;
        var scanWidth = Math.Min(bitmap.PixelWidth, 20);
        var scanHeight = Math.Min(bitmap.PixelHeight, 20);
        for (var y = 0; y < scanHeight && !hasBlackNearOrigin; y++)
        {
            for (var x = 0; x < scanWidth; x++)
            {
                var idx = (y * bitmap.PixelWidth + x) * 4;
                if (pixels[idx] < 20 && pixels[idx + 1] < 20 && pixels[idx + 2] < 20 && pixels[idx + 3] > 200)
                {
                    hasBlackNearOrigin = true;
                    break;
                }
            }
        }

        Assert.True(hasBlackNearOrigin, "Print renderer must not clip design content at label edge because of printable margin");
    }

    private static RenderTargetBitmap RenderToBitmap(Visual visual, double widthDip, double heightDip)
    {
        var bitmap = new RenderTargetBitmap(
            Math.Max(1, (int)Math.Ceiling(widthDip)),
            Math.Max(1, (int)Math.Ceiling(heightDip)),
            96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        return bitmap;
    }
}

file sealed class CapturingBarcodeRenderer : IBarcodeRenderer
{
    public int LastDpi { get; private set; }

    public BarcodePixelImage RenderBarcode(string data, BarcodeType type, double widthMm, double heightMm, int dpi, BarcodeRenderOptions? options = null)
    {
        LastDpi = dpi;
        return new BarcodePixelImage(1, 1, [0, 0, 0, 255]);
    }

    public bool ValidateData(string data, BarcodeType type) => true;

    public string GetBarcodeInfo(string data, BarcodeType type) => string.Empty;

    public BarcodeVectorData? RenderBarcodeVector(string data, BarcodeType type, double widthMm, double heightMm, int dpi, BarcodeRenderOptions? options = null) => null;
}
