using System.Printing;
using ANLAbel.App;
using ANLAbel.App.Services;
using ANLAbel.Printing.PrinterProfiles;
using ANLAbel.Project.SaveLoad;

/// <summary>
/// Plan 13 §9, QĐ-12: a template set up on another PC whose printer queue has the
/// same name but another driver. Reproduced on one PC with the installed
/// XP-DT426B queue and a driver name saved by "the other PC"; the driver is only
/// read, nothing is printed. Printing stops before any job with both driver
/// names and the fix, and running Printer Setup on this PC clears the stop.
/// Skipped (INFO) when the queue is not installed or cannot be used right now.
/// </summary>
internal static class DriverIdentityRegression
{
    private const string OperatorQueue = "Xprinter XP-DT426B";

    public static async Task TemplateFromAnotherDriverStopsUntilPrinterSetup()
    {
        if (!IsQueueInstalled(OperatorQueue))
        {
            Console.WriteLine($"INFO driver identity case skipped: queue “{OperatorQueue}” is not installed on this PC.");
            return;
        }

        var root = AppContext.BaseDirectory;
        while (!File.Exists(Path.Combine(root, "ANLAbel.slnx")))
        {
            root = Path.GetDirectoryName(root.TrimEnd(Path.DirectorySeparatorChar))!;
        }

        var template = await new ProjectFileService().LoadAsync(
            Path.Combine(root, "src", "ANLAbel.App", "TemplateLibrary", "01_part_40x20_barcode.anlabel"));
        var snapshot = await new PrinterCapabilityService().GetSnapshotAsync(OperatorQueue);
        var thisDriver = snapshot.DriverIdentity;
        var service = new PrintService();

        // Printer Setup on this PC, with the paper the driver is set to, so only
        // the driver identity can stop the plan (the objects are not checked here).
        var (paperWidth, paperHeight) = DriverPaperMm(OperatorQueue) ?? (template.WidthMm, template.HeightMm);
        var paper = new PrinterPaperInfo { Name = $"{paperWidth:0.##} x {paperHeight:0.##} mm", WidthMm = paperWidth, HeightMm = paperHeight };
        var orientation = paperWidth >= paperHeight ? ANLAbel.Core.Enums.LabelOrientation.Landscape : ANLAbel.Core.Enums.LabelOrientation.Portrait;
        void SetUpOnThisPc() => PrinterSelectionApplier.Apply(
            template,
            new PrinterInfo { Name = OperatorQueue, DriverName = thisDriver },
            paper,
            template.Dpi,
            orientation,
            driverIdentity: thisDriver);

        SetUpOnThisPc();
        ANLAbel.Printing.RenderPipeline.PrintRenderPlan verified;
        try
        {
            verified = await service.CreateEffectivePlanAsync(template, OperatorQueue);
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"INFO driver identity case skipped: queue “{OperatorQueue}” cannot be used right now ({ex.Message}).");
            return;
        }

        // Set up on the other PC: same queue name and label, another driver.
        template.PrinterProfile.DriverIdentity = "Seagull " + thisDriver;
        string stop;
        try
        {
            await service.CreateEffectivePlanAsync(template, OperatorQueue);
            throw new InvalidOperationException("A template saved with another driver behind the same queue must stop before any job.");
        }
        catch (InvalidOperationException ex) when (ex.Message.StartsWith("Printing stopped because the saved driver", StringComparison.Ordinal))
        {
            stop = ex.Message;
        }

        var explained = PrintFailureExplainer.Explain(stop);
        Require(explained.Problem.StartsWith($"Driver máy in đã đổi từ “Seagull {thisDriver}” sang “{thisDriver}”.", StringComparison.Ordinal)
                && explained.NextAction.StartsWith("Mở Cài đặt máy in", StringComparison.Ordinal),
            $"The operator must see both drivers and the fix: {explained.StatusLine}");

        // Printer Setup on this PC records its own driver again, and the plan is
        // the one the label had before.
        SetUpOnThisPc();
        var again = await service.CreateEffectivePlanAsync(template, OperatorQueue);
        Console.WriteLine($"MEASURE driver identity: {thisDriver} at {verified.DpiX}×{verified.DpiY} DPI; another saved driver stops before any job; Printer Setup clears it ({again.DpiX}×{again.DpiY} DPI).");
        Require(again.DpiX == verified.DpiX && again.DpiY == verified.DpiY
                && Math.Abs(again.LabelWidthMm - verified.LabelWidthMm) < 0.01 && Math.Abs(again.LabelHeightMm - verified.LabelHeightMm) < 0.01,
            "After Printer Setup the label must plan as it did on this PC before.");
    }

    /// <summary>The paper the queue's own ticket resolves to, as the print plan reads it; nothing is printed.</summary>
    private static (double WidthMm, double HeightMm)? DriverPaperMm(string name)
    {
        try
        {
            using var server = new LocalPrintServer();
            using var queue = server.GetPrintQueue(name);
            var ticket = queue.MergeAndValidatePrintTicket(queue.UserPrintTicket ?? queue.DefaultPrintTicket, new PrintTicket()).ValidatedPrintTicket;
            return ticket?.PageMediaSize is { Width: double width, Height: double height } && width > 0 && height > 0
                ? (Math.Round(width * 25.4 / 96, 2), Math.Round(height * 25.4 / 96, 2))
                : null;
        }
        catch (Exception ex) when (ex is PrintQueueException or PrintServerException or InvalidOperationException)
        {
            return null;
        }
    }

    private static bool IsQueueInstalled(string name)
    {
        try
        {
            using var server = new LocalPrintServer();
            return server.GetPrintQueues().Any(queue => string.Equals(queue.FullName, name, StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception ex) when (ex is PrintQueueException or PrintServerException or InvalidOperationException)
        {
            return false;
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
