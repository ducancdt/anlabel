using ZXing;
using ZXing.OneD;

namespace ANLAbel.Barcode.Renderers;

/// <summary>
/// Code 128 with operator-chosen code sets: the data is split by
/// <c>&lt;MODEA&gt;</c>, <c>&lt;MODEB&gt;</c> and <c>&lt;MODEC&gt;</c> markers and each
/// part is encoded in that set (ISO/IEC 15417). Rendering, quiet zones and
/// integer scaling come from ZXing's own one-dimensional writer, so a manual
/// symbol is laid out exactly like an automatic one.
/// </summary>
internal sealed class Code128ManualWriter : OneDimensionalCodeWriter
{
    private const int CodeA = 101;
    private const int CodeB = 100;
    private const int CodeC = 99;
    private const int StartA = 103;
    private const int StartB = 104;
    private const int StartC = 105;
    private const int Stop = 106;

    // Bar/space widths of the 107 Code 128 symbol characters (ISO/IEC 15417 table 1).
    private static readonly string[] Patterns =
    [
        "212222", "222122", "222221", "121223", "121322", "131222", "122213", "122312",
        "132212", "221213", "221312", "231212", "112232", "122132", "122231", "113222",
        "123122", "123221", "223211", "221132", "221231", "213212", "223112", "312131",
        "311222", "321122", "321221", "312212", "322112", "322211", "212123", "212321",
        "232121", "111323", "131123", "131321", "112313", "132113", "132311", "211313",
        "231113", "231311", "112133", "112331", "132131", "113123", "113321", "133121",
        "313121", "211331", "231131", "213113", "213311", "213131", "311123", "311321",
        "331121", "312113", "312311", "332111", "314111", "221411", "431111", "111224",
        "111422", "121124", "121421", "141122", "141221", "112214", "112412", "122114",
        "122411", "142112", "142211", "241211", "221114", "413111", "241112", "134111",
        "111242", "121142", "121241", "114212", "124112", "124211", "411212", "421112",
        "421211", "212141", "214121", "412121", "111143", "111341", "131141", "114113",
        "114311", "411113", "411311", "113141", "114131", "311141", "411131", "211412",
        "211214", "211232", "2331112",
    ];

    protected override IList<BarcodeFormat> SupportedWriteFormats => [BarcodeFormat.CODE_128];

    public override bool[] encode(string contents)
    {
        if (!Code128ManualEncoding.TryParse(contents, out var segments, out var error))
        {
            throw new ArgumentException(error);
        }

        var values = new List<int>();
        var current = ' ';
        foreach (var (set, text) in segments)
        {
            if (values.Count == 0)
            {
                values.Add(set switch { 'A' => StartA, 'B' => StartB, _ => StartC });
            }
            else if (set != current)
            {
                values.Add(set switch { 'A' => CodeA, 'B' => CodeB, _ => CodeC });
            }

            current = set;
            if (set == 'C')
            {
                for (var i = 0; i < text.Length; i += 2)
                {
                    values.Add(((text[i] - '0') * 10) + (text[i + 1] - '0'));
                }
            }
            else
            {
                foreach (var ch in text)
                {
                    values.Add(set == 'A'
                        ? (ch < 32 ? ch + 64 : ch - 32)
                        : ch - 32);
                }
            }
        }

        var checksum = values[0];
        for (var i = 1; i < values.Count; i++)
        {
            checksum += values[i] * i;
        }

        values.Add(checksum % 103);
        values.Add(Stop);

        var modules = new List<bool>();
        foreach (var value in values)
        {
            var bar = true;
            foreach (var width in Patterns[value])
            {
                for (var w = 0; w < width - '0'; w++)
                {
                    modules.Add(bar);
                }

                bar = !bar;
            }
        }

        return modules.ToArray();
    }
}

/// <summary>Parsing and display rules for manual Code 128 code-set markers.</summary>
public static class Code128ManualEncoding
{
    private static readonly (string Marker, char Set)[] Markers =
    [
        ("<MODEA>", 'A'),
        ("<MODEB>", 'B'),
        ("<MODEC>", 'C'),
    ];

    /// <summary>The data without code-set markers, as printed under the barcode.</summary>
    public static string StripMarkers(string? data)
    {
        var text = data ?? string.Empty;
        foreach (var (marker, _) in Markers)
        {
            text = text.Replace(marker, string.Empty, StringComparison.OrdinalIgnoreCase);
        }

        return text;
    }

    public static bool TryParse(string? data, out IReadOnlyList<(char Set, string Text)> segments, out string error)
    {
        var parts = new List<(char Set, string Text)>();
        segments = parts;
        error = string.Empty;
        var text = data ?? string.Empty;
        var position = 0;
        char? set = null;
        var buffer = new System.Text.StringBuilder();
        while (position < text.Length)
        {
            var marker = Markers.FirstOrDefault(item =>
                string.Compare(text, position, item.Marker, 0, item.Marker.Length, StringComparison.OrdinalIgnoreCase) == 0);
            if (marker.Marker is not null)
            {
                if (set is not null && buffer.Length > 0)
                {
                    parts.Add((set.Value, buffer.ToString()));
                    buffer.Clear();
                }

                set = marker.Set;
                position += marker.Marker.Length;
                continue;
            }

            if (set is null)
            {
                error = "Manual Code 128 data must start with <MODEA>, <MODEB> or <MODEC>.";
                return false;
            }

            buffer.Append(text[position]);
            position++;
        }

        if (set is not null && buffer.Length > 0)
        {
            parts.Add((set.Value, buffer.ToString()));
        }

        if (parts.Count == 0)
        {
            error = "Manual Code 128 data has no characters to encode.";
            return false;
        }

        foreach (var (partSet, partText) in parts)
        {
            var invalid = partSet switch
            {
                'A' => partText.FirstOrDefault(ch => ch > 95),
                'B' => partText.FirstOrDefault(ch => ch < 32 || ch > 127),
                _ => partText.FirstOrDefault(ch => !char.IsAsciiDigit(ch)),
            };
            if (invalid != default)
            {
                error = $"Manual Code 128 code set {partSet} cannot encode '{invalid}'. Code set A takes upper case and control characters, B takes printable ASCII, C takes digits.";
                return false;
            }

            if (partSet == 'C' && partText.Length % 2 != 0)
            {
                error = $"Manual Code 128 code set C encodes digit pairs; \"{partText}\" has an odd number of digits.";
                return false;
            }
        }

        return true;
    }
}
