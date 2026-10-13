using System.Collections.Immutable;
using System.Text;
using ANLAbel.Core.Data;

namespace ANLAbel.Data.Automation;

public sealed record AutomationSourceParseResult(IReadOnlyList<DataRecord> Records, IReadOnlyList<string> Diagnostics);

/// <summary>
/// Explicit UTF-8 header CSV parser for a future automation binding. Parsing is
/// isolated from claim, template selection, manifest, queue and dispatch.
/// The intake is untrusted, so parsing is bounded (byte size, record count,
/// scanned-record count, field-count-per-row, per-field length, per-record length,
/// diagnostics count) and fails closed on invalid encoding.
///
/// The byte-size bound (<see cref="MaxInputBytes"/>) is enforced by the buffered
/// file-drop caller before <see cref="Parse"/> is invoked. <see cref="Parse"/>
/// nevertheless enforces its own internal, defense-in-depth bounds so it is safe
/// even when called directly on an arbitrary (possibly unbounded, lazily generated)
/// stream: no single field, record, or malformed/blank-row flood can grow memory or
/// CPU without limit.
/// </summary>
public static class CsvAutomationSourceParser
{
    /// <summary>Maximum permitted size, in bytes, of an untrusted CSV source. Enforced while buffering.</summary>
    public const long MaxInputBytes = 50_000_000;

    /// <summary>Maximum number of accepted data rows processed from an untrusted CSV source.</summary>
    public const int MaxRecords = 1_000_000;

    /// <summary>
    /// Maximum number of physical records consumed from the source (accepted, blank, or malformed).
    /// Bounds time/CPU independent of the upstream byte cap so a flood of blank/malformed rows
    /// cannot be scanned indefinitely.
    /// </summary>
    public const int MaxScannedRecords = 2_000_000;

    /// <summary>Maximum number of fields allowed per row (including the header row).</summary>
    public const int MaxFieldsPerRow = 4_096;

    /// <summary>
    /// Maximum number of characters permitted in a single field. Bounds StringBuilder growth so a
    /// giant single quoted field (e.g. gigabytes with no delimiter) fails closed instead of allocating.
    /// </summary>
    public const int MaxFieldLength = 1_000_000;

    /// <summary>
    /// Maximum number of accumulated characters permitted in a single record (across all fields).
    /// Bounds StringBuilder growth for a monster line that has no newline but many fields.
    /// </summary>
    public const int MaxRecordLength = 10_000_000;

    /// <summary>Maximum number of accumulated diagnostics before the list is capped and summarized.</summary>
    public const int MaxDiagnostics = 100;

    public static AutomationSourceParseResult Parse(Stream stream)
    {
        // GAP 3: StreamReader's built-in BOM auto-detection swaps in a decoder that uses the default
        // (silent U+FFFD replacement) fallback for the BOM-detected encoding, so only UTF-8 failed
        // closed on invalid bytes. Detect the BOM ourselves and construct an encoding whose decoder
        // throws on invalid byte sequences regardless of the detected encoding.
        var (encoding, encodingName, decodeStream) = ResolveEncoding(stream);
        using var reader = new StreamReader(decodeStream, encoding, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
        var currentPhysicalLine = 1;

        var diagnostics = new List<string>();
        var suppressedDiagnostics = 0;

        void AddDiagnostic(string message)
        {
            if (diagnostics.Count < MaxDiagnostics) diagnostics.Add(message);
            else suppressedDiagnostics++;
        }

        List<DataRecord> records;
        try
        {
            List<string>? headerRecord;
            try
            {
                headerRecord = ReadRecord(reader, ref currentPhysicalLine);
            }
            catch (FormatException ex)
            {
                return new([], [$"CSV header is malformed: {ex.Message}"]);
            }
            if (headerRecord is null || headerRecord.Count == 0)
            {
                return new([], ["CSV source is empty."]);
            }

            var headers = headerRecord;
            if (headers.Any(string.IsNullOrWhiteSpace))
            {
                return new([], ["CSV header contains an empty field name."]);
            }

            if (headers.Distinct(StringComparer.OrdinalIgnoreCase).Count() != headers.Count)
            {
                return new([], ["CSV header contains duplicate field names."]);
            }

            records = new List<DataRecord>();
            var processedRows = 0;
            var scannedRows = 0;

            while (true)
            {
                var recordStartLine = currentPhysicalLine;
                List<string>? values;
                try
                {
                    values = ReadRecord(reader, ref currentPhysicalLine);
                }
                catch (FormatException ex)
                {
                    // GAP 2: a malformed physical record still consumes work; count it toward the scan bound.
                    if (++scannedRows > MaxScannedRecords)
                    {
                        AddDiagnostic($"CSV source exceeds the maximum of {MaxScannedRecords} scanned record(s).");
                        break;
                    }
                    AddDiagnostic($"CSV row {recordStartLine}: {ex.Message}");
                    continue;
                }

                if (values is null) break;

                // GAP 2: every physical record consumed (accepted, blank, or malformed) counts toward
                // the scan bound so time/CPU is bounded even without the upstream byte cap.
                if (++scannedRows > MaxScannedRecords)
                {
                    AddDiagnostic($"CSV source exceeds the maximum of {MaxScannedRecords} scanned record(s).");
                    break;
                }

                if (values.Count == 1 && string.IsNullOrEmpty(values[0]))
                {
                    // Blank line. For multi-column sources this is standard trailing/blank-line
                    // skipping. For a single-column source it is ambiguous and would silently drop
                    // a record, so fail closed with a diagnostic (FIX 3 / fail-closed policy).
                    if (headers.Count == 1)
                    {
                        AddDiagnostic($"CSV row {recordStartLine} is blank in a single-column source; blank rows are not silently dropped.");
                    }
                    continue;
                }

                if (++processedRows > MaxRecords)
                {
                    AddDiagnostic($"CSV source exceeds the maximum of {MaxRecords} record(s).");
                    break;
                }

                if (values.Count != headers.Count)
                {
                    AddDiagnostic($"CSV row {recordStartLine} has {values.Count} field(s); expected {headers.Count}.");
                    continue;
                }

                var row = headers.Select((header, index) => new KeyValuePair<string, string?>(header, values[index]))
                                 .ToImmutableDictionary(StringComparer.OrdinalIgnoreCase);
                records.Add(new DataRecord(row));
            }
        }
        catch (DecoderFallbackException)
        {
            return new([], [$"CSV source contains invalid {encodingName} byte sequences; the source was rejected without corrupting field values."]);
        }

        if (suppressedDiagnostics > 0)
        {
            diagnostics.Add($"…and {suppressedDiagnostics} more diagnostic(s) were suppressed.");
        }

        return new(records, diagnostics);
    }

    /// <summary>
    /// Detects a byte-order mark at the head of <paramref name="stream"/> and returns an encoding whose
    /// decoder throws (<see cref="DecoderFallbackException"/>) on invalid byte sequences, plus a stream
    /// with the BOM stripped so the returned encoding can be used with BOM auto-detection disabled.
    /// When no BOM is present the source is treated as strict UTF-8.
    /// </summary>
    private static (Encoding encoding, string name, Stream stream) ResolveEncoding(Stream stream)
    {
        var head = new byte[4];
        var read = 0;
        while (read < head.Length)
        {
            var r = stream.Read(head, read, head.Length - read);
            if (r == 0) break;
            read += r;
        }

        Encoding encoding;
        string name;
        int bomLength;

        if (read >= 4 && head[0] == 0xFF && head[1] == 0xFE && head[2] == 0x00 && head[3] == 0x00)
        {
            encoding = new UTF32Encoding(bigEndian: false, byteOrderMark: false, throwOnInvalidCharacters: true);
            name = "UTF-32";
            bomLength = 4;
        }
        else if (read >= 4 && head[0] == 0x00 && head[1] == 0x00 && head[2] == 0xFE && head[3] == 0xFF)
        {
            encoding = new UTF32Encoding(bigEndian: true, byteOrderMark: false, throwOnInvalidCharacters: true);
            name = "UTF-32";
            bomLength = 4;
        }
        else if (read >= 2 && head[0] == 0xFF && head[1] == 0xFE)
        {
            encoding = new UnicodeEncoding(bigEndian: false, byteOrderMark: false, throwOnInvalidBytes: true);
            name = "UTF-16";
            bomLength = 2;
        }
        else if (read >= 2 && head[0] == 0xFE && head[1] == 0xFF)
        {
            encoding = new UnicodeEncoding(bigEndian: true, byteOrderMark: false, throwOnInvalidBytes: true);
            name = "UTF-16";
            bomLength = 2;
        }
        else if (read >= 3 && head[0] == 0xEF && head[1] == 0xBB && head[2] == 0xBF)
        {
            encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
            name = "UTF-8";
            bomLength = 3;
        }
        else
        {
            encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
            name = "UTF-8";
            bomLength = 0;
        }

        var leftover = new byte[read - bomLength];
        Array.Copy(head, bomLength, leftover, 0, leftover.Length);
        return (encoding, name, new PrefixedStream(leftover, stream));
    }

    /// <summary>
    /// Read-only stream that yields a small in-memory prefix (the bytes already consumed for BOM
    /// detection, minus the BOM) before delegating to the underlying source. The underlying stream is
    /// never disposed by this wrapper.
    /// </summary>
    private sealed class PrefixedStream : Stream
    {
        private readonly byte[] _prefix;
        private readonly Stream _inner;
        private int _prefixPos;

        public PrefixedStream(byte[] prefix, Stream inner)
        {
            _prefix = prefix;
            _inner = inner;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_prefixPos < _prefix.Length)
            {
                var n = Math.Min(count, _prefix.Length - _prefixPos);
                Array.Copy(_prefix, _prefixPos, buffer, offset, n);
                _prefixPos += n;
                return n;
            }
            return _inner.Read(buffer, offset, count);
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

    private static List<string>? ReadRecord(TextReader reader, ref int currentPhysicalLine)
    {
        var cells = new List<string>();
        var cell = new StringBuilder();
        var quoted = false;
        var closedQuote = false;
        var anyCharInRecord = false;
        var recordLength = 0;

        // GAP 1: bound StringBuilder growth incrementally while appending, so a single gigantic field
        // (per-field cap) or a monster line with no newline (per-record cap) fails closed for this
        // record instead of allocating without limit.
        void AppendToCell(char c)
        {
            if (cell.Length >= MaxFieldLength)
            {
                throw new FormatException($"field {cells.Count + 1} exceeds the maximum of {MaxFieldLength} character(s).");
            }
            if (recordLength >= MaxRecordLength)
            {
                throw new FormatException($"record exceeds the maximum of {MaxRecordLength} character(s).");
            }
            cell.Append(c);
            recordLength++;
        }

        while (true)
        {
            var next = reader.Read();
            if (next < 0)
            {
                if (quoted)
                {
                    throw new FormatException("unterminated quoted value.");
                }

                if (!anyCharInRecord && cells.Count == 0 && cell.Length == 0)
                {
                    return null;
                }

                cells.Add(cell.ToString());
                return cells;
            }

            anyCharInRecord = true;
            var current = (char)next;

            if (current == '\r')
            {
                if (reader.Peek() == '\n')
                {
                    reader.Read();
                    if (quoted)
                    {
                        AppendToCell('\r');
                        AppendToCell('\n');
                        currentPhysicalLine++;
                        continue;
                    }
                    currentPhysicalLine++;
                    cells.Add(cell.ToString());
                    return cells;
                }
                currentPhysicalLine++;
                if (!quoted)
                {
                    cells.Add(cell.ToString());
                    return cells;
                }
                AppendToCell('\r');
                continue;
            }

            if (current == '\n')
            {
                currentPhysicalLine++;
                if (!quoted)
                {
                    cells.Add(cell.ToString());
                    return cells;
                }
                AppendToCell('\n');
                continue;
            }

            if (current == '"')
            {
                if (closedQuote || (!quoted && cell.Length != 0))
                {
                    throw new FormatException($"Unexpected quote in field {cells.Count + 1}.");
                }

                if (quoted && reader.Peek() == '"')
                {
                    reader.Read();
                    AppendToCell('"');
                }
                else
                {
                    closedQuote = quoted;
                    quoted = !quoted;
                }
                continue;
            }

            if (!quoted && current == ',')
            {
                if (cells.Count >= MaxFieldsPerRow)
                {
                    throw new FormatException($"row exceeds the maximum of {MaxFieldsPerRow} field(s) per row.");
                }
                cells.Add(cell.ToString());
                cell.Clear();
                closedQuote = false;
                continue;
            }

            if (closedQuote)
            {
                throw new FormatException($"Unexpected character after the closing quote in field {cells.Count + 1}.");
            }

            AppendToCell(current);
        }
    }
}
