using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace ANLAbel.Project.NiceLabel10;

/// <summary>
/// Minimal read-only ZIP reader for the subset NiceLabel 10 writes:
/// stored/deflated entries and WinZip AES entries (method 99, AE-1/AE-2,
/// AES-128/192/256). Anything else (traditional ZipCrypto, other methods,
/// multi-disk archives) fails closed with <see cref="InvalidDataException"/>
/// instead of returning partial or garbled content. The layout follows the
/// public WinZip AES specification: PBKDF2-HMAC-SHA1 (1000 iterations)
/// derives an encryption key, an HMAC key and a 2-byte password check;
/// the payload is AES in the WinZip little-endian CTR construction and is
/// authenticated by a 10-byte truncated HMAC-SHA1 that is verified before
/// any decrypted byte is handed out.
/// </summary>
internal static class WinZipAesArchive
{
    public static IReadOnlyDictionary<string, byte[]> ReadAll(Stream stream, string password)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(password);

        var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        var data = buffer.ToArray();

        var entries = ReadCentralDirectory(data);
        var result = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in entries)
        {
            var content = ExtractEntry(data, entry, password);
            result[entry.Name.Replace('\\', '/')] = content;
        }

        return result;
    }

    private sealed record CentralEntry(
        string Name,
        int Method,
        long CompressedSize,
        long UncompressedSize,
        uint Crc32,
        int LocalHeaderOffset,
        byte[] CentralExtra,
        bool Encrypted);

    private static List<CentralEntry> ReadCentralDirectory(byte[] data)
    {
        // End of central directory record: scan the tail for its signature.
        var eocdOffset = -1;
        for (var i = data.Length - 22; i >= Math.Max(0, data.Length - 22 - 65536); i--)
        {
            if (ReadUInt32(data, i) == 0x06054b50)
            {
                eocdOffset = i;
                break;
            }
        }

        if (eocdOffset < 0)
        {
            throw new InvalidDataException("Not a ZIP archive: end of central directory not found.");
        }

        var totalEntries = ReadUInt16(data, eocdOffset + 10);
        var offset = (int)ReadUInt32(data, eocdOffset + 16);
        var entries = new List<CentralEntry>(totalEntries);
        for (var index = 0; index < totalEntries; index++)
        {
            if (offset + 46 > data.Length || ReadUInt32(data, offset) != 0x02014b50)
            {
                throw new InvalidDataException("Corrupt ZIP central directory.");
            }

            var flags = ReadUInt16(data, offset + 8);
            var method = ReadUInt16(data, offset + 10);
            var crc = ReadUInt32(data, offset + 16);
            var compressedSize = ReadUInt32(data, offset + 20);
            var uncompressedSize = ReadUInt32(data, offset + 24);
            var nameLength = ReadUInt16(data, offset + 28);
            var extraLength = ReadUInt16(data, offset + 30);
            var commentLength = ReadUInt16(data, offset + 32);
            var localHeaderOffset = (int)ReadUInt32(data, offset + 42);
            if (compressedSize == 0xFFFFFFFF || uncompressedSize == 0xFFFFFFFF || localHeaderOffset < 0)
            {
                throw new InvalidDataException("ZIP64 archives are not supported.");
            }

            var name = Encoding.UTF8.GetString(data, offset + 46, nameLength);
            var extra = data.AsSpan(offset + 46 + nameLength, extraLength).ToArray();
            entries.Add(new CentralEntry(
                name,
                method,
                compressedSize,
                uncompressedSize,
                crc,
                localHeaderOffset,
                extra,
                (flags & 0x0001) != 0));
            offset += 46 + nameLength + extraLength + commentLength;
        }

        return entries;
    }

    private static byte[] ExtractEntry(byte[] data, CentralEntry entry, string password)
    {
        var offset = entry.LocalHeaderOffset;
        if (offset + 30 > data.Length || ReadUInt32(data, offset) != 0x04034b50)
        {
            throw new InvalidDataException($"Corrupt ZIP local header for '{entry.Name}'.");
        }

        var nameLength = ReadUInt16(data, offset + 26);
        var extraLength = ReadUInt16(data, offset + 28);
        var dataStart = offset + 30 + nameLength + extraLength;
        if (dataStart + entry.CompressedSize > data.Length)
        {
            throw new InvalidDataException($"Truncated ZIP entry '{entry.Name}'.");
        }

        var raw = data.AsSpan(dataStart, (int)entry.CompressedSize);
        byte[] compressed;
        int actualMethod;
        var aeVersion = 0;
        if (entry.Method == 99)
        {
            var (decrypted, method, version) = DecryptWinZipAes(raw, entry, password);
            compressed = decrypted;
            actualMethod = method;
            aeVersion = version;
        }
        else if (entry.Encrypted)
        {
            throw new InvalidDataException(
                $"Entry '{entry.Name}' uses traditional ZIP encryption, which is not supported.");
        }
        else
        {
            compressed = raw.ToArray();
            actualMethod = entry.Method;
        }

        var content = actualMethod switch
        {
            0 => compressed,
            8 => Inflate(compressed, entry.Name),
            _ => throw new InvalidDataException(
                $"Entry '{entry.Name}' uses unsupported compression method {actualMethod}.")
        };

        if (content.LongLength != entry.UncompressedSize)
        {
            throw new InvalidDataException($"Entry '{entry.Name}' decompressed to an unexpected size.");
        }

        // AE-2 stores a zero CRC; AE-1 (and plain entries) carry a real one.
        if (aeVersion != 2 && entry.Crc32 != 0 && Crc32(content) != entry.Crc32)
        {
            throw new InvalidDataException($"Entry '{entry.Name}' failed its CRC check.");
        }

        return content;
    }

    private static (byte[] Plain, int ActualMethod, int Version) DecryptWinZipAes(
        ReadOnlySpan<byte> raw,
        CentralEntry entry,
        string password)
    {
        var extra = FindExtraField(entry.CentralExtra, 0x9901)
            ?? throw new InvalidDataException($"AES entry '{entry.Name}' lacks its WinZip AES extra field.");
        if (extra.Length < 7)
        {
            throw new InvalidDataException($"AES entry '{entry.Name}' has a truncated extra field.");
        }

        var version = ReadUInt16(extra, 0);
        var strength = extra[4];
        var actualMethod = ReadUInt16(extra, 5);
        var (saltSize, keySize) = strength switch
        {
            1 => (8, 16),
            2 => (12, 24),
            3 => (16, 32),
            _ => throw new InvalidDataException($"AES entry '{entry.Name}' has unknown strength {strength}.")
        };

        const int verifySize = 2;
        const int authSize = 10;
        if (raw.Length < saltSize + verifySize + authSize)
        {
            throw new InvalidDataException($"AES entry '{entry.Name}' is truncated.");
        }

        var salt = raw[..saltSize];
        var expectedVerify = raw.Slice(saltSize, verifySize);
        var cipher = raw.Slice(saltSize + verifySize, raw.Length - saltSize - verifySize - authSize);
        var expectedAuth = raw[^authSize..];

        var passwordBytes = Encoding.UTF8.GetBytes(password);
        var derived = Rfc2898DeriveBytes.Pbkdf2(
            passwordBytes, salt, 1000, HashAlgorithmName.SHA1, keySize * 2 + verifySize);
        var encryptionKey = derived.AsSpan(0, keySize);
        var macKey = derived.AsSpan(keySize, keySize);
        var verify = derived.AsSpan(keySize * 2, verifySize);
        if (!verify.SequenceEqual(expectedVerify))
        {
            throw new InvalidDataException(
                $"Entry '{entry.Name}' cannot be opened: the label package password check failed.");
        }

        using var hmac = new HMACSHA1(macKey.ToArray());
        var auth = hmac.ComputeHash(cipher.ToArray());
        if (!auth.AsSpan(0, authSize).SequenceEqual(expectedAuth))
        {
            throw new InvalidDataException($"Entry '{entry.Name}' failed its AES integrity check.");
        }

        var plain = AesCtrTransform(cipher, encryptionKey);
        return (plain, actualMethod, version);
    }

    /// <summary>
    /// WinZip AES uses AES in counter mode where the counter is a 128-bit
    /// little-endian integer starting at 1, encrypted as a block and XORed
    /// with the data, block by block.
    /// </summary>
    private static byte[] AesCtrTransform(ReadOnlySpan<byte> input, ReadOnlySpan<byte> key)
    {
        using var aes = Aes.Create();
        aes.Key = key.ToArray();
        aes.Mode = CipherMode.ECB;
        aes.Padding = PaddingMode.None;
        using var encryptor = aes.CreateEncryptor();

        var output = new byte[input.Length];
        var counter = new byte[16];
        var keystream = new byte[16];
        for (var blockStart = 0; blockStart < input.Length; blockStart += 16)
        {
            // Increment the little-endian counter before each block (first value is 1).
            for (var i = 0; i < counter.Length; i++)
            {
                if (++counter[i] != 0)
                {
                    break;
                }
            }

            encryptor.TransformBlock(counter, 0, 16, keystream, 0);
            var blockLength = Math.Min(16, input.Length - blockStart);
            for (var i = 0; i < blockLength; i++)
            {
                output[blockStart + i] = (byte)(input[blockStart + i] ^ keystream[i]);
            }
        }

        return output;
    }

    private static byte[] Inflate(byte[] compressed, string entryName)
    {
        using var input = new MemoryStream(compressed);
        using var deflate = new DeflateStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        try
        {
            deflate.CopyTo(output);
        }
        catch (InvalidDataException exception)
        {
            throw new InvalidDataException($"Entry '{entryName}' cannot be inflated.", exception);
        }

        return output.ToArray();
    }

    private static byte[]? FindExtraField(byte[] extra, int targetId)
    {
        var offset = 0;
        while (offset + 4 <= extra.Length)
        {
            var id = ReadUInt16(extra, offset);
            var size = ReadUInt16(extra, offset + 2);
            if (offset + 4 + size > extra.Length)
            {
                return null;
            }

            if (id == targetId)
            {
                return extra.AsSpan(offset + 4, size).ToArray();
            }

            offset += 4 + size;
        }

        return null;
    }

    private static uint Crc32(ReadOnlySpan<byte> data)
    {
        var crc = 0xFFFFFFFFu;
        foreach (var value in data)
        {
            crc ^= value;
            for (var bit = 0; bit < 8; bit++)
            {
                crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xEDB88320u : crc >> 1;
            }
        }

        return ~crc;
    }

    private static ushort ReadUInt16(byte[] data, int offset) =>
        BitConverter.ToUInt16(data, offset);

    private static uint ReadUInt32(byte[] data, int offset) =>
        BitConverter.ToUInt32(data, offset);
}
