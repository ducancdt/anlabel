namespace ANLAbel.Core.Expressions.Formulas;

public sealed class FormulaEvaluator
{
    /// <summary>
    /// The functions a formula can call. An expression is a formula when it
    /// starts with one of them (<see cref="FormulaBindingEvaluator.LooksLikeFormula"/>).
    /// </summary>
    public static IReadOnlyList<string> FunctionNames { get; } =
    [
        "FIELD", "CONCAT", "MID", "GS1CHECKDIGIT", "UPPER", "LOWER", "TRIM", "LEFT", "RIGHT", "EQUALS", "NOT", "AND", "DATEADD", "DATEFORMAT"
    ];

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
            "UPPER" => EvaluateText(call, context, text => text.ToUpperInvariant()),
            "LOWER" => EvaluateText(call, context, text => text.ToLowerInvariant()),
            "TRIM" => EvaluateText(call, context, text => text.Trim()),
            "LEFT" => EvaluateEnd(call, context, fromLeft: true),
            "RIGHT" => EvaluateEnd(call, context, fromLeft: false),
            "EQUALS" => EvaluateEquals(call, context),
            "NOT" => EvaluateText(call, context, text => FormulaTruth.From(!FormulaTruth.IsTrue(text))),
            "AND" => EvaluateAnd(call, context),
            "DATEADD" => EvaluateDateAdd(call, context),
            "DATEFORMAT" => EvaluateDateFormat(call, context),
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

    /// <summary>UPPER(text), LOWER(text), TRIM(text): the text in capitals, in small letters, without outer spaces.</summary>
    private static string EvaluateText(FormulaFunctionCallNode call, EvaluationContext context, Func<string, string> change)
    {
        if (call.Arguments.Count != 1)
        {
            return AddError(context, $"{call.Name.ToUpperInvariant()} expects exactly one argument.");
        }

        return change(EvaluateNode(call.Arguments[0], context));
    }

    /// <summary>
    /// EQUALS(a, b): "true" when the two values are the same ignoring case (as
    /// NiceLabel 10 compares visibility conditions), otherwise "false".
    /// </summary>
    private static string EvaluateEquals(FormulaFunctionCallNode call, EvaluationContext context)
    {
        if (call.Arguments.Count != 2)
        {
            return AddError(context, "EQUALS expects two arguments.");
        }

        var left = EvaluateNode(call.Arguments[0], context);
        var right = EvaluateNode(call.Arguments[1], context);
        return FormulaTruth.From(string.Equals(left, right, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>How a date given to DATEADD or DATEFORMAT is read when no read format is named: Excel date cells (yyyy-MM-dd), then the common written forms.</summary>
    private static readonly string[] DateReadFormats = ["yyyy-MM-dd", "yyyy-MM-dd HH:mm:ss", "dd/MM/yyyy", "d/M/yyyy", "dd.MM.yyyy", "yyyyMMdd"];

    /// <summary>
    /// DATEFORMAT(date, format[, readFormat]): the date written in format, e.g.
    /// an Excel date as yyMMdd for a GS1 (17). An empty date gives an empty value.
    /// </summary>
    private static string EvaluateDateFormat(FormulaFunctionCallNode call, EvaluationContext context)
    {
        if (call.Arguments.Count is < 2 or > 3)
        {
            return AddError(context, "DATEFORMAT expects a date, a format and an optional read format.");
        }

        var text = EvaluateNode(call.Arguments[0], context);
        var format = EvaluateNode(call.Arguments[1], context);
        var readFormat = call.Arguments.Count == 3 ? EvaluateNode(call.Arguments[2], context) : null;
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        return TryReadDate(text, readFormat, context, out var date) ? WriteDate(date, format, context) : string.Empty;
    }

    /// <summary>
    /// DATEADD(date, amount, unit[, format[, readFormat]]): the date moved by amount
    /// days (d), weeks (w), months (mo) or years (y), written in format (dd/MM/yyyy
    /// when none), e.g. an expiry from a production date. An empty date gives an
    /// empty value.
    /// </summary>
    private static string EvaluateDateAdd(FormulaFunctionCallNode call, EvaluationContext context)
    {
        if (call.Arguments.Count is < 3 or > 5)
        {
            return AddError(context, "DATEADD expects a date, an amount, a unit (d, w, mo, y) and an optional format and read format.");
        }

        var text = EvaluateNode(call.Arguments[0], context);
        var amountText = EvaluateNode(call.Arguments[1], context).Trim();
        var unit = EvaluateNode(call.Arguments[2], context).Trim().ToLowerInvariant();
        var format = call.Arguments.Count >= 4 ? EvaluateNode(call.Arguments[3], context) : "dd/MM/yyyy";
        var readFormat = call.Arguments.Count == 5 ? EvaluateNode(call.Arguments[4], context) : null;
        if (!int.TryParse(amountText, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var amount))
        {
            return AddError(context, "DATEADD amount must be a whole number.");
        }

        if (unit is not ("d" or "w" or "mo" or "y"))
        {
            return AddError(context, "DATEADD unit must be d, w, mo or y.");
        }

        if (string.IsNullOrWhiteSpace(text) || !TryReadDate(text, readFormat, context, out var date))
        {
            return string.Empty;
        }

        DateTime moved;
        try
        {
            moved = unit switch
            {
                "d" => date.AddDays(amount),
                "w" => date.AddDays(7.0 * amount),
                "mo" => date.AddMonths(amount),
                _ => date.AddYears(amount)
            };
        }
        catch (ArgumentOutOfRangeException)
        {
            return AddError(context, "DATEADD moves the date out of range.");
        }

        return WriteDate(moved, format, context);
    }

    private static bool TryReadDate(string text, string? readFormat, EvaluationContext context, out DateTime date)
    {
        var trimmed = text.Trim();
        string[] formats = string.IsNullOrEmpty(readFormat) ? DateReadFormats : [readFormat];
        bool read;
        try
        {
            read = DateTime.TryParseExact(trimmed, formats, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out date);
        }
        catch (FormatException)
        {
            date = default;
            read = false;
        }

        if (!read)
        {
            AddError(context, $"Date '{trimmed}' is not understood; give it as yyyy-MM-dd or dd/MM/yyyy, or name its format.");
        }

        return read;
    }

    private static string WriteDate(DateTime date, string format, EvaluationContext context)
    {
        try
        {
            return date.ToString(string.IsNullOrEmpty(format) ? "dd/MM/yyyy" : format, System.Globalization.CultureInfo.InvariantCulture);
        }
        catch (FormatException)
        {
            return AddError(context, $"Date format '{format}' is not valid.");
        }
    }

    /// <summary>AND(a, b, …): "true" when every value is true (FormulaTruth); every argument is read.</summary>
    private static string EvaluateAnd(FormulaFunctionCallNode call, EvaluationContext context)
    {
        if (call.Arguments.Count == 0)
        {
            return AddError(context, "AND expects at least one argument.");
        }

        var values = call.Arguments.Select(argument => EvaluateNode(argument, context)).ToList();
        return FormulaTruth.From(values.All(FormulaTruth.IsTrue));
    }

    /// <summary>LEFT(text, count) and RIGHT(text, count): at most count characters from that end of the text.</summary>
    private static string EvaluateEnd(FormulaFunctionCallNode call, EvaluationContext context, bool fromLeft)
    {
        var name = fromLeft ? "LEFT" : "RIGHT";
        if (call.Arguments.Count != 2)
        {
            return AddError(context, $"{name} expects two arguments: text, count.");
        }

        var text = EvaluateNode(call.Arguments[0], context);
        if (!int.TryParse(EvaluateNode(call.Arguments[1], context).Trim(), System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var count) || count < 0)
        {
            return AddError(context, $"{name} count must be a whole number of 0 or more.");
        }

        count = Math.Min(count, text.Length);
        return fromLeft ? text[..count] : text[^count..];
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
