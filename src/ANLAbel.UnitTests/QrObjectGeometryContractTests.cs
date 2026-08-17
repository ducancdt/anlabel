using ANLAbel.Core.Barcode;
using ANLAbel.Core.Enums;
using ANLAbel.Core.Models;
using Xunit;

namespace ANLAbel.UnitTests;

public sealed class QrObjectGeometryContractTests
{
    [Fact]
    public void StaticAutoSizeUsesTheSharedTargetContract()
    {
        var item = new LabelObject
        {
            Type = ObjectType.QRCode,
            Text = "PART-001",
            QrSizingMode = QrSizingMode.AutoSizeByData,
            WidthMm = 8,
            HeightMm = 8
        };

        var target = QrObjectGeometryContract.ResolveTargetSizeMm(item, item.Text);

        Assert.NotNull(target);
        Assert.Equal(item.WidthMm, item.HeightMm);
        Assert.False(QrObjectGeometryContract.HasMeaningfulSizeDelta(item, item.WidthMm));
    }

    [Fact]
    public void BoundAutoSizeWaitsForResolvedDataButFixedSizeDoesNot()
    {
        var item = new LabelObject
        {
            Type = ObjectType.DataMatrix,
            BindingExpression = "{SKU}",
            QrSizingMode = QrSizingMode.AutoSizeByData
        };

        Assert.Null(QrObjectGeometryContract.ResolveTargetSizeMm(item, null));

        item.QrSizingMode = QrSizingMode.FixedVersionAndModuleSize;
        Assert.NotNull(QrObjectGeometryContract.ResolveTargetSizeMm(item, null));
    }

    [Fact]
    public void AvailableLabelSpaceIsAppliedByTheSameContract()
    {
        var item = new LabelObject
        {
            Type = ObjectType.QRCode,
            Text = new string('A', 2331),
            QrSizingMode = QrSizingMode.AutoSizeByData,
            WidthMm = 8,
            HeightMm = 8
        };

        var target = QrObjectGeometryContract.ResolveTargetSizeMm(item, item.Text, maxSizeMm: 14);

        Assert.Equal(14, target);
    }

    [Fact]
    public void ToleranceIsSymmetricAcrossWidthAndHeight()
    {
        var item = new LabelObject { Type = ObjectType.QRCode, WidthMm = 10, HeightMm = 10 };

        Assert.False(QrObjectGeometryContract.HasMeaningfulSizeDelta(item, 10.049));
        Assert.True(QrObjectGeometryContract.HasMeaningfulSizeDelta(item, 10.051));
        item.HeightMm = 10.2;
        Assert.True(QrObjectGeometryContract.HasMeaningfulSizeDelta(item, 10));
    }

    [Fact]
    public void NonSquareObjectNeverReceivesMatrixGeometry()
    {
        var item = new LabelObject
        {
            Type = ObjectType.BarcodeCode128,
            BarcodeSymbology = BarcodeSymbology.Code128,
            Text = "123"
        };

        Assert.Null(QrObjectGeometryContract.ResolveTargetSizeMm(item, item.Text));
    }
}
