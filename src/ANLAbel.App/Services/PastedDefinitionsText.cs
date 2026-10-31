using ANLAbel.Core.Data;

namespace ANLAbel.App.Services;

/// <summary>
/// The status line after a paste brought functions or prompt variables into the label,
/// or found the label already defining one of their names otherwise.
/// </summary>
public static class PastedDefinitionsText
{
    public static string Describe(DataDefinitionImport.Brought brought)
    {
        ArgumentNullException.ThrowIfNull(brought);
        var parts = new List<string>();
        var added = Named(brought.Functions, brought.Prompts);
        if (added.Length > 0)
        {
            parts.Add($"Đã thêm {added} mà đối tượng dán đọc.");
        }

        var different = Named(brought.DifferentFunctions, brought.DifferentPrompts);
        if (different.Length > 0)
        {
            parts.Add($"Tem này đã có {different} khác tem nguồn; đối tượng dán dùng định nghĩa của tem này.");
        }

        if (brought.ColumnFunctions.Count > 0)
        {
            parts.Add($"Tem này đọc cột {Names(brought.ColumnFunctions)} của tệp dữ liệu nên không thêm hàm cùng tên; đối tượng dán đọc cột đó.");
        }

        return string.Join(" ", parts);
    }

    private static string Named(IReadOnlyList<string> functions, IReadOnlyList<string> prompts)
    {
        var kinds = new List<string>();
        if (functions.Count > 0)
        {
            kinds.Add("hàm " + Names(functions));
        }

        if (prompts.Count > 0)
        {
            kinds.Add("biến nhập khi in " + Names(prompts));
        }

        return string.Join(" và ", kinds);
    }

    private static string Names(IEnumerable<string> names) => string.Join(", ", names.Select(name => $"“{name}”"));
}
