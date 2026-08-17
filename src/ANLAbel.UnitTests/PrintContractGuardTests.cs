using ANLAbel.Core.Printing;
using Xunit;

namespace ANLAbel.UnitTests;

public sealed class PrintContractGuardTests
{
    [Fact]
    public void EmptyExpectationKeepsDirectPrintCompatible()
    {
        Assert.True(PrintContractGuard.Matches(string.Empty, "effective"));
        Assert.True(PrintContractGuard.Matches(null, null));
    }

    [Fact]
    public void PreparedContractRequiresAnEffectiveHash()
    {
        Assert.False(PrintContractGuard.Matches("prepared", string.Empty));
        Assert.False(PrintContractGuard.Matches("prepared", null));
    }

    [Fact]
    public void ContractComparisonIsTrimmedAndCaseInsensitive()
    {
        Assert.True(PrintContractGuard.Matches("  ABC123 ", "abc123"));
        Assert.False(PrintContractGuard.Matches("abc123", "abc124"));
    }

    [Fact]
    public void PreparedDispatchRequiresVerifiedTicketEvidence()
    {
        Assert.False(PrintContractGuard.Matches("prepared", "prepared", actualTicketVerified: false));
        Assert.True(PrintContractGuard.Matches("prepared", "prepared", actualTicketVerified: true));
        Assert.True(PrintContractGuard.Matches(string.Empty, string.Empty, actualTicketVerified: false));
    }

    [Fact]
    public void DispatchSnapshotRequiresStableDocumentOutputAndEvidence()
    {
        Assert.True(PrintContractGuard.MatchesDispatchSnapshot(
            "doc-1", "OUTPUT-1", preparedTicketVerified: true,
            "doc-1", "output-1", finalTicketVerified: true));
        Assert.False(PrintContractGuard.MatchesDispatchSnapshot(
            "doc-1", "output-1", preparedTicketVerified: true,
            "doc-2", "output-1", finalTicketVerified: true));
        Assert.False(PrintContractGuard.MatchesDispatchSnapshot(
            "doc-1", "output-1", preparedTicketVerified: true,
            "doc-1", "output-2", finalTicketVerified: true));
        Assert.False(PrintContractGuard.MatchesDispatchSnapshot(
            "doc-1", "output-1", preparedTicketVerified: false,
            "doc-1", "output-1", finalTicketVerified: true));
        Assert.False(PrintContractGuard.MatchesDispatchSnapshot(
            "", "output-1", preparedTicketVerified: true,
            "doc-1", "output-1", finalTicketVerified: true));
    }
}
