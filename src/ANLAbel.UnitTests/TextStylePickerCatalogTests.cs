using ANLAbel.Core.Text;
using Xunit;

namespace ANLAbel.UnitTests;

public sealed class TextStylePickerCatalogTests
{
    [Fact]
    public void StandardSizes_IncludeIndustrialLabelRange()
    {
        Assert.Contains(4d, TextStylePickerCatalog.StandardSizesPt);
        Assert.Contains(11d, TextStylePickerCatalog.StandardSizesPt);
        Assert.Contains(32d, TextStylePickerCatalog.StandardSizesPt);
        Assert.Equal(4, TextStylePickerCatalog.MinimumSizePt);
        Assert.Equal(200, TextStylePickerCatalog.MaximumSizePt);
    }

    [Theory]
    [InlineData("9.5", 9.5)]
    [InlineData("11", 11)]
    [InlineData(" 8 ", 8)]
    [InlineData("32", 32)]
    public void TryParseSizePt_AcceptsTypedExcelValues(string text, double expected)
    {
        Assert.True(TextStylePickerCatalog.TryParseSizePt(text, out var size));
        Assert.Equal(expected, size);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("abc")]
    [InlineData("0")]
    [InlineData("3.9")]
    [InlineData("201")]
    [InlineData("NaN")]
    [InlineData("Infinity")]
    public void TryParseSizePt_FailsClosed_OnUnusableInput(string? text)
    {
        Assert.False(TextStylePickerCatalog.TryParseSizePt(text, out var size));
        Assert.Equal(0, size);
    }

    [Theory]
    [InlineData("Arial")]
    [InlineData("Segoe UI")]
    [InlineData("Consolas")]
    [InlineData("Liberation Sans")]
    public void IsLicensedFamily_AcceptsDocumentedFamilies(string family)
    {
        Assert.True(TextStylePickerCatalog.IsLicensedFamily(family));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("CustomerBrand")]
    [InlineData("Comic Sans MS")]
    [InlineData("Segoe UI Semibold")]
    [InlineData("Arial Narrow")]
    public void IsLicensedFamily_RejectsUnlicensedOrUnknownFamilies(string? family)
    {
        Assert.False(TextStylePickerCatalog.IsLicensedFamily(family));
    }

    [Fact]
    public void FilterInstalled_KeepsWhitelistOrder_AndDropsUnknownFaces()
    {
        var filtered = TextStylePickerCatalog.FilterInstalled(
            new[] { "CustomerBrand", "Consolas", "Arial", "Hack" });

        Assert.Equal(new[] { "Arial", "Consolas" }, filtered);
        Assert.DoesNotContain("CustomerBrand", filtered);
        Assert.DoesNotContain("Hack", filtered);
    }

    [Fact]
    public void FilterInstalled_FallsBackToSegoeUi_WhenNoneAreInstalled()
    {
        var filtered = TextStylePickerCatalog.FilterInstalled(Array.Empty<string>());
        Assert.Equal(new[] { "Segoe UI" }, filtered);
    }
}
