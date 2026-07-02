namespace ANLAbel.Core.Geometry;

public static class MmConverter
{
    public const double MillimetersPerInch = 25.4;
    public const double WpfDpi = 96.0;

    public static double MmToDip(double mm)
    {
        return mm / MillimetersPerInch * WpfDpi;
    }

    public static double DipToMm(double dip)
    {
        return dip / WpfDpi * MillimetersPerInch;
    }

    public static int MmToPrinterDots(double mm, int dpi)
    {
        ValidatePrinterDpi(dpi);
        return (int)Math.Round(mm / MillimetersPerInch * dpi, MidpointRounding.AwayFromZero);
    }

    public static double PrinterDotsToMm(int dots, int dpi)
    {
        ValidatePrinterDpi(dpi);
        return dots * MillimetersPerInch / dpi;
    }

    private static void ValidatePrinterDpi(int dpi)
    {
        if (dpi <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(dpi), dpi, "Printer DPI must be greater than zero.");
        }
    }
}
