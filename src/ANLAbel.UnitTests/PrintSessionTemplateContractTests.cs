using ANLAbel.Core.Enums;
using ANLAbel.Core.Models;
using ANLAbel.Core.Printing;
using Xunit;

namespace ANLAbel.UnitTests;

public sealed class PrintSessionTemplateContractTests
{
    [Fact]
    public void CaptureCreatesAnIndependentCompleteSessionSnapshot()
    {
        var authored = new LabelTemplate { Name = "Authored", WidthMm = 50, HeightMm = 30 };
        authored.PrinterProfile.PrinterName = "Datamax E-4203";
        authored.Objects.Add(new LabelObject { Type = ObjectType.Text, Text = "Protected authored text" });

        var session = PrintSessionTemplateContract.Capture(authored);

        Assert.NotSame(authored, session);
        Assert.NotSame(authored.PrinterProfile, session.PrinterProfile);
        Assert.NotSame(authored.Objects[0], session.Objects[0]);
        Assert.Equal("Protected authored text", session.Objects[0].Text);

        session.WidthMm = 72;
        session.PrinterProfile.PrinterName = "Session queue";
        session.Objects[0].Text = "Session value";
        Assert.Equal(50, authored.WidthMm);
        Assert.Equal("Datamax E-4203", authored.PrinterProfile.PrinterName);
        Assert.Equal("Protected authored text", authored.Objects[0].Text);
    }

    [Fact]
    public void ExplicitCommitCopiesPrintConfigurationButNotAuthoredContent()
    {
        var authored = new LabelTemplate { Id = "authored-id", Name = "Authored", WidthMm = 50, HeightMm = 30 };
        authored.Objects.Add(new LabelObject { Type = ObjectType.Text, Text = "Keep me" });
        var session = PrintSessionTemplateContract.Capture(authored);
        session.Id = "session-id";
        session.Name = "Session name";
        session.WidthMm = 72;
        session.HeightMm = 40;
        session.PrinterProfile.PrinterName = "Zebra ZT411";
        session.PrinterProfile.PaperName = "USER";
        session.PrinterProfile.OffsetXMm = 1.25;
        session.Objects[0].Text = "Do not commit me";

        PrintSessionTemplateContract.CommitPrintConfiguration(session, authored);

        Assert.Equal(72, authored.WidthMm);
        Assert.Equal(40, authored.HeightMm);
        Assert.Equal("Zebra ZT411", authored.PrinterProfile.PrinterName);
        Assert.Equal("USER", authored.PrinterProfile.PaperName);
        Assert.Equal(1.25, authored.PrinterProfile.OffsetXMm);
        Assert.Equal("authored-id", authored.Id);
        Assert.Equal("Authored", authored.Name);
        Assert.Equal("Keep me", authored.Objects[0].Text);
    }

    [Fact]
    public void CapturePreservesEnumPropertiesAsStringEnums()
    {
        var authored = new LabelTemplate { Name = "Authored", WidthMm = 100, HeightMm = 50, Orientation = LabelOrientation.Landscape };
        authored.PrinterProfile.PaperSizeSource = PaperSizeSource.DriverAutomatic;
        authored.PrinterProfile.MediaProvenance = PrinterMediaProvenance.DriverWpf;
        authored.PrinterProfile.SettingsSource = PrinterSettingsSource.Driver;
        authored.Objects.Add(new LabelObject { Type = ObjectType.QRCode, BarcodeSymbology = BarcodeSymbology.QRCode, Text = "123" });

        var session = PrintSessionTemplateContract.Capture(authored);

        Assert.Equal(LabelOrientation.Landscape, session.Orientation);
        Assert.Equal(PaperSizeSource.DriverAutomatic, session.PrinterProfile.PaperSizeSource);
        Assert.Equal(PrinterMediaProvenance.DriverWpf, session.PrinterProfile.MediaProvenance);
        Assert.Equal(PrinterSettingsSource.Driver, session.PrinterProfile.SettingsSource);
        Assert.Equal(BarcodeSymbology.QRCode, session.Objects[0].BarcodeSymbology);
    }
}
