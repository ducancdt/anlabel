using System.Data;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ANLAbel.Barcode.Renderers;
using ANLAbel.Core.Enums;
using ANLAbel.Core.Geometry;
using ANLAbel.Core.Models;
using ANLAbel.Data.Excel;
using ANLAbel.Printing.PrinterProfiles;
using ANLAbel.Printing.RenderPipeline;
using ANLAbel.Project.SaveLoad;
using ClosedXML.Excel;
using ZXing;
using ZXing.Common;

/// <summary>
/// The A5 test sheet (owner request 2026-10-30): one 148 × 210 mm label with
/// every barcode type ANLAbel prints, each bound to a column of a real Excel
/// workbook (text, numbers, a leading-zero number format, dates, Vietnamese).
/// The workbook is written with ClosedXML and read back through the
/// application's own <see cref="ExcelDataService"/>; every row is preflighted,
/// printed through the paginator's renderer at the office printer's DPI and
/// every symbol is decoded. Rows with data a symbology cannot carry must be
/// blocked before printing. Set ANLABEL_A5_KIT_DIR to also keep the workbook
/// and the .anlabel there for a real print.
/// </summary>
internal static class DatabaseBarcodeRegression
{
    public static Task EveryBarcodeOnAnA5SheetDecodesForEveryExcelRow()
    {
        var directory = KitDirectory();
        var workbook = Path.Combine(directory, A5BarcodeSheet.WorkbookName);
        A5BarcodeSheet.WriteWorkbook(workbook);
        var template = A5BarcodeSheet.CreateTemplate(workbook);
        new ProjectFileService().SaveAsync(template, Path.Combine(directory, A5BarcodeSheet.TemplateName)).GetAwaiter().GetResult();
        var rows = A5BarcodeSheet.ReadRows(workbook, A5BarcodeSheet.SheetName);
        Require(rows.Count == A5BarcodeSheet.RowCount, $"The workbook must read back {A5BarcodeSheet.RowCount} rows, read {rows.Count}.");

        // The Excel reader keeps numbers whole and the leading zeros of a formatted number.
        Require(rows[0]["UPCA"] == "03600029001", "UPC-A typed as a number with format 00000000000 must keep its leading zero: " + rows[0]["UPCA"]);
        Require(rows[0]["EAN13"] == "893456000007", "A 12-digit EAN typed as a number must read as digits, not 8.93E+11: " + rows[0]["EAN13"]);
        Require(rows[1]["ITF14"] == A5BarcodeSheet.WithGs1Check("1893456000002"), "A 14-digit ITF typed as a number must read whole: " + rows[1]["ITF14"]);
        Require(rows[0]["HSD"] == "2027-01-04", "A date cell must read as an ISO date: " + rows[0]["HSD"]);

        var failures = new List<string>();
        foreach (var dpi in new[] { 300, 600 })
        {
            var preflight = new PrintPreflightValidator(new ZxingBarcodeRenderer()).Validate(template, rows.ToArray(), dpi);
            foreach (var issue in preflight.Issues.Where(issue => issue.IsBlocking))
            {
                failures.Add($"{dpi} DPI preflight blocked {issue.Summary}");
            }

            // 600 DPI is the real print; it is checked on a third of the rows to keep the gate fast.
            var printed = dpi == 300 ? Enumerable.Range(0, rows.Count) : Enumerable.Range(0, rows.Count).Where(index => index % 3 == 0);
            foreach (var index in printed)
            {
                var row = rows[index];
                var page = RunOnSta(() => Rasterize(template, row, dpi));
                foreach (var slot in A5BarcodeSheet.Slots)
                {
                    var expected = A5BarcodeSheet.Expected(slot, row[slot.Column]);
                    var decoded = Decode(page, slot, dpi);
                    if (decoded != expected)
                    {
                        failures.Add($"{dpi} DPI row {index + 1} {slot.Caption}: decoded '{decoded}', expected '{expected}'.");
                    }
                }
            }
        }

        Console.WriteLine($"MEASURE A5 sheet: {rows.Count} Excel rows x {A5BarcodeSheet.Slots.Count} barcode types decoded at 300 DPI, every third row at 600 DPI; kit in {directory}");
        if (failures.Count > 0)
        {
            throw new InvalidOperationException(string.Join(Environment.NewLine, failures.GroupBy(failure => failure.Split(':')[0].Split(' ').Last()).SelectMany(group => group.Take(3)).Take(60)));
        }

        return Task.CompletedTask;
    }

    public static Task ExcelDataABarcodeCannotCarryBlocksItsRow()
    {
        var directory = KitDirectory();
        var workbook = Path.Combine(directory, "A5-loi.xlsx");
        A5BarcodeSheet.WriteWorkbook(workbook, withInvalidSheet: true);
        var template = A5BarcodeSheet.CreateTemplate(workbook);
        var rows = A5BarcodeSheet.ReadRows(workbook, A5BarcodeSheet.InvalidSheetName);
        var preflight = new PrintPreflightValidator(new ZxingBarcodeRenderer()).Validate(template, rows.ToArray(), 600);
        var failures = new List<string>();
        for (var index = 0; index < A5BarcodeSheet.InvalidCases.Count; index++)
        {
            var (column, value) = A5BarcodeSheet.InvalidCases[index];
            var slot = A5BarcodeSheet.Slots.Single(item => item.Column == column);
            var blocked = preflight.Issues.Where(issue => issue.IsBlocking && issue.RowNumber == index + 1).ToArray();
            if (!blocked.Any(issue => issue.ObjectName == slot.ObjectName))
            {
                failures.Add($"Row {index + 1}: {slot.Caption} '{value}' must block its row; got "
                    + (blocked.Length == 0 ? "no blocking issue" : string.Join(" | ", blocked.Select(issue => issue.Summary))));
            }

            if (blocked.Any(issue => issue.ObjectName != slot.ObjectName))
            {
                failures.Add($"Row {index + 1}: only {slot.Caption} is wrong, but also blocked: "
                    + string.Join(" | ", blocked.Where(issue => issue.ObjectName != slot.ObjectName).Select(issue => issue.Summary)));
            }
        }

        if (failures.Count > 0)
        {
            throw new InvalidOperationException(string.Join(Environment.NewLine, failures));
        }

        return Task.CompletedTask;
    }

    private sealed record Page(byte[] Gray, int Width, int Height);

    private static Page Rasterize(LabelTemplate template, IReadOnlyDictionary<string, string> row, int dpi)
    {
        var plan = new PrintRenderPlan
        {
            Dpi = dpi,
            DpiX = dpi,
            DpiY = dpi,
            LabelWidthMm = template.WidthMm,
            LabelHeightMm = template.HeightMm,
            ScaleX = 1,
            ScaleY = 1
        };
        var visual = new LabelVisualRenderer().Render(template, row, plan);
        var width = (int)Math.Ceiling(template.WidthMm / 25.4 * dpi);
        var height = (int)Math.Ceiling(template.HeightMm / 25.4 * dpi);
        var bitmap = new RenderTargetBitmap(width, height, dpi, dpi, PixelFormats.Pbgra32);
        var background = new DrawingVisual();
        using (var context = background.RenderOpen())
        {
            context.DrawRectangle(Brushes.White, null, new Rect(0, 0, MmConverter.MmToDip(template.WidthMm), MmConverter.MmToDip(template.HeightMm)));
        }

        bitmap.Render(background);
        bitmap.Render(visual);
        var pixels = new byte[width * height * 4];
        bitmap.CopyPixels(pixels, width * 4, 0);
        var gray = new byte[width * height];
        for (var index = 0; index < gray.Length; index++)
        {
            gray[index] = (byte)((pixels[index * 4] + pixels[index * 4 + 1] + pixels[index * 4 + 2]) / 3);
        }

        return new Page(gray, width, height);
    }

    /// <summary>Decodes the slot's area as a scanner sees one code: its column of the sheet, its own band.</summary>
    private static string Decode(Page page, A5BarcodeSheet.Slot slot, int dpi)
    {
        if (slot.Symbology == BarcodeSymbology.Plessey)
        {
            // ZXing.Net has no Plessey reader.
            return DecodePlessey(page, slot, dpi);
        }

        int Px(double mm) => (int)Math.Round(mm / 25.4 * dpi);
        var (left, right) = slot.AreaXMm;
        var x0 = Math.Max(0, Px(left));
        var x1 = Math.Min(page.Width, Px(right));
        var y0 = Math.Max(0, Px(slot.YMm - 1.5));
        var y1 = Math.Min(page.Height, Px(slot.YMm + slot.HeightMm + 1.5));
        var cropWidth = x1 - x0;
        var cropHeight = y1 - y0;
        var crop = new byte[cropWidth * cropHeight];
        for (var y = 0; y < cropHeight; y++)
        {
            Array.Copy(page.Gray, (y0 + y) * page.Width + x0, crop, y * cropWidth, cropWidth);
        }

        var hints = new Dictionary<DecodeHintType, object>
        {
            [DecodeHintType.POSSIBLE_FORMATS] = new[] { slot.Format },
            [DecodeHintType.TRY_HARDER] = true,
            [DecodeHintType.RETURN_CODABAR_START_END] = true
        };
        var dump = Environment.GetEnvironmentVariable("ANLABEL_A5_KIT_DIR");
        try
        {
            var result = new MultiFormatReader().decode(
                new BinaryBitmap(new HybridBinarizer(new RGBLuminanceSource(crop, cropWidth, cropHeight, RGBLuminanceSource.BitmapFormat.Gray8))),
                hints);
            return result?.Text ?? string.Empty;
        }
        catch (ReaderException)
        {
            if (!string.IsNullOrWhiteSpace(dump))
            {
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(BitmapSource.Create(cropWidth, cropHeight, dpi, dpi, PixelFormats.Gray8, null, crop, cropWidth)));
                using var stream = File.Create(Path.Combine(dump, $"fail-{slot.Column}-{dpi}-{Guid.NewGuid():N}.png"));
                encoder.Save(stream);
            }

            return string.Empty;
        }
    }

    /// <summary>
    /// Plessey read like a scanner: a line through the bars, each bar/space pair
    /// is a bit (bar wider than space = 1), start 1101, digits least significant
    /// bit first, then the CRC-8 x⁸+x⁷+x⁶+x⁵+x³+1, then the wider termination bar.
    /// </summary>
    private static string DecodePlessey(Page page, A5BarcodeSheet.Slot slot, int dpi)
    {
        int Px(double mm) => (int)Math.Round(mm / 25.4 * dpi);
        foreach (var fraction in new[] { 0.3, 0.45, 0.2 })
        {
            var y = Px(slot.YMm + slot.HeightMm * fraction);
            var (leftMm, rightMm) = slot.AreaXMm;
            var runs = new List<(bool Black, int Length)>();
            for (var x = Math.Max(0, Px(leftMm)); x < Math.Min(page.Width, Px(rightMm)); x++)
            {
                var black = page.Gray[y * page.Width + x] < 128;
                if (runs.Count > 0 && runs[^1].Black == black)
                {
                    runs[^1] = (black, runs[^1].Length + 1);
                }
                else
                {
                    runs.Add((black, 1));
                }
            }

            var first = runs.FindIndex(run => run.Black);
            if (first < 0)
            {
                continue;
            }

            var cells = new List<(int Bar, int Space)>();
            for (var index = first; index + 1 < runs.Count; index += 2)
            {
                cells.Add((runs[index].Length, runs[index + 1].Length));
            }

            if (cells.Count < 13)
            {
                continue;
            }

            var cellWidth = cells.Take(4).Average(cell => cell.Bar + cell.Space);
            var bits = new List<int>();
            foreach (var (bar, space) in cells)
            {
                if (bar + space > cellWidth * 1.5)
                {
                    break;
                }

                bits.Add(bar > space ? 1 : 0);
            }

            if (bits.Count < 16 || (bits.Count - 12) % 4 != 0 || bits[0] != 1 || bits[1] != 1 || bits[2] != 0 || bits[3] != 1)
            {
                continue;
            }

            var data = bits.Skip(4).Take(bits.Count - 12).ToArray();
            var remainder = data.Concat(new int[8]).ToArray();
            int[] grid = [1, 1, 1, 1, 0, 1, 0, 0, 1];
            for (var index = 0; index < data.Length; index++)
            {
                if (remainder[index] != 0)
                {
                    for (var tap = 0; tap < grid.Length; tap++)
                    {
                        remainder[index + tap] ^= grid[tap];
                    }
                }
            }

            if (!remainder.Skip(data.Length).SequenceEqual(bits.Skip(bits.Count - 8)))
            {
                continue;
            }

            var text = new System.Text.StringBuilder();
            for (var index = 0; index < data.Length; index += 4)
            {
                text.Append("0123456789ABCDEF"[data[index] | data[index + 1] << 1 | data[index + 2] << 2 | data[index + 3] << 3]);
            }

            return text.ToString();
        }

        return string.Empty;
    }

    private static string KitDirectory()
    {
        var configured = Environment.GetEnvironmentVariable("ANLABEL_A5_KIT_DIR");
        var directory = string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(Path.GetTempPath(), $"anl-a5-kit-{Guid.NewGuid():N}")
            : configured;
        Directory.CreateDirectory(directory);
        return directory;
    }

    private static T RunOnSta<T>(Func<T> work)
    {
        T result = default!;
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                result = work();
            }
            catch (Exception ex)
            {
                failure = ex;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (failure is not null)
        {
            throw failure;
        }

        return result;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}

/// <summary>The A5 sheet: workbook, label and the decode each symbology must give.</summary>
internal static class A5BarcodeSheet
{
    public const string WorkbookName = "A5-du-lieu-ma-vach.xlsx";
    public const string TemplateName = "A5-du-moi-loai-ma.anlabel";
    public const string SheetName = "DuLieu";
    public const string InvalidSheetName = "DuLieuLoi";
    public const int RowCount = 30;
    public const double WidthMm = 148;
    public const double HeightMm = 210;

    /// <param name="AreaXMm">The strip of the sheet a scanner sees for this code (its column).</param>
    public sealed record Slot(
        string Column,
        string Caption,
        BarcodeSymbology Symbology,
        ObjectType Type,
        BarcodeFormat Format,
        double XMm,
        double YMm,
        double WidthMm,
        double HeightMm,
        (double Left, double Right) AreaXMm,
        double ModuleMm = 0.254)
    {
        public string ObjectName => "Mã " + Caption;
    }

    private const double Left = 7;
    private const double Right = 77;
    private static readonly (double, double) Full = (0, WidthMm);
    private static readonly (double, double) LeftColumn = (0, 74);
    private static readonly (double, double) RightColumn = (74, WidthMm);
    private const double LinearHeight = 11;

    public static readonly IReadOnlyList<Slot> Slots =
    [
        new("Code128", "Code 128", BarcodeSymbology.Code128, ObjectType.BarcodeCode128, BarcodeFormat.CODE_128, Left, 26, 134, LinearHeight, Full),
        new("Code39", "Code 39", BarcodeSymbology.Code39, ObjectType.BarcodeCode128, BarcodeFormat.CODE_39, Left, 42, 134, LinearHeight, Full),
        new("Code93", "Code 93", BarcodeSymbology.Code93, ObjectType.BarcodeCode128, BarcodeFormat.CODE_93, Left, 58, 64, LinearHeight, LeftColumn),
        new("ITF14", "ITF-14", BarcodeSymbology.ITF, ObjectType.BarcodeCode128, BarcodeFormat.ITF, Right, 58, 64, LinearHeight, RightColumn),
        new("EAN13", "EAN-13", BarcodeSymbology.Ean13, ObjectType.BarcodeCode128, BarcodeFormat.EAN_13, Left, 74, 64, LinearHeight, LeftColumn, 0.33),
        new("UPCA", "UPC-A", BarcodeSymbology.UpcA, ObjectType.BarcodeCode128, BarcodeFormat.UPC_A, Right, 74, 64, LinearHeight, RightColumn, 0.33),
        new("EAN8", "EAN-8", BarcodeSymbology.Ean8, ObjectType.BarcodeCode128, BarcodeFormat.EAN_8, Left, 90, 64, LinearHeight, LeftColumn, 0.33),
        new("UPCE", "UPC-E", BarcodeSymbology.UpcE, ObjectType.BarcodeCode128, BarcodeFormat.UPC_E, Right, 90, 64, LinearHeight, RightColumn, 0.33),
        new("Codabar", "Codabar", BarcodeSymbology.Codabar, ObjectType.BarcodeCode128, BarcodeFormat.CODABAR, Left, 106, 64, LinearHeight, LeftColumn),
        new("MSI", "MSI", BarcodeSymbology.MSI, ObjectType.BarcodeCode128, BarcodeFormat.MSI, Right, 106, 64, LinearHeight, RightColumn),
        new("Plessey", "Plessey", BarcodeSymbology.Plessey, ObjectType.BarcodeCode128, BarcodeFormat.PLESSEY, Left, 122, 64, LinearHeight, LeftColumn),
        new("QR", "QR Code", BarcodeSymbology.QRCode, ObjectType.QRCode, BarcodeFormat.QR_CODE, Left, 142, 30, 30, (0, 41)),
        new("DataMatrix", "Data Matrix", BarcodeSymbology.DataMatrix, ObjectType.DataMatrix, BarcodeFormat.DATA_MATRIX, 50, 142, 26, 26, (43, 82)),
        new("Aztec", "Aztec", BarcodeSymbology.Aztec, ObjectType.BarcodeCode128, BarcodeFormat.AZTEC, 92, 142, 30, 30, (84, 130)),
        new("PDF417", "PDF417", BarcodeSymbology.Pdf417, ObjectType.BarcodeCode128, BarcodeFormat.PDF_417, Left, 180, 134, 22, Full)
    ];

    private static readonly string[] Products =
    [
        "Sữa tươi tiệt trùng 1L", "Bánh quy bơ hộp 400g", "Nước mắm cá cơm 500ml", "Cà phê rang xay Đắk Lắk", "Gạo ST25 túi 5kg",
        "Dầu ăn hướng dương 2L", "Trà xanh Thái Nguyên", "Mì gói tôm chua cay", "Bột giặt hương hoa 3kg", "Kẹo dừa Bến Tre"
    ];

    /// <summary>One wrong value per row; every other column stays valid.</summary>
    public static readonly IReadOnlyList<(string Column, string Value)> InvalidCases =
    [
        ("EAN13", "89345600000AB"),
        ("EAN8", "12345"),
        ("UPCA", "1234567890123456"),
        ("UPCE", "9123456"),
        ("ITF14", "1234567"),
        ("Codabar", "12#34"),
        ("Code39", "MÃ HÀNG"),
        ("MSI", "12A4"),
        ("Plessey", "XYZ"),
        ("Code128", "Lô hàng ữ")
    ];

    public static string WithGs1Check(string digits)
    {
        var sum = 0;
        for (var index = 0; index < digits.Length; index++)
        {
            var digit = digits[index] - '0';
            sum += (digits.Length - index) % 2 == 1 ? digit * 3 : digit;
        }

        return digits + ((10 - sum % 10) % 10).ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>UPC-E: number system, six digits and the check digit of its UPC-A expansion.</summary>
    public static string UpcEWithCheck(string seven)
    {
        var ns = seven[0];
        var d = seven[1..];
        var upcA = d[5] switch
        {
            '0' or '1' or '2' => $"{ns}{d[0]}{d[1]}{d[5]}0000{d[2]}{d[3]}{d[4]}",
            '3' => $"{ns}{d[0]}{d[1]}{d[2]}00000{d[3]}{d[4]}",
            '4' => $"{ns}{d[0]}{d[1]}{d[2]}{d[3]}00000{d[4]}",
            _ => $"{ns}{d[0]}{d[1]}{d[2]}{d[3]}{d[4]}0000{d[5]}"
        };
        return seven + WithGs1Check(upcA)[^1];
    }

    /// <summary>What a scanner must read for the Excel value: EAN/UPC gain their check digit.</summary>
    public static string Expected(Slot slot, string value) => slot.Symbology switch
    {
        BarcodeSymbology.Ean13 when value.Length == 12 => WithGs1Check(value),
        BarcodeSymbology.Ean8 when value.Length == 7 => WithGs1Check(value),
        BarcodeSymbology.UpcA when value.Length == 11 => WithGs1Check(value),
        BarcodeSymbology.UpcE when value.Length == 7 => UpcEWithCheck(value),
        _ => value
    };

    private static string[] Header =>
    [
        "STT", "MaHang", "TenHang", "SoLo", "HSD", "SoLuong",
        "Code128", "Code39", "Code93", "ITF14", "EAN13", "UPCA", "EAN8", "UPCE", "Codabar", "MSI", "Plessey", "QR", "DataMatrix", "Aztec", "PDF417"
    ];

    /// <summary>Row <paramref name="i"/> (1-based) as Excel cell values: text, numbers and a date.</summary>
    private static object[] Row(int i)
    {
        var code = $"SP{i:0000}";
        var name = Products[(i - 1) % Products.Length];
        var lot = $"L2610-{i:000}";
        var expiry = new DateTime(2027, (i - 1) % 12 + 1, i * 3 % 28 + 1);
        var quantity = i * 12;
        var code128 = (i % 4) switch
        {
            0 => $"{code}-{lot}",
            1 => $"{i:0000000000}",
            2 => $"Ab-{i:00} x/7+%",
            _ => $"lot {i} #{i * 7}"
        };
        var code39 = (i % 3) switch
        {
            0 => $"SP-{i:0000}",
            1 => $"LOT {i:000}/A",
            _ => $"$+% .{i:00}"
        };
        var code93 = i % 2 == 0 ? $"C93-{code}" : $"MIX.{i:000}/$+%";
        var itf = long.Parse(WithGs1Check($"1893456{i:000000}"), CultureInfo.InvariantCulture);
        object ean13 = i % 2 == 1
            ? 893456000000L + i * 7
            : long.Parse(WithGs1Check($"8934560{i:00000}"), CultureInfo.InvariantCulture);
        var upcA = 3600029000L + i;
        var ean8 = $"{9638500 + i}";
        var upcE = $"{(i % 5 == 0 ? 1 : 0)}{123450 + i % 10}";
        var codabar = (i % 3) switch
        {
            0 => $"A{i:00000}B",
            1 => $"C{i:000}-{i * 3:00}D",
            _ => $"B{i:000}$:/.+{i % 10}A"
        };
        var msi = $"{(long)i * 1234567 % 100000000:00000000}";
        var plessey = $"{(long)i * 48271 % 1000000:000000}";
        var qr = $"https://anlabel.vn/sp/{code}?lo={lot}\n{name}\nHSD {expiry:dd/MM/yyyy} · SL {quantity}";
        var dataMatrix = $"(01){itf}(17){expiry:yyMMdd}(10){lot}";
        var aztec = $"{code}|{lot}|{name}";
        var pdf417 = $"{code};{name};Lô {lot};HSD {expiry:dd/MM/yyyy};SL {quantity};Kiểm thử PDF417 dòng {i:00}";
        return [i, code, name, lot, expiry, quantity, code128, code39, code93, itf, ean13, upcA, ean8, upcE, codabar, msi, plessey, qr, dataMatrix, aztec, pdf417];
    }

    public static void WriteWorkbook(string path, bool withInvalidSheet = false)
    {
        using var book = new XLWorkbook();
        Fill(book.AddWorksheet(SheetName), Enumerable.Range(1, RowCount).Select(Row));
        if (withInvalidSheet)
        {
            var header = Header;
            Fill(book.AddWorksheet(InvalidSheetName), InvalidCases.Select(item =>
            {
                var values = Row(1);
                values[Array.IndexOf(header, item.Column)] = item.Value;
                return values;
            }));
        }

        book.SaveAs(path);
    }

    private static void Fill(IXLWorksheet sheet, IEnumerable<object[]> rows)
    {
        var header = Header;
        for (var column = 0; column < header.Length; column++)
        {
            sheet.Cell(1, column + 1).Value = header[column];
        }

        var excelRow = 2;
        foreach (var values in rows)
        {
            for (var column = 0; column < values.Length; column++)
            {
                var cell = sheet.Cell(excelRow, column + 1);
                switch (values[column])
                {
                    case int number:
                        cell.Value = number;
                        break;
                    case long number:
                        cell.Value = number;
                        break;
                    case DateTime date:
                        cell.Value = date;
                        cell.Style.DateFormat.Format = "dd/MM/yyyy";
                        break;
                    default:
                        cell.Value = (string)values[column];
                        break;
                }
            }

            excelRow++;
        }

        // An operator keeps UPC-A's leading zero with a number format, as Excel users do.
        sheet.Column(Array.IndexOf(header, "UPCA") + 1).Style.NumberFormat.Format = "00000000000";
        sheet.Columns().AdjustToContents();
    }

    public static IReadOnlyList<IReadOnlyDictionary<string, string>> ReadRows(string workbook, string sheet)
    {
        var table = new ExcelDataService().LoadSheetAsync(workbook, sheet).GetAwaiter().GetResult();
        return table.Rows.Cast<DataRow>()
            .Select(row => (IReadOnlyDictionary<string, string>)table.Columns.Cast<DataColumn>()
                .ToDictionary(column => column.ColumnName, column => row[column]?.ToString() ?? string.Empty, StringComparer.OrdinalIgnoreCase))
            .ToArray();
    }

    /// <summary>The A5 label bound to the workbook; office printer DPI 600 (DocuCentre class).</summary>
    public static LabelTemplate CreateTemplate(string workbook)
    {
        var template = new LabelTemplate { Name = "A5 đủ mọi loại mã", WidthMm = WidthMm, HeightMm = HeightMm, Dpi = 600 };
        template.PrinterProfile.Dpi = 600;
        template.PrinterProfile.LabelWidthMm = WidthMm;
        template.PrinterProfile.LabelHeightMm = HeightMm;
        template.DatabaseConfig.FilePath = workbook;
        template.DatabaseConfig.SheetName = SheetName;
        template.DatabaseConfig.HeaderRowIndex = 1;
        template.DatabaseConfig.KeyField = "STT";
        // As Nhập Excel leaves a linked label: every column available and offered to the objects.
        var header = Header;
        for (var column = 0; column < header.Length; column++)
        {
            template.DatabaseConfig.AvailableFields.Add(new DatabaseField { Name = header[column], DisplayName = header[column], SourceColumn = column + 1 });
            template.DatabaseConfig.LabelFields.Add(new DatabaseField { Name = header[column], DisplayName = header[column], SourceColumn = column + 1 });
        }

        var z = 0;
        LabelObject Text(string name, string binding, double x, double y, double size, bool bold = false, double width = 30) => new()
        {
            Type = ObjectType.Text,
            Name = name,
            Text = binding,
            BindingExpression = binding.Contains('{') ? binding : string.Empty,
            XMm = x,
            YMm = y,
            WidthMm = width,
            HeightMm = size * 0.45,
            ZIndex = z++,
            Style = { FontFamily = "Arial", FontSizePt = size, Bold = bold, BorderThicknessMm = 0 }
        };

        template.Objects.Add(Text("Tiêu đề", "ANLAbel · Tem kiểm thử mã vạch A5", 7, 6.5, 12, bold: true, width: 120));
        template.Objects.Add(Text("Dòng dữ liệu", "Dòng {STT} · {MaHang} · Lô {SoLo} · HSD {HSD}", 7, 12.5, 8, width: 120));
        template.Objects.Add(Text("Tên hàng", "{TenHang} · SL {SoLuong}", 7, 16.5, 8, width: 120));
        foreach (var slot in Slots)
        {
            template.Objects.Add(Text("Nhãn " + slot.Caption, slot.Caption, slot.XMm, slot.YMm - 4, 7, bold: true));
            var linear = slot.Type == ObjectType.BarcodeCode128 && slot.Symbology is not (BarcodeSymbology.Aztec or BarcodeSymbology.Pdf417);
            template.Objects.Add(new LabelObject
            {
                Type = slot.Type,
                Name = slot.ObjectName,
                BarcodeSymbology = slot.Symbology,
                BindingExpression = "{" + slot.Column + "}",
                XMm = slot.XMm,
                YMm = slot.YMm,
                WidthMm = slot.WidthMm,
                HeightMm = slot.HeightMm,
                ZIndex = z++,
                BarcodeWidthMode = linear ? BarcodeWidthMode.SizedFromX : BarcodeWidthMode.FrameOwned,
                BarcodeModuleWidthMm = linear ? slot.ModuleMm : 0,
                QrQuietZoneModules = linear ? 10 : 2,
                ShowBarcodeText = linear,
                BarcodeTextFontSizePt = 7,
                Style = { BorderThicknessMm = 0 }
            });
        }

        return template;
    }
}
