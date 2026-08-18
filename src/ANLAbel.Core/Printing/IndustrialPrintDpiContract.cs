namespace ANLAbel.Core.Printing;

/// <summary>
/// Authored thermal DPI. Office/screen values (72/96/150) fail closed.
/// Driver-reported non-square plans stay on <see cref="EffectiveDpiContract"/>.
/// </summary>
public static class IndustrialPrintDpiContract
{
    public static bool IsOfficeDpi(int dpi) => dpi is 72 or 96 or 150;

    public static bool IsIndustrialDpi(int dpi)
        => dpi is 152 or 203 or 300 or 305 or 600 or 609 or 1200;

    public static IndustrialDpiDecision Evaluate(int profileDpi, int templateDpi)
    {
        var dpi = profileDpi > 0 ? profileDpi : templateDpi;
        if (dpi <= 0)
        {
            return IndustrialDpiDecision.Blocked(
                "Print DPI must be a positive industrial value (203, 300, 305, 600, or 609).");
        }

        if (IsOfficeDpi(dpi))
        {
            return IndustrialDpiDecision.Blocked(
                "72/96/150 DPI is office/screen resolution, not thermal print DPI. Choose 203, 300, 305, 600, or 609 in Printer Setup.");
        }

        if (!IsIndustrialDpi(dpi))
        {
            return IndustrialDpiDecision.Blocked(
                $"DPI {dpi} is not a known industrial thermal resolution. Choose 203, 300, 305, 600, or 609 in Printer Setup.");
        }

        return IndustrialDpiDecision.Allowed;
    }
}

public readonly record struct IndustrialDpiDecision(bool IsAllowed, string Diagnostic)
{
    public static IndustrialDpiDecision Allowed { get; } = new(true, string.Empty);

    public static IndustrialDpiDecision Blocked(string diagnostic)
        => new(false, diagnostic);
}
