using ZXing;
using ZXing.OneD;

namespace ANLAbel.Barcode.Renderers;

/// <summary>
/// Plessey drawn in whole narrow modules. ZXing.Net's <c>PlesseyWriter</c>
/// draws in fifths of a narrow bar (a zero bit is bar 5 + space 20, a one bit
/// bar 14 + space 11) inside 100 units of its own white margin, so every
/// fifth was counted as a module: a six-digit code measured 1 245 modules
/// (316 mm at X 0.254 mm) and was blocked, or squeezed into its frame. This
/// writer keeps ZXing's element sequence — start bits 1101, each digit least
/// significant bit first, the CRC-8 x⁸+x⁷+x⁶+x⁵+x³+1, the termination bar and
/// the end pattern — with every width rounded to whole modules (5→1, 20→4,
/// 14→3, 11→2, 25→5), so each bit stays five modules wide and the quiet zone
/// is ANLAbel's own like every other linear code.
/// </summary>
internal sealed class PlesseyModuleWriter : OneDimensionalCodeWriter
{
    private const string Alphabet = "0123456789ABCDEF";
    private static readonly int[] ZeroBit = [1, 4];
    private static readonly int[] OneBit = [3, 2];
    private static readonly int[] Start = [3, 2, 3, 2, 1, 4, 3, 2];
    private static readonly int[] Termination = [5];
    private static readonly int[] End = [4, 1, 4, 1, 3, 2, 3, 2];
    private static readonly byte[] CrcGrid = [1, 1, 1, 1, 0, 1, 0, 0, 1];

    protected override IList<BarcodeFormat> SupportedWriteFormats => [BarcodeFormat.PLESSEY];

    public override bool[] encode(string contents)
    {
        var bits = new List<byte>(contents.Length * 4);
        foreach (var character in contents)
        {
            var value = Alphabet.IndexOf(character);
            if (value < 0)
            {
                throw new ArgumentException($"Requested contents contains a not encodable character: '{character}'");
            }

            for (var bit = 0; bit < 4; bit++)
            {
                bits.Add((byte)((value >> bit) & 1));
            }
        }

        var remainder = new byte[bits.Count + 8];
        bits.CopyTo(remainder);
        for (var index = 0; index < bits.Count; index++)
        {
            if (remainder[index] != 0)
            {
                for (var tap = 0; tap < CrcGrid.Length; tap++)
                {
                    remainder[index + tap] ^= CrcGrid[tap];
                }
            }
        }

        var widths = new List<int>(Start);
        foreach (var bit in bits.Concat(remainder.Skip(bits.Count)))
        {
            widths.AddRange(bit == 1 ? OneBit : ZeroBit);
        }

        var result = new bool[widths.Sum() + Termination.Sum() + End.Sum()];
        var position = appendPattern(result, 0, widths.ToArray(), true);
        position += appendPattern(result, position, Termination, true);
        appendPattern(result, position, End, false);
        return result;
    }
}
