namespace AgyDocumentDispatcher;

public sealed record AgyWorkerInvocation(
    string FileName,
    IReadOnlyList<string> Arguments,
    string WorkingDirectory,
    string Prompt,
    DocumentWorkItem Assignment)
{
    public string DisplayCommand => FileName + " " + string.Join(" ", Arguments.Select(QuoteIfNeeded));

    private static string QuoteIfNeeded(string value)
    {
        if (value.Length == 0 || value.Any(char.IsWhiteSpace))
        {
            return "\"" + value.Replace("\"", "\\\"", StringComparison.Ordinal) + "\"";
        }

        return value;
    }
}

public static class AgyWorkerAssembler
{
    public const string DefaultAgyRelativePath = @"agy\bin\agy.exe";

    public static string ResolveAgyPath(string? overridePath = null)
    {
        if (!string.IsNullOrWhiteSpace(overridePath))
        {
            return Path.GetFullPath(overridePath);
        }

        var local = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            DefaultAgyRelativePath);
        if (File.Exists(local))
        {
            return local;
        }

        return "agy";
    }

    public static AgyWorkerInvocation Assemble(
        string repoRoot,
        DocumentWorkItem assignment,
        string? agyPath = null)
    {
        var toplevel = GitToplevel.Resolve(repoRoot);
        var exe = ResolveAgyPath(agyPath);
        var prompt = BuildPrompt(assignment, toplevel);
        IReadOnlyList<string> arguments = ["--print=" + prompt];
        return new AgyWorkerInvocation(
            FileName: exe,
            Arguments: arguments,
            WorkingDirectory: toplevel,
            Prompt: prompt,
            Assignment: assignment);
    }

    public static string BuildPrompt(DocumentWorkItem assignment, string gitToplevel)
    {
        return
            "You are agy (Gemini), the worker for this ANLAbel document dispatch." + Environment.NewLine
            + "Work in this same git working tree. Do not create, clone, or switch git worktrees." + Environment.NewLine
            + "Required cwd git toplevel: " + gitToplevel + Environment.NewLine
            + "Follow AGENTS.md and AGENT.md. Do not change the protected Text/TextBox contract." + Environment.NewLine
            + Environment.NewLine
            + assignment.AssignmentText;
    }
}
