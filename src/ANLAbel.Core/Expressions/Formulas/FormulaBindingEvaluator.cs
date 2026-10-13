namespace ANLAbel.Core.Expressions.Formulas;

public static class FormulaBindingEvaluator
{
    private static readonly FormulaEngine Engine = new();

    public static bool LooksLikeFormula(string? expression)
    {
        if (string.IsNullOrWhiteSpace(expression))
        {
            return false;
        }

        var trimmed = expression.TrimStart();
        return StartsWithFunctionCall(trimmed, "FIELD")
            || StartsWithFunctionCall(trimmed, "CONCAT");
    }

    // A value is only a formula when the function name is immediately followed by
    // '(' (whitespace allowed). This prevents plain static text such as
    // "FIELD DAY 2026" or "CONCATENATED" from being routed into the formula
    // engine (where it would fail to parse and render empty).
    private static bool StartsWithFunctionCall(string text, string functionName)
    {
        if (!text.StartsWith(functionName, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var index = functionName.Length;
        while (index < text.Length && char.IsWhiteSpace(text[index]))
        {
            index++;
        }

        return index < text.Length && text[index] == '(';
    }

    public static FormulaEvaluationResult Evaluate(string expression, IReadOnlyDictionary<string, string> row)
    {
        var nullableRow = row.ToDictionary(pair => pair.Key, pair => (string?)pair.Value, StringComparer.OrdinalIgnoreCase);
        return Engine.Evaluate(expression, nullableRow);
    }
}
