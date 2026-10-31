namespace ANLAbel.App.Services;

/// <summary>
/// The words on an error frame in a preview: the renderer draws them in English, kept for logs
/// and tests, and the operator reads them here. A frame names the fault in a few words; the
/// issue list beside the preview says what to do.
/// </summary>
public static class PreviewFrameText
{
    public static string Caption(string message) => message switch
    {
        "Invalid barcode data" => "Dữ liệu mã không hợp lệ",
        "Text overflow" => "Chữ tràn khung",
        "HRI cannot be laid out" => "Không đặt được dòng chữ dưới mã",
        "Barcode cannot be rendered" => "Không vẽ được mã",
        "No image" => "Chưa có ảnh",
        // BarcodeHriLayout's sentences carry measures; the frame names the fault.
        _ when message.StartsWith("HRI text is ", StringComparison.Ordinal) => "Dòng chữ dưới mã rộng hơn khung",
        _ when message.StartsWith("Barcode frame is too short for the HRI strip", StringComparison.Ordinal) => "Khung mã thấp, không đủ chỗ cho dòng chữ",
        _ when message.StartsWith("HRI text has no measurable", StringComparison.Ordinal) => "Không đo được dòng chữ dưới mã",
        _ when message.StartsWith("HRI font size must be between", StringComparison.Ordinal) => "Cỡ chữ dưới mã ngoài khoảng cho phép",
        _ when message.StartsWith("Barcode frame must have positive", StringComparison.Ordinal) => "Khung mã chưa có kích thước",
        _ when message.StartsWith("Unsupported HRI placement", StringComparison.Ordinal) => "Vị trí dòng chữ dưới mã không hỗ trợ",
        _ => message,
    };
}
