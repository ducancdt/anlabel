using System.Xml;
using System.Xml.Linq;

namespace ANLAbel.Core.Printing;

public sealed record PrinterSchemaFeature(
    string NamespaceUri,
    string Name,
    bool IsStandard);

public sealed record PrinterSchemaFeatureInventory(
    IReadOnlyList<PrinterSchemaFeature> Features,
    bool WasTruncated,
    string Diagnostic)
{
    public int StandardFeatureCount => Features.Count(feature => feature.IsStandard);
    public int PrivateFeatureCount => Features.Count(feature => !feature.IsStandard);
}

/// <summary>
/// Reads feature identities from PrintCapabilities XML without retaining
/// options, values or the raw driver document.
/// </summary>
public static class PrinterSchemaFeatureInventoryParser
{
    private const int MaximumFeatures = 512;
    private const long MaximumXmlCharacters = 2 * 1024 * 1024;
    private const string FrameworkHttp = "http://schemas.microsoft.com/windows/2003/08/printing/printschemaframework";
    private const string FrameworkHttps = "https://schemas.microsoft.com/windows/2003/08/printing/printschemaframework";
    private const string KeywordsHttp = "http://schemas.microsoft.com/windows/2003/08/printing/printschemakeywords";
    private const string KeywordsHttps = "https://schemas.microsoft.com/windows/2003/08/printing/printschemakeywords";

    public static PrinterSchemaFeatureInventory Parse(Stream xml)
    {
        ArgumentNullException.ThrowIfNull(xml);

        try
        {
            using var reader = XmlReader.Create(xml, new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
                MaxCharactersInDocument = MaximumXmlCharacters,
                IgnoreComments = true,
                IgnoreProcessingInstructions = true
            });
            var document = XDocument.Load(reader, LoadOptions.None);
            var parsed = document
                .Descendants()
                .Where(element =>
                    element.Name.LocalName == "Feature" &&
                    IsFrameworkNamespace(element.Name.NamespaceName))
                .Select(ParseFeature)
                .Where(feature => feature is not null)
                .Cast<PrinterSchemaFeature>()
                .DistinctBy(feature => (feature.NamespaceUri, feature.Name))
                .OrderBy(feature => feature.IsStandard ? 0 : 1)
                .ThenBy(feature => feature.NamespaceUri, StringComparer.Ordinal)
                .ThenBy(feature => feature.Name, StringComparer.Ordinal)
                .ToArray();

            var wasTruncated = parsed.Length > MaximumFeatures;
            var features = parsed.Take(MaximumFeatures).ToArray();
            return new(
                features,
                wasTruncated,
                wasTruncated
                    ? $"Print Schema feature inventory was truncated at {MaximumFeatures} identities."
                    : string.Empty);
        }
        catch (Exception ex) when (ex is XmlException or InvalidOperationException)
        {
            return new(
                Array.Empty<PrinterSchemaFeature>(),
                WasTruncated: false,
                $"Print Schema feature inventory could not be parsed: {ex.Message}");
        }
    }

    private static PrinterSchemaFeature? ParseFeature(XElement element)
    {
        var rawName = element.Attribute("name")?.Value.Trim();
        if (string.IsNullOrWhiteSpace(rawName))
        {
            return null;
        }

        var separator = rawName.IndexOf(':');
        string namespaceUri;
        string localName;
        if (separator > 0 && separator < rawName.Length - 1)
        {
            var prefix = rawName[..separator];
            namespaceUri = element.GetNamespaceOfPrefix(prefix)?.NamespaceName ?? string.Empty;
            localName = rawName[(separator + 1)..];
        }
        else
        {
            namespaceUri = string.Empty;
            localName = rawName;
        }

        if (string.IsNullOrWhiteSpace(localName))
        {
            return null;
        }

        return new(
            namespaceUri,
            localName,
            namespaceUri is KeywordsHttp or KeywordsHttps);
    }

    private static bool IsFrameworkNamespace(string namespaceUri) =>
        namespaceUri is FrameworkHttp or FrameworkHttps;
}
