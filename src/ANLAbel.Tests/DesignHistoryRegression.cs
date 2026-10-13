using System.Reflection;
using System.Windows;
using System.Windows.Documents;
using ANLAbel.App.Controls;
using ANLAbel.App.ViewModels;
using ANLAbel.Core.Enums;
using ANLAbel.Core.Models;

/// <summary>
/// Plan 13 GĐ3: one edit is one undo step and undo never silently puts back
/// an older data link (D02, H16); a missing font or image is visible while
/// editing (D07). Each method is one named gate in Program.cs.
/// </summary>
internal static class DesignHistoryRegression
{
    public static Task UndoAndRedoKeepTheCurrentDataLink()
    {
        var vm = new MainViewModel();
        vm.BeginTemplateEditGesture();
        vm.Template.Objects.Add(Rectangle("link-object", 5));
        vm.CommitTemplateEditGesture();

        // Linking a workbook is not a design edit and has no undo step of its own.
        var link = vm.Template.DatabaseConfig;
        link.FilePath = @"C:\data\linked.xlsx";
        link.SheetName = "Data";
        link.HeaderRowIndex = 2;
        link.CopiesField = "Qty";

        vm.UndoCommand.Execute(null);
        Require(vm.Template.Objects.Count == 0, "Undo must remove the object added by the last gesture.");
        RequireLink(vm.Template.DatabaseConfig, "Undo");

        vm.RedoCommand.Execute(null);
        Require(vm.Template.Objects.Count == 1, "Redo must restore the object.");
        RequireLink(vm.Template.DatabaseConfig, "Redo");

        vm.BeginTemplateEditGesture();
        vm.Template.Objects.Single().XMm = 30;
        vm.CancelTemplateEditGesture();
        RequireLink(vm.Template.DatabaseConfig, "A cancelled gesture");
        return Task.CompletedTask;
    }

    public static Task RotationAndMultiObjectGesturesAreOneUndoStep()
    {
        var vm = new MainViewModel();
        vm.BeginTemplateEditGesture();
        vm.Template.Objects.Add(Rectangle("rotate-a", 5));
        vm.Template.Objects.Add(Rectangle("rotate-b", 25));
        vm.CommitTemplateEditGesture();

        vm.SelectedObject = vm.Template.Objects[0];
        vm.SetRotationCommand.Execute("90");
        vm.UndoCommand.Execute(null);
        Require(vm.Template.Objects.Count == 2 && vm.Template.Objects[0].Rotation == 0,
            "Undo after a rotation must undo only the rotation.");

        // A group resize changes several objects inside one canvas gesture.
        vm.BeginTemplateEditGesture();
        foreach (var item in vm.Template.Objects)
        {
            item.WidthMm *= 2;
            item.HeightMm *= 2;
        }

        vm.CommitTemplateEditGesture();
        vm.UndoCommand.Execute(null);
        Require(vm.Template.Objects.All(item => item.WidthMm == 10 && item.HeightMm == 6),
            "One undo must restore every object of a group resize.");
        Require(vm.Template.Objects.Count == 2, "Undoing the group resize must not also undo the earlier creation.");
        return Task.CompletedTask;
    }

    public static Task CanvasPasteAndGroupResizeAreSingleGestures()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                var template = new LabelTemplate { WidthMm = 100, HeightMm = 50 };
                var first = Rectangle("paste-a", 5);
                var second = Rectangle("paste-b", 25);
                template.Objects.Add(first);
                template.Objects.Add(second);
                var canvas = new LabelDesignerCanvas { Template = template, Zoom = 1, Width = 400, Height = 200 };
                var decorator = new AdornerDecorator { Child = canvas };
                decorator.Measure(new Size(400, 200));
                decorator.Arrange(new Rect(0, 0, 400, 200));
                var started = 0;
                var completed = 0;
                canvas.EditGestureStarted += (_, _) => started++;
                canvas.EditGestureCompleted += (_, _) => completed++;

                var selection = (HashSet<LabelObject>)Field(canvas, "_selectedObjects");
                selection.Add(first);
                selection.Add(second);
                Require((bool)Invoke(canvas, "CopySelection")!, "Copy needs the two selected objects.");
                Require((bool)Invoke(canvas, "PasteSelection")!, "Paste must add the copied objects.");
                Require(template.Objects.Count == 4, "Paste must add both copies.");
                Require(started == 1 && completed == 1, $"One paste must be one gesture (started {started}, completed {completed}).");

                started = completed = 0;
                Invoke(canvas, "ShowGroupResizeAdorner");
                var adorner = (SelectionResizeAdorner?)Field(canvas, "_groupResizeAdorner");
                Require(adorner is not null, "Two selected objects must get the group resize frame.");
                typeof(SelectionResizeAdorner).GetMethod("BeginResize", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(adorner, null);
                typeof(SelectionResizeAdorner).GetMethod("CompleteResize", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(adorner, [false]);
                Require(started == 1 && completed == 1, $"One group resize must be one gesture (started {started}, completed {completed}).");
            }
            catch (Exception ex)
            {
                failure = ex;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (failure is not null)
        {
            throw failure;
        }

        return Task.CompletedTask;
    }

    public static Task MissingFontOrImageIsShownWhileEditing()
    {
        const string missingFont = "ANLAbel Missing Font 7Q";
        var vm = new MainViewModel();
        var text = new LabelObject { Id = "missing-font", Type = ObjectType.Text, Name = "Tiêu đề", Text = "ABC", XMm = 2, YMm = 2, WidthMm = 20, HeightMm = 5 };
        text.Style.FontFamily = missingFont;
        var image = new LabelObject { Id = "missing-image", Type = ObjectType.Image, Name = "Logo", XMm = 30, YMm = 2, WidthMm = 10, HeightMm = 10 };
        vm.BeginTemplateEditGesture();
        vm.Template.Objects.Add(text);
        vm.Template.Objects.Add(image);
        vm.CommitTemplateEditGesture();

        Require(vm.HasMissingResources && vm.MissingResourcesText == "Thiếu 1 phông chữ, 1 ảnh",
            "A missing font and an image without data must show in the status bar: " + vm.MissingResourcesText);
        Require(vm.MissingResourcesDetail.Contains($"phông “{missingFont}” chưa cài trên máy này", StringComparison.Ordinal)
                && vm.MissingResourcesDetail.Contains("Ảnh “Logo” không có dữ liệu ảnh", StringComparison.Ordinal),
            "The detail must name each object and what is missing: " + vm.MissingResourcesDetail);
        vm.SelectFirstMissingResource();
        Require(vm.SelectedObject?.Id == "missing-font", "The chip must select the first affected object.");

        vm.BeginTemplateEditGesture();
        vm.Template.Objects.Single(item => item.Id == "missing-font").Style.FontFamily =
            ANLAbel.Printing.RenderPipeline.TextBoxOverflowDetector.ResolveFontFamilyName(missingFont);
        vm.Template.Objects.Remove(vm.Template.Objects.Single(item => item.Id == "missing-image"));
        vm.CommitTemplateEditGesture();
        Require(!vm.HasMissingResources && vm.MissingResourcesText.Length == 0, "Fixing both must clear the chip.");

        var root = AppContext.BaseDirectory;
        while (!File.Exists(Path.Combine(root, "ANLAbel.slnx")))
        {
            root = Path.GetDirectoryName(root.TrimEnd(Path.DirectorySeparatorChar))!;
        }

        var xaml = File.ReadAllText(Path.Combine(root, "src", "ANLAbel.App", "MainWindow.xaml"));
        Require(xaml.Contains("Shell.Status.MissingResources", StringComparison.Ordinal)
                && xaml.Contains("{Binding MissingResourcesText}", StringComparison.Ordinal),
            "The missing-resource chip belongs in the status bar (Figma 37:259), not on the label.");
        return Task.CompletedTask;
    }

    private static LabelObject Rectangle(string id, double x) => new()
    {
        Id = id,
        Type = ObjectType.Rectangle,
        Name = id,
        XMm = x,
        YMm = 5,
        WidthMm = 10,
        HeightMm = 6
    };

    private static void RequireLink(DatabaseConfig config, string action)
    {
        Require(config.FilePath == @"C:\data\linked.xlsx" && config.SheetName == "Data"
                && config.HeaderRowIndex == 2 && config.CopiesField == "Qty",
            $"{action} must keep the current data link (file, sheet, header row, copies column).");
    }

    private static object Field(object target, string name)
        => target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(target)!;

    private static object? Invoke(object target, string name)
        => target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(target, null);

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
