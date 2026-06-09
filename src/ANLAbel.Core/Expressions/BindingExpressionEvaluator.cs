using System.Text.RegularExpressions;

namespace ANLAbel.Core.Expressions;

public static partial class BindingExpressionEvaluator
{
    public static string Evaluate(string expression, IReadOnlyDictionary<string, string> row)
    {
        if (string.IsNullOrEmpty(expression))
        {
            return string.Empty;
        }

        return FieldPattern().Replace(expression, match =>
        {
            var field = match.Groups[1].Value;
            return FieldNameResolver.TryGetValue(row, field, out var value, out _)
                ? value
                : string.Empty;
        });
    }

    public static IReadOnlyList<string> GetFields(string expression)
    {
        return FieldPattern()
            .Matches(expression ?? string.Empty)
            .Select(match => match.Groups[1].Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    [GeneratedRegex(@"\{([^{}]+)\}")]
    private static partial Regex FieldPattern();
}
