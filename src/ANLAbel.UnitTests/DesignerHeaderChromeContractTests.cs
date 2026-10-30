using System.Text.RegularExpressions;
using Xunit;

namespace ANLAbel.UnitTests;

public sealed class DesignerHeaderChromeContractTests
{
    [Fact]
    public void ShippedHeader_HasOneCommandPlacementAndUniqueGlyphs()
    {
        var inventory = DesignerHeaderChromeInventory.LoadFromRepository();

        Assert.False(inventory.QuickAccess.Any(e => e.AutomationId.Contains("Zoom", StringComparison.Ordinal)),
            "Quick Access must not host zoom.");

        // One icon, one meaning: two header commands share an icon only when they run the same action.
        var mixed = inventory.QuickAccess.Concat(inventory.Ribbon)
            .Where(e => e.Icon.Length > 0)
            .GroupBy(e => e.Icon, StringComparer.Ordinal)
            .Where(g => g.Select(e => e.Action).Distinct(StringComparer.Ordinal).Count() > 1)
            .Select(g => g.Key + ": " + string.Join(", ", g.Select(e => e.AutomationId)))
            .ToArray();
        Assert.True(mixed.Length == 0, "Two different header actions must not share one icon: " + string.Join("; ", mixed));

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

        // docs/NICELABEL_10_UI_LAYOUT.md "Shell rebuild": the ribbon draws one vector icon family.
        Assert.DoesNotContain(inventory.Ribbon, e => e.Icon.StartsWith("Icons/", StringComparison.Ordinal));
        Assert.Contains(inventory.Ribbon, e => e.AutomationId == "Shell.Ribbon.SnapObjects" && e.Icon == "Icon.SnapObjects16");
        Assert.Contains(inventory.Ribbon, e => e.AutomationId == "Shell.Ribbon.SnapGrid" && e.Icon == "Icon.SnapGrid16");
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
        "Shell.Ribbon",
        "Shell.Ribbon.File",
        "Shell.File.SaveAs",
        "Shell.File.Revisions",
        "Shell.File.Settings",
        "Shell.Ribbon.Templates",
        "Shell.Ribbon.ImportExcel",
        "Shell.Ribbon.ImportTextFile",
        "Shell.Ribbon.EditDatabase",
        "Shell.Ribbon.UpdateExcel",
        "Shell.Ribbon.DeleteDatabase",
        "Shell.Ribbon.PrinterSetup",
        "Shell.Ribbon.Preview",
        "Shell.Ribbon.PrintMore",
        "Shell.Ribbon.PrintCurrent",
        "Shell.Ribbon.PrintAllRows",
        "Shell.Ribbon.PrintHistory",
        "Shell.Ribbon.ExportExcel",
        "Shell.Ribbon.TestPrint",
        "Shell.Ribbon.Panels",
        "Shell.Ribbon.Gridlines",
        "Shell.Ribbon.SnapObjects",
        "Shell.Ribbon.SnapGrid",
        "Shell.Ribbon.DeleteSelection",
        "Shell.Ribbon.Help",
        "Shell.Ribbon.About"
    };

    /// <summary>A header command: its id, the icon it draws and the action it runs.</summary>
    public sealed record Entry(string AutomationId, string Icon, string Action);

    public required IReadOnlyList<Entry> QuickAccess { get; init; }
    public required IReadOnlyList<Entry> Ribbon { get; init; }
    public required IReadOnlyList<string> StatusAutomationIds { get; init; }

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
        var entries = new List<Entry> { new(rootId, string.Empty, string.Empty) };
        // Self-closing or paired command elements; a paired element's body holds its glyph or menu icon.
        var controlRx = new Regex(
            @"<(?<tag>Button|ToggleButton|RadioButton|MenuItem|controls:RibbonSplitButton)\b(?<attrs>[^>]*?)(?<self>/?)>",
            RegexOptions.CultureInvariant);
        foreach (Match match in controlRx.Matches(slice))
        {
            var attrs = match.Groups["attrs"].Value;
            var id = First(attrs, @"AutomationProperties.AutomationId=""([^""]+)""");
            if (id.Length == 0 || string.Equals(id, rootId, StringComparison.Ordinal))
            {
                continue;
            }

            var body = string.Empty;
            if (match.Groups["self"].Value.Length == 0)
            {
                var close = slice.IndexOf("</" + match.Groups["tag"].Value + ">", match.Index + match.Length, StringComparison.Ordinal);
                body = close > 0 ? slice.Substring(match.Index + match.Length, close - match.Index - match.Length) : string.Empty;
            }

            var icon = First(attrs, @"controls:Ribbon.Icon=""\{StaticResource (Icon\.[A-Za-z0-9]+)\}""");
            if (icon.Length == 0)
            {
                // a menu item's own icon, or a quick access glyph
                icon = First(body, @"^\s*<MenuItem\.Icon>\s*<Image[^>]*Source=""\{StaticResource (Icon\.[A-Za-z0-9]+)\}""");
            }

            if (icon.Length == 0)
            {
                icon = First(body, @"Style=""\{StaticResource ChromeGlyph\}""[^>]*Text=""([^""]+)""");
            }

            if (icon.Length == 0)
            {
                icon = First(attrs + body, @"Source=""(Icons/[^""]+)""");
            }

            var action = First(attrs, @"Click=""([^""]+)""");
            var tag = First(attrs, @"\bTag=""([^""]+)""");
            if (action.Length == 0)
            {
                action = First(attrs, @"Command=""([^""]+)""");
            }

            if (action.Length == 0)
            {
                action = First(attrs, @"IsChecked=""([^""]+)""");
            }

            entries.Add(new Entry(id, icon, action.Length == 0 ? "drop-down " + id : tag.Length == 0 ? action : action + ":" + tag));
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
