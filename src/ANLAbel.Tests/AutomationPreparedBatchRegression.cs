using System.Text;
using ANLAbel.App;
using ANLAbel.Core.Automation;
using ANLAbel.Core.Data;
using ANLAbel.Core.Models;
using ANLAbel.Core.Workflow;
using ANLAbel.Printing.PrinterProfiles;
using ANLAbel.Project.SaveLoad;

internal static class AutomationPreparedBatchRegression
{
    public static async Task Run()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".anlabel");
        try
        {
            var template = new LabelTemplate();
            template.PrinterProfile.PrinterName = "Named queue";
            template.PrinterProfile.Dpi = 203;
            template.Objects.Add(new LabelObject { BindingExpression = "{SKU} / {LOT}" });
            var files = new ProjectFileService();
            await files.SaveAsync(template, path);
            var source = Encoding.UTF8.GetBytes("SKU,LOT\nA-1,L-2\n");
            var eventIdentity = FileDropClaimContract.CreateIdentity("trigger", "configuration", FileDropClaimContract.ComputeContentFingerprint(source));
            var configuration = new FileDropTriggerConfiguration("trigger", "CSV", Path.GetTempPath(), "*.csv", false, true, path, "Named queue", DocumentWorkflowPrintPolicyMode.Off);
            var records = new[] { DataRecord.Create([new("SKU", "A-1"), new("LOT", "L-2")]) };
            var builder = new AutomationPreparedBatchBuilder(files);

            var first = await builder.BuildAsync(eventIdentity, configuration, records);
            var second = await builder.BuildAsync(eventIdentity, configuration, records);
            Require(first.Allowed && first.Identity is not null, "A policy-valid prepared record set must create a payload-free batch identity.");
            var firstIdentity = first.Identity ?? throw new InvalidOperationException("Prepared batch identity is missing.");
            Require(firstIdentity == second.Identity, "The same source, configuration, template and ordered values must create the same prepared-batch identity.");
            Require(firstIdentity.SourceFingerprint == eventIdentity.SourceFingerprint && firstIdentity.ConfigurationFingerprint == eventIdentity.ConfigurationFingerprint,
                "Prepared-batch identity must preserve claimed source and configuration fingerprints.");
            Require(firstIdentity.RecordCount == 1 && firstIdentity.TemplateFingerprint.Length == 64 && firstIdentity.DataFingerprint.Length == 64,
                "Prepared-batch identity must include exact template and data fingerprints without retaining payload values.");

            var changed = await builder.BuildAsync(eventIdentity, configuration, [DataRecord.Create([new("SKU", "A-1"), new("LOT", "L-3")])]);
            Require(changed.Allowed && changed.Identity!.PreparedBatchId != firstIdentity.PreparedBatchId,
                "Changing one prepared value must produce a different immutable batch identity.");

            var exactConfiguration = configuration with { TriggerId = eventIdentity.TriggerId, Name = "CSV exact" };
            var exactEvent = FileDropClaimContract.CreateIdentity(exactConfiguration.TriggerId, exactConfiguration.ConfigurationFingerprint, eventIdentity.SourceFingerprint);
            var exactBatch = await builder.BuildAsync(exactEvent, exactConfiguration, records);
            var preflight = new AutomationPreparedBatchPreflightService(files, queueLookup: new AvailableQueue("Named queue"));
            var checkedBatch = await preflight.ValidateAsync(exactBatch.Identity!, exactConfiguration, records);
            Require(checkedBatch.Passed && checkedBatch.CanonicalQueueName == "Named queue" && checkedBatch.Issues.Count == 0,
                "Prepared batch must reuse preflight only when its exact template and named queue still match.");

            var unavailable = new AutomationPreparedBatchPreflightService(files, queueLookup: new MissingQueue());
            var missingQueue = await unavailable.ValidateAsync(exactBatch.Identity!, exactConfiguration, records);
            Require(!missingQueue.Passed && missingQueue.Diagnostic.Contains("unavailable", StringComparison.OrdinalIgnoreCase),
                "A missing named queue must block automation preflight without fallback or dispatch.");
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }

    private sealed class AvailableQueue(string name) : IPrinterQueueLookup
    {
        public PrinterQueueLookupResult Resolve(string printerName) => PrinterQueueLookupResult.Available(printerName, name);
    }

    private sealed class MissingQueue : IPrinterQueueLookup
    {
        public PrinterQueueLookupResult Resolve(string printerName) => PrinterQueueLookupResult.Missing(printerName, "Configured queue is unavailable.");
    }
}
