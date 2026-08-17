using ANLAbel.App.ViewModels;
using ANLAbel.App;
using ANLAbel.Core.Data;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

internal static class DataWorkspaceRegression
{
    public static Task Run()
    {
        var committed = new List<DataTransformDefinition> { new("Old", "FIELD(\"PartNo\")") };
        var applied = Array.Empty<DataTransformDefinition>();
        var vm = new DataWorkspaceViewModel(
            () => DataRecord.Create(new[] { new KeyValuePair<string, string?>("PartNo", "ABC"), new KeyValuePair<string, string?>("Lot", "42") }),
            committed,
            definitions => applied = definitions.ToArray());
        vm.Selected = vm.Drafts.Single(); vm.OutputName = "PrintName"; vm.Formula = "CONCAT(FIELD(\"PartNo\"), \"-\", FIELD(\"Lot\"))"; vm.CommitEditor();
        Require(vm.Validate(), "Valid transform draft must validate through the Core pipeline.");
        Require(vm.Result == "ABC-42" && vm.Lineage.Contains("PartNo", StringComparison.Ordinal), "Sample result and exact lineage must be presented.");
        Require(committed.Single().Name == "Old", "Draft edits must not mutate committed definitions.");
        Require(vm.Apply() && applied.Single().Name == "PrintName", "Apply must commit the complete valid draft once.");
        vm.Add(); vm.Selected = vm.Drafts.Last(); vm.OutputName = "PrintName"; vm.Formula = "FIELD(\"PartNo\")"; vm.CommitEditor();
        Require(!vm.Validate() && !vm.Apply(), "Duplicate output must fail closed without a partial apply.");
        var noSample = new DataWorkspaceViewModel(() => null, committed, _ => throw new InvalidOperationException("No-sample draft must not apply."));
        Require(!noSample.Validate() && !noSample.Apply() && noSample.Status.Contains("No sample row", StringComparison.OrdinalIgnoreCase), "No selected row must remain explicit and fail closed.");
        var main = new MainViewModel();
        main.ReplaceDataTransforms(new[] { new DataTransformDefinition("Derived", "FIELD(\"PartNo\")") });
        Require(main.DataTransforms.Single().Name == "Derived", "Valid Apply owner must replace the committed collection.");
        main.UndoCommand.Execute(null);
        Require(!main.DataTransforms.Any(), "A transform Apply must remain one undoable template mutation.");
        var xaml = File.ReadAllText(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "ANLAbel.App", "DataWorkspaceWindow.xaml")));
        foreach (var id in new[] { "DataWorkspace.Root", "DataWorkspace.SourceFieldList", "DataWorkspace.TransformList", "DataWorkspace.TransformFormula", "DataWorkspace.ApplyTransform", "DataWorkspace.Diagnostics" }) Require(xaml.Contains(id, StringComparison.Ordinal), $"Data Workspace must expose {id}.");
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
                var window = new DataWorkspaceWindow(new MainViewModel());
                window.Measure(new Size(1024, 600));
                window.Arrange(new Rect(0, 0, 1024, 600));
                window.UpdateLayout();
                var found = new HashSet<string>(StringComparer.Ordinal);
                var visited = new HashSet<DependencyObject>();
                void Walk(DependencyObject node)
                {
                    if (!visited.Add(node)) return;
                    if (node is UIElement element)
                    {
                        var id = System.Windows.Automation.AutomationProperties.GetAutomationId(element);
                        if (!string.IsNullOrWhiteSpace(id)) found.Add(id);
                    }
                    if (node is Visual or Visual3D)
                        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(node); index++) Walk(VisualTreeHelper.GetChild(node, index));
                    foreach (var child in LogicalTreeHelper.GetChildren(node).OfType<DependencyObject>()) Walk(child);
                }
                Walk(window);
                foreach (var id in new[] { "DataWorkspace.Root", "DataWorkspace.Refresh", "DataWorkspace.SourceFieldList", "DataWorkspace.TransformList", "DataWorkspace.AddTransform", "DataWorkspace.TransformOutputName", "DataWorkspace.TransformFormula", "DataWorkspace.ValidateTransform", "DataWorkspace.ApplyTransform", "DataWorkspace.Diagnostics" })
                    Require(found.Contains(id), $"Data Workspace visual tree must expose '{id}' at 1024 x 600.");
                window.Close();
            }
            catch (Exception ex) { failure = ex; }
            finally { System.Windows.Threading.Dispatcher.CurrentDispatcher.InvokeShutdown(); }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (failure is not null) throw failure;
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
