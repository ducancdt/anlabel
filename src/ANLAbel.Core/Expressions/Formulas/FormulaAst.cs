namespace ANLAbel.Core.Expressions.Formulas;

public abstract record FormulaNode;

public sealed record FormulaStringLiteralNode(string Value) : FormulaNode;

public sealed record FormulaFunctionCallNode(string Name, IReadOnlyList<FormulaNode> Arguments) : FormulaNode;

public sealed record FormulaParseResult(FormulaNode? Root, IReadOnlyList<string> Errors)
{
    public bool Success => Root is not null && Errors.Count == 0;
}

public sealed record FormulaEvaluationResult(string Value, IReadOnlyList<string> Errors, IReadOnlyList<string> UsedFields);
