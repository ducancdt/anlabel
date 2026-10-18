using ANLAbel.Barcode.Options;
using ANLAbel.Core.Barcode;
using ANLAbel.Core.Enums;
using ZXing;
using ZXing.Common;
using ZXing.Datamatrix.Encoder;
using ZXing.QrCode.Internal;

namespace ANLAbel.Barcode.Renderers;

public sealed class ZxingBarcodeRenderer : IBarcodeRenderer, INonSquareBarcodeRenderer
{
    private const int MaxPixelCount = 25_000_000;

    /// <summary>
    /// ZXing.Net's Code128Writer FNC1 escape character (U+00F1). It is the only
    /// value the Code128 encoder converts into an FNC1 symbol character.
    /// </summary>
    private const char Gs1Code128Fnc1Escape = 'ñ';

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
            BarcodeType.Ean13 => AcceptsRetailPayload(BarcodeSymbology.Ean13, data),
            BarcodeType.Ean8 => AcceptsRetailPayload(BarcodeSymbology.Ean8, data),
            BarcodeType.UpcA => AcceptsRetailPayload(BarcodeSymbology.UpcA, data),
            BarcodeType.UpcE => AcceptsRetailPayload(BarcodeSymbology.UpcE, data),
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
    /// <summary>
    /// Retail symbologies share the application contract: a full payload with a
    /// wrong check digit is invalid, while the short form that omits the check
    /// digit stays valid so the encoder may append that one digit.
    /// </summary>
    private static bool AcceptsRetailPayload(BarcodeSymbology symbology, string data)
        => BarcodeApplicationContract.ValidateData(BarcodeApplicationProfile.General, symbology, data).Count == 0;

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

        // ZXing.Net's Code128Writer only converts the FNC1 escape char (U+00F1)
        // into an FNC1 symbol character; a literal GS (0x1D) is encoded as a
        // plain Code-Set data character, silently breaking GS1 conformance. The
        // leading FNC1 is still supplied automatically because we pass the
        // GS1_FORMAT hint. The QR and Data Matrix encoders, by contrast, treat a
        // literal 0x1D as the FNC1 separator, so only the Code128 path needs the
        // translation.
        if (type == BarcodeType.Code128)
        {
            gs1Data = gs1Data.Replace(BarcodeApplicationContract.GroupSeparator, Gs1Code128Fnc1Escape);
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
            if (TryGetCode39Geometry(type, options, out var ratio, out var gapModules))
            {
                // An explicit ratio makes wide elements ratio × X, not ZXing's 2 × X,
                // and a wider gap adds (gap - 1) × X between characters.
                var native = new MultiFormatWriter().encode(data, ToZxingFormat(type), 1, 1, CreateHints(type, options));
                var elements = ReadCode39Elements(native);
                var wide = elements.Count(element => element.Wide);
                return Core.Barcode.Code39RatioContract.LogicalModules(
                    elements.Count - wide,
                    wide,
                    options.QuietZoneModules,
                    ratio) + (CountGaps(elements) * (gapModules - 1));
            }

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

        if (type == BarcodeType.Code128 && options.Code128CodeSet is Core.Enums.Code128CodeSet.A or Core.Enums.Code128CodeSet.B or Core.Enums.Code128CodeSet.C)
        {
            hints[EncodeHintType.FORCE_CODE_SET] = options.Code128CodeSet.ToString();
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
        if (type == BarcodeType.Code128 && options.Code128CodeSet == Core.Enums.Code128CodeSet.Manual)
        {
            if (options.IsGs1)
            {
                throw new ArgumentException("Manual Code 128 code sets cannot be combined with GS1 encoding.");
            }

            return new Code128ManualWriter().encode(data, BarcodeFormat.CODE_128, widthPixels, heightPixels, CreateHints(type, options));
        }

        if (!TryGetCode39Geometry(type, options, out var ratio, out var gapModules))
        {
            return new MultiFormatWriter().encode(data, ToZxingFormat(type), widthPixels, heightPixels, CreateHints(type, options));
        }

        // Classify wide/narrow elements from ZXing's native 1/2-unit matrix,
        // never from a frame-scaled matrix where both runs may be many pixels.
        var native = new MultiFormatWriter().encode(data, ToZxingFormat(type), 1, 1, CreateHints(type, options));
        return RescaleCode39Runs(native, Math.Max(0, options.QuietZoneModules), ratio, gapModules, widthPixels, heightPixels);
    }

    /// <summary>
    /// Code 39 needs whole-dot rebuilding when the operator chose a ratio or a
    /// wider intercharacter gap; the legacy engine pattern is 2:1 with a 1X gap.
    /// </summary>
    private static bool TryGetCode39Geometry(BarcodeType type, BarcodeRenderOptions options, out double ratio, out int gapModules)
    {
        ratio = 2.0;
        gapModules = Math.Clamp(options.Code39IntercharacterGapModules, 1, 5);
        if (type != BarcodeType.Code39)
        {
            return false;
        }

        if (options.Code39WideNarrowRatio != Core.Enums.Code39WideNarrowRatio.LegacyEngineDefault)
        {
            ratio = Core.Barcode.Code39RatioContract.ToValue(options.Code39WideNarrowRatio)
                ?? throw new ArgumentException("Unsupported Code 39 wide:narrow ratio.");
            return true;
        }

        return gapModules > 1;
    }

    /// <summary>Each Code 39 character is 9 elements followed by a gap space before the next one.</summary>
    private static bool IsIntercharacterGap(int elementIndex) => elementIndex % 10 == 9;

    private static int CountGaps(IReadOnlyList<(bool Black, bool Wide)> elements)
        => Enumerable.Range(0, elements.Count).Count(IsIntercharacterGap);

    private static BitMatrix RescaleCode39Runs(BitMatrix source, int quietZoneModules, double ratio, int gapModules, int targetWidth, int targetHeight)
    {
        // Every narrow element gets the same whole number of dots and every
        // wide element the same legal multiple of it, so the printed ratio is
        // the one preflight checked. The largest narrow width that fits the
        // target is used; the remainder is centred like ZXing's integer scaling.
        var elements = ReadCode39Elements(source);
        var wideCount = elements.Count(element => element.Wide);
        var narrowCount = elements.Count - wideCount;
        var extraGapUnits = CountGaps(elements) * (gapModules - 1);
        var margin = Math.Max(0, quietZoneModules);
        int SymbolWidth(int narrow) =>
            ((2 * margin) + narrowCount + extraGapUnits) * narrow
            + (wideCount * Core.Barcode.Code39RatioContract.ResolveWideDots(narrow, ratio));

        var narrowDots = 1;
        while (SymbolWidth(narrowDots + 1) <= targetWidth)
        {
            narrowDots++;
        }

        var wideDots = Core.Barcode.Code39RatioContract.ResolveWideDots(narrowDots, ratio);
        var symbolWidth = SymbolWidth(narrowDots);
        var outputWidth = Math.Max(targetWidth, symbolWidth);
        var height = Math.Max(1, targetHeight);
        var target = new BitMatrix(outputWidth, height);
        var cursor = ((outputWidth - symbolWidth) / 2) + (margin * narrowDots);
        for (var index = 0; index < elements.Count; index++)
        {
            var element = elements[index];
            var length = element.Wide
                ? wideDots
                : IsIntercharacterGap(index) ? narrowDots * gapModules : narrowDots;
            if (element.Black)
            {
                target.setRegion(cursor, 0, length, height);
            }

            cursor += length;
        }

        return target;
    }

    /// <summary>
    /// Bars and spaces of ZXing's native Code 39 row without its outer margins.
    /// ZXing encodes narrow elements as one unit and wide elements as two.
    /// </summary>
    private static List<(bool Black, bool Wide)> ReadCode39Elements(BitMatrix source)
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

        var first = runs[0].Black ? 0 : 1;
        var last = runs[^1].Black ? runs.Count - 1 : runs.Count - 2;
        return runs
            .Skip(first)
            .Take(Math.Max(0, last - first + 1))
            .Select(run => (run.Black, run.Length >= 2))
            .ToList();
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
