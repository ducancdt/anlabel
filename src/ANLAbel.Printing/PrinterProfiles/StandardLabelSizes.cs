namespace ANLAbel.Printing.PrinterProfiles;

/// <summary>
/// Built-in catalog of common label/thermal paper sizes used in industrial label printers,
/// logistics, and production environments. Works with any printer — no driver detection needed.
/// </summary>
public static class StandardLabelSizes
{
    public static IReadOnlyList<PrinterPaperInfo> All { get; } = BuildCatalog();

    public static IReadOnlyList<PrinterPaperInfo> GetByCategory(string category)
    {
        return All.Where(item => string.Equals(item.Category, category, StringComparison.OrdinalIgnoreCase)).ToArray();
    }

    public static IReadOnlyList<string> Categories { get; } = All
        .Select(item => item.Category)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray();

    private static List<PrinterPaperInfo> BuildCatalog()
    {
        var list = new List<PrinterPaperInfo>();

        // Standard thermal label sizes (industrial/logistics)
        AddRange(list, "Standard Thermal Labels", new (string name, double w, double h)[]
        {
            ("30 × 20 mm", 30, 20),
            ("38 × 25 mm", 38, 25),
            ("40 × 20 mm", 40, 20),
            ("40 × 30 mm", 40, 30),
            ("43 × 25 mm", 43, 25),
            ("48 × 25 mm", 48, 25),
            ("50 × 20 mm", 50, 20),
            ("50 × 25 mm", 50, 25),
            ("50 × 30 mm", 50, 30),
            ("50 × 40 mm", 50, 40),
            ("50 × 50 mm", 50, 50),
            ("52 × 30 mm", 52, 30),
            ("55 × 30 mm", 55, 30),
            ("58 × 40 mm", 58, 40),
            ("60 × 20 mm", 60, 20),
            ("60 × 30 mm", 60, 30),
            ("60 × 40 mm", 60, 40),
            ("60 × 50 mm", 60, 50),
            ("65 × 30 mm", 65, 30),
            ("65 × 35 mm", 65, 35),
            ("70 × 30 mm", 70, 30),
            ("70 × 35 mm", 70, 35),
            ("70 × 40 mm", 70, 40),
            ("70 × 50 mm", 70, 50),
            ("75 × 50 mm", 75, 50),
            ("75 × 100 mm", 75, 100),
            ("76 × 51 mm (3×2\")", 76, 51),
            ("80 × 40 mm", 80, 40),
            ("80 × 50 mm", 80, 50),
            ("80 × 60 mm", 80, 60),
            ("82 × 45 mm", 82, 45),
            ("90 × 50 mm", 90, 50),
            ("90 × 60 mm", 90, 60),
            ("100 × 40 mm", 100, 40),
            ("100 × 50 mm", 100, 50),
            ("100 × 60 mm", 100, 60),
            ("100 × 80 mm", 100, 80),
            ("100 × 100 mm", 100, 100),
            ("102 × 152 mm (4×6\")", 102, 152),
            ("105 × 70 mm", 105, 70),
            ("110 × 50 mm", 110, 50),
            ("110 × 70 mm", 110, 70),
            ("120 × 60 mm", 120, 60),
            ("120 × 80 mm", 120, 80),
            ("120 × 100 mm", 120, 100),
            ("130 × 80 mm", 130, 80),
            ("150 × 100 mm", 150, 100),
            ("150 × 150 mm", 150, 150),
            ("200 × 100 mm", 200, 100),
            ("200 × 150 mm", 200, 150),
            ("210 × 148 mm (A5)", 210, 148),
        });

        // Shipping / warehouse labels
        AddRange(list, "Shipping & Warehouse", new (string name, double w, double h)[]
        {
            ("100 × 150 mm shipping", 100, 150),
            ("102 × 152 mm (4×6\" shipping)", 102, 152),
            ("102 × 203 mm (4×8\")", 102, 203),
            ("104 × 159 mm (Zebra 4×6\")", 104, 159),
            ("152 × 203 mm (6×8\")", 152, 203),
            ("100 × 200 mm", 100, 200),
            ("A4 210 × 297 mm", 210, 297),
        });

        // Jewelry / small product labels
        AddRange(list, "Small & Jewelry Labels", new (string name, double w, double h)[]
        {
            ("20 × 10 mm", 20, 10),
            ("25 × 15 mm", 25, 15),
            ("30 × 15 mm", 30, 15),
            ("30 × 25 mm", 30, 25),
            ("35 × 15 mm", 35, 15),
            ("40 × 15 mm", 40, 15),
            ("50 × 15 mm", 50, 15),
            ("52 × 20 mm", 52, 20),
            ("55 × 20 mm", 55, 20),
            ("58 × 30 mm", 58, 30),
        });

        // Dymo-compatible sizes
        AddRange(list, "Dymo Compatible", new (string name, double w, double h)[]
        {
            ("Dymo 89 × 36 mm", 89, 36),
            ("Dymo 54 × 101 mm", 54, 101),
            ("Dymo 101 × 54 mm landscape", 101, 54),
            ("Dymo 89 × 28 mm", 89, 28),
            ("Dymo 89 × 41 mm", 89, 41),
            ("Dymo 57 × 32 mm", 57, 32),
            ("Dymo 104 × 159 mm", 104, 159),
        });

        // Brother QL-compatible sizes
        AddRange(list, "Brother QL Compatible", new (string name, double w, double h)[]
        {
            ("Brother 17 × 54 mm", 17, 54),
            ("Brother 29 × 90 mm", 29, 90),
            ("Brother 38 × 90 mm", 38, 90),
            ("Brother 62 × 29 mm", 62, 29),
            ("Brother 62 × 100 mm", 62, 100),
            ("Brother 102 × 152 mm", 102, 152),
        });

        // Rack / shelf labels
        AddRange(list, "Rack & Shelf Labels", new (string name, double w, double h)[]
        {
            ("200 × 50 mm rack", 200, 50),
            ("200 × 75 mm rack", 200, 75),
            ("200 × 100 mm rack", 200, 100),
            ("250 × 100 mm", 250, 100),
            ("300 × 100 mm", 300, 100),
            ("210 × 48 mm shelf", 210, 48),
            ("210 × 98 mm shelf", 210, 98),
        });

        return list;
    }

    private static void AddRange(List<PrinterPaperInfo> list, string category, (string name, double widthMm, double heightMm)[] sizes)
    {
        foreach (var (name, widthMm, heightMm) in sizes)
        {
            list.Add(new PrinterPaperInfo
            {
                Name = name,
                WidthMm = widthMm,
                HeightMm = heightMm,
                Category = category,
                Source = PaperSizeSourceKind.StandardCatalog
            });
        }
    }
}

