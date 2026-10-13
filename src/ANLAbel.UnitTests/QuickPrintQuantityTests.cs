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
    public void TryExpand_RefusesAJobAboveThePreviewLabelLimit()
    {
        // Audit 2026-10-13 (DB08): quick print must refuse what Print Preview refuses.
        var database = new DatabaseConfig { CopiesField = "Qty" };
        var rows = Enumerable.Range(1, 101)
            .Select(row => (row, (IReadOnlyDictionary<string, string>?)new Dictionary<string, string> { ["Qty"] = "999" }))
            .ToArray();

        Assert.False(QuickPrintQuantity.TryExpand(database, fallbackCopies: 1, rows, out var expanded, out var error));
        Assert.Empty(expanded);
        Assert.Contains("cannot exceed 100000 labels", error, StringComparison.Ordinal);

        Assert.False(QuickPrintQuantity.TryExpand(null, fallbackCopies: 999, rows, out _, out var manualError));
        Assert.Contains("cannot exceed 100000 labels", manualError, StringComparison.Ordinal);
    }

    [Fact]
    public void TryExpand_KeepsRecordAndCopyIdentityInPrintOrder()
    {
        // P04: A×2, B×1 → A1, A2, B1 — the same order the manifest records.
        var database = new DatabaseConfig { CopiesField = "Qty" };
        var rows = new (int SourceRowNumber, IReadOnlyDictionary<string, string>? Row)[]
        {
            (4, new Dictionary<string, string> { ["Part"] = "A", ["Qty"] = "2" }),
            (7, new Dictionary<string, string> { ["Part"] = "B", ["Qty"] = "1" })
        };

        Assert.True(QuickPrintQuantity.TryExpand(database, fallbackCopies: 1, rows, out var expanded, out var sequence, out var error));
        Assert.Equal(string.Empty, error);
        Assert.Equal(
            new[] { new PrintDispatchCopy(4, 1, 1), new PrintDispatchCopy(4, 2, 2), new PrintDispatchCopy(7, 1, 3) },
            sequence);
        Assert.Equal(new[] { "A", "A", "B" }, expanded.Select(row => row!["Part"]));

        Assert.True(QuickPrintQuantity.TryExpand(null, fallbackCopies: 2, rows, out _, out var manualSequence, out _));
        Assert.Equal(
            new[] { new PrintDispatchCopy(4, 1, 1), new PrintDispatchCopy(4, 2, 2), new PrintDispatchCopy(7, 1, 3), new PrintDispatchCopy(7, 2, 4) },
            manualSequence);
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
