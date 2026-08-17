using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ANLAbel.Core.Enums;
using ANLAbel.Core.Printing;

namespace ANLAbel.Printing.RenderPipeline;

/// <summary>
/// Single WPF image decode/transform seam shared by the designer canvas and
/// print presenter. Keeping the transform here prevents preview from showing a
/// colour/alpha bitmap while print silently applies a different thermal mode.
/// </summary>
public static class ImageRasterizer
{
    public const int MaxEncodedBytes = 64 * 1024 * 1024;
    public const long MaxDecodedPixels = 64_000_000;
    private const int Threshold = 128;
    private const int MaxCachedImages = 8;
    private const long MaxCachedBytes = 64L * 1024 * 1024;
    private static readonly object CacheGate = new();
    private static readonly Dictionary<string, (BitmapSource Source, long Bytes)> Cache = new(StringComparer.Ordinal);
    private static readonly LinkedList<string> CacheOrder = new();
    private static long CachedBytes;
    private static readonly int[,] Bayer4 =
    {
        { 0, 8, 2, 10 },
        { 12, 4, 14, 6 },
        { 3, 11, 1, 9 },
        { 15, 7, 13, 5 }
    };

    public static BitmapSource? Decode(string? base64, ImageRasterMode mode)
    {
        if (string.IsNullOrWhiteSpace(base64) || !ImageRasterContract.IsSupported(mode))
        {
            return null;
        }

        try
        {
            if (base64.Length > ((MaxEncodedBytes + 2) / 3) * 4)
            {
                return null;
            }

            var bytes = Convert.FromBase64String(base64);
            if (bytes.Length == 0 || bytes.Length > MaxEncodedBytes)
            {
                return null;
            }

            var payloadFingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(base64)));
            var cacheKey = $"{ImageRasterContract.AlgorithmRevision}|{mode}|{payloadFingerprint}";
            if (TryGetCached(cacheKey, out var cached))
            {
                return cached;
            }

            using var stream = new MemoryStream(bytes, writable: false);
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.StreamSource = stream;
            bitmap.EndInit();
            bitmap.Freeze();

            var pixels = (long)bitmap.PixelWidth * bitmap.PixelHeight;
            if (bitmap.PixelWidth <= 0 || bitmap.PixelHeight <= 0 || pixels > MaxDecodedPixels)
            {
                return null;
            }

            var result = mode == ImageRasterMode.DriverManaged
                ? bitmap
                : ApplyMonochrome(bitmap, mode);
            if (result is not null)
            {
                CacheResult(cacheKey, result);
            }

            return result;
        }
        catch (Exception ex) when (ex is FormatException
            or InvalidDataException
            or ArgumentException
            or NotSupportedException
            or InvalidOperationException
            or IOException)
        {
            return null;
        }
    }

    public static bool TryGetPixelDimensions(string? base64, out int pixelWidth, out int pixelHeight)
    {
        pixelWidth = 0;
        pixelHeight = 0;
        var bitmap = Decode(base64, ImageRasterMode.DriverManaged);
        if (bitmap is null)
        {
            return false;
        }

        pixelWidth = bitmap.PixelWidth;
        pixelHeight = bitmap.PixelHeight;
        return pixelWidth > 0 && pixelHeight > 0;
    }

    private static BitmapSource? ApplyMonochrome(BitmapSource source, ImageRasterMode mode)
    {
        var converted = new FormatConvertedBitmap(source, PixelFormats.Bgra32, null, 0);
        converted.Freeze();

        var width = converted.PixelWidth;
        var height = converted.PixelHeight;
        var stride = checked(width * 4);
        var bytes = new byte[checked(stride * height)];
        converted.CopyPixels(bytes, stride, 0);

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var offset = checked(y * stride + x * 4);
                var blue = bytes[offset];
                var green = bytes[offset + 1];
                var red = bytes[offset + 2];
                var alpha = bytes[offset + 3];
                // BT.709 integer luma, composited over the white label stock so
                // transparent pixels do not become unintended black blocks.
                var luma = (54 * red + 183 * green + 19 * blue + 128) >> 8;
                var composited = (luma * alpha + 255 * (255 - alpha) + 127) / 255;
                var black = mode == ImageRasterMode.MonochromeThreshold
                    ? composited < Threshold
                    : composited < ((Bayer4[y & 3, x & 3] * 2 + 1) * 255 / 32);
                var channel = black ? (byte)0 : (byte)255;
                bytes[offset] = channel;
                bytes[offset + 1] = channel;
                bytes[offset + 2] = channel;
                bytes[offset + 3] = 255;
            }
        }

        var dpiX = source.DpiX > 0 && double.IsFinite(source.DpiX) ? source.DpiX : 96;
        var dpiY = source.DpiY > 0 && double.IsFinite(source.DpiY) ? source.DpiY : 96;
        var result = BitmapSource.Create(width, height, dpiX, dpiY, PixelFormats.Bgra32, null, bytes, stride);
        result.Freeze();
        return result;
    }

    private static bool TryGetCached(string key, out BitmapSource? source)
    {
        lock (CacheGate)
        {
            if (!Cache.TryGetValue(key, out var entry))
            {
                source = null;
                return false;
            }

            CacheOrder.Remove(key);
            CacheOrder.AddFirst(key);
            source = entry.Source;
            return true;
        }
    }

    private static void CacheResult(string key, BitmapSource source)
    {
        var estimatedBytes = (long)source.PixelWidth * source.PixelHeight * 4;
        if (estimatedBytes <= 0 || estimatedBytes > MaxCachedBytes)
        {
            return;
        }

        lock (CacheGate)
        {
            if (Cache.TryGetValue(key, out var previous))
            {
                CachedBytes -= previous.Bytes;
                CacheOrder.Remove(key);
            }

            Cache[key] = (source, estimatedBytes);
            CacheOrder.AddFirst(key);
            CachedBytes += estimatedBytes;
            while (CacheOrder.Count > MaxCachedImages || CachedBytes > MaxCachedBytes)
            {
                var last = CacheOrder.Last;
                if (last is null)
                {
                    break;
                }

                CacheOrder.RemoveLast();
                if (Cache.Remove(last.Value, out var removed))
                {
                    CachedBytes -= removed.Bytes;
                }
            }
        }
    }
}
