namespace ANLAbel.Core.Geometry;

/// <summary>
/// Bounded, allocation-free-on-recording telemetry for the designer pointer
/// path. The canvas records one sample after each committed preview frame;
/// snapshots are intentionally explicit so percentile calculation never runs
/// inside a pointer event.
/// </summary>
public sealed class PointerFrameTelemetry
{
    public const int DefaultCapacity = 256;
    public const int MaximumCapacity = 4096;
    public const double DefaultFrameBudgetMilliseconds = 16.667;
    public const double MinimumPixelsPerDip = 0.5;
    public const double MaximumPixelsPerDip = 8.0;

    private readonly PointerFrameSample[] _samples;
    private readonly object _sync = new();
    private int _count;
    private int _nextIndex;
    private long _totalFrames;

    public PointerFrameTelemetry(int capacity = DefaultCapacity)
    {
        if (capacity < 1 || capacity > MaximumCapacity)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity), capacity, $"Capacity must be between 1 and {MaximumCapacity}.");
        }

        _samples = new PointerFrameSample[capacity];
    }

    public int Capacity => _samples.Length;

    public long TotalFrames
    {
        get
        {
            lock (_sync)
            {
                return _totalFrames;
            }
        }
    }

    public void Record(TimeSpan elapsed, double zoom, double pixelsPerDip)
    {
        if (elapsed < TimeSpan.Zero)
        {
            return;
        }

        Record(elapsed.TotalMilliseconds, zoom, pixelsPerDip);
    }

    public void Record(double elapsedMilliseconds, double zoom, double pixelsPerDip)
    {
        if (!double.IsFinite(elapsedMilliseconds) || elapsedMilliseconds < 0)
        {
            return;
        }

        var sample = new PointerFrameSample(
            elapsedMilliseconds,
            SnapToleranceContract.NormalizeZoom(zoom),
            NormalizePixelsPerDip(pixelsPerDip));

        lock (_sync)
        {
            _samples[_nextIndex] = sample;
            _nextIndex = (_nextIndex + 1) % _samples.Length;
            _count = Math.Min(_count + 1, _samples.Length);
            _totalFrames++;
        }
    }

    public PointerFrameTelemetrySnapshot Snapshot()
        => Snapshot(zoom: null, pixelsPerDip: null);

    public PointerFrameTelemetrySnapshot Snapshot(double zoom, double pixelsPerDip)
        => Snapshot(
            (double?)SnapToleranceContract.NormalizeZoom(zoom),
            (double?)NormalizePixelsPerDip(pixelsPerDip));

    public PointerFrameTelemetrySnapshot Snapshot(double? zoom, double? pixelsPerDip)
    {
        var normalizedZoom = zoom is double requestedZoom
            ? SnapToleranceContract.NormalizeZoom(requestedZoom)
            : (double?)null;
        var normalizedPixelsPerDip = pixelsPerDip is double requestedPixelsPerDip
            ? NormalizePixelsPerDip(requestedPixelsPerDip)
            : (double?)null;

        PointerFrameSample[] selected;
        lock (_sync)
        {
            selected = new PointerFrameSample[_count];
            var selectedCount = 0;
            var oldestIndex = (_nextIndex - _count + _samples.Length) % _samples.Length;
            for (var i = 0; i < _count; i++)
            {
                var sample = _samples[(oldestIndex + i) % _samples.Length];
                if (normalizedZoom is double filterZoom && Math.Abs(sample.Zoom - filterZoom) > 0.0001)
                {
                    continue;
                }

                if (normalizedPixelsPerDip is double filterPixelsPerDip
                    && Math.Abs(sample.PixelsPerDip - filterPixelsPerDip) > 0.0001)
                {
                    continue;
                }

                selected[selectedCount++] = sample;
            }

            if (selectedCount != selected.Length)
            {
                Array.Resize(ref selected, selectedCount);
            }
        }

        if (selected.Length == 0)
        {
            return new PointerFrameTelemetrySnapshot(
                TotalFrames,
                0,
                0,
                0,
                0,
                normalizedZoom,
                normalizedPixelsPerDip);
        }

        var durations = new double[selected.Length];
        var total = 0.0;
        var maximum = 0.0;
        double? commonZoom = normalizedZoom;
        double? commonPixelsPerDip = normalizedPixelsPerDip;
        for (var i = 0; i < selected.Length; i++)
        {
            var sample = selected[i];
            durations[i] = sample.ElapsedMilliseconds;
            total += sample.ElapsedMilliseconds;
            maximum = Math.Max(maximum, sample.ElapsedMilliseconds);
            commonZoom = ResolveCommonValue(commonZoom, sample.Zoom);
            commonPixelsPerDip = ResolveCommonValue(commonPixelsPerDip, sample.PixelsPerDip);
        }

        Array.Sort(durations);
        var p95Index = Math.Clamp((int)Math.Ceiling(durations.Length * 0.95) - 1, 0, durations.Length - 1);
        return new PointerFrameTelemetrySnapshot(
            TotalFrames,
            selected.Length,
            total / selected.Length,
            durations[p95Index],
            maximum,
            commonZoom,
            commonPixelsPerDip);
    }

    public void Reset()
    {
        lock (_sync)
        {
            Array.Clear(_samples);
            _count = 0;
            _nextIndex = 0;
            _totalFrames = 0;
        }
    }

    public static double NormalizePixelsPerDip(double pixelsPerDip)
        => !double.IsFinite(pixelsPerDip) || pixelsPerDip <= 0
            ? 1.0
            : Math.Clamp(pixelsPerDip, MinimumPixelsPerDip, MaximumPixelsPerDip);

    private static double? ResolveCommonValue(double? current, double candidate)
    {
        if (current is null)
        {
            return null;
        }

        return Math.Abs(current.Value - candidate) <= 0.0001 ? current : null;
    }
}

public readonly record struct PointerFrameSample(
    double ElapsedMilliseconds,
    double Zoom,
    double PixelsPerDip);

public readonly record struct PointerFrameTelemetrySnapshot(
    long TotalFrames,
    int SampleCount,
    double AverageMilliseconds,
    double P95Milliseconds,
    double MaximumMilliseconds,
    double? Zoom,
    double? PixelsPerDip)
{
    public bool HasSamples => SampleCount > 0;

    public bool MeetsBudget(double budgetMilliseconds = PointerFrameTelemetry.DefaultFrameBudgetMilliseconds)
        => HasSamples
            && double.IsFinite(budgetMilliseconds)
            && budgetMilliseconds > 0
            && P95Milliseconds <= budgetMilliseconds;
}
