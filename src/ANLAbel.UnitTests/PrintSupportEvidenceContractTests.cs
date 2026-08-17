using ANLAbel.Core.Printing;
using Xunit;

namespace ANLAbel.UnitTests;

public sealed class PrintSupportEvidenceContractTests
{
    [Fact]
    public void BundleKeepsCorrelationIdentitiesAndRedactsSensitiveMetadata()
    {
        var bundle = PrintSupportEvidenceContract.Build(
            jobId: "job-42",
            queueName: "Zebra-GK420d",
            spoolJobId: "spool-9",
            documentHash: "doc-hash",
            sceneHash: "scene-hash",
            outputContractHash: "output-hash",
            manifestFingerprint: "manifest-hash",
            textResourceFingerprint: "text-hash",
            imageRasterFingerprint: "image-hash",
            thermalGoldenFingerprint: "thermal-hash",
            outcome: "SpoolAccepted",
            physicalOutputVerified: false,
            metadata: new[]
            {
                new KeyValuePair<string, string?>("operator", "line-a"),
                new KeyValuePair<string, string?>("password", "super-secret"),
                new KeyValuePair<string, string?>("RawValue", "CUSTOMER-SKU-999"),
                new KeyValuePair<string, string?>("note", new string('x', 200))
            },
            lifecycleStates: new[] { "Created", "PreflightPassed", "SpoolAccepted" });

        Assert.Equal(PrintSupportEvidenceContract.ContractVersion, bundle.ContractVersion);
        Assert.Equal("job-42", bundle.JobId);
        Assert.Equal("Zebra-GK420d", bundle.QueueName);
        Assert.Equal("spool-9", bundle.SpoolJobId);
        Assert.False(bundle.PhysicalOutputVerified);
        Assert.Equal(3, bundle.LifecycleStates.Count);
        Assert.False(string.IsNullOrWhiteSpace(bundle.EvidenceFingerprint));

        var password = bundle.Metadata.Single(pair => pair.Key == "password");
        var raw = bundle.Metadata.Single(pair => pair.Key == "RawValue");
        var note = bundle.Metadata.Single(pair => pair.Key == "note");
        Assert.Equal("[redacted]", password.Value);
        Assert.Equal("[redacted]", raw.Value);
        Assert.Equal("[redacted-long-value]", note.Value);
        Assert.Equal("line-a", bundle.Metadata.Single(pair => pair.Key == "operator").Value);
    }

    [Fact]
    public void CanonicalJsonExcludesForbiddenPayloadFragments()
    {
        const string secretSku = "ACME-PRIVATE-SKU-7788";
        var bundle = PrintSupportEvidenceContract.Build(
            jobId: "job-7",
            queueName: "TSC",
            spoolJobId: null,
            documentHash: "d",
            sceneHash: "s",
            outputContractHash: "o",
            manifestFingerprint: "m",
            textResourceFingerprint: "t",
            imageRasterFingerprint: "i",
            thermalGoldenFingerprint: null,
            outcome: "Unknown",
            physicalOutputVerified: false,
            metadata: new[]
            {
                new KeyValuePair<string, string?>("payload", secretSku),
                new KeyValuePair<string, string?>("rowCount", "12")
            });

        Assert.False(PrintSupportEvidenceContract.ContainsRawPayloadLeak(bundle, secretSku));
        var json = PrintSupportEvidenceContract.ToCanonicalJson(bundle);
        Assert.Contains("job-7", json, StringComparison.Ordinal);
        Assert.Contains("rowCount", json, StringComparison.Ordinal);
        Assert.DoesNotContain(secretSku, json, StringComparison.Ordinal);
        Assert.Contains("[redacted]", json, StringComparison.Ordinal);
    }

    [Fact]
    public void FingerprintIsStableForEquivalentRedactedBundles()
    {
        static PrintSupportEvidenceBundle Make() => PrintSupportEvidenceContract.Build(
            "job-1",
            "Q",
            "S",
            "D",
            "SC",
            "O",
            "M",
            "T",
            "I",
            "TH",
            "Failed",
            physicalOutputVerified: false,
            metadata: new[]
            {
                new KeyValuePair<string, string?>("b", "2"),
                new KeyValuePair<string, string?>("a", "1")
            },
            lifecycleStates: new[] { "Created", "Failed" });

        var first = Make();
        var second = Make();
        Assert.Equal(first.EvidenceFingerprint, second.EvidenceFingerprint);
        Assert.Equal(first.Summarize(), second.Summarize());
    }

    [Fact]
    public void MissingJobIdentityFailsClosed()
    {
        Assert.Throws<ArgumentException>(() => PrintSupportEvidenceContract.Build(
            jobId: " ",
            queueName: null,
            spoolJobId: null,
            documentHash: null,
            sceneHash: null,
            outputContractHash: null,
            manifestFingerprint: null,
            textResourceFingerprint: null,
            imageRasterFingerprint: null,
            thermalGoldenFingerprint: null,
            outcome: "Unknown",
            physicalOutputVerified: false));
    }

    [Fact]
    public void BuildFromDurableJobNeverClaimsPhysicalCompletionAndRedactsReasonLength()
    {
        var longReason = new string('R', 200) + "SECRET-SKU-99";
        var bundle = PrintSupportEvidenceContract.BuildFromDurableJob(
            jobId: "job-durable-1",
            printerName: "TSC-244",
            spoolJobId: 17,
            queueState: "Completed",
            documentHash: "doc",
            sceneHash: "scene",
            outputContractHash: "out",
            manifestFingerprint: "man",
            lifecycleState: "SpoolAccepted",
            operatorAction: "Acknowledged",
            relatedJobId: "parent-1",
            reason: longReason);

        Assert.False(bundle.PhysicalOutputVerified);
        Assert.Equal("job-durable-1", bundle.JobId);
        Assert.Equal("TSC-244", bundle.QueueName);
        Assert.Equal("17", bundle.SpoolJobId);
        Assert.Contains(bundle.LifecycleStates, state => state == "SpoolAccepted");
        Assert.Contains(bundle.LifecycleStates, state => state == "Acknowledged");
        Assert.False(PrintSupportEvidenceContract.ContainsRawPayloadLeak(bundle, "SECRET-SKU-99"));
        Assert.Equal("[redacted-long-value]", bundle.Metadata.Single(pair => pair.Key == "reason").Value);
    }

    [Fact]
    public async Task WriteJsonAsync_IsAtomicAndReadable()
    {
        var directory = Path.Combine(Path.GetTempPath(), "anlabel-support-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "evidence.json");
        try
        {
            var bundle = PrintSupportEvidenceContract.BuildFromDurableJob(
                "job-write",
                "Queue-A",
                3,
                "Printing",
                "d",
                "s",
                "o",
                "m",
                "Failed",
                operatorAction: "None",
                relatedJobId: null,
                reason: "driver offline");

            await PrintSupportEvidenceContract.WriteJsonAsync(bundle, path);

            Assert.True(File.Exists(path));
            var json = await File.ReadAllTextAsync(path);
            Assert.Equal(PrintSupportEvidenceContract.ToCanonicalJson(bundle), json);
            Assert.DoesNotContain("payload", json, StringComparison.OrdinalIgnoreCase);
            Assert.Empty(Directory.GetFiles(directory, ".evidence.json.*.tmp"));
        }
        finally
        {
            try { Directory.Delete(directory, recursive: true); } catch { /* best effort */ }
        }
    }
}
