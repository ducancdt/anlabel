using ANLAbel.Core.Printing;
using Xunit;

namespace ANLAbel.UnitTests;

public sealed class PrintPreviewContractsTests
{
    [Fact]
    public void SelectedRowsAndCopiesProduceDeterministicStableInstances()
    {
        var intent = new PrintIntent(new QuantityPlan(new[]
        {
            new PrintRecordQuantity("A", 2, 2),
            new PrintRecordQuantity("B", 3, 2),
            new PrintRecordQuantity("C", 4, 2)
        }));

        var first = PrintSequenceBuilder.Build(intent);
        var second = PrintSequenceBuilder.Build(intent);

        Assert.Equal(
            new[] { "A:row-2:copy-1", "A:row-2:copy-2", "B:row-3:copy-1", "B:row-3:copy-2", "C:row-4:copy-1", "C:row-4:copy-2" },
            first.Select(instance => instance.InstanceId));
        Assert.Equal(first, second);
        Assert.Equal(Enumerable.Range(1, 6), first.Select(instance => instance.SequenceOrdinal));
    }

    [Fact]
    public void ZeroCopiesKeepRecordOutOfPreparedSequence()
    {
        var plan = new QuantityPlan(new[]
        {
            new PrintRecordQuantity("not-selected", 2, 0),
            new PrintRecordQuantity("selected", 3, 1)
        });

        var sequence = PrintSequenceBuilder.Build(new PrintIntent(plan));

        Assert.Equal(1, plan.SelectedRecordCount);
        Assert.Equal(1, plan.TotalLabelCount);
        Assert.Equal("selected", Assert.Single(sequence).RecordId);
    }

    [Fact]
    public void OneLabelPerFeedPagesPreserveInstanceIdentity()
    {
        var sequence = PrintSequenceBuilder.Build(new PrintIntent(new QuantityPlan(new[]
        {
            new PrintRecordQuantity("row-7", 7, 2)
        })));

        var pages = PrintSequenceBuilder.CreateOneLabelPerFeedPages(sequence, 100, 50);

        Assert.Equal(2, pages.Count);
        Assert.Equal(new[] { 1, 2 }, pages.Select(page => page.LabelNumber));
        Assert.Equal(sequence, pages.Select(page => page.Instance));
        Assert.All(pages, page =>
        {
            Assert.Equal(100, page.LabelWidthMm);
            Assert.Equal(50, page.LabelHeightMm);
        });
    }

    [Theory]
    [InlineData("", 1, 1)]
    [InlineData("row", 0, 1)]
    [InlineData("row", 1, -1)]
    [InlineData("row", 1, 1000)]
    public void InvalidRecordQuantityFailsClosed(string recordId, int sourceRow, int copies)
    {
        var intent = new PrintIntent(new QuantityPlan(new[]
        {
            new PrintRecordQuantity(recordId, sourceRow, copies)
        }));

        Assert.ThrowsAny<ArgumentException>(() => PrintSequenceBuilder.Build(intent));
    }

    [Fact]
    public void DuplicateRecordOrSourceRowIdentityFailsClosed()
    {
        Assert.Throws<ArgumentException>(() => PrintSequenceBuilder.Build(new PrintIntent(new QuantityPlan(new[]
        {
            new PrintRecordQuantity("same", 1, 1),
            new PrintRecordQuantity("same", 2, 1)
        }))));

        Assert.Throws<ArgumentException>(() => PrintSequenceBuilder.Build(new PrintIntent(new QuantityPlan(new[]
        {
            new PrintRecordQuantity("first", 1, 1),
            new PrintRecordQuantity("second", 1, 1)
        }))));
    }

    [Fact]
    public void ExcessivePreparedLabelCountFailsBeforeAllocation()
    {
        var records = Enumerable.Range(1, 102)
            .Select(row => new PrintRecordQuantity($"row-{row}", row, QuantityPlan.MaximumCopiesPerRecord))
            .ToArray();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            PrintSequenceBuilder.Build(new PrintIntent(new QuantityPlan(records))));
    }

    [Theory]
    [InlineData(0, 50)]
    [InlineData(100, 0)]
    [InlineData(double.NaN, 50)]
    [InlineData(100, double.PositiveInfinity)]
    public void InvalidPreviewDimensionsFailClosed(double widthMm, double heightMm)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            PrintSequenceBuilder.CreateOneLabelPerFeedPages(Array.Empty<LabelInstance>(), widthMm, heightMm));
    }

    [Fact]
    public void TrustLevelDoesNotCollapseDesignOnlyIntoReady()
    {
        Assert.NotEqual(PreviewTrustLevel.DesignOnly, PreviewTrustLevel.EffectiveReady);
        Assert.NotEqual(PreviewTrustLevel.EffectiveUnverified, PreviewTrustLevel.EffectiveReady);
        Assert.NotEqual(PreviewTrustLevel.EffectiveBlocked, PreviewTrustLevel.EffectiveReady);
        Assert.NotEqual(PreviewTrustLevel.Stale, PreviewTrustLevel.EffectiveReady);
    }
}
