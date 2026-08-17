using ANLAbel.App;
using ANLAbel.Core.Automation;
using ANLAbel.Core.Data;
using ANLAbel.Core.Models;
using ANLAbel.Core.Workflow;
using ANLAbel.Project.SaveLoad;

internal static class AutomationPreparedDataBindingRegression
{
    public static async Task Run()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".anlabel");
        try
        {
            var template = new LabelTemplate();
            template.Objects.Add(new LabelObject { BindingExpression = "{SKU} / {LOT}" });
            var files = new ProjectFileService();
            await files.SaveAsync(template, path);
            var configuration = new FileDropTriggerConfiguration("trigger", "CSV", Path.GetTempPath(), "*.csv", false, true, path, "Named queue", DocumentWorkflowPrintPolicyMode.Off);
            var validator = new AutomationPreparedDataBindingValidator(files);
            var good = await validator.ValidateAsync(configuration, [DataRecord.Create([new("SKU", "A-1"), new("LOT", "L-2")])]);
            Require(good.Allowed && good.RequiredFields.SequenceEqual(["LOT", "SKU"]), "Prepared records that supply every bound field must be dispatch-ready in principle without printing.");
            var missing = await validator.ValidateAsync(configuration, [DataRecord.Create([new("SKU", "A-1")])]);
            Require(!missing.Allowed && missing.Diagnostic.Contains("LOT", StringComparison.Ordinal), "A missing bound field must block before any manifest or queue request.");
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
