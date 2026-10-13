using System.Runtime.InteropServices;
using ANLAbel.Core.Printing;

namespace ANLAbel.Printing.PrinterProfiles;

internal static class Win32PrinterCapabilityReader
{
    private const ushort DcFields = 1;
    private const ushort DcMinExtent = 4;
    private const ushort DcMaxExtent = 5;
    private const int DmPaperLength = 0x00000004;
    private const int DmPaperWidth = 0x00000008;

    internal static PrinterCustomMediaRange? ReadCustomMediaRange(
        string queueName,
        out string diagnostic)
    {
        diagnostic = string.Empty;
        var fields = DeviceCapabilities(queueName, null, DcFields, IntPtr.Zero, IntPtr.Zero);
        if (fields == -1 || (fields & (DmPaperLength | DmPaperWidth)) != (DmPaperLength | DmPaperWidth))
        {
            return null;
        }

        var minimum = DeviceCapabilities(queueName, null, DcMinExtent, IntPtr.Zero, IntPtr.Zero);
        var maximum = DeviceCapabilities(queueName, null, DcMaxExtent, IntPtr.Zero, IntPtr.Zero);
        if (minimum == -1 || maximum == -1)
        {
            diagnostic = "The Win32 driver advertises custom paper fields but did not expose both size limits.";
            return null;
        }

        var range = PrinterCustomMediaRange.FromTenthsOfMillimeter(
            LowWord(minimum),
            HighWord(minimum),
            LowWord(maximum),
            HighWord(maximum));
        if (range.IsValid)
        {
            return range;
        }

        diagnostic = "The Win32 driver returned invalid custom paper size limits.";
        return null;
    }

    private static int LowWord(int value) => (int)((uint)value & 0xffff);

    private static int HighWord(int value) => (int)(((uint)value >> 16) & 0xffff);

    [DllImport("winspool.drv", EntryPoint = "DeviceCapabilitiesW", CharSet = CharSet.Unicode, ExactSpelling = true)]
    private static extern int DeviceCapabilities(
        string device,
        string? port,
        ushort capability,
        IntPtr output,
        IntPtr devMode);
}
