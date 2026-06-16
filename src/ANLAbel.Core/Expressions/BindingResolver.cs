using ANLAbel.Core.Expressions.Formulas;
using ANLAbel.Core.Models;

namespace ANLAbel.Core.Expressions;

/// <summary>
/// Single source of truth for resolving a <see cref="LabelObject"/> binding expression
/// against an Excel data row.  All callers — designer preview, print renderer, preflight
/// validator, and print preview — must use these methods so the resolved value is always
/// identical regardless of context.
///
/// Rule: the expression string is returned unchanged when <paramref name="row"/> is null,
/// so callers that have no data row still show the raw expression as a placeholder.
/// </summary>
public static class BindingResolver
{
    /// <summary>
    /// Resolves a raw binding expression string against a data row.
    /// Returns <paramref name="expression"/> unchanged when <paramref name="row"/> is null.
    /// </summary>
    public static string Resolve(string expression, IReadOnlyDictionary<string, string>? row)
    {
        if (row is null)
            return expression;

        return FormulaBindingEvaluator.LooksLikeFormula(expression)
            ? FormulaBindingEvaluator.Evaluate(expression, row).Value
            : BindingExpressionEvaluator.Evaluate(expression, row);
    }

    /// <summary>
    /// Resolves the display value of a <see cref="LabelObject"/> against a data row.
    /// Returns <see cref="LabelObject.Text"/> when no binding expression is set,
    /// or <see cref="LabelObject.BindingExpression"/> unchanged when <paramref name="row"/> is null.
    /// </summary>
    public static string ResolveObject(LabelObject item, IReadOnlyDictionary<string, string>? row)
    {
        if (string.IsNullOrWhiteSpace(item.BindingExpression))
            return item.Text;

        return Resolve(item.BindingExpression, row);
    }
}
