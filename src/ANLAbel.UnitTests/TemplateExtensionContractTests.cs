using System.Text.Json;
using ANLAbel.Core.Models;
using ANLAbel.Core.Scene;
using Xunit;

namespace ANLAbel.UnitTests;

public sealed class TemplateExtensionContractTests
{
    [Fact]
    public void FingerprintIsIndependentOfObjectMemberOrder()
    {
        var first = Parse("{\"vendor\":{\"mode\":\"strict\",\"limit\":7},\"flags\":[true,false]}");
        var second = Parse("{\"flags\":[true,false],\"vendor\":{\"limit\":7,\"mode\":\"strict\"}}");

        Assert.Equal(
            TemplateExtensionContract.ComputeFingerprint(first),
            TemplateExtensionContract.ComputeFingerprint(second));
    }

    [Fact]
    public void ExtensionMetadataChangesDocumentIdentity()
    {
        var first = new LabelTemplate { Id = "extension-identity", Name = "Extension identity" };
        first.ExtensionData = Parse("{\"vendor\":{\"mode\":\"strict\"}}");
        var second = new LabelTemplate { Id = "extension-identity", Name = "Extension identity" };
        second.ExtensionData = Parse("{\"vendor\":{\"mode\":\"relaxed\"}}");

        var firstSnapshot = DocumentSnapshot.Capture(first);
        var secondSnapshot = DocumentSnapshot.Capture(second);

        Assert.NotEqual(firstSnapshot.ExtensionFingerprint, secondSnapshot.ExtensionFingerprint);
        Assert.NotEqual(firstSnapshot.DocumentHash, secondSnapshot.DocumentHash);
    }

    private static Dictionary<string, JsonElement> Parse(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement
            .EnumerateObject()
            .ToDictionary(property => property.Name, property => property.Value.Clone(), StringComparer.Ordinal);
    }
}
