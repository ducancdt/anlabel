using ANLAbel.Core.Enums;

namespace ANLAbel.App.Services;

/// <summary>
/// Vietnamese names for object types and the option lists of the Properties
/// panel, matching the toolbox labels (audit 2026-10-13). The stored values
/// stay the enum names.
/// </summary>
public static class DisplayNames
{
    public static string ForObjectType(ObjectType type) => type switch
    {
        ObjectType.Text => "Chữ",
        ObjectType.TextBox => "Ô chữ",
        ObjectType.BarcodeCode128 => "Mã vạch",
        ObjectType.QRCode => "Mã QR",
        ObjectType.DataMatrix => "Data Matrix",
        ObjectType.Rectangle => "Hình chữ nhật",
        ObjectType.Ellipse => "Hình bầu dục",
        ObjectType.Line => "Đường kẻ",
        ObjectType.Image => "Hình ảnh",
        _ => "Đối tượng",
    };

    public static string For(object? value) => value switch
    {
        null => string.Empty,
        ObjectType type => ForObjectType(type),
        FillStyle.None => "Không",
        FillStyle.Solid => "Đặc",
        OutlineStyle.None => "Không",
        OutlineStyle.Solid => "Liền",
        OutlineStyle.Dash => "Gạch",
        OutlineStyle.Dot => "Chấm",
        BarcodeApplicationProfile.General => "Chung",
        BarcodeApplicationProfile.Industrial => "Công nghiệp",
        BarcodeApplicationProfile.Gs1 => "GS1",
        BarcodeHriPlacement.None => "Không",
        BarcodeHriPlacement.Below => "Dưới",
        BarcodeHriPlacement.Above => "Trên",
        BarcodeCheckDigitPolicy.None => "Không",
        BarcodeCheckDigitPolicy.Auto => "Tự động",
        BarcodeCheckDigitPolicy.Verify => "Xác minh",
        BarcodeCheckDigitPolicy.Generate => "Tự sinh",
        Code128CodeSet.Auto => "Tự động",
        Code128CodeSet.A => "Bộ A",
        Code128CodeSet.B => "Bộ B",
        Code128CodeSet.C => "Bộ C",
        Code128CodeSet.Manual => "Thủ công (<MODEA/B/C>)",
        Code39WideNarrowRatio.LegacyEngineDefault => "Mặc định (bộ máy cũ)",
        Code39WideNarrowRatio.Ratio2_0 => "2.0:1",
        Code39WideNarrowRatio.Ratio2_2 => "2.2:1",
        Code39WideNarrowRatio.Ratio2_5 => "2.5:1",
        Code39WideNarrowRatio.Ratio3_0 => "3.0:1",
        BearerBarStyle.None => "Không",
        BearerBarStyle.TopBottom => "Trên và dưới",
        BearerBarStyle.Frame => "Khung bao",
        ImageRasterMode.DriverManaged => "Trình điều khiển xử lý",
        ImageRasterMode.MonochromeThreshold => "Đơn sắc theo ngưỡng",
        ImageRasterMode.MonochromeOrderedDither => "Đơn sắc hòa sắc",
        TextDirectionMode.Auto => "Tự động",
        TextDirectionMode.LeftToRight => "Trái sang phải",
        TextDirectionMode.RightToLeft => "Phải sang trái",
        _ => value.ToString() ?? string.Empty,
    };
}
