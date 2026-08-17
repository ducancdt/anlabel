using ANLAbel.Core.Automation;

internal static class FileDropClaimContractRegression
{
    public static Task Run()
    {
        var source = FileDropClaimContract.ComputeContentFingerprint("label bytes"u8);
        var first = FileDropClaimContract.CreateIdentity("incoming-csv", "config-v1", source);
        var repeat = FileDropClaimContract.CreateIdentity("incoming-csv", "config-v1", source);
        var changedConfig = FileDropClaimContract.CreateIdentity("incoming-csv", "config-v2", source);

        Require(first.EventId == repeat.EventId, "Duplicate file notifications must resolve to one deterministic event identity.");
        Require(first.EventId != changedConfig.EventId, "Configuration drift must not reuse a previous file event identity.");
        Require(FileDropClaimContract.TryTransition(FileDropEventState.Unknown, FileDropEventState.Detected, out _), "A trigger may record detection.");
        Require(FileDropClaimContract.TryTransition(FileDropEventState.Detected, FileDropEventState.Claimed, out _), "A detected source may be claimed once.");
        Require(FileDropClaimContract.TryTransition(FileDropEventState.Claimed, FileDropEventState.Prepared, out _), "A claimed source may become prepared only after a future verified parser stage.");
        Require(FileDropClaimContract.TryTransition(FileDropEventState.Claimed, FileDropEventState.ChangedAfterClaim, out _), "Changed bytes must become an explicit terminal outcome.");
        Require(!FileDropClaimContract.TryTransition(FileDropEventState.Detected, FileDropEventState.Dispatched, out _), "Detection must never dispatch without a claim and approved host path.");
        Require(!FileDropClaimContract.TryTransition(FileDropEventState.Dispatched, FileDropEventState.Claimed, out _), "A terminal event must never be reclaimed implicitly.");
        Require(FileDropClaimContract.IsTerminal(FileDropEventState.Quarantined), "Quarantine must be terminal.");
        return Task.CompletedTask;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
