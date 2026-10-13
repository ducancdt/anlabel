using ANLAbel.Core.Models;
using ANLAbel.Core.Printing;
using Xunit;

namespace ANLAbel.UnitTests;

/// <summary>
/// Shipped prepare/print gate for configured CopiesField (CORE DB08/DB10).
/// Drives <see cref="CopiesFieldContract.TryAuthorizePrepare"/> — the same entry
/// Print Preview calls before copy expansion and dispatch.
/// </summary>
public sealed class CopiesFieldContractTests
{
    [Fact]
    public void TryAuthorizePrepare_BlocksEntireBatchWhenOneSelectedRowHasInvalidCopies()
    {
        // Row 1 valid (2 copies), row 2 blank, row 3 valid (1). All selected.
        // Old ResolveCopiesForRow path would set row 2 → 0 and still prepare rows 1+3.
        // The prepare gate must refuse the whole selection with a row/field diagnostic.
        var selected = new (int SourceRowNumber, IReadOnlyDictionary<string, string>? Row)[]
        {
            (1, Row(("Part", "A"), ("Qty", "2"))),
            (2, Row(("Part", "B"), ("Qty", ""))),
            (3, Row(("Part", "C"), ("Qty", "1")))
        };

        Assert.False(CopiesFieldContract.TryAuthorizePrepare("Qty", selected, out var issues));
        Assert.Equal(0, DatabaseConfig.ResolveCopiesForRow("Qty", selected[1].Row));

        var issue = Assert.Single(issues);
        Assert.Equal(2, issue.SourceRowNumber);
        Assert.Equal("Qty", issue.FieldName);
        Assert.Contains("blank", issue.Message, StringComparison.OrdinalIgnoreCase);

        var message = CopiesFieldContract.ToUserMessage(issues);
        Assert.Contains("blocked", message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Row 2", message, StringComparison.Ordinal);
        Assert.Contains("Qty", message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("-3")]
    [InlineData("1.5")]
    [InlineData("1000")]
    public void TryAuthorizePrepare_BlocksNonIntegerNegativeAndOverflow(string raw)
    {
        var selected = new (int SourceRowNumber, IReadOnlyDictionary<string, string>? Row)[]
        {
            (1, Row(("Qty", "1"))),
            (2, Row(("Qty", raw)))
        };

        Assert.False(CopiesFieldContract.TryAuthorizePrepare("Qty", selected, out var issues));
        Assert.Equal(2, Assert.Single(issues).SourceRowNumber);
        Assert.False(string.IsNullOrWhiteSpace(issues[0].Message));
    }

    [Fact]
    public void TryAuthorizePrepare_AllowsBatchAfterInvalidRowIsUnselected()
    {
        var all = new (int SourceRowNumber, IReadOnlyDictionary<string, string>? Row)[]
        {
            (1, Row(("Qty", "2"))),
            (2, Row(("Qty", ""))),
            (3, Row(("Qty", "1")))
        };

        Assert.False(CopiesFieldContract.TryAuthorizePrepare("Qty", all, out _));

        // Operator unselects the bad row — remaining selection must authorize.
        var remaining = new[] { all[0], all[2] };
        Assert.True(CopiesFieldContract.TryAuthorizePrepare("Qty", remaining, out var issues));
        Assert.Empty(issues);
    }

    [Fact]
    public void TryAuthorizePrepare_SkipsGateWhenCopiesFieldNotConfigured()
    {
        var selected = new (int SourceRowNumber, IReadOnlyDictionary<string, string>? Row)[]
        {
            (1, Row(("Part", "A"))),
            (2, Row(("Part", "B")))
        };

        Assert.True(CopiesFieldContract.TryAuthorizePrepare(null, selected, out var issues));
        Assert.Empty(issues);
        Assert.True(CopiesFieldContract.TryAuthorizePrepare("  ", selected, out issues));
        Assert.Empty(issues);
    }

    [Fact]
    public void ExpandedZeroCopiesDoesNotAuthorizeSelectedInvalidRow()
    {
        // Simulates Print Preview tracking: invalid cell → Copies=0 expansion drop,
        // but the tracking row remains IsSelected. Prepare must still block.
        var trackingSelected = new (int SourceRowNumber, IReadOnlyDictionary<string, string>? Row)[]
        {
            (1, Row(("Qty", "2"))),
            (2, Row(("Qty", "nope")))
        };

        var expandedLabelCount = trackingSelected.Sum(entry =>
            DatabaseConfig.ResolveCopiesForRow("Qty", entry.Row));
        Assert.Equal(2, expandedLabelCount); // silent drop of row 2 from the sequence

        Assert.False(CopiesFieldContract.TryAuthorizePrepare("Qty", trackingSelected, out var issues));
        Assert.Contains(issues, issue => issue.SourceRowNumber == 2);
    }

    private static Dictionary<string, string> Row(params (string Key, string Value)[] cells)
    {
        var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in cells)
        {
            row[key] = value;
        }

        return row;
    }
}
