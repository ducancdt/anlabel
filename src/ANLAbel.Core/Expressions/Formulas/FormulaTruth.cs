namespace ANLAbel.Core.Expressions.Formulas;

/// <summary>
/// When a value counts as true: not empty, 0, false, no or không. Visibility
/// conditions and the NOT/AND formula functions share this rule.
/// </summary>
public static class FormulaTruth
{
    private static readonly string[] FalseValues = ["0", "false", "no", "không", "khong"];

    public static bool IsTrue(string? value)
    {
        var trimmed = (value ?? string.Empty).Trim();
        return trimmed.Length > 0 && !FalseValues.Contains(trimmed, StringComparer.OrdinalIgnoreCase);
    }

    public static string From(bool value) => value ? "true" : "false";
}
