using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using ANLAbel.App;
using ANLAbel.Core.Enums;
using ANLAbel.Core.Geometry;
using ANLAbel.Core.Models;
using ANLAbel.Core.Printing;
using ANLAbel.Printing.PrinterProfiles;
using ANLAbel.Printing.RenderPipeline;

/// <summary>
/// Paper size and paper choice as NiceLabel 10 has them (Label Properties ›
/// Label Dimensions: margins, labels across, processing order; Paper: paper
/// type, paper, rotated, preview), and the page the print service sends for
/// a label on a sheet of paper.
/// </summary>
internal static class PaperLayoutRegression
{
    /// <summary>
    /// 60 × 40 mm labels, 2 × 4 on A5 from 5 mm margins with 3 mm gaps: the
    /// right and bottom margins are what the paper leaves (NiceLabel), a
    /// third column is refused before Apply, and Apply hands the grid on.
    /// </summary>
    public static Task LabelPropertiesLayLabelsOnASheet()
    {
        string? right = null, bottom = null, summary = null, blockedSummary = null, blockedRight = null;
        int previewShapes = 0;
        bool? applyEnabled = null, blockedApplyEnabled = null;
        PageLayoutSelection? selected = null;
        PrinterPaperInfo? media = null;
        RunOnSta(() =>
        {
            var printers = new[] { new PrinterInfo { Name = "Office A5", DriverName = "PCL 6" } };
            var window = new PrinterSetupWindow(
                printers,
                initialPrinterName: "Office A5",
                initialPageSizeSource: PaperSizeSource.Manual,
                initialDriverMediaIdentity: "A5-id",
                capabilityService: new SheetCapabilityStub(),
                initialWidthMm: 60,
                initialHeightMm: 40,
                initialOffsetXMm: 5,
                initialOffsetYMm: 5,
                initialLayout: new PageLayoutSelection(2, 4, 3, 3, false, false, false, false));
            T Field<T>(string name) => (T)typeof(PrinterSetupWindow).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(window)!;
            Exception? inner = null;
            window.Loaded += (_, _) =>
            {
                try
                {
                    Pump();
                    var mediaBox = Field<ComboBox>("DriverMediaBox");
                    Require(mediaBox.SelectedItem is PrinterPaperInfo { WidthMm: 148 }, "Reopening Label Properties must select the saved A5 paper.");
                    right = Field<TextBox>("RightMarginBox").Text;
                    bottom = Field<TextBox>("BottomMarginBox").Text;
                    summary = Field<TextBlock>("SheetSummaryText").Text;
                    previewShapes = Field<Canvas>("LayoutPreview").Children.Count;
                    applyEnabled = Field<Button>("ApplyButton").IsEnabled;

                    Field<TextBox>("LabelsAcrossBox").Text = "3";
                    Pump();
                    blockedSummary = Field<TextBlock>("SheetSummaryText").Text;
                    blockedRight = Field<TextBox>("RightMarginBox").Text;
                    blockedApplyEnabled = Field<Button>("ApplyButton").IsEnabled;

                    Field<TextBox>("LabelsAcrossBox").Text = "2";
                    Pump();
                    typeof(PrinterSetupWindow).GetMethod("Apply_Click", BindingFlags.Instance | BindingFlags.NonPublic)!
                        .Invoke(window, new object?[] { window, new RoutedEventArgs() });
                    selected = window.SelectedLayout;
                    media = window.SelectedMedia;
                }
                catch (Exception ex)
                {
                    inner = ex;
                }
                finally
                {
                    if (window.DialogResult is null)
                    {
                        window.DialogResult = false;
                    }
                }
            };
            window.ShowDialog();
            if (inner is not null)
            {
                throw inner;
            }
        });

        Require(right == "20" && bottom == "36", $"148 − (5 + 2 × 60 + 3) = 20 mm right and 210 − (5 + 4 × 40 + 3 × 3) = 36 mm bottom margin expected, got {right} / {bottom}.");
        Require(summary?.Contains("2 × 4 tem (8 tem mỗi trang)", StringComparison.Ordinal) == true, "The page summary must count the labels: " + summary);
        Require(previewShapes == 1 + 8 + 8, $"The preview must draw the paper, 8 cells and their print order numbers, drew {previewShapes} shapes.");
        Require(applyEnabled == true, "A grid that fits must be applicable.");
        Require(blockedSummary?.Contains("Lưới 3 × 4 tem cần", StringComparison.Ordinal) == true && blockedRight == "-43",
            "A third 60 mm column on A5 must be refused in Vietnamese before Apply: " + blockedSummary + " / " + blockedRight);
        Require(blockedApplyEnabled == true, "The compatibility card stays a driver check; the grid is refused on Apply with the same reason.");
        Require(selected == new PageLayoutSelection(2, 4, 3, 3, false, false, false, false), "Apply must hand on the grid as typed: " + selected);
        Require(media is { WidthMm: 148, HeightMm: 210 }, "Apply must hand on the A5 paper.");

        var template = new LabelTemplate { Name = "Sheet", WidthMm = 60, HeightMm = 40, Dpi = 300 };
        PrinterSelectionApplier.Apply(template, new PrinterInfo { Name = "Office A5" }, new PrinterPaperInfo { Name = "Custom", WidthMm = 60, HeightMm = 40 },
            300, LabelOrientation.Landscape, media, PaperSizeSource.Manual, "PCL 6", PrinterSettingsSource.Label, 5, 5, 1, 1, selected);
        var profile = template.PrinterProfile;
        Require(profile is { LabelsAcross: 2, LabelsDown: 4, HorizontalGapMm: 3, VerticalGapMm: 3, PhysicalWidthMm: 148, PhysicalHeightMm: 210, OffsetXMm: 5, OffsetYMm: 5 },
            "The label must keep the grid, the A5 sheet and the margins.");
        var refused = false;
        try
        {
            PrinterSelectionApplier.Apply(template, new PrinterInfo { Name = "Office A5" }, new PrinterPaperInfo { Name = "Custom", WidthMm = 60, HeightMm = 40 },
                300, LabelOrientation.Landscape, media, PaperSizeSource.Manual, "PCL 6", PrinterSettingsSource.Label, 5, 5, 1, 1,
                selected! with { LabelsAcross = 3 });
        }
        catch (InvalidOperationException)
        {
            refused = true;
        }

        Require(refused && template.PrinterProfile.LabelsAcross == 2, "A grid wider than the sheet must not reach the label.");
        return Task.CompletedTask;
    }

    /// <summary>
    /// A 100 × 50 mm label chosen on A5 paper prints A5 pages with the label
    /// at its margins; before this the page was only the label's size and the
    /// driver placed it, cutting off what the margins moved past the label.
    /// Rotated 180° turns the whole page (NiceLabel 10 Paper › Rotated).
    /// </summary>
    public static Task OneLabelOnASheetPrintsTheWholeSheet()
    {
        RunOnSta(() =>
        {
            foreach (var rotated in new[] { false, true })
            {
                var template = new LabelTemplate { Name = "A5", WidthMm = 100, HeightMm = 50, Dpi = 300 };
                template.Objects.Add(new LabelObject
                {
                    Type = ObjectType.Rectangle,
                    Name = "Block",
                    XMm = 0,
                    YMm = 0,
                    WidthMm = 100,
                    HeightMm = 50,
                    Style = { FillStyle = FillStyle.Solid, FillColor = "#FF000000", BorderThicknessMm = 0, OutlineStyle = OutlineStyle.None }
                });
                var profile = template.PrinterProfile;
                profile.SettingsSource = PrinterSettingsSource.Label;
                profile.PaperSizeSource = PaperSizeSource.Manual;
                profile.PhysicalWidthMm = 148;
                profile.PhysicalHeightMm = 210;
                profile.OffsetXMm = 10;
                profile.OffsetYMm = 20;
                profile.Rotated180 = rotated;
                var plan = new PrintRenderPlan
                {
                    Dpi = 300,
                    DpiX = 300,
                    DpiY = 300,
                    LabelWidthMm = 100,
                    LabelHeightMm = 50,
                    OffsetXMm = 10,
                    OffsetYMm = 20,
                    ScaleX = 1,
                    ScaleY = 1,
                    Rotated180 = rotated
                };
                IReadOnlyDictionary<string, string>?[] rows = [null, null];
                var paginator = (DocumentPaginator)typeof(PrintService)
                    .GetMethod("CreateRowsPaginator", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .Invoke(new PrintService(), new object?[] { template, rows, plan, 0 })!;
                Require(paginator.PageCount == 2, "Each row prints its own A5 page.");
                Require(Math.Abs(paginator.PageSize.Width - MmConverter.MmToDip(148)) < 0.5 && Math.Abs(paginator.PageSize.Height - MmConverter.MmToDip(210)) < 0.5,
                    $"The page sent to the driver must be the A5 sheet, not the label: {paginator.PageSize}.");

                var page = paginator.GetPage(0);
                var width = (int)Math.Ceiling(page.Size.Width);
                var height = (int)Math.Ceiling(page.Size.Height);
                var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
                bitmap.Render(page.Visual);
                var pixels = new byte[width * height * 4];
                bitmap.CopyPixels(pixels, width * 4, 0);
                bool Dark(double xMm, double yMm)
                {
                    var x = (int)MmConverter.MmToDip(xMm);
                    var y = (int)MmConverter.MmToDip(yMm);
                    return pixels[(y * width + x) * 4 + 1] < 64;
                }

                // Upright: the label covers x 10–110 mm, y 20–70 mm; turned, x 38–138 mm, y 140–190 mm.
                var (inX, inY, outX, outY) = rotated ? (88.0, 165.0, 60.0, 45.0) : (60.0, 45.0, 88.0, 165.0);
                Require(Dark(inX, inY), $"Rotated {rotated}: the label must print at its margins ({inX}, {inY} mm).");
                Require(!Dark(outX, outY) && !Dark(5, 10), $"Rotated {rotated}: the rest of the sheet stays white.");
                Require(Dark(rotated ? 40 : 108, rotated ? 142 : 68), $"Rotated {rotated}: the label's far corner must not be cut off by a label-sized page.");
            }

            // Label stock of the label's own size keeps label pages.
            var stock = new LabelTemplate { Name = "Stock", WidthMm = 100, HeightMm = 50, Dpi = 203 };
            stock.PrinterProfile.SettingsSource = PrinterSettingsSource.Label;
            stock.PrinterProfile.PaperSizeSource = PaperSizeSource.Manual;
            stock.PrinterProfile.PhysicalWidthMm = 100;
            stock.PrinterProfile.PhysicalHeightMm = 50;
            var stockPlan = new PrintRenderPlan { Dpi = 203, DpiX = 203, DpiY = 203, LabelWidthMm = 100, LabelHeightMm = 50, ScaleX = 1, ScaleY = 1 };
            var stockPages = (DocumentPaginator)typeof(PrintService)
                .GetMethod("CreateRowsPaginator", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(new PrintService(), new object?[] { stock, new IReadOnlyDictionary<string, string>?[] { null }, stockPlan, 0 })!;
            Require(Math.Abs(stockPages.PageSize.Width - MmConverter.MmToDip(100)) < 0.5, "Label stock keeps a page of the label's size.");
        });

        return Task.CompletedTask;
    }

    /// <summary>
    /// NiceLabel 10 New Label Setup Wizard: a new label continues into Label
    /// Properties (offered by default) as guided steps: Back is off on the first
    /// page, Next walks printer, label dimensions and paper, and only the last
    /// page offers Finish.
    /// </summary>
    public static Task NewLabelWalksLabelPropertiesStepByStep()
    {
        var steps = new List<string>();
        RunOnSta(() =>
        {
            var offered = new NewTemplateWindow().OpenLabelPropertiesNext;
            Require(offered, "A new label must offer to continue into Label Properties by default.");
            var printers = new[] { new PrinterInfo { Name = "Office A5", DriverName = "PCL 6" } };
            var window = new PrinterSetupWindow(printers, initialPrinterName: "Office A5", capabilityService: new SheetCapabilityStub(), initialWidthMm: 60, initialHeightMm: 40)
            {
                IsGuided = true
            };
            T Field<T>(string name) => (T)typeof(PrinterSetupWindow).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(window)!;
            Exception? inner = null;
            window.Loaded += (_, _) =>
            {
                try
                {
                    Pump();
                    var tabs = Field<TabControl>("SetupTabs");
                    for (var page = 0; page < tabs.Items.Count; page++)
                    {
                        var back = Field<Button>("BackButton");
                        var next = Field<Button>("NextButton");
                        var finish = Field<Button>("ApplyButton");
                        steps.Add($"{tabs.SelectedIndex}:{back.IsEnabled}:{next.Visibility}:{finish.Visibility}:{finish.Content}:{Field<TextBlock>("FooterText").Text}");
                        if (next.Visibility == Visibility.Visible)
                        {
                            next.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
                            Pump();
                        }
                    }

                    Field<Button>("BackButton").RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
                    Pump();
                    steps.Add($"back:{tabs.SelectedIndex}");
                }
                catch (Exception ex)
                {
                    inner = ex;
                }
                finally
                {
                    window.DialogResult = false;
                }
            };
            window.ShowDialog();
            if (inner is not null)
            {
                throw inner;
            }
        });

        Require(steps.Count == 4, "Three pages and one step back expected: " + string.Join(" | ", steps));
        Require(steps[0] == "0:False:Visible:Collapsed:Hoàn tất:Bước 1/3 · Máy in", "First page: Back off, Next on, no Finish: " + steps[0]);
        Require(steps[1] == "1:True:Visible:Collapsed:Hoàn tất:Bước 2/3 · Kích thước tem", "Second page: " + steps[1]);
        Require(steps[2] == "2:True:Collapsed:Visible:Hoàn tất:Bước 3/3 · Giấy · Hoàn tất để áp dụng", "Last page: Finish only: " + steps[2]);
        Require(steps[3] == "back:1", "Back returns to the previous page: " + steps[3]);
        return Task.CompletedTask;
    }

    private sealed class SheetCapabilityStub : IPrinterCapabilityService
    {
        public Task<PrinterCapabilitySnapshot> GetSnapshotAsync(string queueName, CancellationToken cancellationToken = default)
        {
            PrinterMediaCandidate[] media =
            [
                new("A5 (148 x 210mm)", 148, 210, PrinterMediaProvenance.DriverWin32, "A5-id"),
                new("A4 (210 x 297mm)", 210, 297, PrinterMediaProvenance.DriverWin32, "A4-id")
            ];
            return Task.FromResult(new PrinterCapabilitySnapshot(
                queueName,
                "PCL 6",
                DateTimeOffset.UtcNow,
                IsAvailable: true,
                IsComplete: true,
                SupportsCustomMedia: false,
                media));
        }
    }

    private static void Pump()
    {
        var frame = new DispatcherFrame();
        Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => frame.Continue = false));
        Dispatcher.PushFrame(frame);
    }

    private static void RunOnSta(Action work)
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                work();
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
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
