using ANLAbel.App;
using ANLAbel.Core.Automation;
using ANLAbel.Core.Models;
using ANLAbel.Core.Workflow;
using ANLAbel.Project.SaveLoad;

internal static class AutomationTemplateBindingRegression
{
    public static async Task Run()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".anlabel");
        try
        {
            var files = new ProjectFileService();
            await files.SaveAsync(new LabelTemplate { Name = "Automation target" }, path);
            var validator = new AutomationTemplateBindingValidator(files);
            var off = new FileDropTriggerConfiguration("trigger", "Trigger", Path.GetTempPath(), "*.csv", false, true, path, "Explicit queue", DocumentWorkflowPrintPolicyMode.Off);
            Require((await validator.ValidateAsync(off)).Allowed, "Off policy may validate a saved exact template without implying dispatch.");
            var published = off with { PrintPolicyMode = DocumentWorkflowPrintPolicyMode.RequirePublished };
            Require(!(await validator.ValidateAsync(published)).Allowed, "RequirePublished must fail closed for a template with no Published workflow event.");
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
            var sidecar = path + ".workflow.jsonl"; if (File.Exists(sidecar)) File.Delete(sidecar);
        }
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
