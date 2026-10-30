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
    private const string SubsetId = "eeeeeeee-2222-3333-4444-555555555555";
    private const string ScriptId = "f1f1f1f1-2222-3333-4444-555555555555";
    private const string ScriptCallId = "f2f2f2f2-2222-3333-4444-555555555555";
    private const string AscScriptId = "f3f3f3f3-2222-3333-4444-555555555555";
    private const string FieldId = "f4f4f4f4-2222-3333-4444-555555555555";
    private const string FieldAliasId = "f5f5f5f5-2222-3333-4444-555555555555";

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
            <Item Type="DBField">
              <Id>{FieldId}</Id>
              <Name>ProdCode</Name>
              <FieldName>ProdCode</FieldName>
            </Item>
            <Item Type="Variable">
              <Id>{FieldAliasId}</Id>
              <Name>Products.ProdCode</Name>
              <DefaultValue Type="StringContents"><StringValue>[{FieldId}]</StringValue><UserValue>[{FieldId}]</UserValue></DefaultValue>
              <DataSourceReference Type="SolutionDataSourceReference"><Id>{FieldId}</Id></DataSourceReference>
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
            <Item Type="SubsetFunction">
              <Id>{SubsetId}</Id>
              <Name>Short lot</Name>
              <Offset>2</Offset>
              <SubstringLength>5</SubstringLength>
              <Delimited>False</Delimited>
              <InputDataSourceReference Type="SolutionDataSourceReference"><Id>{LotId}</Id></InputDataSourceReference>
            </Item>
            <Item Type="TransferDataSyntaxFunction">
              <Id>{EnvelopeOtherFormatId}</Id>
              <Name>Format 01</Name>
              <Formats>
                <Item Type="TransferDataSyntaxFormat"><FormatId>1</FormatId><DataValues><Item Type="DataValue"><DataSourceReference Type="SolutionDataSourceReference"><Id>{LotId}</Id></DataSourceReference></Item></DataValues></Item>
              </Formats>
            </Item>
            <Item Type="ExecuteScriptFunction">
              <Id>{ScriptId}</Id>
              <Name>License plate data</Name>
              <Script>Result = &quot;UN&quot; &amp; lot &amp; [Qty] &amp; &quot;-&quot;&quot;A&quot;&quot;&quot;</Script>
              <InputDataSourceReferences>
                <Item Type="SolutionDataSourceReference"><Id>{LotId}</Id></Item>
                <Item Type="SolutionDataSourceReference"><Id>{QtyId}</Id></Item>
              </InputDataSourceReferences>
            </Item>
            <Item Type="ExecuteScriptFunction">
              <Id>{ScriptCallId}</Id>
              <Name>Upper lot</Name>
              <Script>Result = Len(Lot) - 1</Script>
              <InputDataSourceReferences><Item Type="SolutionDataSourceReference"><Id>{LotId}</Id></Item></InputDataSourceReferences>
            </Item>
            <Item Type="AscFactFunction">
              <Id>{AscScriptId}</Id>
              <Name>License plate</Name>
              <DataValues>
                <Item Type="AscFactDataValue"><AscFactIdentifierName>1J</AscFactIdentifierName><IsUsingShortTitle>False</IsUsingShortTitle><DataSourceReference Type="SolutionDataSourceReference"><Id>{ScriptId}</Id></DataSourceReference></Item>
              </DataValues>
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
    public void Gs1CompositionWithAutoCheckDigit_BecomesCheckDigitFormula()
    {
        var template = Import($"""
            <Item Type="TextDocumentItem">
              <Name>Gtin</Name>
              <Geometry Type="PositionGeometry"><X>1000</X><Y>1000</Y></Geometry>
              <FixedContents>(01)1234567890123</FixedContents>
              <DataSourceReference Type="SolutionDataSourceReference"><Id>{Gs1CheckDigitId}</Id></DataSourceReference>
            </Item>
            """);

        var text = Assert.Single(template.Objects);
        Assert.Equal("CONCAT(\"(01)\", GS1CHECKDIGIT(FIELD(\"?Lot\")))", text.BindingExpression);
        Assert.Equal("Lot", Assert.Single(template.PromptVariables).Name);
    }

    [Fact]
    public void SubsetOfVariable_BecomesMidFormula()
    {
        var template = Import($"""
            <Item Type="TextDocumentItem">
              <Name>Short</Name>
              <Geometry Type="PositionGeometry"><X>1000</X><Y>1000</Y></Geometry>
              <FixedContents>001</FixedContents>
              <DataSourceReference Type="SolutionDataSourceReference"><Id>{SubsetId}</Id></DataSourceReference>
            </Item>
            """);

        // Offset 2 (characters skipped) and length 5 → MID start 3.
        Assert.Equal("MID(FIELD(\"?Lot\"), \"3\", \"5\")", Assert.Single(template.Objects).BindingExpression);
    }

    [Fact]
    public void VariableRules_BecomePromptRules()
    {
        const string solution = """
            <?xml version="1.0" encoding="utf-8"?>
            <EuroPlus.NiceLabel Type="Solution">
              <Variables>
                <Item Type="Variable">
                  <Id>a1a1a1a1-2222-3333-4444-555555555555</Id>
                  <Name>Security Sign</Name>
                  <DefaultValue Type="PickListContents"><Values><Item>no</Item><Item>yes</Item></Values><SelectedValue>no</SelectedValue></DefaultValue>
                  <Constraints Type="DataSourceConstraints"><Length>10</Length></Constraints>
                </Item>
                <Item Type="Variable">
                  <Id>a2a2a2a2-2222-3333-4444-555555555555</Id>
                  <Name>Product_Weight</Name>
                  <DefaultValue Type="StringContents"><StringValue>12.9</StringValue></DefaultValue>
                  <Prefix>Weight: </Prefix>
                  <Suffix> kg</Suffix>
                </Item>
                <Item Type="Variable">
                  <Id>a3a3a3a3-2222-3333-4444-555555555555</Id>
                  <Name>PO</Name>
                  <DefaultValue Type="StringContents"><StringValue>123456</StringValue></DefaultValue>
                  <Padding>1</Padding>
                  <PaddingValue>0</PaddingValue>
                  <Constraints Type="DataSourceConstraints"><Length>16</Length></Constraints>
                </Item>
                <Item Type="Variable">
                  <Id>a4a4a4a4-2222-3333-4444-555555555555</Id>
                  <Name>GTIN</Name>
                  <DefaultValue Type="StringContents"><StringValue>03831234560087</StringValue></DefaultValue>
                  <AllowedCharactersForCustomFormat>0123456789</AllowedCharactersForCustomFormat>
                  <Constraints Type="DataSourceConstraints"><Length>14</Length><FixedLength>True</FixedLength></Constraints>
                </Item>
                <Item Type="Variable">
                  <Id>a5a5a5a5-2222-3333-4444-555555555555</Id>
                  <Name>Code</Name>
                  <DefaultValue Type="StringContents"><StringValue>AB-1</StringValue></DefaultValue>
                  <FormatId>11111111-aaaa-bbbb-cccc-000000000000</FormatId>
                  <AllowedCharactersForCustomFormat>ABC-0123456789</AllowedCharactersForCustomFormat>
                </Item>
                <Item Type="Variable">
                  <Id>a6a6a6a6-2222-3333-4444-555555555555</Id>
                  <Name>Phone</Name>
                  <DefaultValue Type="StringContents"><StringValue>0123 456</StringValue></DefaultValue>
                  <AllowedCharactersForCustomFormat>123456789:;ABC</AllowedCharactersForCustomFormat>
                </Item>
              </Variables>
            </EuroPlus.NiceLabel>
            """;
        var items = string.Concat(new[] { "a1a1a1a1", "a2a2a2a2", "a3a3a3a3", "a4a4a4a4", "a5a5a5a5", "a6a6a6a6" }.Select((id, index) => $"""
            <Item Type="TextDocumentItem">
              <Name>T{index}</Name>
              <Geometry Type="PositionGeometry"><X>1000</X><Y>{1000 + index * 6000}</Y></Geometry>
              <FontDescriptor Type="FontDescriptor"><Name>Arial</Name><Height>10</Height></FontDescriptor>
              <FixedContents>x</FixedContents>
              <DataSourceReference Type="SolutionDataSourceReference"><Id>{id}-2222-3333-4444-555555555555</Id></DataSourceReference>
            </Item>
            """));
        var template = NiceLabel10NlblImporter.CreateTemplate(Parse(Format(items), solution), "sample.nlbl");

        var prompts = template.PromptVariables.ToDictionary(prompt => prompt.Name);
        Assert.Equal(new[] { "no", "yes" }, prompts["Security Sign"].PickListValues);
        Assert.Equal("no", prompts["Security Sign"].DefaultValue);
        Assert.Equal(("Weight: ", " kg"), (prompts["Product_Weight"].Prefix, prompts["Product_Weight"].Suffix));
        Assert.Equal((ANLAbel.Core.Enums.PromptPadding.Left, "0", 16), (prompts["PO"].Padding, prompts["PO"].PadCharacter, prompts["PO"].MaxLength));
        Assert.True(prompts["GTIN"].FixedLength && prompts["GTIN"].DigitsOnly && prompts["GTIN"].AllowedCharacters.Length == 0);
        Assert.Equal("ABC-0123456789", prompts["Code"].AllowedCharacters);
        // Without a data format the stored set is a leftover NiceLabel does not apply.
        Assert.Equal(string.Empty, prompts["Phone"].AllowedCharacters);
    }

    [Fact]
    public void ConcatenatingScript_BecomesTheSameTemplate()
    {
        // GTL license plate: data identifier 1J + script "UN" & supplier & package number.
        var template = Import($"""
            <Item Type="BarcodeDocumentItem">
              <Name>License plate</Name>
              <Geometry Type="PositionGeometry"><X>10000</X><Y>38000</Y></Geometry>
              <BarcodeData Type="Code128BarcodeData"><BaseBarWidth>300</BaseBarWidth><ModuleHeight>10000</ModuleHeight></BarcodeData>
              <FixedContents>1JUN000125</FixedContents>
              <DataSourceReference Type="SolutionDataSourceReference"><Id>{AscScriptId}</Id></DataSourceReference>
            </Item>
            """);

        Assert.Equal("1JUN{?Lot}{?Qty}-\"A\"", Assert.Single(template.Objects).BindingExpression);
    }

    [Fact]
    public void VariableMirroringADatabaseField_BindsTheField()
    {
        var template = Import($"""
            <Item Type="BarcodeDocumentItem">
              <Name>Product</Name>
              <Geometry Type="PositionGeometry"><X>10000</X><Y>38000</Y></Geometry>
              <BarcodeData Type="Ean13BarcodeData"><BaseBarWidth>330</BaseBarWidth><ModuleHeight>15000</ModuleHeight></BarcodeData>
              <FixedContents>977000000001</FixedContents>
              <DataSourceReference Type="SolutionDataSourceReference"><Id>{FieldAliasId}</Id></DataSourceReference>
            </Item>
            """);

        var code = Assert.Single(template.Objects);
        Assert.Equal("{ProdCode}", code.BindingExpression);
        Assert.DoesNotContain(template.PromptVariables, prompt => prompt.Name == "Products.ProdCode");
    }

    [Fact]
    public void LinearFrameIsTheProductionWidthSoAnchoredCodesStayOnTheLabel()
    {
        // GS1-128 (02)+(37) at X 0.5 mm, anchored at its centre (AnchoringPoint 4)
        // at x = 50 mm: the digits pack two per character, so the frame is about
        // 95 mm and starts near 2.5 mm, not 160 mm wide from x = -30 mm.
        var template = Import("""
            <Item Type="BarcodeDocumentItem">
              <Name>Gs1</Name>
              <Geometry Type="PositionGeometry"><X>50000</X><Y>40000</Y><AnchoringPoint>4</AnchoringPoint></Geometry>
              <BarcodeData Type="GS1128BarcodeData"><BaseBarWidth>500</BaseBarWidth><ModuleHeight>20000</ModuleHeight></BarcodeData>
              <FixedContents>(02)03831234560087(37)0001</FixedContents>
            </Item>
            """);

        var code = Assert.Single(template.Objects);
        var modules = new ANLAbel.Barcode.Renderers.ZxingBarcodeRenderer().CountLinearModules(
            "(02)03831234560087(37)0001",
            ANLAbel.Barcode.Renderers.BarcodeType.Code128,
            ANLAbel.Barcode.Options.BarcodeRenderOptions.FromObject(code));
        Assert.NotNull(modules);
        Assert.Equal(modules!.Value * 0.5, code.WidthMm, 3);
        Assert.Equal(50 - code.WidthMm / 2, code.XMm, 3);
        Assert.True(code.XMm > 0 && code.WidthMm < 110, $"frame {code.XMm:0.#} + {code.WidthMm:0.#} mm");
    }

    [Fact]
    public void LinearCodeWithAPlaceholderIsSizedFromItsDesignValue()
    {
        // "??" is not Code 39, so NiceLabel's stored contents say nothing about the
        // printed width; the Qty prompt's default (12) does. Centre anchor at 50 mm.
        var template = Import($"""
            <Item Type="BarcodeDocumentItem">
              <Name>Quantity</Name>
              <Geometry Type="PositionGeometry"><X>50000</X><Y>40000</Y><AnchoringPoint>4</AnchoringPoint></Geometry>
              <BarcodeData Type="Code39BarcodeData"><BaseBarWidth>300</BaseBarWidth><ModuleHeight>10000</ModuleHeight></BarcodeData>
              <FixedContents>??</FixedContents>
              <DataSourceReference Type="SolutionDataSourceReference"><Id>{QtyId}</Id></DataSourceReference>
            </Item>
            """);

        var code = Assert.Single(template.Objects);
        var modules = new ANLAbel.Barcode.Renderers.ZxingBarcodeRenderer().CountLinearModules(
            "12",
            ANLAbel.Barcode.Renderers.BarcodeType.Code39,
            ANLAbel.Barcode.Options.BarcodeRenderOptions.FromObject(code));
        Assert.NotNull(modules);
        Assert.Equal(modules!.Value * 0.3, code.WidthMm, 2);
        Assert.Equal(50 - code.WidthMm / 2, code.XMm, 2);
    }

    [Fact]
    public void BoundAutomaticDataMatrixFrameHoldsTheStoredAndTheDesignValue()
    {
        // NiceLabel drew each code from its stored contents and grows it with the
        // data at the authored module; the frame here holds both.
        var template = Import($"""
            <Item Type="BarcodeDocumentItem">
              <Name>Short stored</Name>
              <Geometry Type="PositionGeometry"><X>5000</X><Y>5000</Y></Geometry>
              <BarcodeData Type="DatamatrixBarcodeData"><BaseBarWidth>381</BaseBarWidth><ModuleHeight>0</ModuleHeight><DatamatrixFormat>0</DatamatrixFormat></BarcodeData>
              <FixedContents>00</FixedContents>
              <DataSourceReference Type="SolutionDataSourceReference"><Id>{ConcatId}</Id></DataSourceReference>
            </Item>
            <Item Type="BarcodeDocumentItem">
              <Name>Long stored</Name>
              <Geometry Type="PositionGeometry"><X>50000</X><Y>5000</Y></Geometry>
              <BarcodeData Type="DatamatrixBarcodeData"><BaseBarWidth>381</BaseBarWidth><ModuleHeight>0</ModuleHeight><DatamatrixFormat>0</DatamatrixFormat></BarcodeData>
              <FixedContents>SUPPLIER DATA LINE 1 / LINE 2 / LINE 3</FixedContents>
              <DataSourceReference Type="SolutionDataSourceReference"><Id>{LotId}</Id></DataSourceReference>
            </Item>
            """);

        var codes = template.Objects.ToDictionary(item => item.Name);
        var row = ANLAbel.Core.Printing.JobVariableContract.ApplyForDesign(template, null, DateTimeOffset.Now, new Dictionary<string, string>())!;
        double Side(LabelObject code, string value)
        {
            var bare = ANLAbel.Core.Models.LabelObjectCloner.Clone(code);
            bare.QrQuietZoneModules = 0;
            var grid = new ANLAbel.Barcode.Renderers.ZxingBarcodeRenderer().CountMatrixModules(
                value,
                ANLAbel.Barcode.Renderers.BarcodeType.DataMatrix,
                ANLAbel.Barcode.Options.BarcodeRenderOptions.FromObject(bare));
            Assert.NotNull(grid);
            return grid!.Value.WidthModules * 0.381;
        }

        // Frames are kept to 0.01 mm.
        var caption = ANLAbel.Core.Expressions.BindingExpressionEvaluator.Evaluate(codes["Short stored"].BindingExpression, row);
        Assert.Equal(Side(codes["Short stored"], caption), codes["Short stored"].WidthMm, 2);
        Assert.True(codes["Short stored"].WidthMm > Side(codes["Short stored"], "00") + 0.5);
        Assert.Equal(Side(codes["Long stored"], "SUPPLIER DATA LINE 1 / LINE 2 / LINE 3"), codes["Long stored"].WidthMm, 2);
        Assert.Equal(codes["Long stored"].WidthMm, codes["Long stored"].HeightMm, 2);
        Assert.Equal((50.0, 5.0), (codes["Long stored"].XMm, codes["Long stored"].YMm));
    }

    [Fact]
    public void ScriptCallingTextFunctions_BecomesAFormula()
    {
        const string solution = """
            <?xml version="1.0" encoding="utf-8"?>
            <EuroPlus.NiceLabel Type="Solution">
              <Variables>
                <Item Type="Variable">
                  <Id>c1c1c1c1-2222-3333-4444-555555555555</Id>
                  <Name>Ship Date</Name>
                  <DefaultValue Type="StringContents"><StringValue>jan31</StringValue></DefaultValue>
                </Item>
                <Item Type="Variable">
                  <Id>c2c2c2c2-2222-3333-4444-555555555555</Id>
                  <Name>CRC16</Name>
                  <DefaultValue Type="StringContents"><StringValue>A1B2</StringValue></DefaultValue>
                </Item>
              </Variables>
              <Functions>
                <Item Type="ExecuteScriptFunction">
                  <Id>c3c3c3c3-2222-3333-4444-555555555555</Id>
                  <Name>Dated</Name>
                  <Script>Result = &quot;D:&quot; &amp; UCase([Ship Date]) &amp; Right(crc16, 2)</Script>
                  <InputDataSourceReferences>
                    <Item Type="SolutionDataSourceReference"><Id>c1c1c1c1-2222-3333-4444-555555555555</Id></Item>
                    <Item Type="SolutionDataSourceReference"><Id>c2c2c2c2-2222-3333-4444-555555555555</Id></Item>
                  </InputDataSourceReferences>
                </Item>
              </Functions>
            </EuroPlus.NiceLabel>
            """;
        var template = NiceLabel10NlblImporter.CreateTemplate(Parse(Format("""
            <Item Type="BarcodeDocumentItem">
              <Name>Dated</Name>
              <Geometry Type="PositionGeometry"><X>10000</X><Y>38000</Y></Geometry>
              <BarcodeData Type="Code128BarcodeData"><BaseBarWidth>300</BaseBarWidth><ModuleHeight>10000</ModuleHeight></BarcodeData>
              <FixedContents>D:JAN31B2</FixedContents>
              <DataSourceReference Type="SolutionDataSourceReference"><Id>c3c3c3c3-2222-3333-4444-555555555555</Id></DataSourceReference>
            </Item>
            """), solution), "sample.nlbl");

        var code = Assert.Single(template.Objects);
        Assert.Equal("CONCAT(\"D:\", UPPER(FIELD(\"?Ship Date\")), RIGHT(FIELD(\"?CRC16\"), \"2\"))", code.BindingExpression);
        var row = new Dictionary<string, string> { ["?Ship Date"] = "jan31", ["?CRC16"] = "A1B2" };
        Assert.Equal("D:JAN31B2", ANLAbel.Core.Expressions.Formulas.FormulaBindingEvaluator.Evaluate(code.BindingExpression, row).Value);
    }

    [Fact]
    public void ScriptThatIsOneCall_BecomesThatFormula()
    {
        const string solution = """
            <?xml version="1.0" encoding="utf-8"?>
            <EuroPlus.NiceLabel Type="Solution">
              <Variables>
                <Item Type="Variable">
                  <Id>d1d1d1d1-2222-3333-4444-555555555555</Id>
                  <Name>Lot</Name>
                  <DefaultValue Type="StringContents"><StringValue>l-7</StringValue></DefaultValue>
                </Item>
              </Variables>
              <Functions>
                <Item Type="ExecuteScriptFunction">
                  <Id>d2d2d2d2-2222-3333-4444-555555555555</Id>
                  <Name>Upper lot</Name>
                  <Script>Result = UCase(Lot)</Script>
                  <InputDataSourceReferences>
                    <Item Type="SolutionDataSourceReference"><Id>d1d1d1d1-2222-3333-4444-555555555555</Id></Item>
                  </InputDataSourceReferences>
                </Item>
              </Functions>
            </EuroPlus.NiceLabel>
            """;
        var template = NiceLabel10NlblImporter.CreateTemplate(Parse(Format("""
            <Item Type="TextDocumentItem">
              <Name>Lot</Name>
              <Geometry Type="PositionGeometry"><X>1000</X><Y>1000</Y></Geometry>
              <FontDescriptor Type="FontDescriptor"><Name>Arial</Name><Height>10</Height></FontDescriptor>
              <FixedContents>L-7</FixedContents>
              <DataSourceReference Type="SolutionDataSourceReference"><Id>d2d2d2d2-2222-3333-4444-555555555555</Id></DataSourceReference>
            </Item>
            """), solution), "sample.nlbl");

        var text = Assert.Single(template.Objects);
        Assert.Equal("UPPER(FIELD(\"?Lot\"))", text.BindingExpression);
        Assert.True(ANLAbel.Core.Expressions.Formulas.FormulaBindingEvaluator.LooksLikeFormula(text.BindingExpression));
        var row = new Dictionary<string, string> { ["?Lot"] = "l-7" };
        Assert.Equal("L-7", ANLAbel.Core.Expressions.Formulas.FormulaBindingEvaluator.Evaluate(text.BindingExpression, row).Value);
    }

    [Fact]
    public void ScriptWithArithmetic_IsNotConverted()
    {
        var template = Import($"""
            <Item Type="BarcodeDocumentItem">
              <Name>Upper</Name>
              <Geometry Type="PositionGeometry"><X>10000</X><Y>38000</Y></Geometry>
              <BarcodeData Type="Code128BarcodeData"><BaseBarWidth>300</BaseBarWidth><ModuleHeight>10000</ModuleHeight></BarcodeData>
              <FixedContents>L-001</FixedContents>
              <DataSourceReference Type="SolutionDataSourceReference"><Id>{ScriptCallId}</Id></DataSourceReference>
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
    public void Code39Details_MapCheckDigitRatioAndGap()
    {
        var template = Import("""
            <Item Type="BarcodeDocumentItem">
              <Name>Serial</Name>
              <Geometry Type="PositionGeometry"><X>10000</X><Y>38000</Y></Geometry>
              <BarcodeData Type="Code39BarcodeData"><BaseBarWidth>300</BaseBarWidth><ModuleHeight>10000</ModuleHeight><Ratio>2.5</Ratio><HasCheckDigit>True</HasCheckDigit><DisplayCheckDigit>False</DisplayCheckDigit><InterCharacterGap>2</InterCharacterGap></BarcodeData>
              <FixedContents>ABC123</FixedContents>
            </Item>
            """);

        var code = Assert.Single(template.Objects);
        Assert.Equal(BarcodeCheckDigitPolicy.Generate, code.BarcodeCheckDigitPolicy);
        Assert.False(code.BarcodeHriShowCheckDigit);
        Assert.Equal(Code39WideNarrowRatio.Ratio2_5, code.Code39WideNarrowRatio);
        Assert.Equal(2, code.Code39IntercharacterGapModules);
    }

    [Fact]
    public void DottedLineAndRoundedRectangle_MapStrokeStyleAndRadius()
    {
        var template = Import("""
            <Item Type="LineDocumentItem">
              <Name>Rule</Name>
              <Geometry Type="PointGeometry"><StartPointX>5000</StartPointX><StartPointY>45000</StartPointY><EndPointX>95000</EndPointX><EndPointY>45000</EndPointY></Geometry>
              <Thickness>250</Thickness>
              <LineStyle>4</LineStyle>
            </Item>
            <Item Type="RectangleDocumentItem">
              <Name>Box</Name>
              <Geometry Type="RectGeometry"><Left>70000</Left><Top>20000</Top><Width>20000</Width><Height>10000</Height></Geometry>
              <Thickness>500</Thickness>
              <LineStyle>2</LineStyle>
              <Radius>1500</Radius>
            </Item>
            """);

        Assert.Equal(2, template.Objects.Count);
        Assert.Equal(OutlineStyle.Dot, template.Objects[0].Style.OutlineStyle);
        Assert.Equal(OutlineStyle.Dash, template.Objects[1].Style.OutlineStyle);
        Assert.Equal(1.5, template.Objects[1].Style.CornerRadiusMm, 3);
    }

    [Theory]
    [InlineData("", FillStyle.None)]
    [InlineData("<FillStyle>0</FillStyle>", FillStyle.None)]
    [InlineData("<FillStyle>1</FillStyle>", FillStyle.None)]
    [InlineData("<FillStyle>5</FillStyle>", FillStyle.None)]
    [InlineData("<FillStyle>2</FillStyle><FillColor>FF102030</FillColor>", FillStyle.Solid)]
    public void RectangleFill_IsOpenUnlessSolid(string fill, FillStyle expected)
    {
        // A NiceLabel box without a solid fill leaves what lies under it visible.
        var template = Import($"""
            <Item Type="RectangleDocumentItem">
              <Name>Box</Name>
              <Geometry Type="RectGeometry"><Left>2000</Left><Top>2000</Top><Width>90000</Width><Height>50000</Height></Geometry>
              <Thickness>400</Thickness>
              {fill}
            </Item>
            """);

        var box = Assert.Single(template.Objects);
        Assert.Equal(expected, box.Style.FillStyle);
        if (expected == FillStyle.Solid)
        {
            Assert.Equal("#FF102030", box.Style.FillColor);
        }
    }

    [Fact]
    public void SheetLayout_UsesTheLabelElementSizeNotThePage()
    {
        var document = Parse("""
            <?xml version="1.0" encoding="utf-8"?>
            <EuroPlus.NiceLabel Type="Format">
              <Name>Sheet</Name>
              <Media Type="FormatMedia"><Width>210000</Width><Height>297000</Height></Media>
              <PageLayout Type="WebFormatLayout"><ElementWidth>101600</ElementWidth><ElementHeight>50800</ElementHeight><HorizontalCount>1</HorizontalCount><VerticalCount>5</VerticalCount></PageLayout>
              <DocumentItems />
            </EuroPlus.NiceLabel>
            """);

        Assert.Equal((101.6, 50.8), (document.WidthMm, document.HeightMm));
    }

    [Fact]
    public void SingleLayoutWithoutElementSize_FallsBackToMedia()
    {
        var document = Parse(Format(string.Empty));

        Assert.Equal((100.0, 60.0), (document.WidthMm, document.HeightMm));
    }

    [Fact]
    public void Reader_CollectsVariableFacts()
    {
        var document = Parse(Format(string.Empty));

        // Three variables, the date-add, both GS1 functions, three data
        // identifier functions, the format-06 envelope, the subset and the
        // concatenating script, a database field and the variable mirroring
        // it; the other-format envelope and the script doing arithmetic are
        // not registered.
        Assert.Equal(15, document.VariablesById.Count);
        Assert.False(document.VariablesById.ContainsKey(EnvelopeOtherFormatId));
        Assert.False(document.VariablesById.ContainsKey(ScriptCallId));
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
    public void UnanchoredUnrotatedText_KeepsStoredPointAndTakesItsMeasuredFrame()
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
        Assert.Equal(1, measurer.Calls);
        Assert.Equal((5.0, 7.0, 0), (text.XMm, text.YMm, text.Rotation));
        // Content-owned bounds: the frame starts at the natural size, not
        // at the new-object default.
        Assert.Equal((40.0, 8.0), (text.WidthMm, text.HeightMm));
        Assert.Equal(TextSizingMode.AutoFit, text.Style.TextSizing);
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
    public void ScaledPositionText_CarriesFontScaleAtScaledWidth()
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
        Assert.Equal(TextSizingMode.AutoFit, text.Style.TextSizing);
        Assert.Equal(79, text.Style.FontScalePercent, 2);
        Assert.Equal(40 * 0.79, text.WidthMm, 2);
        Assert.Equal(8, text.HeightMm, 2);
        // RightTop anchor: the scaled width ends at the stored X.
        Assert.Equal(50 - 40 * 0.79, text.XMm, 2);
        Assert.Equal(ObjectAnchorPoint.TopRight, text.AnchorPoint);
    }

    [Fact]
    public void MaskedText_KeepsTheVendorContentMask()
    {
        var template = Import("""
            <Item Type="TextDocumentItem">
              <Name>Masked</Name>
              <Geometry Type="PositionGeometry"><X>5000</X><Y>7000</Y></Geometry>
              <FixedContents>1J</FixedContents>
              <HasMaskedContents>True</HasMaskedContents>
              <ContentsMask>(**)</ContentsMask>
            </Item>
            """, new FixedMeasurer(40, 8));

        var text = Assert.Single(template.Objects);
        Assert.Equal("(**)", text.ContentMask);
        Assert.Equal("*", text.ContentMaskCharacter);
    }

    [Fact]
    public void ExpandedPositionText_WidensLikeNiceLabel()
    {
        var template = Import("""
            <Item Type="TextDocumentItem">
              <Name>Wide</Name>
              <Geometry Type="PositionGeometry"><X>5000</X><Y>7000</Y></Geometry>
              <ScaleFactor>104</ScaleFactor>
              <FixedContents>WIDE</FixedContents>
            </Item>
            """, new FixedMeasurer(40, 8));

        var text = Assert.Single(template.Objects);
        Assert.Equal(104, text.Style.FontScalePercent, 2);
        Assert.Equal(40 * 1.04, text.WidthMm, 2);
    }

    [Fact]
    public void FramedText_VariableHeight_BecomesAdjustHeight()
    {
        var template = Import("""
            <Item Type="TextDocumentItem">
              <Name>Grow</Name>
              <Geometry Type="RectGeometry"><Left>5000</Left><Top>5000</Top><Width>40000</Width><Height>10000</Height></Geometry>
              <TextType>2</TextType>
              <BestFit>3</BestFit>
              <FixedContents>Grow with me</FixedContents>
            </Item>
            """);

        var box = Assert.Single(template.Objects);
        Assert.Equal(ObjectType.TextBox, box.Type);
        Assert.Equal(TextSizingMode.AdjustHeight, box.Style.TextSizing);
        Assert.Equal(40, box.WidthMm, 2);
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
        // The stored text below the 10 mm bars belongs to the object NiceLabel centres.
        Assert.True(code.HeightMm > 10);
        Assert.Equal(30 - code.HeightMm / 2, code.YMm, 2);
    }

    [Fact]
    public void DateMovedByAnotherSource_BecomesADateAddFormula()
    {
        // UDI samples: Expiry Date = Mfg Date (today) + "Expiry (days)", a prompt.
        const string solution = """
            <?xml version="1.0" encoding="utf-8"?>
            <EuroPlus.NiceLabel Type="Solution">
              <Variables>
                <Item Type="Variable">
                  <Id>f1f1f1f1-2222-3333-4444-555555555555</Id>
                  <Name>Mfg Date</Name>
                  <DefaultValue Type="SystemDateTimeContents"><OutputFormat>yyyy-MM-dd</OutputFormat></DefaultValue>
                </Item>
                <Item Type="Variable">
                  <Id>f2f2f2f2-2222-3333-4444-555555555555</Id>
                  <Name>Expiry (days)</Name>
                  <DefaultValue Type="StringContents"><StringValue>100</StringValue></DefaultValue>
                </Item>
              </Variables>
              <Functions>
                <Item Type="DateAddFunction">
                  <Id>f3f3f3f3-2222-3333-4444-555555555555</Id>
                  <Name>Expiry Date</Name>
                  <Day Type="DataValue"><DataSourceReference Type="SolutionDataSourceReference"><Id>f2f2f2f2-2222-3333-4444-555555555555</Id></DataSourceReference></Day>
                  <Month Type="DataValue"><FixedValue Type="IntegerContents" /></Month>
                  <Year Type="DataValue"><FixedValue Type="IntegerContents" /></Year>
                  <InputDataSourceReference Type="SolutionDataSourceReference"><Id>f1f1f1f1-2222-3333-4444-555555555555</Id></InputDataSourceReference>
                  <OutputFormat>yyMMdd</OutputFormat>
                </Item>
              </Functions>
            </EuroPlus.NiceLabel>
            """;
        var template = NiceLabel10NlblImporter.CreateTemplate(Parse(Format("""
            <Item Type="TextDocumentItem">
              <Name>Expiry</Name>
              <Geometry Type="PositionGeometry"><X>1000</X><Y>1000</Y></Geometry>
              <FontDescriptor Type="FontDescriptor"><Name>Arial</Name><Height>10</Height></FontDescriptor>
              <FixedContents>EXP 260209</FixedContents>
              <Contents Type="ExtendedDataValue">
                <FixedValue Type="StringContents"><StringValue>EXP [f3f3f3f3-2222-3333-4444-555555555555]</StringValue></FixedValue>
                <DataSourceReferences><Item Type="SolutionDataSourceReference"><Id>f3f3f3f3-2222-3333-4444-555555555555</Id></Item></DataSourceReferences>
              </Contents>
            </Item>
            """), solution), "sample.nlbl");

        var expiry = Assert.Single(template.Objects);
        Assert.Equal("CONCAT(\"EXP \", DATEFORMAT(DATEADD(FIELD(\"#NOW:yyyy-MM-dd\"), FIELD(\"?Expiry (days)\"), \"d\", \"yyyy-MM-dd\"), \"yyMMdd\"))", expiry.BindingExpression);
        var prompt = Assert.Single(template.PromptVariables);
        Assert.Equal(("Expiry (days)", "100"), (prompt.Name, prompt.DefaultValue));

        // At print the job day moves by the answer.
        IReadOnlyDictionary<string, string>?[] rows = [null];
        var jobTime = new DateTimeOffset(2026, 10, 31, 9, 0, 0, TimeSpan.FromHours(7));
        Assert.True(ANLAbel.Core.Printing.JobVariableContract.TryApply(template, rows, jobTime, new Dictionary<string, string> { ["Expiry (days)"] = "100" }, out var applied, out var error), error);
        Assert.Equal("EXP 270208", ANLAbel.Core.Expressions.Formulas.FormulaBindingEvaluator.Evaluate(expiry.BindingExpression, applied[0]!).Value);
    }

    [Fact]
    public void PromptedDates_BecomeDatePrompts()
    {
        const string solution = """
            <?xml version="1.0" encoding="utf-8"?>
            <EuroPlus.NiceLabel Type="Solution">
              <Variables>
                <Item Type="Variable">
                  <Id>e1e1e1e1-2222-3333-4444-555555555555</Id>
                  <Name>Ship Date</Name>
                  <DefaultValue Type="DateTimeContents">
                    <DateTimeValue>2958465.99999999</DateTimeValue>
                    <DisplayType>1</DisplayType>
                    <InputFormat>d. MM. yyyy</InputFormat>
                    <OutputFormat>yyyyMMdd</OutputFormat>
                    <UserValue>.</UserValue>
                  </DefaultValue>
                </Item>
                <Item Type="Variable">
                  <Id>e2e2e2e2-2222-3333-4444-555555555555</Id>
                  <Name>Pack Date</Name>
                  <DefaultValue Type="DateTimeContents">
                    <DateTimeValue>40620</DateTimeValue>
                    <InputFormat>yyMMdd</InputFormat>
                    <OutputFormat>dd.MM.yyyy</OutputFormat>
                    <UserValue>110318</UserValue>
                  </DefaultValue>
                </Item>
              </Variables>
            </EuroPlus.NiceLabel>
            """;
        var items = string.Concat(new[] { "e1e1e1e1", "e2e2e2e2" }.Select((id, index) => $"""
            <Item Type="TextDocumentItem">
              <Name>D{index}</Name>
              <Geometry Type="PositionGeometry"><X>1000</X><Y>{1000 + index * 6000}</Y></Geometry>
              <FontDescriptor Type="FontDescriptor"><Name>Arial</Name><Height>10</Height></FontDescriptor>
              <FixedContents>x</FixedContents>
              <DataSourceReference Type="SolutionDataSourceReference"><Id>{id}-2222-3333-4444-555555555555</Id></DataSourceReference>
            </Item>
            """));
        var template = NiceLabel10NlblImporter.CreateTemplate(Parse(Format(items), solution), "sample.nlbl");

        var prompts = template.PromptVariables.ToDictionary(prompt => prompt.Name);
        Assert.Equal(("yyyyMMdd", "d. MM. yyyy", ""), (prompts["Ship Date"].DateFormat, prompts["Ship Date"].DateInputFormat, prompts["Ship Date"].DefaultValue));
        Assert.Equal(("dd.MM.yyyy", "2011-03-18"), (prompts["Pack Date"].DateFormat, prompts["Pack Date"].DefaultValue));
        Assert.Equal("18.03.2011", ANLAbel.Core.Printing.JobVariableContract.FormatPromptValue(prompts["Pack Date"], prompts["Pack Date"].DefaultValue));
        // The text frame is sized from the date as it prints (here the default in dd.MM.yyyy).
        var em = 10 * 25.4 / 72;
        Assert.Equal("18.03.2011".Length * 0.55 * em, template.Objects.Single(item => item.Name == "D1").WidthMm, 2);
    }

    [Fact]
    public void LayerAndItemConditions_BecomeVisibilityFormulas()
    {
        const string solution = """
            <?xml version="1.0" encoding="utf-8"?>
            <EuroPlus.NiceLabel Type="Solution">
              <Variables>
                <Item Type="Variable">
                  <Id>b1b1b1b1-2222-3333-4444-555555555555</Id>
                  <Name>Barcode type</Name>
                  <DefaultValue Type="StringContents"><StringValue>Code 128</StringValue></DefaultValue>
                </Item>
              </Variables>
            </EuroPlus.NiceLabel>
            """;
        const string condition = """
            <IsVisibilityConditional>True</IsVisibilityConditional>
            <VisibilityConditionLeftValue Type="DataValue"><DataSourceReference Type="SolutionDataSourceReference"><Id>b1b1b1b1-2222-3333-4444-555555555555</Id></DataSourceReference></VisibilityConditionLeftValue>
            <VisibilityConditionRightValue Type="DataValue"><FixedValue Type="StringContents"><StringValue>QR</StringValue></FixedValue></VisibilityConditionRightValue>
            """;
        var items = $"""
            <Layers>
              <Layer Type="Layer">
                <Name>QR variant</Name>
                <ChildReferences>
                  <Item Type="DocumentItemReference"><Id>c0c0c0c0-0000-0000-0000-000000000001</Id></Item>
                  <Item Type="DocumentItemReference"><Id>c0c0c0c0-0000-0000-0000-000000000003</Id></Item>
                </ChildReferences>
                <ZOrder>10000</ZOrder>
                {condition}
              </Layer>
            </Layers>
            <Item Type="TextDocumentItem">
              <Id>c0c0c0c0-0000-0000-0000-000000000001</Id>
              <Name>In layer</Name>
              <Geometry Type="PositionGeometry"><X>1000</X><Y>1000</Y></Geometry>
              <FontDescriptor Type="FontDescriptor"><Name>Arial</Name><Height>10</Height></FontDescriptor>
              <FixedContents>QR</FixedContents>
            </Item>
            <Item Type="TextDocumentItem">
              <Id>c0c0c0c0-0000-0000-0000-000000000002</Id>
              <Name>Unequal</Name>
              <Geometry Type="PositionGeometry"><X>1000</X><Y>8000</Y></Geometry>
              <FontDescriptor Type="FontDescriptor"><Name>Arial</Name><Height>10</Height></FontDescriptor>
              <FixedContents>Linear</FixedContents>
              {condition}
              <VisibilityConditionOperator>1</VisibilityConditionOperator>
            </Item>
            <Item Type="TextDocumentItem">
              <Id>c0c0c0c0-0000-0000-0000-000000000003</Id>
              <Name>Both</Name>
              <Geometry Type="PositionGeometry"><X>1000</X><Y>16000</Y></Geometry>
              <FontDescriptor Type="FontDescriptor"><Name>Arial</Name><Height>10</Height></FontDescriptor>
              <FixedContents>Both</FixedContents>
              {condition}
            </Item>
            """;
        var template = NiceLabel10NlblImporter.CreateTemplate(Parse(Format(items), solution), "sample.nlbl");

        var byName = template.Objects.ToDictionary(item => item.Name);
        const string isQr = "EQUALS(FIELD(\"?Barcode type\"), \"QR\")";
        Assert.Equal(isQr, byName["In layer"].VisibilityCondition);
        Assert.Equal("NOT(" + isQr + ")", byName["Unequal"].VisibilityCondition);
        Assert.Equal("AND(" + isQr + ", " + isQr + ")", byName["Both"].VisibilityCondition);
        var prompt = Assert.Single(template.PromptVariables);
        Assert.Equal(("Barcode type", "Code 128"), (prompt.Name, prompt.DefaultValue));

        // With the stored default the linear variant prints and the QR layer does not.
        var row = ANLAbel.Core.Printing.JobVariableContract.ApplyForDesign(template, null, DateTimeOffset.Now, new Dictionary<string, string>());
        Assert.False(ANLAbel.Core.Data.VisibilityConditionContract.IsShown(byName["In layer"].VisibilityCondition, row));
        Assert.True(ANLAbel.Core.Data.VisibilityConditionContract.IsShown(byName["Unequal"].VisibilityCondition, row));
    }

    [Fact]
    public void TextBoundToADatabaseField_IsSizedFromTheFieldSample()
    {
        // The bound item stores the placeholder "Text"; NiceLabel shows the field's
        // stored sample, so the imported frame is that sample's natural size.
        const string solution = """
            <?xml version="1.0" encoding="utf-8"?>
            <EuroPlus.NiceLabel Type="Solution">
              <Databases>
                <Item Type="OleDb">
                  <DataSetCollection>
                    <Item Type="OleDBDatabaseDataSet">
                      <FieldCollection>
                        <Item Type="DBField">
                          <Id>f1f1f1f1-2222-3333-4444-555555555555</Id>
                          <Name>Sheet1$.MPN</Name>
                          <SampleValue Type="StringContents"><StringValue>SEMCG-MR-00135</StringValue></SampleValue>
                          <FieldName>MPN</FieldName>
                        </Item>
                      </FieldCollection>
                    </Item>
                  </DataSetCollection>
                </Item>
              </Databases>
            </EuroPlus.NiceLabel>
            """;
        var template = NiceLabel10NlblImporter.CreateTemplate(Parse(Format("""
            <Item Type="TextDocumentItem">
              <Name>Bound</Name>
              <Geometry Type="PositionGeometry"><X>1000</X><Y>1000</Y></Geometry>
              <FontDescriptor Type="FontDescriptor"><Name>Arial</Name><Height>10</Height></FontDescriptor>
              <FixedContents>Text</FixedContents>
              <DataSourceReference Type="SolutionDataSourceReference"><Id>f1f1f1f1-2222-3333-4444-555555555555</Id></DataSourceReference>
            </Item>
            <Item Type="TextDocumentItem">
              <Name>Composed</Name>
              <Geometry Type="PositionGeometry"><X>1000</X><Y>9000</Y></Geometry>
              <FontDescriptor Type="FontDescriptor"><Name>Arial</Name><Height>10</Height></FontDescriptor>
              <FixedContents>MPN:</FixedContents>
              <Contents Type="ExtendedDataValue">
                <FixedValue Type="StringContents"><StringValue>MPN: [f1f1f1f1-2222-3333-4444-555555555555]</StringValue></FixedValue>
                <DataSourceReferences><Item Type="SolutionDataSourceReference"><Id>f1f1f1f1-2222-3333-4444-555555555555</Id></Item></DataSourceReferences>
              </Contents>
            </Item>
            """), solution), "sample.nlbl");

        var byName = template.Objects.ToDictionary(item => item.Name);
        Assert.Equal("{MPN}", byName["Bound"].BindingExpression);
        // The importer's font-metric estimate without a measurer: 0.55 em per character.
        var em = 10 * 25.4 / 72;
        Assert.Equal("SEMCG-MR-00135".Length * 0.55 * em, byName["Bound"].WidthMm, 2);
        Assert.Equal("MPN: SEMCG-MR-00135".Length * 0.55 * em, byName["Composed"].WidthMm, 2);
    }

    [Fact]
    public void InverseText_GetsAnInverseAreaAbove_AndLockedPositionStays()
    {
        var template = Import("""
            <Item Type="TextDocumentItem">
              <Name>Title</Name>
              <Geometry Type="PositionGeometry"><X>3000</X><Y>3000</Y></Geometry>
              <FontDescriptor Type="FontDescriptor"><Name>Arial</Name><Height>32</Height></FontDescriptor>
              <FixedContents>LABEL 1</FixedContents>
              <InverseText>True</InverseText>
              <PositionLocked>True</PositionLocked>
              <ZOrder>2</ZOrder>
            </Item>
            <Item Type="TextDocumentItem">
              <Name>Note</Name>
              <Geometry Type="PositionGeometry"><X>3000</X><Y>30000</Y></Geometry>
              <FontDescriptor Type="FontDescriptor"><Name>Arial</Name><Height>10</Height></FontDescriptor>
              <FixedContents>note</FixedContents>
              <ZOrder>5</ZOrder>
            </Item>
            """);

        var title = template.Objects.Single(item => item.Name == "Title");
        var inverse = template.Objects.Single(item => item.Type == ObjectType.Inverse);
        Assert.True(title.IsLocked);
        Assert.False(template.Objects.Single(item => item.Name == "Note").IsLocked);
        Assert.Equal((title.XMm, title.YMm, title.WidthMm, title.HeightMm), (inverse.XMm, inverse.YMm, inverse.WidthMm, inverse.HeightMm));
        // Right above the text and below the later note.
        Assert.Equal(title.ZIndex + 1, inverse.ZIndex);
        Assert.True(inverse.ZIndex < template.Objects.Single(item => item.Name == "Note").ZIndex);
    }

    [Fact]
    public void GroupedItems_ImportAsAGroup()
    {
        var document = Parse("""
            <?xml version="1.0" encoding="utf-8"?>
            <EuroPlus.NiceLabel Type="Format">
              <Name>Groups</Name>
              <Media Type="FormatMedia"><Width>100000</Width><Height>60000</Height></Media>
              <DocumentItems>
                <Item Type="TextDocumentItem">
                  <Id>91919191-0000-4000-8000-000000000001</Id>
                  <Name>Caption</Name>
                  <Geometry Type="PositionGeometry"><X>1000</X><Y>1000</Y></Geometry>
                  <FontDescriptor Type="FontDescriptor"><Name>Arial</Name><Height>10</Height></FontDescriptor>
                  <FixedContents>Lot</FixedContents>
                </Item>
                <Item Type="TextDocumentItem">
                  <Id>91919191-0000-4000-8000-000000000002</Id>
                  <Name>Value</Name>
                  <Geometry Type="PositionGeometry"><X>20000</X><Y>1000</Y></Geometry>
                  <FontDescriptor Type="FontDescriptor"><Name>Arial</Name><Height>10</Height></FontDescriptor>
                  <FixedContents>L-1</FixedContents>
                </Item>
                <Item Type="TextDocumentItem">
                  <Id>91919191-0000-4000-8000-000000000003</Id>
                  <Name>Alone</Name>
                  <Geometry Type="PositionGeometry"><X>1000</X><Y>20000</Y></Geometry>
                  <FontDescriptor Type="FontDescriptor"><Name>Arial</Name><Height>10</Height></FontDescriptor>
                  <FixedContents>x</FixedContents>
                </Item>
              </DocumentItems>
              <GroupedItems>
                <Item Type="SolutionItemReferenceCollectionWrapper">
                  <SolutionItemReferences>
                    <Item Type="SolutionItemReference"><Id>91919191-0000-4000-8000-000000000001</Id></Item>
                    <Item Type="SolutionItemReference"><Id>91919191-0000-4000-8000-000000000002</Id></Item>
                  </SolutionItemReferences>
                </Item>
              </GroupedItems>
            </EuroPlus.NiceLabel>
            """);
        var template = NiceLabel10NlblImporter.CreateTemplate(document, "sample.nlbl");

        var byName = template.Objects.ToDictionary(item => item.Name);
        Assert.False(string.IsNullOrEmpty(byName["Caption"].GroupId));
        Assert.Equal(byName["Caption"].GroupId, byName["Value"].GroupId);
        Assert.True(string.IsNullOrEmpty(byName["Alone"].GroupId));
    }

    [Fact]
    public void Objects_StackInTheStoredZOrder_AndKeepNotPrintable()
    {
        // The file lists the white caption before the black band it sits on;
        // NiceLabel draws by ZOrder, so the caption stays on top.
        var template = Import("""
            <Item Type="TextDocumentItem">
              <Name>Caption</Name>
              <Geometry Type="PositionGeometry"><X>12000</X><Y>12000</Y></Geometry>
              <FontDescriptor Type="FontDescriptor"><Name>Arial</Name><Height>10</Height></FontDescriptor>
              <Color>FFFFFFFF</Color>
              <FixedContents>WHITE</FixedContents>
              <ZOrder>7</ZOrder>
            </Item>
            <Item Type="RectangleDocumentItem">
              <Name>Band</Name>
              <Geometry Type="RectGeometry"><Left>10000</Left><Top>10000</Top><Width>40000</Width><Height>10000</Height></Geometry>
              <FillStyle>2</FillStyle>
              <FillColor>FF000000</FillColor>
              <ZOrder>3</ZOrder>
            </Item>
            <Item Type="TextDocumentItem">
              <Name>Guide</Name>
              <Geometry Type="PositionGeometry"><X>12000</X><Y>30000</Y></Geometry>
              <FontDescriptor Type="FontDescriptor"><Name>Arial</Name><Height>8</Height></FontDescriptor>
              <FixedContents>screen only</FixedContents>
              <ZOrder>5</ZOrder>
              <IsPrintable>False</IsPrintable>
            </Item>
            """);

        var byName = template.Objects.ToDictionary(item => item.Name);
        Assert.True(byName["Band"].ZIndex < byName["Guide"].ZIndex);
        Assert.True(byName["Guide"].ZIndex < byName["Caption"].ZIndex);
        Assert.False(byName["Guide"].IsPrintable);
        Assert.True(byName["Caption"].IsPrintable && byName["Band"].IsPrintable);
    }

    [Fact]
    public void BarcodeText_PrintsInTheVendorFontBelowTheStoredBars()
    {
        // No stored font or position: NiceLabel prints the text below the bars in
        // Arial bold, auto-scaled to eight narrow bars (X = 0.375 mm -> 3 mm, 8.5 pt).
        var template = Import("""
            <Item Type="BarcodeDocumentItem">
              <Name>Code</Name>
              <Geometry Type="PositionGeometry"><X>10000</X><Y>10000</Y></Geometry>
              <BarcodeData Type="Code128BarcodeData"><BaseBarWidth>375</BaseBarWidth><ModuleHeight>6000</ModuleHeight></BarcodeData>
              <FixedContents>LOT-0042</FixedContents>
            </Item>
            """, new HriStripMeasurer(4.0));

        var code = Assert.Single(template.Objects);
        Assert.Equal(BarcodeHriPlacement.Below, code.BarcodeHriPlacement);
        Assert.Equal(("Arial", true), (code.BarcodeHriFontFamily, code.BarcodeHriBold));
        Assert.Equal(8 * 0.375 * 72 / 25.4, code.BarcodeTextFontSizePt, 3);
        // The bars keep NiceLabel's 6 mm; the frame adds the measured text strip.
        Assert.Equal(6.0 + 4.0, code.HeightMm, 3);
        Assert.Equal((10.0, 10.0), (code.XMm, code.YMm));
    }

    [Fact]
    public void BarcodeText_WithAStoredFontAndNoAutoScaling_KeepsTheStoredFont()
    {
        var template = Import("""
            <Item Type="BarcodeDocumentItem">
              <Name>Code</Name>
              <Geometry Type="PositionGeometry"><X>10000</X><Y>10000</Y></Geometry>
              <BarcodeData Type="Code128BarcodeData"><BaseBarWidth>250</BaseBarWidth><ModuleHeight>8000</ModuleHeight>
                <HumanInterpretationPosition>2</HumanInterpretationPosition>
                <AutoFontScaling>False</AutoFontScaling>
                <IsCustomHumanReadableFont>True</IsCustomHumanReadableFont>
                <HumanInterpretationFontDescriptor Type="FontDescriptor"><Name>Courier New</Name><Height>9</Height>
                  <LogFontWrapper Type="LogFontWrapper"><Height>-38</Height><Weight>400</Weight><FaceName>Courier New</FaceName></LogFontWrapper>
                </HumanInterpretationFontDescriptor>
              </BarcodeData>
              <FixedContents>A1</FixedContents>
            </Item>
            """);

        var code = Assert.Single(template.Objects);
        Assert.Equal(BarcodeHriPlacement.Above, code.BarcodeHriPlacement);
        Assert.Equal(("Courier New", false, 9.0), (code.BarcodeHriFontFamily, code.BarcodeHriBold, code.BarcodeTextFontSizePt));
        // Without a measurer the strip is estimated: a 1.2 em line, padding and gap.
        Assert.Equal(8.0 + 1.2 * 9 * 25.4 / 72 + 0.4 + 0.5, code.HeightMm, 3);
    }

    [Fact]
    public void BarcodeText_TooSmallForPrint_GrowsToTheSmallestHriSize()
    {
        // X = 0.125 mm auto-scales to 2.8 pt; ANLAbel prints HRI text from 5 pt.
        var template = Import("""
            <Item Type="BarcodeDocumentItem">
              <Name>Code</Name>
              <Geometry Type="PositionGeometry"><X>10000</X><Y>10000</Y></Geometry>
              <BarcodeData Type="Code128BarcodeData"><BaseBarWidth>125</BaseBarWidth><ModuleHeight>5000</ModuleHeight></BarcodeData>
              <FixedContents>12345678</FixedContents>
            </Item>
            """, new HriStripMeasurer(2.5));

        var code = Assert.Single(template.Objects);
        Assert.Equal(5.0, code.BarcodeTextFontSizePt, 3);
        Assert.Equal(7.5, code.HeightMm, 3);
    }

    [Fact]
    public void BarcodeTextOff_KeepsTheBarHeightAndTheDefaultFont()
    {
        var template = Import("""
            <Item Type="BarcodeDocumentItem">
              <Name>Code</Name>
              <Geometry Type="PositionGeometry"><X>10000</X><Y>10000</Y></Geometry>
              <BarcodeData Type="Code128BarcodeData"><BaseBarWidth>250</BaseBarWidth><ModuleHeight>8000</ModuleHeight><HumanInterpretationPosition>0</HumanInterpretationPosition></BarcodeData>
              <FixedContents>A1</FixedContents>
            </Item>
            """, new HriStripMeasurer(4.0));

        var code = Assert.Single(template.Objects);
        Assert.Equal(BarcodeHriPlacement.None, code.BarcodeHriPlacement);
        Assert.Equal(8.0, code.HeightMm, 3);
        Assert.Equal((LabelObject.DefaultBarcodeHriFontFamily, false), (code.BarcodeHriFontFamily, code.BarcodeHriBold));
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
    public void DataMatrixFormat_BecomesTheChosenSymbolSize()
    {
        var template = Import("""
            <Item Type="BarcodeDocumentItem">
              <Name>Fixed</Name>
              <Geometry Type="PositionGeometry"><X>1000</X><Y>1000</Y></Geometry>
              <BarcodeData Type="DatamatrixBarcodeData"><BaseBarWidth>500</BaseBarWidth><ModuleHeight>0</ModuleHeight><DatamatrixFormat>15</DatamatrixFormat></BarcodeData>
              <FixedContents>12345</FixedContents>
            </Item>
            <Item Type="BarcodeDocumentItem">
              <Name>Narrow</Name>
              <Geometry Type="PositionGeometry"><X>40000</X><Y>1000</Y></Geometry>
              <BarcodeData Type="DatamatrixBarcodeData"><BaseBarWidth>500</BaseBarWidth><ModuleHeight>0</ModuleHeight><DatamatrixFormat>25</DatamatrixFormat></BarcodeData>
              <FixedContents>12345</FixedContents>
            </Item>
            <Item Type="BarcodeDocumentItem">
              <Name>Automatic</Name>
              <Geometry Type="PositionGeometry"><X>1000</X><Y>40000</Y></Geometry>
              <BarcodeData Type="DatamatrixBarcodeData"><BaseBarWidth>500</BaseBarWidth><ModuleHeight>0</ModuleHeight><DatamatrixFormat>0</DatamatrixFormat></BarcodeData>
              <FixedContents>12345</FixedContents>
            </Item>
            """);

        var codes = template.Objects.ToDictionary(item => item.Name);
        Assert.Equal(DataMatrixSymbolSize.C52x52, codes["Fixed"].DataMatrixSymbolSize);
        Assert.Equal((26.0, 26.0), (codes["Fixed"].WidthMm, codes["Fixed"].HeightMm));
        Assert.Equal(DataMatrixSymbolSize.C8x18, codes["Narrow"].DataMatrixSymbolSize);
        Assert.Equal((9.0, 4.0), (codes["Narrow"].WidthMm, codes["Narrow"].HeightMm));
        Assert.Equal(DataMatrixSymbolSize.Auto, codes["Automatic"].DataMatrixSymbolSize);
    }

    [Fact]
    public void GraphicResizeModeAndMirrors_BecomePictureSizingAndFlips()
    {
        var payload = Convert.ToBase64String(TinyPng(3, 2));
        var template = Import($"""
            <Item Type="GraphicDocumentItem">
              <Name>Stretched</Name>
              <Geometry Type="RectGeometry"><Left>1000</Left><Top>1000</Top><Width>12700</Width><Height>10200</Height></Geometry>
              <EmbeddedGraphicContents Type="BinaryContents"><Base64StringValue>{payload}</Base64StringValue></EmbeddedGraphicContents>
              <ResizeMode>1</ResizeMode>
              <HorizontalMirror>True</HorizontalMirror>
            </Item>
            <Item Type="GraphicDocumentItem">
              <Name>Proportional</Name>
              <Geometry Type="RectGeometry"><Left>20000</Left><Top>1000</Top><Width>12700</Width><Height>10200</Height></Geometry>
              <EmbeddedGraphicContents Type="BinaryContents"><Base64StringValue>{payload}</Base64StringValue></EmbeddedGraphicContents>
              <ResizeMode>2</ResizeMode>
              <VerticalMirror>true</VerticalMirror>
            </Item>
            <Item Type="GraphicDocumentItem">
              <Name>Original</Name>
              <Geometry Type="RectGeometry"><Left>40000</Left><Top>1000</Top><Width>12700</Width><Height>10200</Height></Geometry>
              <EmbeddedGraphicContents Type="BinaryContents"><Base64StringValue>{payload}</Base64StringValue></EmbeddedGraphicContents>
              <ResizeMode>0</ResizeMode>
            </Item>
            """);

        var pictures = template.Objects.ToDictionary(item => item.Name);
        Assert.Equal((PictureSizing.Stretch, true, false), (pictures["Stretched"].PictureSizing, pictures["Stretched"].PictureFlipHorizontal, pictures["Stretched"].PictureFlipVertical));
        Assert.Equal((PictureSizing.Proportional, false, true), (pictures["Proportional"].PictureSizing, pictures["Proportional"].PictureFlipHorizontal, pictures["Proportional"].PictureFlipVertical));
        // A vendor picture kept at its original size stores that size as its frame,
        // so it is drawn whole in that frame.
        Assert.Equal(PictureSizing.Proportional, pictures["Original"].PictureSizing);
        Assert.Equal((12.7, 10.2), (pictures["Original"].WidthMm, pictures["Original"].HeightMm));
    }

    [Fact]
    public void GraphicWithoutPayload_IsReadFromTheFileBesideTheLabel()
    {
        var directory = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "anlabel-nlbl-gfx-" + Guid.NewGuid().ToString("N")));
        try
        {
            var png = TinyPng(4, 3);
            File.WriteAllBytes(Path.Combine(directory.FullName, "logo1.png"), png);
            var document = Parse(Format("""
                <Item Type="GraphicDocumentItem">
                  <Name>Logo</Name>
                  <Geometry Type="RectGeometry"><Left>1000</Left><Top>1000</Top><Width>12700</Width><Height>10200</Height></Geometry>
                  <GraphicFileName>C:\authoring\machine\.\logo1.png</GraphicFileName>
                  <EmbeddedGraphicContents Type="BinaryContents"><Base64StringValue></Base64StringValue></EmbeddedGraphicContents>
                </Item>
                """));

            var template = NiceLabel10NlblImporter.CreateTemplate(document, Path.Combine(directory.FullName, "label.nlbl"));

            var image = Assert.Single(template.Objects);
            Assert.Equal(ObjectType.Image, image.Type);
            Assert.Equal(Convert.ToBase64String(png), image.ImageDataBase64);
            Assert.Equal((4, 3), (image.ImagePixelWidth, image.ImagePixelHeight));
        }
        finally
        {
            directory.Delete(recursive: true);
        }
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

    private sealed class HriStripMeasurer(double stripMm) : INiceLabel10TextMeasurer
    {
        public (double WidthMm, double HeightMm)? MeasureText(LabelObject item, string sampleText) => null;

        public double? MeasureBarcodeHriStripMm(LabelObject item, string sampleText) => stripMm;
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
