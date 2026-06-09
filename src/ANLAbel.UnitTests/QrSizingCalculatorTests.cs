using ANLAbel.Core.Barcode;
using ANLAbel.Core.Enums;
using ANLAbel.Core.Models;
using Xunit;

namespace ANLAbel.UnitTests;

public sealed class QrSizingCalculatorTests
{
    private readonly BarcodeObjectBounds _bounds = new(10, 10, 20, 20);

    [Fact]
    public void AutoSizeByData_ShortData_UsesSmallestVersionAndSquareSize()
    {
        var calculator = new QrSizingCalculator();

        var result = calculator.Calculate("ABC123", _bounds, new QrBarcodeOptions
        {
            SizingMode = QrSizingMode.AutoSizeByData,
            ErrorCorrection = QrErrorCorrection.M,
            ModuleSizePx = 6,
            QuietZoneModules = 4,
            Dpi = 300
        });

        Assert.True(result.IsValid);
        Assert.Equal(1, result.Version);
        Assert.Equal(21, result.ModuleCount);
        Assert.Equal(29, result.TotalModules);
        Assert.Equal(174, result.FinalSizePx);
        Assert.Equal(result.FinalSizeMm, result.WidthMm, precision: 6);
        Assert.Equal(result.FinalSizeMm, result.HeightMm, precision: 6);
    }

    [Fact]
    public void AutoSizeByData_LongData_IncreasesVersion()
    {
        var calculator = new QrSizingCalculator();
        var data = new string('A', 100);

        var result = calculator.Calculate(data, _bounds, new QrBarcodeOptions
        {
            SizingMode = QrSizingMode.AutoSizeByData,
            ErrorCorrection = QrErrorCorrection.M,
            ModuleSizePx = 6,
            QuietZoneModules = 4,
            Dpi = 300
        });

        Assert.True(result.IsValid);
        Assert.True(result.Version > 1);
        Assert.Equal(QrVersionHelper.GetModuleCount(result.Version), result.ModuleCount);
    }

    [Fact]
    public void AutoSizeHelper_LongData_GrowsBeyondCurrentBounds()
    {
        var small = QrAutoSizeHelper.CalculateRequiredSizeMm("ABC123", 8, 8, QrErrorCorrection.M, 6, 4, 300);
        var large = QrAutoSizeHelper.CalculateRequiredSizeMm(new string('A', 100), 8, 8, QrErrorCorrection.M, 6, 4, 300);

        Assert.NotNull(small);
        Assert.NotNull(large);
        Assert.True(large > small);
        Assert.True(large > 8);
    }

    [Fact]
    public void AutoSizeHelper_LongData_ClampsToAvailableLabelSpace()
    {
        var size = QrAutoSizeHelper.CalculateRequiredSizeMm(new string('A', 2331), 8, 8, QrErrorCorrection.M, 6, 4, 300, maxSizeMm: 42);

        Assert.Equal(42, size);
    }

    [Fact]
    public void AutoSizeHelper_ShortData_UsesSmallReadableLabelRelativeSize()
    {
        var size = QrAutoSizeHelper.CalculateRequiredSizeMm("123456789012", 20, 20, QrErrorCorrection.M, 6, 4, 300, maxSizeMm: 42);

        Assert.InRange(size!.Value, 7, 8);
    }

    [Fact]
    public void FixedSizeHelper_HigherVersionAndModuleSizeGrowPredictably()
    {
        var small = QrAutoSizeHelper.CalculateFixedSizeMm(1, 3, 4, 300, 42);
        var medium = QrAutoSizeHelper.CalculateFixedSizeMm(10, 6, 4, 300, 42);
        var large = QrAutoSizeHelper.CalculateFixedSizeMm(20, 10, 4, 300, 42);

        Assert.True(small < medium);
        Assert.True(medium < large);
        Assert.Equal(42, large);
    }

    [Fact]
    public void LabelObject_QrTextChange_AutoGrowsObjectBounds()
    {
        var item = new LabelObject
        {
            Type = ObjectType.QRCode,
            QrSizingMode = QrSizingMode.AutoSizeByData,
            WidthMm = 8,
            HeightMm = 8
        };

        item.Text = new string('A', 100);

        Assert.True(item.WidthMm > 8);
        Assert.Equal(item.WidthMm, item.HeightMm);
    }

    [Fact]
    public void LabelObject_BarcodeObjectWithQrSymbology_TextChangeAutoGrowsObjectBounds()
    {
        var item = new LabelObject
        {
            Type = ObjectType.BarcodeCode128,
            BarcodeSymbology = BarcodeSymbology.QRCode,
            QrSizingMode = QrSizingMode.AutoSizeByData,
            WidthMm = 8,
            HeightMm = 8
        };

        item.Text = new string('A', 100);

        Assert.True(item.WidthMm > 8);
        Assert.Equal(item.WidthMm, item.HeightMm);
    }

    [Theory]
    [InlineData(BarcodeSymbology.DataMatrix)]
    [InlineData(BarcodeSymbology.Aztec)]
    [InlineData(BarcodeSymbology.Pdf417)]
    public void LabelObject_BarcodeObjectWithSquare2DSymbology_TextChangeAutoGrowsObjectBounds(BarcodeSymbology symbology)
    {
        var item = new LabelObject
        {
            Type = ObjectType.BarcodeCode128,
            BarcodeSymbology = symbology,
            QrSizingMode = QrSizingMode.AutoSizeByData,
            WidthMm = 8,
            HeightMm = 8
        };

        item.Text = new string('A', 100);

        Assert.True(item.WidthMm > 8);
        Assert.Equal(item.WidthMm, item.HeightMm);
    }

    [Fact]
    public void LabelObject_DataMatrixObject_TextChangeAutoGrowsObjectBounds()
    {
        var item = new LabelObject
        {
            Type = ObjectType.DataMatrix,
            QrSizingMode = QrSizingMode.AutoSizeByData,
            WidthMm = 8,
            HeightMm = 8
        };

        item.Text = new string('A', 100);

        Assert.True(item.WidthMm > 8);
        Assert.Equal(item.WidthMm, item.HeightMm);
    }

    [Fact]
    public void FixedVersion_DataTooLong_ReturnsErrorWithoutAutoGrowing()
    {
        var calculator = new QrSizingCalculator();
        var data = new string('A', 50);

        var result = calculator.Calculate(data, _bounds, new QrBarcodeOptions
        {
            SizingMode = QrSizingMode.FixedVersionAndModuleSize,
            ErrorCorrection = QrErrorCorrection.H,
            FixedVersion = 1,
            ModuleSizePx = 9,
            QuietZoneModules = 4,
            Dpi = 300
        });

        Assert.False(result.IsValid);
        Assert.Equal("Data is too long for selected QR version.", result.ErrorMessage);
    }

    [Fact]
    public void FixedVersion_ValidData_UsesSelectedVersion()
    {
        var calculator = new QrSizingCalculator();

        var result = calculator.Calculate("SHORT", _bounds, new QrBarcodeOptions
        {
            SizingMode = QrSizingMode.FixedVersionAndModuleSize,
            ErrorCorrection = QrErrorCorrection.Q,
            FixedVersion = 3,
            ModuleSizePx = 9,
            QuietZoneModules = 4,
            Dpi = 300
        });

        Assert.True(result.IsValid);
        Assert.Equal(3, result.Version);
        Assert.Equal(29, result.ModuleCount);
        Assert.Equal(37, result.TotalModules);
        Assert.Equal(333, result.FinalSizePx);
        Assert.Equal(result.WidthMm, result.HeightMm, precision: 6);
    }

    [Theory]
    [InlineData(0, 300, 4, "ModuleSizePx must be greater than 0.")]
    [InlineData(6, 0, 4, "Dpi must be greater than 0.")]
    [InlineData(6, 300, -1, "QuietZoneModules must be greater than or equal to 0.")]
    public void InvalidOptions_ReturnsError(int moduleSizePx, int dpi, int quietZoneModules, string expectedError)
    {
        var calculator = new QrSizingCalculator();

        var result = calculator.Calculate("ABC", _bounds, new QrBarcodeOptions
        {
            ModuleSizePx = moduleSizePx,
            Dpi = dpi,
            QuietZoneModules = quietZoneModules
        });

        Assert.False(result.IsValid);
        Assert.Equal(expectedError, result.ErrorMessage);
    }
}
