using ANLAbel.Core.Models;

namespace ANLAbel.Core.Data;

/// <summary>
/// Nhập nguồn dữ liệu (NiceLabel 10 Import Data Sources): another label's prompt
/// variables and functions come into this one. A name already here is either replaced
/// by the imported definition (overwrite) or kept as it is (merge); new names go after
/// the existing ones. Names compare without letter case, as bindings do. The serial
/// counter is never imported: each label keeps its own number sequence.
/// </summary>
public static class DataDefinitionImport
{
    public sealed record Result<T>(IReadOnlyList<T> Items, int Added, int Replaced, int Kept);

    public static Result<PromptVariableDefinition> MergePrompts(
        IReadOnlyList<PromptVariableDefinition> current,
        IReadOnlyList<PromptVariableDefinition> imported,
        bool overwrite) =>
        Merge(current, imported.Select(prompt => prompt.Clone()).ToArray(), prompt => prompt.Name, overwrite);

    public static Result<DataTransformDefinition> MergeTransforms(
        IReadOnlyList<DataTransformDefinition> current,
        IReadOnlyList<DataTransformDefinition> imported,
        bool overwrite) =>
        Merge(current, imported, transform => transform.Name, overwrite);

    private static Result<T> Merge<T>(IReadOnlyList<T> current, IReadOnlyList<T> imported, Func<T, string> name, bool overwrite)
    {
        ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(imported);
        var items = current.ToList();
        int added = 0, replaced = 0, kept = 0;
        foreach (var item in imported)
        {
            var key = name(item);
            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            var index = items.FindIndex(existing => string.Equals(name(existing), key, StringComparison.OrdinalIgnoreCase));
            if (index < 0)
            {
                items.Add(item);
                added++;
            }
            else if (overwrite)
            {
                items[index] = item;
                replaced++;
            }
            else
            {
                kept++;
            }
        }

        return new Result<T>(items, added, replaced, kept);
    }
}
