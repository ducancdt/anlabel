using System.Data;
using System.Text;
using ANLAbel.Barcode.Options;
using ANLAbel.Barcode.Renderers;
using ANLAbel.Core.Enums;
using ANLAbel.Core.Expressions;
using ANLAbel.Core.Models;
using ANLAbel.Core.Printing;
using ANLAbel.Core.Scene;
using ANLAbel.Data.Excel;
using ClosedXML.Excel;
using Xunit;

namespace ANLAbel.UnitTests;

/// <summary>
/// Composite integration fixture verifying the entire end-to-end pipeline
/// as planned in docs/CORE_SOFTWARE_STANDARD.md:
/// Excel data read -> Format preservation -> Binding evaluation ->
/// Preflight validation -> Scene compilation -> Sequence generation.
/// [Agent: Gemini / Antigravity]
/// </summary>
public class IndustrialPipelineCompositeFixtureTests
{
    [Fact]
    public async Task EndToEndPipeline_FormattedNumericAndMultiline_PreservesPayloadAndGeometry()
    {
        // 1. Arrange: Create real Excel workbook with zero-padded numeric format and multiline text
        var dir = Path.Combine(Path.GetTempPath(), "anlabel_composite_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var excelPath = Path.Combine(dir, "pipeline_data.xlsx");

        try
        {
            using (var workbook = new XLWorkbook())
            {
                var sheet = workbook.AddWorksheet("Parts");
                // Header row
                sheet.Cell(1, 1).Value = " Part No ";
                sheet.Cell(1, 2).Value = "Qty";
                sheet.Cell(1, 3).Value = "Batch";
                sheet.Cell(1, 4).Value = "Description";

                // Row 2: PartNo has explicit zero-padded format 000000 -> 123 becomes 000123
                sheet.Cell(2, 1).Value = 123;
                sheet.Cell(2, 1).Style.NumberFormat.Format = "000000";
                sheet.Cell(2, 2).Value = 2;
                sheet.Cell(2, 3).Value = "VN-LOT-01";
                sheet.Cell(2, 4).Value = "Header Line\nSub Line";

                // Row 3: PartNo 456 becomes 000456
                sheet.Cell(3, 1).Value = 456;
                sheet.Cell(3, 1).Style.NumberFormat.Format = "000000";
                sheet.Cell(3, 2).Value = 1;
                sheet.Cell(3, 3).Value = "VN-LOT-02";
                sheet.Cell(3, 4).Value = "Widget \"Pro\"";

                workbook.SaveAs(excelPath);
            }

            // 2. Arrange: Authored template with bound Text, TextBox, and Barcodes
            var template = new LabelTemplate
            {
                Name = "Industrial Shipment Label",
                WidthMm = 100,
                HeightMm = 50,
                Dpi = 300,
                Orientation = LabelOrientation.Portrait
            };
            template.PrinterProfile.PrinterName = "Zebra ZT411";
            template.PrinterProfile.Dpi = 300;

            // Barcode Code128 bound to {Part No}
            var barcodeObj = new LabelObject
            {
                Id = "obj-barcode",
                Type = ObjectType.BarcodeCode128,
                BindingExpression = "{Part No}",
                Text = "000000",
                XMm = 10,
                YMm = 10,
                WidthMm = 60,
                HeightMm = 18
            };
            template.Objects.Add(barcodeObj);

            // Text bound to {Batch}
            var textObj = new LabelObject
            {
                Id = "obj-batch",
                Type = ObjectType.Text,
                BindingExpression = "{Batch}",
                Text = "BATCH",
                XMm = 10,
                YMm = 30,
                WidthMm = 30,
                HeightMm = 6
            };
            template.Objects.Add(textObj);

            // TextBox bound to multiline {Description}
            var textBoxObj = new LabelObject
            {
                Id = "obj-desc",
                Type = ObjectType.TextBox,
                BindingExpression = "{Description}",
                Text = "Description",
                XMm = 45,
                YMm = 30,
                WidthMm = 50,
                HeightMm = 15
            };
            template.Objects.Add(textBoxObj);

            // Record initial authored geometry
            var initialWidth = template.WidthMm;
            var initialHeight = template.HeightMm;
            var initialBarcodeW = barcodeObj.WidthMm;
            var initialBarcodeH = barcodeObj.HeightMm;
            var initialTextW = textObj.WidthMm;
            var initialTextH = textObj.HeightMm;
            var initialTextBoxW = textBoxObj.WidthMm;
            var initialTextBoxH = textBoxObj.HeightMm;

            // 3. Step 1 (Data): Read Excel workbook via ExcelDataService
            var dataService = new ExcelDataService();
            var table = await dataService.LoadSheetAsync(excelPath, "Parts");

            Assert.Equal(2, table.Rows.Count);
            Assert.Contains("Part No", table.Columns.Cast<DataColumn>().Select(c => c.ColumnName));

            // Verify verbatim formatted payload preservation
            Assert.Equal("000123", table.Rows[0]["Part No"]);
            Assert.Equal("Header Line\nSub Line", table.Rows[0]["Description"]);
            Assert.Equal("000456", table.Rows[1]["Part No"]);

            // 4. Step 2 (Binding Evaluation): Evaluate row 1 and row 2
            var row1Map = table.Columns.Cast<DataColumn>()
                .ToDictionary(c => c.ColumnName, c => Convert.ToString(table.Rows[0][c]) ?? string.Empty, StringComparer.OrdinalIgnoreCase);
            var row2Map = table.Columns.Cast<DataColumn>()
                .ToDictionary(c => c.ColumnName, c => Convert.ToString(table.Rows[1][c]) ?? string.Empty, StringComparer.OrdinalIgnoreCase);

            var evalBarcode1 = BindingExpressionEvaluator.Evaluate(barcodeObj.BindingExpression, row1Map);
            Assert.Equal("000123", evalBarcode1);

            var evalDesc1 = BindingExpressionEvaluator.Evaluate(textBoxObj.BindingExpression, row1Map);
            Assert.Equal("Header Line\nSub Line", evalDesc1);

            // 5. Step 3 (Barcode Render): Render barcode with evaluated formatted payload at 300 DPI
            var renderer = new ZxingBarcodeRenderer();
            var barcodeImage = renderer.RenderBarcode(evalBarcode1, BarcodeType.Code128, barcodeObj.WidthMm, barcodeObj.HeightMm, 300);
            Assert.NotNull(barcodeImage);
            Assert.True(barcodeImage.WidthPixels > 0);
            Assert.True(barcodeImage.HeightPixels > 0);
            Assert.NotNull(barcodeImage.BgraPixels);
            Assert.True(barcodeImage.BgraPixels.Length > 0);

            // 6. Step 4 (Scene Compilation): Compile scene and verify integrity
            var docSnapshot = DocumentSnapshot.Capture(template);
            var sceneResult = SceneCompiler.Compile(docSnapshot);

            Assert.NotNull(sceneResult);
            Assert.True(sceneResult.Succeeded);
            Assert.Equal(100, sceneResult.Snapshot.WidthMm);
            Assert.Equal(50, sceneResult.Snapshot.HeightMm);
            Assert.Equal(3, sceneResult.Nodes.Length);

            // 7. Step 5 (Sequence Generation): Build quantity sequence
            // Row 1 has Qty 2 -> copy 1, copy 2. Row 2 has Qty 1 -> copy 1. Total = 3 labels.
            var intent = new PrintIntent(new QuantityPlan(new[]
            {
                new PrintRecordQuantity("rec-1", 1, 2),
                new PrintRecordQuantity("rec-2", 2, 1)
            }));

            var instances = PrintSequenceBuilder.Build(intent);
            Assert.Equal(3, instances.Count);

            Assert.Equal("rec-1", instances[0].RecordId);
            Assert.Equal(1, instances[0].CopyOrdinal);
            Assert.Equal(1, instances[0].SequenceOrdinal);
            Assert.Equal(1, instances[0].SourceRowNumber);

            Assert.Equal("rec-1", instances[1].RecordId);
            Assert.Equal(2, instances[1].CopyOrdinal);
            Assert.Equal(2, instances[1].SequenceOrdinal);
            Assert.Equal(1, instances[1].SourceRowNumber);

            Assert.Equal("rec-2", instances[2].RecordId);
            Assert.Equal(1, instances[2].CopyOrdinal);
            Assert.Equal(3, instances[2].SequenceOrdinal);
            Assert.Equal(2, instances[2].SourceRowNumber);

            // 8. Step 6 (Protected Invariants): Verify NO authored geometry was mutated
            Assert.Equal(initialWidth, template.WidthMm);
            Assert.Equal(initialHeight, template.HeightMm);
            Assert.Equal(initialBarcodeW, barcodeObj.WidthMm);
            Assert.Equal(initialBarcodeH, barcodeObj.HeightMm);
            Assert.Equal(initialTextW, textObj.WidthMm);
            Assert.Equal(initialTextH, textObj.HeightMm);
            Assert.Equal(initialTextBoxW, textBoxObj.WidthMm);
            Assert.Equal(initialTextBoxH, textBoxObj.HeightMm);
        }
        finally
        {
            try { Directory.Delete(dir, true); } catch { }
        }
    }
}
