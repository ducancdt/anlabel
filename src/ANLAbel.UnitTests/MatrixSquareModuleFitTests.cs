using ANLAbel.Core.Barcode;
using Xunit;

namespace ANLAbel.UnitTests;

public sealed class MatrixSquareModuleFitTests
{
    [Fact]
    public void SquareDpiKeepsEqualModuleDots()
    {
        var layout = MatrixSquareModuleFit.Fit(25, 25, 472, 472, 300, 300);

        Assert.Equal(25, layout.NativeWidth);
        Assert.Equal(25, layout.NativeHeight);
        Assert.Equal(472, layout.FrameWidth);
        Assert.Equal(472, layout.FrameHeight);
        Assert.Equal(layout.ModuleDotsX, layout.ModuleDotsY);
        Assert.Equal(layout.NativeWidth * layout.ModuleDotsX, layout.FittedWidth);
        Assert.Equal(layout.NativeHeight * layout.ModuleDotsY, layout.FittedHeight);
        Assert.True(layout.FittedWidth <= layout.FrameWidth);
        Assert.True(layout.FittedHeight <= layout.FrameHeight);
        Assert.Equal(layout.FrameWidth - layout.FittedWidth, layout.LeftoverX);
        Assert.Equal(layout.LeftoverX / 2, layout.PadLeft);
        Assert.True(layout.PadLeft < layout.ModuleDotsX);
        Assert.True(layout.PadTop < layout.ModuleDotsY);
        Assert.True(layout.LeftoverX / 2.0 < layout.ModuleDotsX);
        Assert.True(layout.LeftoverY / 2.0 < layout.ModuleDotsY);
    }

    [Fact]
    public void NonSquareFrameDoesNotStretchModules()
    {
        var layout = MatrixSquareModuleFit.Fit(25, 25, 472, 330, 300, 300);

        Assert.Equal(layout.ModuleDotsX, layout.ModuleDotsY);
        Assert.Equal(layout.FittedWidth, layout.FittedHeight);
        Assert.True(layout.FittedHeight <= layout.FrameHeight);
        Assert.True(layout.FittedWidth <= layout.FrameWidth);
        Assert.True(layout.PadTop < layout.ModuleDotsY);
        Assert.Equal(layout.LeftoverY / 2, layout.PadTop);
    }

    [Fact]
    public void NonSquareDpiKeepsPhysicalModuleSquare()
    {
        var layout = MatrixSquareModuleFit.Fit(25, 25, 240, 480, 300, 600);

        var mmX = layout.ModuleDotsX / 300.0 * 25.4;
        var mmY = layout.ModuleDotsY / 600.0 * 25.4;
        Assert.InRange(Math.Abs(mmX - mmY), 0, 25.4 / 300.0 + 1e-6);
        Assert.True(layout.ModuleDotsY >= layout.ModuleDotsX);
        Assert.True(layout.ModuleDotsX >= 1);
        Assert.True(layout.ModuleDotsY >= 1);
    }

    [Fact]
    public void NarrowFrameIsLimitedByWidthNotHeight()
    {
        var layout = MatrixSquareModuleFit.Fit(25, 25, 200, 500, 300, 300);

        Assert.Equal(layout.ModuleDotsX, layout.ModuleDotsY);
        Assert.True(layout.FittedWidth <= 200);
        Assert.True(layout.FittedWidth < layout.FittedHeight || layout.FittedHeight <= 200);
        Assert.True(layout.FittedWidth <= layout.FrameWidth);
        Assert.True(layout.PadLeft < layout.ModuleDotsX || layout.ModuleDotsX == 1);
        var wide = MatrixSquareModuleFit.Fit(25, 25, 500, 500, 300, 300);
        Assert.True(layout.ModuleDotsX < wide.ModuleDotsX);
    }

    [Fact]
    public void TightFrameStillProducesAtLeastOneDotPerModule()
    {
        var layout = MatrixSquareModuleFit.Fit(25, 25, 10, 10, 203, 203);

        Assert.Equal(1, layout.ModuleDotsX);
        Assert.Equal(1, layout.ModuleDotsY);
        Assert.Equal(layout.NativeWidth, layout.FittedWidth);
        Assert.Equal(10, layout.FrameWidth);
    }

    [Theory]
    [InlineData(0, 25, 100, 100, 300, 300)]
    [InlineData(-1, 25, 100, 100, 300, 300)]
    [InlineData(25, 0, 100, 100, 300, 300)]
    [InlineData(25, 25, 0, 100, 300, 300)]
    [InlineData(25, 25, 100, 0, 300, 300)]
    [InlineData(25, 25, -4, 100, 300, 300)]
    [InlineData(25, 25, 100, 100, 0, 300)]
    [InlineData(25, 25, 100, 100, 300, 0)]
    [InlineData(25, 25, 100, 100, -203, 300)]
    public void Fit_RejectsNonPositiveNativeFrameOrDpi(
        int nativeWidth,
        int nativeHeight,
        int frameWidth,
        int frameHeight,
        int dpiX,
        int dpiY)
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            MatrixSquareModuleFit.Fit(nativeWidth, nativeHeight, frameWidth, frameHeight, dpiX, dpiY));
        Assert.Contains("must be", ex.Message, StringComparison.OrdinalIgnoreCase);
    }
}
