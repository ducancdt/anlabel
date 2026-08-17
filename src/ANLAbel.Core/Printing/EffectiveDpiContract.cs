namespace ANLAbel.Core.Printing;

/// <summary>
/// Validates the effective printer resolution before a render plan is
/// dispatched.  The values are printer dots per inch, not WPF DIPs.
/// </summary>
public static class EffectiveDpiContract
{
    // Higher values are not supported by the current WPF/vector pipeline and
    // can overflow dot-size calculations.  This still covers common thermal,
    // office and industrial devices (203/300/305/600/609/1200 DPI).
    public const int MaximumSupportedDpi = 2400;

    public static EffectiveDpiValidation Validate(int dpiX, int dpiY)
    {
        if (dpiX <= 0 || dpiY <= 0)
        {
            return new EffectiveDpiValidation(false, "effective-dpi-non-positive");
        }

        if (dpiX > MaximumSupportedDpi || dpiY > MaximumSupportedDpi)
        {
            return new EffectiveDpiValidation(false, "effective-dpi-out-of-range");
        }

        return new EffectiveDpiValidation(true, string.Empty);
    }
}

public readonly record struct EffectiveDpiValidation(bool IsValid, string FailureCode);
