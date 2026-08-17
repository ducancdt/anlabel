using System.Data;
using ANLAbel.Core.Data;
using ANLAbel.Data;
using Xunit;

namespace ANLAbel.UnitTests;

public sealed class DataConnectorContractTests
{
    [Fact]
    public async Task DataTableAdapterProvidesTypedSchemaAndStablePages()
    {
        var table = new DataTable("Products");
        table.Columns.Add("PartNo", typeof(string));
        table.Columns.Add("Quantity", typeof(int));
        table.Columns.Add("Active", typeof(bool));
        table.Rows.Add("PN-001", 3, true);
        table.Rows.Add("PN-002", 5, false);
        table.Rows.Add("PN-003", 8, true);
        var connector = new DataTableDataConnector(
            new DataConnectorDescriptor("csv-products", "Products CSV", "csv", SupportsPaging: true, SupportsRefresh: true),
            table);

        // Connector pages must remain stable even when the UI's DataTable is
        // subsequently changed for preview purposes.
        table.Rows[0]["PartNo"] = "MUTATED";
        table.Rows.Add("PN-004", 13, false);

        var first = await connector.ReadPageAsync(new DataReadRequest(Offset: -4, Limit: 2));
        var second = await connector.ReadPageAsync(new DataReadRequest(Limit: 2, ContinuationToken: first.ContinuationToken));

        Assert.Equal(DataValueKind.Text, first.Schema.Single(field => field.Name == "PartNo").ValueKind);
        Assert.Equal(DataValueKind.Integer, first.Schema.Single(field => field.Name == "Quantity").ValueKind);
        Assert.Equal(DataValueKind.Boolean, first.Schema.Single(field => field.Name == "Active").ValueKind);
        Assert.Equal(2, first.Records.Length);
        Assert.True(first.Records[0].TryGetValue("partno", out var partNo));
        Assert.Equal("PN-001", partNo);
        Assert.Equal("2", first.ContinuationToken);
        Assert.False(first.IsComplete);
        Assert.Single(second.Records);
        Assert.Equal("PN-003", second.Records[0].Values["PartNo"]);
        Assert.Null(second.ContinuationToken);
        Assert.True(second.IsComplete);

        await Assert.ThrowsAsync<ArgumentException>(() => connector.ReadPageAsync(new DataReadRequest(ContinuationToken: "bad")));
    }

    [Fact]
    public async Task DataTableAdapterHonorsCancellationBeforeReading()
    {
        var connector = new DataTableDataConnector(
            new DataConnectorDescriptor("empty", "Empty", "memory", false, false),
            new DataTable());
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() => connector.ReadPageAsync(new DataReadRequest(), cancellation.Token));
    }

    [Fact]
    public void TypedRecordsReuseExistingBindingAndFormulaSemantics()
    {
        var record = DataRecord.Create(new[]
        {
            new KeyValuePair<string, string?>("Part No", "PN-42"),
            new KeyValuePair<string, string?>("Qty", "7")
        });

        Assert.Equal("Part PN-42", DataRecordExpressionEvaluator.EvaluateBinding("Part {part-no}", record));
        var formula = DataRecordExpressionEvaluator.EvaluateFormula("CONCAT(FIELD(\"Part No\"), \" x\", FIELD(\"Qty\"))", record);
        Assert.Empty(formula.Errors);
        Assert.Equal("PN-42 x7", formula.Value);
    }

    [Fact]
    public void TransformPipelineOrdersDependenciesAndReportsLineage()
    {
        var source = DataRecord.Create(new[]
        {
            new KeyValuePair<string, string?>("Sku", "AX-17"),
            new KeyValuePair<string, string?>("Qty", "4")
        });
        var result = DataTransformPipeline.Evaluate(source, new[]
        {
            new DataTransformDefinition("Label", "CONCAT(FIELD(\"Prefix\"), \" x\", FIELD(\"Qty\"))"),
            new DataTransformDefinition("Prefix", "CONCAT(\"P-\", FIELD(\"Sku\"))")
        });

        Assert.True(result.IsValid);
        Assert.Equal("P-AX-17 x4", result.Record.Values["Label"]);
        Assert.Equal(new[] { "Sku" }, result.Lineage.Single(item => item.OutputField == "Prefix").InputFields);
        Assert.Equal(new[] { "Prefix", "Qty" }, result.Lineage.Single(item => item.OutputField == "Label").InputFields);
    }

    [Fact]
    public void TransformPipelineRejectsDependencyCycles()
    {
        var result = DataTransformPipeline.Evaluate(DataRecord.Create(Array.Empty<KeyValuePair<string, string?>>()), new[]
        {
            new DataTransformDefinition("A", "FIELD(\"B\")"),
            new DataTransformDefinition("B", "FIELD(\"A\")")
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("cycle", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void TransformPipelineDoesNotExposePartialValuesWhenAnyTransformFails()
    {
        var source = DataRecord.Create(new[]
        {
            new KeyValuePair<string, string?>("Sku", "AX-17")
        });

        var result = DataTransformPipeline.Evaluate(source, new[]
        {
            new DataTransformDefinition("Prefix", "CONCAT(\"P-\", FIELD(\"Sku\"))"),
            new DataTransformDefinition("Broken", "UNKNOWN(FIELD(\"Prefix\"))")
        });

        Assert.False(result.IsValid);
        Assert.Equal(source.Values, result.Record.Values);
        Assert.False(result.Record.TryGetValue("Prefix", out _));
        Assert.Empty(result.Lineage);
    }

    [Fact]
    public void TransformPipelineRejectsOutputThatWouldShadowSourceField()
    {
        var source = DataRecord.Create(new[]
        {
            new KeyValuePair<string, string?>("Sku", "AX-17")
        });

        var result = DataTransformPipeline.Evaluate(source, new[]
        {
            new DataTransformDefinition("sku", "CONCAT(\"replacement\")")
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("conflicts", StringComparison.OrdinalIgnoreCase));
        Assert.Equal("AX-17", result.Record.Values["Sku"]);
        Assert.Empty(result.Lineage);
    }

    [Fact]
    public void TransformDefinitionsHaveAStableFingerprintForDocumentIdentity()
    {
        var first = DataTransformPipeline.ComputeFingerprint(new[]
        {
            new DataTransformDefinition("Label", "CONCAT(FIELD(\"Sku\"), \"-X\")")
        });
        var second = DataTransformPipeline.ComputeFingerprint(new[]
        {
            new DataTransformDefinition("Label", "CONCAT(FIELD(\"Sku\"), \"-X\")")
        });
        var changed = DataTransformPipeline.ComputeFingerprint(new[]
        {
            new DataTransformDefinition("Label", "CONCAT(FIELD(\"Sku\"), \"-Y\")")
        });

        Assert.Equal(first, second);
        Assert.NotEqual(first, changed);
    }

    [Fact]
    public void FileSourceIdentityDetectsContentChangeWhenTimestampAndLengthArePreserved()
    {
        var path = Path.Combine(Path.GetTempPath(), $"anlabel-source-identity-{Guid.NewGuid():N}.csv");
        var timestamp = new DateTime(2026, 8, 14, 7, 0, 0, DateTimeKind.Utc);
        try
        {
            File.WriteAllText(path, "alpha");
            File.SetLastWriteTimeUtc(path, timestamp);
            Assert.True(FileSourceIdentity.TryCapture(path, out var captured));

            File.WriteAllText(path, "bravo");
            File.SetLastWriteTimeUtc(path, timestamp);
            Assert.True(FileSourceIdentity.TryCapture(path, out var changed));

            Assert.Equal(captured!.Length, changed!.Length);
            Assert.Equal(captured.LastWriteTimeUtc, changed.LastWriteTimeUtc);
            Assert.NotEqual(captured.Sha256, changed.Sha256);
            Assert.True(FileSourceIdentity.IsStale(captured, changed));
        }
        finally
        {
            File.Delete(path);
        }
    }
}
