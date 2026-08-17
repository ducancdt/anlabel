using ANLAbel.Core.Printing;
using Xunit;

namespace ANLAbel.UnitTests;

public sealed class PrintableAreaContractTests
{
    [Fact]
    public void ValidAreaContainedByMediaIsVerified()
    {
        var result = PrintableAreaContract.Validate(2, 3, 370, 180, 400, 200);

        Assert.True(result.HasUsableGeometry);
        Assert.True(result.IsVerified);
        Assert.Equal(string.Empty, result.FailureCode);
        Assert.Equal("verified", result.UserFacingMessage);
        Assert.Equal("verified:", result.ToString());
        Assert.Equal(1.0, PrintableAreaContract.BoundaryToleranceDip);
    }

    [Theory]
    [InlineData(-2, 3, 370, 180, 400, 200, "imageable-area-negative-origin", false)]
    [InlineData(2, -2, 370, 180, 400, 200, "imageable-area-negative-origin", false)]
    [InlineData(-1.01, 3, 370, 180, 400, 200, "imageable-area-negative-origin", false)]
    [InlineData(2, -1.01, 370, 180, 400, 200, "imageable-area-negative-origin", false)]
    [InlineData(2, 3, 0, 180, 400, 200, "imageable-area-non-positive-extent", false)]
    [InlineData(2, 3, 370, 0, 400, 200, "imageable-area-non-positive-extent", false)]
    [InlineData(2, 3, -1, 180, 400, 200, "imageable-area-non-positive-extent", false)]
    [InlineData(2, 3, 370, -1, 400, 200, "imageable-area-non-positive-extent", false)]
    [InlineData(2, 3, 401, 180, 400, 200, "imageable-area-outside-media", false)]
    [InlineData(2, 3, 370, 198.01, 400, 200, "imageable-area-outside-media", false)]
    public void InvalidGeometryFailsClosedOnEachAxis(
        double originX,
        double originY,
        double width,
        double height,
        double mediaWidth,
        double mediaHeight,
        string expectedCode,
        bool expectedUsable)
    {
        var result = PrintableAreaContract.Validate(originX, originY, width, height, mediaWidth, mediaHeight);

        Assert.Equal(expectedUsable, result.HasUsableGeometry);
        Assert.False(result.IsVerified);
        Assert.Equal(expectedCode, result.FailureCode);
        Assert.Equal(expectedCode.Replace('-', ' '), result.UserFacingMessage);
        Assert.Equal($"invalid:{expectedCode}", result.ToString());
    }

    [Theory]
    [InlineData(double.NaN, 0, 100, 100, 200, 200)]
    [InlineData(0, double.NaN, 100, 100, 200, 200)]
    [InlineData(0, 0, double.PositiveInfinity, 100, 200, 200)]
    [InlineData(0, 0, 100, double.NegativeInfinity, 200, 200)]
    public void NonFiniteAreaFailsClosed(
        double originX,
        double originY,
        double width,
        double height,
        double mediaWidth,
        double mediaHeight)
    {
        var result = PrintableAreaContract.Validate(originX, originY, width, height, mediaWidth, mediaHeight);

        Assert.False(result.HasUsableGeometry);
        Assert.False(result.IsVerified);
        Assert.Equal("imageable-area-non-finite", result.FailureCode);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData(400.0, null)]
    [InlineData(null, 200.0)]
    [InlineData(0.0, 200.0)]
    [InlineData(400.0, 0.0)]
    [InlineData(-1.0, 200.0)]
    [InlineData(400.0, -1.0)]
    [InlineData(double.NaN, 200.0)]
    [InlineData(400.0, double.PositiveInfinity)]
    public void MissingOrInvalidMediaNeverCertifiesContainment(double? mediaWidth, double? mediaHeight)
    {
        var result = PrintableAreaContract.Validate(2, 3, 370, 180, mediaWidth, mediaHeight);

        Assert.True(result.HasUsableGeometry);
        Assert.False(result.IsVerified);
        Assert.Equal("imageable-area-media-unverified", result.FailureCode);
        Assert.Equal("imageable area media unverified", result.UserFacingMessage);
        Assert.Equal("usable:imageable-area-media-unverified", result.ToString());
    }

    [Fact]
    public void OverflowingExtentFailsClosed()
    {
        var result = PrintableAreaContract.Validate(double.MaxValue, 0, double.MaxValue, 10, 400, 200);

        Assert.False(result.HasUsableGeometry);
        Assert.False(result.IsVerified);
        Assert.Equal("imageable-area-overflow", result.FailureCode);
    }

    [Theory]
    [InlineData(-1.0, 0, 400, 200)]
    [InlineData(0, -1.0, 400, 200)]
    [InlineData(-0.5, 0, 400, 200)]
    [InlineData(0, -0.5, 400, 200)]
    [InlineData(0, 0, 401, 200)]
    [InlineData(0, 0, 400, 201)]
    [InlineData(0.5, 0.5, 399.0, 199.0)]
    public void BoundaryRoundingWithinToleranceRemainsVerified(
        double originX,
        double originY,
        double width,
        double height)
    {
        var result = PrintableAreaContract.Validate(originX, originY, width, height, 400, 200);

        Assert.True(result.HasUsableGeometry);
        Assert.True(result.IsVerified);
        Assert.Equal(string.Empty, result.FailureCode);
        Assert.Equal("verified", result.UserFacingMessage);
    }

    [Theory]
    [InlineData(true, "")]
    [InlineData(true, "   ")]
    [InlineData(false, "")]
    [InlineData(false, "\t")]
    public void EmptyFailureCodeIsReportedAsUnverified(bool hasUsableGeometry, string failureCode)
    {
        var result = new PrintableAreaValidation(hasUsableGeometry, IsVerified: false, failureCode);

        Assert.False(result.IsVerified);
        Assert.Equal("unverified", result.UserFacingMessage);
        Assert.Equal($"{(hasUsableGeometry ? "usable" : "invalid")}:{failureCode}", result.ToString());
    }
}
