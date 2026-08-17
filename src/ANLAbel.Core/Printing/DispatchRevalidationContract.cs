using System.Collections.ObjectModel;
using System.Globalization;

namespace ANLAbel.Core.Printing;

/// <summary>
/// Field-level comparison of the immutable output contract prepared during
/// preflight with the contract re-read immediately before <c>PrintDocument</c>.
/// The ship path fails closed on any drift; this contract names the changed
/// fields so operators and tests can distinguish DPI, media, ticket and
/// imageable-area coercion without requiring a physical printer.
/// </summary>
public static class DispatchRevalidationContract
{
    public const string BlockDiagnosticPrefix =
        "Printing stopped because the printer output contract changed immediately before dispatch";

    public static DispatchRevalidationResult Evaluate(
        string? preparedDocumentHash,
        EffectiveOutputContract? preparedOutput,
        bool preparedTicketVerified,
        string? finalDocumentHash,
        EffectiveOutputContract? finalOutput,
        bool finalTicketVerified,
        string? expectedOutputContractHash = null)
    {
        var changes = new List<string>();

        if (string.IsNullOrWhiteSpace(preparedDocumentHash)
            || string.IsNullOrWhiteSpace(finalDocumentHash)
            || !string.Equals(preparedDocumentHash.Trim(), finalDocumentHash.Trim(), StringComparison.Ordinal))
        {
            changes.Add("document-hash");
        }

        if (preparedOutput is null || finalOutput is null)
        {
            changes.Add("output-contract-missing");
            return Block(changes, preparedTicketVerified, finalTicketVerified);
        }

        if (preparedTicketVerified != finalTicketVerified
            || preparedOutput.IsTicketValidated != finalOutput.IsTicketValidated)
        {
            changes.Add("ticket-evidence");
        }

        if (!string.Equals(
                preparedOutput.RequestedTicketHash.Trim(),
                finalOutput.RequestedTicketHash.Trim(),
                StringComparison.OrdinalIgnoreCase))
        {
            changes.Add("requested-ticket");
        }

        if (!string.Equals(
                preparedOutput.EffectiveTicketHash.Trim(),
                finalOutput.EffectiveTicketHash.Trim(),
                StringComparison.OrdinalIgnoreCase))
        {
            changes.Add("effective-ticket");
        }

        if (!string.Equals(
                preparedOutput.PrinterName.Trim(),
                finalOutput.PrinterName.Trim(),
                StringComparison.OrdinalIgnoreCase))
        {
            changes.Add("queue-name");
        }

        if (preparedOutput.DpiX != finalOutput.DpiX || preparedOutput.DpiY != finalOutput.DpiY)
        {
            changes.Add("dpi");
        }

        if (preparedOutput.MediaType != finalOutput.MediaType
            || !NearlyEqual(preparedOutput.LabelWidthMm, finalOutput.LabelWidthMm)
            || !NearlyEqual(preparedOutput.LabelHeightMm, finalOutput.LabelHeightMm)
            || !NearlyEqual(preparedOutput.GapMm, finalOutput.GapMm))
        {
            changes.Add("media");
        }

        if (preparedOutput.PrintableOriginXDots != finalOutput.PrintableOriginXDots
            || preparedOutput.PrintableOriginYDots != finalOutput.PrintableOriginYDots
            || preparedOutput.PrintableWidthDots != finalOutput.PrintableWidthDots
            || preparedOutput.PrintableHeightDots != finalOutput.PrintableHeightDots
            || preparedOutput.PrintableAreaVerified != finalOutput.PrintableAreaVerified
            || !NearlyEqual(preparedOutput.PrintableOriginXDip, finalOutput.PrintableOriginXDip)
            || !NearlyEqual(preparedOutput.PrintableOriginYDip, finalOutput.PrintableOriginYDip)
            || !NearlyEqual(preparedOutput.PrintableWidthDip, finalOutput.PrintableWidthDip)
            || !NearlyEqual(preparedOutput.PrintableHeightDip, finalOutput.PrintableHeightDip))
        {
            changes.Add("imageable-area");
        }

        if (!string.Equals(preparedOutput.Fingerprint, finalOutput.Fingerprint, StringComparison.OrdinalIgnoreCase))
        {
            // Catch-all for any remaining fingerprint fields (offsets, scale,
            // feed direction, rotation) not listed above.
            if (!changes.Contains("effective-ticket")
                && !changes.Contains("dpi")
                && !changes.Contains("media")
                && !changes.Contains("imageable-area")
                && !changes.Contains("queue-name")
                && !changes.Contains("requested-ticket")
                && !changes.Contains("ticket-evidence"))
            {
                changes.Add("output-contract-fingerprint");
            }
            else if (!changes.Contains("output-contract-fingerprint")
                     && changes.Count == 0)
            {
                changes.Add("output-contract-fingerprint");
            }
        }

        if (!PrintContractGuard.Matches(
                expectedOutputContractHash,
                finalOutput.Fingerprint,
                finalTicketVerified && finalOutput.IsTicketValidated))
        {
            changes.Add("prepared-expectation");
        }

        if (!PrintContractGuard.MatchesDispatchSnapshot(
                preparedDocumentHash,
                preparedOutput.Fingerprint,
                preparedTicketVerified && preparedOutput.IsTicketValidated,
                finalDocumentHash,
                finalOutput.Fingerprint,
                finalTicketVerified && finalOutput.IsTicketValidated))
        {
            if (changes.Count == 0)
            {
                changes.Add("dispatch-snapshot");
            }
        }

        if (changes.Count > 0)
        {
            return Block(changes, preparedTicketVerified, finalTicketVerified);
        }

        return new DispatchRevalidationResult(
            Allowed: true,
            ChangedFields: Array.Empty<string>(),
            Diagnostic: string.Empty,
            SubmissionAllowed: true);
    }

    /// <summary>
    /// Compatibility helper used when only plan fingerprints are available
    /// (no full <see cref="EffectiveOutputContract"/> reconstruction).  Hash
    /// drift still fails closed; field names are not available in that path.
    /// </summary>
    public static DispatchRevalidationResult EvaluateFingerprints(
        string? preparedDocumentHash,
        string? preparedOutputContractHash,
        bool preparedTicketVerified,
        string? finalDocumentHash,
        string? finalOutputContractHash,
        bool finalTicketVerified,
        string? expectedOutputContractHash = null)
    {
        if (PrintContractGuard.MatchesDispatchSnapshot(
                preparedDocumentHash,
                preparedOutputContractHash,
                preparedTicketVerified,
                finalDocumentHash,
                finalOutputContractHash,
                finalTicketVerified)
            && PrintContractGuard.Matches(
                expectedOutputContractHash,
                finalOutputContractHash,
                finalTicketVerified))
        {
            return new DispatchRevalidationResult(
                Allowed: true,
                ChangedFields: Array.Empty<string>(),
                Diagnostic: string.Empty,
                SubmissionAllowed: true);
        }

        var changes = new List<string>();
        if (string.IsNullOrWhiteSpace(preparedDocumentHash)
            || string.IsNullOrWhiteSpace(finalDocumentHash)
            || !string.Equals(preparedDocumentHash.Trim(), finalDocumentHash.Trim(), StringComparison.Ordinal))
        {
            changes.Add("document-hash");
        }

        if (string.IsNullOrWhiteSpace(preparedOutputContractHash)
            || string.IsNullOrWhiteSpace(finalOutputContractHash)
            || !string.Equals(
                preparedOutputContractHash.Trim(),
                finalOutputContractHash.Trim(),
                StringComparison.OrdinalIgnoreCase))
        {
            changes.Add("output-contract-fingerprint");
        }

        if (preparedTicketVerified != finalTicketVerified)
        {
            changes.Add("ticket-evidence");
        }

        if (!PrintContractGuard.Matches(
                expectedOutputContractHash,
                finalOutputContractHash,
                finalTicketVerified))
        {
            changes.Add("prepared-expectation");
        }

        if (changes.Count == 0)
        {
            changes.Add("dispatch-snapshot");
        }

        return Block(changes, preparedTicketVerified, finalTicketVerified);
    }

    private static DispatchRevalidationResult Block(
        List<string> changes,
        bool preparedTicketVerified,
        bool finalTicketVerified)
    {
        var distinct = changes
            .Where(static c => !string.IsNullOrWhiteSpace(c))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static c => c, StringComparer.Ordinal)
            .ToArray();
        var fields = distinct.Length == 0
            ? "unknown"
            : string.Join(", ", distinct);
        var diagnostic =
            $"{BlockDiagnosticPrefix} ({fields}). Reopen preview and review the updated contract; no label was submitted.";
        _ = preparedTicketVerified;
        _ = finalTicketVerified;
        return new DispatchRevalidationResult(
            Allowed: false,
            ChangedFields: new ReadOnlyCollection<string>(distinct),
            Diagnostic: diagnostic,
            SubmissionAllowed: false);
    }

    private static bool NearlyEqual(double left, double right)
        => Math.Abs(left - right) <= 1e-9
           || (double.IsNaN(left) && double.IsNaN(right));
}

/// <summary>
/// Fail-closed decision for last-mile dispatch.  When
/// <see cref="SubmissionAllowed"/> is false the print path must not call
/// <c>PrintDocument</c> or write a durable preflight-success event.
/// </summary>
public sealed record DispatchRevalidationResult(
    bool Allowed,
    IReadOnlyList<string> ChangedFields,
    string Diagnostic,
    bool SubmissionAllowed)
{
    public override string ToString()
        => Allowed
            ? "allow"
            : string.Create(CultureInfo.InvariantCulture, $"block:{string.Join('+', ChangedFields)}");
}
