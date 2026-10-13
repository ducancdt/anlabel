using ANLAbel.Core.Automation;
using ANLAbel.Data.Automation;
using Xunit;

namespace ANLAbel.UnitTests;

/// <summary>
/// FIX 1 — the untrusted CSV intake enforces a maximum input byte size while buffering,
/// so an oversized stream is rejected (batch Blocked) before it is fully materialized.
/// </summary>
public sealed class FileDropCsvPreparationBoundsTests
{
    [Fact]
    public void TryPrepare_OversizedSource_IsBlockedBeforeBuffering()
    {
        var root = Path.Combine(Path.GetTempPath(), "anlabel-csv-bounds-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var configuration = new FileDropTriggerConfiguration("bounds-trigger", "Bounds", root, "*.csv", false, true);
            var identity = FileDropClaimContract.CreateIdentity(
                configuration.TriggerId,
                configuration.ConfigurationFingerprint,
                FileDropClaimContract.ComputeContentFingerprint(new byte[] { 1, 2, 3 }));
            var ledger = new FileDropClaimLedger(Path.Combine(root, "ledger.jsonl"));
            Assert.True(ledger.TryRecordDetection(identity, out _, out _));
            Assert.True(ledger.TryTransition(identity, FileDropEventState.Claimed, "review", out _, out _));

            var service = new FileDropCsvPreparationService(ledger);
            using var oversized = new ConstantByteStream((byte)'A', CsvAutomationSourceParser.MaxInputBytes + 1);

            var prepared = service.TryPrepare(identity, oversized, out var records, out var result);

            Assert.False(prepared);
            Assert.Empty(records);
            Assert.Contains("maximum permitted size", result, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(FileDropEventState.Blocked, ledger.ReadValid(out _).Last().To);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, true);
            }
        }
    }

    /// <summary>Read-only stream that yields a fixed byte value up to a length without allocating it all.</summary>
    private sealed class ConstantByteStream : Stream
    {
        private readonly byte _value;
        private long _remaining;
        public ConstantByteStream(byte value, long length) { _value = value; _remaining = length; }
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() { }
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_remaining <= 0) return 0;
            var toWrite = (int)Math.Min(count, _remaining);
            for (var i = 0; i < toWrite; i++) buffer[offset + i] = _value;
            _remaining -= toWrite;
            return toWrite;
        }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
