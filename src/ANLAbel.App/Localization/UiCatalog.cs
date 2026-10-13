using ANLAbel.Data.Preferences;

namespace ANLAbel.App.Localization;

public static class UiCatalog
{
    public static IReadOnlyList<string> Keys => Vi.Keys.ToArray();

    public static string Get(string key, UiLanguage language = UiLanguage.Vietnamese)
    {
        return Vi.TryGetValue(key, out var value) ? value : key;
    }

    private static readonly Dictionary<string, string> Vi = new(StringComparer.Ordinal)
    {
        ["Subtitle"] = "Thiết kế tem",
        ["Settings"] = "Cài đặt",
        ["SettingsTitle"] = "Cài đặt ANLAbel",
        ["Language"] = "Ngôn ngữ",
        ["LanguageEnglish"] = "Tiếng Anh",
        ["LanguageVietnamese"] = "Tiếng Việt",
        ["LanguageHint"] = "Ứng dụng ANLAbel vận hành đơn ngữ tiếng Việt chuẩn công nghiệp.",
        ["Close"] = "Đóng",
        ["Templates"] = "Mẫu tem",
        ["Library"] = "Thư viện",
        ["Import"] = "Nhập",
        ["Excel"] = "Excel",
        ["Update"] = "Cập nhật",
        ["Data"] = "Dữ liệu",
        ["Printer"] = "Máy in",
        ["Setup"] = "Cài đặt",
        ["Preview"] = "Xem trước",
        ["Print"] = "In",
        ["Current"] = "Dòng này",
        ["PrintAllRows"] = "In mọi dòng",
        ["PrintHistory"] = "Lịch sử in",
        ["ExportExcel"] = "Xuất Excel",
        ["TestPrint"] = "In thử",
        ["PrintOutput"] = "In ấn",
        ["Panels"] = "Bảng",
        ["Restore"] = "hiện lại",
        ["Objects"] = "Đối tượng",
        ["Snap"] = "dính",
        ["Grid"] = "Lưới",
        ["Workspace"] = "Làm việc",
        ["Delete"] = "Xóa",
        ["Selection"] = "Chọn",
        ["Help"] = "Trợ giúp",
        ["InsertObjects"] = "Chèn đối tượng",
        ["PrinterPrefix"] = "Máy in: ",
        ["PrinterNotSelected"] = "Chưa chọn máy in",
        ["NewTemplateTitle"] = "Mẫu tem mới",
        ["NewTemplateHeading"] = "Tạo mẫu tem mới",
        ["TemplateName"] = "Tên mẫu tem",
        ["WidthMm"] = "Chiều rộng (mm)",
        ["HeightMm"] = "Chiều cao (mm)",
        ["ResolutionDpi"] = "Độ phân giải (DPI)",
        ["Cancel"] = "Hủy",
        ["Create"] = "Tạo mẫu",
        ["HelpTitle"] = "ANLAbel - Hướng dẫn sử dụng",
        ["UserGuide"] = "Hướng dẫn sử dụng"
    };
}
