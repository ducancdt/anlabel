using ANLAbel.Core.Barcode;
using ANLAbel.Core.Enums;
using ANLAbel.Core.Models;
using Xunit;

namespace ANLAbel.UnitTests;

public sealed class CustomCheckDigitAlgorithmTests
{
    private static LabelObject Barcode(
        BarcodeSymbology symbology,
        BarcodeCheckDigitPolicy policy,
        int modulus,
        string weights,
        bool fromRight = true,
        bool sumDigits = false,
        bool complement = true,
        bool keepAll = false) => new()
    {
        Type = ObjectType.BarcodeCode128,
        BarcodeSymbology = symbology,
        BarcodeCheckDigitPolicy = policy,
        CheckDigitModulus = modulus,
        CheckDigitWeights = weights,
        CheckDigitWeightsFromRight = fromRight,
        CheckDigitSumDigitsOfProducts = sumDigits,
        CheckDigitComplement = complement,
        CheckDigitKeepAllDigits = keepAll
    };

    [Theory]
    // GS1 mod 10 (EAN-13 body 629104150021 -> 3)
    [InlineData(10, "3,1", true, false, true, false, "629104150021", "3")]
    // Luhn (7992739871 -> 3)
    [InlineData(10, "2,1", true, true, true, false, "7992739871", "3")]
    // Plain remainder, weights from the left: (1*1 + 2*2 + 3*3) mod 7 = 0
    [InlineData(7, "1,2,3", false, false, false, false, "123", "0")]
    // Mod 11 kept in full: 5*2 = 10, 11 - 10 = 1; 9*2 = 18 % 11 = 7 -> 4
    [InlineData(11, "2", true, false, true, true, "5", "1")]
    [InlineData(11, "1", true, false, false, true, "10", "1")]
    public void ComputeMatchesPublishedVectors(
        int modulus, string weights, bool fromRight, bool sumDigits, bool complement, bool keepAll, string body, string expected)
    {
        var item = Barcode(BarcodeSymbology.Code128, BarcodeCheckDigitPolicy.Generate, modulus, weights, fromRight, sumDigits, complement, keepAll);
        var algorithm = CustomCheckDigitAlgorithm.FromObject(item);

        Assert.NotNull(algorithm);
        Assert.Equal(expected, algorithm!.Compute(body));
    }

    [Fact]
    public void KeepAllDigitsWritesTwoCharactersForTenOrMore()
    {
        // 1*1 = 1 -> complement 11 - 1 = 10.
        var item = Barcode(BarcodeSymbology.Code128, BarcodeCheckDigitPolicy.Generate, 11, "1", keepAll: true);

        var encoded = BarcodeCheckDigitContract.ToEncodedPayload(item, "1");

        Assert.Equal("110", encoded);
        item.BarcodeCheckDigitPolicy = BarcodeCheckDigitPolicy.Verify;
        Assert.Empty(BarcodeCheckDigitContract.Validate(item, BarcodeSymbology.Code128, encoded));
        item.BarcodeHriShowCheckDigit = false;
        Assert.Equal("1", BarcodeCheckDigitContract.FormatHriText(item, BarcodeSymbology.Code128, encoded));
    }

    [Fact]
    public void Code128GeneratesAndVerifiesOnlyWithACustomRule()
    {
        var custom = Barcode(BarcodeSymbology.Code128, BarcodeCheckDigitPolicy.Generate, 10, "3,1");
        Assert.Equal("6291041500213", BarcodeCheckDigitContract.ToEncodedPayload(custom, "629104150021"));

        var standard = Barcode(BarcodeSymbology.Code128, BarcodeCheckDigitPolicy.Generate, 0, "3,1");
        Assert.Equal("629104150021", BarcodeCheckDigitContract.ToEncodedPayload(standard, "629104150021"));

        custom.BarcodeCheckDigitPolicy = BarcodeCheckDigitPolicy.Verify;
        Assert.Empty(BarcodeCheckDigitContract.Validate(custom, BarcodeSymbology.Code128, "6291041500213"));
        Assert.NotEmpty(BarcodeCheckDigitContract.Validate(custom, BarcodeSymbology.Code128, "6291041500214"));
    }

    [Fact]
    public void CustomRuleReplacesTheItfStandardRule()
    {
        // Luhn on ITF: 7992739871 + 3 = 11 digits, odd -> needs one more data digit.
        var item = Barcode(BarcodeSymbology.ITF, BarcodeCheckDigitPolicy.Generate, 10, "2,1", sumDigits: true);

        Assert.Contains(BarcodeCheckDigitContract.Validate(item, BarcodeSymbology.ITF, "7992739871"),
            message => message.Contains("ITF needs an even number of digits"));
        Assert.Empty(BarcodeCheckDigitContract.Validate(item, BarcodeSymbology.ITF, "799273987"));
        var encoded = BarcodeCheckDigitContract.ToEncodedPayload(item, "799273987");
        Assert.Equal(10, encoded.Length);
        Assert.NotEqual(BarcodeCheckDigitContract.ComputeItfCheckDigit("799273987"), encoded[^1]);
    }

    [Theory]
    [InlineData(1, "3,1")]
    [InlineData(10, "")]
    [InlineData(10, "0,0")]
    [InlineData(10, "3,x")]
    [InlineData(10, "100")]
    public void InvalidDefinitionsAreReportedInPlainLanguage(int modulus, string weights)
    {
        var item = Barcode(BarcodeSymbology.Code128, BarcodeCheckDigitPolicy.Generate, modulus, weights);

        Assert.NotEmpty(BarcodeCheckDigitContract.Validate(item, BarcodeSymbology.Code128, "123"));
        Assert.Null(CustomCheckDigitAlgorithm.FromObject(item));
        Assert.Equal("123", BarcodeCheckDigitContract.ToEncodedPayload(item, "123"));

        // A broken custom rule must not silently fall back to the ITF standard rule.
        item.BarcodeSymbology = BarcodeSymbology.ITF;
        Assert.Equal("123", BarcodeCheckDigitContract.ToEncodedPayload(item, "123"));
    }

    [Fact]
    public void NonDigitDataCannotTakeACustomCheckDigit()
    {
        var item = Barcode(BarcodeSymbology.Code39, BarcodeCheckDigitPolicy.Generate, 10, "3,1");

        Assert.NotEmpty(BarcodeCheckDigitContract.Validate(item, BarcodeSymbology.Code39, "AB12"));
        Assert.Equal("AB12", BarcodeCheckDigitContract.ToEncodedPayload(item, "AB12"));
    }

    [Theory]
    // ISBN-10: 0-306-40615-2 and 0-8044-2957-X
    [InlineData(CustomCheckDigitPresets.Mod11X, "030640615", "2")]
    [InlineData(CustomCheckDigitPresets.Mod11X, "080442957", "X")]
    // UPU S10 serials: RR 12345678 5 VN, remainder 0 -> 5, remainder 1 -> 10 -> 0
    [InlineData(CustomCheckDigitPresets.UpuS10, "12345678", "5")]
    [InlineData(CustomCheckDigitPresets.UpuS10, "10000002", "5")]
    [InlineData(CustomCheckDigitPresets.UpuS10, "10000014", "0")]
    [InlineData(CustomCheckDigitPresets.Gs1Mod10, "629104150021", "3")]
    [InlineData(CustomCheckDigitPresets.Luhn, "7992739871", "3")]
    public void PresetsReproducePublishedCheckDigits(string preset, string body, string expected)
    {
        var item = new LabelObject { Type = ObjectType.BarcodeCode128, BarcodeSymbology = BarcodeSymbology.Code128 };

        Assert.True(CustomCheckDigitPresets.Apply(item, preset));
        item.BarcodeCheckDigitPolicy = BarcodeCheckDigitPolicy.Generate;

        Assert.Empty(CustomCheckDigitAlgorithm.ValidateDefinition(item));
        Assert.Equal(body + expected, BarcodeCheckDigitContract.ToEncodedPayload(item, body));
        item.BarcodeCheckDigitPolicy = BarcodeCheckDigitPolicy.Verify;
        Assert.Empty(BarcodeCheckDigitContract.Validate(item, BarcodeSymbology.Code128, body + expected));
    }

    [Theory]
    [InlineData("10")]
    [InlineData("10=XYZ")]
    [InlineData("100=1")]
    [InlineData("10=X;10=0")]
    [InlineData("10=#")]
    public void InvalidValueMapsAreExplained(string map)
    {
        var item = Barcode(BarcodeSymbology.Code128, BarcodeCheckDigitPolicy.Generate, 11, "2,3", keepAll: false);
        item.CheckDigitValueMap = map;

        Assert.Contains(BarcodeCheckDigitContract.Validate(item, BarcodeSymbology.Code128, "123"),
            message => message.StartsWith("Custom check digit value map ", StringComparison.Ordinal));
        Assert.Equal("123", BarcodeCheckDigitContract.ToEncodedPayload(item, "123"));
    }

    [Fact]
    public void ALetterCheckDigitCannotBeEncodedInItf()
    {
        var item = new LabelObject { Type = ObjectType.BarcodeCode128, BarcodeSymbology = BarcodeSymbology.ITF };
        CustomCheckDigitPresets.Apply(item, CustomCheckDigitPresets.Mod11X);
        item.BarcodeCheckDigitPolicy = BarcodeCheckDigitPolicy.Generate;

        var encoded = BarcodeCheckDigitContract.ToEncodedPayload(item, "080442957");

        Assert.Equal("080442957X", encoded);
        Assert.NotEmpty(BarcodeApplicationContract.ValidateData(BarcodeApplicationProfile.General, BarcodeSymbology.ITF, encoded));
    }

    [Fact]
    public void ClonerAndSnapshotCarryTheCustomRule()
    {
        var item = Barcode(BarcodeSymbology.Code128, BarcodeCheckDigitPolicy.Generate, 11, "7,6,5", false, true, false, true);

        var clone = LabelObjectCloner.Clone(item);
        var snapshot = Core.Scene.SceneObjectSnapshot.Capture(item);

        Assert.Equal((11, "7,6,5", false, true, false, true),
            (clone.CheckDigitModulus, clone.CheckDigitWeights, clone.CheckDigitWeightsFromRight,
             clone.CheckDigitSumDigitsOfProducts, clone.CheckDigitComplement, clone.CheckDigitKeepAllDigits));
        Assert.Equal((11, "7,6,5", false, true, false, true),
            (snapshot.CheckDigitModulus, snapshot.CheckDigitWeights, snapshot.CheckDigitWeightsFromRight,
             snapshot.CheckDigitSumDigitsOfProducts, snapshot.CheckDigitComplement, snapshot.CheckDigitKeepAllDigits));
    }
}
