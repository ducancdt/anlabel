using ANLAbel.Core.Barcode;
using ANLAbel.Core.Enums;
using Xunit;

namespace ANLAbel.UnitTests;

public sealed class BarcodeCheckDigitContractTests
{
    [Fact]
    public void Code39_Verify_FailsClosed_WithoutValidCheckDigit()
    {
        var errors = BarcodeCheckDigitContract.Validate(
            BarcodeSymbology.Code39,
            "ABC123",
            BarcodeCheckDigitPolicy.Verify);

        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Contains("Verify", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Code39_Verify_Passes_WithCorrectCheckDigit()
    {
        var body = "ABC123";
        var check = BarcodeCheckDigitContract.ComputeCode39CheckDigit(body);
        var payload = body + check;

        var errors = BarcodeCheckDigitContract.Validate(
            BarcodeSymbology.Code39,
            payload,
            BarcodeCheckDigitPolicy.Verify);

        Assert.Empty(errors);
        Assert.True(BarcodeCheckDigitContract.HasValidTrailingCheckDigit(BarcodeSymbology.Code39, payload));
    }

    [Fact]
    public void Code39_None_NeverBlocks()
    {
        var errors = BarcodeCheckDigitContract.Validate(
            BarcodeSymbology.Code39,
            "ABC123",
            BarcodeCheckDigitPolicy.None);
        Assert.Empty(errors);
    }

    [Fact]
    public void Code39_Auto_AcceptsWithOrWithoutCheckDigit()
    {
        Assert.Empty(BarcodeCheckDigitContract.Validate(
            BarcodeSymbology.Code39, "ABC123", BarcodeCheckDigitPolicy.Auto));

        var body = "CODE";
        var payload = body + BarcodeCheckDigitContract.ComputeCode39CheckDigit(body);
        Assert.Empty(BarcodeCheckDigitContract.Validate(
            BarcodeSymbology.Code39, payload, BarcodeCheckDigitPolicy.Auto));
    }

    [Fact]
    public void FormatHri_HidesValidatedCheckDigitOnly()
    {
        var body = "WIDGET";
        var check = BarcodeCheckDigitContract.ComputeCode39CheckDigit(body);
        var payload = body + check;

        var shown = BarcodeCheckDigitContract.FormatHriText(
            BarcodeSymbology.Code39, payload, BarcodeCheckDigitPolicy.Verify, showCheckDigit: true);
        var hidden = BarcodeCheckDigitContract.FormatHriText(
            BarcodeSymbology.Code39, payload, BarcodeCheckDigitPolicy.Verify, showCheckDigit: false);

        Assert.Equal(payload, shown);
        Assert.Equal(body, hidden);
        // Hide does not invent different body when check is invalid
        Assert.Equal("ABC123", BarcodeCheckDigitContract.FormatHriText(
            BarcodeSymbology.Code39, "ABC123", BarcodeCheckDigitPolicy.Auto, showCheckDigit: false));
    }

    [Fact]
    public void Itf_Verify_UsesMod10CheckDigit()
    {
        var body = "1234567";
        var check = BarcodeCheckDigitContract.ComputeItfCheckDigit(body);
        var payload = body + check;

        Assert.Empty(BarcodeCheckDigitContract.Validate(
            BarcodeSymbology.ITF, payload, BarcodeCheckDigitPolicy.Verify));
        Assert.NotEmpty(BarcodeCheckDigitContract.Validate(
            BarcodeSymbology.ITF, body, BarcodeCheckDigitPolicy.Verify));
    }

    [Fact]
    public void Code128_IgnoresOptionalPolicy()
    {
        Assert.False(BarcodeCheckDigitContract.SupportsOptionalCheckDigit(BarcodeSymbology.Code128));
        Assert.Empty(BarcodeCheckDigitContract.Validate(
            BarcodeSymbology.Code128, "anything", BarcodeCheckDigitPolicy.Verify));
    }

    [Theory]
    [InlineData(BarcodeSymbology.Code39, true)]
    [InlineData(BarcodeSymbology.ITF, true)]
    [InlineData(BarcodeSymbology.Code128, false)]
    [InlineData(BarcodeSymbology.Code93, false)]
    [InlineData(BarcodeSymbology.QRCode, false)]
    [InlineData(BarcodeSymbology.DataMatrix, false)]
    [InlineData(BarcodeSymbology.Aztec, false)]
    [InlineData(BarcodeSymbology.Pdf417, false)]
    [InlineData(BarcodeSymbology.Ean13, false)]
    [InlineData(BarcodeSymbology.Ean8, false)]
    [InlineData(BarcodeSymbology.UpcA, false)]
    [InlineData(BarcodeSymbology.UpcE, false)]
    [InlineData(BarcodeSymbology.Codabar, false)]
    [InlineData(BarcodeSymbology.MSI, false)]
    [InlineData(BarcodeSymbology.Plessey, false)]
    public void SupportsOptionalCheckDigit_OnlyCode39AndItf(BarcodeSymbology symbology, bool expected)
    {
        Assert.Equal(expected, BarcodeCheckDigitContract.SupportsOptionalCheckDigit(symbology));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void Verify_FailsClosed_OnEmptyOrWhitespacePayload(string? payload)
    {
        var code39 = BarcodeCheckDigitContract.Validate(
            BarcodeSymbology.Code39, payload!, BarcodeCheckDigitPolicy.Verify);
        var itf = BarcodeCheckDigitContract.Validate(
            BarcodeSymbology.ITF, payload!, BarcodeCheckDigitPolicy.Verify);

        Assert.Single(code39);
        Assert.Contains("empty", code39[0], StringComparison.OrdinalIgnoreCase);
        Assert.Single(itf);
        Assert.Contains("empty", itf[0], StringComparison.OrdinalIgnoreCase);
        Assert.Empty(BarcodeCheckDigitContract.Validate(
            BarcodeSymbology.Code39, payload!, BarcodeCheckDigitPolicy.None));
        Assert.Empty(BarcodeCheckDigitContract.Validate(
            BarcodeSymbology.Code39, payload!, BarcodeCheckDigitPolicy.Auto));
    }

    [Fact]
    public void Verify_FailsClosed_WhenTrailingDigitIsWrong()
    {
        var body = "ABC123";
        var check = BarcodeCheckDigitContract.ComputeCode39CheckDigit(body);
        var wrong = check == '0' ? '1' : '0';
        var errors = BarcodeCheckDigitContract.Validate(
            BarcodeSymbology.Code39,
            body + wrong,
            BarcodeCheckDigitPolicy.Verify);

        Assert.Single(errors);
        Assert.Contains("does not end with a valid check digit", errors[0], StringComparison.Ordinal);
        Assert.False(BarcodeCheckDigitContract.HasValidTrailingCheckDigit(
            BarcodeSymbology.Code39, body + wrong));
    }

    [Fact]
    public void Code39_Mod43_PublishedAlphabetIdentities()
    {
        // USS-39 values: '0'=0 … '9'=9, 'A'=10 … 'Z'=35. Single-char body is
        // its own check character. "ZZ" sums 70, 70 % 43 = 27 → 'R'.
        Assert.Equal('0', BarcodeCheckDigitContract.ComputeCode39CheckDigit("0"));
        Assert.Equal('1', BarcodeCheckDigitContract.ComputeCode39CheckDigit("1"));
        Assert.Equal('9', BarcodeCheckDigitContract.ComputeCode39CheckDigit("9"));
        Assert.Equal('A', BarcodeCheckDigitContract.ComputeCode39CheckDigit("A"));
        Assert.Equal('Z', BarcodeCheckDigitContract.ComputeCode39CheckDigit("Z"));
        Assert.Equal('R', BarcodeCheckDigitContract.ComputeCode39CheckDigit("ZZ"));
        Assert.Equal(
            BarcodeCheckDigitContract.ComputeCode39CheckDigit("ABC"),
            BarcodeCheckDigitContract.ComputeCode39CheckDigit("abc"));
    }

    [Fact]
    public void Code39_InvalidCharacter_ThrowsFromCompute_AndFailsHasValid()
    {
        var ex = Assert.Throws<ArgumentException>(
            () => BarcodeCheckDigitContract.ComputeCode39CheckDigit("A*B"));
        Assert.Contains("Invalid Code 39 character", ex.Message, StringComparison.Ordinal);
        Assert.Contains("*", ex.Message, StringComparison.Ordinal);

        Assert.False(BarcodeCheckDigitContract.HasValidTrailingCheckDigit(
            BarcodeSymbology.Code39, "A*B0"));
        Assert.False(BarcodeCheckDigitContract.HasValidTrailingCheckDigit(
            BarcodeSymbology.Code39, "*"));
        Assert.NotEmpty(BarcodeCheckDigitContract.Validate(
            BarcodeSymbology.Code39, "A*B0", BarcodeCheckDigitPolicy.Verify));
    }

    [Fact]
    public void Itf_Mod10_PublishedRightWeightedIdentities()
    {
        // Optional ITF check: weights 3,1,3… from the right.
        // "12" → 2*3 + 1*1 = 7 → check 3.
        // "1234567" → 60 → check 0 (exact multiple of 10).
        Assert.Equal('3', BarcodeCheckDigitContract.ComputeItfCheckDigit("12"));
        Assert.Equal('0', BarcodeCheckDigitContract.ComputeItfCheckDigit("1234567"));
        Assert.Equal('5', BarcodeCheckDigitContract.ComputeItfCheckDigit("123456"));

        var payload = "12345670";
        Assert.True(BarcodeCheckDigitContract.HasValidTrailingCheckDigit(
            BarcodeSymbology.ITF, payload));
        Assert.Empty(BarcodeCheckDigitContract.Validate(
            BarcodeSymbology.ITF, payload, BarcodeCheckDigitPolicy.Verify));
        Assert.False(BarcodeCheckDigitContract.HasValidTrailingCheckDigit(
            BarcodeSymbology.ITF, "12345671"));
    }

    [Fact]
    public void Itf_Compute_RequiresNonEmptyDigits()
    {
        var empty = Assert.Throws<ArgumentException>(
            () => BarcodeCheckDigitContract.ComputeItfCheckDigit(""));
        Assert.Contains("non-empty digit body", empty.Message, StringComparison.Ordinal);

        var letters = Assert.Throws<ArgumentException>(
            () => BarcodeCheckDigitContract.ComputeItfCheckDigit("12A"));
        Assert.Contains("non-empty digit body", letters.Message, StringComparison.Ordinal);

        Assert.False(BarcodeCheckDigitContract.HasValidTrailingCheckDigit(
            BarcodeSymbology.ITF, "12A3"));
        Assert.False(BarcodeCheckDigitContract.HasValidTrailingCheckDigit(
            BarcodeSymbology.ITF, "1"));
        Assert.False(BarcodeCheckDigitContract.HasValidTrailingCheckDigit(
            BarcodeSymbology.ITF, ""));
        Assert.False(BarcodeCheckDigitContract.HasValidTrailingCheckDigit(
            BarcodeSymbology.ITF, null!));
    }

    [Theory]
    [InlineData(BarcodeSymbology.Code128)]
    [InlineData(BarcodeSymbology.QRCode)]
    [InlineData(BarcodeSymbology.Ean13)]
    public void HasValidTrailingCheckDigit_IsFalse_ForUnsupportedSymbology(
        BarcodeSymbology symbology)
    {
        Assert.False(BarcodeCheckDigitContract.HasValidTrailingCheckDigit(symbology, "12345670"));
        Assert.False(BarcodeCheckDigitContract.HasValidTrailingCheckDigit(symbology, "ABC123A"));
        Assert.Empty(BarcodeCheckDigitContract.Validate(
            symbology, "12345670", BarcodeCheckDigitPolicy.Verify));
    }

    [Fact]
    public void HasValidTrailingCheckDigit_RejectsShortOrEmptyPayload()
    {
        Assert.False(BarcodeCheckDigitContract.HasValidTrailingCheckDigit(
            BarcodeSymbology.Code39, ""));
        Assert.False(BarcodeCheckDigitContract.HasValidTrailingCheckDigit(
            BarcodeSymbology.Code39, null!));
        Assert.False(BarcodeCheckDigitContract.HasValidTrailingCheckDigit(
            BarcodeSymbology.Code39, "A"));
        Assert.False(BarcodeCheckDigitContract.HasValidTrailingCheckDigit(
            BarcodeSymbology.ITF, "5"));
    }

    [Fact]
    public void LengthTwoValidPayload_IsAcceptedAndHriCanHide()
    {
        // Body+check length 2 distinguishes `< 2` from `<= 2` on the hide/validate gates.
        Assert.Equal('A', BarcodeCheckDigitContract.ComputeCode39CheckDigit("A"));
        Assert.True(BarcodeCheckDigitContract.HasValidTrailingCheckDigit(
            BarcodeSymbology.Code39, "AA"));
        Assert.Equal("A", BarcodeCheckDigitContract.FormatHriText(
            BarcodeSymbology.Code39, "AA", BarcodeCheckDigitPolicy.Auto, showCheckDigit: false));
        Assert.Empty(BarcodeCheckDigitContract.Validate(
            BarcodeSymbology.Code39, "AA", BarcodeCheckDigitPolicy.Verify));

        Assert.Equal('7', BarcodeCheckDigitContract.ComputeItfCheckDigit("1"));
        Assert.True(BarcodeCheckDigitContract.HasValidTrailingCheckDigit(
            BarcodeSymbology.ITF, "17"));
        Assert.Equal("1", BarcodeCheckDigitContract.FormatHriText(
            BarcodeSymbology.ITF, "17", BarcodeCheckDigitPolicy.Verify, showCheckDigit: false));
        Assert.Empty(BarcodeCheckDigitContract.Validate(
            BarcodeSymbology.ITF, "17", BarcodeCheckDigitPolicy.Verify));
    }

    [Fact]
    public void FormatHri_NonePolicyNeverStrips()
    {
        var body = "WIDGET";
        var payload = body + BarcodeCheckDigitContract.ComputeCode39CheckDigit(body);

        Assert.Equal(payload, BarcodeCheckDigitContract.FormatHriText(
            BarcodeSymbology.Code39, payload, BarcodeCheckDigitPolicy.None, showCheckDigit: false));
        Assert.Equal(payload, BarcodeCheckDigitContract.FormatHriText(
            BarcodeSymbology.Code39, payload, BarcodeCheckDigitPolicy.None, showCheckDigit: true));
    }

    [Fact]
    public void FormatHri_KeepsFullText_WhenHideIsNotAllowed()
    {
        var body = "12";
        var payload = body + BarcodeCheckDigitContract.ComputeItfCheckDigit(body);

        Assert.Equal(payload, BarcodeCheckDigitContract.FormatHriText(
            BarcodeSymbology.ITF, payload, BarcodeCheckDigitPolicy.Auto, showCheckDigit: true));
        Assert.Equal(body, BarcodeCheckDigitContract.FormatHriText(
            BarcodeSymbology.ITF, payload, BarcodeCheckDigitPolicy.Auto, showCheckDigit: false));
        Assert.Equal("1", BarcodeCheckDigitContract.FormatHriText(
            BarcodeSymbology.ITF, "1", BarcodeCheckDigitPolicy.Verify, showCheckDigit: false));
        Assert.Equal("ABC", BarcodeCheckDigitContract.FormatHriText(
            BarcodeSymbology.Code128, "ABC", BarcodeCheckDigitPolicy.Verify, showCheckDigit: false));
        Assert.Equal(string.Empty, BarcodeCheckDigitContract.FormatHriText(
            BarcodeSymbology.Code39, null!, BarcodeCheckDigitPolicy.Auto, showCheckDigit: false));
    }
}
