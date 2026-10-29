using System.Text;
using ANLAbel.Core.Enums;
using ANLAbel.Core.Models;
using ANLAbel.Project.NiceLabel10;
using Xunit;

namespace ANLAbel.UnitTests;

/// <summary>
/// NiceLabel 10 XML → ANLAbel mapping on decrypted package entries (the
/// package cipher itself is covered by the regression gate).
/// </summary>
public sealed class NiceLabel10ImportMappingTests
{
    private const string LotId = "11111111-2222-3333-4444-555555555555";
    private const string DateId = "22222222-2222-3333-4444-555555555555";
    private const string ConcatId = "33333333-2222-3333-4444-555555555555";
    private const string MissingId = "99999999-2222-3333-4444-555555555555";
    private const string Gs1Id = "55555555-2222-3333-4444-555555555555";
    private const string Gs1CheckDigitId = "66666666-2222-3333-4444-555555555555";
    private const string DateAddId = "77777777-2222-3333-4444-555555555555";
    private const string QtyId = "88888888-2222-3333-4444-555555555555";
    private const string AscSingleId = "aaaaaaaa-2222-3333-4444-555555555555";
    private const string AscMultiId = "bbbbbbbb-2222-3333-4444-555555555555";
    private const string EnvelopeId = "cccccccc-2222-3333-4444-555555555555";
    private const string EnvelopeOtherFormatId = "dddddddd-2222-3333-4444-555555555555";

    private const string SolutionXml = $"""
        <?xml version="1.0" encoding="utf-8"?>
        <EuroPlus.NiceLabel Type="Solution">
          <Variables>
            <Item Type="Variable">
              <Id>{LotId}</Id>
              <Name>Lot</Name>
              <DefaultValue Type="StringContents"><StringValue>L-001</StringValue><UserValue>L-001</UserValue></DefaultValue>
              <PromptText>Enter the lot</PromptText>
              <AllowedCharactersForCustomFormat>0123456789</AllowedCharactersForCustomFormat>
              <Constraints Type="DataSourceConstraints"><Length>8</Length><IsRequired>True</IsRequired></Constraints>
            </Item>
            <Item Type="Variable">
              <Id>{DateId}</Id>
              <Name>Packed</Name>
              <DefaultValue Type="SystemDateTimeContents">
                <Offset Type="DateTimeOffsets"><Days>3</Days><Hours>0</Hours></Offset>
                <InputFormat>dd.MM.yyyy</InputFormat>
                <OutputFormat>yyyyMMdd</OutputFormat>
              </DefaultValue>
            </Item>
            <Item Type="Variable">
              <Id>{ConcatId}</Id>
              <Name>Caption</Name>
              <DefaultValue Type="StringContents"><StringValue>Lot [{LotId}] packed [{DateId}]</StringValue></DefaultValue>
              <InternalFunction Type="ExtendedDataValueFunction">
                <Id>44444444-2222-3333-4444-555555555555</Id>
                <DataValue Type="ExtendedDataValue">
                  <DataSourceReferences>
                    <Item Type="SolutionDataSourceReference"><Id>{LotId}</Id></Item>
                    <Item Type="SolutionDataSourceReference"><Id>{DateId}</Id></Item>
                  </DataSourceReferences>
                  <FixedValue Type="StringContents"><StringValue>Lot [{LotId}] packed [{DateId}]</StringValue></FixedValue>
                </DataValue>
              </InternalFunction>
            </Item>
            <Item Type="Variable">
              <Id>{QtyId}</Id>
              <Name>Qty</Name>
              <DefaultValue Type="StringContents"><StringValue>12</StringValue></DefaultValue>
            </Item>
          </Variables>
          <Functions>
            <Item Type="DateAddFunction">
              <Id>{DateAddId}</Id>
              <Name>Use by</Name>
              <Day Type="DataValue"><FixedValue Type="IntegerContents"><Int32Value>4</Int32Value></FixedValue></Day>
              <Month Type="DataValue"><FixedValue Type="IntegerContents"><Int32Value>1</Int32Value></FixedValue></Month>
              <Year Type="DataValue"><FixedValue Type="IntegerContents"></FixedValue></Year>
              <InputDataSourceReference Type="SolutionDataSourceReference"><Id>{DateId}</Id></InputDataSourceReference>
              <OutputFormat>MM/dd/yyyy</OutputFormat>
            </Item>
            <Item Type="GS1128Function">
              <Id>{Gs1Id}</Id>
              <Name>GS1-128-(02+17+37+3103)</Name>
              <DataValues>
                <Item Type="GS1128DataValue"><ApplicationIdentifierId>3</ApplicationIdentifierId><Multiplier>0</Multiplier><AutoCheckDigitCalculation>False</AutoCheckDigitCalculation><DataSourceReference Type="SolutionDataSourceReference"><Id>{LotId}</Id></DataSourceReference></Item>
                <Item Type="GS1128DataValue"><ApplicationIdentifierId>8</ApplicationIdentifierId><Multiplier>0</Multiplier><AutoCheckDigitCalculation>False</AutoCheckDigitCalculation><DataSourceReference Type="SolutionDataSourceReference"><Id>{DateAddId}</Id></DataSourceReference></Item>
                <Item Type="GS1128DataValue"><ApplicationIdentifierId>30</ApplicationIdentifierId><Multiplier>0</Multiplier><AutoCheckDigitCalculation>False</AutoCheckDigitCalculation><DataSourceReference Type="SolutionDataSourceReference"><Id>{QtyId}</Id></DataSourceReference></Item>
                <Item Type="GS1128DataValue"><ApplicationIdentifierId>71</ApplicationIdentifierId><Multiplier>3</Multiplier><AutoCheckDigitCalculation>False</AutoCheckDigitCalculation><DataSourceReference Type="SolutionDataSourceReference"><Id>{QtyId}</Id></DataSourceReference></Item>
              </DataValues>
            </Item>
            <Item Type="GS1128Function">
              <Id>{Gs1CheckDigitId}</Id>
              <Name>GS1-128-(01)</Name>
              <DataValues>
                <Item Type="GS1128DataValue"><ApplicationIdentifierId>2</ApplicationIdentifierId><Multiplier>0</Multiplier><AutoCheckDigitCalculation>True</AutoCheckDigitCalculation><DataSourceReference Type="SolutionDataSourceReference"><Id>{LotId}</Id></DataSourceReference></Item>
              </DataValues>
            </Item>
            <Item Type="AscFactFunction">
              <Id>{AscSingleId}</Id>
              <Name>Serial</Name>
              <DataValues>
                <Item Type="AscFactDataValue"><AscFactIdentifierName>1S</AscFactIdentifierName><IsUsingShortTitle>False</IsUsingShortTitle><DataSourceReference Type="SolutionDataSourceReference"><Id>{LotId}</Id></DataSourceReference></Item>
              </DataValues>
            </Item>
            <Item Type="AscFactFunction">
              <Id>{AscMultiId}</Id>
              <Name>Data Identifiers</Name>
              <DataValues>
                <Item Type="AscFactDataValue"><AscFactIdentifierName>1P</AscFactIdentifierName><IsUsingShortTitle>False</IsUsingShortTitle><DataSourceReference Type="SolutionDataSourceReference"><Id>{LotId}</Id></DataSourceReference></Item>
                <Item Type="AscFactDataValue"><AscFactIdentifierName>Q</AscFactIdentifierName><IsUsingShortTitle>False</IsUsingShortTitle><DataSourceReference Type="SolutionDataSourceReference"><Id>{QtyId}</Id></DataSourceReference></Item>
                <Item Type="AscFactDataValue"><AscFactIdentifierName>12V</AscFactIdentifierName><IsUsingShortTitle>False</IsUsingShortTitle><FixedValue Type="StringContents"><StringValue>ACME</StringValue></FixedValue></Item>
              </DataValues>
            </Item>
            <Item Type="TransferDataSyntaxFunction">
              <Id>{EnvelopeId}</Id>
              <Name>2D code</Name>
              <Formats>
                <Item Type="TransferDataSyntaxFormat"><FormatId>6</FormatId><DataValues><Item Type="DataValue"><DataSourceReference Type="SolutionDataSourceReference"><Id>{AscMultiId}</Id></DataSourceReference></Item></DataValues></Item>
              </Formats>
            </Item>
            <Item Type="TransferDataSyntaxFunction">
              <Id>{EnvelopeOtherFormatId}</Id>
              <Name>Format 01</Name>
              <Formats>
                <Item Type="TransferDataSyntaxFormat"><FormatId>1</FormatId><DataValues><Item Type="DataValue"><DataSourceReference Type="SolutionDataSourceReference"><Id>{LotId}</Id></DataSourceReference></Item></DataValues></Item>
              </Formats>
            </Item>
          </Functions>
        </EuroPlus.NiceLabel>
        """;

    private static NiceLabel10Document Parse(string formatXml, string? solutionXml = SolutionXml)
    {
        var entries = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["Formats/Sample"] = Encoding.UTF8.GetBytes(formatXml)
        };
        if (solutionXml is not null)
        {
            entries["Sample.nsln"] = Encoding.UTF8.GetBytes(solutionXml);
        }

        return NiceLabel10NlblReader.Parse(entries);
    }

    private static string Format(string items) => $"""
        <?xml version="1.0" encoding="utf-8"?>
        <EuroPlus.NiceLabel Type="Format">
          <Name>Mapping</Name>
          <Media Type="FormatMedia"><Width>100000</Width><Height>60000</Height></Media>
          <DocumentItems>
            {items}
          </DocumentItems>
        </EuroPlus.NiceLabel>
        """;

    private static LabelTemplate Import(string items, INiceLabel10TextMeasurer? measurer = null) =>
        NiceLabel10NlblImporter.CreateTemplate(Parse(Format(items)), "sample.nlbl", measurer);

    [Fact]
    public void Gs1Composition_BecomesParenthesizedAiExpression()
    {
        var template = Import($"""
            <Item Type="BarcodeDocumentItem">
              <Name>Gs1</Name>
              <Geometry Type="PositionGeometry"><X>10000</X><Y>38000</Y></Geometry>
              <BarcodeData Type="GS1128BarcodeData"><BaseBarWidth>300</BaseBarWidth><ModuleHeight>10000</ModuleHeight></BarcodeData>
              <FixedContents>(02)00000000000000</FixedContents>
              <DataSourceReference Type="SolutionDataSourceReference"><Id>{Gs1Id}</Id></DataSourceReference>
            </Item>
            """);

        var code = Assert.Single(template.Objects);
        Assert.Equal(BarcodeApplicationProfile.Gs1, code.BarcodeApplicationProfile);
        // The date AI takes the GS1 yyMMdd form; the input current-date
        // variable's own +3 days add to the function's +4 days.
        Assert.Equal("(02){?Lot}(17){#NOW+7d+1mo:yyMMdd}(37){?Qty}(3103){?Qty}", code.BindingExpression);
        Assert.Equal(new[] { "Lot", "Qty" }, template.PromptVariables.Select(v => v.Name).ToArray());
    }

    [Fact]
    public void Gs1CompositionWithAutoCheckDigit_IsNotConverted()
    {
        var template = Import($"""
            <Item Type="TextDocumentItem">
              <Name>Gtin</Name>
              <Geometry Type="PositionGeometry"><X>1000</X><Y>1000</Y></Geometry>
              <FixedContents>(01)1234567890123</FixedContents>
              <DataSourceReference Type="SolutionDataSourceReference"><Id>{Gs1CheckDigitId}</Id></DataSourceReference>
            </Item>
            """);

        Assert.Empty(template.Objects);
    }

    [Fact]
    public void SingleDataIdentifier_PrefixesTheBarcodeValue()
    {
        var template = Import($"""
            <Item Type="BarcodeDocumentItem">
              <Name>Serial</Name>
              <Geometry Type="PositionGeometry"><X>10000</X><Y>38000</Y></Geometry>
              <BarcodeData Type="Code39BarcodeData"><BaseBarWidth>300</BaseBarWidth><ModuleHeight>10000</ModuleHeight></BarcodeData>
              <FixedContents>1S000125</FixedContents>
              <DataSourceReference Type="SolutionDataSourceReference"><Id>{AscSingleId}</Id></DataSourceReference>
            </Item>
            """);

        Assert.Equal("1S{?Lot}", Assert.Single(template.Objects).BindingExpression);
    }

    [Fact]
    public void TransferDataSyntaxFormat06_WrapsDataIdentifierFieldsInTheEnvelope()
    {
        var template = Import($"""
            <Item Type="BarcodeDocumentItem">
              <Name>Dm</Name>
              <Geometry Type="PositionGeometry"><X>80000</X><Y>20000</Y></Geometry>
              <BarcodeData Type="DatamatrixBarcodeData"><BaseBarWidth>500</BaseBarWidth><ModuleHeight>0</ModuleHeight><DatamatrixFormat>0</DatamatrixFormat></BarcodeData>
              <FixedContents>sample</FixedContents>
              <DataSourceReference Type="SolutionDataSourceReference"><Id>{EnvelopeId}</Id></DataSourceReference>
            </Item>
            """);

        Assert.Equal("[)>\u001E06\u001D1P{?Lot}\u001DQ{?Qty}\u001D12VACME\u001E\u0004", Assert.Single(template.Objects).BindingExpression);
    }

    [Fact]
    public void TransferDataSyntaxOtherFormat_IsNotConverted()
    {
        var template = Import($"""
            <Item Type="BarcodeDocumentItem">
              <Name>Dm</Name>
              <Geometry Type="PositionGeometry"><X>80000</X><Y>20000</Y></Geometry>
              <BarcodeData Type="DatamatrixBarcodeData"><BaseBarWidth>500</BaseBarWidth><ModuleHeight>0</ModuleHeight></BarcodeData>
              <FixedContents>sample</FixedContents>
              <DataSourceReference Type="SolutionDataSourceReference"><Id>{EnvelopeOtherFormatId}</Id></DataSourceReference>
            </Item>
            """);

        Assert.Empty(template.Objects);
    }

    [Fact]
    public void DateAddOnCurrentDate_BecomesOffsetNowField()
    {
        var template = Import($"""
            <Item Type="TextDocumentItem">
              <Name>UseBy</Name>
              <Geometry Type="PositionGeometry"><X>1000</X><Y>1000</Y></Geometry>
              <FixedContents>11/07/2026</FixedContents>
              <DataSourceReference Type="SolutionDataSourceReference"><Id>{DateAddId}</Id></DataSourceReference>
            </Item>
            """);

        // The input current-date variable carries +3 days itself.
        Assert.Equal("{#NOW+7d+1mo:MM/dd/yyyy}", Assert.Single(template.Objects).BindingExpression);
    }

    [Fact]
    public void Reader_CollectsVariableFacts()
    {
        var document = Parse(Format(string.Empty));

        // Three variables, the date-add and the mappable GS1 function; the
        // GS1 function needing a computed check digit is not registered.
        Assert.Equal(9, document.VariablesById.Count);
        Assert.False(document.VariablesById.ContainsKey(Gs1CheckDigitId));
        Assert.False(document.VariablesById.ContainsKey(EnvelopeOtherFormatId));
        var lot = document.VariablesById[LotId];
        Assert.Equal("Lot", lot.Name);
        Assert.Equal("Enter the lot", lot.PromptText);
        Assert.Equal("L-001", lot.DefaultValue);
        Assert.Equal(8, lot.MaxLength);
        Assert.True(lot.Required);
        Assert.True(lot.DigitsOnly);
        var packed = document.VariablesById[DateId];
        Assert.Equal("yyyyMMdd", packed.SystemDateFormat);
        Assert.Equal(3, packed.SystemDateOffsetDays);
        Assert.Contains("[" + LotId + "]", document.VariablesById[ConcatId].FunctionTemplate);
    }

    [Fact]
    public void BoundText_KeepsPromptFactsAndSampleContent()
    {
        var template = Import($"""
            <Item Type="TextDocumentItem">
              <Name>LotText</Name>
              <FontDescriptor Type="FontDescriptor"><Name>Arial</Name><Height>9</Height>
                <LogFontWrapper Type="LogFontWrapper"><Underline>255</Underline></LogFontWrapper></FontDescriptor>
              <Geometry Type="PositionGeometry"><X>10000</X><Y>12000</Y></Geometry>
              <FixedContents>SAMPLE LOT</FixedContents>
              <DataSourceReference Type="SolutionDataSourceReference"><Id>{LotId}</Id></DataSourceReference>
            </Item>
            """);

        var text = Assert.Single(template.Objects);
        Assert.Equal("{?Lot}", text.BindingExpression);
        Assert.True(text.Style.Underline);
        var prompt = Assert.Single(template.PromptVariables);
        Assert.Equal("Lot", prompt.Name);
        Assert.Equal("Enter the lot", prompt.Prompt);
        Assert.Equal("L-001", prompt.DefaultValue);
        Assert.Equal(8, prompt.MaxLength);
        Assert.True(prompt.Required);
        Assert.True(prompt.DigitsOnly);
    }

    [Fact]
    public void SystemDateVariable_BecomesNowField()
    {
        var template = Import($"""
            <Item Type="TextDocumentItem">
              <Name>Date</Name>
              <Geometry Type="PositionGeometry"><X>1000</X><Y>1000</Y></Geometry>
              <FixedContents>20260101</FixedContents>
              <DataSourceReference Type="SolutionDataSourceReference"><Id>{DateId}</Id></DataSourceReference>
            </Item>
            """);

        Assert.Equal("{#NOW+3d:yyyyMMdd}", Assert.Single(template.Objects).BindingExpression);
        Assert.Empty(template.PromptVariables);
    }

    [Fact]
    public void ConcatenationFunction_BecomesMixedExpression()
    {
        var template = Import($"""
            <Item Type="TextDocumentItem">
              <Name>Caption</Name>
              <Geometry Type="PositionGeometry"><X>1000</X><Y>1000</Y></Geometry>
              <FixedContents>Lot L-001 packed 20260101</FixedContents>
              <DataSourceReference Type="SolutionDataSourceReference"><Id>{ConcatId}</Id></DataSourceReference>
            </Item>
            """);

        Assert.Equal("Lot {?Lot} packed {#NOW+3d:yyyyMMdd}", Assert.Single(template.Objects).BindingExpression);
        Assert.Equal("Lot", Assert.Single(template.PromptVariables).Name);
    }

    [Fact]
    public void InlineContentReferences_BecomeMixedExpression()
    {
        var template = Import($"""
            <Item Type="TextDocumentItem">
              <Name>Mixed</Name>
              <Geometry Type="PositionGeometry"><X>1000</X><Y>1000</Y></Geometry>
              <FixedContents>Lot: L-001</FixedContents>
              <Contents Type="ExtendedDataValue">
                <DataSourceReferences><Item Type="SolutionDataSourceReference"><Id>{LotId}</Id></Item></DataSourceReferences>
                <FixedValue Type="StringContents"><StringValue>Lot: [{LotId}]</StringValue></FixedValue>
              </Contents>
            </Item>
            """);

        Assert.Equal("Lot: {?Lot}", Assert.Single(template.Objects).BindingExpression);
    }

    [Fact]
    public void UndeclaredReference_IsNotConverted()
    {
        var template = Import($"""
            <Item Type="TextDocumentItem">
              <Name>Orphan</Name>
              <Geometry Type="PositionGeometry"><X>1000</X><Y>1000</Y></Geometry>
              <FixedContents>SAMPLE</FixedContents>
              <DataSourceReference Type="SolutionDataSourceReference"><Id>{MissingId}</Id></DataSourceReference>
            </Item>
            """);

        Assert.Empty(template.Objects);
    }

    [Fact]
    public void AnchoredRotatedText_IsPlacedFromMeasuredSize()
    {
        // RightTop anchor at 270°: the stored point is the top-left of the
        // rotated box, so the unrotated 40 x 8 frame is centred on (X + 4, Y + 20).
        var template = Import("""
            <Item Type="TextDocumentItem">
              <Name>Side</Name>
              <FontDescriptor Type="FontDescriptor"><Name>Arial</Name><Height>12</Height></FontDescriptor>
              <Geometry Type="PositionGeometry"><X>13700</X><Y>162800</Y><Rotation>270</Rotation><AnchoringPoint>2</AnchoringPoint></Geometry>
              <FixedContents>887766554</FixedContents>
            </Item>
            """, new FixedMeasurer(40, 8));

        var text = Assert.Single(template.Objects);
        Assert.Equal(270, text.Rotation);
        Assert.Equal(13.7 + 4 - 20, text.XMm, 2);
        Assert.Equal(162.8 + 20 - 4, text.YMm, 2);
    }

    [Fact]
    public void UnanchoredUnrotatedText_IsNotMeasured()
    {
        var measurer = new FixedMeasurer(40, 8);
        var template = Import("""
            <Item Type="TextDocumentItem">
              <Name>Plain</Name>
              <Geometry Type="PositionGeometry"><X>5000</X><Y>7000</Y></Geometry>
              <FixedContents>X</FixedContents>
            </Item>
            """, measurer);

        var text = Assert.Single(template.Objects);
        Assert.Equal(0, measurer.Calls);
        Assert.Equal((5.0, 7.0, 0), (text.XMm, text.YMm, text.Rotation));
    }

    [Fact]
    public void FramedText_AnchoredAtBottomLeft_UsesStoredFrameSize()
    {
        var template = Import("""
            <Item Type="TextDocumentItem">
              <Name>Footer</Name>
              <Geometry Type="RectGeometry"><Left>2700</Left><Top>68400</Top><Width>55500</Width><Height>19700</Height><AnchoringPoint>6</AnchoringPoint></Geometry>
              <TextType>2</TextType>
              <FixedContents>Text Box</FixedContents>
            </Item>
            """);

        var box = Assert.Single(template.Objects);
        Assert.Equal(ObjectType.TextBox, box.Type);
        Assert.Equal(2.7, box.XMm, 2);
        Assert.Equal(68.4 - 19.7, box.YMm, 2);
        Assert.Equal(55.5, box.WidthMm, 2);
        Assert.Equal(19.7, box.HeightMm, 2);
    }

    [Fact]
    public void ScaledPositionText_CompressesIntoFixedFrameAtScaledWidth()
    {
        var template = Import("""
            <Item Type="TextDocumentItem">
              <Name>Narrow</Name>
              <FontDescriptor Type="FontDescriptor"><Name>Arial</Name><Height>10</Height></FontDescriptor>
              <Geometry Type="PositionGeometry"><X>50000</X><Y>7000</Y><AnchoringPoint>2</AnchoringPoint></Geometry>
              <ScaleFactor>79</ScaleFactor>
              <FixedContents>COMPRESSED</FixedContents>
            </Item>
            """, new FixedMeasurer(40, 8));

        var text = Assert.Single(template.Objects);
        Assert.Equal(ObjectType.Text, text.Type);
        Assert.Equal(TextSizingMode.FixedFrame, text.Style.TextSizing);
        Assert.Equal(40 * 0.79, text.WidthMm, 2);
        Assert.Equal(8, text.HeightMm, 2);
        // RightTop anchor: the scaled width ends at the stored X.
        Assert.Equal(50 - 40 * 0.79, text.XMm, 2);
    }

    [Theory]
    [InlineData("100", TextSizingMode.AutoFit)]
    [InlineData("109", TextSizingMode.AutoFit)]
    [InlineData("", TextSizingMode.AutoFit)]
    public void UnscaledOrExpandedText_KeepsContentOwnedSizing(string scale, TextSizingMode expected)
    {
        var element = scale.Length == 0 ? string.Empty : $"<ScaleFactor>{scale}</ScaleFactor>";
        var template = Import($"""
            <Item Type="TextDocumentItem">
              <Name>Plain</Name>
              <Geometry Type="PositionGeometry"><X>5000</X><Y>7000</Y></Geometry>
              {element}
              <FixedContents>X</FixedContents>
            </Item>
            """, new FixedMeasurer(40, 8));

        Assert.Equal(expected, Assert.Single(template.Objects).Style.TextSizing);
    }

    [Fact]
    public void RichFramedText_CarriesRecoveredPlainText()
    {
        var rtf = @"{\rtf1\ansi\deff0{\fonttbl{\f0 Arial;}}\pard\b ACME\b0  IDEAL\par 1 ROADRUNNER WAY\par}";
        var stored = Convert.ToBase64String(Encoding.UTF8.GetBytes(rtf));
        var template = Import($"""
            <Item Type="TextDocumentItem">
              <Name>Address</Name>
              <Geometry Type="RectGeometry"><Left>5000</Left><Top>5000</Top><Width>40000</Width><Height>10000</Height></Geometry>
              <TextType>2</TextType>
              <RichTextFormatting>True</RichTextFormatting>
              <FixedContents>{stored}</FixedContents>
            </Item>
            """);

        var box = Assert.Single(template.Objects);
        Assert.Equal(ObjectType.TextBox, box.Type);
        Assert.Equal("ACME IDEAL\n1 ROADRUNNER WAY", box.Text);
    }

    [Fact]
    public void MultiLineFixedContents_AreDecodedFromBase64()
    {
        var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes("Line one\r\nLine two"));
        var template = Import($"""
            <Item Type="TextDocumentItem">
              <Name>Two</Name>
              <Geometry Type="PositionGeometry"><X>1000</X><Y>1000</Y></Geometry>
              <FixedContents>{encoded}</FixedContents>
            </Item>
            """);

        Assert.Equal("Line one\r\nLine two", Assert.Single(template.Objects).Text);
    }

    [Fact]
    public void PlainFramedText_BoundToMultiLineVariable_DecodesTheVariableDefault()
    {
        var encodedDefault = Convert.ToBase64String(Encoding.UTF8.GetBytes("ACME IDEAL AUTO PARTS\r\n1 ROADRUNNER WAY"));
        var solution = SolutionXml.Replace(
            "<StringValue>L-001</StringValue><UserValue>L-001</UserValue>",
            $"<StringValue>{encodedDefault}</StringValue><UserValue>{encodedDefault}</UserValue>");
        var entries = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["Formats/Sample"] = Encoding.UTF8.GetBytes(Format($"""
                <Item Type="TextDocumentItem">
                  <Name>FromAddress</Name>
                  <Geometry Type="RectGeometry"><Left>5000</Left><Top>5000</Top><Width>40000</Width><Height>10000</Height></Geometry>
                  <TextType>2</TextType>
                  <FixedContents>????????</FixedContents>
                  <DataSourceReference Type="SolutionDataSourceReference"><Id>{LotId}</Id></DataSourceReference>
                </Item>
                """)),
            ["Sample.slnx"] = Encoding.UTF8.GetBytes(solution)
        };

        var template = NiceLabel10NlblImporter.CreateTemplate(NiceLabel10NlblReader.Parse(entries), "sample.nlbl");

        Assert.Equal("ACME IDEAL AUTO PARTS\r\n1 ROADRUNNER WAY", Assert.Single(template.PromptVariables).DefaultValue);
    }

    [Fact]
    public void RichFramedText_BoundToVariable_DecodesTheVariableDefault()
    {
        var encodedDefault = Convert.ToBase64String(Encoding.UTF8.GetBytes("ACME IDEAL AUTO PARTS\r\n1 ROADRUNNER WAY"));
        var solution = SolutionXml.Replace(
            "<StringValue>L-001</StringValue><UserValue>L-001</UserValue>",
            $"<StringValue>{encodedDefault}</StringValue><UserValue>{encodedDefault}</UserValue>");
        var entries = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["Formats/Sample"] = Encoding.UTF8.GetBytes(Format($"""
                <Item Type="TextDocumentItem">
                  <Name>FromAddress</Name>
                  <Geometry Type="RectGeometry"><Left>5000</Left><Top>5000</Top><Width>40000</Width><Height>10000</Height></Geometry>
                  <TextType>2</TextType>
                  <RichTextFormatting>True</RichTextFormatting>
                  <FixedContents>{encodedDefault}</FixedContents>
                  <DataSourceReference Type="SolutionDataSourceReference"><Id>{LotId}</Id></DataSourceReference>
                </Item>
                """)),
            ["Sample.slnx"] = Encoding.UTF8.GetBytes(solution)
        };

        var template = NiceLabel10NlblImporter.CreateTemplate(NiceLabel10NlblReader.Parse(entries), "sample.nlbl");

        var box = Assert.Single(template.Objects);
        Assert.Equal("{?Lot}", box.BindingExpression);
        Assert.Equal("ACME IDEAL AUTO PARTS\r\n1 ROADRUNNER WAY", Assert.Single(template.PromptVariables).DefaultValue);
    }

    [Fact]
    public void FramedText_BestFitByFontSize_BecomesShrinkFontWithBounds()
    {
        var template = Import("""
            <Item Type="TextDocumentItem">
              <Name>Fit</Name>
              <Geometry Type="RectGeometry"><Left>5000</Left><Top>5000</Top><Width>40000</Width><Height>10000</Height></Geometry>
              <TextType>2</TextType>
              <BestFit>1</BestFit>
              <BestFitMinimumFontSize>4</BestFitMinimumFontSize>
              <BestFitMaximumFontSize>48</BestFitMaximumFontSize>
              <FixedContents>Fit me</FixedContents>
            </Item>
            """);

        var box = Assert.Single(template.Objects);
        Assert.Equal(TextSizingMode.ShrinkFont, box.Style.TextSizing);
        Assert.Equal(4, box.Style.TextFitMinimumFontSizePt);
        Assert.Equal(48, box.Style.TextFitMaximumFontSizePt);
        Assert.Equal((40.0, 10.0), (box.WidthMm, box.HeightMm));
    }

    [Fact]
    public void FramedText_BestFitByFontScaling_BecomesScaleWidthWithBounds()
    {
        var template = Import("""
            <Item Type="TextDocumentItem">
              <Name>Fit</Name>
              <Geometry Type="RectGeometry"><Left>5000</Left><Top>5000</Top><Width>40000</Width><Height>10000</Height></Geometry>
              <TextType>2</TextType>
              <BestFit>2</BestFit>
              <BestFitMinimumFontScaling>50</BestFitMinimumFontScaling>
              <BestFitMaximumFontScaling>100</BestFitMaximumFontScaling>
              <FixedContents>Fit me</FixedContents>
            </Item>
            """);

        var box = Assert.Single(template.Objects);
        Assert.Equal(TextSizingMode.ScaleWidth, box.Style.TextSizing);
        Assert.Equal(0.5, box.Style.TextFitMinimumScale, 3);
        Assert.Equal(1.0, box.Style.TextFitMaximumScale, 3);
    }

    [Theory]
    [InlineData("<HumanInterpretationPosition>0</HumanInterpretationPosition>", BarcodeHriPlacement.None)]
    [InlineData("<HumanInterpretationPosition>1</HumanInterpretationPosition>", BarcodeHriPlacement.Below)]
    [InlineData("<HumanInterpretationPosition>2</HumanInterpretationPosition>", BarcodeHriPlacement.Above)]
    [InlineData("", BarcodeHriPlacement.Below)]
    public void BarcodeHumanReadable_FollowsVendorEnumAndLoaderDefault(string element, BarcodeHriPlacement expected)
    {
        var template = Import($"""
            <Item Type="BarcodeDocumentItem">
              <Name>Code</Name>
              <Geometry Type="PositionGeometry"><X>10000</X><Y>38000</Y></Geometry>
              <BarcodeData Type="Code128BarcodeData"><BaseBarWidth>300</BaseBarWidth><ModuleHeight>10000</ModuleHeight>{element}</BarcodeData>
              <FixedContents>12345678</FixedContents>
            </Item>
            """);

        Assert.Equal(expected, Assert.Single(template.Objects).BarcodeHriPlacement);
    }

    [Fact]
    public void AnchoredBarcode_IsPlacedFromItsSeededFrame()
    {
        // CenterMiddle anchor: the stored point is the symbol's centre.
        var template = Import("""
            <Item Type="BarcodeDocumentItem">
              <Name>Code</Name>
              <Geometry Type="PositionGeometry"><X>50000</X><Y>30000</Y><AnchoringPoint>4</AnchoringPoint></Geometry>
              <BarcodeData Type="Code128BarcodeData"><BaseBarWidth>250</BaseBarWidth><ModuleHeight>10000</ModuleHeight></BarcodeData>
              <FixedContents>1234</FixedContents>
            </Item>
            """);

        var code = Assert.Single(template.Objects);
        Assert.True(code.WidthMm > 0);
        Assert.Equal(50 - code.WidthMm / 2, code.XMm, 2);
        Assert.Equal(30 - 10 / 2.0, code.YMm, 2);
    }

    [Fact]
    public void EmbeddedPngGraphic_BecomesImageWithFrameAndAnchor()
    {
        var png = TinyPng(3, 2);
        var template = Import($"""
            <Item Type="GraphicDocumentItem">
              <Name>Logo</Name>
              <Geometry Type="RectGeometry"><Left>150800</Left><Top>39500</Top><Width>12700</Width><Height>10200</Height><AnchoringPoint>2</AnchoringPoint></Geometry>
              <EmbeddedGraphicContents Type="BinaryContents"><Base64StringValue>{Convert.ToBase64String(png)}</Base64StringValue></EmbeddedGraphicContents>
              <ResizeMode>1</ResizeMode>
            </Item>
            """);

        var image = Assert.Single(template.Objects);
        Assert.Equal(ObjectType.Image, image.Type);
        Assert.Equal(Convert.ToBase64String(png), image.ImageDataBase64);
        Assert.Equal((3, 2), (image.ImagePixelWidth, image.ImagePixelHeight));
        Assert.Equal(150.8 - 12.7, image.XMm, 2);
        Assert.Equal(39.5, image.YMm, 2);
        Assert.Equal((12.7, 10.2), (image.WidthMm, image.HeightMm));
    }

    [Fact]
    public void GraphicWithoutPayload_IsNotConverted()
    {
        var template = Import("""
            <Item Type="GraphicDocumentItem">
              <Name>Missing</Name>
              <Geometry Type="RectGeometry"><Left>1000</Left><Top>1000</Top><Width>12700</Width><Height>10200</Height></Geometry>
              <GraphicFileName>C:\somewhere\symbol.wmf</GraphicFileName>
              <EmbeddedGraphicContents Type="BinaryContents"><Base64StringValue></Base64StringValue></EmbeddedGraphicContents>
            </Item>
            """);

        Assert.Empty(template.Objects);
    }

    [Fact]
    public void EmbeddedMetafile_IsRasterisedAtTheFrame()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var template = Import($"""
            <Item Type="GraphicDocumentItem">
              <Name>Clip</Name>
              <Geometry Type="RectGeometry"><Left>5000</Left><Top>5000</Top><Width>25400</Width><Height>12700</Height></Geometry>
              <EmbeddedGraphicContents Type="BinaryContents"><Base64StringValue>{Convert.ToBase64String(MinimalPlaceableWmf())}</Base64StringValue></EmbeddedGraphicContents>
              <ResizeMode>1</ResizeMode>
            </Item>
            """);

        var image = Assert.Single(template.Objects);
        Assert.Equal(ObjectType.Image, image.Type);
        // 25.4 x 12.7 mm at 300 DPI.
        Assert.Equal((300, 150), (image.ImagePixelWidth, image.ImagePixelHeight));
        var bytes = Convert.FromBase64String(image.ImageDataBase64);
        Assert.Equal(new byte[] { 0x89, (byte)'P', (byte)'N', (byte)'G' }, bytes[..4]);
    }

    private sealed class FixedMeasurer(double widthMm, double heightMm) : INiceLabel10TextMeasurer
    {
        public int Calls { get; private set; }

        public (double WidthMm, double HeightMm)? MeasureText(LabelObject item, string sampleText)
        {
            Calls++;
            return (widthMm, heightMm);
        }
    }

    /// <summary>A valid single-colour PNG of the given size (stored, uncompressed deflate).</summary>
    private static byte[] TinyPng(int width, int height)
    {
        using var stream = new MemoryStream();
        stream.Write(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });
        var ihdr = new byte[13];
        WriteBigEndian(ihdr, 0, width);
        WriteBigEndian(ihdr, 4, height);
        ihdr[8] = 8; // bit depth
        ihdr[9] = 0; // greyscale
        WriteChunk(stream, "IHDR", ihdr);
        var raw = new byte[(width + 1) * height];
        var deflate = new byte[raw.Length + 11];
        deflate[0] = 0x78;
        deflate[1] = 0x01;
        deflate[2] = 0x01;
        deflate[3] = (byte)(raw.Length & 0xFF);
        deflate[4] = (byte)(raw.Length >> 8);
        deflate[5] = (byte)(~raw.Length & 0xFF);
        deflate[6] = (byte)((~raw.Length >> 8) & 0xFF);
        raw.CopyTo(deflate, 7);
        WriteBigEndian(deflate, deflate.Length - 4, Adler32(raw));
        WriteChunk(stream, "IDAT", deflate);
        WriteChunk(stream, "IEND", Array.Empty<byte>());
        return stream.ToArray();
    }

    private static void WriteChunk(Stream stream, string type, byte[] data)
    {
        var header = new byte[4];
        WriteBigEndian(header, 0, data.Length);
        stream.Write(header);
        var typeBytes = Encoding.ASCII.GetBytes(type);
        stream.Write(typeBytes);
        stream.Write(data);
        var crcInput = new byte[typeBytes.Length + data.Length];
        typeBytes.CopyTo(crcInput, 0);
        data.CopyTo(crcInput, typeBytes.Length);
        var crc = new byte[4];
        WriteBigEndian(crc, 0, (int)Crc32(crcInput));
        stream.Write(crc);
    }

    private static void WriteBigEndian(byte[] buffer, int offset, int value)
    {
        buffer[offset] = (byte)(value >> 24);
        buffer[offset + 1] = (byte)(value >> 16);
        buffer[offset + 2] = (byte)(value >> 8);
        buffer[offset + 3] = (byte)value;
    }

    private static int Adler32(byte[] data)
    {
        uint a = 1, b = 0;
        foreach (var value in data)
        {
            a = (a + value) % 65521;
            b = (b + a) % 65521;
        }

        return (int)((b << 16) | a);
    }

    private static uint Crc32(byte[] data)
    {
        var crc = 0xFFFFFFFFu;
        foreach (var value in data)
        {
            crc ^= value;
            for (var i = 0; i < 8; i++)
            {
                crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xEDB88320u : crc >> 1;
            }
        }

        return ~crc;
    }

    /// <summary>
    /// A placeable WMF (Aldus header, 1000 units/inch, 1 x 0.5 inch) with one
    /// filled rectangle, written from the public WMF record layout.
    /// </summary>
    private static byte[] MinimalPlaceableWmf()
    {
        using var stream = new MemoryStream();
        var writer = new BinaryWriter(stream);
        // Placeable header.
        writer.Write(0x9AC6CDD7u);
        writer.Write((ushort)0); // handle
        writer.Write((short)0); // left
        writer.Write((short)0); // top
        writer.Write((short)1000); // right
        writer.Write((short)500); // bottom
        writer.Write((ushort)1000); // units per inch
        writer.Write(0u); // reserved
        ushort checksum = 0;
        var header = stream.ToArray();
        for (var i = 0; i < 20; i += 2)
        {
            checksum ^= (ushort)(header[i] | (header[i + 1] << 8));
        }

        writer.Write(checksum);

        // Standard header: memory metafile, header size 9 words, version 0x300.
        var records = new List<byte[]>
        {
            Record(0x020B, 0, 0), // SETWINDOWORG (y, x)
            Record(0x020C, 500, 1000), // SETWINDOWEXT (y, x)
            Record(0x041B, 400, 900, 100, 100), // RECTANGLE (bottom, right, top, left)
            Record(0x0000) // EOF
        };
        var recordWords = records.Sum(r => r.Length) / 2;
        var maxRecordWords = records.Max(r => r.Length) / 2;
        var fileWords = 9 + recordWords;
        writer.Write((ushort)1); // memory metafile
        writer.Write((ushort)9); // header size in words
        writer.Write((ushort)0x0300);
        writer.Write((uint)fileWords);
        writer.Write((ushort)0); // objects
        writer.Write((uint)maxRecordWords);
        writer.Write((ushort)0);
        foreach (var record in records)
        {
            writer.Write(record);
        }

        writer.Flush();
        return stream.ToArray();

        static byte[] Record(ushort function, params short[] parameters)
        {
            var size = 3 + parameters.Length;
            var bytes = new byte[size * 2];
            BitConverter.TryWriteBytes(bytes.AsSpan(0, 4), (uint)size);
            BitConverter.TryWriteBytes(bytes.AsSpan(4, 2), function);
            for (var i = 0; i < parameters.Length; i++)
            {
                BitConverter.TryWriteBytes(bytes.AsSpan(6 + i * 2, 2), parameters[i]);
            }

            return bytes;
        }
    }
}
