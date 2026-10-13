using ANLAbel.Core.Models;
using ANLAbel.Core.Printing;
using Xunit;

namespace ANLAbel.UnitTests;

public sealed class DatabaseConfigCopiesTests
{
    [Fact]
    public void UnconfiguredCopiesFieldDefaultsToOne()
    {
        var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["Qty"] = "3" };

        Assert.True(DatabaseConfig.TryResolveCopiesForRow(null, row, out var copies, out var error));
        Assert.Equal(1, copies);
        Assert.Null(error);
        Assert.Equal(1, DatabaseConfig.ResolveCopiesForRow("  ", row));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("abc")]
    [InlineData("1.5")]
    [InlineData("-1")]
    [InlineData("1000")]
    public void ConfiguredCopiesFieldRejectsInvalidValues(string raw)
    {
        var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["Qty"] = raw };

        Assert.False(DatabaseConfig.TryResolveCopiesForRow("Qty", row, out var copies, out var error));
        Assert.Equal(0, copies);
        Assert.False(string.IsNullOrWhiteSpace(error));
        Assert.Equal(0, DatabaseConfig.ResolveCopiesForRow("Qty", row));
    }

    [Fact]
    public void ConfiguredCopiesFieldAcceptsZeroThroughLimit()
    {
        Assert.True(DatabaseConfig.TryResolveCopiesForRow(
            "Qty",
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["Qty"] = "0" },
            out var zero,
            out _));
        Assert.Equal(0, zero);

        Assert.True(DatabaseConfig.TryResolveCopiesForRow(
            "Qty",
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Qty"] = QuantityPlan.MaximumCopiesPerRecord.ToString()
            },
            out var max,
            out _));
        Assert.Equal(QuantityPlan.MaximumCopiesPerRecord, max);
    }

    [Fact]
    public void MissingColumnAndNullRowFailClosed()
    {
        var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["Part"] = "A" };

        Assert.False(DatabaseConfig.TryResolveCopiesForRow("Qty", row, out var missing, out var missingError));
        Assert.Equal(0, missing);
        Assert.Contains("missing", missingError, StringComparison.OrdinalIgnoreCase);

        Assert.False(DatabaseConfig.TryResolveCopiesForRow("Qty", null, out var nullRow, out var nullError));
        Assert.Equal(0, nullRow);
        Assert.Contains("missing", nullError, StringComparison.OrdinalIgnoreCase);
    }
}
