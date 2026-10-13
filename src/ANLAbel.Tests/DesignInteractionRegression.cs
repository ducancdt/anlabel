using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using ANLAbel.App.Controls;
using ANLAbel.Core.Enums;
using ANLAbel.Core.Models;

/// <summary>
/// Plan 13 GĐ3 D01/D03: a resize through the real selection handle changes the
/// object by the same physical millimetres at 50, 100 and 200 % zoom, and is
/// one committed gesture. Windows display scaling is not simulated here.
/// </summary>
internal static class DesignInteractionRegression
{
    public static Task HandleResizeKeepsMillimetresAtEveryZoom()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                foreach (var zoom in new[] { 0.5, 1.0, 2.0 })
                {
                    var item = new LabelObject { Id = $"zoom-{zoom}", Type = ObjectType.Rectangle, Name = "Khung", XMm = 10, YMm = 10, WidthMm = 20, HeightMm = 10 };
                    var template = new LabelTemplate { WidthMm = 100, HeightMm = 50 };
                    template.Objects.Add(item);
                    var canvas = new LabelDesignerCanvas
                    {
                        Template = template,
                        Zoom = zoom,
                        Width = 1000,
                        Height = 600,
                        IsSnapToObjectsEnabled = false,
                        IsSnapToGridEnabled = false
                    };
                    var decorator = new AdornerDecorator { Child = canvas };
                    decorator.Measure(new Size(1000, 600));
                    decorator.Arrange(new Rect(0, 0, 1000, 600));
                    var started = 0;
                    var completed = 0;
                    canvas.EditGestureStarted += (_, _) => started++;
                    canvas.EditGestureCompleted += (_, _) => completed++;
                    canvas.SelectedObject = item;
                    decorator.UpdateLayout();

                    var adorner = typeof(LabelDesignerCanvas).GetField("_selectionAdorner", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(canvas)
                        ?? throw new InvalidOperationException($"Zoom {zoom}: the selection handles were not shown.");
                    var handle = (Thumb)typeof(SelectionResizeAdorner).GetField("_bottomRight", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(adorner)!;

                    // Drag the bottom-right handle 20 × 10 screen DIP.
                    handle.RaiseEvent(new DragStartedEventArgs(0, 0));
                    handle.RaiseEvent(new DragDeltaEventArgs(20, 10));
                    handle.RaiseEvent(new DragCompletedEventArgs(20, 10, false));

                    var expectedWidth = 20 + 20 / zoom * 25.4 / 96;
                    var expectedHeight = 10 + 10 / zoom * 25.4 / 96;
                    // The canvas keeps resized geometry at 0.01 mm.
                    Require(Math.Abs(item.WidthMm - expectedWidth) < 0.006 && Math.Abs(item.HeightMm - expectedHeight) < 0.006,
                        $"Zoom {zoom:P0}: expected {expectedWidth:0.###} × {expectedHeight:0.###} mm, got {item.WidthMm:0.###} × {item.HeightMm:0.###} mm.");
                    Require(item.XMm == 10 && item.YMm == 10, $"Zoom {zoom:P0}: a bottom-right resize must not move the object.");
                    Require(started == 1 && completed == 1, $"Zoom {zoom:P0}: one handle drag must be one gesture (started {started}, completed {completed}).");
                }
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

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
