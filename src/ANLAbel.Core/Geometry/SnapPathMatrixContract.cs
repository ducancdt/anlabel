namespace ANLAbel.Core.Geometry;

/// <summary>
/// Interaction path identity for the shared snap decision matrix.  Every
/// pointer path (single move, group move, resize edge and freehand draw)
/// must evaluate candidates with the same screen-space acquire/release
/// budget, priority ranking and hysteresis rules.
/// </summary>
public enum SnapPathKind
{
    SingleMove = 0,
    GroupMove = 1,
    Resize = 2,
    Draw = 3
}

/// <summary>
/// Pure matrix that converts one screen-space pointer observation into a
/// document-space snap decision.  The canvas supplies candidates already
/// expressed in millimetres; this contract owns only the zoom-normalized
/// tolerance, ranking and hysteresis so path-specific code cannot invent a
/// second acquire/release budget.
/// </summary>
public static class SnapPathMatrixContract
{
    /// <summary>
    /// Canonical zoom ladder used by the DP-129 software fixture set.  Real
    /// pointer/display traces on a baseline workstation remain a separate
    /// hardware evidence gate.
    /// </summary>
    public static IReadOnlyList<double> SoftwareZoomLadder { get; } =
        new[] { 0.25, 0.5, 1.0, 2.0, 4.0 };

    public static double AcquireToleranceMm(double zoom)
        => SnapToleranceContract.AcquireToleranceMm(zoom);

    public static double ReleaseToleranceMm(double zoom)
        => SnapToleranceContract.ReleaseToleranceMm(zoom);

    /// <summary>
    /// Shared candidate ranking entry for every interaction path.  Callers must
    /// not invoke <see cref="SnapCandidateSelector.Choose"/> directly for pointer
    /// work — this method records the path kind and applies the zoom-normalized
    /// acquire budget so a single/group/resize/draw bypass cannot invent a
    /// second tolerance.
    /// </summary>
    public static SnapCandidate? Choose(
        SnapPathKind pathKind,
        double zoom,
        IEnumerable<SnapCandidate> candidates)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        // Path kind is part of the call contract even though ranking itself is
        // path-agnostic; fixtures assert every lane routes through here.
        _ = pathKind;
        return SnapCandidateSelector.Choose(candidates, AcquireToleranceMm(zoom));
    }

    /// <summary>
    /// Shared release-window hysteresis entry.  Alt/typed bypass returns null
    /// without mutating the lock so a later non-bypass frame can re-acquire.
    /// </summary>
    public static double? ApplyHysteresis(
        SnapPathKind pathKind,
        double zoom,
        SnapHysteresisState hysteresis,
        double proposedPositionMm,
        double? candidateTargetMm,
        bool bypassSnap = false)
    {
        ArgumentNullException.ThrowIfNull(hysteresis);
        _ = pathKind;
        if (bypassSnap)
        {
            return null;
        }

        return hysteresis.Resolve(proposedPositionMm, candidateTargetMm, ReleaseToleranceMm(zoom));
    }

    /// <summary>
    /// Chooses the winning candidate for a path.  When <paramref name="bypassSnap"/>
    /// is true (Alt / typed exact dimensions) the path returns no snap target
    /// and leaves hysteresis untouched so a later non-bypass frame can re-lock.
    /// </summary>
    public static SnapPathDecision Resolve(
        SnapPathKind pathKind,
        double zoom,
        IEnumerable<SnapCandidate> candidates,
        SnapHysteresisState hysteresis,
        double proposedPositionMm,
        bool bypassSnap = false)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        ArgumentNullException.ThrowIfNull(hysteresis);

        if (bypassSnap)
        {
            return new SnapPathDecision(
                pathKind,
                SnapToleranceContract.NormalizeZoom(zoom),
                AcquireToleranceMm(zoom),
                ReleaseToleranceMm(zoom),
                Snapped: false,
                TargetMm: null,
                WinnerKey: null,
                Bypassed: true,
                Winner: null);
        }

        var acquireMm = AcquireToleranceMm(zoom);
        var releaseMm = ReleaseToleranceMm(zoom);
        var winner = Choose(pathKind, zoom, candidates);
        var locked = ApplyHysteresis(
            pathKind,
            zoom,
            hysteresis,
            proposedPositionMm,
            winner?.TargetPosition,
            bypassSnap: false);
        var snapped = locked is not null;
        return new SnapPathDecision(
            pathKind,
            SnapToleranceContract.NormalizeZoom(zoom),
            acquireMm,
            releaseMm,
            Snapped: snapped,
            TargetMm: locked,
            WinnerKey: snapped ? winner?.StableKey ?? hysteresis.LockedTarget?.ToString("R") : null,
            Bypassed: false,
            Winner: winner);
    }

    /// <summary>
    /// Returns true when the same candidate set yields the same winner key at
    /// every zoom in the software ladder for a fixed screen-space offset.  The
    /// candidate distances must already be expressed in document millimetres
    /// for the zoom under test (i.e. callers convert DIP→mm via
    /// <see cref="SnapToleranceContract.ToDocumentMm"/>).
    /// </summary>
    public static bool SameWinnerAcrossZooms(
        SnapPathKind pathKind,
        IReadOnlyList<double> zooms,
        Func<double, IReadOnlyList<SnapCandidate>> candidatesForZoom,
        double proposedPositionMm)
    {
        ArgumentNullException.ThrowIfNull(zooms);
        ArgumentNullException.ThrowIfNull(candidatesForZoom);
        if (zooms.Count == 0)
        {
            return false;
        }

        string? expected = null;
        foreach (var zoom in zooms)
        {
            var hysteresis = new SnapHysteresisState();
            var decision = Resolve(
                pathKind,
                zoom,
                candidatesForZoom(zoom),
                hysteresis,
                proposedPositionMm,
                bypassSnap: false);
            if (decision.WinnerKey is null)
            {
                return false;
            }

            expected ??= decision.WinnerKey;
            if (!string.Equals(expected, decision.WinnerKey, StringComparison.Ordinal))
            {
                return false;
            }
        }

        return expected is not null;
    }
}

/// <summary>
/// Immutable result of one snap evaluation.  Values are document millimetres
/// except for the normalized zoom and path identity.
/// </summary>
public readonly record struct SnapPathDecision(
    SnapPathKind PathKind,
    double NormalizedZoom,
    double AcquireToleranceMm,
    double ReleaseToleranceMm,
    bool Snapped,
    double? TargetMm,
    string? WinnerKey,
    bool Bypassed,
    SnapCandidate? Winner = null);
