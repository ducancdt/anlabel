using System.Globalization;
using ANLAbel.Core.Enums;
using ANLAbel.Core.Models;
using ANLAbel.Core.Text;
using Xunit;

namespace ANLAbel.UnitTests;

public sealed class TextLayoutContractTests
{
    [Fact]
    public void Capture_NormalizesNewlinesAndKeepsGraphemeClustersIntact()
    {
        var snapshot = TextLayoutContract.Capture("A\r\n👩‍🔬e\u0301\rB");

        Assert.Equal("A\n👩‍🔬e\u0301\nB", snapshot.Text);
        Assert.Contains("👩‍🔬", snapshot.GraphemeClusters);
        Assert.Contains("e\u0301", snapshot.GraphemeClusters);
        Assert.DoesNotContain(snapshot.GraphemeClusters, cluster => cluster == "\u0301");
        Assert.Equal(TextDirectionMode.LeftToRight, snapshot.ResolvedDirection);
        Assert.NotEmpty(snapshot.ContentHash);
    }

    [Fact]
    public void WrapGraphemes_UsesCompleteClustersAndExplicitNewlines()
    {
        var widths = new Dictionary<string, double>(StringComparer.Ordinal)
        {
            ["A"] = 1,
            ["👩‍🔬"] = 2,
            ["e\u0301"] = 1,
            ["B"] = 1
        };

        var wrapped = TextLayoutContract.WrapGraphemes(
            "A👩‍🔬e\u0301B",
            width: 2,
            line => TextLayoutContract.SegmentGraphemes(line)
                .Sum(cluster => widths.TryGetValue(cluster, out var value) ? value : 0));

        Assert.Equal("A\n👩‍🔬\ne\u0301B", wrapped);
        var emojiCluster = TextLayoutContract.SegmentGraphemes(wrapped)
            .Single(cluster => cluster.Contains("👩", StringComparison.Ordinal));
        Assert.DoesNotContain('\n', emojiCluster);
    }

    [Theory]
    [InlineData(TextDirectionMode.Auto, "123-ABC", TextDirectionMode.LeftToRight)]
    [InlineData(TextDirectionMode.Auto, "123-עברית", TextDirectionMode.RightToLeft)]
    [InlineData(TextDirectionMode.LeftToRight, "עברית", TextDirectionMode.LeftToRight)]
    [InlineData(TextDirectionMode.RightToLeft, "ABC", TextDirectionMode.RightToLeft)]
    public void ResolveDirection_IsDeterministicForMixedLabelValues(
        TextDirectionMode requested,
        string value,
        TextDirectionMode expected)
    {
        Assert.Equal(expected, TextLayoutContract.ResolveDirection(requested, value));
    }

    [Fact]
    public void LineHeightAndVerticalOffsetFollowPersistedPolicy()
    {
        Assert.Equal(16, TextLayoutContract.ResolveLineHeightDip(12, 12), precision: 6);
        Assert.Equal(12, TextLayoutContract.ResolveLineHeightDip(12, 0), precision: 6);
        Assert.Equal(4, TextLayoutContract.ResolveVerticalOffset(TextVerticalAlignmentMode.Bottom, 8, 12, true), precision: 6);
        Assert.Equal(2, TextLayoutContract.ResolveVerticalOffset(TextVerticalAlignmentMode.Center, 8, 12, true), precision: 6);
        Assert.Equal(0, TextLayoutContract.ResolveVerticalOffset(null, 8, 12, true), precision: 6);
    }

    [Fact]
    public void TextResourceContract_IsCanonicalAndChangesWithPresentationPolicy()
    {
        var first = TextResourceContract.Describe(
            "  Arial   ",
            fontSizePt: 10,
            bold: false,
            italic: false,
            underline: false,
            direction: TextDirectionMode.Auto,
            lineHeightPt: 0);
        var equivalent = TextResourceContract.Describe(
            "ARIAL",
            fontSizePt: 10,
            bold: false,
            italic: false,
            underline: false,
            direction: TextDirectionMode.Auto,
            lineHeightPt: 0);
        var changed = TextResourceContract.Describe(
            "Arial",
            fontSizePt: 10,
            bold: true,
            italic: false,
            underline: false,
            direction: TextDirectionMode.Auto,
            lineHeightPt: 0);

        Assert.Equal("ARIAL", first.CanonicalFamily);
        Assert.Equal(first.Fingerprint, equivalent.Fingerprint);
        Assert.NotEqual(first.Fingerprint, changed.Fingerprint);
        Assert.Equal(TextResourceContract.BaselineFallbackFamily, first.BaselineFallbackFamily);
        Assert.Equal(TextResourceContract.ContractVersion, first.ContractVersion);
    }

    [Fact]
    public void TextResourceContract_DescribesObjectStyleWithoutPlatformClaims()
    {
        var style = new ObjectStyle
        {
            FontFamily = "Bahnschrift",
            FontSizePt = 12,
            Bold = true,
            Italic = true,
            TextDirection = TextDirectionMode.RightToLeft,
            LineHeightPt = 16
        };

        var descriptor = TextResourceContract.Describe(style);

        Assert.Equal("BAHNSCHRIFT", descriptor.CanonicalFamily);
        Assert.True(descriptor.Bold);
        Assert.True(descriptor.Italic);
        Assert.Equal(TextDirectionMode.RightToLeft, descriptor.Direction);
        Assert.Equal(16, descriptor.LineHeightPt, precision: 6);
        Assert.Matches("^[0-9A-F]{64}$", descriptor.Fingerprint);
    }

    [Fact]
    public void TextLayoutIdentityContract_IsStableAndCultureIndependent()
    {
        var input = new TextLayoutIdentityInput(
            TextHash: "TEXT-HASH",
            TextResourceFingerprint: "RESOURCE-HASH",
            ResolvedDirection: TextDirectionMode.RightToLeft,
            ConstrainToBox: true,
            PixelsPerDip: 1.25,
            FrameWidthDip: 120,
            FrameHeightDip: 40,
            WidthDip: 96.5,
            HeightDip: 28,
            InkExtentDip: 22,
            BaselineDip: 14.25,
            LineHeightDip: 16,
            LineCount: 2,
            ContentWidthDip: 120,
            VerticalOffsetDip: 3,
            IsOverflowing: false);

        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");
            var first = TextLayoutIdentityContract.ComputeFingerprint(input);
            CultureInfo.CurrentCulture = new CultureInfo("ar-SA");
            var second = TextLayoutIdentityContract.ComputeFingerprint(input);
            Assert.Equal(first, second);
            Assert.Matches("^[0-9A-F]{64}$", first);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Fact]
    public void TextLayoutIdentityContract_ChangesWhenMetricsOrPolicyChange()
    {
        var baseline = new TextLayoutIdentityInput(
            "TEXT", "RESOURCE", TextDirectionMode.LeftToRight, true,
            1, 100, 30, 80, 20, 16, 12, 14, 1, 100, 0, false);
        var changedBaseline = baseline with { BaselineDip = 13 };
        var changedPolicy = baseline with { ConstrainToBox = false };

        var first = TextLayoutIdentityContract.ComputeFingerprint(baseline);
        Assert.NotEqual(first, TextLayoutIdentityContract.ComputeFingerprint(changedBaseline));
        Assert.NotEqual(first, TextLayoutIdentityContract.ComputeFingerprint(changedPolicy));
    }
}
