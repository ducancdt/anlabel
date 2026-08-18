using ANLAbel.Core.Printing;
using Xunit;

namespace ANLAbel.UnitTests;

public sealed class IndustrialPrintDpiContractTests
{
    [Theory]
    [InlineData(203)]
    [InlineData(300)]
    [InlineData(305)]
    [InlineData(600)]
    [InlineData(609)]
    [InlineData(152)]
    [InlineData(1200)]
    public void KnownThermalDpiIsAllowed(int dpi)
    {
        Assert.True(IndustrialPrintDpiContract.IsIndustrialDpi(dpi));
        Assert.True(IndustrialPrintDpiContract.Evaluate(dpi, 203).IsAllowed);
    }

    [Theory]
    [InlineData(72)]
    [InlineData(96)]
    [InlineData(150)]
    public void OfficeDpiIsRejected(int dpi)
    {
        Assert.True(IndustrialPrintDpiContract.IsOfficeDpi(dpi));
        var decision = IndustrialPrintDpiContract.Evaluate(dpi, dpi);
        Assert.False(decision.IsAllowed);
        Assert.Contains("office/screen", decision.Diagnostic, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void NonPositiveAndUnknownDpiFailClosed()
    {
        Assert.False(IndustrialPrintDpiContract.Evaluate(0, 0).IsAllowed);
        Assert.False(IndustrialPrintDpiContract.Evaluate(-203, 0).IsAllowed);
        var unknown = IndustrialPrintDpiContract.Evaluate(360, 360);
        Assert.False(unknown.IsAllowed);
        Assert.Contains("360", unknown.Diagnostic, StringComparison.Ordinal);
        Assert.True(IndustrialPrintDpiContract.Evaluate(0, 203).IsAllowed);
    }
}
