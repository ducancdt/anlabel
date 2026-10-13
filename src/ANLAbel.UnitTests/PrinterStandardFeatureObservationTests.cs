using ANLAbel.Core.Printing;
using Xunit;

namespace ANLAbel.UnitTests;

public sealed class PrinterStandardFeatureObservationTests
{
    [Fact]
    public void AdvertisedValuesAreNormalizedWithoutInventingDefaults()
    {
        var observation = new PrinterStandardFeatureObservation(
            "Labels",
            new[] { " Labels ", "labels", "", "Plain" },
            "High",
            new[] { "High", "Normal", "high", " " });

        Assert.Equal(new[] { "Labels", "Plain" }, observation.AvailableMediaTypes);
        Assert.Equal(new[] { "High", "Normal" }, observation.AvailableOutputQualities);
        Assert.Equal("Labels", observation.EffectiveMediaType);
        Assert.Equal("High", observation.EffectiveOutputQuality);
        Assert.True(observation.IsEffectiveMediaTypeAdvertised);
        Assert.True(observation.IsEffectiveOutputQualityAdvertised);
    }

    [Fact]
    public void EmptyCapabilityListsStayEmpty()
    {
        var observation = new PrinterStandardFeatureObservation(
            string.Empty,
            Array.Empty<string>(),
            string.Empty,
            Array.Empty<string>());

        Assert.Empty(observation.AvailableMediaTypes);
        Assert.Empty(observation.AvailableOutputQualities);
        Assert.True(observation.IsEffectiveMediaTypeAdvertised);
        Assert.True(observation.IsEffectiveOutputQualityAdvertised);
    }

    [Fact]
    public void EffectiveValueWithoutCapabilityEvidenceIsExplicit()
    {
        var observation = new PrinterStandardFeatureObservation(
            "Plain",
            Array.Empty<string>(),
            "Normal",
            Array.Empty<string>());

        Assert.False(observation.IsEffectiveMediaTypeAdvertised);
        Assert.False(observation.IsEffectiveOutputQualityAdvertised);
    }
}
