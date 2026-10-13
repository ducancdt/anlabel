using ANLAbel.Core.Printing;

namespace ANLAbel.Data.PrintLogs;

/// <summary>
/// Shared append seam for preview and quick-print: required pre-dispatch evidence
/// failures propagate; post-dispatch append failures stay best-effort (P08).
/// </summary>
public static class PrintJobEvidenceAppend
{
    /// <returns>
    /// False when a post-dispatch event could not be saved. The job then stays
    /// uncertain in the log, so the caller tells the operator why it will be
    /// listed for review (audit 2026-10-13).
    /// </returns>
    public static async Task<bool> AppendAsync(
        PrintJobStateStore store,
        PrintJobStateTransition transition,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(transition);

        try
        {
            await store.AppendAsync(transition, cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (Exception ex) when (!PrintJobEvidencePolicy.IsRequiredBeforeDispatch(transition.To))
        {
            System.Diagnostics.Debug.WriteLine($"Print job state log failed (best-effort): {ex.Message}");
            return false;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Print was blocked because required job evidence could not be saved ({ex.Message}). Fix local log access and try again.",
                ex);
        }
    }
}
