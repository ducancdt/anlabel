using System.IO;

namespace ANLAbel.Data.Excel;

/// <summary>
/// Whether Excel has a data file open. While a workbook is open Excel keeps an
/// owner file "~$name" beside it (removed on close; a crash can leave one
/// behind). ANLAbel reads the saved file, so edits not yet saved in Excel are
/// not what prints; the operator is reminded, nothing is blocked.
/// </summary>
public static class ExcelOwnerFile
{
    public static bool IsOpenInExcel(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return false;
        }

        try
        {
            var directory = Path.GetDirectoryName(filePath);
            var name = Path.GetFileName(filePath);
            if (string.IsNullOrEmpty(directory) || string.IsNullOrEmpty(name))
            {
                return false;
            }

            // Office drops the first two characters of a long name instead.
            return File.Exists(Path.Combine(directory, "~$" + name))
                   || (name.Length > 2 && File.Exists(Path.Combine(directory, "~$" + name[2..])));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            return false;
        }
    }
}
