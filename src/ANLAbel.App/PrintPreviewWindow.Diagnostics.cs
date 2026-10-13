using ANLAbel.Printing.PrinterProfiles;
using ANLAbel.Printing.RenderPipeline;

namespace ANLAbel.App;

public partial class PrintPreviewWindow
{
    private IReadOnlyList<PreviewContentIssue> _contentIssues = Array.Empty<PreviewContentIssue>();

    // Validate each source record once, while retaining the first label ordinal
    // for navigation and the print handler's source-row mapping.
    private async Task<PrintPreflightResult> ValidateSelectedContentAsync(
        IReadOnlyList<IReadOnlyDictionary<string, string>?> rows,
        IReadOnlyList<int> sourceRows,
        PrintRenderPlan plan,
        CancellationToken cancellationToken,
        IProgress<PrintPreflightProgress> progress)
    {
        var seen = new HashSet<int>();
        var uniqueRows = new List<IReadOnlyDictionary<string, string>?>();
        var firstLabels = new List<int>();
        for (var index = 0; index < rows.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (seen.Add(sourceRows[index]))
            {
                uniqueRows.Add(rows[index]);
                firstLabels.Add(index + 1);
            }
        }

        var result = await _printService.ValidateRowsAsync(
            _template, uniqueRows, plan, cancellationToken, progress);
        return new PrintPreflightResult(result.Issues.Select(issue => issue.RowNumber > 0
            ? issue with { RowNumber = firstLabels[issue.RowNumber - 1] }
            : issue).ToArray());
    }

    private void SetPreflightResult(PrintPreflightResult result, IReadOnlyList<int>? sourceRows = null)
    {
        _preflightResult = result;
        var firstLabelBySource = new Dictionary<int, int>();
        foreach (var instance in _previewInstances)
        {
            firstLabelBySource.TryAdd(instance.SourceRowNumber, instance.SequenceOrdinal);
        }

        var groups = new List<PreviewContentIssue>();
        foreach (var group in result.Issues.GroupBy(issue =>
                     (issue.ObjectName, issue.ObjectType, issue.Message, IsTemplate: issue.RowNumber <= 0)))
        {
            var affectedRows = group.Where(issue => issue.RowNumber > 0)
                .Select(issue =>
                {
                    // Copies issues are numbered by data row already; validator
                    // issues by label (see ToCopiesPreflightResult).
                    if (string.Equals(issue.ObjectType, CopiesIssueObjectType, StringComparison.Ordinal))
                    {
                        return issue.RowNumber;
                    }

                    if (sourceRows is not null
                        && issue.RowNumber <= sourceRows.Count)
                    {
                        return sourceRows[issue.RowNumber - 1];
                    }

                    return issue.RowNumber <= _previewInstances.Count
                        ? _previewInstances[issue.RowNumber - 1].SourceRowNumber
                        : 0;
                })
                .Where(row => row > 0)
                .Distinct()
                .OrderBy(row => row)
                .ToArray();
            var labelNumber = affectedRows.Length > 0
                && firstLabelBySource.TryGetValue(affectedRows[0], out var firstLabel) ? firstLabel : 0;
            groups.Add(new PreviewContentIssue(
                labelNumber,
                ANLAbel.App.Services.PreflightIssueText.Line(affectedRows, group.Key.ObjectName, group.Key.ObjectType, group.Key.Message)));
        }
        _contentIssues = groups;
    }

    public sealed record PreviewContentIssue(int LabelNumber, string Summary)
    {
        public bool CanNavigate => LabelNumber > 0;
        public string NavigationText => CanNavigate ? "Đi tới tem đầu tiên bị ảnh hưởng" : "Xem lại thiết kế hoặc cài đặt tem";
    }
}
