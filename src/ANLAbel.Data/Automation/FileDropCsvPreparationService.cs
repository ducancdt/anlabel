using ANLAbel.Core.Data;
using ANLAbel.Core.Automation;

namespace ANLAbel.Data.Automation;

/// <summary>Verified CSV preparation. Valid records stay in-memory and never imply a print job.</summary>
public sealed class FileDropCsvPreparationService
{
    private readonly FileDropClaimLedger _ledger;
    private readonly FileDropSourceVerificationService _verification;
    public FileDropCsvPreparationService(FileDropClaimLedger ledger)
    {
        _ledger = ledger;
        _verification = new FileDropSourceVerificationService(ledger);
    }

    public bool TryPrepare(FileDropEventIdentity identity, Stream source, out IReadOnlyList<DataRecord> records, out string result)
    {
        using var buffered = new MemoryStream();
        source.CopyTo(buffered);
        buffered.Position = 0;
        if (!_verification.VerifyClaimed(identity, buffered, out result)) { records = []; return false; }
        buffered.Position = 0;
        var parsed = CsvAutomationSourceParser.Parse(buffered);
        if (parsed.Diagnostics.Count != 0)
        {
            _ledger.TryTransition(identity, FileDropEventState.Blocked, string.Join(" ", parsed.Diagnostics), out _, out var error);
            records = [];
            result = string.IsNullOrWhiteSpace(error) ? "CSV source is blocked by explicit parser diagnostics." : error;
            return false;
        }
        if (!_ledger.TryTransition(identity, FileDropEventState.Prepared, $"CSV parsed into {parsed.Records.Count} in-memory record(s); no manifest, queue or print was created.", out _, out var preparedError))
        {
            records = [];
            result = preparedError;
            return false;
        }
        records = parsed.Records;
        result = $"Prepared {records.Count} CSV record(s) in memory; document binding, manifest, queue and dispatch remain unavailable.";
        return true;
    }
}
