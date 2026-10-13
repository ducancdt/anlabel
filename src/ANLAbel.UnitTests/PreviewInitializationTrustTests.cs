using ANLAbel.Core.Printing;
using Xunit;

namespace ANLAbel.UnitTests;

public sealed class PreviewInitializationTrustTests
{
    [Fact]
    public void Combine_BlocksReadyWhenTicketTrustIsReadyButSceneFailed()
    {
        var message = PreviewInitializationTrust.SceneBlockMessage("SCENE: object geometry is invalid");

        Assert.Contains("SCENE: object geometry is invalid", message, StringComparison.Ordinal);
        Assert.Equal(
            PreviewTrustLevel.EffectiveBlocked,
            PreviewInitializationTrust.Combine(
                PreviewTrustLevel.EffectiveReady,
                sceneCompilationVerified: false,
                contentPreflightSucceeded: true));
    }

    [Fact]
    public void Combine_StaysReadyWhenSceneAndContentPass()
    {
        Assert.Equal(
            PreviewTrustLevel.EffectiveReady,
            PreviewInitializationTrust.Combine(
                PreviewTrustLevel.EffectiveReady,
                sceneCompilationVerified: true,
                contentPreflightSucceeded: true));
    }

    [Fact]
    public void Combine_KeepsDesignOnlyWhenNoPrinterEvenIfSceneFails()
    {
        Assert.Equal(
            PreviewTrustLevel.DesignOnly,
            PreviewInitializationTrust.Combine(
                PreviewTrustLevel.DesignOnly,
                sceneCompilationVerified: false,
                contentPreflightSucceeded: false));
    }

    [Fact]
    public void RetainForCurrentSnapshot_DropsReadyWhenQuantityOrTicketChanges()
    {
        var prepared = PrintSequenceBuilder.Build(new PrintIntent(new QuantityPlan(new[]
        {
            new PrintRecordQuantity("row-1", 1, 2),
            new PrintRecordQuantity("row-2", 2, 1)
        })));

        Assert.True(PreviewInitializationTrust.QuantitySnapshotMatches(
            prepared,
            new (int SourceRowNumber, int Copies)[] { (2, 1), (1, 2), (3, 0) }));
        Assert.False(PreviewInitializationTrust.QuantitySnapshotMatches(
            prepared,
            new (int SourceRowNumber, int Copies)[] { (1, 3), (2, 1) }));
        Assert.False(PreviewInitializationTrust.QuantitySnapshotMatches(
            prepared,
            new (int SourceRowNumber, int Copies)[] { (1, 2), (1, 2) }));

        Assert.True(PreviewInitializationTrust.OutputSnapshotUnchanged(
            "doc-a", "hash-a", true, "doc-a", "HASH-A", true));
        Assert.False(PreviewInitializationTrust.OutputSnapshotUnchanged(
            "doc-a", "hash-a", true, "doc-a", "hash-b", true));
        Assert.False(PreviewInitializationTrust.OutputSnapshotUnchanged(
            "doc-a", "hash-a", true, "doc-a", "hash-a", false));

        Assert.Equal(
            PreviewTrustLevel.Stale,
            PreviewInitializationTrust.RetainForCurrentSnapshot(PreviewTrustLevel.EffectiveReady, snapshotStillCurrent: false));
        Assert.Equal(
            PreviewTrustLevel.EffectiveReady,
            PreviewInitializationTrust.RetainForCurrentSnapshot(PreviewTrustLevel.EffectiveReady, snapshotStillCurrent: true));
        Assert.Equal(
            PreviewTrustLevel.DesignOnly,
            PreviewInitializationTrust.RetainForCurrentSnapshot(PreviewTrustLevel.DesignOnly, snapshotStillCurrent: false));
        Assert.NotEqual(
            PreviewTrustLevel.EffectiveReady,
            PreviewInitializationTrust.RetainForCurrentSnapshot(PreviewTrustLevel.EffectiveReady, snapshotStillCurrent: false));
    }
}
