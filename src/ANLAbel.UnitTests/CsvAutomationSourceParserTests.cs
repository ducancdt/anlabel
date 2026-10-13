using System.Text;
using ANLAbel.Data.Automation;
using Xunit;

namespace ANLAbel.UnitTests;

public class CsvAutomationSourceParserTests
{
    [Fact]
    public void Parse_StandardRows_ReturnsRecords()
    {
        var csv = "Code,Name,Qty\nITEM-001,Widget,10\nITEM-002,Gadget,25\n";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var result = CsvAutomationSourceParser.Parse(stream);

        Assert.Empty(result.Diagnostics);
        Assert.Equal(2, result.Records.Count);
        Assert.Equal("ITEM-001", result.Records[0].Values["Code"]);
        Assert.Equal("Widget", result.Records[0].Values["Name"]);
        Assert.Equal("10", result.Records[0].Values["Qty"]);
        Assert.Equal("ITEM-002", result.Records[1].Values["Code"]);
    }

    [Fact]
    public void Parse_MultilineQuotedField_PreservesNewlinesAndDoesNotSplit()
    {
        var csv = "Code,Description\n\"PN-100\",\"Line 1\nLine 2\nLine 3\"\n\"PN-200\",\"Single line\"\n";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var result = CsvAutomationSourceParser.Parse(stream);

        Assert.Empty(result.Diagnostics);
        Assert.Equal(2, result.Records.Count);
        Assert.Equal("PN-100", result.Records[0].Values["Code"]);
        Assert.Equal("Line 1\nLine 2\nLine 3", result.Records[0].Values["Description"]);
        Assert.Equal("PN-200", result.Records[1].Values["Code"]);
        Assert.Equal("Single line", result.Records[1].Values["Description"]);
    }

    [Fact]
    public void Parse_EscapedQuotesAndCommas_HandledCorrectly()
    {
        var csv = "Code,Company\n\"001\",\"Acme, \"\"Special\"\" Inc.\"\n";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var result = CsvAutomationSourceParser.Parse(stream);

        Assert.Empty(result.Diagnostics);
        Assert.Single(result.Records);
        Assert.Equal("001", result.Records[0].Values["Code"]);
        Assert.Equal("Acme, \"Special\" Inc.", result.Records[0].Values["Company"]);
    }

    [Fact]
    public void Parse_UnterminatedQuote_ReportsDiagnostic()
    {
        var csv = "Code,Name\n\"001\",\"Unterminated\n";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var result = CsvAutomationSourceParser.Parse(stream);

        Assert.Empty(result.Records);
        Assert.Contains(result.Diagnostics, d => d.Contains("unterminated", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Parse_UnexpectedQuoteInField_ReportsDiagnostic()
    {
        var csv = "Code,Name\nA,One\nB,Two\"Three\n";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var result = CsvAutomationSourceParser.Parse(stream);

        Assert.Single(result.Records);
        Assert.Contains(result.Diagnostics, d => d.Contains("Unexpected quote", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Parse_CharactersAfterClosingQuote_ReportsDiagnostic()
    {
        var csv = "Code,Name\nA,One\nB,\"Two\"Three\n";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var result = CsvAutomationSourceParser.Parse(stream);

        Assert.Single(result.Records);
        Assert.Contains(result.Diagnostics, d => d.Contains("after the closing quote", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Parse_DuplicateHeaders_FailsClosed()
    {
        var csv = "PartNo,partno\n100,200\n";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var result = CsvAutomationSourceParser.Parse(stream);

        Assert.Empty(result.Records);
        Assert.Contains(result.Diagnostics, d => d.Contains("duplicate", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Parse_EmptySource_FailsClosed()
    {
        using var stream = new MemoryStream(Array.Empty<byte>());

        var result = CsvAutomationSourceParser.Parse(stream);

        Assert.Empty(result.Records);
        Assert.Contains(result.Diagnostics, d => d.Contains("empty", StringComparison.OrdinalIgnoreCase));
    }

    // FIX 1 — a row with too many fields is rejected during parsing (bounded, no unbounded growth).
    [Fact]
    public void Parse_RowExceedingMaxFields_ReportsDiagnostic()
    {
        var oversizedRow = new string(',', CsvAutomationSourceParser.MaxFieldsPerRow + 1);
        var csv = "A\n" + oversizedRow + "\n";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var result = CsvAutomationSourceParser.Parse(stream);

        Assert.Empty(result.Records);
        Assert.Contains(result.Diagnostics, d => d.Contains("maximum", StringComparison.OrdinalIgnoreCase) && d.Contains("field", StringComparison.OrdinalIgnoreCase));
    }

    // FIX 1 — the number of processed data rows is bounded and fails closed when exceeded.
    [Fact]
    public void Parse_RecordCountExceedingMax_ReportsDiagnostic()
    {
        var builder = new StringBuilder("A\n");
        for (var i = 0; i < CsvAutomationSourceParser.MaxRecords + 1; i++)
        {
            builder.Append("x\n");
        }
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(builder.ToString()));

        var result = CsvAutomationSourceParser.Parse(stream);

        Assert.Contains(result.Diagnostics, d => d.Contains("maximum", StringComparison.OrdinalIgnoreCase) && d.Contains("record", StringComparison.OrdinalIgnoreCase));
        Assert.True(result.Records.Count <= CsvAutomationSourceParser.MaxRecords);
    }

    // FIX 2 — accumulated diagnostics are capped and summarized rather than unbounded.
    [Fact]
    public void Parse_ManyMalformedRows_CapsDiagnostics()
    {
        var builder = new StringBuilder("A,B\n");
        for (var i = 0; i < CsvAutomationSourceParser.MaxDiagnostics + 150; i++)
        {
            builder.Append("x\n"); // one field, expected two -> a diagnostic per row
        }
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(builder.ToString()));

        var result = CsvAutomationSourceParser.Parse(stream);

        Assert.Equal(CsvAutomationSourceParser.MaxDiagnostics + 1, result.Diagnostics.Count);
        Assert.Contains("more", result.Diagnostics[^1], StringComparison.OrdinalIgnoreCase);
    }

    // FIX 3 — a blank row in a single-column source is not silently dropped.
    [Fact]
    public void Parse_SingleColumnBlankRow_IsNotSilentlyDropped()
    {
        var csv = "SKU\nA\n\nB\n";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var result = CsvAutomationSourceParser.Parse(stream);

        Assert.Equal(2, result.Records.Count);
        Assert.Contains(result.Diagnostics, d => d.Contains("blank", StringComparison.OrdinalIgnoreCase));
    }

    // FIX 3 — multi-column sources keep standard blank-line skipping with no diagnostic.
    [Fact]
    public void Parse_MultiColumnBlankLine_StillSkippedSilently()
    {
        var csv = "A,B\nx,y\n\n";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var result = CsvAutomationSourceParser.Parse(stream);

        Assert.Single(result.Records);
        Assert.Empty(result.Diagnostics);
    }

    // FIX 4 — invalid UTF-8 bytes fail closed instead of being silently replaced with U+FFFD.
    [Fact]
    public void Parse_InvalidUtf8_FailsClosed()
    {
        var bytes = new List<byte>();
        bytes.AddRange(Encoding.UTF8.GetBytes("Code\n"));
        bytes.Add(0xFF); // invalid stand-alone UTF-8 byte
        bytes.AddRange(Encoding.UTF8.GetBytes("\n"));
        using var stream = new MemoryStream(bytes.ToArray());

        var result = CsvAutomationSourceParser.Parse(stream);

        Assert.Empty(result.Records);
        Assert.Contains(result.Diagnostics, d => d.Contains("invalid UTF-8", StringComparison.OrdinalIgnoreCase));
    }

    // FIX 4 — BOM detection still selects UTF-16 for BOM-prefixed sources.
    [Fact]
    public void Parse_Utf16BomSource_StillParses()
    {
        var content = "Code\nA\n";
        var bytes = new byte[] { 0xFF, 0xFE }.Concat(Encoding.Unicode.GetBytes(content)).ToArray();
        using var stream = new MemoryStream(bytes);

        var result = CsvAutomationSourceParser.Parse(stream);

        Assert.Empty(result.Diagnostics);
        Assert.Single(result.Records);
        Assert.Equal("A", result.Records[0].Values["Code"]);
    }

    // GAP 1 — a single gigantic quoted field (no delimiter) is rejected by the per-field character cap
    // without allocating gigabytes. The oversized content is generated lazily by the stream, never
    // materialized in memory by the test.
    [Fact]
    public void Parse_GiantSingleField_FailsClosedWithoutAllocatingGigabytes()
    {
        // Header "A,B\n", then an opened quote followed by far more than MaxFieldLength 'x' bytes and no
        // closing quote. The per-field cap must trip before the whole field is buffered. The two-column
        // header means the small leftover after the cap trips is a field-count mismatch, not a record.
        var prefix = Encoding.UTF8.GetBytes("A,B\n\"");
        var overflow = (long)CsvAutomationSourceParser.MaxFieldLength + 5;
        using var stream = new RepeatingByteStream(prefix, (byte)'x', overflow);

        var result = CsvAutomationSourceParser.Parse(stream);

        Assert.Empty(result.Records);
        Assert.Contains(result.Diagnostics, d =>
            d.Contains("maximum", StringComparison.OrdinalIgnoreCase) &&
            d.Contains("character", StringComparison.OrdinalIgnoreCase));
    }

    // GAP 2 — a flood of blank rows (which previously "continue"d before any bound) is bounded by the
    // scanned-record cap so CPU/time is limited even without the upstream byte cap. Blank lines produce
    // no per-row diagnostic in a multi-column source, so the scanned-cap diagnostic is observable.
    [Fact]
    public void Parse_BlankRowFlood_BoundedByScannedCap()
    {
        var prefix = Encoding.UTF8.GetBytes("A,B\n");
        var lineCount = (long)CsvAutomationSourceParser.MaxScannedRecords + 5;
        using var stream = new RepeatingByteStream(prefix, (byte)'\n', lineCount);

        var result = CsvAutomationSourceParser.Parse(stream);

        Assert.Empty(result.Records);
        Assert.Contains(result.Diagnostics, d => d.Contains("scanned", StringComparison.OrdinalIgnoreCase));
    }

    // GAP 3 — a valid UTF-16 (LE BOM) source parses correctly with the fail-closed decoder.
    [Fact]
    public void Parse_ValidUtf16BeBom_Parses()
    {
        var content = "Code,Name\nA,One\n";
        var bytes = new byte[] { 0xFE, 0xFF }.Concat(Encoding.BigEndianUnicode.GetBytes(content)).ToArray();
        using var stream = new MemoryStream(bytes);

        var result = CsvAutomationSourceParser.Parse(stream);

        Assert.Empty(result.Diagnostics);
        Assert.Single(result.Records);
        Assert.Equal("A", result.Records[0].Values["Code"]);
        Assert.Equal("One", result.Records[0].Values["Name"]);
    }

    // GAP 3 — invalid bytes after a UTF-16 BOM fail closed (no silent U+FFFD), matching UTF-8 behavior.
    [Fact]
    public void Parse_InvalidUtf16AfterBom_FailsClosed()
    {
        var bytes = new List<byte> { 0xFF, 0xFE }; // UTF-16 LE BOM
        bytes.AddRange(Encoding.Unicode.GetBytes("Code\n"));
        bytes.AddRange(new byte[] { 0x00, 0xD8 }); // unpaired high surrogate -> ill-formed UTF-16
        using var stream = new MemoryStream(bytes.ToArray());

        var result = CsvAutomationSourceParser.Parse(stream);

        Assert.Empty(result.Records);
        Assert.Contains(result.Diagnostics, d =>
            d.Contains("invalid", StringComparison.OrdinalIgnoreCase) &&
            d.Contains("UTF-16", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Read-only stream that yields a fixed prefix then repeats a single byte a given number of times,
    /// generating content lazily so tests can exercise multi-megabyte / multi-million-record inputs
    /// without ever allocating the full payload.
    /// </summary>
    private sealed class RepeatingByteStream : Stream
    {
        private readonly byte[] _prefix;
        private readonly byte _repeated;
        private readonly long _repeatCount;
        private int _prefixPos;
        private long _repeatPos;

        public RepeatingByteStream(byte[] prefix, byte repeated, long repeatCount)
        {
            _prefix = prefix;
            _repeated = repeated;
            _repeatCount = repeatCount;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var written = 0;
            while (written < count && _prefixPos < _prefix.Length)
            {
                buffer[offset + written] = _prefix[_prefixPos++];
                written++;
            }
            while (written < count && _repeatPos < _repeatCount)
            {
                buffer[offset + written] = _repeated;
                _repeatPos++;
                written++;
            }
            return written;
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
