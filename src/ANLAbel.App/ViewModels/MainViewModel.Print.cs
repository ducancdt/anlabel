using System.Data;
using System.IO;
using ANLAbel.Core.Expressions;
using ANLAbel.Core.Models;
using ANLAbel.Data.PrintLogs;

namespace ANLAbel.App.ViewModels;

public sealed partial class MainViewModel
{
    private async void PrintCurrentRow()
    {
        try
        {
            var rows = ExpandRowsForCopies(PreviewRow is null ? new IReadOnlyDictionary<string, string>?[] { null } : new IReadOnlyDictionary<string, string>?[] { PreviewRow }).ToArray();
            var validationError = ValidatePrintableContent(rows);
            if (validationError is not null)
            {
                StatusText = validationError;
                return;
            }

            if (_printService.PrintRows(Template, rows, $"{Template.Name} label"))
            {
                StatusText = $"Print job sent: {rows.Length} label(s)";
                await WritePrintLogAsync("Current row", rows, PreviewRow is null ? 0 : 1, rows.Length);
                OpenPrintHistoryFile();
            }
        }
        catch (Exception ex)
        {
            StatusText = $"Print failed: {ex.Message}";
        }
    }

    private async void PrintAllRows()
    {
        try
        {
            if (ExcelDataView is null || ExcelDataView.Count == 0)
            {
                StatusText = "No Excel rows to print";
                return;
            }

            var rows = ExpandRowsForCopies(ExcelDataView
                .Cast<DataRowView>()
                .Select(CreatePreviewRow)
                .Where(row => row is not null)
                .Cast<IReadOnlyDictionary<string, string>>()
                .Cast<IReadOnlyDictionary<string, string>?>()).ToArray();

            var validationError = ValidatePrintableContent(rows);
            if (validationError is not null)
            {
                StatusText = validationError;
                return;
            }

            if (_printService.PrintRows(Template, rows, $"{Template.Name} labels"))
            {
                StatusText = $"Print job sent: {rows.Length} labels";
                await WritePrintLogAsync("All rows", rows, ExcelDataView.Count, rows.Length);
                OpenPrintHistoryFile();
            }
        }
        catch (Exception ex)
        {
            StatusText = $"Print failed: {ex.Message}";
        }
    }

    private void PrintCalibration()
    {
        try
        {
            if (_printService.PrintCalibration(Template))
            {
                StatusText = "Calibration print job sent";
            }
        }
        catch (Exception ex)
        {
            StatusText = $"Calibration print failed: {ex.Message}";
        }
    }

    private IEnumerable<IReadOnlyDictionary<string, string>?> ExpandRowsForCopies(IEnumerable<IReadOnlyDictionary<string, string>?> rows)
    {
        foreach (var row in rows)
        {
            var copies = GetCopyCount(row);
            for (var i = 0; i < copies; i++)
            {
                yield return row;
            }
        }
    }

    private int GetCopyCount(IReadOnlyDictionary<string, string>? row)
    {
        if (row is not null && !string.IsNullOrWhiteSpace(PrintCopiesField))
        {
            var field = PrintCopiesField.Trim();
            if (field.StartsWith('{') && field.EndsWith('}') && field.Length > 2)
            {
                field = field[1..^1];
            }

            if (row.TryGetValue(field, out var value) && int.TryParse(value, out var fieldCopies) && fieldCopies > 0)
            {
                return Math.Min(999, fieldCopies);
            }
        }

        return Math.Min(999, Math.Max(1, PrintCopies));
    }

    public async Task WritePrintLogAsync(string printMode, IEnumerable<IReadOnlyDictionary<string, string>?> rows, int rowCount, int labelCount, string notes = "")
    {
        try
        {
            var printedAt = DateTime.Now;
            var entries = rows.Select((row, index) => CreatePrintLogEntry(printMode, row, rowCount, labelCount, index + 1, printedAt, notes)).ToArray();
            await _printLogService.AppendManyAsync(entries);
        }
        catch (Exception ex)
        {
            StatusText = $"Print sent, but log failed: {ex.Message}";
        }
    }

    private PrintLogEntry CreatePrintLogEntry(string printMode, IReadOnlyDictionary<string, string>? row, int rowCount, int labelCount, int labelIndex, DateTime printedAt, string notes)
    {
        return new PrintLogEntry
        {
            PrintedAt = printedAt,
            TemplateName = Template.Name,
            TemplateFilePath = CurrentFilePath,
            PrinterName = Template.PrinterProfile.PrinterName,
            LabelWidthMm = Template.PrinterProfile.LabelWidthMm,
            LabelHeightMm = Template.PrinterProfile.LabelHeightMm,
            Dpi = Template.PrinterProfile.Dpi,
            PrintMode = printMode,
            RowCount = rowCount,
            LabelCount = labelCount,
            LabelIndex = labelIndex,
            ExcelFilePath = Template.DatabaseConfig.FilePath,
            ExcelSheetName = Template.DatabaseConfig.SheetName,
            PartNo = GetRowValue(row, "PartNo", "Part No", "PN", "MaHang", "Ma Hang", "Mã hàng"),
            ItemName = GetRowValue(row, "Name", "ItemName", "Item Name", "Ten", "Tên", "TenHang", "Ten Hang", "Tên hàng"),
            Lot = GetRowValue(row, "Lot", "LotNo", "Lot No", "Batch"),
            Quantity = GetRowValue(row, "Qty", "Quantity", "SoLuong", "So Luong", "Số lượng"),
            LabelContent = CreateLabelContent(row),
            RowData = row is null ? string.Empty : string.Join("; ", row.Select(pair => $"{pair.Key}={pair.Value}")),
            Notes = notes
        };
    }

    private string CreateLabelContent(IReadOnlyDictionary<string, string>? row)
    {
        return string.Join(" | ", Template.Objects
            .Where(item => item.IsVisible)
            .OrderBy(item => item.ZIndex)
            .Select(item => CreateObjectContent(item, row))
            .Where(value => !string.IsNullOrWhiteSpace(value)));
    }

    private static string CreateObjectContent(LabelObject item, IReadOnlyDictionary<string, string>? row)
    {
        var value = BindingResolver.ResolveObject(item, row);
        return string.IsNullOrWhiteSpace(value) ? string.Empty : $"{item.Name}={value}";
    }

    private static string GetRowValue(IReadOnlyDictionary<string, string>? row, params string[] names)
    {
        if (row is null)
        {
            return string.Empty;
        }

        foreach (var name in names)
        {
            if (row.TryGetValue(name, out var value))
            {
                return value;
            }
        }

        var normalizedNames = names.Select(NormalizeKey).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in row)
        {
            if (normalizedNames.Contains(NormalizeKey(pair.Key)))
            {
                return pair.Value;
            }
        }

        return string.Empty;
    }

    private static string NormalizeKey(string value)
    {
        return new string(value.Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant).ToArray());
    }

    public void OpenPrintHistoryFile()
    {
        try
        {
            if (!File.Exists(_printLogService.LogFilePath))
            {
                StatusText = "Print history file has not been created yet";
                return;
            }

            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = _printLogService.LogFilePath,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            StatusText = $"Cannot open print history: {ex.Message}";
        }
    }
}
