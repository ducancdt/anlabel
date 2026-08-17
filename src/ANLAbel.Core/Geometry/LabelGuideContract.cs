using ANLAbel.Core.Enums;
using ANLAbel.Core.Models;

namespace ANLAbel.Core.Geometry;

/// <summary>
/// Pure policy for persistent ruler guides. It keeps guide coordinates in the
/// document's physical millimetre space and provides deterministic clamping,
/// hit testing and stable ordering for the WPF overlay.
/// </summary>
public static class LabelGuideContract
{
    public const double MinimumPositionMm = 0;
    public const double HitToleranceDip = 8;

    public static double ClampPosition(double positionMm, LabelGuideOrientation orientation, double widthMm, double heightMm)
    {
        var length = orientation == LabelGuideOrientation.Vertical ? widthMm : heightMm;
        if (!double.IsFinite(length) || length <= 0)
        {
            return 0;
        }

        if (!double.IsFinite(positionMm))
        {
            return 0;
        }

        return Math.Round(Math.Clamp(positionMm, MinimumPositionMm, length), 3, MidpointRounding.AwayFromZero);
    }

    public static bool IsValid(LabelGuide guide, double widthMm, double heightMm)
    {
        ArgumentNullException.ThrowIfNull(guide);
        return !string.IsNullOrWhiteSpace(guide.Id)
            && Enum.IsDefined(guide.Orientation)
            && double.IsFinite(guide.PositionMm)
            && guide.PositionMm >= MinimumPositionMm
            && guide.PositionMm <= (guide.Orientation == LabelGuideOrientation.Vertical ? widthMm : heightMm);
    }

    public static LabelGuide? FindNearest(
        IEnumerable<LabelGuide> guides,
        LabelGuideOrientation orientation,
        double positionMm,
        double zoom,
        double widthMm,
        double heightMm,
        bool includeLocked = true)
    {
        ArgumentNullException.ThrowIfNull(guides);
        if (!double.IsFinite(positionMm))
        {
            return null;
        }

        var toleranceMm = MmConverter.DipToMm(HitToleranceDip / Math.Max(0.01, zoom));
        return guides
            .Where(guide => guide.IsVisible
                && (includeLocked || !guide.IsLocked)
                && guide.Orientation == orientation
                && IsValid(guide, widthMm, heightMm))
            .Select(guide => new { Guide = guide, Distance = Math.Abs(guide.PositionMm - positionMm) })
            .Where(candidate => candidate.Distance <= toleranceMm)
            .OrderBy(candidate => candidate.Distance)
            .ThenBy(candidate => candidate.Guide.Id, StringComparer.Ordinal)
            .Select(candidate => candidate.Guide)
            .FirstOrDefault();
    }

    public static IReadOnlyList<LabelGuide> StableOrder(IEnumerable<LabelGuide> guides)
    {
        ArgumentNullException.ThrowIfNull(guides);
        return guides
            .OrderBy(guide => guide.Orientation)
            .ThenBy(guide => guide.PositionMm)
            .ThenBy(guide => guide.Id, StringComparer.Ordinal)
            .ToArray();
    }
}
