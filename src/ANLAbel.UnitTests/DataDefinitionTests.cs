using ANLAbel.Core.Data;
using ANLAbel.Core.Enums;
using ANLAbel.Core.Models;
using Xunit;

namespace ANLAbel.UnitTests;

/// <summary>Quản lý dữ liệu works on copies, and Nhập nguồn dữ liệu merges or overwrites by name.</summary>
public sealed class DataDefinitionTests
{
    [Fact]
    public void SerialCounterClone_CopiesEverySetting()
    {
        var source = new SerialCounterDefinition
        {
            CounterId = "abc",
            StartValue = 10,
            Step = 2,
            ChangeEvery = 3,
            MinimumValue = 5,
            MaximumValue = 500,
            Rollover = true,
            Digits = 6,
            Prefix = "SN",
            Suffix = "X",
            SharedFolder = @"\\server\counters",
            Alphabet = SerialCounterAlphabet.Custom,
            CustomAlphabet = "0123456789AB",
            Enabled = true
        };

        var copy = source.Clone();

        // Every public settable property, so a setting added later cannot be forgotten.
        foreach (var property in typeof(SerialCounterDefinition).GetProperties().Where(p => p.CanWrite && p.GetSetMethod() is not null))
        {
            Assert.Equal(property.GetValue(source), property.GetValue(copy));
        }

        copy.Prefix = "Y";
        Assert.Equal("SN", source.Prefix);
    }

    [Fact]
    public void PromptVariableClone_CopiesEverySetting()
    {
        var source = new PromptVariableDefinition { Name = "Lot", Prompt = "Số lô", DefaultValue = "A1", MaxLength = 6, DigitsOnly = true, Required = false };
        var copy = source.Clone();
        foreach (var property in typeof(PromptVariableDefinition).GetProperties().Where(p => p.CanWrite && p.GetSetMethod() is not null))
        {
            Assert.Equal(property.GetValue(source), property.GetValue(copy));
        }

        Assert.NotSame(source, copy);
    }

    [Fact]
    public void Import_Merge_KeepsExistingNames_AndAddsNewOnes()
    {
        var current = new[] { new PromptVariableDefinition { Name = "Lot", Prompt = "Số lô" } };
        var imported = new[] { new PromptVariableDefinition { Name = "LOT", Prompt = "Khác" }, new PromptVariableDefinition { Name = "Shift" } };

        var merged = DataDefinitionImport.MergePrompts(current, imported, overwrite: false);

        Assert.Equal(new[] { "Lot", "Shift" }, merged.Items.Select(p => p.Name));
        Assert.Equal("Số lô", merged.Items[0].Prompt);
        Assert.Equal((1, 0, 1), (merged.Added, merged.Replaced, merged.Kept));
        Assert.NotSame(imported[1], merged.Items[1]);
    }

    [Fact]
    public void Import_Overwrite_ReplacesInPlace()
    {
        var current = new[] { new DataTransformDefinition("Code", "CONCAT(\"A\")"), new DataTransformDefinition("Other", "CONCAT(\"B\")") };
        var imported = new[] { new DataTransformDefinition("code", "CONCAT(\"Z\")") };

        var merged = DataDefinitionImport.MergeTransforms(current, imported, overwrite: true);

        Assert.Equal(new[] { "code", "Other" }, merged.Items.Select(t => t.Name));
        Assert.Equal("CONCAT(\"Z\")", merged.Items[0].Formula);
        Assert.Equal((0, 1, 0), (merged.Added, merged.Replaced, merged.Kept));
    }
}
