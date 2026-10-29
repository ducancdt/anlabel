using ANLAbel.Barcode.Options;
using ANLAbel.Barcode.Renderers;
using Xunit;

namespace ANLAbel.UnitTests;

/// <summary>
/// The native matrix grid used to seed imported PDF417 frames must come
/// from the shipped encoder path: deterministic for the same data and
/// options, sensitive to the error-correction level, and unavailable for
/// linear symbologies or unencodable data.
/// </summary>
public sealed class MatrixModuleCountTests
{
    private readonly ZxingBarcodeRenderer _renderer = new();

    [Fact]
    public void CountMatrixModules_Pdf417_IsDeterministicAndLevelSensitive()
    {
        var level2 = new BarcodeRenderOptions { Pdf417ErrorCorrectionLevel = 2 };
        var first = _renderer.CountMatrixModules("PDF-DATA-123", BarcodeType.Pdf417, level2);
        var again = _renderer.CountMatrixModules("PDF-DATA-123", BarcodeType.Pdf417, level2);
        Assert.NotNull(first);
        Assert.Equal(first, again);
        Assert.True(first!.Value.WidthModules > first.Value.HeightModules,
            "A short PDF417 payload must produce a symbol wider than it is tall.");

        var level5 = new BarcodeRenderOptions { Pdf417ErrorCorrectionLevel = 5 };
        var stronger = _renderer.CountMatrixModules("PDF-DATA-123", BarcodeType.Pdf417, level5);
        Assert.NotNull(stronger);
        Assert.True(
            stronger!.Value.WidthModules * stronger.Value.HeightModules
            > first.Value.WidthModules * first.Value.HeightModules,
            "A higher error-correction level must grow the native module grid.");
    }

    [Fact]
    public void CountMatrixModules_RejectsLinearTypesAndEmptyData()
    {
        Assert.Null(_renderer.CountMatrixModules("ABC123", BarcodeType.Code128));
        Assert.Null(_renderer.CountMatrixModules("", BarcodeType.Pdf417));
        Assert.Null(_renderer.CountMatrixModules("   ", BarcodeType.QRCode));
    }

    [Fact]
    public void CountMatrixModules_QrAndDataMatrix_ReturnSquareGrids()
    {
        var qr = _renderer.CountMatrixModules("HELLO", BarcodeType.QRCode);
        Assert.NotNull(qr);
        Assert.Equal(qr!.Value.WidthModules, qr.Value.HeightModules);

        var dm = _renderer.CountMatrixModules("HELLO", BarcodeType.DataMatrix);
        Assert.NotNull(dm);
        Assert.True(dm!.Value.WidthModules > 0 && dm.Value.HeightModules > 0);
    }
}
