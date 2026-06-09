namespace ANLAbel.Core.Expressions.Formulas;

public sealed class FormulaEvaluator
{
    public FormulaEvaluationResult Evaluate(FormulaNode root, IReadOnlyDictionary<string, string?> row)
    {
        var context = new EvaluationContext(row);
        var value = EvaluateNode(root, context);
        return new FormulaEvaluationResult(
            value,
            context.Errors.ToArray(),
            context.UsedFields.Distinct(StringComparer.OrdinalIgnoreCase).ToArray());
    }

    private static string EvaluateNode(FormulaNode node, EvaluationContext context)
    {
        return node switch
        {
            FormulaStringLiteralNode literal => literal.Value,
            FormulaFunctionCallNode call => EvaluateFunction(call, context),
            _ => string.Empty
        };
    }

    private static string EvaluateFunction(FormulaFunctionCallNode call, EvaluationContext context)
    {
        return call.Name.ToUpperInvariant() switch
        {
            "FIELD" => EvaluateField(call, context),
            "CONCAT" => EvaluateConcat(call, context),
            _ => AddError(context, $"Unknown function '{call.Name}'.")
        };
    }

    private static string EvaluateField(FormulaFunctionCallNode call, EvaluationContext context)
    {
        if (call.Arguments.Count != 1)
        {
            return AddError(context, "FIELD expects exactly one argument.");
        }

        var fieldName = EvaluateNode(call.Arguments[0], context).Trim();
        if (string.IsNullOrWhiteSpace(fieldName))
        {
            return AddError(context, "FIELD field name is empty.");
        }

        context.UsedFields.Add(fieldName);
        if (FieldNameResolver.TryGetNullableValue(context.Row, fieldName, out var value, out _))
        {
            return value;
        }

        return AddError(context, $"Field '{fieldName}' was not found.");
    }

    private static string EvaluateConcat(FormulaFunctionCallNode call, EvaluationContext context)
    {
        return string.Concat(call.Arguments.Select(argument => EvaluateNode(argument, context)));
    }

    private static string AddError(EvaluationContext context, string error)
    {
        context.Errors.Add(error);
        return string.Empty;
    }

    private sealed class EvaluationContext(IReadOnlyDictionary<string, string?> row)
    {
        public IReadOnlyDictionary<string, string?> Row { get; } = row;
        public List<string> Errors { get; } = new();
        public List<string> UsedFields { get; } = new();
    }
}
