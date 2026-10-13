using ANLAbel.Barcode.Options;
using ANLAbel.Barcode.Renderers;
using ANLAbel.Core.Data;
using ANLAbel.Core.Expressions;
using ANLAbel.Data;
using ANLAbel.Data.Excel;
using ClosedXML.Excel;
using Xunit;
using ZXing;
using ZXing.Common;

namespace ANLAbel.UnitTests;

public sealed class ImportedBarcodeRoundTripTests
{
    public static IEnumerable<object[]> Cases()
    {
        foreach (var extension in new[] { ".csv", ".xlsx" })
        foreach (var dpi in new[] { 203, 300, 600 })
        {
            yield return new object[] { extension, dpi, BarcodeType.Code128, " 000123 " };
            yield return new object[] { extension, dpi, BarcodeType.QRCode, " 000123\nLô A " };
            yield return new object[] { extension, dpi, BarcodeType.DataMatrix, "LOT-2026-X" };
            yield return new object[] { extension, dpi, BarcodeType.Code39, "CODE-39" };
            yield return new object[] { extension, dpi, BarcodeType.Ean13, "5901234123457" };
            yield return new object[] { extension, dpi, BarcodeType.ITF, "1234567890" };
            yield return new object[] { extension, dpi, BarcodeType.Code93, "CODE93" };
            yield return new object[] { extension, dpi, BarcodeType.Ean8, "96385074" };
            yield return new object[] { extension, dpi, BarcodeType.UpcA, "036000291452" };
            yield return new object[] { extension, dpi, BarcodeType.UpcE, "01234565" };
            yield return new object[] { extension, dpi, BarcodeType.Codabar, "A123456B" };
            yield return new object[] { extension, dpi, BarcodeType.MSI, "123456" };
            yield return new object[] { extension, dpi, BarcodeType.Plessey, "98765" };
            yield return new object[] { extension, dpi, BarcodeType.Pdf417, "PDF417-2026" };
            yield return new object[] { extension, dpi, BarcodeType.Aztec, "AZTEC-2026" };
        }
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public async Task ImportedBoundPayloadDecodesExactly(string extension, int dpi, BarcodeType type, string expected)
    {
        var path = Path.Combine(Path.GetTempPath(), $"anlabel-roundtrip-{Guid.NewGuid():N}{extension}");
        var sheetName = extension == ".csv" ? ExcelDataService.CsvSheetName : "Data";
        try
        {
            if (extension == ".csv")
                await File.WriteAllTextAsync(path, "Code\n\"" + expected + "\"\n");
            else
            {
                using var workbook = new XLWorkbook();
                var sheet = workbook.AddWorksheet(sheetName);
                sheet.Cell(1, 1).Value = "Code";
                sheet.Cell(2, 1).Value = expected;
                workbook.SaveAs(path);
            }

            var table = await new ExcelDataService().LoadSheetAsync(path, sheetName);
            var connector = new DataTableDataConnector(
                new DataConnectorDescriptor("test", "test", extension, true, true), table);
            var page = await connector.ReadPageAsync(new DataReadRequest());
            var record = Assert.Single(page.Records);
            var row = record.Values.ToDictionary(pair => pair.Key, pair => pair.Value ?? string.Empty);
            var bound = BindingExpressionEvaluator.Evaluate("{Code}", row);
            Assert.Equal(expected, bound);

            var renderer = new ZxingBarcodeRenderer();
            Assert.True(renderer.ValidateData(bound, type));
            var options = type == BarcodeType.UpcE ? new BarcodeRenderOptions { QuietZoneModules = 6 } : null;
            var image = renderer.RenderBarcode(bound, type, 60, 40, dpi, options);
            var source = new RGBLuminanceSource(image.BgraPixels, image.WidthPixels, image.HeightPixels,
                RGBLuminanceSource.BitmapFormat.BGRA32);
            var hints = new Dictionary<DecodeHintType, object>
            {
                [DecodeHintType.POSSIBLE_FORMATS] = new[] { ToExpectedFormat(type) },
                [DecodeHintType.TRY_HARDER] = true,
                [DecodeHintType.PURE_BARCODE] = type == BarcodeType.DataMatrix,
                [DecodeHintType.RETURN_CODABAR_START_END] = true
            };
            var decoded = type == BarcodeType.Plessey
                ? DecodePlesseyImage(image, expected.Length)
                : new MultiFormatReader().decode(new BinaryBitmap(new HybridBinarizer(source)), hints);
            Assert.NotNull(decoded);
            Assert.Equal(expected, decoded.Text);
            Assert.Equal(ToExpectedFormat(type), decoded.BarcodeFormat);
        }
        finally { File.Delete(path); }
    }

    private static Result? DecodePlesseyImage(BarcodePixelImage image, int expectedLength)
    {
        var midY = image.HeightPixels / 2;
        var width = image.WidthPixels;
        var rowBits = new bool[width];
        for (var x = 0; x < width; x++)
        {
            var offset = (midY * width + x) * 4;
            rowBits[x] = image.BgraPixels[offset] < 128;
        }

        var runs = new List<(bool Black, int Length)>();
        var current = rowBits[0];
        var len = 0;
        for (var x = 0; x < width; x++)
        {
            if (rowBits[x] == current) len++;
            else
            {
                runs.Add((current, len));
                current = rowBits[x];
                len = 1;
            }
        }
        runs.Add((current, len));

        var runIndex = 0;
        if (!runs[0].Black) runIndex = 1;

        var bits = new List<int>();
        while (runIndex + 1 < runs.Count)
        {
            var bar = runs[runIndex];
            var space = runs[runIndex + 1];
            if (!bar.Black || space.Black) break;
            bits.Add(bar.Length > space.Length ? 1 : 0);
            runIndex += 2;
        }

        if (bits.Count < 4 + expectedLength * 4) return null;
        if (bits[0] != 1 || bits[1] != 1 || bits[2] != 0 || bits[3] != 1) return null;

        var text = new System.Text.StringBuilder();
        for (var i = 4; i + 4 <= 4 + expectedLength * 4; i += 4)
        {
            var val = bits[i] | (bits[i + 1] << 1) | (bits[i + 2] << 2) | (bits[i + 3] << 3);
            if (val is >= 0 and <= 9)
                text.Append((char)('0' + val));
            else if (val is >= 10 and <= 15)
                text.Append((char)('A' + (val - 10)));
        }

        return new Result(text.ToString(), null, null, BarcodeFormat.PLESSEY);
    }

    private static BarcodeFormat ToExpectedFormat(BarcodeType type) => type switch
    {
        BarcodeType.Code128 => BarcodeFormat.CODE_128,
        BarcodeType.QRCode => BarcodeFormat.QR_CODE,
        BarcodeType.DataMatrix => BarcodeFormat.DATA_MATRIX,
        BarcodeType.Code39 => BarcodeFormat.CODE_39,
        BarcodeType.Ean13 => BarcodeFormat.EAN_13,
        BarcodeType.ITF => BarcodeFormat.ITF,
        BarcodeType.Code93 => BarcodeFormat.CODE_93,
        BarcodeType.Ean8 => BarcodeFormat.EAN_8,
        BarcodeType.UpcA => BarcodeFormat.UPC_A,
        BarcodeType.UpcE => BarcodeFormat.UPC_E,
        BarcodeType.Codabar => BarcodeFormat.CODABAR,
        BarcodeType.MSI => BarcodeFormat.MSI,
        BarcodeType.Plessey => BarcodeFormat.PLESSEY,
        BarcodeType.Pdf417 => BarcodeFormat.PDF_417,
        BarcodeType.Aztec => BarcodeFormat.AZTEC,
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };
}
