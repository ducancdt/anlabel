using ANLAbel.Core.Expressions;
using ANLAbel.Core.Expressions.Formulas;
using Xunit;

namespace ANLAbel.UnitTests;

/// <summary>
/// DB03: which header names tie for a binding (so only column order decides),
/// and formula string literals decoded exactly as the formula parser does.
/// </summary>
public sealed class FieldResolutionTieTests
{
    [Fact]
    public void An_exact_header_never_ties_with_lookalikes()
    {
        Assert.Equal(new[] { "Part No" }, FieldNameResolver.FindTies("part no", new[] { "Part-No", "Part No", "PartNo" }));
    }

    [Fact]
    public void Lookalike_headers_of_the_same_length_tie_and_the_first_is_the_one_resolved()
    {
        var available = new[] { "Part-No", "Lot", "Part No" };

        Assert.Equal(new[] { "Part-No", "Part No" }, FieldNameResolver.FindTies("PartNo", available));
        Assert.True(FieldNameResolver.TryResolveFieldName("PartNo", available, out var resolved));
        Assert.Equal("Part-No", resolved);
    }

    [Fact]
    public void A_closer_length_wins_without_a_tie()
    {
        Assert.Equal(new[] { "Part No" }, FieldNameResolver.FindTies("PartNo", new[] { "PART_NO.", "Part No" }));
    }

    [Fact]
    public void Token_matches_tie_on_length()
    {
        Assert.Equal(new[] { "Part No", "Part_No" }, FieldNameResolver.FindTies("Part-No", new[] { "Part No", "Lot", "Part_No" }));
    }

    [Fact]
    public void No_match_gives_no_ties()
    {
        Assert.Empty(FieldNameResolver.FindTies("Qty", new[] { "Part No", "Lot" }));
    }

    [Theory]
    [InlineData("Part No")]
    [InlineData("W\\H")]
    [InlineData("C:\\\\data")]
    [InlineData("say \\\"hi\\\"")]
    [InlineData("line\\nbreak")]
    [InlineData("tab\\there")]
    public void String_literal_decoding_matches_the_parser(string body)
    {
        var parsed = new FormulaEngine().Parse($"FIELD(\"{body}\")");
        var call = Assert.IsType<FormulaFunctionCallNode>(parsed.Root);
        var literal = Assert.IsType<FormulaStringLiteralNode>(Assert.Single(call.Arguments));

        Assert.Equal(literal.Value, FormulaParser.UnescapeStringLiteral(body));
    }
}
