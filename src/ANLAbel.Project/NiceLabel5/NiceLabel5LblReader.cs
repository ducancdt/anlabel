using System.Text;

namespace ANLAbel.Project.NiceLabel5;

/// <summary>
/// Minimal, fail-closed reader for the NiceLabel 5 <c>.lbl</c> header.
/// The format evidence is recorded in
/// <c>docs/NICELABEL_5_DEEP_REVERSE_ENGINEERING.md</c> §16-18 and §41:
/// an OLE compound file whose <c>Contents</c> stream starts with the
/// length-prefixed magic <c>NicePower</c>, a format version, and the label
/// size in 1/100 mm. Anything outside that verified shape throws
/// <see cref="InvalidDataException"/> rather than being interpreted loosely.
/// </summary>
public static class NiceLabel5LblReader
{
    private const int SupportedFormatVersion = 29;
    private const int DimensionOffset = 0x23;

    public static NiceLabel5LblHeader ReadHeader(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        var file = buffer.ToArray();

        var compound = CompoundFile.Open(file);
        var contents = compound.ReadStream("Contents")
            ?? throw new InvalidDataException("This .lbl file has no Contents stream, so it is not a NiceLabel 5 label.");

        var magic = ReadLengthPrefixedString(contents, 0, out var afterMagic);
        if (magic != "NicePower")
        {
            throw new InvalidDataException("This .lbl file does not start with the NicePower marker, so it is not a NiceLabel 5 label.");
        }

        if (contents.Length < DimensionOffset + 12)
        {
            throw new InvalidDataException("This .lbl Contents stream is too short to hold a NiceLabel 5 label header.");
        }

        var version = BitConverter.ToUInt16(contents, afterMagic);
        if (version != SupportedFormatVersion)
        {
            throw new InvalidDataException($"This .lbl file uses NiceLabel format version {version}; only version {SupportedFormatVersion} has been verified, so it cannot be opened safely.");
        }

        var width = BitConverter.ToInt32(contents, DimensionOffset);
        var height = BitConverter.ToInt32(contents, DimensionOffset + 4);
        var third = BitConverter.ToInt32(contents, DimensionOffset + 8);
        if (!IsPlausibleDimension(width) || !IsPlausibleDimension(height) || !IsPlausibleDimension(third))
        {
            throw new InvalidDataException("This .lbl file uses a NiceLabel header layout variant that has not been verified, so it cannot be opened safely.");
        }

        return new NiceLabel5LblHeader(version, width, height, third, compound.StreamNames);
    }

    private static bool IsPlausibleDimension(int hundredthsMm) => hundredthsMm is >= 100 and <= 50_000;

    private static string ReadLengthPrefixedString(byte[] data, int offset, out int after)
    {
        if (data.Length < offset + 2)
        {
            throw new InvalidDataException("This .lbl Contents stream is truncated.");
        }

        var length = BitConverter.ToUInt16(data, offset);
        if (length == 0 || data.Length < offset + 2 + length)
        {
            throw new InvalidDataException("This .lbl Contents stream is truncated.");
        }

        after = offset + 2 + length;
        return Encoding.ASCII.GetString(data, offset + 2, length).TrimEnd('\0');
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
