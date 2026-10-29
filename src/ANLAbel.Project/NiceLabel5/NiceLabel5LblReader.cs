using System.Text;

namespace ANLAbel.Project.NiceLabel5;

// Minimal, fail-closed reader for the NiceLabel 5 .lbl header.
// Evidence: docs/NICELABEL_5_DEEP_REVERSE_ENGINEERING.md sections 41 and 46.
// Contents stream: length-prefixed magic "NicePower", format version,
// three length-prefixed strings (label name, description, author), then
// six 32-bit fields in 1/100 mm; the third and fourth are label width/height.
// Anything outside that verified shape throws InvalidDataException.
public static class NiceLabel5LblReader
{
    private const int SupportedFormatVersion = 29;
    private const int MaxHeaderStringLength = 4096;

    public static NiceLabel5LblHeader ReadHeader(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        var file = buffer.ToArray();

        var compound = CompoundFile.Open(file);
        var contents = compound.ReadStream("Contents")
            ?? throw new InvalidDataException("This .lbl file has no Contents stream, so it is not a NiceLabel 5 label.");

        return ParseHeader(contents, compound.StreamNames);
    }

    /// <summary>
    /// Counts the object records stored in the file: a known object
    /// type code (text, paragraph, graphics, barcode, shape) in the
    /// six bytes in front of a rectangle that passes the same geometry
    /// cross-check used for conversion. This counts records whether
    /// or not the importer can convert them, so the operator can be
    /// told honestly how much of the label was carried over.
    /// </summary>
    public static int CountObjectRecords(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        var file = buffer.ToArray();

        var compound = CompoundFile.Open(file);
        var contents = compound.ReadStream("Contents")
            ?? throw new InvalidDataException("This .lbl file has no Contents stream, so it is not a NiceLabel 5 label.");

        var header = ParseHeader(contents, compound.StreamNames);
        var count = 0;
        for (var offset = 6; offset + 120 <= contents.Length; offset++)
        {
            var type = BitConverter.ToUInt16(contents, offset - 4);
            if (type is not (0x12D or 0x12E or 0x12F or 0x130 or 0x131)
                || BitConverter.ToUInt16(contents, offset - 2) != 0)
            {
                continue;
            }

            if (TryReadGeometry(contents, offset, header.LabelWidthHundredthsMm, header.LabelHeightHundredthsMm,
                    out _, out _, out _, out _, out _))
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>
    /// Decodes barcode object records whose full signature cross-checks:
    /// the four long rectangle fields, the unit doubles (top/width/height
    /// always, left edge or horizontal centre for the first), a literal
    /// data string with no variable mask, and a known symbology block.
    /// Records that fail any check are skipped, never guessed.
    /// </summary>
    public static IReadOnlyList<NiceLabel5BarcodeRecord> ReadBarcodeRecords(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        var file = buffer.ToArray();

        var compound = CompoundFile.Open(file);
        var contents = compound.ReadStream("Contents")
            ?? throw new InvalidDataException("This .lbl file has no Contents stream, so it is not a NiceLabel 5 label.");

        var header = ParseHeader(contents, compound.StreamNames);
        return ScanBarcodeRecords(contents, header.LabelWidthHundredthsMm, header.LabelHeightHundredthsMm);
    }

    private static NiceLabel5LblHeader ParseHeader(byte[] contents, IReadOnlyList<string> streamNames)
    {
        var magic = ReadLengthPrefixedString(contents, 0, out var afterMagic);
        if (magic != "NicePower")
        {
            throw new InvalidDataException("This .lbl file does not start with the NicePower marker, so it is not a NiceLabel 5 label.");
        }

        if (contents.Length < afterMagic + 2)
        {
            throw new InvalidDataException("This .lbl Contents stream is too short to hold a NiceLabel 5 label header.");
        }

        var version = BitConverter.ToUInt16(contents, afterMagic);
        if (version != SupportedFormatVersion)
        {
            throw new InvalidDataException($"This .lbl file uses NiceLabel format version {version}; only version {SupportedFormatVersion} has been verified, so it cannot be opened safely.");
        }

        var offset = afterMagic + 2;
        var name = ReadLengthPrefixedString(contents, offset, out offset);
        var description = ReadLengthPrefixedString(contents, offset, out offset);
        var author = ReadLengthPrefixedString(contents, offset, out offset);

        if (contents.Length < offset + 24)
        {
            throw new InvalidDataException("This .lbl Contents stream is too short to hold the NiceLabel 5 label size fields.");
        }

        var fields = new int[6];
        for (var i = 0; i < fields.Length; i++)
        {
            fields[i] = BitConverter.ToInt32(contents, offset + 4 * i);
        }

        // Fields 1-2 are stored paper/printer-area sizes, fields 3-4 the
        // label size, fields 5-6 stored offsets/gaps that may be zero.
        if (!IsPlausibleDimension(fields[0]) || !IsPlausibleDimension(fields[1])
            || !IsPlausibleDimension(fields[2]) || !IsPlausibleDimension(fields[3])
            || fields[4] is < 0 or > 50_000 || fields[5] is < 0 or > 50_000)
        {
            throw new InvalidDataException("This .lbl file uses a NiceLabel header layout variant that has not been verified, so it cannot be opened safely.");
        }

        return new NiceLabel5LblHeader(version, name, description, author, fields[2], fields[3], streamNames);
    }

    private static readonly IReadOnlyDictionary<int, string> KnownSymbologies = new Dictionary<int, string>
    {
        [1] = "Ean-13",
        [3] = "Upc-A",
        [5] = "Interleaved 2of5",
        [6] = "Code-39",
        [33] = "Code-39 full ASCII",
    };

    private static IReadOnlyDictionary<int, string> ScanVariables(byte[] contents)
    {
        // Variable definitions carry an id (1001+) followed by the stored
        // name and description strings; evidence §54-55.
        var variables = new Dictionary<int, string>();
        for (var offset = 0; offset + 6 <= contents.Length; offset++)
        {
            var id = BitConverter.ToUInt16(contents, offset);
            if (id is < 1001 or > 1099)
            {
                continue;
            }

            var nameLength = BitConverter.ToUInt16(contents, offset + 2);
            if (nameLength is < 2 or > 41
                || offset + 4 + nameLength > contents.Length
                || contents[offset + 4 + nameLength - 1] != 0)
            {
                continue;
            }

            var nameBytes = contents.AsSpan(offset + 4, nameLength - 1);
            var printable = true;
            foreach (var b in nameBytes)
            {
                if (b is < 0x20 or > 0x7E)
                {
                    printable = false;
                    break;
                }
            }

            if (!printable)
            {
                continue;
            }

            variables[id] = Encoding.ASCII.GetString(nameBytes);
        }

        return variables;
    }

    /// <summary>
    /// Decodes fixed text object records whose geometry cross-checks the
    /// same way as barcode records and whose tail is a literal content
    /// string followed by a font block. Masked (variable) text and
    /// records that are barcodes are skipped, never guessed.
    /// </summary>
    public static IReadOnlyList<NiceLabel5TextRecord> ReadTextRecords(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        var file = buffer.ToArray();

        var compound = CompoundFile.Open(file);
        var contents = compound.ReadStream("Contents")
            ?? throw new InvalidDataException("This .lbl file has no Contents stream, so it is not a NiceLabel 5 label.");

        var header = ParseHeader(contents, compound.StreamNames);
        var variables = ScanVariables(contents);
        var records = new List<NiceLabel5TextRecord>();
        for (var offset = 0; offset + 120 <= contents.Length; offset++)
        {
            if (!TryReadGeometry(contents, offset, header.LabelWidthHundredthsMm, header.LabelHeightHundredthsMm,
                    out var left, out var top, out var right, out var bottom, out var anchor))
            {
                continue;
            }

            if (!TryReadTextTail(contents, offset + 48, 0, 12, out var content, out var fontFamily, out var linkVariable, out var fontSizePt, out var isBold))
            {
                continue;
            }

            string? variableName = null;
            if (content.Contains('?'))
            {
                // Masked text converts only when the mask is a single run
                // and the record's variable link resolves to a variable
                // defined in the same file (evidence §60-62).
                var runStart = content.IndexOf('?');
                var runEnd = content.LastIndexOf('?');
                var singleRun = content[runStart..(runEnd + 1)].All(c => c == '?');
                if (!singleRun || !variables.TryGetValue(1000 + linkVariable, out variableName))
                {
                    continue;
                }
            }

            records.Add(new NiceLabel5TextRecord(content, fontFamily, left, top, right, bottom, variableName, fontSizePt, isBold, anchor));
        }

        return records;
    }

    private static bool TryReadGeometry(byte[] contents, int offset, int labelWidth, int labelHeight,
        out int left, out int top, out int right, out int bottom, out NiceLabel5HorizontalAnchor anchor)
    {
        left = BitConverter.ToInt32(contents, offset);
        top = BitConverter.ToInt32(contents, offset + 4);
        right = BitConverter.ToInt32(contents, offset + 8);
        bottom = BitConverter.ToInt32(contents, offset + 12);
        anchor = NiceLabel5HorizontalAnchor.Left;
        if (left < 0 || top < 0 || right <= left || bottom <= top || right > labelWidth || bottom > labelHeight)
        {
            return false;
        }

        var first = BitConverter.ToDouble(contents, offset + 16);
        var doubleTop = BitConverter.ToDouble(contents, offset + 24);
        var doubleWidth = BitConverter.ToDouble(contents, offset + 32);
        var doubleHeight = BitConverter.ToDouble(contents, offset + 40);
        const double tolerance = 3.0;

        // The doubles are stored in the label's measurement unit: inches
        // in most files, centimetres or millimetres in others (the unit
        // is a per-label setting; FACT.lbl stores centimetres). The long
        // fields stay authoritative; a unit only passes when top, width
        // and height all agree within tolerance, so trying every unit
        // cannot guess geometry. The first double is the horizontal
        // anchor: the left edge, centre or right edge, matching the
        // object's alignment (evidence §76).
        foreach (var factor in new[] { 2540.0, 1000.0, 100.0 })
        {
            if (Math.Abs(top - doubleTop * factor) > tolerance
                || Math.Abs(right - left - doubleWidth * factor) > tolerance
                || Math.Abs(bottom - top - doubleHeight * factor) > tolerance)
            {
                continue;
            }

            if (Math.Abs(left - first * factor) <= tolerance)
            {
                anchor = NiceLabel5HorizontalAnchor.Left;
                return true;
            }

            if (Math.Abs((left + right) / 2.0 - first * factor) <= tolerance)
            {
                anchor = NiceLabel5HorizontalAnchor.Center;
                return true;
            }

            if (Math.Abs(right - first * factor) <= tolerance)
            {
                anchor = NiceLabel5HorizontalAnchor.Right;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Decodes paragraph object records (stored type code 0x12E): the
    /// geometry cross-check plus a content string and font block, but the
    /// content sits much later in the record (after a nested sub-record)
    /// than in a plain text record. Paragraphs whose content is not a
    /// literal or single-mask string in the Contents stream (for example
    /// RTF payloads) are skipped, never guessed.
    /// </summary>
    public static IReadOnlyList<NiceLabel5TextRecord> ReadParagraphRecords(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        var file = buffer.ToArray();

        var compound = CompoundFile.Open(file);
        var contents = compound.ReadStream("Contents")
            ?? throw new InvalidDataException("This .lbl file has no Contents stream, so it is not a NiceLabel 5 label.");

        var header = ParseHeader(contents, compound.StreamNames);
        var variables = ScanVariables(contents);
        var records = new List<NiceLabel5TextRecord>();
        for (var offset = 6; offset + 120 <= contents.Length; offset++)
        {
            if (BitConverter.ToUInt16(contents, offset - 4) != 0x12E
                || BitConverter.ToUInt16(contents, offset - 2) != 0
                || !TryReadGeometry(contents, offset, header.LabelWidthHundredthsMm, header.LabelHeightHundredthsMm,
                    out var left, out var top, out var right, out var bottom, out var anchor))
            {
                continue;
            }

            if (!TryReadTextTail(contents, offset + 48, 86, 110, out var content, out var fontFamily, out var linkVariable, out var fontSizePt, out var isBold))
            {
                continue;
            }

            string? variableName = null;
            if (content.Contains('?'))
            {
                var runStart = content.IndexOf('?');
                var runEnd = content.LastIndexOf('?');
                var singleRun = content[runStart..(runEnd + 1)].All(c => c == '?');
                if (!singleRun || !variables.TryGetValue(1000 + linkVariable, out variableName))
                {
                    continue;
                }
            }

            records.Add(new NiceLabel5TextRecord(content, fontFamily, left, top, right, bottom, variableName, fontSizePt, isBold, anchor));
        }

        return records;
    }

    private static bool TryReadTextTail(byte[] contents, int afterGeometry, int minTailOffset, int maxTailOffset, out string content, out string fontFamily, out int linkVariable, out double fontSizePt, out bool isBold)
    {
        content = string.Empty;
        fontFamily = string.Empty;
        linkVariable = 0;
        fontSizePt = 0;
        isBold = false;
        for (var q = afterGeometry + minTailOffset; q <= afterGeometry + maxTailOffset && q + 6 <= contents.Length; q++)
        {
            var count = BitConverter.ToUInt16(contents, q + 4);
            if (count is < 1 or > 200 || q + 6 + count > contents.Length)
            {
                continue;
            }

            var bytes = contents.AsSpan(q + 6, count);
            if (bytes[^1] == 0)
            {
                bytes = bytes[..^1];
            }

            if (bytes.Length == 0)
            {
                continue;
            }

            // Stored text may span lines with CRLF breaks (for example
            // "PART NO.\r\n(P)"); ANLAbel's text layout normalizes
            // newlines. Other control bytes still reject the record.
            var printable = true;
            foreach (var b in bytes)
            {
                if ((b is < 0x20 or > 0x7E) && b is not (0x0D or 0x0A))
                {
                    printable = false;
                    break;
                }
            }

            if (!printable)
            {
                continue;
            }

            var text = Encoding.ASCII.GetString(bytes);
            if (text.All(c => c <= ' '))
            {
                // A candidate holding only line breaks is a misaligned
                // offset, not a text object.
                continue;
            }

            // A barcode record carries its symbology block right here;
            // those are converted by the barcode path, not as text.
            if (TryReadRecordTail(contents, afterGeometry, out _, out _, out _, out _, out _))
            {
                return false;
            }

            var dataEnd = q + 6 + count;
            for (var f = dataEnd; f + 4 <= Math.Min(contents.Length, dataEnd + 64); f++)
            {
                // The byte pair in front of the face name is 0x01 plus the
                // LOGFONT lfPitchAndFamily byte (family in the high nibble,
                // pitch in the low one): Arial stores 0x22, Courier New
                // 0x31, Times New Roman 0x12. Accepting only 0x22 silently
                // dropped every non-Arial text record (evidence §70).
                if (contents[f] != 0x01 || !IsPitchAndFamily(contents[f + 1]))
                {
                    continue;
                }

                var nameStart = f + 2;
                var nameEnd = nameStart;
                while (nameEnd < contents.Length && contents[nameEnd] is >= 0x20 and <= 0x7E)
                {
                    nameEnd++;
                }

                if (nameEnd >= contents.Length || contents[nameEnd] != 0 || nameEnd - nameStart is < 3 or > 32
                    || contents[nameStart] is not ((>= (byte)'A' and <= (byte)'Z') or (>= (byte)'a' and <= (byte)'z')))
                {
                    continue;
                }

                content = text;
                fontFamily = Encoding.ASCII.GetString(contents, nameStart, nameEnd - nameStart);
                linkVariable = BitConverter.ToUInt16(contents, q + 2);

                // LOGFONT weight sits 8 bytes after the content string;
                // only the two standard weights are trusted.
                if (dataEnd + 12 <= contents.Length)
                {
                    var weight = BitConverter.ToInt32(contents, dataEnd + 8);
                    isBold = weight == 700;
                }

                // The stored point size (tenths of a point) sits at a
                // fixed offset of 60 bytes after the content string —
                // right after the fixed-size serialized LOGFONT block,
                // whose face name field is padded, so the offset does
                // not depend on the face name length (evidence §71).
                // Out-of-range values leave the default size.
                var sizeOffset = dataEnd + 60;
                if (sizeOffset + 6 <= contents.Length
                    && BitConverter.ToInt16(contents, sizeOffset + 4) == 0)
                {
                    var tenths = BitConverter.ToInt32(contents, sizeOffset);
                    if (tenths is >= 30 and <= 1000)
                    {
                        fontSizePt = tenths / 10.0;
                    }
                }

                if (fontSizePt == 0)
                {
                    // Other faces store the size at a different offset in
                    // the variable-length block after the face name. Only
                    // a single unambiguous candidate in range is trusted;
                    // two or more candidates leave the default (§70).
                    var candidate = 0;
                    var candidates = 0;
                    for (var s = nameEnd + 1; s + 6 <= Math.Min(contents.Length, nameEnd + 70); s++)
                    {
                        var tenths = BitConverter.ToInt32(contents, s);
                        if (tenths is >= 30 and <= 1000 && BitConverter.ToInt16(contents, s + 4) == 0)
                        {
                            candidate = tenths;
                            candidates++;
                        }
                    }

                    if (candidates == 1)
                    {
                        fontSizePt = candidate / 10.0;
                    }
                }

                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Decodes rectangle/line object records: the shared geometry
    /// cross-check plus the stored object type code 0x131 in the six
    /// bytes in front of the rectangle fields (a category counter, the
    /// type code and a zero word). Records already claimed as barcodes
    /// or texts are skipped.
    /// </summary>
    public static IReadOnlyList<NiceLabel5ShapeRecord> ReadShapeRecords(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        var file = buffer.ToArray();

        var compound = CompoundFile.Open(file);
        var contents = compound.ReadStream("Contents")
            ?? throw new InvalidDataException("This .lbl file has no Contents stream, so it is not a NiceLabel 5 label.");

        var header = ParseHeader(contents, compound.StreamNames);
        var records = new List<NiceLabel5ShapeRecord>();
        for (var offset = 6; offset + 120 <= contents.Length; offset++)
        {
            if (BitConverter.ToUInt16(contents, offset - 4) != 0x131
                || BitConverter.ToUInt16(contents, offset - 2) != 0
                || !TryReadGeometry(contents, offset, header.LabelWidthHundredthsMm, header.LabelHeightHundredthsMm,
                    out var left, out var top, out var right, out var bottom, out _))
            {
                continue;
            }

            if (TryReadRecordTail(contents, offset + 48, out _, out _, out _, out _, out _)
                || TryReadTextTail(contents, offset + 48, 0, 12, out _, out _, out _, out _, out _))
            {
                continue;
            }

            records.Add(new NiceLabel5ShapeRecord(
                right - left <= 100 || bottom - top <= 100, left, top, right, bottom));
        }

        return records;
    }

    private static IReadOnlyList<NiceLabel5BarcodeRecord> ScanBarcodeRecords(byte[] contents, int labelWidth, int labelHeight)
    {
        var variables = ScanVariables(contents);
        var records = new List<NiceLabel5BarcodeRecord>();
        for (var offset = 0; offset + 120 <= contents.Length; offset++)
        {
            if (!TryReadGeometry(contents, offset, labelWidth, labelHeight,
                    out var left, out var top, out var right, out var bottom, out _))
            {
                continue;
            }

            if (!TryReadRecordTail(contents, offset + 48, out var data, out var symbologyId, out var symbologyName, out var linkKind, out var linkVariable))
            {
                continue;
            }

            string? variableName = null;
            if (data.Contains('?'))
            {
                // A masked value is only converted when it is a pure mask
                // whose variable link (variable id 1000 + link offset)
                // resolves to a variable defined in the same file. The
                // companion link field selects a data format (FindFormat),
                // not the data source, so it does not gate conversion;
                // evidence §55-58.
                if (data.Any(c => c != '?')
                    || !variables.TryGetValue(1000 + linkVariable, out variableName))
                {
                    continue;
                }
            }

            records.Add(new NiceLabel5BarcodeRecord(symbologyId, symbologyName, data, left, top, right, bottom, variableName));
        }

        return records;
    }

    private static bool TryReadRecordTail(byte[] contents, int afterGeometry, out string data, out int symbologyId, out string symbologyName, out int linkKind, out int linkVariable)
    {
        data = string.Empty;
        symbologyId = 0;
        symbologyName = string.Empty;
        linkKind = 0;
        linkVariable = 0;
        for (var q = afterGeometry; q <= afterGeometry + 10 && q + 8 <= contents.Length; q++)
        {
            var flag = BitConverter.ToUInt16(contents, q + 4);
            var count = BitConverter.ToUInt16(contents, q + 6);
            if (flag != 0 || count is < 1 or > 64 || q + 8 + count + 10 > contents.Length)
            {
                continue;
            }

            // Most records store the data right after the count field.
            // A second verified variant (the UPC-A record in FACT.lbl)
            // repeats the count as an extra word in front of the data;
            // both variants still require the exact symbology block
            // immediately after the data (evidence §73).
            var dataStart = q + 8;
            if (q + 10 + count <= contents.Length
                && BitConverter.ToUInt16(contents, q + 8) == count)
            {
                dataStart = q + 10;
            }

            var bytes = contents.AsSpan(dataStart, count);
            var printable = true;
            foreach (var b in bytes)
            {
                if (b is < 0x20 or > 0x7E)
                {
                    printable = false;
                    break;
                }
            }

            if (!printable)
            {
                continue;
            }

            var typeOffset = dataStart + count;
            if (BitConverter.ToUInt32(contents, typeOffset) != 8)
            {
                continue;
            }

            var id = BitConverter.ToUInt16(contents, typeOffset + 4);
            var nameLength = BitConverter.ToUInt16(contents, typeOffset + 6);
            if (!KnownSymbologies.TryGetValue(id, out var expected)
                || nameLength != expected.Length
                || typeOffset + 8 + nameLength >= contents.Length
                || contents[typeOffset + 8 + nameLength] != 0
                || Encoding.ASCII.GetString(contents, typeOffset + 8, nameLength) != expected)
            {
                continue;
            }

            data = Encoding.ASCII.GetString(bytes);
            symbologyId = id;
            symbologyName = expected;
            linkKind = BitConverter.ToUInt16(contents, q);
            linkVariable = BitConverter.ToUInt16(contents, q + 2);
            return true;
        }

        return false;
    }

    private static bool IsPitchAndFamily(byte value) =>
        value >> 4 is <= 5 && (value & 0x0F) is 1 or 2;

    private static bool IsPlausibleDimension(int hundredthsMm) => hundredthsMm is >= 100 and <= 50_000;

    private static string ReadLengthPrefixedString(byte[] data, int offset, out int after)
    {
        if (data.Length < offset + 2)
        {
            throw new InvalidDataException("This .lbl Contents stream is truncated.");
        }

        var length = BitConverter.ToUInt16(data, offset);
        if (length == 0 || length > MaxHeaderStringLength || data.Length < offset + 2 + length)
        {
            throw new InvalidDataException("This .lbl Contents stream is truncated.");
        }

        // Verified NiceLabel strings carry a terminating NUL inside the
        // counted length and no interior NUL; reject anything else.
        if (data[offset + 2 + length - 1] != 0 || Array.IndexOf(data, (byte)0, offset + 2, length - 1) >= 0)
        {
            throw new InvalidDataException("This .lbl file uses a NiceLabel header string layout that has not been verified, so it cannot be opened safely.");
        }

        after = offset + 2 + length;
        return Encoding.ASCII.GetString(data, offset + 2, length - 1);
    }

    /// <summary>Small read-only OLE compound-file (CFBF) reader: FAT and mini-stream chains.</summary>
    private sealed class CompoundFile
    {
        private const uint EndOfChain = 0xFFFFFFFE;
        private const uint FreeSector = 0xFFFFFFFF;

        private readonly byte[] _file;
        private readonly int _sectorSize;
        private readonly int _miniSectorSize;
        private readonly uint _miniCutoff;
        private readonly uint[] _fat;
        private readonly List<(string Name, uint StartSector, long Size)> _streams = new();
        private readonly byte[] _miniStream;
        private readonly uint[] _miniFat;

        public IReadOnlyList<string> StreamNames => _streams.Select(s => s.Name).ToArray();

        private CompoundFile(byte[] file)
        {
            _file = file;
            if (file.Length < 512 || !file.AsSpan(0, 8).SequenceEqual(new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 }))
            {
                throw new InvalidDataException("This file is not an OLE compound file, so it is not a NiceLabel 5 label.");
            }

            _sectorSize = 1 << BitConverter.ToUInt16(file, 30);
            _miniSectorSize = 1 << BitConverter.ToUInt16(file, 32);
            var numFatSectors = BitConverter.ToUInt32(file, 44);
            var firstDirectorySector = BitConverter.ToUInt32(file, 48);
            _miniCutoff = BitConverter.ToUInt32(file, 56);
            var firstMiniFatSector = BitConverter.ToUInt32(file, 60);
            var numMiniFatSectors = BitConverter.ToUInt32(file, 64);

            var fatSectorIds = new List<uint>();
            for (var i = 0; i < 109 && fatSectorIds.Count < numFatSectors; i++)
            {
                var id = BitConverter.ToUInt32(file, 76 + 4 * i);
                if (id != FreeSector)
                {
                    fatSectorIds.Add(id);
                }
            }

            if (fatSectorIds.Count < numFatSectors)
            {
                throw new InvalidDataException("This .lbl file uses an extended FAT layout that is not supported.");
            }

            var entriesPerFat = _sectorSize / 4;
            _fat = new uint[fatSectorIds.Count * entriesPerFat];
            for (var s = 0; s < fatSectorIds.Count; s++)
            {
                var offset = SectorOffset(fatSectorIds[s]);
                for (var i = 0; i < entriesPerFat; i++)
                {
                    _fat[s * entriesPerFat + i] = BitConverter.ToUInt32(file, offset + 4 * i);
                }
            }

            var directoryBytes = ReadChain(firstDirectorySector, _fat);
            (uint RootStart, long RootSize) root = default;
            for (var offset = 0; offset + 128 <= directoryBytes.Length; offset += 128)
            {
                var nameLength = BitConverter.ToUInt16(directoryBytes, offset + 64);
                if (nameLength < 2)
                {
                    continue;
                }

                var name = Encoding.Unicode.GetString(directoryBytes, offset, nameLength - 2);
                var type = directoryBytes[offset + 66];
                var start = BitConverter.ToUInt32(directoryBytes, offset + 116);
                var size = BitConverter.ToInt64(directoryBytes, offset + 120);
                if (type == 5)
                {
                    root = (start, size);
                }
                else if (type == 2)
                {
                    _streams.Add((name, start, size));
                }
            }

            _miniStream = root.RootSize > 0 ? ReadChain(root.RootStart, _fat, root.RootSize) : [];
            var miniFatBytes = numMiniFatSectors > 0 ? ReadChain(firstMiniFatSector, _fat) : [];
            _miniFat = new uint[miniFatBytes.Length / 4];
            for (var i = 0; i < _miniFat.Length; i++)
            {
                _miniFat[i] = BitConverter.ToUInt32(miniFatBytes, 4 * i);
            }
        }

        public static CompoundFile Open(byte[] file) => new(file);

        public byte[]? ReadStream(string name)
        {
            var entry = _streams.FirstOrDefault(s => string.Equals(s.Name, name, StringComparison.Ordinal));
            if (entry.Name is null)
            {
                return null;
            }

            return entry.Size < _miniCutoff
                ? ReadChain(entry.StartSector, _miniFat, entry.Size, _miniSectorSize, _miniStream)
                : ReadChain(entry.StartSector, _fat, entry.Size);
        }

        private int SectorOffset(uint sectorId) => 512 + checked((int)sectorId) * _sectorSize;

        private byte[] ReadChain(uint startSector, uint[] fat) => ReadChain(startSector, fat, long.MaxValue);

        private byte[] ReadChain(uint startSector, uint[] fat, long size) => ReadChain(startSector, fat, size, _sectorSize, _file, useFileOffsets: true);

        private byte[] ReadChain(uint startSector, uint[] fat, long size, int unitSize, byte[] source, bool useFileOffsets = false)
        {
            var result = new MemoryStream();
            var sector = startSector;
            var guard = 0;
            while (sector != EndOfChain && sector != FreeSector && sector < fat.Length && guard++ < 100_000)
            {
                var offset = useFileOffsets ? SectorOffset(sector) : checked((int)sector) * unitSize;
                if (offset < 0 || offset + unitSize > source.Length)
                {
                    break;
                }

                result.Write(source, offset, unitSize);
                sector = fat[sector];
            }

            var bytes = result.ToArray();
            return size < bytes.Length ? bytes[..(int)size] : bytes;
        }
    }
}
