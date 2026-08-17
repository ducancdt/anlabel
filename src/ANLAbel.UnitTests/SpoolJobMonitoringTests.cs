using ANLAbel.Core.Printing;
using Xunit;

namespace ANLAbel.UnitTests;

public sealed class SpoolJobMonitoringTests
{
    [Fact]
    public async Task StopsOnTerminalQueueStateWithoutClaimingPhysicalOutput()
    {
        var reader = new SequenceReader(
            Observation(SpoolJobState.Pending, terminal: false),
            Observation(SpoolJobState.Printing, terminal: false),
            Observation(SpoolJobState.Completed, terminal: true));
        var progress = new List<SpoolJobObservation>();

        var result = await new SpoolJobMonitor(reader).MonitorAsync(
            "Zebra-01",
            42,
            TimeSpan.FromSeconds(1),
            TimeSpan.Zero,
            progress: new InlineProgress<SpoolJobObservation>(progress.Add));

        Assert.False(result.TimedOut);
        Assert.Equal(SpoolJobState.Completed, result.FinalObservation.State);
        Assert.Equal(3, result.PollCount);
        Assert.Equal(3, progress.Count);
        Assert.False(result.PhysicalOutputVerified);
        Assert.Contains("not verified", result.UserFacingStatus, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TimeoutReturnsUnknownInsteadOfFailureOrSuccess()
    {
        var reader = new SequenceReader(new SpoolJobObservation(
            "Zebra-01",
            43,
            SpoolJobState.Printing,
            IsTerminal: false));

        var result = await new SpoolJobMonitor(reader).MonitorAsync(
            "Zebra-01",
            43,
            TimeSpan.FromMilliseconds(35),
            TimeSpan.FromMilliseconds(2));

        Assert.True(result.TimedOut);
        Assert.Equal(SpoolJobState.Unknown, result.FinalObservation.State);
        Assert.False(result.FinalObservation.IsTerminal);
        Assert.True(result.PollCount > 0);
        Assert.False(result.PhysicalOutputVerified);
    }

    [Fact]
    public async Task CancellationStopsPollingWithoutConvertingToUnknown()
    {
        var reader = new BlockingReader();
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(20));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new SpoolJobMonitor(reader).MonitorAsync(
            "Zebra-01",
            44,
            TimeSpan.FromSeconds(1),
            TimeSpan.FromMilliseconds(5),
            cancellation.Token));

        Assert.Equal(1, reader.ReadCount);
    }

    [Fact]
    public async Task IdentityMismatchFailsClosedBeforeAnotherPoll()
    {
        var reader = new SequenceReader(new SpoolJobObservation(
            "Other-Queue",
            99,
            SpoolJobState.Printing,
            IsTerminal: false));

        var result = await new SpoolJobMonitor(reader).MonitorAsync(
            "Zebra-01",
            45,
            TimeSpan.FromSeconds(1),
            TimeSpan.Zero);

        Assert.Equal(SpoolJobState.Unknown, result.FinalObservation.State);
        Assert.True(result.FinalObservation.IsTerminal);
        Assert.Equal(1, result.PollCount);
        Assert.Contains("different printer/job identity", result.FinalObservation.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SpoolerRestartReaderFaultFailsClosedAsUnknown()
    {
        var result = await new SpoolJobMonitor(new FaultingReader()).MonitorAsync(
            "Zebra-01",
            46,
            TimeSpan.FromSeconds(1),
            TimeSpan.Zero);

        Assert.False(result.TimedOut);
        Assert.Equal(SpoolJobState.Unknown, result.FinalObservation.State);
        Assert.True(result.FinalObservation.IsTerminal);
        Assert.Contains("spool-status reader failed", result.FinalObservation.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("reader failed", result.UserFacingStatus, StringComparison.OrdinalIgnoreCase);
        Assert.False(result.PhysicalOutputVerified);
    }

    [Theory]
    [InlineData(SpoolJobState.Offline, "offline")]
    [InlineData(SpoolJobState.PaperOut, "paper/media")]
    [InlineData(SpoolJobState.UserIntervention, "intervention")]
    public async Task DeviceFaultObservationRemainsTerminalOperatorReview(
        SpoolJobState fault,
        string expectedMessage)
    {
        var reader = new SequenceReader(new SpoolJobObservation(
            "Zebra-01",
            47,
            fault,
            $"The device reports {expectedMessage}.",
            IsTerminal: true));

        var result = await new SpoolJobMonitor(reader).MonitorAsync(
            "Zebra-01",
            47,
            TimeSpan.FromSeconds(1),
            TimeSpan.Zero);

        Assert.False(result.TimedOut);
        Assert.Equal(fault, result.FinalObservation.State);
        Assert.True(result.FinalObservation.IsTerminal);
        Assert.False(result.PhysicalOutputVerified);
        Assert.Contains(expectedMessage, result.FinalObservation.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("not verified", result.UserFacingStatus, StringComparison.OrdinalIgnoreCase);
    }

    private static SpoolJobObservation Observation(SpoolJobState state, bool terminal)
    {
        return new SpoolJobObservation("Zebra-01", 42, state, IsTerminal: terminal);
    }

    private sealed class SequenceReader : ISpoolJobStatusReader
    {
        private readonly Queue<SpoolJobObservation> _observations;
        private SpoolJobObservation _last;

        public SequenceReader(params SpoolJobObservation[] observations)
        {
            _observations = new Queue<SpoolJobObservation>(observations);
            _last = observations.LastOrDefault()
                ?? new SpoolJobObservation("Zebra-01", 1, SpoolJobState.Unknown, IsTerminal: true);
        }

        public int ReadCount { get; private set; }

        public ValueTask<SpoolJobObservation> ReadAsync(string printerName, int spoolJobId, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ReadCount++;
            if (_observations.Count > 0)
            {
                _last = _observations.Dequeue();
            }

            return ValueTask.FromResult(_last);
        }
    }

    private sealed class BlockingReader : ISpoolJobStatusReader
    {
        public int ReadCount { get; private set; }

        public async ValueTask<SpoolJobObservation> ReadAsync(string printerName, int spoolJobId, CancellationToken cancellationToken = default)
        {
            ReadCount++;
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return new SpoolJobObservation(printerName, spoolJobId, SpoolJobState.Unknown, IsTerminal: true);
        }
    }

    private sealed class FaultingReader : ISpoolJobStatusReader
    {
        public ValueTask<SpoolJobObservation> ReadAsync(
            string printerName,
            int spoolJobId,
            CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("The local spooler is restarting.");
        }
    }

    private sealed class InlineProgress<T>(Action<T> handler) : IProgress<T>
    {
        public void Report(T value) => handler(value);
    }
}
