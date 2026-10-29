using System.Globalization;
using ANLAbel.Project.NiceLabel10;

namespace ANLAbel.App.Services;

/// <summary>
/// Explains, in the operator's language, what an imported NiceLabel 10
/// label's printer and paper settings became in ANLAbel and what the
/// operator still has to choose.
/// </summary>
public static class NiceLabelPrinterSetupText
{
    public static string Describe(NiceLabelPrinterSetupResult? result)
    {
        if (result is null || result.Setup == NiceLabelMediaSetup.None)
        {
            return string.Empty;
        }

        var printer = string.IsNullOrWhiteSpace(result.AuthoredPrinterName)
            ? "máy in mặc định của Windows"
            : $"\"{result.AuthoredPrinterName}\"";
        var page = $"{Mm(result.PageWidthMm)} x {Mm(result.PageHeightMm)} mm";
        return result.Setup switch
        {
            NiceLabelMediaSetup.LabelStock => result.AppliedDpi > 0
                ? $"Khổ cuộn tem {page} và độ phân giải {result.AppliedDpi} DPI lấy từ tệp; tệp được thiết kế cho máy in {printer}, hãy chọn máy in nhãn trong Cài đặt máy in."
                : $"Khổ cuộn tem {page} lấy từ tệp; tệp được thiết kế cho máy in {printer}, hãy chọn máy in nhãn trong Cài đặt máy in.",
            NiceLabelMediaSetup.DriverSettings =>
                $"Tệp dùng khổ giấy và độ phân giải của driver máy in {printer}; ANLAbel cũng để driver quyết định, hãy chọn máy in nhãn có khổ tem đúng trong Cài đặt máy in.",
            NiceLabelMediaSetup.OfficeSheet =>
                $"Tem in trên tờ {page} như trong NiceLabel; tệp được thiết kế cho máy in {printer}, hãy chọn máy in trong Cài đặt máy in.",
            NiceLabelMediaSetup.MultipleLabelsPerPage =>
                $"Tệp in {result.LabelsAcross * result.LabelsDown} tem trên một trang {page} ({result.LabelsAcross} x {result.LabelsDown}); ANLAbel in đúng lưới đó. Tệp được thiết kế cho máy in {printer}, hãy chọn máy in trong Cài đặt máy in.",
            _ => string.Empty
        };
    }

    private static string Mm(double value) => value.ToString("0.##", CultureInfo.GetCultureInfo("vi-VN"));
}
