using ANLAbel.Core.Automation;
using ANLAbel.Data.Automation;
using Xunit;

namespace ANLAbel.UnitTests;

/// <summary>
/// L4 file-drop: a source that changes after claim must fail closed for
/// dispatch/archive and may only move to quarantine. Drives the shipped
/// contract and move service, not a copy of their tables.
/// </summary>
public sealed class FileDropChangedAfterClaimProtectTests
{
    [Fact]
    public void ChangedAfterClaimCannotDispatchOrArchiveAndMustQuarantine()
    {
        Assert.True(
            FileDropClaimContract.TryTransition(FileDropEventState.Claimed, FileDropEventState.ChangedAfterClaim, out _),
            "Changed bytes after claim must be an explicit fail-closed state.");
        Assert.False(
            FileDropClaimContract.TryTransition(FileDropEventState.ChangedAfterClaim, FileDropEventState.Dispatched, out _),
            "A changed source must never become dispatched.");
        Assert.False(
            FileDropClaimContract.TryTransition(FileDropEventState.ChangedAfterClaim, FileDropEventState.Dispatching, out _),
            "A changed source must never start dispatch.");
        Assert.False(
            FileDropClaimContract.TryTransition(FileDropEventState.ChangedAfterClaim, FileDropEventState.MovingToArchive, out _),
            "A changed source must not be archived as a successful job.");
        Assert.True(
            FileDropClaimContract.TryTransition(FileDropEventState.ChangedAfterClaim, FileDropEventState.MovingToQuarantine, out _),
            "A changed source must still be allowed to leave the watch root into quarantine.");
        Assert.True(FileDropClaimContract.IsTerminal(FileDropEventState.Quarantined));
        Assert.True(FileDropClaimContract.IsTerminal(FileDropEventState.Archived));
        Assert.True(FileDropClaimContract.IsTerminal(FileDropEventState.Blocked));
    }

    [Fact]
    public void MoveServiceRejectsArchiveAndAllowsQuarantineAfterChangedClaim()
    {
        var root = Path.Combine(Path.GetTempPath(), "anlabel-filedrop-protect-" + Guid.NewGuid().ToString("N"));
        var watch = Path.Combine(root, "watch");
        var archive = Path.Combine(root, "archive");
        var quarantine = Path.Combine(root, "quarantine");
        Directory.CreateDirectory(watch);

        try
        {
            var source = Path.Combine(watch, "batch.csv");
            File.WriteAllText(source, "SKU\nA\n");
            var configuration = new FileDropTriggerConfiguration("protect-trigger", "Protect", watch, "*.csv", false, true);
            var identity = FileDropClaimContract.CreateIdentity(
                configuration.TriggerId,
                configuration.ConfigurationFingerprint,
                FileDropClaimContract.ComputeContentFingerprint(File.ReadAllBytes(source)));
            var ledger = new FileDropClaimLedger(Path.Combine(root, "ledger.jsonl"));
            Assert.True(ledger.TryRecordDetection(identity, out _, out _));
            Assert.True(ledger.TryTransition(identity, FileDropEventState.Claimed, "review", out _, out _));
            Assert.True(ledger.TryTransition(identity, FileDropEventState.ChangedAfterClaim, "bytes changed", out _, out _));
            Assert.False(
                ledger.TryTransition(identity, FileDropEventState.Dispatched, "must not print", out _, out _),
                "Ledger must refuse dispatch after the source changed.");

            var mover = new FileDropSourceFileMoveService(ledger);
            Assert.False(
                mover.TryMove(identity, configuration, source, archive, FileDropSourceDisposition.Archive, out _, out var archiveRejected),
                "Archive after change would treat a stale source as a successful job.");
            Assert.Contains("not permitted", archiveRejected, StringComparison.OrdinalIgnoreCase);
            Assert.True(File.Exists(source), "A rejected archive must leave the source in the watch root.");

            Assert.True(
                mover.TryMove(identity, configuration, source, quarantine, FileDropSourceDisposition.Quarantine, out var quarantinedPath, out _),
                "Changed source must be movable to a local quarantine root.");
            Assert.False(File.Exists(source));
            Assert.True(File.Exists(quarantinedPath));
            Assert.Equal(FileDropEventState.Quarantined, ledger.ReadValid(out var diagnostics).Last().To);
            Assert.Empty(diagnostics);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, true);
            }
        }
    }
}
