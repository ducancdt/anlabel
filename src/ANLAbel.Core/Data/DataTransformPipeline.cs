using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using ANLAbel.Core.Expressions.Formulas;
using ANLAbel.Core.Printing;

namespace ANLAbel.Core.Data;

/// <summary>A named derived field evaluated from source and earlier derived fields.</summary>
public sealed record DataTransformDefinition(string Name, string Formula)
{
    /// <summary>
    /// Members written by a newer ANLAbel that this version does not know. They
    /// are kept on a template load/save round trip instead of being dropped
    /// (audit 2026-10-13). Null when there are none.
    /// </summary>
    [System.Text.Json.Serialization.JsonExtensionData]
    public Dictionary<string, System.Text.Json.JsonElement>? ExtensionData { get; init; }
}

/// <summary>Auditable lineage for one derived field without retaining external connector state.</summary>
public sealed record DataTransformLineage(string OutputField, ImmutableArray<string> InputFields);

public sealed record DataTransformResult(
    DataRecord Record,
    ImmutableArray<DataTransformLineage> Lineage,
    ImmutableArray<string> Errors)
{
    public bool IsValid => Errors.IsEmpty;
}

/// <summary>
/// Executes Formula-AST transforms over immutable records. Definitions are
/// topologically ordered, so a transform can reference another transform even
/// when it appears later in a workspace list. Cycles and invalid formulas fail
/// closed with diagnostics instead of producing an order-dependent print value.
/// </summary>
public static partial class DataTransformPipeline
{
    private static readonly FormulaEngine FormulaEngine = new();

    /// <summary>
    /// The expression with renamed functions: <c>{Old}</c> and
    /// <c>FIELD("Old")</c> become <c>{New}</c> and <c>FIELD("New")</c>, every
    /// name in one pass; prompts (<c>{?...}</c>), dates (<c>{#...}</c>) and other
    /// fields stay as written. <paramref name="renames"/> maps old to new names
    /// and should ignore case, as fields are looked up.
    /// </summary>
    public static string RenameReferences(string? expression, IReadOnlyDictionary<string, string> renames)
    {
        ArgumentNullException.ThrowIfNull(renames);
        if (string.IsNullOrEmpty(expression) || renames.Count == 0)
        {
            return expression ?? string.Empty;
        }

        string Rename(Match match) => renames.TryGetValue(match.Groups["name"].Value.Trim(), out var renamed)
            ? match.Groups["open"].Value + renamed + match.Groups["close"].Value
            : match.Value;
        return FieldCallPattern().Replace(PlaceholderPattern().Replace(expression, Rename), Rename);
    }

    /// <summary>
    /// <paramref name="renames"/> with every name in <paramref name="expressions"/> that
    /// reaches a renamed function as printing finds it, loosely (FIELD("Mã_QR") reads “Mã QR”,
    /// see <see cref="DataDefinitionImport.FunctionRead"/>): such a reference moves with the
    /// function, or it would read nothing once the function has its new name. A name that
    /// reaches one of <paramref name="columnNames"/> or another function stays.
    /// </summary>
    public static IReadOnlyDictionary<string, string> RenamesReaching(
        IReadOnlyDictionary<string, string> renames,
        IEnumerable<string?> expressions,
        IEnumerable<string> functionNames,
        IEnumerable<string>? columnNames = null)
    {
        ArgumentNullException.ThrowIfNull(renames);
        ArgumentNullException.ThrowIfNull(expressions);
        ArgumentNullException.ThrowIfNull(functionNames);
        var widened = new Dictionary<string, string>(renames, StringComparer.OrdinalIgnoreCase);
        var functions = functionNames.ToArray();
        var columns = (columnNames ?? Array.Empty<string>()).ToArray();
        foreach (var name in expressions.SelectMany(expression => JobVariableContract.NamesRead(expression)))
        {
            if (!widened.ContainsKey(name)
                && !JobVariableContract.IsPromptToken(name)
                && !JobVariableContract.IsDateToken(name)
                && !JobVariableContract.IsSerialToken(name)
                && DataDefinitionImport.FunctionRead(name, functions, columns) is { } function
                && renames.TryGetValue(function, out var renamed))
            {
                widened[name] = renamed;
            }
        }

        return widened;
    }

    [GeneratedRegex(@"(?<open>\{\s*)(?<name>[^{}?#\s][^{}]*?)(?<close>\s*\})")]
    private static partial Regex PlaceholderPattern();

    [GeneratedRegex(@"(?<open>FIELD\s*\(\s*"")(?<name>[^""?#][^""]*)(?<close>""\s*\))", RegexOptions.IgnoreCase)]
    private static partial Regex FieldCallPattern();

    public static string ComputeFingerprint(IEnumerable<DataTransformDefinition>? definitions)
    {
        var builder = new StringBuilder();
        foreach (var definition in definitions ?? Array.Empty<DataTransformDefinition>())
        {
            builder.Append(definition.Name.Length).Append(':').Append(definition.Name)
                .Append('|').Append(definition.Formula.Length).Append(':').Append(definition.Formula).Append('\n');
        }

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString())));
    }

    public static DataTransformResult Evaluate(
        DataRecord source,
        IEnumerable<DataTransformDefinition> definitions)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(definitions);

        var transforms = definitions.ToArray();
        var errors = ImmutableArray.CreateBuilder<string>();
        var names = new Dictionary<string, DataTransformDefinition>(StringComparer.OrdinalIgnoreCase);
        foreach (var transform in transforms)
        {
            if (string.IsNullOrWhiteSpace(transform.Name))
            {
                errors.Add("A transform output field name is required.");
                continue;
            }

            if (source.Values.ContainsKey(transform.Name))
            {
                errors.Add($"Transform output '{transform.Name}' conflicts with a source field.");
                continue;
            }

            if (!names.TryAdd(transform.Name, transform))
            {
                errors.Add($"Transform output '{transform.Name}' is defined more than once.");
            }
        }

        if (errors.Count > 0)
        {
            return new DataTransformResult(source, ImmutableArray<DataTransformLineage>.Empty, errors.ToImmutable());
        }

        var inputs = new Dictionary<string, ImmutableArray<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var transform in transforms)
        {
            var parseResult = FormulaEngine.Parse(transform.Formula);
            if (parseResult.Root is null)
            {
                errors.AddRange(parseResult.Errors.Select(error => $"Transform '{transform.Name}': {error}"));
                continue;
            }

            inputs[transform.Name] = CollectFieldReferences(parseResult.Root)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToImmutableArray();
        }

        if (errors.Count > 0)
        {
            return new DataTransformResult(source, ImmutableArray<DataTransformLineage>.Empty, errors.ToImmutable());
        }

        var ordered = OrderTransforms(transforms, names, inputs, errors);
        if (errors.Count > 0)
        {
            return new DataTransformResult(source, ImmutableArray<DataTransformLineage>.Empty, errors.ToImmutable());
        }

        var values = source.Values.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);
        var lineage = ImmutableArray.CreateBuilder<DataTransformLineage>();
        foreach (var transform in ordered)
        {
            var evaluation = FormulaEngine.Evaluate(transform.Formula, values);
            if (evaluation.Errors.Count > 0)
            {
                errors.AddRange(evaluation.Errors.Select(error => $"Transform '{transform.Name}': {error}"));
                continue;
            }

            values[transform.Name] = evaluation.Value;
            lineage.Add(new DataTransformLineage(transform.Name, inputs[transform.Name]));
        }

        // A transform set is an all-or-nothing data contract. Returning values
        // derived before a later failure would make a caller able to render or
        // prepare a label from a partial configuration. Preserve the immutable
        // source record and publish no lineage unless every definition succeeds.
        if (errors.Count > 0)
        {
            return new DataTransformResult(
                source,
                ImmutableArray<DataTransformLineage>.Empty,
                errors.ToImmutable());
        }

        return new DataTransformResult(
            DataRecord.Create(values),
            lineage.ToImmutable(),
            errors.ToImmutable());
    }

    private static IReadOnlyList<DataTransformDefinition> OrderTransforms(
        IReadOnlyList<DataTransformDefinition> transforms,
        IReadOnlyDictionary<string, DataTransformDefinition> names,
        IReadOnlyDictionary<string, ImmutableArray<string>> inputs,
        ImmutableArray<string>.Builder errors)
    {
        var unresolved = transforms.ToList();
        var resolved = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var ordered = new List<DataTransformDefinition>(transforms.Count);

        while (unresolved.Count > 0)
        {
            var next = unresolved.FirstOrDefault(transform => inputs[transform.Name]
                .Where(names.ContainsKey)
                .All(resolved.Contains));
            if (next is null)
            {
                errors.Add($"Transform dependency cycle detected: {string.Join(", ", unresolved.Select(transform => transform.Name))}.");
                break;
            }

            unresolved.Remove(next);
            resolved.Add(next.Name);
            ordered.Add(next);
        }

        return ordered;
    }

    private static IEnumerable<string> CollectFieldReferences(FormulaNode node)
    {
        if (node is not FormulaFunctionCallNode call)
        {
            yield break;
        }

        if (string.Equals(call.Name, "FIELD", StringComparison.OrdinalIgnoreCase)
            && call.Arguments.Count == 1
            && call.Arguments[0] is FormulaStringLiteralNode literal)
        {
            yield return literal.Value.Trim();
        }

        foreach (var argument in call.Arguments)
        {
            foreach (var reference in CollectFieldReferences(argument))
            {
                yield return reference;
            }
        }
    }
}
