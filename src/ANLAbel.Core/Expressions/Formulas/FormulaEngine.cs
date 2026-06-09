namespace ANLAbel.Core.Expressions.Formulas;

public sealed class FormulaEngine
{
    private readonly FormulaParser _parser = new();
    private readonly FormulaEvaluator _evaluator = new();

    public FormulaParseResult Parse(string? formula)
    {
        return _parser.Parse(formula);
    }

    public FormulaEvaluationResult Evaluate(string? formula, IReadOnlyDictionary<string, string?> row)
    {
        var parseResult = Parse(formula);
        if (parseResult.Root is null)
        {
            return new FormulaEvaluationResult(string.Empty, parseResult.Errors, Array.Empty<string>());
        }

        var evaluationResult = _evaluator.Evaluate(parseResult.Root, row);
        return new FormulaEvaluationResult(
            evaluationResult.Value,
            parseResult.Errors.Concat(evaluationResult.Errors).ToArray(),
            evaluationResult.UsedFields);
    }

    public FormulaEvaluationResult Evaluate(FormulaNode root, IReadOnlyDictionary<string, string?> row)
    {
        return _evaluator.Evaluate(root, row);
    }
}
