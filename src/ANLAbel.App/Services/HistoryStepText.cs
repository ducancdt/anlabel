using ANLAbel.Core.Models;

namespace ANLAbel.App.Services;

/// <summary>One change inside an undo step: what was done, and to which object (none for the label itself).</summary>
public readonly record struct HistoryChange(string? Action, LabelObject? Target);

/// <summary>
/// Names an undo step from the changes it holds, for the Undo and Redo lists
/// and the status bar: "Di chuyển “PO”", "Đổi phông chữ 3 đối tượng", "Đổi khổ
/// tem". NiceLabel 10 lists its undo steps by name the same way.
/// </summary>
public static class HistoryStepText
{
    public const string Added = "Thêm";
    public const string Removed = "Xóa";
    public const string Reordered = "Đổi thứ tự lớp";
    public const string Prompts = "Sửa câu nhắc khi in";
    public const string Counter = "Sửa bộ đếm";
    public const string Guides = "Sửa đường gióng";
    private const string Moved = "Di chuyển";
    private const string Resized = "Đổi kích thước";
    private const string Edited = "Sửa";
    private const string LabelSize = "Đổi khổ tem";
    private static readonly string[] BarcodePrefixes = ["Barcode", "Qr", "Pdf417", "CheckDigit", "Bearer"];

    /// <summary>What a change of an object property does.</summary>
    public static string ObjectAction(string? property) => property switch
    {
        nameof(LabelObject.XMm) or nameof(LabelObject.YMm) => Moved,
        nameof(LabelObject.WidthMm) or nameof(LabelObject.HeightMm) or nameof(LabelObject.LineEndXMm)
            or nameof(LabelObject.LineEndYMm) or nameof(LabelObject.KeepAspectRatio) => Resized,
        nameof(LabelObject.Rotation) => "Xoay",
        nameof(LabelObject.ZIndex) => Reordered,
        nameof(LabelObject.Name) => "Đổi tên",
        nameof(LabelObject.Type) => "Đổi loại",
        nameof(LabelObject.Text) or nameof(LabelObject.RichText) => "Sửa nội dung",
        nameof(LabelObject.BindingExpression) => "Đổi nguồn dữ liệu",
        nameof(LabelObject.VisibilityCondition) => "Đổi điều kiện in",
        nameof(LabelObject.IsLocked) => "Khóa/mở khóa",
        nameof(LabelObject.IsVisible) => "Ẩn/hiện",
        nameof(LabelObject.IsPrintable) => "Bật/tắt in",
        nameof(LabelObject.GroupId) => "Nhóm/bỏ nhóm",
        nameof(LabelObject.AnchorPoint) => "Đổi điểm neo",
        nameof(LabelObject.ContentMask) or nameof(LabelObject.ContentMaskCharacter) => "Đổi mặt nạ nội dung",
        nameof(LabelObject.Style) => "Đổi định dạng",
        nameof(LabelObject.ImageDataBase64) or nameof(LabelObject.ImageRasterMode) or nameof(LabelObject.ImagePixelWidth)
            or nameof(LabelObject.ImagePixelHeight) or nameof(LabelObject.PictureSizing)
            or nameof(LabelObject.PictureFlipHorizontal) or nameof(LabelObject.PictureFlipVertical) => "Đổi ảnh",
        nameof(LabelObject.ShowBarcodeText) or nameof(LabelObject.DataMatrixSymbolSize)
            or nameof(LabelObject.Code39IntercharacterGapModules) => "Đổi thiết lập mã vạch",
        { } name when BarcodePrefixes.Any(prefix => name.StartsWith(prefix, StringComparison.Ordinal)) => "Đổi thiết lập mã vạch",
        _ => Edited
    };

    /// <summary>What a change of an object's text style does.</summary>
    public static string StyleAction(string? property) => property switch
    {
        nameof(ObjectStyle.FontFamily) or nameof(ObjectStyle.FontSizePt) or nameof(ObjectStyle.LineHeightPt)
            or nameof(ObjectStyle.Bold) or nameof(ObjectStyle.Italic) or nameof(ObjectStyle.Underline)
            or nameof(ObjectStyle.Strikethrough) or nameof(ObjectStyle.MirrorText) or nameof(ObjectStyle.TextDirection)
            or nameof(ObjectStyle.FontScalePercent) => "Đổi phông chữ",
        nameof(ObjectStyle.Alignment) or nameof(ObjectStyle.VerticalAlignment) => "Căn chữ",
        nameof(ObjectStyle.BorderThicknessMm) or nameof(ObjectStyle.OutlineStyle) or nameof(ObjectStyle.FillStyle)
            or nameof(ObjectStyle.CornerRadiusMm) or nameof(ObjectStyle.FillColor) or nameof(ObjectStyle.StrokeColor) => "Đổi viền và nền",
        { } name when name.StartsWith("TextPadding", StringComparison.Ordinal) => "Đổi lề chữ",
        _ => "Đổi định dạng"
    };

    /// <summary>What a change of a label property does.</summary>
    public static string LabelAction(string? property) => property switch
    {
        nameof(LabelTemplate.WidthMm) or nameof(LabelTemplate.HeightMm) or nameof(LabelTemplate.Orientation)
            or nameof(LabelTemplate.GapMm) or nameof(LabelTemplate.MarginMm) => LabelSize,
        nameof(LabelTemplate.Name) => "Đổi tên tem",
        nameof(LabelTemplate.ProtectFromEditing) or nameof(LabelTemplate.EditingPasswordHash) => "Đổi bảo vệ tem",
        _ => "Đổi thiết lập tem"
    };

    /// <summary>What a change of the printer settings does; their label size follows the label's.</summary>
    public static string PrinterAction(string? property) => property is nameof(PrinterProfile.LabelWidthMm) or nameof(PrinterProfile.LabelHeightMm)
        ? LabelSize
        : "Đổi thiết lập máy in";

    /// <summary>
    /// The name of a step: an added or removed object comes first, then a
    /// resize (dragging a left or top handle moves the object too), else the
    /// first change that is not a plain edit; with the object it touched, or
    /// how many objects.
    /// </summary>
    public static string Compose(IReadOnlyList<HistoryChange> changes)
    {
        var actions = changes.Select(change => change.Action).OfType<string>().Distinct().ToList();
        var action = actions.Contains(Added) ? Added
            : actions.Contains(Removed) ? Removed
            : actions.Contains(Resized) && actions.Contains(Moved) ? Resized
            : actions.FirstOrDefault(item => item != Edited) ?? actions.FirstOrDefault();
        if (action is null)
        {
            return "Sửa tem";
        }

        var targets = changes
            .Where(change => change.Action == action && change.Target is not null)
            .Select(change => change.Target!)
            .Distinct<LabelObject>(ReferenceEqualityComparer.Instance)
            .ToList();
        return targets.Count switch
        {
            0 => action == Edited ? "Sửa tem" : action,
            1 when !string.IsNullOrWhiteSpace(targets[0].Name) => $"{action} “{targets[0].Name}”",
            _ => $"{action} {targets.Count} đối tượng"
        };
    }
}
