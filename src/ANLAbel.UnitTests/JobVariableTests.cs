using ANLAbel.Core.Enums;
using ANLAbel.Core.Models;
using ANLAbel.Core.Printing;
using ANLAbel.Core.Scene;
using ANLAbel.Data.PrintLogs;
using ANLAbel.Project.SaveLoad;
using Xunit;

namespace ANLAbel.UnitTests;

public sealed class JobVariableTests : IDisposable
{
    private static readonly DateTimeOffset JobTime = new(2026, 1, 31, 22, 15, 0, TimeZoneInfo.Local.GetUtcOffset(new DateTime(2026, 1, 31, 22, 15, 0)));
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"anl-jobvars-{Guid.NewGuid():N}");

    public JobVariableTests() => Directory.CreateDirectory(_directory);

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    private static LabelTemplate Template(params string[] bindings)
    {
        var template = new LabelTemplate { Name = "Job vars" };
        foreach (var binding in bindings)
        {
            template.Objects.Add(new LabelObject { Type = ObjectType.Text, BindingExpression = binding });
        }

        return template;
    }

    private static Dictionary<string, string> Answers(params (string Key, string Value)[] pairs)
        => pairs.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);

    [Theory]
    [InlineData("#NOW", "31/01/2026")]
    [InlineData("#NOW:yyyy-MM-dd HH:mm", "2026-01-31 22:15")]
    [InlineData("#NOW+1d:dd/MM/yyyy", "01/02/2026")]
    [InlineData("#NOW+1mo:dd/MM/yyyy", "28/02/2026")]
    [InlineData("#NOW-1y+2w:yyyyMMdd", "20250214")]
    [InlineData("#now+3h:HH", "01")]
    public void DateFieldsUseTheJobTimeWithOffsetsAndFormats(string token, string expected)
    {
        Assert.True(JobVariableContract.TryFormatDate(token, JobTime, out var value, out var error), error);
        Assert.Equal(expected, value);
    }

    [Theory]
    [InlineData("#NOW+3x")]
    [InlineData("#NOWISH")]
    public void UnreadableDateFieldsAreExplained(string token)
    {
        Assert.False(JobVariableContract.TryFormatDate(token, JobTime, out _, out var error));
        Assert.Contains("is not understood", error);
    }

    [Fact]
    public void TokensComeFromVisibleObjectBindingsOnly()
    {
        var template = Template("Lot {?Lot} exp {#NOW+30d:dd/MM/yyyy}", "{Part}", "{#SERIAL}", "{?lot}");
        template.Objects.Add(new LabelObject { Type = ObjectType.Text, BindingExpression = "{?Hidden}", IsVisible = false });

        Assert.Equal(new[] { "?Lot", "#NOW+30d:dd/MM/yyyy", "?lot" }, JobVariableContract.FindTokens(template));
    }

    [Fact]
    public void PromptsUseTheirDefinitionsAndValidateEachAnswer()
    {
        var template = Template("{?Lot}", "{?Shift}");
        template.PromptVariables.Add(new PromptVariableDefinition { Name = "?Lot", Prompt = "Số lô", MaxLength = 6, DigitsOnly = true });

        var prompts = JobVariableContract.RequiredPrompts(template);

        Assert.Equal(new[] { "Lot", "Shift" }, prompts.Select(p => p.Name));
        Assert.Equal("Số lô", prompts[0].Prompt);
        Assert.Contains("needs a value", JobVariableContract.ValidatePromptValue(prompts[1], " "));
        Assert.Contains("digits only", JobVariableContract.ValidatePromptValue(prompts[0], "12A"));
        Assert.Contains("at most 6", JobVariableContract.ValidatePromptValue(prompts[0], "1234567"));
        Assert.Equal(string.Empty, JobVariableContract.ValidatePromptValue(prompts[0], "123456"));
    }

    [Fact]
    public void ApplyWritesTheSameJobValuesIntoEveryRow()
    {
        var template = Template("{?Lot}/{#NOW:yyMMdd}");
        IReadOnlyDictionary<string, string>?[] rows = [new Dictionary<string, string> { ["Part"] = "A" }, null];

        Assert.True(JobVariableContract.TryApply(template, rows, JobTime, Answers(("Lot", "L7")), out var applied, out var error), error);

        Assert.All(applied, row =>
        {
            Assert.Equal("L7", row!["?Lot"]);
            Assert.Equal("260131", row["#NOW:yyMMdd"]);
        });
        Assert.Equal("A", applied[0]!["Part"]);
    }

    [Fact]
    public void AMissingRequiredAnswerBlocksTheJob()
    {
        var template = Template("{?Lot}");

        Assert.False(JobVariableContract.TryApply(template, [null], JobTime, Answers(), out _, out var error));
        Assert.Contains("needs a value", error);
    }

    [Fact]
    public void DateFieldsThatDifferOnlyInCaseAreRefused()
    {
        var template = Template("{#NOW:MM}", "{#NOW:mm}");

        Assert.False(JobVariableContract.TryApply(template, [null], JobTime, Answers(), out _, out var error));
        Assert.Contains("differ only in letter case", error);
    }

    [Fact]
    public void TheDesignerShowsPlaceholdersInsteadOfBlanks()
    {
        var template = Template("{?Lot}", "{?Shift}", "{#NOW:yyyy}");
        template.PromptVariables.Add(new PromptVariableDefinition { Name = "Shift", DefaultValue = "A" });

        var row = JobVariableContract.ApplyForDesign(template, null, JobTime, Answers());

        Assert.Equal("‹Lot›", row!["?Lot"]);
        Assert.Equal("A", row["?Shift"]);
        Assert.Equal("2026", row["#NOW:yyyy"]);
    }

    [Fact]
    public void AReprintRecoversTheOriginalTimeAndAnswers()
    {
        var store = new PrintJobValueStore(Path.Combine(_directory, "values.jsonl"));
        var template = Template("{?Lot} {#NOW:dd/MM/yyyy HH:mm}");
        Assert.True(JobVariableContract.TryApply(template, [null], JobTime, Answers(("Lot", "L1")), out var original, out _));
        store.Record("job-1", JobTime, Answers(("Lot", "L1")));
        store.Record("job-1", JobTime.AddDays(3), Answers(("Lot", "changed")));

        var stored = store.Find("job-1:reprint:abc");
        Assert.NotNull(stored);
        Assert.True(JobVariableContract.TryApply(template, [null], stored!.JobTime, stored.PromptValues, out var reprinted, out _));

        Assert.Equal(PrintJobManifest.ComputeRowsFingerprint(original), PrintJobManifest.ComputeRowsFingerprint(reprinted));
    }

    [Fact]
    public void ACutOffRecordIsIgnoredAndADamagedOneBlocks()
    {
        var path = Path.Combine(_directory, "values.jsonl");
        var store = new PrintJobValueStore(path);
        store.Record("job-1", JobTime, Answers());
        File.AppendAllText(path, "{\"jobId\":\"torn");
        Assert.Null(store.Find("torn"));
        store.Record("job-2", JobTime, Answers());
        Assert.NotNull(new PrintJobValueStore(path).Find("job-2"));

        File.WriteAllText(path, "nope\n");
        Assert.Throws<InvalidDataException>(() => new PrintJobValueStore(path).Find("job-1"));
    }

    [Fact]
    public async Task PromptDefinitionsAreSavedAndOnlyChangeTheDocumentHashWhenPresent()
    {
        var template = Template("{?Lot}");
        var before = DocumentSnapshot.Capture(template).DocumentHash;
        template.PromptVariables.Add(new PromptVariableDefinition { Name = "Lot", Prompt = "Lô", MaxLength = 8, DigitsOnly = true, Required = false, DefaultValue = "0" });
        Assert.NotEqual(before, DocumentSnapshot.Capture(template).DocumentHash);

        var path = Path.Combine(_directory, "prompts.anlabel");
        var service = new ProjectFileService();
        await service.SaveAsync(template, path);
        var loaded = (await service.LoadAsync(path)).PromptVariables.Single();

        Assert.Equal(("Lot", "Lô", "0", 8, true, false), (loaded.Name, loaded.Prompt, loaded.DefaultValue, loaded.MaxLength, loaded.DigitsOnly, loaded.Required));
        template.PromptVariables.Clear();
        Assert.Equal(before, DocumentSnapshot.Capture(template).DocumentHash);
    }
}
