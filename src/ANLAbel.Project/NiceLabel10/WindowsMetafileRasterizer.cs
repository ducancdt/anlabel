using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace ANLAbel.Project.NiceLabel10;

/// <summary>
/// Rasterises a Windows metafile (placeable/standard WMF or EMF) into a PNG
/// at a given physical size, so a NiceLabel vector clipart can become an
/// ANLAbel raster image. Windows only: GDI+ is the only metafile player
/// available, and the importer checks <see cref="OperatingSystem.IsWindows"/>
/// before calling.
/// </summary>
[SupportedOSPlatform("windows")]
internal static class WindowsMetafileRasterizer
{
    private const double DefaultDpi = 300;
    private const int MaxPixels = 4096;

    /// <summary>Aldus placeable WMF header key.</summary>
    private static readonly byte[] PlaceableKey = { 0xD7, 0xCD, 0xC6, 0x9A };

    /// <summary>True for payloads GDI+ can play as a metafile.</summary>
    public static bool LooksLikeMetafile(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length >= 4 && bytes[..4].SequenceEqual(PlaceableKey))
        {
            return true;
        }

        // Standard WMF header: Type (1 memory / 2 disk), HeaderSize 9 words.
        if (bytes.Length >= 18 && bytes[0] is 1 or 2 && bytes[1] == 0 && bytes[2] == 9 && bytes[3] == 0)
        {
            return true;
        }

        // EMF: record type 1 then the " EMF" signature at offset 40.
        return bytes.Length >= 44
            && bytes[0] == 1 && bytes[1] == 0 && bytes[2] == 0 && bytes[3] == 0
            && bytes[40] == 0x20 && bytes[41] == (byte)'E' && bytes[42] == (byte)'M' && bytes[43] == (byte)'F';
    }

    /// <summary>
    /// Plays the metafile into a white-backed PNG of the given frame size at
    /// 300 DPI. With <paramref name="keepAspectRatio"/> the drawing is fitted
    /// and centred inside the frame; otherwise it is stretched to fill it.
    /// Returns null when GDI+ rejects the payload.
    /// </summary>
    public static (byte[] Png, int PixelWidth, int PixelHeight)? RasterizeToPng(
        byte[] metafileBytes,
        double frameWidthMm,
        double frameHeightMm,
        bool keepAspectRatio)
    {
        if (frameWidthMm <= 0 || frameHeightMm <= 0)
        {
            return null;
        }

        var pixelWidth = Math.Clamp((int)Math.Round(frameWidthMm / 25.4 * DefaultDpi), 1, MaxPixels);
        var pixelHeight = Math.Clamp((int)Math.Round(frameHeightMm / 25.4 * DefaultDpi), 1, MaxPixels);
        try
        {
            using var input = new MemoryStream(metafileBytes, writable: false);
            using var metafile = new Metafile(input);
            using var bitmap = new Bitmap(pixelWidth, pixelHeight, PixelFormat.Format32bppArgb);
            bitmap.SetResolution((float)DefaultDpi, (float)DefaultDpi);
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.Clear(Color.White);
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.DrawImage(metafile, TargetRectangle(metafile, pixelWidth, pixelHeight, keepAspectRatio));
            }

            using var output = new MemoryStream();
            bitmap.Save(output, ImageFormat.Png);
            return (output.ToArray(), pixelWidth, pixelHeight);
        }
        catch (Exception ex) when (ex is ArgumentException or ExternalException or OutOfMemoryException)
        {
            // GDI+ reports an unplayable metafile through these; the caller
            // leaves the graphic unconverted instead of guessing.
            return null;
        }
    }

    private static Rectangle TargetRectangle(Metafile metafile, int pixelWidth, int pixelHeight, bool keepAspectRatio)
    {
        if (!keepAspectRatio)
        {
            return new Rectangle(0, 0, pixelWidth, pixelHeight);
        }

        var bounds = metafile.GetMetafileHeader().Bounds;
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return new Rectangle(0, 0, pixelWidth, pixelHeight);
        }

        var scale = Math.Min(pixelWidth / (double)bounds.Width, pixelHeight / (double)bounds.Height);
        var width = Math.Max(1, (int)Math.Round(bounds.Width * scale));
        var height = Math.Max(1, (int)Math.Round(bounds.Height * scale));
        return new Rectangle((pixelWidth - width) / 2, (pixelHeight - height) / 2, width, height);
    }
}
