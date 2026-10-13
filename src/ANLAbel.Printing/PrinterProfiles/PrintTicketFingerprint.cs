using System.Printing;
using ANLAbel.Core.Printing;

namespace ANLAbel.Printing.PrinterProfiles;

internal static class PrintTicketFingerprint
{
    /// <summary>
    /// Returns a content fingerprint only when the driver ticket can be
    /// serialized. An empty result is explicit missing evidence.
    /// </summary>
    public static string Compute(PrintTicket? ticket)
    {
        if (ticket is null)
        {
            return string.Empty;
        }

        try
        {
            using var xml = ticket.GetXmlStream();
            return PrintTicketXmlFingerprint.Compute(xml);
        }
        catch
        {
            return string.Empty;
        }
    }
}
