using ANLAbel.Core.Expressions.Formulas;
using Xunit;

namespace ANLAbel.UnitTests;

/// <summary>
/// A formula string reads \uXXXX, so an ISO/IEC 15434 envelope can carry RS,
/// GS and EOT; any other backslash stays as typed.
/// </summary>
public sealed class FormulaControlCharacterTests
{
    private static readonly Dictionary<string, string> Row = new() { ["P"] = "A1" };

    [Fact]
    public void UnicodeEscapes_BecomeControlCharacters()
    {
        var value = FormulaBindingEvaluator.Evaluate("CONCAT(\"[)>\\u001E06\\u001DP\", FIELD(\"P\"), \"\\u001E\\u0004\")", Row).Value;

        Assert.Equal("[)>\u001E06\u001DPA1\u001E\u0004", value);
    }

    [Theory]
    [InlineData("CONCAT(\"C:\\users\")", "C:\\users")]
    [InlineData("CONCAT(\"x\\u12\")", "x\\u12")]
    [InlineData("CONCAT(\"\\u004\")", "\\u004")]
    public void OtherBackslashes_StayAsTyped(string formula, string expected)
    {
        Assert.Equal(expected, FormulaBindingEvaluator.Evaluate(formula, Row).Value);
        Assert.Equal(expected, FormulaParser.UnescapeStringLiteral(formula[8..^2]));
    }
}
