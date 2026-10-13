using System.Windows.Controls;
using ANLAbel.App.Controls;
using ANLAbel.Core.Models;

/// <summary>
/// Plan 13 GĐ3 (Figma 37:208): the canvas right-click menu and its status
/// messages are Vietnamese, and the developer telemetry toggle is not offered
/// to operators. Each method is one named gate in Program.cs.
/// </summary>
internal static class DesignerLanguageRegression
{
    public static Task CanvasContextMenuIsVietnameseWithoutDeveloperItems()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                var canvas = new LabelDesignerCanvas
                {
                    Width = 180,
                    Height = 120,
                    Template = new LabelTemplate { WidthMm = 40, HeightMm = 25 }
                };
                var headers = new List<string>();
                Collect(canvas.ContextMenu!.Items, headers);

                Require(headers.Contains("Dính theo đối tượng") && headers.Contains("Đường gióng") && headers.Contains("Bước lưới"),
                    "The canvas menu must use the Vietnamese labels: " + string.Join(" | ", headers));
                string[] english = ["Snap", "Grid", "guide", "Guide", "pointer", "performance", "Lock", "Delete", "Clear", "Add "];
                var leftover = headers.Where(header => english.Any(word => header.Contains(word, StringComparison.Ordinal))).ToArray();
                Require(leftover.Length == 0, "English menu labels remain: " + string.Join(" | ", leftover));
                Require(headers.Count(header => header.EndsWith(" mm", StringComparison.Ordinal)) == 5,
                    "The grid step choices must stay in millimetres.");

                canvas.IsSnapToObjectsEnabled = !canvas.IsSnapToObjectsEnabled;
                Require(canvas.InteractionStatusText.StartsWith("Đã ", StringComparison.Ordinal),
                    "Snap feedback must be Vietnamese: " + canvas.InteractionStatusText);
                Require(!canvas.ShowPointerTelemetry, "The developer telemetry overlay stays off for operators.");
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

    private static void Collect(ItemCollection items, List<string> headers)
    {
        foreach (var item in items.OfType<MenuItem>())
        {
            if (item.Header is string header)
            {
                headers.Add(header);
            }

            Collect(item.Items, headers);
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
