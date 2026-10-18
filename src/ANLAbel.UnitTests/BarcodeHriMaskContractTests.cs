using ANLAbel.Core.Barcode;
using ANLAbel.Core.Enums;
using ANLAbel.Core.Models;
using Xunit;

namespace ANLAbel.UnitTests;

public sealed class BarcodeHriMaskContractTests
{
    [Theory]
    [InlineData("(***) ***-****", "*", "0123456789", "(012) 345-6789")]
    [InlineData("LOT ##-##", "#", "2026", "LOT 20-26")]
    [InlineData("", "*", "ABC", "ABC")]
    public void MaskReplacesEachPositionWithTheNextCharacter(string mask, string slot, string text, string expected)
    {
        Assert.True(BarcodeHriMaskContract.TryApply(mask, slot, text, out var masked));
        Assert.Equal(expected, masked);
        Assert.Equal(string.Empty, BarcodeHriMaskContract.Validate(mask, slot, text));
    }

    [Theory]
    [InlineData("***-**", "1234")]
    [InlineData("**", "1234")]
    public void AMaskThatWouldDropOrInventCharactersIsRefused(string mask, string text)
    {
        Assert.False(BarcodeHriMaskContract.TryApply(mask, "*", text, out var shown));
        Assert.Equal(text, shown);
        Assert.Contains("would drop or invent characters", BarcodeHriMaskContract.Validate(mask, "*", text));
    }

    [Fact]
    public void ObjectHriAppliesTheMaskAfterTheCheckDigitRule()
    {
        var item = new LabelObject
        {
            Type = ObjectType.BarcodeCode128,
            BarcodeSymbology = BarcodeSymbology.ITF,
            BarcodeCheckDigitPolicy = BarcodeCheckDigitPolicy.Generate,
            BarcodeHriShowCheckDigit = false,
            BarcodeHriMask = "***-**"
        };
        var encoded = BarcodeCheckDigitContract.ToEncodedPayload(item, "12345");

        Assert.Equal("123456".Length, encoded.Length);
        Assert.Equal("123-45", BarcodeCheckDigitContract.FormatHriText(item, BarcodeSymbology.ITF, encoded));
        Assert.Equal("123-45", BarcodeCheckDigitContract.HriMeasureText(item, encoded));

        item.BarcodeHriMask = "**";
        Assert.Equal("12345", BarcodeCheckDigitContract.FormatHriText(item, BarcodeSymbology.ITF, encoded));
    }

    [Fact]
    public void WithoutAMaskTheHriIsMeasuredFromTheRawData()
    {
        var item = new LabelObject { Type = ObjectType.BarcodeCode128, BarcodeSymbology = BarcodeSymbology.Code128 };

        Assert.Equal("ABC", BarcodeCheckDigitContract.HriMeasureText(item, "ABC"));
    }

    [Fact]
    public void ClonerAndSnapshotCarryTheMask()
    {
        var item = new LabelObject { BarcodeHriMask = "##-##", BarcodeHriMaskCharacter = "#" };

        var clone = LabelObjectCloner.Clone(item);
        var snapshot = Core.Scene.SceneObjectSnapshot.Capture(item);

        Assert.Equal(("##-##", "#"), (clone.BarcodeHriMask, clone.BarcodeHriMaskCharacter));
        Assert.Equal(("##-##", "#"), (snapshot.BarcodeHriMask, snapshot.BarcodeHriMaskCharacter));
        item.BarcodeHriMaskCharacter = "";
        Assert.Equal("*", item.BarcodeHriMaskCharacter);
    }
}
