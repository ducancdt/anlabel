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

    [Fact]
    public void EmptyOrNullExtensionsProduceEmptyFingerprint()
    {
        Assert.Equal(string.Empty, TemplateExtensionContract.ComputeFingerprint(null));
        Assert.Equal(
            string.Empty,
            TemplateExtensionContract.ComputeFingerprint(new Dictionary<string, JsonElement>()));
    }

    [Fact]
    public void FingerprintMatchesTheDeterministicCanonicalForm()
    {
        // Canonical form (keys and object members sorted ordinal, arrays kept in
        // order, each value kind rendered explicitly):
        //   a={x:"hi",y:1,};b=[true,false,null,];n=3.5;
        // The SHA-256 (upper-hex) of that UTF-8 string pins every separator,
        // ordering and value-kind literal in the builder.
        var extensions = Parse(
            "{\"a\":{\"y\":1,\"x\":\"hi\"},\"b\":[true,false,null],\"n\":3.5}");

        Assert.Equal(
            "4062BA33FD62188F495B8F9B2995EDECD556DFF58E64FBEA4508DE318FA8F436",
            TemplateExtensionContract.ComputeFingerprint(extensions));
    }

    [Fact]
    public void ArrayOrderAndBooleanValueChangeTheFingerprint()
    {
        var baseline = Parse("{\"a\":[true,false]}");
        var swappedBool = Parse("{\"a\":[false,true]}");

        Assert.NotEqual(
            TemplateExtensionContract.ComputeFingerprint(baseline),
            TemplateExtensionContract.ComputeFingerprint(swappedBool));
    }

    private static Dictionary<string, JsonElement> Parse(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement
            .EnumerateObject()
            .ToDictionary(property => property.Name, property => property.Value.Clone(), StringComparer.Ordinal);
    }
}
