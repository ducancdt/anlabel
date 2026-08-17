using System.Text.Json;
using ANLAbel.Core.Automation;
using ANLAbel.Core.Data;
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

    private static PrintJobManifest Create(IEnumerable<IReadOnlyDictionary<string, string>?> rows)
    {
        return PrintJobManifest.Create(
            "Warehouse label",
            "warehouse.alabel",
            "Quick Print",
            "Zebra-01",
            100,
            50,
            203,
            203,
            rows.Count(),
            rows.Count(),
            rows,
            documentHash: "doc",
            textResourceFingerprint: "text",
            sceneHash: "scene",
            outputContractHash: "ticket");
    }
}
