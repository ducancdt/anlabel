using ANLAbel.Core.Expressions;
using ANLAbel.Core.Models;
using ANLAbel.Core.Printing;

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

    /// <summary>Functions and prompt variables that go with copied objects.</summary>
    public sealed record Carried(IReadOnlyList<DataTransformDefinition> Transforms, IReadOnlyList<PromptVariableDefinition> Prompts)
    {
        public static Carried None { get; } = new(Array.Empty<DataTransformDefinition>(), Array.Empty<PromptVariableDefinition>());
    }

    /// <summary>
    /// The names a paste added to the label, the names the label already had with another
    /// definition (it keeps its own), and the functions left behind because the label reads
    /// a data column of the same name.
    /// </summary>
    public sealed record Brought(
        IReadOnlyList<string> Functions,
        IReadOnlyList<string> Prompts,
        IReadOnlyList<string> DifferentFunctions,
        IReadOnlyList<string> DifferentPrompts,
        IReadOnlyList<string> ColumnFunctions);

    /// <summary>
    /// The functions and prompt variables of <paramref name="source"/> that
    /// <paramref name="objects"/> read, directly or through another function, in the
    /// source's order: what a paste into another label brings along, as NiceLabel 10's
    /// paste adds the variables its items use. The serial counter stays with each label.
    /// </summary>
    /// <param name="columnNames">The source's data columns: a name that reaches one of them reads the column, not a function.</param>
    public static Carried ReadBy(LabelTemplate source, IEnumerable<LabelObject> objects, IEnumerable<string>? columnNames = null)
    {
        return ReadByNames(source, objects, (columnNames ?? Array.Empty<string>()).ToArray());
    }

    /// <summary>
    /// The function a name read by FIELD or a {Name} placeholder reaches, as printing resolves
    /// it (FieldNameResolver: the exact name first, then the same words or the same letters):
    /// null when it reaches one of <paramref name="columnNames"/> or nothing. A print row holds
    /// the columns before the function outputs, so a column wins a tie.
    /// </summary>
    public static string? FunctionRead(string name, IEnumerable<string> functionNames, IEnumerable<string>? columnNames = null)
    {
        var functions = functionNames.Where(item => !string.IsNullOrWhiteSpace(item)).Select(item => item.Trim()).ToArray();
        return FieldNameResolver.TryResolveFieldName(name, (columnNames ?? Array.Empty<string>()).Concat(functions), out var resolved)
               && functions.Contains(resolved, StringComparer.OrdinalIgnoreCase)
            ? resolved
            : null;
    }

    private static Carried ReadByNames(LabelTemplate source, IEnumerable<LabelObject> objects, IReadOnlyList<string> columnNames)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(objects);
        var functions = new Dictionary<string, DataTransformDefinition>(StringComparer.OrdinalIgnoreCase);
        foreach (var transform in source.DataTransforms.Where(transform => !string.IsNullOrWhiteSpace(transform.Name)))
        {
            functions.TryAdd(transform.Name.Trim(), transform);
        }

        var read = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var reached = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var prompts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var pending = new Queue<string>(objects
            .SelectMany(item => new[] { item.BindingExpression, item.VisibilityCondition })
            .Where(expression => !string.IsNullOrWhiteSpace(expression))
            .SelectMany(expression => JobVariableContract.ExpressionTokens(expression!)));
        while (pending.TryDequeue(out var token))
        {
            if (!read.Add(token))
            {
                continue;
            }

            if (JobVariableContract.IsPromptToken(token))
            {
                prompts.Add(token[1..].Trim());
            }
            else if (FunctionRead(token, functions.Keys, columnNames) is { } name
                     && reached.Add(name)
                     && !string.IsNullOrWhiteSpace(functions[name].Formula))
            {
                foreach (var inner in JobVariableContract.ExpressionTokens(functions[name].Formula))
                {
                    pending.Enqueue(inner);
                }
            }
        }

        return new Carried(
            source.DataTransforms.Where(transform => reached.Contains(transform.Name?.Trim() ?? string.Empty)).ToArray(),
            source.PromptVariables.Where(prompt => prompts.Contains(prompt.Name?.Trim() ?? string.Empty)).Select(prompt => prompt.Clone()).ToArray());
    }

    /// <summary>
    /// Adds the carried definitions <paramref name="target"/> does not have (names compare
    /// without letter case); a name it has keeps the label's own definition, and is listed
    /// when the two differ. A function named like one of <paramref name="columnNames"/>, the
    /// label's data columns, stays behind: the label reads that column, and a function of the
    /// same name would stop all its functions (the Data Manager refuses such a name too).
    /// </summary>
    public static Brought BringAlong(LabelTemplate target, Carried carried, IEnumerable<string>? columnNames = null)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(carried);
        var columns = (columnNames ?? Array.Empty<string>())
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var functions = new List<string>();
        var prompts = new List<string>();
        var differentFunctions = new List<string>();
        var differentPrompts = new List<string>();
        var columnFunctions = new List<string>();
        foreach (var transform in carried.Transforms)
        {
            var existing = target.DataTransforms.FirstOrDefault(item => SameName(item.Name, transform.Name));
            if (existing is null && columns.Contains(transform.Name.Trim()))
            {
                columnFunctions.Add(transform.Name.Trim());
            }
            else if (existing is null)
            {
                target.DataTransforms.Add(transform);
                functions.Add(transform.Name.Trim());
            }
            else if (!string.Equals(existing.Formula?.Trim(), transform.Formula?.Trim(), StringComparison.Ordinal))
            {
                differentFunctions.Add(existing.Name.Trim());
            }
        }

        foreach (var prompt in carried.Prompts)
        {
            var existing = target.PromptVariables.FirstOrDefault(item => SameName(item.Name, prompt.Name));
            if (existing is null)
            {
                target.PromptVariables.Add(prompt.Clone());
                prompts.Add(prompt.Name.Trim());
            }
            else if (!SamePrompt(existing, prompt))
            {
                differentPrompts.Add(existing.Name.Trim());
            }
        }

        return new Brought(functions, prompts, differentFunctions, differentPrompts, columnFunctions);
    }

    private static bool SameName(string? left, string? right)
        => string.Equals(left?.Trim(), right?.Trim(), StringComparison.OrdinalIgnoreCase);

    // The fields Clone copies, apart from the name.
    private static bool SamePrompt(PromptVariableDefinition a, PromptVariableDefinition b)
        => a.Prompt == b.Prompt && a.DefaultValue == b.DefaultValue && a.MaxLength == b.MaxLength && a.MinLength == b.MinLength
           && a.DigitsOnly == b.DigitsOnly && a.Required == b.Required && a.FixedLength == b.FixedLength
           && a.AllowedCharacters == b.AllowedCharacters && a.PickList == b.PickList && a.Prefix == b.Prefix && a.Suffix == b.Suffix
           && a.Padding == b.Padding && a.PadCharacter == b.PadCharacter && a.DateFormat == b.DateFormat && a.DateInputFormat == b.DateInputFormat;
}
