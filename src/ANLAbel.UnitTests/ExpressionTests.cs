using ANLAbel.Core.Expressions;
using ANLAbel.Core.Expressions.Formulas;
using Xunit;

namespace ANLAbel.UnitTests;

public sealed class ExpressionTests
{
    [Fact]
    public void BindingExpression_ReplacesFieldPlaceholders()
    {
        var row = new Dictionary<string, string> { ["PartNo"] = "PN-001", ["Qty"] = "20", ["Lot"] = "L01" };
        var value = BindingExpressionEvaluator.Evaluate("P{PartNo} Q{Qty} 1T{Lot}", row);
        Assert.Equal("PPN-001 Q20 1TL01", value);
    }

    [Fact]
    public void BindingExpression_ExposesUsedFieldNames()
    {
        Assert.Equal(3, BindingExpressionEvaluator.GetFields("P{PartNo} Q{Qty} 1T{Lot}").Count);
    }

    [Fact]
    public void BindingExpression_NormalizesFieldNameSeparators()
    {
        var row = new Dictionary<string, string> { ["Part No"] = "PN-SPACE" };
        var value = BindingExpressionEvaluator.Evaluate("Normalized {Part_No}", row);
        Assert.Equal("Normalized PN-SPACE", value);
    }

    [Fact]
    public void FormulaEngine_ConcatAndField_ProducesCorrectResult()
    {
        var engine = new FormulaEngine();
        var row = new Dictionary<string, string?> { ["Name"] = " Product A ", ["Code"] = "ABC123", ["Empty"] = null, ["Lot No"] = "LOT-77" };
        var result = engine.Evaluate("CONCAT(\"Tên: \", FIELD(\"Name\"), \", Mã: \", FIELD(\"code\"))", row);
        Assert.Equal("Tên: Product A, Mã: ABC123", result.Value);
        Assert.Empty(result.Errors);
        Assert.Equal(2, result.UsedFields.Count);
        Assert.Equal("Name", result.UsedFields[0]);
        Assert.Equal("code", result.UsedFields[1]);
    }

    [Fact]
    public void FormulaEngine_NestedConcatWithNullField_TreatsNullAsEmpty()
    {
        var engine = new FormulaEngine();
        var row = new Dictionary<string, string?> { ["Code"] = "ABC123", ["Empty"] = null };
        var result = engine.Evaluate("CONCAT(CONCAT(\"Mã hàng: \", FIELD(\"Code\")), \" - \", FIELD(\"Empty\"))", row);
        Assert.Equal("Mã hàng: ABC123 - ", result.Value);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void FormulaEngine_NormalizedFieldName_Resolves()
    {
        var engine = new FormulaEngine();
        var row = new Dictionary<string, string?> { ["Lot No"] = "LOT-77" };
        var result = engine.Evaluate("CONCAT(\"Lot: \", FIELD(\"LotNo\"))", row);
        Assert.Equal("Lot: LOT-77", result.Value);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void FormulaEngine_MissingField_ReturnsEmptyValueAndReportsError()
    {
        var engine = new FormulaEngine();
        var row = new Dictionary<string, string?>();
        var result = engine.Evaluate("CONCAT(\"Missing: \", FIELD(\"Unknown\"))", row);
        Assert.Equal("Missing: ", result.Value);
        Assert.Single(result.Errors);
        Assert.Equal("Unknown", result.UsedFields[0]);
    }

    [Fact]
    public void FormulaEngine_ParseError_ReturnsEmptyValueWithErrors()
    {
        var engine = new FormulaEngine();
        var row = new Dictionary<string, string?> { ["Code"] = "ABC123" };
        var result = engine.Evaluate("CONCAT(\"A\", FIELD(\"Code\")", row);
        Assert.Equal(string.Empty, result.Value);
        Assert.NotEmpty(result.Errors);
    }
}
