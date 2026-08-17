using ANLAbel.Core.Text;
using Xunit;

namespace ANLAbel.UnitTests;

public sealed class TextStylePickerChromeContractTests
{
    [Fact]
    public void PropertiesXaml_ShipsExcelLikeFontToolbar()
    {
        var xaml = File.ReadAllText(FindMainWindowXaml());

        Assert.Contains("AutomationProperties.AutomationId=\"Properties.TextStyle.FontFamily\"", xaml);
        Assert.Contains("AutomationProperties.AutomationId=\"Properties.TextStyle.FontSize\"", xaml);
        Assert.Contains("AutomationProperties.AutomationId=\"Properties.TextStyle.Bold\"", xaml);
        Assert.Contains("AutomationProperties.AutomationId=\"Properties.TextStyle.Italic\"", xaml);
        Assert.Contains("AutomationProperties.AutomationId=\"Properties.TextStyle.Underline\"", xaml);
        Assert.Contains("AutomationProperties.AutomationId=\"Properties.TextStyle.IconGroup\"", xaml);
        Assert.Contains("AutomationProperties.AutomationId=\"Properties.TextStyle.AlignLeft\"", xaml);
        Assert.Contains("AutomationProperties.AutomationId=\"Properties.TextStyle.AlignCenter\"", xaml);
        Assert.Contains("AutomationProperties.AutomationId=\"Properties.TextStyle.AlignRight\"", xaml);
        Assert.Contains("AutomationProperties.AutomationId=\"Properties.TextStyle.AlignJustify\"", xaml);
        Assert.Contains("AutomationProperties.AutomationId=\"Properties.TextStyle.AlignTop\"", xaml);
        Assert.Contains("AutomationProperties.AutomationId=\"Properties.TextStyle.AlignMiddle\"", xaml);
        Assert.Contains("AutomationProperties.AutomationId=\"Properties.TextStyle.AlignBottom\"", xaml);
        Assert.DoesNotContain("ItemsSource=\"{Binding TextAlignments}\"", SliceTextStyle(xaml));
        Assert.DoesNotContain("ItemsSource=\"{Binding TextVerticalAlignments}\"", SliceTextStyle(xaml));
        Assert.Contains("IsEditable=\"True\"", xaml);
        Assert.Contains("FontFamily=\"{Binding}\"", xaml);
        Assert.Contains("Style=\"{StaticResource FontStyleIcon}\"", xaml);
        Assert.Contains("ApplyTypedFontSize", File.ReadAllText(FindMainWindowCodeBehind()));
        var viewModel = File.ReadAllText(FindMainViewModel());
        Assert.Contains("TextStylePickerCatalog.StandardSizesPt", viewModel);
        Assert.Contains("TextStylePickerCatalog.FilterInstalled", viewModel);
        Assert.DoesNotContain("PreferredIndustrialFonts", viewModel);
        Assert.DoesNotContain("Content=\"Bold\"", SliceTextStyle(xaml));
    }

    [Fact]
    public void TypedSizePath_UsesTheShippedCatalog()
    {
        Assert.True(TextStylePickerCatalog.TryParseSizePt("9.5", out var size));
        Assert.Equal(9.5, size);
        Assert.Contains(11d, TextStylePickerCatalog.StandardSizesPt);
    }

    private static string SliceTextStyle(string xaml)
    {
        var start = xaml.IndexOf("Properties.TextStyle.FontFamily", StringComparison.Ordinal);
        var end = xaml.IndexOf("Text=\"Align\"", start, StringComparison.Ordinal);
        Assert.True(start >= 0 && end > start);
        return xaml[start..end];
    }

    private static string FindMainWindowXaml() => FindRepoFile("src", "ANLAbel.App", "MainWindow.xaml");

    private static string FindMainWindowCodeBehind() => FindRepoFile("src", "ANLAbel.App", "MainWindow.xaml.cs");

    private static string FindMainViewModel() => FindRepoFile("src", "ANLAbel.App", "ViewModels", "MainViewModel.cs");

    private static string FindRepoFile(params string[] parts)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(new[] { dir.FullName }.Concat(parts).ToArray());
            if (File.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        throw new FileNotFoundException(string.Join(Path.DirectorySeparatorChar, parts));
    }
}
