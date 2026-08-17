namespace ANLAbel.Core.Geometry;

/// <summary>
/// One contiguous dark run in a linear barcode after it has been mapped to the
/// effective printer's integer dot grid.
/// </summary>
public readonly record struct DeviceBarcodeDarkRun(int StartDot, int WidthDots)
{
    public int EndDotExclusive => StartDot + WidthDots;
}

/// <summary>
/// Platform-neutral layout for a vector barcode. The WPF renderer consumes
/// this value object and only converts the resulting dot coordinates back to
/// DIP for drawing. Keeping the layout here makes X/Y DPI, boundary rounding
/// and dark-run coverage testable without a DrawingContext.
/// </summary>
public sealed class DeviceBarcodeLayout
{
    private DeviceBarcodeLayout(
        int leftDot,
        int topDot,
        int widthDots,
        int heightDots,
        IReadOnlyList<DeviceBarcodeDarkRun> darkRuns)
    {
        LeftDot = leftDot;
        TopDot = topDot;
        WidthDots = widthDots;
        HeightDots = heightDots;
        DarkRuns = darkRuns;
    }

    public int LeftDot { get; }
    public int TopDot { get; }
    public int WidthDots { get; }
    public int HeightDots { get; }
    public IReadOnlyList<DeviceBarcodeDarkRun> DarkRuns { get; }

    public static DeviceBarcodeLayout Create(
        double leftDip,
        double topDip,
        double widthDip,
        double heightDip,
        int dpiX,
        int dpiY,
        int totalModules,
        IReadOnlyList<bool> rowBits)
    {
        ArgumentNullException.ThrowIfNull(rowBits);
        if (totalModules <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(totalModules), totalModules, "Barcode module count must be positive.");
        }

        if (rowBits.Count < totalModules)
        {
            throw new ArgumentException("Barcode bit data is shorter than the declared module count.", nameof(rowBits));
        }

        if (!double.IsFinite(widthDip) || widthDip <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(widthDip), widthDip, "Barcode width must be finite and positive.");
        }

        if (!double.IsFinite(heightDip) || heightDip <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(heightDip), heightDip, "Barcode height must be finite and positive.");
        }

        var leftDot = DeviceDotQuantizer.DipToDots(leftDip, dpiX);
        var rightDot = DeviceDotQuantizer.DipToDots(leftDip + widthDip, dpiX);
        if (rightDot <= leftDot)
        {
            rightDot = checked(leftDot + 1);
        }

        var topDot = DeviceDotQuantizer.DipToDots(topDip, dpiY);
        var bottomDot = DeviceDotQuantizer.DipToDots(topDip + heightDip, dpiY);
        if (bottomDot <= topDot)
        {
            bottomDot = checked(topDot + 1);
        }

        var widthDots = checked(rightDot - leftDot);
        var heightDots = checked(bottomDot - topDot);
        var darkRuns = new List<DeviceBarcodeDarkRun>();
        var moduleIndex = 0;
        while (moduleIndex < totalModules)
        {
            if (!rowBits[moduleIndex])
            {
                moduleIndex++;
                continue;
            }

            var startModule = moduleIndex;
            while (moduleIndex < totalModules && rowBits[moduleIndex])
            {
                moduleIndex++;
            }

            var startDot = Math.Clamp(
                DeviceDotQuantizer.QuantizeModuleBoundary(startModule, totalModules, widthDots),
                0,
                widthDots);
            var endDot = Math.Clamp(
                DeviceDotQuantizer.QuantizeModuleBoundary(moduleIndex, totalModules, widthDots),
                0,
                widthDots);
            if (startDot >= widthDots)
            {
                continue;
            }

            var runWidth = Math.Min(widthDots - startDot, Math.Max(1, endDot - startDot));
            var candidate = new DeviceBarcodeDarkRun(startDot, runWidth);
            if (darkRuns.Count > 0 && candidate.StartDot <= darkRuns[^1].EndDotExclusive)
            {
                // When a module is smaller than one device dot, two logical
                // dark runs can collapse onto the same boundary. Merge them
                // instead of emitting overlapping WPF rectangles; preflight
                // still reports the undersized module separately.
                var previous = darkRuns[^1];
                var mergedEnd = Math.Max(previous.EndDotExclusive, candidate.EndDotExclusive);
                darkRuns[^1] = new DeviceBarcodeDarkRun(
                    previous.StartDot,
                    mergedEnd - previous.StartDot);
            }
            else
            {
                darkRuns.Add(candidate);
            }
        }

        return new DeviceBarcodeLayout(
            leftDot,
            topDot,
            widthDots,
            heightDots,
            darkRuns.AsReadOnly());
    }
}
