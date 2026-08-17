using ANLAbel.Core.Workflow;
using ANLAbel.App.ViewModels;
using ANLAbel.App;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

internal static class DocumentWorkflowRegression
{
    public static Task Run()
    {
        Require(DocumentWorkflowContract.TryTransition(DocumentWorkflowState.Draft, DocumentWorkflowState.InReview, null, out _), "Draft must request review.");
        Require(!DocumentWorkflowContract.TryTransition(DocumentWorkflowState.Unknown, DocumentWorkflowState.Published, null, out _), "Unknown must never become Published implicitly.");
        Require(!DocumentWorkflowContract.TryTransition(DocumentWorkflowState.InReview, DocumentWorkflowState.Rejected, null, out var reason) && reason.Contains("comment", StringComparison.OrdinalIgnoreCase), "Request changes must require a comment.");
        Require(DocumentWorkflowContract.TryTransition(DocumentWorkflowState.InReview, DocumentWorkflowState.Rejected, "Missing approval evidence", out _), "Commented request changes must be allowed.");
        Require(!DocumentWorkflowContract.TryTransition(DocumentWorkflowState.Published, DocumentWorkflowState.Approved, "", out _), "Published must not silently regress to Approved.");
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".anlabel");
        try
        {
            var hash = "hash-one";
            var vm = new DocumentWorkflowViewModel(path, () => hash);
            Require(vm.Transition(vm.Available.Single(item => item.To == DocumentWorkflowState.InReview)), "Workflow host must record an available transition.");
            hash = "hash-two"; vm.Refresh();
            Require(vm.StateText.StartsWith("Draft", StringComparison.Ordinal), "Changed document hash must start a new Draft revision.");
        }
        finally { var sidecar = path + ".workflow.jsonl"; if (File.Exists(sidecar)) File.Delete(sidecar); }
        AssertWpfAutomationTree();
        return Task.CompletedTask;
    }
    private static void AssertWpfAutomationTree()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".anlabel");
                var window = new DocumentWorkflowWindow(path, () => "test-hash");
                window.Measure(new Size(1024, 600)); window.Arrange(new Rect(0, 0, 1024, 600)); window.UpdateLayout();
                var found = new HashSet<string>(StringComparer.Ordinal); var visited = new HashSet<DependencyObject>();
                void Walk(DependencyObject node) { if (!visited.Add(node)) return; if (node is UIElement e) { var id = System.Windows.Automation.AutomationProperties.GetAutomationId(e); if (!string.IsNullOrWhiteSpace(id)) found.Add(id); } if (node is Visual or Visual3D) for (var i = 0; i < VisualTreeHelper.GetChildrenCount(node); i++) Walk(VisualTreeHelper.GetChild(node, i)); foreach (var child in LogicalTreeHelper.GetChildren(node).OfType<DependencyObject>()) Walk(child); }
                Walk(window);
                foreach (var id in new[] { "CC.P4.Workflow.Root", "CC.P4.Workflow.Status" }) Require(found.Contains(id), $"Workflow tree must expose {id} at 1024 x 600.");
                window.Close();
            }
            catch (Exception ex) { failure = ex; }
            finally { System.Windows.Threading.Dispatcher.CurrentDispatcher.InvokeShutdown(); }
        });
        thread.SetApartmentState(ApartmentState.STA); thread.Start(); thread.Join(); if (failure is not null) throw failure;
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
