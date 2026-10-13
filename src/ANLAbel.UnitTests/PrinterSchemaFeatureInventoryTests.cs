using System.Text;
using ANLAbel.Core.Printing;
using Xunit;

namespace ANLAbel.UnitTests;

public sealed class PrinterSchemaFeatureInventoryTests
{
    [Fact]
    public void InventorySeparatesStandardAndPrivateFeatureNamesWithoutValues()
    {
        const string xml = """
            <psf:PrintCapabilities
              xmlns:psf="http://schemas.microsoft.com/windows/2003/08/printing/printschemaframework"
              xmlns:psk="http://schemas.microsoft.com/windows/2003/08/printing/printschemakeywords"
              xmlns:vendor="urn:vendor:thermal">
              <psf:Feature name="psk:PageMediaSize">
                <psf:Option name="vendor:SecretOption">
                  <psf:Property name="vendor:Password"><psf:Value>do-not-retain</psf:Value></psf:Property>
                </psf:Option>
              </psf:Feature>
              <psf:Feature name="vendor:PrintDarkness" />
              <psf:Feature name="vendor:PrintDarkness" />
            </psf:PrintCapabilities>
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        var inventory = PrinterSchemaFeatureInventoryParser.Parse(stream);

        Assert.Equal(2, inventory.Features.Count);
        Assert.Equal(1, inventory.StandardFeatureCount);
        Assert.Equal(1, inventory.PrivateFeatureCount);
        Assert.Contains(inventory.Features, feature => feature.IsStandard && feature.Name == "PageMediaSize");
        Assert.Contains(inventory.Features, feature =>
            !feature.IsStandard && feature.NamespaceUri == "urn:vendor:thermal" && feature.Name == "PrintDarkness");
        Assert.DoesNotContain("do-not-retain", string.Join('|', inventory.Features));
        Assert.Empty(inventory.Diagnostic);
    }

    [Fact]
    public void DtdIsRejectedWithoutReturningRawXml()
    {
        const string xml = """
            <!DOCTYPE root [<!ENTITY leaked SYSTEM "file:///C:/secret.txt">]>
            <root>&leaked;</root>
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        var inventory = PrinterSchemaFeatureInventoryParser.Parse(stream);

        Assert.Empty(inventory.Features);
        Assert.Contains("could not be parsed", inventory.Diagnostic, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("secret.txt", inventory.Diagnostic, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void UnknownPrefixDoesNotBecomeStandardEvidence()
    {
        const string xml = """
            <psf:PrintCapabilities xmlns:psf="https://schemas.microsoft.com/windows/2003/08/printing/printschemaframework">
              <psf:Feature name="missing:PageMediaSize" />
            </psf:PrintCapabilities>
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        var feature = Assert.Single(PrinterSchemaFeatureInventoryParser.Parse(stream).Features);

        Assert.False(feature.IsStandard);
        Assert.Empty(feature.NamespaceUri);
    }
}
