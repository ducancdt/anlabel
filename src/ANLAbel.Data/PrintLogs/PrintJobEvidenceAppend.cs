using ANLAbel.Core.Printing;

namespace ANLAbel.Data.PrintLogs;

/// <summary>
/// Shared append seam for preview and quick-print: required pre-dispatch evidence
/// failures propagate; post-dispatch append failures stay best-effort (P08).
/// </summary>
public static class PrintJobEvidenceAppend
{
    public static async Task AppendAsync(
        PrintJobStateStore store,
        PrintJobStateTransition transition,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(transition);

        try
        {
            await store.AppendAsync(transition, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (!PrintJobEvidencePolicy.IsRequiredBeforeDispatch(transition.To))
        {
            System.Diagnostics.Debug.WriteLine($"Print job state log failed (best-effort): {ex.Message}");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Print was blocked because required job evidence could not be saved ({ex.Message}). Fix local log access and try again.",
                ex);
        }
    }
}
