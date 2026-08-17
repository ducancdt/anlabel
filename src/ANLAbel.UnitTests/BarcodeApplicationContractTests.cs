using ANLAbel.Core.Barcode;
using ANLAbel.Core.Enums;
using Xunit;

namespace ANLAbel.UnitTests;

public sealed class BarcodeApplicationContractTests
{
    [Fact]
    public void GeneralProfileKeepsLegacyQuietZoneButStillValidatesRenderedHri()
    {
        var errors = BarcodeApplicationContract.ValidateGeometry(
            BarcodeApplicationProfile.General,
            BarcodeSymbology.Code128,
            quietZoneModules: 2,
            showHri: true,
            hriFontSizePt: 4);

        Assert.Single(errors);
        Assert.Contains(errors, message => message.Contains("HRI font size", StringComparison.Ordinal));
    }

    [Fact]
    public void IndustrialProfileRequiresLinearQuietZoneAndValidHriSize()
    {
        var errors = BarcodeApplicationContract.ValidateGeometry(
            BarcodeApplicationProfile.Industrial,
            BarcodeSymbology.Code128,
            quietZoneModules: 4,
            showHri: true,
            hriFontSizePt: 4);

        Assert.Equal(2, errors.Count);
        Assert.Contains(errors, message => message.Contains("at least 10", StringComparison.Ordinal));
        Assert.Contains(errors, message => message.Contains("HRI font size", StringComparison.Ordinal));
    }

    [Fact]
    public void Gs1ParenthesizedDataNormalizesVariableFieldWithGroupSeparator()
    {
        const string source = "(10)LOT-42(17)250101";

        var valid = BarcodeApplicationContract.TryNormalizeGs1Data(source, out var normalized, out var errors);

        Assert.True(valid);
        Assert.Empty(errors);
        Assert.Equal($"10LOT-42{BarcodeApplicationContract.GroupSeparator}17250101", normalized);
    }

    [Fact]
    public void Gs1ChecksGtinCheckDigitAndDateShape()
    {
        const string validData = "(01)09506000134352(17)250101(10)ABC";
        var errors = BarcodeApplicationContract.ValidateData(
            BarcodeApplicationProfile.Gs1,
            BarcodeSymbology.DataMatrix,
            validData);

        Assert.Empty(errors);
    }

    [Fact]
    public void Gs1RejectsInvalidCheckDigitAndUnsupportedSymbology()
    {
        var dataErrors = BarcodeApplicationContract.ValidateData(
            BarcodeApplicationProfile.Gs1,
            BarcodeSymbology.Code128,
            "(01)09506000134353");
        var typeErrors = BarcodeApplicationContract.ValidateData(
            BarcodeApplicationProfile.Gs1,
            BarcodeSymbology.Code39,
            "(01)09506000134352");

        Assert.Contains(dataErrors, message => message.Contains("check digit", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(typeErrors, message => message.Contains("supports Code 128", StringComparison.Ordinal));
    }

    [Fact]
    public void Gs1QuietZonePolicyDiffersForMatrixAndLinearSymbols()
    {
        Assert.Equal(10, BarcodeApplicationContract.GetRequiredQuietZoneModules(BarcodeApplicationProfile.Gs1, BarcodeSymbology.Code128));
        Assert.Equal(4, BarcodeApplicationContract.GetRequiredQuietZoneModules(BarcodeApplicationProfile.Gs1, BarcodeSymbology.QRCode));
        Assert.Equal(1, BarcodeApplicationContract.GetRequiredQuietZoneModules(BarcodeApplicationProfile.Gs1, BarcodeSymbology.DataMatrix));
    }

    [Fact]
    public void Gs1IndustrialAiSubsetValidatesWeightGlnAndCompanyInternal()
    {
        // Valid GTIN check digit already covered; 3103 = net kg with 3 decimals.
        var ok = BarcodeApplicationContract.ValidateData(
            BarcodeApplicationProfile.Gs1,
            BarcodeSymbology.Code128,
            "(01)09506000134352(3103)000150(10)LOT1(91)PLANT-A");
        Assert.Empty(ok);

        var weightErrors = BarcodeApplicationContract.ValidateData(
            BarcodeApplicationProfile.Gs1,
            BarcodeSymbology.Code128,
            "(3103)15");
        Assert.Contains(weightErrors, message => message.Contains("6 numeric", StringComparison.OrdinalIgnoreCase));

        var glnErrors = BarcodeApplicationContract.ValidateData(
            BarcodeApplicationProfile.Gs1,
            BarcodeSymbology.DataMatrix,
            "(414)123");
        Assert.Contains(glnErrors, message => message.Contains("13 digits", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Gs1VariableIndustrialFieldsInsertGroupSeparators()
    {
        const string source = "(240)PART-42(10)LOT-9(17)250101";
        var valid = BarcodeApplicationContract.TryNormalizeGs1Data(source, out var normalized, out var errors);
        Assert.True(valid);
        Assert.Empty(errors);
        Assert.Equal(
            $"240PART-42{BarcodeApplicationContract.GroupSeparator}10LOT-9{BarcodeApplicationContract.GroupSeparator}17250101",
            normalized);
    }

    [Fact]
    public void Gs1OnlyPredefinedLengthFamiliesAvoidSeparators()
    {
        const string source = "(3103)001015(7003)2712312359(10)LOT-42";

        var valid = BarcodeApplicationContract.TryNormalizeGs1Data(source, out var normalized, out var errors);

        Assert.True(valid);
        Assert.Empty(errors);
        Assert.Equal($"310300101570032712312359{BarcodeApplicationContract.GroupSeparator}10LOT-42", normalized);
        Assert.Equal(1, normalized.Count(character => character == BarcodeApplicationContract.GroupSeparator));
    }

    [Fact]
    public void Gs1FixedCountryCodesStillNeedSeparatorsWhenNotPredefinedLength()
    {
        const string source = "(422)840(425)840124(10)LOT-42";

        var valid = BarcodeApplicationContract.TryNormalizeGs1Data(source, out var normalized, out var errors);

        Assert.True(valid);
        Assert.Empty(errors);
        Assert.Equal($"422840{BarcodeApplicationContract.GroupSeparator}425840124{BarcodeApplicationContract.GroupSeparator}10LOT-42", normalized);
        Assert.Equal(2, normalized.Count(character => character == BarcodeApplicationContract.GroupSeparator));
    }

    [Theory]
    [InlineData("(3302)001015", true)]
    [InlineData("(3601)000007", true)]
    [InlineData("(3302)00101A", false)]
    [InlineData("(7003)2712312460", false)]
    [InlineData("(7003)271231235", false)]
    [InlineData("(422)84A", false)]
    [InlineData("(425)84012", false)]
    public void Gs1MeasureAndExpirationTimeValidateFixedFieldShapes(string source, bool isValid)
    {
        var errors = BarcodeApplicationContract.ValidateData(
            BarcodeApplicationProfile.Gs1,
            BarcodeSymbology.DataMatrix,
            source);

        Assert.Equal(isValid, errors.Count == 0);
    }

    [Fact]
    public void Gs1RejectsIdentifiersOutsideTheVersionedRegistryInsteadOfGuessingFnc1()
    {
        var valid = BarcodeApplicationContract.TryNormalizeGs1Data(
            "(23)UNVERSIONED(17)250101",
            out _,
            out var errors);

        Assert.False(valid);
        Assert.Contains(errors, error => error.Contains(BarcodeApplicationContract.Gs1RegistryVersion, StringComparison.Ordinal));
    }

    [Fact]
    public void Gs1RegistryProvidesTheSharedBoundaryDecisionForAiFamilies()
    {
        Assert.True(Gs1AiRegistry.TryGetDefinition("3103", out var measure));
        Assert.Equal(Gs1AiBoundaryKind.PredefinedLength, measure.BoundaryKind);
        Assert.True(Gs1AiRegistry.TryGetDefinition("91", out var internalAi));
        Assert.Equal(Gs1AiBoundaryKind.SeparatorRequired, internalAi.BoundaryKind);
        Assert.False(Gs1AiRegistry.TryGetDefinition("23", out _));
    }

    [Fact]
    public void OfficialGs1JsonSnapshotUsesThePublishedSeparatorFlagAndRetainsProvenance()
    {
        const string registry = """
            { "applicationIdentifiers": [
              { "owl:versionInfo": "1.2", "dc:lastModified": { "@value": "2026-01-26" } },
              { "applicationIdentifier": "01", "separatorRequired": false, "regex": "(\\d{14})" },
              { "applicationIdentifier": "422", "separatorRequired": true, "regex": "(\\d{3})" }
            ] }
            """;

        var valid = Gs1OfficialRegistrySnapshot.TryParse(registry, out var snapshot, out var errors);

        Assert.True(valid);
        Assert.Empty(errors);
        Assert.Equal("1.2", snapshot!.SourceVersion);
        Assert.Equal("2026-01-26", snapshot.SourceLastModified);
        Assert.Equal(Gs1AiBoundaryKind.PredefinedLength, snapshot.Definitions["01"].BoundaryKind);
        Assert.Equal(Gs1AiBoundaryKind.SeparatorRequired, snapshot.Definitions["422"].BoundaryKind);
        Assert.NotNull(snapshot.Definitions["422"].ValuePattern);
        Assert.Equal(64, snapshot.ContentSha256.Length);
    }

    [Fact]
    public void BundledOfficialGs1RegistryIsAvailableOfflineForAdditionalAiCoverage()
    {
        var snapshot = Gs1OfficialRegistryBundle.Load();

        Assert.True(snapshot.Definitions.Count > 500);
        Assert.Equal("1.2", snapshot.SourceVersion);
        Assert.True(Gs1AiRegistry.TryGetDefinition("253", out var definition));
        Assert.Equal(Gs1AiBoundaryKind.SeparatorRequired, definition.BoundaryKind);
    }

    [Fact]
    public void OfficialRegistryFallbackNormalizesAnAiOutsideTheCuratedSubset()
    {
        var valid = BarcodeApplicationContract.TryNormalizeGs1Data(
            "(253)0000000000000DOC-7(10)LOT-1",
            out var normalized,
            out var errors);

        Assert.True(valid);
        Assert.Empty(errors);
        Assert.Equal($"2530000000000000DOC-7{BarcodeApplicationContract.GroupSeparator}10LOT-1", normalized);
    }

    [Fact]
    public void Gs1ExtendedLogisticsAndAssetAisValidateCorrectly()
    {
        // AI (20) variant, AI (250) sec serial, AI (3902) amount, AI (8004) GIAI, AI (423) origin country
        const string validPayload = "(20)01(250)SN-9988(3902)1250(423)840(8004)ASSET-4455";
        var errors = BarcodeApplicationContract.ValidateData(
            BarcodeApplicationProfile.Gs1,
            BarcodeSymbology.DataMatrix,
            validPayload);

        Assert.Empty(errors);
    }

    [Fact]
    public void Gs1ExtendedAisFailClosedOnMalformedStructure()
    {
        // AI 20 requires exactly 2 digits, AI 7001 requires 13 digits
        var invalidVariantErrors = BarcodeApplicationContract.ValidateData(
            BarcodeApplicationProfile.Gs1,
            BarcodeSymbology.DataMatrix,
            "(20)123");
        Assert.Contains(invalidVariantErrors, e => e.Contains("GS1 AI 20 (product variant) requires exactly 2 numeric digits."));

        var invalidNsnErrors = BarcodeApplicationContract.ValidateData(
            BarcodeApplicationProfile.Gs1,
            BarcodeSymbology.DataMatrix,
            "(7001)12345");
        Assert.Contains(invalidNsnErrors, e => e.Contains("GS1 AI 7001 (NATO Stock Number) requires exactly 13 numeric digits."));

        var invalidPriceErrors = BarcodeApplicationContract.ValidateData(
            BarcodeApplicationProfile.Gs1,
            BarcodeSymbology.DataMatrix,
            "(3912)US"); // Requires 3-digit ISO currency + amount digits
        Assert.Contains(invalidPriceErrors, e => e.Contains("requires 3 numeric ISO currency digits followed by 1–15 numeric amount digits."));
    }
}
