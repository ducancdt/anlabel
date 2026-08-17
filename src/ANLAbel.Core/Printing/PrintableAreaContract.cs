namespace ANLAbel.Core.Printing;

/// <summary>
/// Validates the value-only imageable rectangle returned by a printer driver.
/// WPF exposes the rectangle in 1/96-inch DIPs; the driver is still responsible
/// for proving that it is finite, positive and contained by the effective media.
/// </summary>
public static class PrintableAreaContract
{
    /// <summary>
    /// Small allowance for driver rounding at the media boundary.  It is large
    /// enough for a 1/96-inch conversion round trip, but never repairs a real
    /// negative origin or an area that extends beyond the stock.
    /// </summary>
    public const double BoundaryToleranceDip = 1.0;

    public static PrintableAreaValidation Validate(
        double originXDip,
        double originYDip,
        double extentWidthDip,
        double extentHeightDip,
        double? mediaWidthDip = null,
        double? mediaHeightDip = null)
    {
        if (!IsFinite(originXDip)
            || !IsFinite(originYDip)
            || !IsFinite(extentWidthDip)
            || !IsFinite(extentHeightDip))
        {
            return new PrintableAreaValidation(false, false, "imageable-area-non-finite");
        }

        if (originXDip < -BoundaryToleranceDip || originYDip < -BoundaryToleranceDip)
        {
            return new PrintableAreaValidation(false, false, "imageable-area-negative-origin");
        }

        if (extentWidthDip <= 0 || extentHeightDip <= 0)
        {
            return new PrintableAreaValidation(false, false, "imageable-area-non-positive-extent");
        }

        if (mediaWidthDip is not double effectiveMediaWidth
            || mediaHeightDip is not double effectiveMediaHeight
            || !IsFinite(effectiveMediaWidth)
            || !IsFinite(effectiveMediaHeight)
            || effectiveMediaWidth <= 0
            || effectiveMediaHeight <= 0)
        {
            // The rectangle is geometrically usable, but containment cannot be
            // certified when the effective media dimensions are absent.
            return new PrintableAreaValidation(true, false, "imageable-area-media-unverified");
        }

        var right = originXDip + extentWidthDip;
        var bottom = originYDip + extentHeightDip;
        if (!IsFinite(right) || !IsFinite(bottom))
        {
            return new PrintableAreaValidation(false, false, "imageable-area-overflow");
        }

        if (right > effectiveMediaWidth + BoundaryToleranceDip
            || bottom > effectiveMediaHeight + BoundaryToleranceDip)
        {
            return new PrintableAreaValidation(false, false, "imageable-area-outside-media");
        }

        return new PrintableAreaValidation(true, true, string.Empty);
    }

    private static bool IsFinite(double value) => double.IsFinite(value);
}

public readonly record struct PrintableAreaValidation(
    bool HasUsableGeometry,
    bool IsVerified,
    string FailureCode)
{
    public string UserFacingMessage => IsVerified
        ? "verified"
        : string.IsNullOrWhiteSpace(FailureCode)
            ? "unverified"
            : FailureCode.Replace('-', ' ');

    public override string ToString()
        => $"{(IsVerified ? "verified" : HasUsableGeometry ? "usable" : "invalid")}:{FailureCode}";
}
