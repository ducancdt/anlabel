using ANLAbel.Core.Enums;
using ANLAbel.Core.Text;
using Xunit;

namespace ANLAbel.UnitTests;

public sealed class TextStyleAlignmentContractTests
{
    [Fact]
    public void HorizontalIcon_IsOnOnlyForTheMatchingMode()
    {
        Assert.True(TextStyleAlignmentContract.IsOn(TextAlignmentMode.Center, TextAlignmentMode.Center));
        Assert.False(TextStyleAlignmentContract.IsOn(TextAlignmentMode.Left, TextAlignmentMode.Center));
        Assert.False(TextStyleAlignmentContract.IsOn(TextAlignmentMode.Right, TextAlignmentMode.Justify));
    }

    [Fact]
    public void VerticalIcon_IsOnOnlyForTheMatchingMode()
    {
        Assert.True(TextStyleAlignmentContract.IsOn(TextVerticalAlignmentMode.Center, TextVerticalAlignmentMode.Center));
        Assert.False(TextStyleAlignmentContract.IsOn(TextVerticalAlignmentMode.Top, TextVerticalAlignmentMode.Bottom));
    }

    [Fact]
    public void Apply_TurnsTheClickedIconOn_AndIgnoresOff()
    {
        Assert.Equal(
            TextAlignmentMode.Right,
            TextStyleAlignmentContract.Apply(TextAlignmentMode.Left, TextAlignmentMode.Right, turnOn: true));
        Assert.Equal(
            TextAlignmentMode.Left,
            TextStyleAlignmentContract.Apply(TextAlignmentMode.Left, TextAlignmentMode.Right, turnOn: false));
        Assert.Equal(
            TextVerticalAlignmentMode.Bottom,
            TextStyleAlignmentContract.Apply(TextVerticalAlignmentMode.Top, TextVerticalAlignmentMode.Bottom, turnOn: true));
    }
}
