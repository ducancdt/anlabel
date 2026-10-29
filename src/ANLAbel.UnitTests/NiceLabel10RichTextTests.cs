using System.Text;
using ANLAbel.Project.NiceLabel10;
using Xunit;

namespace ANLAbel.UnitTests;

/// <summary>Plain-text recovery of NiceLabel 10 rich-text contents.</summary>
public sealed class NiceLabel10RichTextTests
{
    private const string SampleRtf =
        @"{\rtf1\ansi\ansicpg1250\deff0{\fonttbl{\f0\fnil\fcharset238 Arial;}}{\colortbl ;\red0\green0\blue0;}" +
        @"{\*\generator Riched20 10.0;}\viewkind4\uc1\pard\b Nutrition\b0  Facts\par Energy: 120\~kcal\line Fibre \'e8 \" + @"u8211?3 g\tab End\par}";

    [Fact]
    public void RawRtf_KeepsTextAndBreaksDropsTablesAndFormatting()
    {
        var text = NiceLabel10RichText.ToPlainText(SampleRtf);

        Assert.Equal("Nutrition Facts\nEnergy: 120 kcal\nFibre č –3 g\tEnd", text);
    }

    [Fact]
    public void Base64Rtf_IsDecodedFirst()
    {
        var stored = Convert.ToBase64String(Encoding.UTF8.GetBytes(SampleRtf));

        Assert.StartsWith("Nutrition Facts", NiceLabel10RichText.ToPlainText(stored));
    }

    [Fact]
    public void Base64PlainText_IsReturnedAsText()
    {
        var stored = Convert.ToBase64String(Encoding.UTF8.GetBytes("ACME IDEAL AUTO PARTS\r\n1 ROADRUNNER WAY"));

        Assert.Equal("ACME IDEAL AUTO PARTS\r\n1 ROADRUNNER WAY", NiceLabel10RichText.ToPlainText(stored));
    }

    [Fact]
    public void PlainText_IsUnchanged()
    {
        Assert.Equal("Just text", NiceLabel10RichText.ToPlainText("Just text"));
        Assert.Equal(string.Empty, NiceLabel10RichText.ToPlainText(null));
    }

    [Fact]
    public void RtfTable_RowsBecomeLinesAndCellsTabs()
    {
        const string table = @"{\rtf1\ansi\deff0\trowd\cellx1000\cellx2000\pard\intbl Calories\cell 250\cell\row\trowd\pard\intbl Fat\cell 8 g\cell\row}";

        // The trailing cell break is trimmed with the other edge whitespace.
        Assert.Equal("Calories\t250\t\nFat\t8 g", NiceLabel10RichText.ToPlainText(table));
    }
}
