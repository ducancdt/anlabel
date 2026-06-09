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
        return trimmed.StartsWith("FIELD", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("CONCAT", StringComparison.OrdinalIgnoreCase);
    }

    public static FormulaEvaluationResult Evaluate(string expression, IReadOnlyDictionary<string, string> row)
    {
        var nullableRow = row.ToDictionary(pair => pair.Key, pair => (string?)pair.Value, StringComparer.OrdinalIgnoreCase);
        return Engine.Evaluate(expression, nullableRow);
    }
}
