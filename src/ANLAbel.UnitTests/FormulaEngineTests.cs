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
}
