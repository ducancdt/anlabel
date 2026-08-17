using ANLAbel.Barcode.Options;
using ANLAbel.Core.Barcode;
using ZXing;
using ZXing.Common;
using ZXing.Datamatrix.Encoder;
using ZXing.QrCode.Internal;

namespace ANLAbel.Barcode.Renderers;

public sealed class ZxingBarcodeRenderer : IBarcodeRenderer, INonSquareBarcodeRenderer
{
    private const int MaxPixelCount = 25_000_000;

    public BarcodePixelImage RenderBarcode(string data, BarcodeType type, double widthMm, double heightMm, int dpi, BarcodeRenderOptions? options = null)
    {
        if (!ValidateData(data, type))
        {
            throw new ArgumentException("Barcode data is empty or invalid.", nameof(data));
        }

        options ??= new BarcodeRenderOptions();
        data = NormalizeData(data, type, options);
        var widthPixels = Math.Max(8, (int)Math.Round(widthMm / 25.4 * dpi, MidpointRounding.AwayFromZero));
        var heightPixels = Math.Max(8, (int)Math.Round(heightMm / 25.4 * dpi, MidpointRounding.AwayFromZero));
        ValidatePixelSize(widthPixels, heightPixels);
        return RenderEncoded(data, type, widthPixels, heightPixels, dpi, dpi, options);
    }

    public BarcodePixelImage RenderBarcode(
        string data,
        BarcodeType type,
        double widthMm,
        double heightMm,
        int dpiX,
        int dpiY,
        BarcodeRenderOptions? options = null)
    {
        if (dpiX <= 0 || dpiY <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(dpiX), "Barcode DPI values must be positive.");
        }

        if (dpiX == dpiY)
        {
            return RenderBarcode(data, type, widthMm, heightMm, dpiX, options);
        }

        if (!ValidateData(data, type))
        {
            throw new ArgumentException("Barcode data is empty or invalid.", nameof(data));
        }

        options ??= new BarcodeRenderOptions();
        data = NormalizeData(data, type, options);
        var widthPixels = Math.Max(8, (int)Math.Round(widthMm / 25.4 * dpiX, MidpointRounding.AwayFromZero));
        var heightPixels = Math.Max(8, (int)Math.Round(heightMm / 25.4 * dpiY, MidpointRounding.AwayFromZero));
        ValidatePixelSize(widthPixels, heightPixels);
        return RenderEncoded(data, type, widthPixels, heightPixels, dpiX, dpiY, options);
    }

    public bool ValidateData(string data, BarcodeType type)
    {
        if (string.IsNullOrWhiteSpace(data))
        {
            return false;
        }

        return type switch
        {
            BarcodeType.Code128 => data.All(ch => ch >= 32 && ch <= 126),
            BarcodeType.Code39 => data.All(ch => "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ-. $/+%".Contains(char.ToUpperInvariant(ch))),
            BarcodeType.Code93 => data.All(ch => ch >= 32 && ch <= 126),
            BarcodeType.Ean13 => data.All(char.IsDigit) && data.Length is 12 or 13,
            BarcodeType.Ean8 => data.All(char.IsDigit) && data.Length is 7 or 8,
            BarcodeType.UpcA => data.All(char.IsDigit) && data.Length is 11 or 12,
            BarcodeType.UpcE => data.All(char.IsDigit) && data.Length is 6 or 7 or 8,
            BarcodeType.ITF => data.All(char.IsDigit) && data.Length % 2 == 0,
            BarcodeType.Codabar => data.All(ch => "0123456789-$:/.+ABCDTN*E".Contains(char.ToUpperInvariant(ch))),
            BarcodeType.MSI => data.All(char.IsDigit),
            BarcodeType.Plessey => data.All(char.IsDigit),
            BarcodeType.QRCode => true,
            BarcodeType.DataMatrix => true,
            BarcodeType.Pdf417 => true,
            BarcodeType.Aztec => true,
            _ => false
        };
    }

    /// <summary>
    /// Code 39 and Codabar are validated case-insensitively (letters may be typed in
    /// any case), but ZXing's writers for these formats do a case-sensitive lookup
    /// against an uppercase-only alphabet. Anything not found — including lowercase
    /// letters — silently triggers Code 39's Full ASCII/extended shift-code mode
    /// instead of throwing, producing a barcode that scanners in standard mode read
    /// back garbled. Uppercase first so the data always maps onto the plain alphabet.
    /// </summary>
    private static string NormalizeData(string data, BarcodeType type, BarcodeRenderOptions options)
    {
        var normalized = type is BarcodeType.Code39 or BarcodeType.Codabar
            ? data.ToUpperInvariant()
            : data;

        if (!options.IsGs1)
        {
            return normalized;
        }

        if (type is not (BarcodeType.Code128 or BarcodeType.QRCode or BarcodeType.DataMatrix))
        {
            throw new ArgumentException("GS1 encoding is supported only for Code 128, QR Code, and Data Matrix.", nameof(type));
        }

        if (!BarcodeApplicationContract.TryNormalizeGs1Data(normalized, out var gs1Data, out var errors))
        {
            throw new ArgumentException(string.Join(" ", errors));
        }

        return gs1Data;
    }

    public string GetBarcodeInfo(string data, BarcodeType type)
    {
        return ValidateData(data, type)
            ? $"{type}: {data.Length} chars"
            : $"{type}: invalid data";
    }

    private static BarcodePixelImage RenderMatrix(BitMatrix matrix)
    {
        ValidatePixelSize(matrix.Width, matrix.Height);
        var pixels = new byte[matrix.Width * matrix.Height * 4];
        var offset = 0;

        for (var y = 0; y < matrix.Height; y++)
        {
            for (var x = 0; x < matrix.Width; x++)
            {
                var color = matrix[x, y] ? (byte)0 : (byte)255;
                pixels[offset++] = color;
                pixels[offset++] = color;
                pixels[offset++] = color;
                pixels[offset++] = 255;
            }
        }

        return new BarcodePixelImage(matrix.Width, matrix.Height, pixels);
    }

    public BarcodeVectorData? RenderBarcodeVector(string data, BarcodeType type, double widthMm, double heightMm, int dpi, BarcodeRenderOptions? options = null)
    {
        // Only provide vector data for 1D barcode types
        if (!IsLinearBarcode(type))
        {
            return null;
        }

        if (!ValidateData(data, type))
        {
            return null;
        }

        options ??= new BarcodeRenderOptions();
        data = NormalizeData(data, type, options);
        var widthPixels = Math.Max(8, (int)Math.Round(widthMm / 25.4 * dpi, MidpointRounding.AwayFromZero));
        var heightPixels = Math.Max(8, (int)Math.Round(heightMm / 25.4 * dpi, MidpointRounding.AwayFromZero));
        ValidatePixelSize(widthPixels, heightPixels);

        var matrix = EncodeMatrix(data, type, widthPixels, heightPixels, options);

        // Extract the first row of the BitMatrix as the barcode pattern
        var rowBits = new bool[matrix.Width];
        for (var x = 0; x < matrix.Width; x++)
        {
            rowBits[x] = matrix[x, 0];
        }

        // WidthModules here is the scaled pixel column count for drawing — not
        // the logical module count. Use CountLinearModules for industrial math.
        return new BarcodeVectorData(matrix.Width, matrix.Height, rowBits);
    }

    /// <inheritdoc />
    public int? CountLinearModules(string data, BarcodeType type, BarcodeRenderOptions? options = null)
    {
        if (!IsLinearBarcode(type) || !ValidateData(data, type))
        {
            return null;
        }

        options ??= new BarcodeRenderOptions();
        try
        {
            data = NormalizeData(data, type, options);
            // Request a 1×1 target so ZXing returns the unscaled pure module
            // matrix (native column count including quiet-zone margin modules).
            var matrix = EncodeMatrix(
                data,
                type,
                1,
                1,
                options);
            return matrix.Width > 0 ? matrix.Width : null;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            return null;
        }
    }

    private static bool IsLinearBarcode(BarcodeType type)
    {
        return type is BarcodeType.Code128
            or BarcodeType.Code39
            or BarcodeType.Code93
            or BarcodeType.Ean13
            or BarcodeType.Ean8
            or BarcodeType.UpcA
            or BarcodeType.UpcE
            or BarcodeType.ITF
            or BarcodeType.Codabar
            or BarcodeType.MSI
            or BarcodeType.Plessey;
    }

    private static void ValidatePixelSize(int widthPixels, int heightPixels)
    {
        var pixelCount = (long)widthPixels * heightPixels;
        if (pixelCount > MaxPixelCount)
        {
            throw new ArgumentException($"Barcode image is too large ({widthPixels}x{heightPixels}px).");
        }

        if (pixelCount * 4 > int.MaxValue)
        {
            throw new ArgumentException($"Barcode image buffer is too large ({widthPixels}x{heightPixels}px).");
        }
    }

    private static BarcodeFormat ToZxingFormat(BarcodeType type)
    {
        return type switch
        {
            BarcodeType.Code128 => BarcodeFormat.CODE_128,
            BarcodeType.QRCode => BarcodeFormat.QR_CODE,
            BarcodeType.DataMatrix => BarcodeFormat.DATA_MATRIX,
            BarcodeType.Code39 => BarcodeFormat.CODE_39,
            BarcodeType.Code93 => BarcodeFormat.CODE_93,
            BarcodeType.Ean13 => BarcodeFormat.EAN_13,
            BarcodeType.Ean8 => BarcodeFormat.EAN_8,
            BarcodeType.UpcA => BarcodeFormat.UPC_A,
            BarcodeType.UpcE => BarcodeFormat.UPC_E,
            BarcodeType.ITF => BarcodeFormat.ITF,
            BarcodeType.Codabar => BarcodeFormat.CODABAR,
            BarcodeType.Pdf417 => BarcodeFormat.PDF_417,
            BarcodeType.Aztec => BarcodeFormat.AZTEC,
            BarcodeType.MSI => BarcodeFormat.MSI,
            BarcodeType.Plessey => BarcodeFormat.PLESSEY,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }

    private static Dictionary<EncodeHintType, object> CreateHints(BarcodeType type, BarcodeRenderOptions options)
    {
        var hints = new Dictionary<EncodeHintType, object>
        {
            [EncodeHintType.MARGIN] = Math.Max(0, options.QuietZoneModules),
            [EncodeHintType.CHARACTER_SET] = "UTF-8"
        };

        if (type == BarcodeType.QRCode)
        {
            hints[EncodeHintType.ERROR_CORRECTION] = ParseQrErrorCorrection(options.ErrorCorrection);
        }
        else if (type == BarcodeType.DataMatrix)
        {
            hints[EncodeHintType.DATA_MATRIX_SHAPE] = SymbolShapeHint.FORCE_NONE;
        }

        if (options.IsGs1)
        {
            hints[EncodeHintType.GS1_FORMAT] = true;
        }

        return hints;
    }

    private static BarcodePixelImage RenderEncoded(
        string data,
        BarcodeType type,
        int widthPixels,
        int heightPixels,
        int dpiX,
        int dpiY,
        BarcodeRenderOptions options)
    {
        // Square 2D (and PDF417 native-aspect): encode the module matrix once,
        // then integer-scale so each module stays uniform. Independent
        // ResizeNearest(frameW, frameH) would squash modules.
        if (IsMatrixBarcode(type))
        {
            var native = EncodeMatrix(data, type, 1, 1, options);
            var nativeImage = RenderMatrix(native);
            var layout = MatrixSquareModuleFit.Fit(
                nativeImage.WidthPixels,
                nativeImage.HeightPixels,
                widthPixels,
                heightPixels,
                dpiX,
                dpiY);
            return nativeImage.ScaleIntegerModules(layout.ModuleDotsX, layout.ModuleDotsY);
        }

        return RenderMatrix(EncodeMatrix(data, type, widthPixels, heightPixels, options));
    }

    private static bool IsMatrixBarcode(BarcodeType type)
    {
        return type is BarcodeType.QRCode
            or BarcodeType.DataMatrix
            or BarcodeType.Pdf417
            or BarcodeType.Aztec;
    }

    private static BitMatrix EncodeMatrix(string data, BarcodeType type, int widthPixels, int heightPixels, BarcodeRenderOptions options)
    {
        if (type != BarcodeType.Code39 || options.Code39WideNarrowRatio == Core.Enums.Code39WideNarrowRatio.LegacyEngineDefault)
        {
            return new MultiFormatWriter().encode(data, ToZxingFormat(type), widthPixels, heightPixels, CreateHints(type, options));
        }

        var ratio = Core.Barcode.Code39RatioContract.ToValue(options.Code39WideNarrowRatio);
        if (ratio is null)
        {
            throw new ArgumentException("Unsupported Code 39 wide:narrow ratio.");
        }

        // Classify wide/narrow elements from ZXing's native 1/2-unit matrix,
        // never from a frame-scaled matrix where both runs may be many pixels.
        var native = new MultiFormatWriter().encode(data, ToZxingFormat(type), 1, 1, CreateHints(type, options));
        return RescaleCode39Runs(native, Math.Max(0, options.QuietZoneModules), ratio.Value, widthPixels, heightPixels);
    }

    private static BitMatrix RescaleCode39Runs(BitMatrix source, int quietZoneModules, double ratio, int targetWidth, int targetHeight)
    {
        var runs = new List<(bool Black, int Length)>();
        var current = source[0, 0];
        var length = 0;
        for (var x = 0; x < source.Width; x++)
        {
            var bit = source[x, 0];
            if (bit == current) { length++; continue; }
            runs.Add((current, length));
            current = bit;
            length = 1;
        }
        runs.Add((current, length));

        // ZXing Code 39 uses a 1/2 narrow/wide pattern. Preserve the two outer
        // margins and reweight interior two-unit runs to the approved ratio.
        var weights = runs.Select((run, index) =>
            index == 0 || index == runs.Count - 1
                ? Math.Max(0, quietZoneModules)
                : run.Length >= 2 ? ratio : 1d).ToArray();
        var total = weights.Sum();
        var target = new BitMatrix(targetWidth, targetHeight);
        var cursor = 0;
        for (var index = 0; index < runs.Count; index++)
        {
            var next = index == runs.Count - 1
                ? targetWidth
                : (int)Math.Round((weights.Take(index + 1).Sum() / total) * targetWidth, MidpointRounding.AwayFromZero);
            if (runs[index].Black && next > cursor)
            {
                target.setRegion(cursor, 0, next - cursor, targetHeight);
            }
            cursor = next;
        }
        return target;
    }

    private static ErrorCorrectionLevel ParseQrErrorCorrection(string value)
    {
        return value?.ToUpperInvariant() switch
        {
            "L" => ErrorCorrectionLevel.L,
            "Q" => ErrorCorrectionLevel.Q,
            "H" => ErrorCorrectionLevel.H,
            _ => ErrorCorrectionLevel.M
        };
    }
}
