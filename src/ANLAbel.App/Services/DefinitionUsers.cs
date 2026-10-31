using ANLAbel.Core.Data;
using ANLAbel.Core.Models;
using ANLAbel.Core.Printing;

namespace ANLAbel.App.Services;

/// <summary>
/// Who reads a prompt variable or a function of the label: the objects, by their text or
/// visibility condition, and the other functions. NiceLabel 10 will not delete an item other
/// items use ("Cannot delete the item because it is used by other items."); Quản lý dữ liệu
/// names them and what the deletion breaks, and keeps the item unless the operator confirms.
/// </summary>
public static class DefinitionUsers
{
    public sealed record Users(IReadOnlyList<string> Objects, IReadOnlyList<string> Functions)
    {
        public static Users None { get; } = new(Array.Empty<string>(), Array.Empty<string>());

        public bool Any => Objects.Count > 0 || Functions.Count > 0;
    }

    /// <param name="names">The names the item answers to: a prompt as <c>?Name</c>, a function by its name.</param>
    /// <param name="columns">The label's data columns: a name that reaches a column first is no use of a function.</param>
    public static Users Of(
        IEnumerable<string> names,
        IEnumerable<LabelObject> objects,
        IEnumerable<(string Name, string Formula)> functions,
        IEnumerable<string>? columns = null)
    {
        ArgumentNullException.ThrowIfNull(names);
        ArgumentNullException.ThrowIfNull(objects);
        ArgumentNullException.ThrowIfNull(functions);
        var read = names.Where(name => !string.IsNullOrWhiteSpace(name)).Select(name => name.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var others = functions.ToArray();
        // A function is reached as printing resolves names, loosely (FIELD("Mã_QR") reads “Mã QR”);
        // a prompt only by its own name.
        var functionNames = others.Select(function => function.Name ?? string.Empty)
            .Concat(read.Where(name => !JobVariableContract.IsPromptToken(name)))
            .ToArray();
        var columnNames = (columns ?? Array.Empty<string>()).ToArray();
        bool Reaches(string name) => read.Contains(name)
            || (!JobVariableContract.IsPromptToken(name)
                && DataDefinitionImport.FunctionRead(name, functionNames, columnNames) is { } function
                && read.Contains(function));
        bool Reads(string? expression) => JobVariableContract.NamesRead(expression).Any(Reaches);
        return new Users(
            objects
                .Where(item => Reads(item.BindingExpression) || Reads(item.VisibilityCondition))
                .Select(item => string.IsNullOrWhiteSpace(item.Name) ? "(không tên)" : item.Name.Trim())
                .Distinct(StringComparer.Ordinal)
                .ToArray(),
            others
                .Where(function => !read.Contains(function.Name?.Trim() ?? string.Empty) && Reads(function.Formula))
                .Select(function => function.Name.Trim())
                .Distinct(StringComparer.Ordinal)
                .ToArray());
    }

    public static IReadOnlyList<string> FunctionLines(string name, Users users) =>
    [
        $"Hàm “{name}” đang được dùng ở {Places(users)}.",
        "Xóa thì các chỗ đó mất dữ liệu của hàm: kiểm tra liên kết báo thiếu trường cho tới khi đổi sang dữ liệu khác."
    ];

    public static IReadOnlyList<string> PromptLines(string name, Users users) =>
    [
        $"Biến nhập khi in “{name}” đang được dùng ở {Places(users)}.",
        $"Xóa thì khi in vẫn hỏi “{name}” như một ô chữ bắt buộc, nhưng mất quy tắc nhập của nó (độ dài, định dạng, danh sách chọn, giá trị mặc định)."
    ];

    /// <summary>Where an item is used: "đối tượng “A”, “B”; hàm “F”".</summary>
    public static string Places(Users users)
    {
        var parts = new List<string>();
        if (users.Objects.Count > 0)
        {
            parts.Add("đối tượng " + Quoted(users.Objects));
        }

        if (users.Functions.Count > 0)
        {
            parts.Add("hàm " + Quoted(users.Functions));
        }

        return string.Join("; ", parts);
    }

    private static string Quoted(IEnumerable<string> names) => string.Join(", ", names.Select(name => $"“{name}”"));
}
