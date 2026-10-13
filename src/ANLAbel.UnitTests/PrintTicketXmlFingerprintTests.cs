using System.Text;
using ANLAbel.Core.Printing;
using Xunit;

namespace ANLAbel.UnitTests;

public class PrintTicketXmlFingerprintTests
{
    private const string Ticket = """
        <psf:PrintTicket xmlns:psf="http://schemas.microsoft.com/windows/2003/08/printing/printschemaframework"
          xmlns:vendor="http://schemas.seagullscientific.com/print/seagull/driver/base">
          <psf:Feature name="vendor:JobTicketProperties">
            <psf:Option name="vendor:strSeagullDriver">
              <psf:ScoredProperty name="vendor:DrvVersion"><psf:Value>12.0.0</psf:Value></psf:ScoredProperty>
              <psf:ScoredProperty name="vendor:CreationTime"><psf:Value>CLOCK</psf:Value></psf:ScoredProperty>
            </psf:Option>
          </psf:Feature>
          <psf:Feature name="vendor:PrintSpeed"><psf:Option name="vendor:Speed"><psf:Value>4</psf:Value></psf:Option></psf:Feature>
        </psf:PrintTicket>
        """;

    private static string Hash(string xml)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        return PrintTicketXmlFingerprint.Compute(stream);
    }

    [Fact]
    public void RegeneratedCreationClockDoesNotInvalidatePreparedJob()
        => Assert.Equal(Hash(Ticket.Replace("CLOCK", "03&amp;colon;33 PM")), Hash(Ticket.Replace("CLOCK", "03&amp;colon;34 PM")));

    [Theory]
    [InlineData("12.0.0", "12.0.1")]
    [InlineData(">4<", ">6<")]
    public void DriverVersionAndPrivateSettingsStillInvalidateJob(string before, string after)
        => Assert.NotEqual(Hash(Ticket), Hash(Ticket.Replace(before, after)));

    [Theory]
    [InlineData("http://schemas.seagullscientific.com/print/seagull/driver/base", "urn:other-driver")]
    [InlineData("JobTicketProperties", "OtherFeature")]
    [InlineData("strSeagullDriver", "OtherOption")]
    [InlineData("CreationTime", "OtherSetting")]
    public void SimilarMetadataOutsideExactDriverPathIsNotIgnored(string before, string after)
    {
        var other = Ticket.Replace(before, after);
        Assert.NotEqual(Hash(other), Hash(other.Replace("CLOCK", "CHANGED")));
    }
}
