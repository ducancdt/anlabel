using System.Text.RegularExpressions;
using Xunit;

namespace ANLAbel.UnitTests;

public sealed class DesignerHeaderChromeContractTests
{
    [Fact]
    public void ShippedHeader_HasOneCommandPlacementAndUniqueGlyphs()
    {
        var inventory = DesignerHeaderChromeInventory.LoadFromRepository();

        Assert.False(inventory.QuickAccess.Any(e => e.Icon.Contains("zoom_", StringComparison.Ordinal)),
            "Quick Access must not host zoom.");
        Assert.False(inventory.Ribbon.Any(e => e.Icon.Contains("zoom_", StringComparison.Ordinal)),
            "Ribbon must not host zoom; zoom stays on the status bar.");

        var headerIcons = inventory.HeaderIcons;
        var duplicateIcons = headerIcons
            .GroupBy(icon => icon, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToArray();
        Assert.True(duplicateIcons.Length == 0,
            "Two header actions must not share one PNG: " + string.Join(", ", duplicateIcons));

        var headerIds = inventory.HeaderAutomationIds;
        var duplicateIds = headerIds
            .GroupBy(id => id, StringComparer.Ordinal)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToArray();
        Assert.True(duplicateIds.Length == 0,
            "Header AutomationIds must be unique: " + string.Join(", ", duplicateIds));

        foreach (var required in DesignerHeaderChromeInventory.RequiredHeaderAutomationIds)
        {
            Assert.Contains(required, headerIds);
        }

        Assert.Contains("Icons/snap_objects.png", headerIcons, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("Icons/snap_grid.png", headerIcons, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("Icons/cursor_select.png", headerIcons, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("Icons/table.png", headerIcons, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("Shell.Status.Zoom", inventory.StatusAutomationIds);
    }
}

internal sealed class DesignerHeaderChromeInventory
{
    public static readonly string[] RequiredHeaderAutomationIds =
    {
        "Shell.QuickAccess",
        "Shell.QuickAccess.New",
        "Shell.QuickAccess.Open",
        "Shell.QuickAccess.Save",
        "Shell.QuickAccess.Undo",
        "Shell.QuickAccess.Redo",
        "Shell.QuickAccess.Revisions",
        "Shell.Ribbon",
        "Shell.Ribbon.Templates",
        "Shell.Ribbon.ImportExcel",
        "Shell.Ribbon.UpdateExcel",
        "Shell.Ribbon.PrinterSetup",
        "Shell.Ribbon.Preview",
        "Shell.Ribbon.PrintCurrent",
        "Shell.Ribbon.PrintAllRows",
        "Shell.Ribbon.PrintHistory",
        "Shell.Ribbon.ExportExcel",
        "Shell.Ribbon.TestPrint",
        "Shell.Ribbon.Panels",
        "Shell.Ribbon.SnapObjects",
        "Shell.Ribbon.SnapGrid",
        "Shell.Ribbon.DeleteSelection",
        "Shell.Ribbon.Help"
    };

    public sealed record Entry(string AutomationId, string Icon);

    public required IReadOnlyList<Entry> QuickAccess { get; init; }
    public required IReadOnlyList<Entry> Ribbon { get; init; }
    public required IReadOnlyList<string> StatusAutomationIds { get; init; }

    public IReadOnlyList<string> HeaderIcons =>
        QuickAccess.Concat(Ribbon)
            .Select(e => e.Icon)
            .Where(icon => icon.Length > 0)
            .ToArray();

    public IReadOnlyList<string> HeaderAutomationIds =>
        QuickAccess.Select(e => e.AutomationId)
            .Concat(Ribbon.Select(e => e.AutomationId))
            .Where(id => id.Length > 0)
            .ToArray();

    public static DesignerHeaderChromeInventory LoadFromRepository()
    {
        var xamlPath = FindMainWindowXaml();
        var xaml = File.ReadAllText(xamlPath);
        return Parse(xaml);
    }

    public static DesignerHeaderChromeInventory Parse(string xaml)
    {
        var quick = Slice(xaml, "AutomationId=\"Shell.QuickAccess\"", "AutomationId=\"Shell.Ribbon\"");
        var ribbon = Slice(xaml, "AutomationId=\"Shell.Ribbon\"", "AutomationId=\"Shell.Status\"");
        var status = Slice(xaml, "AutomationId=\"Shell.Status\"", "AutomationId=\"Shell.LeftColumn\"");
        return new DesignerHeaderChromeInventory
        {
            QuickAccess = ExtractEntries(quick, "Shell.QuickAccess"),
            Ribbon = ExtractEntries(ribbon, "Shell.Ribbon"),
            StatusAutomationIds = ExtractAutomationIds(status)
        };
    }

    internal static string FindMainWindowXaml()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "src", "ANLAbel.App", "MainWindow.xaml");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate src/ANLAbel.App/MainWindow.xaml from the test directory.");
    }

    private static string Slice(string text, string startMarker, string endMarker)
    {
        var start = text.IndexOf(startMarker, StringComparison.Ordinal);
        Assert.True(start >= 0, "Missing header marker " + startMarker);
        var end = text.IndexOf(endMarker, start, StringComparison.Ordinal);
        Assert.True(end > start, "Missing header end marker " + endMarker);
        return text.Substring(start, end - start);
    }

    private static List<Entry> ExtractEntries(string slice, string rootId)
    {
        var entries = new List<Entry> { new(rootId, string.Empty) };
        var controlRx = new Regex(
            @"<(Button|ToggleButton)\b(?<attrs>[\s\S]*?)>(?<body>[\s\S]*?)</\1>",
            RegexOptions.CultureInvariant);
        foreach (Match match in controlRx.Matches(slice))
        {
            var attrs = match.Groups["attrs"].Value;
            var body = match.Groups["body"].Value;
            var id = First(attrs, @"AutomationProperties.AutomationId=""([^""]+)""");
            if (id.Length == 0 || string.Equals(id, rootId, StringComparison.Ordinal))
            {
                continue;
            }

            var icon = First(attrs + body, @"Source=""(Icons/[^""]+)""");
            entries.Add(new Entry(id, icon));
        }

        return entries;
    }

    private static List<string> ExtractAutomationIds(string slice)
    {
        var ids = new List<string>();
        foreach (Match match in Regex.Matches(slice, @"AutomationProperties.AutomationId=""([^""]+)"""))
        {
            ids.Add(match.Groups[1].Value);
        }

        return ids;
    }

    private static string First(string text, string pattern)
    {
        var match = Regex.Match(text, pattern);
        return match.Success ? match.Groups[1].Value : string.Empty;
    }
}
