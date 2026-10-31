using ANLAbel.Core.Data;
using ANLAbel.Core.Models;
using Xunit;

namespace ANLAbel.UnitTests;

public sealed class PastedDefinitionsTests
{
    private static LabelTemplate Source()
    {
        var template = new LabelTemplate();
        template.DataTransforms.Add(new DataTransformDefinition("Mã QR", "CONCAT(FIELD(\"PO\"), \"|\", FIELD(\"?Lô\"), \"|\", FIELD(\"Tổng\"))"));
        template.DataTransforms.Add(new DataTransformDefinition("Tổng", "FIELD(\"QTY\")"));
        template.DataTransforms.Add(new DataTransformDefinition("Thừa", "FIELD(\"X\")"));
        template.PromptVariables.Add(new PromptVariableDefinition { Name = "Lô", Prompt = "Số lô", MaxLength = 8 });
        template.PromptVariables.Add(new PromptVariableDefinition { Name = "Ca", Prompt = "Ca làm" });
        return template;
    }

    [Fact]
    public void ReadBy_FollowsFunctionsAndPromptsTheObjectsRead()
    {
        var objects = new[]
        {
            new LabelObject { Name = "QR", BindingExpression = "{Mã QR}" },
            new LabelObject { Name = "PO", BindingExpression = "{PO}" }
        };

        var carried = DataDefinitionImport.ReadBy(Source(), objects);

        Assert.Equal(new[] { "Mã QR", "Tổng" }, carried.Transforms.Select(transform => transform.Name));
        Assert.Equal(new[] { "Lô" }, carried.Prompts.Select(prompt => prompt.Name));
        Assert.Equal(8, carried.Prompts[0].MaxLength);
    }

    [Fact]
    public void ReadBy_FollowsAFunctionReadByALooseName()
    {
        var carried = DataDefinitionImport.ReadBy(Source(), new[] { new LabelObject { BindingExpression = "{Mã_QR}" } });

        Assert.Equal(new[] { "Mã QR", "Tổng" }, carried.Transforms.Select(transform => transform.Name));
    }

    [Fact]
    public void ReadBy_LeavesTheFunctionWhenTheNameIsASourceColumn()
    {
        // The source label reads its column “Mã_QR”: the function “Mã QR” does not go along.
        var carried = DataDefinitionImport.ReadBy(Source(), new[] { new LabelObject { BindingExpression = "{Mã_QR}" } }, new[] { "Mã_QR" });

        Assert.Empty(carried.Transforms);
    }

    [Fact]
    public void FunctionRead_PrefersAColumnNamedExactly()
    {
        Assert.Equal("Mã QR", DataDefinitionImport.FunctionRead("Mã_QR", new[] { "Mã QR", "Tổng" }));
        Assert.Null(DataDefinitionImport.FunctionRead("Mã_QR", new[] { "Mã QR" }, new[] { "Mã_QR" }));
        Assert.Null(DataDefinitionImport.FunctionRead("PO", new[] { "Mã QR" }));
    }

    [Fact]
    public void ReadBy_ReadsPromptsOfTextAndOfVisibilityConditions()
    {
        var objects = new[]
        {
            new LabelObject { Name = "Lô", BindingExpression = "Lô: {?lô}", VisibilityCondition = "{?Ca}" }
        };

        var carried = DataDefinitionImport.ReadBy(Source(), objects);

        Assert.Empty(carried.Transforms);
        Assert.Equal(new[] { "Lô", "Ca" }, carried.Prompts.Select(prompt => prompt.Name));
    }

    [Fact]
    public void BringAlong_AddsMissingNamesAndKeepsTheLabelsOwn()
    {
        var target = new LabelTemplate();
        target.DataTransforms.Add(new DataTransformDefinition("tổng", "FIELD(\"SL\")"));
        target.PromptVariables.Add(new PromptVariableDefinition { Name = "Ca", Prompt = "Ca làm" });
        var carried = DataDefinitionImport.ReadBy(Source(), new[]
        {
            new LabelObject { Name = "QR", BindingExpression = "{Mã QR}", VisibilityCondition = "{?Ca}" }
        });

        var brought = DataDefinitionImport.BringAlong(target, carried);

        Assert.Equal(new[] { "tổng", "Mã QR" }, target.DataTransforms.Select(transform => transform.Name));
        Assert.Equal("FIELD(\"SL\")", target.DataTransforms[0].Formula);
        Assert.Equal(new[] { "Ca", "Lô" }, target.PromptVariables.Select(prompt => prompt.Name));
        Assert.Equal(new[] { "Mã QR" }, brought.Functions);
        Assert.Equal(new[] { "Lô" }, brought.Prompts);
        Assert.Equal(new[] { "tổng" }, brought.DifferentFunctions);
        Assert.Empty(brought.DifferentPrompts);
    }

    [Fact]
    public void BringAlong_LeavesAFunctionNamedLikeADataColumn()
    {
        var target = new LabelTemplate();
        var carried = DataDefinitionImport.ReadBy(Source(), new[] { new LabelObject { BindingExpression = "{Mã QR}" } });

        var brought = DataDefinitionImport.BringAlong(target, carried, new[] { " tổng " });

        Assert.Equal(new[] { "Mã QR" }, target.DataTransforms.Select(transform => transform.Name));
        Assert.Equal(new[] { "Tổng" }, brought.ColumnFunctions);
        Assert.Equal(new[] { "Mã QR" }, brought.Functions);
    }

    [Fact]
    public void BringAlong_IntoTheSameLabel_ChangesNothing()
    {
        var source = Source();
        var carried = DataDefinitionImport.ReadBy(source, new[] { new LabelObject { BindingExpression = "{Mã QR}" } });

        var brought = DataDefinitionImport.BringAlong(source, carried);

        Assert.Equal(3, source.DataTransforms.Count);
        Assert.Equal(2, source.PromptVariables.Count);
        Assert.Empty(brought.Functions.Concat(brought.Prompts).Concat(brought.DifferentFunctions).Concat(brought.DifferentPrompts));
    }
}
