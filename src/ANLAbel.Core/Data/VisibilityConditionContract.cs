using ANLAbel.Core.Expressions;
using ANLAbel.Core.Expressions.Formulas;

namespace ANLAbel.Core.Data;

/// <summary>
/// NiceLabel 10 Visibility Settings: an object prints only when its condition
/// (a field such as <c>{Lot}</c> or a formula) resolves to a value that is not
/// empty, 0, false, no or không for the data row. Without a data row (design,
/// test print) the object is shown.
/// </summary>
public static class VisibilityConditionContract
{
    private static readonly string[] HiddenValues = ["0", "false", "no", "không", "khong"];

    public static bool IsShown(string? condition, IReadOnlyDictionary<string, string>? row)
    {
        if (string.IsNullOrWhiteSpace(condition) || row is null)
        {
            return true;
        }

        var value = FormulaBindingEvaluator.LooksLikeFormula(condition)
            ? FormulaBindingEvaluator.Evaluate(condition, row).Value
            : BindingExpressionEvaluator.Evaluate(condition, row);
        var trimmed = (value ?? string.Empty).Trim();
        return trimmed.Length > 0 && !HiddenValues.Contains(trimmed, StringComparer.OrdinalIgnoreCase);
    }
}
