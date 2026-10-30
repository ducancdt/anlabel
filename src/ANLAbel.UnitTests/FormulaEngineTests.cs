using ANLAbel.Core.Expressions;
using ANLAbel.Core.Expressions.Formulas;
using Xunit;

namespace ANLAbel.UnitTests;

public sealed class FormulaEngineTests
{
    private static readonly FormulaEngine Engine = new();

    private static string Evaluate(string formula, IReadOnlyDictionary<string, string?> row)
    {
        return Engine.Evaluate(formula, row).Value;
    }

    private static Dictionary<string, string?> Row(params (string Key, string? Value)[] pairs)
    {
        var row = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in pairs)
        {
            row[key] = value;
        }

        return row;
    }

    // DEFECT 1 — unknown escape sequences preserve the backslash.
    [Fact]
    public void StringLiteralPreservesBackslashForUnknownEscape()
    {
        var result = Engine.Evaluate("CONCAT(\"C:\\data\")", Row());
        Assert.Empty(result.Errors);
        Assert.Equal("C:\\data", result.Value);
    }

    [Theory]
    [InlineData("CONCAT(\"a\\\"b\")", "a\"b")] // escaped quote
    [InlineData("CONCAT(\"a\\\\b\")", "a\\b")] // escaped backslash
    [InlineData("CONCAT(\"a\\nb\")", "a\nb")] // newline
    [InlineData("CONCAT(\"a\\tb\")", "a\tb")] // tab
    public void StringLiteralKeepsKnownEscapesWorking(string formula, string expected)
    {
        var result = Engine.Evaluate(formula, Row());
        Assert.Empty(result.Errors);
        Assert.Equal(expected, result.Value);
    }

    // DEFECT 2 — plain text merely starting with FIELD/CONCAT is not a formula.
    [Theory]
    [InlineData("FIELD DAY 2026")]
    [InlineData("CONCATENATED")]
    [InlineData("FIELD")]
    [InlineData("CONCAT report")]
    public void PlainTextStartingWithKeywordIsNotAFormula(string text)
    {
        Assert.False(FormulaBindingEvaluator.LooksLikeFormula(text));
    }

    [Theory]
    [InlineData("FIELD(\"x\")")]
    [InlineData("FIELD (\"x\")")]
    [InlineData("CONCAT(\"a\", \"b\")")]
    [InlineData("concat (\"a\")")]
    public void RealFormulasAreStillDetected(string formula)
    {
        Assert.True(FormulaBindingEvaluator.LooksLikeFormula(formula));
    }

    // DEFECT 3 — FIELD() and the {field} brace path yield identical values,
    // including surrounding whitespace (no trimming on either path).
    [Fact]
    public void FieldAndBracePathReturnSameValueForFieldWithSurroundingSpaces()
    {
        var row = Row(("Name", "  spaced  "));
        var nullableRow = row;
        var stringRow = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Name"] = "  spaced  "
        };

        var formulaValue = Evaluate("FIELD(\"Name\")", nullableRow);
        var braceValue = BindingExpressionEvaluator.Evaluate("{Name}", stringRow);

        Assert.Equal("  spaced  ", braceValue);
        Assert.Equal(braceValue, formulaValue);
    }

    // Baseline coverage.
    [Fact]
    public void ConcatCombinesMultipleArguments()
    {
        var row = Row(("A", "1"), ("B", "2"));
        var result = Evaluate("CONCAT(FIELD(\"A\"), \"-\", FIELD(\"B\"))", row);
        Assert.Equal("1-2", result);
    }

    [Fact]
    public void FieldReturnsValueForExistingField()
    {
        var row = Row(("Lot", "L42"));
        Assert.Equal("L42", Evaluate("FIELD(\"Lot\")", row));
    }

    [Fact]
    public void FieldReturnsEmptyAndErrorsForMissingField()
    {
        var result = Engine.Evaluate("FIELD(\"Nope\")", Row(("Lot", "L42")));
        Assert.Equal(string.Empty, result.Value);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public void MalformedFormulaFailsClearWithEmptyValue()
    {
        var result = Engine.Evaluate("CONCAT(\"unterminated", Row());
        Assert.Equal(string.Empty, result.Value);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public void NestedConcatEvaluates()
    {
        var row = Row(("A", "x"), ("B", "y"));
        var result = Evaluate("CONCAT(CONCAT(FIELD(\"A\"), \"|\"), FIELD(\"B\"))", row);
        Assert.Equal("x|y", result);
    }

    // The designer, preflight and print threads share the static engines. A
    // shared parse position once let a printed page read an empty value after
    // preflight had passed, so each parse must see only its own formula.
    [Fact]
    public void SharedEngineParsesEachFormulaOnItsOwnUnderConcurrentUse()
    {
        var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Part"] = "P-100",
            ["Lot"] = "L42"
        };
        var cases = new[]
        {
            ("FIELD(\"Part\")", "P-100"),
            ("CONCAT(FIELD(\"Part\"), \"/\", FIELD(\"Lot\"), \"/\", \"a longer literal widens the window\")",
                "P-100/L42/a longer literal widens the window"),
            ("CONCAT(\"Lot \", FIELD(\"Lot\"))", "Lot L42")
        };
        var wrong = 0;

        Parallel.For(0, 30_000, new ParallelOptions { MaxDegreeOfParallelism = 8 }, index =>
        {
            var (formula, expected) = cases[index % cases.Length];
            var result = FormulaBindingEvaluator.Evaluate(formula, row);
            if (result.Value != expected || result.Errors.Count != 0)
            {
                Interlocked.Increment(ref wrong);
            }
        });

        Assert.Equal(0, wrong);
    }
}

public sealed class FormulaTextFunctionTests
{
    private static readonly FormulaEngine Engine = new();

    private static FormulaEvaluationResult Run(string formula, params (string Key, string? Value)[] pairs)
    {
        var row = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in pairs)
        {
            row[key] = value;
        }

        return Engine.Evaluate(formula, row);
    }

    [Theory]
    [InlineData("MID(\"(00)123456789012345678\", \"5\", \"18\")", "123456789012345678")]
    [InlineData("MID(\"ABCDEF\", \"3\", \"2\")", "CD")]
    [InlineData("MID(\"ABCDEF\", \"5\", \"10\")", "EF")]
    [InlineData("MID(\"ABCDEF\", \"9\", \"2\")", "")]
    [InlineData("MID(\"ABCDEF\", \"1\", \"0\")", "")]
    public void Mid_TakesTheOneBasedSubstring(string formula, string expected)
    {
        var result = Run(formula);
        Assert.Empty(result.Errors);
        Assert.Equal(expected, result.Value);
    }

    [Theory]
    [InlineData("UPPER(\"lô a-12\")", "LÔ A-12")]
    [InlineData("LOWER(\"PART-ABC\")", "part-abc")]
    [InlineData("TRIM(\"  L01  \")", "L01")]
    [InlineData("LEFT(\"240131\", \"4\")", "2401")]
    [InlineData("RIGHT(\"240131\", \"2\")", "31")]
    [InlineData("RIGHT(\"AB\", \"5\")", "AB")]
    [InlineData("LEFT(\"AB\", \"0\")", "")]
    [InlineData("CONCAT(UPPER(FIELD(\"Lot\")), \"/\", RIGHT(FIELD(\"Year\"), \"2\"))", "L-7/26")]
    public void TextFunctions_ChangeAndCutText(string formula, string expected)
    {
        var result = new FormulaEngine().Evaluate(formula, new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase) { ["Lot"] = "l-7", ["Year"] = "2026" });
        Assert.Empty(result.Errors);
        Assert.Equal(expected, result.Value);
    }

    [Theory]
    [InlineData("EQUALS(\"QR\", \"qr\")", "true")]
    [InlineData("EQUALS(\"QR\", \"Code 128\")", "false")]
    [InlineData("EQUALS(\"\", \"\")", "true")]
    [InlineData("NOT(EQUALS(\"a\", \"b\"))", "true")]
    [InlineData("NOT(\"\")", "true")]
    [InlineData("NOT(\"không\")", "true")]
    [InlineData("NOT(\"Lô 7\")", "false")]
    [InlineData("AND(\"1\", \"true\", \"x\")", "true")]
    [InlineData("AND(\"1\", \"0\")", "false")]
    [InlineData("AND(EQUALS(FIELD(\"Type\"), \"qr\"), NOT(FIELD(\"Hold\")))", "true")]
    public void ConditionFunctions_CompareAndCombine(string formula, string expected)
    {
        var result = new FormulaEngine().Evaluate(formula, new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase) { ["Type"] = "QR", ["Hold"] = "" });
        Assert.Empty(result.Errors);
        Assert.Equal(expected, result.Value);
    }

    [Theory]
    [InlineData("DATEADD(\"2026-10-31\", \"24\", \"mo\")", "31/10/2028")]
    [InlineData("DATEADD(FIELD(\"NSX\"), \"-1\", \"d\", \"yyMMdd\")", "251231")]
    [InlineData("DATEADD(\"31/12/2026\", \"1\", \"w\", \"yyyy-MM-dd\")", "2027-01-07")]
    [InlineData("DATEADD(\"2026-10-31 14:30:00\", \"1\", \"D\")", "01/11/2026")]
    [InlineData("DATEADD(\"261031\", \"1\", \"y\", \"dd.MM.yyyy\", \"yyMMdd\")", "31.10.2027")]
    [InlineData("DATEFORMAT(\"2026-10-31\", \"yyMMdd\")", "261031")]
    [InlineData("DATEFORMAT(\"261031\", \"dd/MM/yyyy\", \"yyMMdd\")", "31/10/2026")]
    [InlineData("DATEFORMAT(FIELD(\"Empty\"), \"yyMMdd\")", "")]
    [InlineData("CONCAT(\"EXP \", DATEADD(FIELD(\"NSX\"), \"2\", \"y\", \"yyyy-MM\"))", "EXP 2028-01")]
    public void DateFunctions_MoveAndWriteDates(string formula, string expected)
    {
        var result = new FormulaEngine().Evaluate(formula, new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase) { ["NSX"] = "2026-01-01", ["Empty"] = "" });
        Assert.Empty(result.Errors);
        Assert.Equal(expected, result.Value);
    }

    [Theory]
    [InlineData("DATEADD(\"hôm nay\", \"1\", \"d\")")]
    [InlineData("DATEADD(\"2026-10-31\", \"một\", \"d\")")]
    [InlineData("DATEADD(\"2026-10-31\", \"1\", \"q\")")]
    [InlineData("DATEADD(\"2026-10-31\", \"1\")")]
    [InlineData("DATEFORMAT(\"2026-10-31\")")]
    [InlineData("DATEFORMAT(\"31-10\", \"yyMMdd\")")]
    public void DateFunctions_RejectWhatTheyCannotRead(string formula)
    {
        var result = Run(formula);
        Assert.NotEmpty(result.Errors);
        Assert.Equal(string.Empty, result.Value);
    }

    [Fact]
    public void EveryFunctionStartsAFormula()
    {
        foreach (var name in FormulaEvaluator.FunctionNames)
        {
            var formula = name + "(\"a\")";
            Assert.True(FormulaBindingEvaluator.LooksLikeFormula(formula), name);
            Assert.DoesNotContain(Run(formula).Errors, error => error.StartsWith("Unknown function", StringComparison.Ordinal));
        }

        // A NiceLabel script whose whole result is one call imports as that call.
        Assert.Equal("L-7", FormulaBindingEvaluator.Evaluate("UPPER(FIELD(\"?Lot\"))", new Dictionary<string, string> { ["?Lot"] = "l-7" }).Value);
        Assert.False(FormulaBindingEvaluator.LooksLikeFormula("LEFTOVER (2 pcs)"));
    }

    [Theory]
    [InlineData("EQUALS(\"a\")")]
    [InlineData("NOT(\"a\", \"b\")")]
    [InlineData("AND()")]
    public void ConditionFunctions_RejectBadArguments(string formula)
    {
        var result = Run(formula);
        Assert.NotEmpty(result.Errors);
        Assert.Equal(string.Empty, result.Value);
    }

    [Theory]
    [InlineData("UPPER(\"a\", \"b\")")]
    [InlineData("LEFT(\"ABC\")")]
    [InlineData("RIGHT(\"ABC\", \"-1\")")]
    [InlineData("LEFT(\"ABC\", \"x\")")]
    public void TextFunctions_RejectBadArguments(string formula)
    {
        var result = Run(formula);
        Assert.NotEmpty(result.Errors);
        Assert.Equal(string.Empty, result.Value);
    }

    [Theory]
    [InlineData("MID(\"ABC\", \"0\", \"2\")")]
    [InlineData("MID(\"ABC\", \"x\", \"2\")")]
    [InlineData("MID(\"ABC\", \"1\")")]
    public void Mid_RejectsBadArguments(string formula)
    {
        var result = Run(formula);
        Assert.NotEmpty(result.Errors);
        Assert.Equal(string.Empty, result.Value);
    }

    [Theory]
    [InlineData("400638133393", "4006381333931")] // EAN-13 data digits → the published EAN-13
    [InlineData("0123456789012", "01234567890128")]
    [InlineData("1234567", "12345670")]
    public void Gs1CheckDigit_AppendsTheModulo10Digit(string digits, string expected)
    {
        var result = Run("GS1CHECKDIGIT(FIELD(\"?GTIN\"))", ("?GTIN", digits));
        Assert.Empty(result.Errors);
        Assert.Equal(expected, result.Value);
    }

    [Fact]
    public void Gs1CheckDigit_RejectsNonDigits()
    {
        var result = Run("GS1CHECKDIGIT(\"12A4\")");
        Assert.NotEmpty(result.Errors);
        Assert.Equal(string.Empty, result.Value);
    }
}
