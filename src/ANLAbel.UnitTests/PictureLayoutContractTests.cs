using ANLAbel.Core.Enums;
using ANLAbel.Core.Models;
using ANLAbel.Core.Printing;
using ANLAbel.Core.Scene;
using Xunit;

namespace ANLAbel.UnitTests;

/// <summary>NiceLabel 10 picture Resize options: proportional, stretch, original size.</summary>
public sealed class PictureLayoutContractTests
{
    [Fact]
    public void ProportionalCentresTheWholePicture()
    {
        // A 40 x 20 px picture in a 30 x 30 mm frame: 30 x 15 mm, centred.
        var placed = PictureLayoutContract.Place(PictureSizing.Proportional, 0, 0, 30, 30, 40, 20, 96, 96, 25.4);
        Assert.Equal(0, placed.X, 6);
        Assert.Equal(7.5, placed.Y, 6);
        Assert.Equal(30, placed.Width, 6);
        Assert.Equal(15, placed.Height, 6);
    }

    [Fact]
    public void StretchFillsTheFrame()
    {
        Assert.Equal((2.0, 3.0, 30.0, 30.0), PictureLayoutContract.Place(PictureSizing.Stretch, 2, 3, 30, 30, 40, 20, 96, 96, 25.4));
    }

    [Fact]
    public void OriginalSizeComesFromPixelsAndResolution()
    {
        var placed = PictureLayoutContract.Place(PictureSizing.Original, 2, 3, 30, 30, 40, 20, 96, 96, 25.4);
        Assert.Equal((2.0, 3.0), (placed.X, placed.Y));
        Assert.Equal(40 / 96.0 * 25.4, placed.Width, 6);
        Assert.Equal(20 / 96.0 * 25.4, placed.Height, 6);
        // A 203 dpi thermal bitmap keeps its dot size; a file without a resolution counts as 96 dpi.
        Assert.Equal(406 / 203.0 * 25.4, PictureLayoutContract.OriginalSizeMm(406, 203, 203, 203).WidthMm, 6);
        Assert.Equal(96 / 96.0 * 25.4, PictureLayoutContract.OriginalSizeMm(96, 96, 0, double.NaN).HeightMm, 6);
    }

    [Fact]
    public void PictureOptionsAreClonedAndOnlyNewOptionsChangeTheFingerprint()
    {
        var template = new LabelTemplate { Name = "Pictures", WidthMm = 50, HeightMm = 30 };
        var picture = new LabelObject { Type = ObjectType.Image, Name = "Logo", WidthMm = 20, HeightMm = 10 };
        template.Objects.Add(picture);
        var before = DocumentSnapshot.Capture(template).DocumentHash;
        picture.PictureSizing = PictureSizing.Proportional;
        Assert.Equal(before, DocumentSnapshot.Capture(template).DocumentHash);

        picture.PictureSizing = PictureSizing.Stretch;
        picture.PictureFlipHorizontal = true;
        var after = DocumentSnapshot.Capture(template).DocumentHash;
        Assert.NotEqual(before, after);

        var copy = LabelObjectCloner.Clone(picture);
        Assert.Equal((PictureSizing.Stretch, true, false), (copy.PictureSizing, copy.PictureFlipHorizontal, copy.PictureFlipVertical));
    }
}
