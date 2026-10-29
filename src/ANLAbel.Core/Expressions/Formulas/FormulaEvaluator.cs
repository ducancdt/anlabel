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
            "MID" => EvaluateMid(call, context),
            "GS1CHECKDIGIT" => EvaluateGs1CheckDigit(call, context),
            _ => AddError(context, $"Unknown function '{call.Name}'.")
        };
    }

    /// <summary>
    /// MID(text, start, length): the substring starting at the 1-based
    /// character position <c>start</c> with at most <c>length</c> characters;
    /// a start past the end yields an empty string.
    /// </summary>
    private static string EvaluateMid(FormulaFunctionCallNode call, EvaluationContext context)
    {
        if (call.Arguments.Count != 3)
        {
            return AddError(context, "MID expects exactly three arguments: text, start, length.");
        }

        var text = EvaluateNode(call.Arguments[0], context);
        if (!int.TryParse(EvaluateNode(call.Arguments[1], context).Trim(), System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var start) || start < 1)
        {
            return AddError(context, "MID start must be a whole number of 1 or more.");
        }

        if (!int.TryParse(EvaluateNode(call.Arguments[2], context).Trim(), System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var length) || length < 0)
        {
            return AddError(context, "MID length must be a whole number of 0 or more.");
        }

        if (start > text.Length)
        {
            return string.Empty;
        }

        return text.Substring(start - 1, Math.Min(length, text.Length - start + 1));
    }

    /// <summary>
    /// GS1CHECKDIGIT(digits): the digits followed by their GS1 modulo-10 check
    /// digit (weights 3 and 1 from the rightmost data digit), as used by
    /// GTIN, SSCC and GS1 AIs 00/01/02. Non-digit input is an error so a
    /// wrong key never prints a plausible-looking code.
    /// </summary>
    private static string EvaluateGs1CheckDigit(FormulaFunctionCallNode call, EvaluationContext context)
    {
        if (call.Arguments.Count != 1)
        {
            return AddError(context, "GS1CHECKDIGIT expects exactly one argument.");
        }

        var digits = EvaluateNode(call.Arguments[0], context).Trim();
        if (digits.Length == 0 || !digits.All(char.IsAsciiDigit))
        {
            return AddError(context, "GS1CHECKDIGIT needs digits only.");
        }

        var sum = 0;
        var multiplier = 3;
        for (var index = digits.Length - 1; index >= 0; index--)
        {
            sum += (digits[index] - '0') * multiplier;
            multiplier = multiplier == 3 ? 1 : 3;
        }

        return digits + (char)('0' + (10 - (sum % 10)) % 10);
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
