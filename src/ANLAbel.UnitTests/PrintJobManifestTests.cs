using System.Text.Json;
using ANLAbel.Core.Automation;
using ANLAbel.Core.Data;
using ANLAbel.Core.Models;
using ANLAbel.Core.Printing;
using Xunit;

namespace ANLAbel.UnitTests;

public sealed class PrintJobManifestTests
{
    [Fact]
    public void RowDictionaryOrderDoesNotChangeManifestIdentity()
    {
        var first = PrintJobManifest.Create(
            "Warehouse label",
            @"H:\\templates\\warehouse.alabel",
            "Quick Print",
            "Zebra-01",
            100,
            50,
            203,
            203,
            2,
            2,
            new IReadOnlyDictionary<string, string>?[]
            {
                new Dictionary<string, string> { ["PartNo"] = "P-100", ["Lot"] = "L-7" },
                new Dictionary<string, string> { ["PartNo"] = "P-200", ["Lot"] = "L-8" }
            },
            documentHash: "doc",
            textResourceFingerprint: "text",
            sceneHash: "scene",
            outputContractHash: "ticket");
        var reordered = PrintJobManifest.Create(
            "Warehouse label",
            @"H:\\templates\\warehouse.alabel",
            "Quick Print",
            "Zebra-01",
            100,
            50,
            203,
            203,
            2,
            2,
            new IReadOnlyDictionary<string, string>?[]
            {
                new Dictionary<string, string> { ["Lot"] = "L-7", ["PartNo"] = "P-100" },
                new Dictionary<string, string> { ["Lot"] = "L-8", ["PartNo"] = "P-200" }
            },
            documentHash: "doc",
            textResourceFingerprint: "text",
            sceneHash: "scene",
            outputContractHash: "ticket");

        Assert.Equal(first.RowsFingerprint, reordered.RowsFingerprint);
        Assert.Equal(first.Fingerprint, reordered.Fingerprint);
        Assert.Equal("TICKET", first.OutputContractHash);
        Assert.Equal(PrintJobManifest.CurrentContractVersion, first.ContractVersion);
    }

    [Fact]
    public void RowOrderAndValueChangesInvalidateManifest()
    {
        var rows = new IReadOnlyDictionary<string, string>?[]
        {
            new Dictionary<string, string> { ["PartNo"] = "P-100" },
            new Dictionary<string, string> { ["PartNo"] = "P-200" }
        };
        var baseline = Create(rows);
        var reordered = Create(rows.Reverse().ToArray());
        var changed = Create(new IReadOnlyDictionary<string, string>?[]
        {
            new Dictionary<string, string> { ["PartNo"] = "P-100" },
            new Dictionary<string, string> { ["PartNo"] = "P-201" }
        });

        Assert.NotEqual(baseline.RowsFingerprint, reordered.RowsFingerprint);
        Assert.NotEqual(baseline.Fingerprint, reordered.Fingerprint);
        Assert.NotEqual(baseline.RowsFingerprint, changed.RowsFingerprint);
        Assert.NotEqual(baseline.Fingerprint, changed.Fingerprint);
    }

    [Fact]
    public void ManifestSerializationContainsNoRawLabelValues()
    {
        var manifest = Create(new IReadOnlyDictionary<string, string>?[]
        {
            new Dictionary<string, string>
            {
                ["PartNo"] = "SECRET-PART-001",
                ["Description"] = "Private customer payload"
            }
        });

        var serialized = JsonSerializer.Serialize(manifest);

        Assert.DoesNotContain("SECRET-PART-001", serialized, StringComparison.Ordinal);
        Assert.DoesNotContain("Private customer payload", serialized, StringComparison.Ordinal);
        Assert.NotEmpty(manifest.RowsFingerprint);
        Assert.NotEmpty(manifest.Fingerprint);
        Assert.True(manifest.IsFingerprintValid);
    }

    [Fact]
    public void TamperedManifestMetadataFailsSelfValidation()
    {
        var manifest = Create(new IReadOnlyDictionary<string, string>?[]
        {
            new Dictionary<string, string> { ["PartNo"] = "P-100" }
        });

        var tampered = manifest with { PrinterName = "Other Queue" };

        Assert.False(tampered.IsFingerprintValid);
    }

    [Fact]
    public void OutputContractChangeInvalidatesPreparedManifest()
    {
        var manifest = Create(new IReadOnlyDictionary<string, string>?[]
        {
            new Dictionary<string, string> { ["PartNo"] = "P-100" }
        });

        var changedContract = manifest with { OutputContractHash = "OTHER-TICKET" };

        Assert.False(changedContract.IsFingerprintValid);
    }

    [Fact]
    public void ThermalGoldenFingerprintIsBoundToManifestIdentity()
    {
        var withoutGolden = Create(Array.Empty<IReadOnlyDictionary<string, string>?>());
        var withGolden = PrintJobManifest.Create(
            "Warehouse label",
            "warehouse.anlabel",
            "Quick Print",
            "Zebra-01",
            100,
            50,
            203,
            203,
            0,
            0,
            Array.Empty<IReadOnlyDictionary<string, string>?>(),
            documentHash: "doc",
            textResourceFingerprint: "text",
            sceneHash: "scene",
            outputContractHash: "ticket",
            thermalRasterGoldenFingerprint: new string('A', 64));

        Assert.Equal(new string('A', 64), withGolden.ThermalRasterGoldenFingerprint);
        Assert.True(withGolden.IsFingerprintValid);
        Assert.NotEqual(withoutGolden.Fingerprint, withGolden.Fingerprint);
        Assert.False((withGolden with { ThermalRasterGoldenFingerprint = new string('B', 64) }).IsFingerprintValid);
    }

    [Fact]
    public void AutomationBatchProvenanceIsBoundWithoutPersistingRawRecords()
    {
        var source = System.Text.Encoding.UTF8.GetBytes("Sku,Lot\nA-1,L-2\n");
        var fileDropEvent = FileDropClaimContract.CreateIdentity("trigger-a", "configuration-a", FileDropClaimContract.ComputeContentFingerprint(source));
        var batch = FileDropPreparedBatchContract.Create(
            fileDropEvent,
            "template-hash",
            [DataRecord.Create([new("SKU", "A-1"), new("LOT", "L-2")])]);
        var manifest = PrintJobManifest.Create(
            "Warehouse label", "warehouse.anlabel", "Automation", "Zebra-01",
            100, 50, 203, 203, 1, 1,
            Array.Empty<IReadOnlyDictionary<string, string>?>(),
            documentHash: "doc", textResourceFingerprint: "text", sceneHash: "scene", outputContractHash: "ticket",
            automationBatch: batch);

        Assert.Equal(batch.EventId, manifest.AutomationEventId);
        Assert.Equal(batch.TriggerId, manifest.AutomationTriggerId);
        Assert.Equal(batch.ConfigurationFingerprint.ToUpperInvariant(), manifest.AutomationConfigurationFingerprint);
        Assert.Equal(batch.SourceFingerprint.ToUpperInvariant(), manifest.AutomationSourceFingerprint);
        Assert.Equal(batch.PreparedBatchId.ToUpperInvariant(), manifest.AutomationPreparedBatchId);
        Assert.True(manifest.IsFingerprintValid);
        Assert.DoesNotContain("A-1", JsonSerializer.Serialize(manifest), StringComparison.Ordinal);
        Assert.False((manifest with { AutomationPreparedBatchId = new string('A', 64) }).IsFingerprintValid);
    }

    [Fact]
    public void LegacyManifestFingerprintRemainsReadableAfterV2Upgrade()
    {
        var current = Create(Array.Empty<IReadOnlyDictionary<string, string>?>());
        var priorShape = current with { ContractVersion = PrintJobManifest.PreviousContractVersion };
        var prior = priorShape with { Fingerprint = PrintJobManifest.ComputePreviousFingerprint(priorShape) };
        var legacyShape = current with { ContractVersion = PrintJobManifest.LegacyContractVersion };
        var legacy = legacyShape with { Fingerprint = PrintJobManifest.ComputeLegacyFingerprint(legacyShape) };

        Assert.True(prior.IsFingerprintValid);
        Assert.True(legacy.IsFingerprintValid);
    }

    [Fact]
    public void TwoThenOneCopyOrdinalsChangeTheManifestAndAMissingSequenceStaysV3()
    {
        var same = new Dictionary<string, string> { ["Part"] = "SAME-LABEL-VALUE" };
        var rows = new IReadOnlyDictionary<string, string>?[] { same, same, same };
        var twoThenOne = PrintSequenceBuilder.Build(new PrintIntent(new QuantityPlan(new[]
        {
            new PrintRecordQuantity("rec-1", 1, 2),
            new PrintRecordQuantity("rec-2", 2, 1)
        })));

        Assert.Equal(new[] { 1, 1, 2 }, twoThenOne.Select(item => item.SourceRowNumber).ToArray());
        Assert.Equal(new[] { 1, 2, 1 }, twoThenOne.Select(item => item.CopyOrdinal).ToArray());
        Assert.Equal(new[] { 1, 2, 3 }, twoThenOne.Select(item => item.SequenceOrdinal).ToArray());

        var sequenced = Create(rows, twoThenOne.Select(item =>
            new PrintDispatchCopy(item.SourceRowNumber, item.CopyOrdinal, item.SequenceOrdinal)).ToArray());
        var oneThenTwo = Create(rows, new[]
        {
            new PrintDispatchCopy(1, 1, 1),
            new PrintDispatchCopy(2, 1, 2),
            new PrintDispatchCopy(2, 2, 3)
        });
        var unsequenced = Create(rows);
        var unsequencedAgain = Create(rows);

        Assert.Equal(PrintJobManifest.SequenceContractVersion, sequenced.ContractVersion);
        Assert.Equal("print-manifest-v3", unsequenced.ContractVersion);
        Assert.NotEqual(sequenced.Fingerprint, oneThenTwo.Fingerprint);
        Assert.NotEqual(sequenced.SequenceFingerprint, oneThenTwo.SequenceFingerprint);
        Assert.Equal(unsequenced.RowsFingerprint, sequenced.RowsFingerprint);
        Assert.Equal(unsequenced.Fingerprint, unsequencedAgain.Fingerprint);
        Assert.NotEqual(unsequenced.Fingerprint, sequenced.Fingerprint);
        Assert.True(sequenced.IsFingerprintValid);
        Assert.True(unsequenced.IsFingerprintValid);
        Assert.False((sequenced with { SequenceFingerprint = new string('A', 64) }).IsFingerprintValid);
        var serialized = JsonSerializer.Serialize(sequenced);
        Assert.DoesNotContain("SAME-LABEL-VALUE", serialized, StringComparison.Ordinal);
        Assert.DoesNotContain("rec-1", serialized, StringComparison.Ordinal);

        var database = new DatabaseConfig { CopiesField = "Qty" };
        var source = new (int SourceRowNumber, IReadOnlyDictionary<string, string>? Row)[]
        {
            (1, new Dictionary<string, string> { ["Part"] = "SAME-LABEL-VALUE", ["Qty"] = "2" }),
            (2, new Dictionary<string, string> { ["Part"] = "SAME-LABEL-VALUE", ["Qty"] = "1" })
        };
        Assert.True(QuickPrintQuantity.TryExpand(database, 9, source, out var expanded, out var sequence, out var error));
        Assert.Equal(string.Empty, error);
        var fromQuickPrint = Create(expanded, sequence);
        var swappedQuickPrint = Create(expanded, new[]
        {
            new PrintDispatchCopy(1, 1, 1),
            new PrintDispatchCopy(2, 1, 2),
            new PrintDispatchCopy(2, 2, 3)
        });
        Assert.Equal(sequenced.SequenceFingerprint, fromQuickPrint.SequenceFingerprint);
        Assert.NotEqual(fromQuickPrint.Fingerprint, swappedQuickPrint.Fingerprint);
        Assert.Throws<ArgumentException>(() => Create(rows, new[] { new PrintDispatchCopy(1, 1, 1) }));
    }

    [Fact]
    public void DispatchCallSitesPassTheCopySequenceIntoTheManifest()
    {
        var preview = ReadRepositoryFile("src", "ANLAbel.App", "PrintPreviewWindow.xaml.cs");
        var viewModel = ReadRepositoryFile("src", "ANLAbel.App", "ViewModels", "MainViewModel.cs");
        var printClick = Slice(preview, "private async void Print_Click", "private void LogPrintOperation");
        var currentRow = Slice(viewModel, "private async Task PrintCurrentRowAsync()", "private async Task PrintAllRowsAsync()");
        var allRows = Slice(viewModel, "private async Task PrintAllRowsAsync()", "private static IReadOnlyList<string> GetIndustrialFontFamilies()");
        var reprint = Slice(viewModel, "public async Task<TrackedPrintResult> DispatchApprovedPrintJobReprintAsync(", "private void ShowAllPanels()");
        var dispatch = Slice(viewModel, "private async Task<TrackedPrintResult> DispatchTrackedPrintAsync(", "PrintJobResult result;");

        Assert.Contains("sequence: selected.Select(entry => entry.Copy).ToArray()", printClick, StringComparison.Ordinal);
        Assert.Contains("out var sequence", currentRow, StringComparison.Ordinal);
        Assert.Contains("sequence: sequence", currentRow, StringComparison.Ordinal);
        Assert.Contains("out var sequence", allRows, StringComparison.Ordinal);
        Assert.Contains("sequence: sequence", allRows, StringComparison.Ordinal);
        Assert.Contains("sequence: sequence", reprint, StringComparison.Ordinal);
        Assert.Contains("sequence: sequence", dispatch, StringComparison.Ordinal);
    }

    private static PrintJobManifest Create(
        IEnumerable<IReadOnlyDictionary<string, string>?> rows,
        IReadOnlyList<PrintDispatchCopy>? sequence = null)
    {
        var list = rows.ToArray();
        return PrintJobManifest.Create(
            "Warehouse label",
            "warehouse.alabel",
            "Quick Print",
            "Zebra-01",
            100,
            50,
            203,
            203,
            list.Length,
            list.Length,
            list,
            documentHash: "doc",
            textResourceFingerprint: "text",
            sceneHash: "scene",
            outputContractHash: "ticket",
            sequence: sequence);
    }

    private static string ReadRepositoryFile(params string[] parts)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "ANLAbel.slnx")))
        {
            dir = dir.Parent;
        }

        Assert.NotNull(dir);
        return File.ReadAllText(Path.Combine(parts.Prepend(dir!.FullName).ToArray()));
    }

    private static string Slice(string source, string start, string end)
    {
        var startAt = source.IndexOf(start, StringComparison.Ordinal);
        var endAt = source.IndexOf(end, startAt + start.Length, StringComparison.Ordinal);
        Assert.True(startAt >= 0 && endAt > startAt);
        return source.Substring(startAt, endAt - startAt);
    }
}
