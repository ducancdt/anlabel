namespace ANLAbel.Core.Barcode;

public static class QrOptionLists
{
    public static IReadOnlyList<QrOptionItem<QrSizingMode>> SizingModes { get; } =
    [
        new(QrSizingMode.AutoSizeByData, "Tự động theo dữ liệu"),
        new(QrSizingMode.FixedVersionAndModuleSize, "Cố định phiên bản / cỡ mô-đun")
    ];

    public static IReadOnlyList<QrOptionItem<QrErrorCorrection>> ErrorCorrections { get; } =
    [
        new(QrErrorCorrection.L, "L - Thấp (7%)"),
        new(QrErrorCorrection.M, "M - Trung bình (15%)"),
        new(QrErrorCorrection.Q, "Q - Khá (25%)"),
        new(QrErrorCorrection.H, "H - Cao (30%)")
    ];

    public static IReadOnlyList<QrOptionItem<int>> Versions { get; } = Enumerable
        .Range(1, 40)
        .Select(version => new QrOptionItem<int>(version, $"Phiên bản {version} = {QrVersionHelper.GetModuleCount(version)} × {QrVersionHelper.GetModuleCount(version)} mô-đun"))
        .ToArray();

    public static IReadOnlyList<int> ModuleSizesPx { get; } = [3, 4, 5, 6, 8, 10, 12, 16, 20];
    public static IReadOnlyList<int> QuietZoneModules { get; } = [0, 1, 2, 3, 4, 5, 6, 8, 10, 12];
}
