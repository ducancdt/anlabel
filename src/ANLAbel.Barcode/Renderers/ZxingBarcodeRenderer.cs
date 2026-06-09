using ANLAbel.Barcode.Options;
using ZXing;
using ZXing.Common;
using ZXing.Datamatrix.Encoder;
using ZXing.QrCode.Internal;

namespace ANLAbel.Barcode.Renderers;

public sealed class ZxingBarcodeRenderer : IBarcodeRenderer
{
    private const int MaxPixelCount = 25_000_000;

    public BarcodePixelImage RenderBarcode(string data, BarcodeType type, double widthMm, double heightMm, int dpi, BarcodeRenderOptions? options = null)
    {
        if (!ValidateData(data, type))
        {
            throw new ArgumentException("Barcode data is empty or invalid.", nameof(data));
        }

        options ??= new BarcodeRenderOptions();
        var widthPixels = Math.Max(8, (int)Math.Round(widthMm / 25.4 * dpi, MidpointRounding.AwayFromZero));
        var heightPixels = Math.Max(8, (int)Math.Round(heightMm / 25.4 * dpi, MidpointRounding.AwayFromZero));
        ValidatePixelSize(widthPixels, heightPixels);
        var matrix = new MultiFormatWriter().encode(data, ToZxingFormat(type), widthPixels, heightPixels, CreateHints(type, options));
        return RenderMatrix(matrix);
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
        var widthPixels = Math.Max(8, (int)Math.Round(widthMm / 25.4 * dpi, MidpointRounding.AwayFromZero));
        var heightPixels = Math.Max(8, (int)Math.Round(heightMm / 25.4 * dpi, MidpointRounding.AwayFromZero));
        ValidatePixelSize(widthPixels, heightPixels);

        var matrix = new MultiFormatWriter().encode(data, ToZxingFormat(type), widthPixels, heightPixels, CreateHints(type, options));

        // Extract the first row of the BitMatrix as the barcode pattern
        var rowBits = new bool[matrix.Width];
        for (var x = 0; x < matrix.Width; x++)
        {
            rowBits[x] = matrix[x, 0];
        }

        return new BarcodeVectorData(matrix.Width, matrix.Height, rowBits);
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

        return hints;
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
