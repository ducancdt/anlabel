using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;

namespace ANLAbel.Core.Printing;

/// <summary>Fingerprints driver settings without Seagull's ticket creation clock.</summary>
public static class PrintTicketXmlFingerprint
{
    private static readonly XNamespace Framework = "http://schemas.microsoft.com/windows/2003/08/printing/printschemaframework";
    private static readonly XNamespace Seagull = "http://schemas.seagullscientific.com/print/seagull/driver/base";

    public static string Compute(Stream ticketXml)
    {
        var document = XDocument.Load(ticketXml, LoadOptions.PreserveWhitespace);
        // The driver regenerates this descriptive timestamp on each queue read
        // and merge. Keep every setting and unknown extension in the identity.
        if (document.Root?.Name == Framework + "PrintTicket")
        {
            foreach (var feature in document.Root.Elements(Framework + "Feature").Where(e => Named(e, "JobTicketProperties")))
            foreach (var option in feature.Elements(Framework + "Option").Where(e => Named(e, "strSeagullDriver")))
            foreach (var property in option.Elements(Framework + "ScoredProperty").Where(e => Named(e, "CreationTime")))
            foreach (var value in property.Elements(Framework + "Value").Where(e => !e.HasElements))
                value.Value = string.Empty;
        }

        return Convert.ToHexString(SHA256.HashData(
            Encoding.UTF8.GetBytes(document.ToString(SaveOptions.DisableFormatting)))).ToLowerInvariant();
    }

    private static bool Named(XElement element, string localName)
    {
        var name = (string?)element.Attribute("name");
        if (name is null) return false;
        var separator = name.IndexOf(':');
        return separator > 0
            && name[(separator + 1)..] == localName
            && element.GetNamespaceOfPrefix(name[..separator]) == Seagull;
    }
}
