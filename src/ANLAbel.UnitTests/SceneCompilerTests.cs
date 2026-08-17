using ANLAbel.Core.Enums;
using ANLAbel.Core.Models;
using ANLAbel.Core.Scene;
using Xunit;

namespace ANLAbel.UnitTests;

public sealed class SceneCompilerTests
{
    [Fact]
    public void Capture_IsolatedFromMutableModelAndPreservesDocumentHash()
    {
        var template = CreateTemplate();
        var snapshot = DocumentSnapshot.Capture(template);
        var hash = snapshot.DocumentHash;

        template.Objects[0].XMm = 77;
        template.Objects[0].Style.FontFamily = "Consolas";
        template.Name = "Changed after capture";

        Assert.Equal(hash, snapshot.DocumentHash);
        Assert.Equal(4, snapshot.Objects[0].XMm, precision: 2);
        Assert.Equal("Arial", snapshot.Objects[0].Style.FontFamily);
        Assert.Equal("Scene fixture", snapshot.Name);
    }

    [Fact]
    public void Compile_IsDeterministicWhenCollectionOrderChanges()
    {
        var first = CreateTemplate();
        var second = CreateTemplate();
        second.Objects.Clear();
        second.Objects.Add(first.Objects[1]);
        second.Objects.Add(first.Objects[0]);

        var firstScene = SceneCompiler.Compile(DocumentSnapshot.Capture(first));
        var secondScene = SceneCompiler.Compile(DocumentSnapshot.Capture(second));

        Assert.True(firstScene.Succeeded);
        Assert.True(secondScene.Succeeded);
        Assert.Equal(firstScene.DocumentHash, secondScene.DocumentHash);
        Assert.Equal(firstScene.SceneHash, secondScene.SceneHash);
        Assert.Equal(new[] { "back", "front" }, firstScene.Nodes.Select(node => node.Id));
    }

    [Fact]
    public void Compile_ResolvesRotatedBoundsAndLineEndpointsInMillimeters()
    {
        var template = new LabelTemplate { Id = "rotation", Name = "Rotation", WidthMm = 100, HeightMm = 50 };
        template.Objects.Add(new LabelObject
        {
            Id = "rotated",
            Type = ObjectType.Rectangle,
            XMm = 10,
            YMm = 5,
            WidthMm = 20,
            HeightMm = 4,
            Rotation = 90
        });
        template.Objects.Add(new LabelObject
        {
            Id = "line",
            Type = ObjectType.Line,
            XMm = 3,
            YMm = 4,
            WidthMm = 7,
            HeightMm = 6
        });

        var result = SceneCompiler.Compile(DocumentSnapshot.Capture(template));
        Assert.True(result.Succeeded);
        var rotated = Assert.Single(result.Nodes, node => node.Id == "rotated");
        Assert.Equal(20, rotated.LayoutBoundsMm.WidthMm, precision: 6);
        Assert.Equal(4, rotated.LayoutBoundsMm.HeightMm, precision: 6);
        Assert.Equal(4, rotated.VisualBoundsMm.WidthMm, precision: 6);
        Assert.Equal(20, rotated.VisualBoundsMm.HeightMm, precision: 6);
        Assert.Equal(20, rotated.Anchors.CenterXMm, precision: 6);
        Assert.Equal(7, rotated.Anchors.CenterYMm, precision: 6);

        var line = Assert.Single(result.Nodes, node => node.Id == "line");
        Assert.Equal(new ScenePoint(3, 4), line.LineStartMm);
        Assert.Equal(new ScenePoint(10, 10), line.LineEndMm);
        Assert.Equal(7, line.LayoutBoundsMm.WidthMm, precision: 6);
        Assert.Equal(6, line.LayoutBoundsMm.HeightMm, precision: 6);
    }

    [Fact]
    public void Compile_RejectsNonPositiveLabelDimensionsWithoutPublishingSuccess()
    {
        var captured = DocumentSnapshot.Capture(CreateTemplate());
        var valid = SceneCompiler.Compile(captured);

        var zeroWidth = SceneCompiler.Compile(captured with { WidthMm = 0 });
        var zeroHeight = SceneCompiler.Compile(captured with { HeightMm = 0 });
        var notANumber = SceneCompiler.Compile(captured with { WidthMm = double.NaN });

        var infiniteTemplate = CreateTemplate();
        infiniteTemplate.WidthMm = double.PositiveInfinity;
        var infinite = SceneCompiler.Compile(DocumentSnapshot.Capture(infiniteTemplate));

        Assert.True(valid.Succeeded);
        Assert.False(zeroWidth.Succeeded);
        Assert.False(zeroHeight.Succeeded);
        Assert.False(notANumber.Succeeded);
        Assert.False(infinite.Succeeded);
        Assert.Contains(zeroWidth.Diagnostics, diagnostic => diagnostic.Code == "SCN001");
        Assert.Contains(zeroHeight.Diagnostics, diagnostic => diagnostic.Code == "SCN001");
        Assert.Contains(notANumber.Diagnostics, diagnostic => diagnostic.Code == "SCN001");
        Assert.Contains(infinite.Diagnostics, diagnostic => diagnostic.Code == "SCN001");
    }

    [Fact]
    public void Compile_RejectsDuplicateIdsWithoutPublishingSuccess()
    {
        var template = CreateTemplate();
        template.Objects[1].Id = template.Objects[0].Id;

        var result = SceneCompiler.Compile(DocumentSnapshot.Capture(template));

        Assert.False(result.Succeeded);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "SCN002");
    }

    [Fact]
    public void Compile_AcceptsHorizontalAndVerticalLinesButRejectsDegenerateOrUnknownObjects()
    {
        var template = new LabelTemplate { Id = "line-fixture", WidthMm = 50, HeightMm = 25 };
        template.Objects.Add(new LabelObject
        {
            Id = "horizontal",
            Type = ObjectType.Line,
            XMm = 2,
            YMm = 4,
            WidthMm = 12,
            HeightMm = 1,
            LineEndXMm = 14,
            LineEndYMm = 4
        });
        template.Objects.Add(new LabelObject
        {
            Id = "vertical",
            Type = ObjectType.Line,
            XMm = 20,
            YMm = 2,
            WidthMm = 1,
            HeightMm = 9,
            LineEndXMm = 20,
            LineEndYMm = 11
        });
        template.Objects.Add(new LabelObject
        {
            Id = "degenerate",
            Type = ObjectType.Line,
            XMm = 1,
            YMm = 1,
            WidthMm = 1,
            HeightMm = 1,
            LineEndXMm = 1,
            LineEndYMm = 1
        });
        template.Objects.Add(new LabelObject
        {
            Id = "unknown",
            Type = (ObjectType)999,
            XMm = 1,
            YMm = 1,
            WidthMm = 1,
            HeightMm = 1
        });

        var result = SceneCompiler.Compile(DocumentSnapshot.Capture(template));

        Assert.Contains(result.Nodes, node => node.Id == "horizontal" && node.LineEndMm == new ScenePoint(14, 4));
        Assert.Contains(result.Nodes, node => node.Id == "vertical" && node.LineEndMm == new ScenePoint(20, 11));
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "SCN005" && diagnostic.Message.Contains("degenerate", StringComparison.Ordinal));
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "SCN007" && diagnostic.Message.Contains("unknown", StringComparison.Ordinal));
        Assert.False(result.Succeeded);
    }

    [Fact]
    public void Compile_HashChangesWhenTextOrStyleChanges()
    {
        var template = CreateTemplate();
        var before = SceneCompiler.Compile(DocumentSnapshot.Capture(template));

        template.Objects[0].Text = "changed";
        var afterText = SceneCompiler.Compile(DocumentSnapshot.Capture(template));
        template.Objects[0].Style.FontSizePt = 16;
        var afterStyle = SceneCompiler.Compile(DocumentSnapshot.Capture(template));
        template.Objects[0].Style.TextSizing = TextSizingMode.FixedFrame;
        var afterSizing = SceneCompiler.Compile(DocumentSnapshot.Capture(template));
        template.Objects[0].Style.TextOverflow = TextOverflowMode.Clip;
        var afterOverflowPolicy = SceneCompiler.Compile(DocumentSnapshot.Capture(template));
        template.Objects[0].Style.TextPaddingMm = 1.25;
        var afterPadding = SceneCompiler.Compile(DocumentSnapshot.Capture(template));
        template.Objects[0].Style.TextPaddingRightMm = 2.5;
        var afterEdgePadding = SceneCompiler.Compile(DocumentSnapshot.Capture(template));

        Assert.NotEqual(before.SceneHash, afterText.SceneHash);
        Assert.NotEqual(afterText.SceneHash, afterStyle.SceneHash);
        Assert.NotEqual(afterStyle.SceneHash, afterSizing.SceneHash);
        Assert.NotEqual(afterSizing.SceneHash, afterOverflowPolicy.SceneHash);
        Assert.NotEqual(afterOverflowPolicy.SceneHash, afterPadding.SceneHash);
        Assert.NotEqual(afterPadding.SceneHash, afterEdgePadding.SceneHash);
    }

    [Fact]
    public void Snapshot_TracksStableAggregateTextResourceFingerprint()
    {
        var template = CreateTemplate();
        var before = DocumentSnapshot.Capture(template);

        Assert.NotEmpty(before.TextResourceFingerprint);
        Assert.Equal(
            before.Objects.Single(item => item.Id == "front").Style.TextResourceFingerprint,
            DocumentSnapshot.Capture(template).Objects.Single(item => item.Id == "front").Style.TextResourceFingerprint);

        template.Objects[0].Style.TextDirection = TextDirectionMode.RightToLeft;
        var afterDirection = DocumentSnapshot.Capture(template);
        template.Objects[0].Style.FontFamily = "Bahnschrift";
        var afterFont = DocumentSnapshot.Capture(template);

        Assert.NotEqual(before.TextResourceFingerprint, afterDirection.TextResourceFingerprint);
        Assert.NotEqual(afterDirection.TextResourceFingerprint, afterFont.TextResourceFingerprint);
    }

    private static LabelTemplate CreateTemplate()
    {
        var template = new LabelTemplate
        {
            Id = "scene-fixture",
            Name = "Scene fixture",
            WidthMm = 100,
            HeightMm = 50,
            GapMm = 2,
            MarginMm = 1,
            Dpi = 203
        };
        template.Objects.Add(new LabelObject
        {
            Id = "front",
            Type = ObjectType.Text,
            Name = "Front text",
            XMm = 4,
            YMm = 3,
            WidthMm = 30,
            HeightMm = 6,
            Text = "front",
            ZIndex = 2
        });
        template.Objects.Add(new LabelObject
        {
            Id = "back",
            Type = ObjectType.Rectangle,
            Name = "Back rectangle",
            XMm = 2,
            YMm = 2,
            WidthMm = 40,
            HeightMm = 12,
            ZIndex = 1
        });
        return template;
    }
}
