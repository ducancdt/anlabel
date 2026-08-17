using ANLAbel.Core.Expressions;
using ANLAbel.Core.Expressions.Formulas;

namespace ANLAbel.Core.Data;

/// <summary>
/// Compatibility bridge from typed immutable records to the existing binding
/// and Formula AST engines. This keeps transform semantics in one place while
/// data connectors stop exposing mutable DataRow instances.
/// </summary>
public static class DataRecordExpressionEvaluator
{
    private static readonly FormulaEngine FormulaEngine = new();

    public static string EvaluateBinding(string? expression, DataRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        var row = record.Values.ToDictionary(
            pair => pair.Key,
            pair => pair.Value ?? string.Empty,
            StringComparer.OrdinalIgnoreCase);
        return BindingExpressionEvaluator.Evaluate(expression ?? string.Empty, row);
    }

    public static FormulaEvaluationResult EvaluateFormula(string? expression, DataRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        return FormulaEngine.Evaluate(expression, record.Values);
    }
}
