using ANLAbel.Core.Models;
using ANLAbel.Core.Printing;
using Xunit;

namespace ANLAbel.UnitTests;

public sealed class QuickPrintQuantityTests
{
    [Fact]
    public void TryExpand_BlocksInvalidDatabaseCopiesFieldOnItsSourceRow()
    {
        var database = new DatabaseConfig { CopiesField = "Qty" };
        var rows = new (int SourceRowNumber, IReadOnlyDictionary<string, string>? Row)[]
        {
            (4, new Dictionary<string, string> { ["Part"] = "A", ["Qty"] = "2" }),
            (7, new Dictionary<string, string> { ["Part"] = "B", ["Qty"] = "" })
        };

        var authorized = QuickPrintQuantity.TryExpand(database, fallbackCopies: 1, rows, out var expanded, out var error);

        Assert.False(authorized);
        Assert.Empty(expanded);
        Assert.Contains("Row 7", error, StringComparison.Ordinal);
        Assert.DoesNotContain("Row 1", error, StringComparison.Ordinal);
        Assert.Contains("blank", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TryExpand_CurrentRowKeepsTheCallerSourceRowNumber()
    {
        var database = new DatabaseConfig { CopiesField = "Qty" };
        var rows = new (int SourceRowNumber, IReadOnlyDictionary<string, string>? Row)[]
        {
            (5, new Dictionary<string, string> { ["Part"] = "B", ["Qty"] = "" })
        };

        var authorized = QuickPrintQuantity.TryExpand(database, fallbackCopies: 1, rows, out var expanded, out var error);

        Assert.False(authorized);
        Assert.Empty(expanded);
        Assert.Contains("Row 5", error, StringComparison.Ordinal);
        Assert.DoesNotContain("Row 1", error, StringComparison.Ordinal);
        Assert.Contains("blank", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TryExpand_DoesNotCapOverflowToTheMaximum()
    {
        var database = new DatabaseConfig { CopiesField = "Qty" };
        var rows = new (int SourceRowNumber, IReadOnlyDictionary<string, string>? Row)[]
        {
            (3, new Dictionary<string, string> { ["Qty"] = "5000" })
        };

        var authorized = QuickPrintQuantity.TryExpand(database, 1, rows, out var expanded, out var error);

        Assert.False(authorized);
        Assert.Empty(expanded);
        Assert.Contains("Row 3", error, StringComparison.Ordinal);
        Assert.Contains("exceeds", error, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("5000", error, StringComparison.Ordinal);
    }

    [Fact]
    public void TryExpand_RepeatsValidFieldCopiesWithoutFallback()
    {
        var database = new DatabaseConfig { CopiesField = "Qty" };
        var rows = new (int SourceRowNumber, IReadOnlyDictionary<string, string>? Row)[]
        {
            (1, new Dictionary<string, string> { ["Part"] = "A", ["Qty"] = "2" }),
            (2, new Dictionary<string, string> { ["Part"] = "B", ["Qty"] = "1" })
        };

        Assert.True(QuickPrintQuantity.TryExpand(database, fallbackCopies: 9, rows, out var expanded, out var error));
        Assert.Equal(string.Empty, error);
        Assert.Equal(new[] { "A", "A", "B" }, expanded.Select(row => row!["Part"]));
    }

    [Fact]
    public void TryExpand_RejectsManualCopyCountAboveTheLimit()
    {
        var database = new DatabaseConfig();
        var rows = new (int SourceRowNumber, IReadOnlyDictionary<string, string>? Row)[]
        {
            (1, new Dictionary<string, string> { ["Part"] = "A" })
        };

        Assert.False(QuickPrintQuantity.TryExpand(database, QuantityPlan.MaximumCopiesPerRecord + 1, rows, out var expanded, out var error));
        Assert.Empty(expanded);
        Assert.Contains(QuantityPlan.MaximumCopiesPerRecord.ToString(), error, StringComparison.Ordinal);
    }

    [Fact]
    public void TryExpand_UnconfiguredDatabaseFieldIgnoresABlankQtyColumn()
    {
        var database = new DatabaseConfig { CopiesField = "  " };
        var rows = new (int SourceRowNumber, IReadOnlyDictionary<string, string>? Row)[]
        {
            (2, new Dictionary<string, string> { ["Part"] = "A", ["Qty"] = "" })
        };

        Assert.True(QuickPrintQuantity.TryExpand(database, fallbackCopies: 3, rows, out var expanded, out var error));
        Assert.Equal(string.Empty, error);
        Assert.Equal(3, expanded.Count);
        Assert.All(expanded, row => Assert.Equal("A", row!["Part"]));
    }
}
