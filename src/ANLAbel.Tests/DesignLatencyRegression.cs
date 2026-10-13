using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ANLAbel.App.Controls;
using ANLAbel.App.ViewModels;
using ANLAbel.Core.Enums;
using ANLAbel.Core.Models;
using ANLAbel.Project.SaveLoad;

/// <summary>
/// Plan 13 GĐ3: design operations on a 200-object label and on the largest
/// real template stay responsive. Prints the measured times (MEASURE lines)
/// and fails only on order-of-magnitude regressions, not on machine noise.
/// </summary>
internal static class DesignLatencyRegression
{
    public static async Task DesignStaysResponsiveWith200Objects()
    {
        var pngBytes = RunOnSta(() => CreatePng(300, 300));
        var template = Build200ObjectTemplate(Convert.ToBase64String(pngBytes));
        var directory = Path.Combine(Path.GetTempPath(), "anlabel-latency-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var path = Path.Combine(directory, "200-objects.anlabel");
            await new ProjectFileService().SaveAsync(template, path);
            var synthetic = await MeasureAsync(path, "200 đối tượng");
            Require(synthetic.OpenMs < 5000, $"Opening a 200-object label took {synthetic.OpenMs:0} ms.");
            Require(synthetic.PropertyEditMs < 150, $"One property edit took {synthetic.PropertyEditMs:0.0} ms.");
            Require(synthetic.UndoMs < 1000, $"Undo took {synthetic.UndoMs:0} ms.");
            Require(synthetic.CanvasBuildMs < 5000, $"Building the canvas took {synthetic.CanvasBuildMs:0} ms.");
            Require(synthetic.DragTickMs < 30, $"One drag tick took {synthetic.DragTickMs:0.0} ms.");

            // The largest real template stays on the operator's machine only
            // (customer data is not committed); measure it when present.
            var real = Path.Combine(FindRepositoryRoot(), "form tem nhãn", "anlabel", "tem ngoai REDACTED-14.anlabel");
            if (File.Exists(real))
            {
                await MeasureAsync(real, "mẫu thật “tem ngoai REDACTED-14”");
            }
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private sealed record Measurement(double OpenMs, double PropertyEditMs, double UndoMs, double CanvasBuildMs, double DragTickMs);

    private static async Task<Measurement> MeasureAsync(string path, string label)
    {
        var vm = new MainViewModel();
        var watch = Stopwatch.StartNew();
        await vm.OpenAsync(path);
        var openMs = watch.Elapsed.TotalMilliseconds;
        var count = vm.Template.Objects.Count;

        // Properties-panel edits outside a canvas gesture (each one records history).
        vm.SelectedObject = vm.Template.Objects[count / 2];
        const int edits = 40;
        watch.Restart();
        for (var edit = 0; edit < edits; edit++)
        {
            vm.SelectedObject!.XMm += 0.1;
        }

        var editMs = watch.Elapsed.TotalMilliseconds / edits;
        watch.Restart();
        vm.UndoCommand.Execute(null);
        var undoMs = watch.Elapsed.TotalMilliseconds;

        // The canvas brackets a drag in one edit gesture, as MainWindow wires it.
        var snapshot = vm.Template;
        vm.BeginTemplateEditGesture();
        var (buildMs, tickMs) = RunOnSta(() =>
        {
            var canvas = new LabelDesignerCanvas { Template = snapshot, Zoom = 1, Width = 800, Height = 1000 };
            var build = Stopwatch.StartNew();
            canvas.Measure(new Size(800, 1000));
            canvas.Arrange(new Rect(0, 0, 800, 1000));
            canvas.UpdateLayout();
            var buildElapsed = build.Elapsed.TotalMilliseconds;

            var dragged = snapshot.Objects[snapshot.Objects.Count / 3];
            canvas.SelectedObject = dragged;
            // Same state as a pointer drag: the canvas owns a drag of this object.
            typeof(LabelDesignerCanvas).GetField("_dragObject", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .SetValue(canvas, dragged);
            const int ticks = 100;
            var drag = Stopwatch.StartNew();
            for (var tick = 0; tick < ticks; tick++)
            {
                dragged.XMm += 0.05;
                dragged.YMm += 0.05;
                canvas.UpdateLayout();
            }

            return (buildElapsed, drag.Elapsed.TotalMilliseconds / ticks);
        });
        vm.CommitTemplateEditGesture();

        Console.WriteLine(
            $"MEASURE thiết kế {label} ({count} đối tượng): mở {openMs:0} ms; sửa thuộc tính {editMs:0.0} ms/lần; " +
            $"hoàn tác {undoMs:0} ms; dựng canvas {buildMs:0} ms; kéo {tickMs:0.00} ms/nhịp");
        return new Measurement(openMs, editMs, undoMs, buildMs, tickMs);
    }

    private static LabelTemplate Build200ObjectTemplate(string imageData)
    {
        var template = new LabelTemplate { Name = "200 đối tượng", WidthMm = 100, HeightMm = 150 };
        for (var index = 0; index < 200; index++)
        {
            var x = 2 + index % 10 * 9.5;
            var y = 2 + index / 10 * 7.2;
            var item = (index % 10) switch
            {
                0 or 1 or 2 or 3 => new LabelObject { Type = ObjectType.Text, Text = $"Dòng {index}" },
                4 or 5 => new LabelObject { Type = ObjectType.TextBox, Text = $"Khung chữ {index}" },
                6 => new LabelObject { Type = ObjectType.BarcodeCode128, BarcodeSymbology = BarcodeSymbology.Code128, Text = $"ABC{index:0000}" },
                7 => new LabelObject { Type = ObjectType.QRCode, Text = $"QR-{index:0000}" },
                8 => new LabelObject { Type = ObjectType.Rectangle },
                _ => index % 40 == 9
                    ? new LabelObject { Type = ObjectType.Image, ImageDataBase64 = imageData, ImagePixelWidth = 300, ImagePixelHeight = 300 }
                    : new LabelObject { Type = ObjectType.Line, LineEndXMm = x + 8, LineEndYMm = y },
            };
            item.Id = $"latency-{index}";
            item.Name = $"Đối tượng {index}";
            item.XMm = x;
            item.YMm = y;
            item.WidthMm = 8;
            item.HeightMm = item.Type is ObjectType.QRCode or ObjectType.Image ? 6.5 : 5;
            item.ZIndex = index + 1;
            template.Objects.Add(item);
        }

        return template;
    }

    private static byte[] CreatePng(int width, int height)
    {
        var visual = new DrawingVisual();
        using (var context = visual.RenderOpen())
        {
            context.DrawRectangle(Brushes.White, null, new Rect(0, 0, width, height));
            context.DrawEllipse(Brushes.Black, null, new Point(width / 2.0, height / 2.0), width / 3.0, height / 3.0);
        }

        var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = new MemoryStream();
        encoder.Save(stream);
        return stream.ToArray();
    }

    private static T RunOnSta<T>(Func<T> work)
    {
        T result = default!;
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                result = work();
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

        return result;
    }

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "ANLAbel.slnx")))
            {
                return directory.FullName;
            }
        }

        throw new InvalidOperationException("Could not locate the repository root.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
