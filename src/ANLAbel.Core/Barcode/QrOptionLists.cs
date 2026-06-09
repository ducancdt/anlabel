namespace ANLAbel.Core.Barcode;

public static class QrOptionLists
{
    public static IReadOnlyList<QrOptionItem<QrSizingMode>> SizingModes { get; } =
    [
        new(QrSizingMode.AutoSizeByData, "Auto size by data"),
        new(QrSizingMode.FixedVersionAndModuleSize, "Fixed version / fixed module size")
    ];

    public static IReadOnlyList<QrOptionItem<QrErrorCorrection>> ErrorCorrections { get; } =
    [
        new(QrErrorCorrection.L, "L - Low"),
        new(QrErrorCorrection.M, "M - Medium"),
        new(QrErrorCorrection.Q, "Q - Quartile"),
        new(QrErrorCorrection.H, "H - High")
    ];

    public static IReadOnlyList<QrOptionItem<int>> Versions { get; } = Enumerable
        .Range(1, 40)
        .Select(version => new QrOptionItem<int>(version, $"Version {version} = {QrVersionHelper.GetModuleCount(version)} x {QrVersionHelper.GetModuleCount(version)} modules"))
        .ToArray();

    public static IReadOnlyList<int> ModuleSizesPx { get; } = [3, 4, 5, 6, 8, 10, 12, 16, 20];
    public static IReadOnlyList<int> QuietZoneModules { get; } = [0, 1, 2, 3, 4, 5, 6, 8];
}
