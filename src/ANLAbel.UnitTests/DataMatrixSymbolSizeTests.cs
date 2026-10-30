using ANLAbel.Barcode.Options;
using ANLAbel.Barcode.Renderers;
using ANLAbel.Core.Barcode;
using ANLAbel.Core.Enums;
using ANLAbel.Core.Models;
using Xunit;

namespace ANLAbel.UnitTests;

/// <summary>
/// NiceLabel 10 Data Matrix symbol size (vendor <c>DatamatrixFormat</c>). Its
/// automatic size is the smallest square symbol for the data; the barcode
/// library it ships considers rectangular sizes in automatic mode only when
/// <c>DataMatrixIncludeRectFormatsInAutoMode</c> is set, which NiceLabel leaves
/// at its default (false). A chosen size that cannot hold the data is an error.
/// </summary>
public sealed class DataMatrixSymbolSizeTests
{
    private readonly ZxingBarcodeRenderer _renderer = new();

    [Fact]
    public void AutomaticSizeIsTheSmallestSquareSymbol()
    {
        // With rectangular sizes allowed, some lengths fit 8x32 or 12x26 first
        // (8 letters came out 8x32 before this rule).
        foreach (var length in Enumerable.Range(1, 60))
        {
            var size = _renderer.CountMatrixModules(new string('A', length), BarcodeType.DataMatrix);
            Assert.NotNull(size);
            Assert.True(size!.Value.WidthModules == size.Value.HeightModules, $"{length} letters gave {size.Value.WidthModules} x {size.Value.HeightModules}");
        }
    }

    [Theory]
    [InlineData(DataMatrixSymbolSize.C24x24, 24, 24)]
    [InlineData(DataMatrixSymbolSize.C52x52, 52, 52)]
    [InlineData(DataMatrixSymbolSize.C8x18, 8, 18)]
    [InlineData(DataMatrixSymbolSize.C12x36, 12, 36)]
    [InlineData(DataMatrixSymbolSize.C16x48, 16, 48)]
    public void ChosenSizeIsExactlyThatSize(DataMatrixSymbolSize size, int rows, int columns)
    {
        var options = new BarcodeRenderOptions { DataMatrixSymbolSize = size, QuietZoneModules = 0 };
        Assert.Equal((columns, rows), _renderer.CountMatrixModules("12345", BarcodeType.DataMatrix, options));
    }

    [Fact]
    public void DataTooLongForTheChosenSizeDoesNotEncode()
    {
        const string data = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        Assert.Null(_renderer.CountMatrixModules(data, BarcodeType.DataMatrix, new BarcodeRenderOptions { DataMatrixSymbolSize = DataMatrixSymbolSize.C10x10 }));
        Assert.NotNull(_renderer.CountMatrixModules(data, BarcodeType.DataMatrix, new BarcodeRenderOptions()));
    }

    [Fact]
    public void SizesNameTheirCellsAndVendorFormatsMapToThem()
    {
        Assert.Equal(DataMatrixSymbolSize.C32x32, DataMatrixSymbolSizes.FromVendorFormat(10));
        Assert.Equal(DataMatrixSymbolSize.C52x52, DataMatrixSymbolSizes.FromVendorFormat(15));
        Assert.Equal(DataMatrixSymbolSize.C8x18, DataMatrixSymbolSizes.FromVendorFormat(25));
        Assert.Equal(DataMatrixSymbolSize.C16x48, DataMatrixSymbolSizes.FromVendorFormat(30));
        Assert.Equal(DataMatrixSymbolSize.Auto, DataMatrixSymbolSizes.FromVendorFormat(0));
        Assert.Equal(DataMatrixSymbolSize.Auto, DataMatrixSymbolSizes.FromVendorFormat(31));
        Assert.Equal((16, 48), DataMatrixSymbolSizes.Cells(DataMatrixSymbolSize.C16x48));
        Assert.Null(DataMatrixSymbolSizes.Cells(DataMatrixSymbolSize.Auto));
        Assert.True(DataMatrixSymbolSizes.IsRectangular(DataMatrixSymbolSize.C12x26));
        Assert.False(DataMatrixSymbolSizes.IsRectangular(DataMatrixSymbolSize.C144x144));
        foreach (var size in Enum.GetValues<DataMatrixSymbolSize>().Where(size => size != DataMatrixSymbolSize.Auto))
        {
            var cells = DataMatrixSymbolSizes.Cells(size);
            Assert.NotNull(cells);
            Assert.Equal(size, Enum.Parse<DataMatrixSymbolSize>($"C{cells!.Value.Rows}x{cells.Value.Columns}"));
        }
    }

    [Fact]
    public void ChosenSizeAppliesOnlyToDataMatrixAndKeepsTheFrameFromQrSizing()
    {
        var dataMatrix = new LabelObject { Type = ObjectType.BarcodeCode128, BarcodeSymbology = BarcodeSymbology.DataMatrix, WidthMm = 20, HeightMm = 20, Text = "12345" };
        var qr = new LabelObject { Type = ObjectType.BarcodeCode128, BarcodeSymbology = BarcodeSymbology.QRCode, DataMatrixSymbolSize = DataMatrixSymbolSize.C8x18 };
        Assert.False(DataMatrixSymbolSizes.HasChosenSize(dataMatrix));
        Assert.False(DataMatrixSymbolSizes.HasChosenSize(qr));
        dataMatrix.DataMatrixSymbolSize = DataMatrixSymbolSize.C12x36;
        Assert.True(DataMatrixSymbolSizes.HasChosenSize(dataMatrix));
        Assert.Null(QrObjectGeometryContract.ResolveTargetSizeMm(dataMatrix, "12345"));
        Assert.Equal(DataMatrixSymbolSize.C12x36, LabelObjectCloner.Clone(dataMatrix).DataMatrixSymbolSize);
    }
}
