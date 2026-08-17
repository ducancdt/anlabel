using ANLAbel.Core.Geometry;
using Xunit;

namespace ANLAbel.UnitTests;

public sealed class PointerFrameTelemetryTests
{
    [Fact]
    public void RingBufferKeepsRecentFramesAndComputesP95()
    {
        var telemetry = new PointerFrameTelemetry(capacity: 4);
        telemetry.Record(1, 1, 1);
        telemetry.Record(2, 1, 1);
        telemetry.Record(3, 1, 1);
        telemetry.Record(4, 1, 1);
        telemetry.Record(20, 1, 1);

        var snapshot = telemetry.Snapshot();

        Assert.Equal(5, snapshot.TotalFrames);
        Assert.Equal(4, snapshot.SampleCount);
        Assert.Equal(7.25, snapshot.AverageMilliseconds, precision: 6);
        Assert.Equal(20, snapshot.P95Milliseconds, precision: 6);
        Assert.False(snapshot.MeetsBudget(16.667));
    }

    [Fact]
    public void SnapshotCanFilterByZoomAndDisplayScale()
    {
        var telemetry = new PointerFrameTelemetry();
        telemetry.Record(4, 0.25, 1.25);
        telemetry.Record(6, 0.25, 1.25);
        telemetry.Record(30, 2, 2);

        var snapshot = telemetry.Snapshot(0.25, 1.25);

        Assert.Equal(3, snapshot.TotalFrames);
        Assert.Equal(2, snapshot.SampleCount);
        Assert.Equal(5, snapshot.AverageMilliseconds, precision: 6);
        Assert.Equal(6, snapshot.P95Milliseconds, precision: 6);
        Assert.Equal(0.25, snapshot.Zoom!.Value, precision: 6);
        Assert.Equal(1.25, snapshot.PixelsPerDip!.Value, precision: 6);
        Assert.True(snapshot.MeetsBudget());
    }

    [Fact]
    public void InvalidValuesAreBoundedAndResettable()
    {
        var telemetry = new PointerFrameTelemetry(capacity: 2);
        telemetry.Record(double.NaN, double.NaN, double.NaN);
        telemetry.Record(-1, 0.01, 100);

        Assert.False(telemetry.Snapshot().HasSamples);
        Assert.Equal(1, PointerFrameTelemetry.NormalizePixelsPerDip(double.NaN), precision: 6);
        Assert.Equal(PointerFrameTelemetry.MaximumPixelsPerDip, PointerFrameTelemetry.NormalizePixelsPerDip(100), precision: 6);

        telemetry.Record(3, 1, 1);
        telemetry.Reset();
        Assert.Equal(0, telemetry.TotalFrames);
        Assert.False(telemetry.Snapshot().HasSamples);
    }
}
