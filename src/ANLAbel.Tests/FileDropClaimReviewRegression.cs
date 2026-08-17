using ANLAbel.Core.Automation;
using ANLAbel.Data.Automation;

internal static class FileDropClaimReviewRegression
{
    public static Task Run()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".jsonl");
        try
        {
            var ledger = new FileDropClaimLedger(path);
            var first = FileDropClaimContract.CreateIdentity("trigger", "config", "source-one");
            var second = FileDropClaimContract.CreateIdentity("trigger", "config", "source-two");
            ledger.TryRecordDetection(first, out _, out _); ledger.TryRecordDetection(second, out _, out _);
            var review = new FileDropClaimReviewService(ledger);
            Require(review.ClaimDetected(out var message) == 2 && message.Contains("no source was moved", StringComparison.OrdinalIgnoreCase), "Explicit review must claim detected evidence without source mutation.");
            Require(review.ClaimDetected(out _) == 0, "Claimed evidence must not be claimed twice.");
            var events = ledger.ReadValid(out var diagnostics);
            Require(diagnostics.Count == 0 && events.Count == 4 && events.Count(item => item.To == FileDropEventState.Claimed) == 2, "Every claim must be durable and explicit.");
        }
        finally { if (File.Exists(path)) File.Delete(path); }
        return Task.CompletedTask;
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
