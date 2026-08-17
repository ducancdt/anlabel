using ANLAbel.Core.Printing;
using Xunit;

namespace ANLAbel.UnitTests;

public sealed class EffectiveDpiContractTests
{
    [Theory]
    [InlineData(1, 1)]
    [InlineData(203, 203)]
    [InlineData(305, 609)]
    [InlineData(1200, 1200)]
    [InlineData(2400, 2400)]
    [InlineData(2400, 203)]
    [InlineData(203, 2400)]
    public void CommonSquareAndNonSquareDpiValuesAreValid(int dpiX, int dpiY)
    {
        var result = EffectiveDpiContract.Validate(dpiX, dpiY);

        Assert.True(result.IsValid);
        Assert.Equal(string.Empty, result.FailureCode);
        Assert.Equal(2400, EffectiveDpiContract.MaximumSupportedDpi);
    }

    [Theory]
    [InlineData(0, 203, "effective-dpi-non-positive")]
    [InlineData(203, 0, "effective-dpi-non-positive")]
    [InlineData(-1, 203, "effective-dpi-non-positive")]
    [InlineData(203, -1, "effective-dpi-non-positive")]
    [InlineData(0, 0, "effective-dpi-non-positive")]
    [InlineData(2401, 203, "effective-dpi-out-of-range")]
    [InlineData(203, 2401, "effective-dpi-out-of-range")]
    [InlineData(2401, 2401, "effective-dpi-out-of-range")]
    [InlineData(int.MaxValue, 300, "effective-dpi-out-of-range")]
    public void InvalidDpiFailsClosed(int dpiX, int dpiY, string expectedCode)
    {
        var result = EffectiveDpiContract.Validate(dpiX, dpiY);

        Assert.False(result.IsValid);
        Assert.Equal(expectedCode, result.FailureCode);
    }

    [Fact]
    public void NonPositiveIsCheckedBeforeRange()
    {
        var result = EffectiveDpiContract.Validate(-5, 5000);

        Assert.False(result.IsValid);
        Assert.Equal("effective-dpi-non-positive", result.FailureCode);
    }
}
