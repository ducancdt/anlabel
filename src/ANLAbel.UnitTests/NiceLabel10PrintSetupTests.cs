using System.Text;
using ANLAbel.Core.Enums;
using ANLAbel.Project.NiceLabel10;
using Xunit;

namespace ANLAbel.UnitTests;

/// <summary>
/// NiceLabel 10 printer/media import (docs/NICELABEL_10_REVERSE_ENGINEERING.md §10).
/// </summary>
public sealed class NiceLabel10PrintSetupTests
{
    private static string DevMode(int dpi, short paperSize = 256, bool withQuality = true)
    {
        // Minimal DEVMODEW: device name, version fields, dmFields and the union.
        var buffer = new byte[220];
        Encoding.Unicode.GetBytes("ZEBRA TEST").CopyTo(buffer, 0);
        BitConverter.GetBytes((ushort)0x0401).CopyTo(buffer, 64);
        BitConverter.GetBytes((ushort)220).CopyTo(buffer, 68);
        var fields = 0x1u | 0x2u | (withQuality ? 0x400u : 0u);
        BitConverter.GetBytes(fields).CopyTo(buffer, 72);
        BitConverter.GetBytes((short)1).CopyTo(buffer, 76);
        BitConverter.GetBytes(paperSize).CopyTo(buffer, 78);
        BitConverter.GetBytes((short)dpi).CopyTo(buffer, 90);
        return Convert.ToBase64String(buffer);
    }

    private static string Format(
        string media,
        string layout,
        string printer,
        string scenarioExtra = "",
        string labelSize = "<ElementWidth>100000</ElementWidth><ElementHeight>70000</ElementHeight>") => $"""
        <?xml version="1.0" encoding="utf-8"?>
        <EuroPlus.NiceLabel Type="Format">
          <Name>Print</Name>
          {media}
          <PageLayout Type="{layout}">{labelSize}</PageLayout>
          <PrintScenarioCollection>
            <PrintScenarioCollection Type="PrintScenario">
              <Printer Type="Printer">{printer}</Printer>
              {scenarioExtra}
            </PrintScenarioCollection>
          </PrintScenarioCollection>
          <DocumentItems />
        </EuroPlus.NiceLabel>
        """;

    private static (ANLAbel.Core.Models.LabelTemplate Template, NiceLabelPrinterSetupResult Result) Import(string formatXml)
    {
        var document = NiceLabel10NlblReader.Parse(new Dictionary<string, byte[]>
        {
            ["Formats/Print"] = Encoding.UTF8.GetBytes(formatXml)
        });
        var template = NiceLabel10NlblImporter.CreateTemplate(document, "print.nlbl", out var result);
        return (template, result);
    }

    [Theory]
    [InlineData(203, true, 203)]
    [InlineData(300, true, 300)]
    [InlineData(600, false, 0)]
    [InlineData(-4, true, 0)]
    public void DevModeResolution_ReadsPrintQualityOnlyWhenFlagged(int stored, bool flagged, int expected)
    {
        var bytes = Convert.FromBase64String(DevMode(stored, withQuality: flagged));
        Assert.Equal(expected, NiceLabel10NlblReader.DevModeResolution(bytes));
    }

    [Fact]
    public void LabelStockWithLabelSettings_AppliesManualStockDpiAndRotation()
    {
        var (template, result) = Import(Format(
            "<Media Type=\"FormatMedia\"><PaperFormat>256</PaperFormat><Width>100000</Width><Height>70000</Height><PaperDetails Type=\"PaperDetails\"><Id>256</Id><Name>Custom</Name></PaperDetails></Media>",
            "SingleFormatLayout",
            $"<Name>ZEBRA ZT420R-203dpi ZPL</Name><DriverName>ZEBRA ZT420R-203dpi ZPL</DriverName><DevModeBuffer>{DevMode(203)}</DevModeBuffer>",
            "<PrintRotation>2</PrintRotation>"));

        var profile = template.PrinterProfile;
        Assert.Equal(NiceLabelMediaSetup.LabelStock, result.Setup);
        Assert.Equal(PrinterSettingsSource.Label, profile.SettingsSource);
        Assert.Equal(PaperSizeSource.Manual, profile.PaperSizeSource);
        Assert.Equal((100.0, 70.0), (profile.PhysicalWidthMm, profile.PhysicalHeightMm));
        Assert.Equal(PrinterMediaProvenance.UserCustom, profile.MediaProvenance);
        Assert.Equal(203, profile.Dpi);
        Assert.Equal(203, template.Dpi);
        Assert.True(profile.Rotated180);
        // The authoring queue is reported, never selected.
        Assert.Equal(string.Empty, profile.PrinterName);
        Assert.Equal("ZEBRA ZT420R-203dpi ZPL", result.AuthoredPrinterName);
    }

    [Fact]
    public void DriverSettings_KeepDriverAutomaticPaper()
    {
        var (template, result) = Import(Format(
            "<Media Type=\"FormatMedia\"><PaperFormat>256</PaperFormat><Width>100000</Width><Height>70000</Height></Media>",
            "SingleFormatLayout",
            $"<UseDefaultPrinterSettings>True</UseDefaultPrinterSettings><Name>Label Printer</Name><DevModeBuffer>{DevMode(203)}</DevModeBuffer>",
            "<UseDefaultPrinter>True</UseDefaultPrinter>"));

        Assert.Equal(NiceLabelMediaSetup.DriverSettings, result.Setup);
        Assert.Equal(PrinterSettingsSource.Driver, template.PrinterProfile.SettingsSource);
        Assert.Equal(PaperSizeSource.DriverAutomatic, template.PrinterProfile.PaperSizeSource);
        Assert.Equal(0, template.PrinterProfile.PhysicalWidthMm);
        Assert.Null(result.AuthoredPrinterName);
    }

    [Fact]
    public void OfficeSheet_AppliesNoPhysicalMedia()
    {
        var (template, result) = Import(Format(
            "<Media Type=\"FormatMedia\"><PaperFormat>9</PaperFormat><Width>210000</Width><Height>297000</Height><Type>1</Type></Media>",
            "SingleFormatLayout",
            $"<Name>Microsoft XPS Document Writer</Name><DevModeBuffer>{DevMode(600, 9)}</DevModeBuffer>",
            labelSize: "<ElementWidth>152400</ElementWidth><ElementHeight>101600</ElementHeight><HorizontalOffset>12700</HorizontalOffset><VerticalOffset>12700</VerticalOffset>"));

        Assert.Equal(NiceLabelMediaSetup.OfficeSheet, result.Setup);
        Assert.Equal((152.4, 101.6), (template.WidthMm, template.HeightMm));
        Assert.Equal(0, template.PrinterProfile.PhysicalWidthMm);
        Assert.Equal(0, template.PrinterProfile.OffsetXMm);
        Assert.Equal(PaperSizeSource.DriverAutomatic, template.PrinterProfile.PaperSizeSource);
    }

    [Fact]
    public void SeveralLabelsPerPage_AppliesNoPhysicalMedia()
    {
        var (template, result) = Import(Format(
            "<Media Type=\"FormatMedia\"><PaperFormat>256</PaperFormat><Width>108000</Width><Height>297000</Height></Media>",
            "WebFormatLayout",
            $"<Name>Label Printer</Name><DevModeBuffer>{DevMode(203)}</DevModeBuffer>",
            labelSize: "<ElementWidth>68580</ElementWidth><ElementHeight>25400</ElementHeight><HorizontalCount>1</HorizontalCount><VerticalCount>10</VerticalCount>"));

        Assert.Equal(NiceLabelMediaSetup.MultipleLabelsPerPage, result.Setup);
        Assert.Equal((1, 10), (result.LabelsAcross, result.LabelsDown));
        Assert.Equal(0, template.PrinterProfile.PhysicalWidthMm);
    }
}
