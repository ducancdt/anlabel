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

    [Theory]
    [InlineData(BarcodeApplicationProfile.General)]
    [InlineData(BarcodeApplicationProfile.Industrial)]
    public void NonGs1ProfileRejectsEmptyOrWhitespaceDataWithSymbologyDiagnostic(BarcodeApplicationProfile profile)
    {
        var nullErrors = BarcodeApplicationContract.ValidateData(profile, BarcodeSymbology.Code128, null);
        Assert.Contains(nullErrors, e => e.Contains("Code128") && e.Contains("empty or whitespace"));

        var emptyErrors = BarcodeApplicationContract.ValidateData(profile, BarcodeSymbology.QRCode, "");
        Assert.Contains(emptyErrors, e => e.Contains("QRCode") && e.Contains("empty or whitespace"));

        var whitespaceErrors = BarcodeApplicationContract.ValidateData(profile, BarcodeSymbology.Ean13, "   ");
        Assert.Contains(whitespaceErrors, e => e.Contains("Ean13") && e.Contains("empty or whitespace"));
    }

    [Theory]
    [InlineData(BarcodeApplicationProfile.General, "CODE-128_TEST!", true)]
    [InlineData(BarcodeApplicationProfile.General, "CODE128\u00A0NBSP", false)]
    [InlineData(BarcodeApplicationProfile.Industrial, "VALID-PART#123", true)]
    [InlineData(BarcodeApplicationProfile.Industrial, "INVALID\u0009TAB", false)]
    public void Code128AndCode93ValidatePrintableAsciiBoundary(BarcodeApplicationProfile profile, string payload, bool expectedValid)
    {
        var code128Errors = BarcodeApplicationContract.ValidateData(profile, BarcodeSymbology.Code128, payload);
        Assert.Equal(expectedValid, code128Errors.Count == 0);
        if (!expectedValid)
        {
            Assert.Contains(code128Errors, e => e.Contains("Code128") && e.Contains("unsupported characters"));
        }

        var code93Errors = BarcodeApplicationContract.ValidateData(profile, BarcodeSymbology.Code93, payload);
        Assert.Equal(expectedValid, code93Errors.Count == 0);
        if (!expectedValid)
        {
            Assert.Contains(code93Errors, e => e.Contains("Code93") && e.Contains("unsupported characters"));
        }
    }

    [Theory]
    [InlineData("CODE39-123. $/+%", true)]
    [InlineData("code39-lower-case", true)]
    [InlineData("CODE39@BAD", false)]
    [InlineData("CODE39_UNDERSCORE", false)]
    [InlineData("CODE39!EXCL", false)]
    public void Code39ValidatesStrictAlphabet(string payload, bool expectedValid)
    {
        var errors = BarcodeApplicationContract.ValidateData(BarcodeApplicationProfile.General, BarcodeSymbology.Code39, payload);
        Assert.Equal(expectedValid, errors.Count == 0);
        if (!expectedValid)
        {
            Assert.Contains(errors, e => e.Contains("Code39") && e.Contains("unsupported characters"));
        }
    }

    [Theory]
    [InlineData("400638133393", true, null)] // 12 digits: encoder generates check digit
    [InlineData("4006381333931", true, null)] // 13 digits: correct check digit
    [InlineData("4006381333932", false, "check digit")] // 13 digits: wrong check digit
    [InlineData("40063813339", false, "length must be 12 digits")] // 11 digits: invalid length
    [InlineData("40063813339315", false, "length must be 12 digits")] // 14 digits: invalid length
    [InlineData("40063813339A", false, "numeric digits only")] // Non-numeric
    public void Ean13ValidatesLengthAndCheckDigitOnlyWhenPresent(string payload, bool expectedValid, string? expectedDiagnosticPart)
    {
        var errors = BarcodeApplicationContract.ValidateData(BarcodeApplicationProfile.General, BarcodeSymbology.Ean13, payload);
        Assert.Equal(expectedValid, errors.Count == 0);
        if (!expectedValid)
        {
            Assert.Contains(errors, e => e.Contains("Ean13") && e.Contains(expectedDiagnosticPart!, StringComparison.OrdinalIgnoreCase));
        }
    }

    [Theory]
    [InlineData("9638507", true, null)] // 7 digits: encoder generates check digit
    [InlineData("96385074", true, null)] // 8 digits: correct check digit
    [InlineData("96385075", false, "check digit")] // 8 digits: wrong check digit
    [InlineData("963850", false, "length must be 7 digits")] // 6 digits: invalid length
    [InlineData("963850741", false, "length must be 7 digits")] // 9 digits: invalid length
    [InlineData("963850A", false, "numeric digits only")] // Non-numeric
    public void Ean8ValidatesLengthAndCheckDigitOnlyWhenPresent(string payload, bool expectedValid, string? expectedDiagnosticPart)
    {
        var errors = BarcodeApplicationContract.ValidateData(BarcodeApplicationProfile.Industrial, BarcodeSymbology.Ean8, payload);
        Assert.Equal(expectedValid, errors.Count == 0);
        if (!expectedValid)
        {
            Assert.Contains(errors, e => e.Contains("Ean8") && e.Contains(expectedDiagnosticPart!, StringComparison.OrdinalIgnoreCase));
        }
    }

    [Theory]
    [InlineData("03600029145", true, null)] // 11 digits: encoder generates check digit
    [InlineData("036000291452", true, null)] // 12 digits: correct check digit
    [InlineData("036000291453", false, "check digit")] // 12 digits: wrong check digit
    [InlineData("0360002914", false, "length must be 11 digits")] // 10 digits: invalid length
    [InlineData("0360002914520", false, "length must be 11 digits")] // 13 digits: invalid length
    [InlineData("0360002914A", false, "numeric digits only")] // Non-numeric
    public void UpcAValidatesLengthAndCheckDigitOnlyWhenPresent(string payload, bool expectedValid, string? expectedDiagnosticPart)
    {
        var errors = BarcodeApplicationContract.ValidateData(BarcodeApplicationProfile.General, BarcodeSymbology.UpcA, payload);
        Assert.Equal(expectedValid, errors.Count == 0);
        if (!expectedValid)
        {
            Assert.Contains(errors, e => e.Contains("UpcA") && e.Contains(expectedDiagnosticPart!, StringComparison.OrdinalIgnoreCase));
        }
    }

    [Theory]
    [InlineData("012345", true, null)]
    [InlineData("0123456", true, null)]
    [InlineData("01234567", true, null)]
    [InlineData("01234", false, "length must be 6, 7, or 8 digits")]
    [InlineData("012345678", false, "length must be 6, 7, or 8 digits")]
    [InlineData("01234A", false, "numeric digits only")]
    public void UpcEValidatesLengthAndNumericDigits(string payload, bool expectedValid, string? expectedDiagnosticPart)
    {
        var errors = BarcodeApplicationContract.ValidateData(BarcodeApplicationProfile.General, BarcodeSymbology.UpcE, payload);
        Assert.Equal(expectedValid, errors.Count == 0);
        if (!expectedValid)
        {
            Assert.Contains(errors, e => e.Contains("UpcE") && e.Contains(expectedDiagnosticPart!, StringComparison.OrdinalIgnoreCase));
        }
    }

    [Theory]
    [InlineData("1234", true, null)]
    [InlineData("12345678", true, null)]
    [InlineData("123", false, "even number of digits")]
    [InlineData("12345", false, "even number of digits")]
    [InlineData("123A", false, "numeric digits only")]
    public void ItfValidatesNumericAndEvenLength(string payload, bool expectedValid, string? expectedDiagnosticPart)
    {
        var errors = BarcodeApplicationContract.ValidateData(BarcodeApplicationProfile.Industrial, BarcodeSymbology.ITF, payload);
        Assert.Equal(expectedValid, errors.Count == 0);
        if (!expectedValid)
        {
            Assert.Contains(errors, e => e.Contains("ITF") && e.Contains(expectedDiagnosticPart!, StringComparison.OrdinalIgnoreCase));
        }
    }

    [Theory]
    [InlineData("A123456B", true)]
    [InlineData("012345-$:/.+", true)]
    [InlineData("123@456", false)]
    [InlineData("ABC?", false)]
    public void CodabarValidatesSupportedAlphabet(string payload, bool expectedValid)
    {
        var errors = BarcodeApplicationContract.ValidateData(BarcodeApplicationProfile.General, BarcodeSymbology.Codabar, payload);
        Assert.Equal(expectedValid, errors.Count == 0);
        if (!expectedValid)
        {
            Assert.Contains(errors, e => e.Contains("Codabar") && e.Contains("unsupported characters"));
        }
    }

    [Theory]
    [InlineData(BarcodeSymbology.MSI, "123456", true)]
    [InlineData(BarcodeSymbology.MSI, "12345A", false)]
    [InlineData(BarcodeSymbology.Plessey, "98765", true)]
    [InlineData(BarcodeSymbology.Plessey, "9876F", false)]
    public void MsiAndPlesseyValidateNumericDigits(BarcodeSymbology symbology, string payload, bool expectedValid)
    {
        var errors = BarcodeApplicationContract.ValidateData(BarcodeApplicationProfile.Industrial, symbology, payload);
        Assert.Equal(expectedValid, errors.Count == 0);
        if (!expectedValid)
        {
            Assert.Contains(errors, e => e.Contains(symbology.ToString()) && e.Contains("numeric digits only"));
        }
    }

    [Theory]
    [InlineData(BarcodeSymbology.QRCode)]
    [InlineData(BarcodeSymbology.DataMatrix)]
    [InlineData(BarcodeSymbology.Pdf417)]
    [InlineData(BarcodeSymbology.Aztec)]
    public void TwoDimensionalSymbologiesAcceptArbitraryPayloadWhenNotEmpty(BarcodeSymbology symbology)
    {
        var errors = BarcodeApplicationContract.ValidateData(BarcodeApplicationProfile.General, symbology, "UTF-8: Nhãn hàng Tiếng Việt 100%!");
        Assert.Empty(errors);
    }

    // DEFECT 2 — AI 20 (product variant, N2) is a GS1 pre-defined-length AI and
    // must NOT be followed by a group separator when another element string
    // follows it. Previously it was misclassified as SeparatorRequired.
    [Fact]
    public void Gs1ProductVariantAi20IsPredefinedLengthAndEmitsNoSeparator()
    {
        Assert.True(Gs1AiRegistry.TryGetDefinition("20", out var definition));
        Assert.Equal(Gs1AiBoundaryKind.PredefinedLength, definition.BoundaryKind);

        var valid = BarcodeApplicationContract.TryNormalizeGs1Data("(20)01(10)ABC", out var normalized, out var errors);

        Assert.True(valid);
        Assert.Empty(errors);
        Assert.Equal("200110ABC", normalized);
        Assert.DoesNotContain(BarcodeApplicationContract.GroupSeparator, normalized);
    }

    // DEFECT 3 — GS1 permits a day component of "00" (year/month only) on date
    // AIs. Strict DateTime.TryParseExact("yyMMdd") rejected DD=00; it must be
    // accepted while genuinely invalid calendar dates stay rejected.
    [Theory]
    [InlineData("(17)250600", true)]  // DD=00 permitted (month-only expiry)
    [InlineData("(17)250630", true)]  // real date
    [InlineData("(17)250000", false)] // MM=00 still invalid
    [InlineData("(17)251300", false)] // MM=13 invalid
    [InlineData("(17)250631", false)] // June has 30 days
    [InlineData("(17)250230", false)] // Feb 30 invalid
    [InlineData("(7003)2506000000", true)]  // DD=00 in date/time
    [InlineData("(7003)2506302359", true)]  // real date/time
    [InlineData("(7003)2506312359", false)] // June 31 invalid
    public void Gs1DateAisAcceptDayZeroButRejectInvalidCalendarDates(string source, bool expectedValid)
    {
        var errors = BarcodeApplicationContract.ValidateData(
            BarcodeApplicationProfile.Gs1,
            BarcodeSymbology.DataMatrix,
            source);

        Assert.Equal(expectedValid, errors.Count == 0);
    }
}
