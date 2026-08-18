using ANLAbel.Core.Printing;
using Xunit;

namespace ANLAbel.UnitTests;

public sealed class PrintScaleContractTests
{
    [Fact]
    public void IdentityAndUnsetAreAllowed()
    {
        Assert.True(PrintScaleContract.Evaluate(1, 1).IsAllowed);
        Assert.True(PrintScaleContract.Evaluate(0, 0).IsAllowed);
        Assert.True(PrintScaleContract.Evaluate(0.98, 1.02).IsAllowed);
        Assert.True(PrintScaleContract.Evaluate(0.5, 2.0).IsAllowed);
    }

    [Fact]
    public void IncompleteAndNonFiniteScalesFailClosed()
    {
        var incomplete = PrintScaleContract.Evaluate(1, 0);
        Assert.False(incomplete.IsAllowed);
        Assert.Contains("incomplete", incomplete.Diagnostic, StringComparison.OrdinalIgnoreCase);

        Assert.False(PrintScaleContract.Evaluate(0, 1.1).IsAllowed);
        Assert.False(PrintScaleContract.Evaluate(double.NaN, 1).IsAllowed);
        Assert.False(PrintScaleContract.Evaluate(1, double.NegativeInfinity).IsAllowed);
        Assert.False(PrintScaleContract.Evaluate(-1, 1).IsAllowed);
    }

    [Fact]
    public void FitToPageScaleIsRejected()
    {
        var shrink = PrintScaleContract.Evaluate(0.25, 0.25);
        Assert.False(shrink.IsAllowed);
        Assert.Contains("fit-to-page", shrink.Diagnostic, StringComparison.OrdinalIgnoreCase);

        var enlarge = PrintScaleContract.Evaluate(4, 4);
        Assert.False(enlarge.IsAllowed);
        Assert.Contains("0.5", enlarge.Diagnostic, StringComparison.Ordinal);
    }
}
