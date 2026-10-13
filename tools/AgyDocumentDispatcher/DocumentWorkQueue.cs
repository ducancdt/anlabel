using System.Text.RegularExpressions;

namespace AgyDocumentDispatcher;

public sealed record DocumentWorkItem(
    string Id,
    string SourcePath,
    int LineNumber,
    string VerbatimPhrase,
    string AssignmentText);

/// <summary>
/// Reads in-repo planning/handoff documents and yields remaining open work
/// as verbatim excerpts. Selection is pure over file contents.
/// </summary>
public static class DocumentWorkQueue
{
    private static readonly Regex OpenCheckbox = new(
        @"^\s*[-*]\s+\[\s\]\s+(.+)$",
        RegexOptions.Compiled);

    private static readonly Regex StatusLine = new(
        @"^\s*\*\*Status:\*\*\s*(.+)$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly string[] ClosedStatusMarkers =
    [
        "implemented",
        "runtime-verified",
        "closed",
        "complete",
        "done",
        "da xong",
        // A "documentation-only" checkpoint asserts there is no code work to do;
        // by itself it must not make a title/status line look like an open task.
        "documentation-only",
    ];

    private static readonly string[] OpenStatusMarkers =
    [
        "deferred",
        "active",
        "open",
        "remain",
    ];

    public static IReadOnlyList<string> ListSourceDocuments(string repoRoot)
    {
        if (string.IsNullOrWhiteSpace(repoRoot) || !Directory.Exists(repoRoot))
        {
            throw new DirectoryNotFoundException("Repository root does not exist: " + repoRoot);
        }

        var files = new List<string>();
        AddIfExists(files, Path.Combine(repoRoot, "PLAN.md"));
        AddIfExists(files, Path.Combine(repoRoot, "MASTER_PLAN.md"));
        AddIfExists(files, Path.Combine(repoRoot, "docs", "figma-ui-handoff-template.md"));

        var docs = Path.Combine(repoRoot, "docs");
        if (Directory.Exists(docs))
        {
            foreach (var path in Directory.EnumerateFiles(docs, "*HANDOFF*.md", SearchOption.AllDirectories)
                         .OrderBy(p => p, StringComparer.OrdinalIgnoreCase))
            {
                AddIfExists(files, path);
            }
        }

        return files;
    }

    public static IReadOnlyList<DocumentWorkItem> Load(string repoRoot)
    {
        var repo = Path.GetFullPath(repoRoot);
        var items = new List<DocumentWorkItem>();
        foreach (var path in ListSourceDocuments(repo))
        {
            items.AddRange(ExtractFromFile(path, repo));
        }

        return items;
    }

    public static DocumentWorkItem? SelectNext(string repoRoot, IEnumerable<string>? skipIds = null)
    {
        var skip = skipIds is null
            ? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(skipIds, StringComparer.OrdinalIgnoreCase);

        foreach (var item in Load(repoRoot))
        {
            if (!skip.Contains(item.Id))
            {
                return item;
            }
        }

        return null;
    }

    public static IReadOnlyList<DocumentWorkItem> ExtractFromFile(string documentPath, string? repoRoot = null)
    {
        var fullPath = Path.GetFullPath(documentPath);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException("Planning document not found.", fullPath);
        }

        var text = File.ReadAllText(fullPath);
        var relative = ToRelative(fullPath, repoRoot);
        var items = new List<DocumentWorkItem>();
        var lines = text.Split(["\r\n", "\n"], StringSplitOptions.None);
        string? heading = null;
        string? statusVerbatim = null;
        var statusLooksOpen = false;

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var trimmed = line.Trim();
            if (trimmed.StartsWith("# ", StringComparison.Ordinal) && heading is null)
            {
                heading = trimmed;
            }

            var statusMatch = StatusLine.Match(line);
            if (statusMatch.Success)
            {
                statusVerbatim = trimmed;
                var status = statusMatch.Groups[1].Value;
                statusLooksOpen = LooksOpen(status);
            }

            var box = OpenCheckbox.Match(line);
            if (!box.Success)
            {
                continue;
            }

            var body = box.Groups[1].Value.Trim();
            if (body.Length == 0)
            {
                continue;
            }

            if (IsConstraintNotWork(body))
            {
                continue;
            }

            var verbatim = trimmed;
            items.Add(CreateItem(relative, fullPath, i + 1, verbatim, heading));
        }

        if (items.Count == 0 && statusLooksOpen && statusVerbatim is not null)
        {
            // Keep the excerpt truly verbatim: the Status line is a literal source
            // line, while the document heading is surfaced separately in the
            // assignment text. Joining them would fabricate a phrase absent from
            // the file and break the "verbatim from the document" contract.
            items.Add(CreateItem(relative, fullPath, 1, statusVerbatim, heading));
        }

        return items;
    }

    private static DocumentWorkItem CreateItem(
        string relative,
        string fullPath,
        int lineNumber,
        string verbatim,
        string? heading)
    {
        var assignment = "Source document: " + relative + Environment.NewLine
                         + (heading is null ? string.Empty : "Document title: " + heading + Environment.NewLine)
                         + "Open task (verbatim from the document):" + Environment.NewLine
                         + verbatim;
        return new DocumentWorkItem(
            Id: relative + ":" + lineNumber.ToString(System.Globalization.CultureInfo.InvariantCulture),
            SourcePath: fullPath,
            LineNumber: lineNumber,
            VerbatimPhrase: verbatim,
            AssignmentText: assignment);
    }

    private static bool LooksOpen(string status)
    {
        var lower = status.ToLowerInvariant();
        if (OpenStatusMarkers.Any(m => lower.Contains(m, StringComparison.Ordinal)))
        {
            return true;
        }

        return !ClosedStatusMarkers.Any(m => lower.Contains(m, StringComparison.Ordinal));
    }

    private static bool IsConstraintNotWork(string body)
    {
        var lower = body.ToLowerInvariant();
        if (lower.Contains("khong phai task", StringComparison.Ordinal)
            || lower.Contains("not a task", StringComparison.Ordinal))
        {
            return true;
        }

        // Standing governance rules phrased as "Any future ..." / "Any UI change ..."
        // describe a policy that always applies, not an action to perform now.
        if (lower.StartsWith("any future", StringComparison.Ordinal)
            || lower.StartsWith("any ui change", StringComparison.Ordinal))
        {
            return true;
        }

        // Meta notes that assert what a document is NOT are not dispatchable work.
        if (lower.Contains("is not evidence", StringComparison.Ordinal))
        {
            return true;
        }

        if (lower.Contains("text/textbox", StringComparison.Ordinal)
            && (lower.Contains("is changed", StringComparison.Ordinal)
                || lower.Contains("unchanged", StringComparison.Ordinal)
                || lower.Contains("untouched", StringComparison.Ordinal)
                || lower.Contains("not changed", StringComparison.Ordinal)
                || lower.Contains("not bundled", StringComparison.Ordinal)))
        {
            return true;
        }

        return lower.Contains("never mutate", StringComparison.Ordinal)
               || lower.Contains("remain intact", StringComparison.Ordinal);
    }

    private static void AddIfExists(List<string> files, string path)
    {
        var full = Path.GetFullPath(path);
        if (File.Exists(full) && files.TrueForAll(existing => !existing.Equals(full, StringComparison.OrdinalIgnoreCase)))
        {
            files.Add(full);
        }
    }

    private static string ToRelative(string fullPath, string? repoRoot)
    {
        if (string.IsNullOrWhiteSpace(repoRoot))
        {
            return Path.GetFileName(fullPath);
        }

        var root = Path.GetFullPath(repoRoot);
        if (!root.EndsWith(Path.DirectorySeparatorChar))
        {
            root += Path.DirectorySeparatorChar;
        }

        var full = Path.GetFullPath(fullPath);
        if (full.StartsWith(root, StringComparison.OrdinalIgnoreCase))
        {
            return full[root.Length..].Replace('\\', '/');
        }

        return Path.GetFileName(fullPath);
    }
}
