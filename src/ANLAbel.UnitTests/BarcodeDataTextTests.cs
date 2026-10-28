using ANLAbel.Core.Barcode;
using ANLAbel.Core.Enums;
using ANLAbel.Core.Models;
using Xunit;

namespace ANLAbel.UnitTests;

/// <summary>
/// An Excel cell pasted from the web carried U+00A0 at both ends of a part
/// number; Code 128 cannot encode it and the row blocked with a message that
/// named no character (2026-10-28). The edges are trimmed on every barcode
/// path and an interior unsupported character is named with its position.
/// </summary>
public class BarcodeDataTextTests
{
    private const string PartNumber = "SEMCM-SP-00037-ALF";

    [Theory]
    [InlineData(" " + PartNumber + " ")]
    [InlineData("​" + PartNumber)]
    [InlineData(PartNumber + "﻿ ")]
    [InlineData("　 " + PartNumber)]
    public void TrimInvisibleRemovesInvisibleEdges(string data)
    {
        Assert.Equal(PartNumber, BarcodeDataText.TrimInvisible(data));
        Assert.True(BarcodeDataText.HasInvisibleEdge(data));
    }

    [Theory]
    [InlineData(" " + PartNumber + " ")]
    [InlineData("\t" + PartNumber + "\n")]
    [InlineData("SEMCM SP")]
    [InlineData("")]
    public void TrimInvisibleKeepsAsciiWhitespaceAndInteriorCharacters(string data)
    {
        Assert.Equal(data, BarcodeDataText.TrimInvisible(data));
        Assert.False(BarcodeDataText.HasInvisibleEdge(data));
    }

    [Fact]
    public void TrimInvisibleTreatsNullAsEmpty()
    {
        Assert.Equal(string.Empty, BarcodeDataText.TrimInvisible(null));
    }

    [Fact]
    public void DescribeNamesInvisibleCharactersAndShowsVisibleOnes()
    {
        Assert.Equal("U+00A0 (no-break space)", BarcodeDataText.Describe(' '));
        Assert.Equal("U+200B (zero-width space)", BarcodeDataText.Describe('​'));
        Assert.Equal("U+000A (line feed)", BarcodeDataText.Describe('\n'));
        Assert.Equal("U+2013 '–'", BarcodeDataText.Describe('–'));
        Assert.Equal("U+1EA5 'ấ'", BarcodeDataText.Describe('ấ'));
    }

    [Fact]
    public void DescribeFirstUnsupportedReportsOneBasedPosition()
    {
        static bool Ascii(char ch) => ch >= 32 && ch <= 126;
        Assert.Null(BarcodeDataText.DescribeFirstUnsupported(PartNumber, Ascii));
        Assert.Null(BarcodeDataText.DescribeFirstUnsupported(null, Ascii));
        Assert.Equal("U+2013 '–' at position 6", BarcodeDataText.DescribeFirstUnsupported("SEMCM–SP", Ascii));
        Assert.Equal("U+00A0 (no-break space) at position 1", BarcodeDataText.DescribeFirstUnsupported(" SEMCM", Ascii));
    }

    [Fact]
    public void EncodedPayloadAndHriDropInvisibleEdgesOnEveryBarcodePath()
    {
        var item = new LabelObject { Type = ObjectType.BarcodeCode128, BarcodeSymbology = BarcodeSymbology.Code128 };
        var data = " " + PartNumber + " ";

        var encoded = BarcodeCheckDigitContract.ToEncodedPayload(item, data);
        Assert.Equal(PartNumber, encoded);
        Assert.Empty(BarcodeApplicationContract.ValidateData(BarcodeApplicationProfile.General, BarcodeSymbology.Code128, encoded));
        Assert.Equal(PartNumber, BarcodeCheckDigitContract.FormatHriText(item, BarcodeSymbology.Code128, data));
        Assert.Equal(PartNumber, BarcodeCheckDigitContract.HriMeasureText(item, data));
        Assert.Empty(BarcodeCheckDigitContract.Validate(item, BarcodeSymbology.Code128, data));
    }

    [Fact]
    public void GenerateAppendsTheCheckDigitToTheTrimmedData()
    {
        var item = new LabelObject
        {
            Type = ObjectType.BarcodeCode128,
            BarcodeSymbology = BarcodeSymbology.ITF,
            BarcodeCheckDigitPolicy = BarcodeCheckDigitPolicy.Generate,
        };

        Assert.Equal(
            BarcodeCheckDigitContract.ToEncodedPayload(item, "1234567"),
            BarcodeCheckDigitContract.ToEncodedPayload(item, " 1234567​"));
    }

    [Theory]
    [InlineData(BarcodeSymbology.Code128, "SEMCM SP", "Code128 data contains an unsupported character: U+00A0 (no-break space) at position 6;")]
    [InlineData(BarcodeSymbology.Code93, "AB–CD", "Code93 data contains an unsupported character: U+2013 '–' at position 3;")]
    [InlineData(BarcodeSymbology.Code39, "AB_CD", "Code39 data contains an unsupported character: U+005F '_' at position 3;")]
    public void ApplicationContractNamesTheFirstUnsupportedCharacter(BarcodeSymbology symbology, string data, string expectedStart)
    {
        var errors = BarcodeApplicationContract.ValidateData(BarcodeApplicationProfile.General, symbology, data);
        var error = Assert.Single(errors);
        Assert.StartsWith(expectedStart, error);
    }
}
