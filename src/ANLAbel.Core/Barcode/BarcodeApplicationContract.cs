using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using ANLAbel.Core.Enums;

namespace ANLAbel.Core.Barcode;

/// <summary>
/// Platform-neutral barcode application checks shared by the authoring and
/// printing paths.  The contract deliberately accepts one unambiguous GS1
/// authoring notation: <c>(AI)value(AI)value</c>.  It converts that notation to
/// the GS1 encoder form (ASCII group separators for variable-length fields),
/// while keeping human-readable parentheses available to the UI/HRI layer.
/// </summary>
public static class BarcodeApplicationContract
{
    /// <summary>
    /// Version of the curated, fail-closed GS1 AI registry shipped with this
    /// application. It is deliberately named as a subset: accepting an AI
    /// without its exact fixed/variable boundary would create a barcode with an
    /// unsafe FNC1 decision.
    /// </summary>
    public const string Gs1RegistryVersion = Gs1AiRegistry.Version;
    public const char GroupSeparator = '\u001D';
    public const double MinimumHriFontSizePt = 5;
    public const double MaximumHriFontSizePt = 20;

    private static readonly Regex Gs1SegmentPattern = new(
        @"\G\((?<ai>[0-9]{2,4})\)(?<value>[^()\u0000-\u001F]+)",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    /// <summary>
    /// Returns the minimum quiet zone in module units for the selected profile.
    /// Values are an explicit ANLAbel production policy; they do not replace a
    /// symbology-specific verifier or a physical label test.
    /// </summary>
    public static int GetRequiredQuietZoneModules(
        BarcodeApplicationProfile profile,
        BarcodeSymbology symbology)
    {
        if (profile == BarcodeApplicationProfile.General)
        {
            return 0;
        }

        return IsLinear(symbology)
            ? 10
            : symbology == BarcodeSymbology.DataMatrix
                ? 1
                : 4;
    }

    /// <summary>
    /// Validates profile-independent geometry and HRI settings.  The returned
    /// strings are suitable for an actionable preflight issue.
    /// </summary>
    public static IReadOnlyList<string> ValidateGeometry(
        BarcodeApplicationProfile profile,
        BarcodeSymbology symbology,
        int quietZoneModules,
        bool showHri,
        double hriFontSizePt)
    {
        var errors = new List<string>();
        if (quietZoneModules < 0)
        {
            errors.Add("Quiet zone cannot be negative.");
        }

        if (profile == BarcodeApplicationProfile.Industrial && !IsLinear(symbology))
        {
            errors.Add("Industrial profile currently supports linear symbologies only; choose General or GS1 for a matrix code.");
        }

        var requiredQuietZone = GetRequiredQuietZoneModules(profile, symbology);
        if (quietZoneModules < requiredQuietZone)
        {
            errors.Add($"Quiet zone is {quietZoneModules} module(s); this profile requires at least {requiredQuietZone} module(s) for {symbology}. Increase the quiet-zone setting before printing.");
        }

        if (showHri
            && IsLinear(symbology)
            && (double.IsNaN(hriFontSizePt)
                || double.IsInfinity(hriFontSizePt)
                || hriFontSizePt < MinimumHriFontSizePt
                || hriFontSizePt > MaximumHriFontSizePt))
        {
            errors.Add($"HRI font size must be between {MinimumHriFontSizePt:0.#} and {MaximumHriFontSizePt:0.#} pt for this production profile.");
        }

        return new ReadOnlyCollection<string>(errors);
    }

    /// <summary>
    /// Validates data-specific rules for a profile.  GS1 uses explicit
    /// parenthesized AI notation so fixed/variable field boundaries are not
    /// guessed from a raw string.
    /// </summary>
    public static IReadOnlyList<string> ValidateData(
        BarcodeApplicationProfile profile,
        BarcodeSymbology symbology,
        string? data)
    {
        var errors = new List<string>();
        if (profile != BarcodeApplicationProfile.Gs1)
        {
            return new ReadOnlyCollection<string>(errors);
        }

        if (symbology is not (BarcodeSymbology.Code128 or BarcodeSymbology.QRCode or BarcodeSymbology.DataMatrix))
        {
            errors.Add("GS1 profile supports Code 128, QR Code, and Data Matrix in this release.");
            return new ReadOnlyCollection<string>(errors);
        }

        if (!TryParseGs1(data, out _, out var parseErrors))
        {
            errors.AddRange(parseErrors);
        }

        return new ReadOnlyCollection<string>(errors);
    }

    /// <summary>
    /// Converts the supported human-readable GS1 notation to encoder data.  The
    /// GS1 renderer adds the leading FNC1; group separators here delimit only
    /// variable-length fields between subsequent AIs.
    /// </summary>
    public static bool TryNormalizeGs1Data(string? data, out string normalized, out IReadOnlyList<string> errors)
    {
        if (TryParseGs1(data, out normalized, out var parseErrors))
        {
            errors = Array.Empty<string>();
            return true;
        }

        normalized = string.Empty;
        errors = new ReadOnlyCollection<string>(parseErrors);
        return false;
    }

    private static bool TryParseGs1(string? data, out string normalized, out List<string> errors)
    {
        normalized = string.Empty;
        errors = new List<string>();
        if (string.IsNullOrWhiteSpace(data))
        {
            errors.Add("GS1 data is empty.");
            return false;
        }

        var input = data.Trim();
        var segments = new List<Gs1Segment>();
        var offset = 0;
        while (offset < input.Length)
        {
            var match = Gs1SegmentPattern.Match(input, offset);
            if (!match.Success)
            {
                errors.Add($"GS1 data must use (AI)value notation; invalid segment near character {offset + 1}.");
                return false;
            }

            var ai = match.Groups["ai"].Value;
            var value = match.Groups["value"].Value;
            ValidateSegment(ai, value, errors);
            segments.Add(new Gs1Segment(ai, value));
            offset = match.Index + match.Length;
        }

        if (segments.Count == 0 || errors.Count > 0)
        {
            return false;
        }

        var builder = new StringBuilder(input.Length);
        for (var index = 0; index < segments.Count; index++)
        {
            var segment = segments[index];
            builder.Append(segment.ApplicationIdentifier).Append(segment.Value);
            if (index < segments.Count - 1 && RequiresSeparatorWhenFollowed(segment.ApplicationIdentifier))
            {
                builder.Append(GroupSeparator);
            }
        }

        normalized = builder.ToString();
        return true;
    }

    private static void ValidateSegment(string ai, string value, List<string> errors)
    {
        if (!Gs1AiRegistry.TryGetDefinition(ai, out var definition))
        {
            errors.Add($"GS1 AI {ai} is not available in registry {Gs1RegistryVersion}. Update the registry before using this application identifier.");
            return;
        }

        if (value.Length == 0)
        {
            errors.Add($"GS1 application identifier {ai} has an empty value.");
            return;
        }

        if (ai is "00" or "01" or "02")
        {
            var requiredLength = ai == "00" ? 18 : 14;
            if (value.Length != requiredLength || !value.All(char.IsDigit))
            {
                errors.Add($"GS1 AI {ai} requires exactly {requiredLength} digits including its check digit.");
                return;
            }

            if (!HasValidGs1CheckDigit(value))
            {
                errors.Add($"GS1 AI {ai} has an invalid check digit.");
            }

            return;
        }

        if (ai is "11" or "12" or "13" or "15" or "16" or "17")
        {
            if (value.Length != 6
                || !value.All(char.IsDigit)
                || !DateTime.TryParseExact(value, "yyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
            {
                errors.Add($"GS1 AI {ai} requires exactly 6 numeric date digits (YYMMDD).");
            }

            return;
        }

        if (ai == "10")
        {
            if (value.Length <= 20 && !value.Any(char.IsControl))
            {
                return;
            }

            errors.Add("GS1 AI 10 (batch/lot) must contain 1–20 printable characters.");
            return;
        }

        if (ai == "21")
        {
            if (value.Length <= 20 && !value.Any(char.IsControl))
            {
                return;
            }

            errors.Add("GS1 AI 21 (serial) must contain 1–20 printable characters.");
            return;
        }

        if (ai is "30" or "37")
        {
            if (value.Length > 8 || !value.All(char.IsDigit))
            {
                errors.Add($"GS1 AI {ai} requires 1–8 numeric digits.");
            }

            return;
        }

        // Additional industrial AIs used on manufacturing/warehouse labels.
        // This is an explicit ANLAbel production subset, not a full GS1 registry.
        if (ai is "240" or "241")
        {
            if (value.Length > 30 || value.Any(char.IsControl))
            {
                errors.Add($"GS1 AI {ai} must contain 1–30 printable characters.");
            }

            return;
        }

        if (ai is "400" or "401" or "402" or "403")
        {
            if (value.Length > 30 || value.Any(char.IsControl))
            {
                errors.Add($"GS1 AI {ai} must contain 1–30 printable characters.");
            }

            return;
        }

        if (ai is "410" or "411" or "412" or "413" or "414" or "415" or "416" or "417")
        {
            if (value.Length != 13 || !value.All(char.IsDigit) || !HasValidGs1CheckDigit(value))
            {
                errors.Add($"GS1 AI {ai} requires exactly 13 digits including a valid check digit (GLN).");
            }

            return;
        }

        if (ai == "420")
        {
            if (value.Length is < 1 or > 20 || value.Any(char.IsControl))
            {
                errors.Add("GS1 AI 420 (ship-to postal) must contain 1–20 printable characters.");
            }

            return;
        }

        if (ai == "421")
        {
            // 3-digit ISO country code + up to 9 postal characters.
            if (value.Length is < 3 or > 12
                || !char.IsDigit(value[0])
                || !char.IsDigit(value[1])
                || !char.IsDigit(value[2])
                || value.Skip(3).Any(char.IsControl))
            {
                errors.Add("GS1 AI 421 requires a 3-digit country code plus up to 9 postal characters.");
            }

            return;
        }

        // Country-of-origin/process fields are fixed numeric element strings,
        // but they are not in GS1's pre-defined-length table. The registry
        // therefore emits a separator when another element string follows.
        if (ai is "422" or "424" or "426")
        {
            if (value.Length != 3 || !value.All(char.IsDigit))
            {
                errors.Add($"GS1 AI {ai} requires exactly 3 numeric country-code digits.");
            }

            return;
        }

        if (ai == "425")
        {
            if (value.Length != 6 || !value.All(char.IsDigit))
            {
                errors.Add("GS1 AI 425 requires exactly 6 numeric country-code digits.");
            }

            return;
        }

        // Trade measures: the 31nn..36nn series has a fixed six-digit
        // numeric value. The last AI digit encodes the implied decimal-point
        // position. Treating the series as fixed-length is essential: a GS
        // separator after one of these values would become data for the next
        // element string. See GS1 General Specifications / AI reference.
        if (ai.Length == 4
            && ai[0] == '3'
            && ai[1] is >= '1' and <= '6'
            && char.IsDigit(ai[2])
            && char.IsDigit(ai[3]))
        {
            if (value.Length != 6 || !value.All(char.IsDigit))
            {
                errors.Add($"GS1 AI {ai} requires exactly 6 numeric measure digits.");
            }

            return;
        }

        // Expiration date and time (YYMMDDhhmm) is fixed data length, but is
        // not pre-defined length; it therefore needs a separator before a
        // following element string.
        if (ai == "7003")
        {
            if (value.Length != 10
                || !value.All(char.IsDigit)
                || !DateTime.TryParseExact(value[..6], "yyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _)
                || value[6..] is not { } time
                || !int.TryParse(time[..2], CultureInfo.InvariantCulture, out var hour)
                || !int.TryParse(time[2..], CultureInfo.InvariantCulture, out var minute)
                || hour > 23
                || minute > 59)
            {
                errors.Add("GS1 AI 7003 requires a valid 10-digit expiration date/time (YYMMDDhhmm).");
            }

            return;
        }

        // Company-internal AIs 90–99: variable, printable, bounded.
        if (ai.Length == 2
            && ai[0] == '9'
            && char.IsDigit(ai[1]))
        {
            if (value.Length > 30 || value.Any(char.IsControl))
            {
                errors.Add($"GS1 AI {ai} must contain 1–30 printable characters.");
            }

            return;
        }

        if (definition.ValuePattern is not null
            && Regex.IsMatch(value, $"\\A(?:{definition.ValuePattern})\\z", RegexOptions.CultureInvariant))
        {
            return;
        }

        errors.Add($"GS1 AI {ai} has a value that does not match the official registry pattern.");
    }

    private static bool HasValidGs1CheckDigit(string value)
    {
        var sum = 0;
        var multiplier = 3;
        for (var index = value.Length - 2; index >= 0; index--)
        {
            sum += (value[index] - '0') * multiplier;
            multiplier = multiplier == 3 ? 1 : 3;
        }

        var expected = (10 - (sum % 10)) % 10;
        return expected.ToString(CultureInfo.InvariantCulture)[0] == value[^1];
    }

    private static bool RequiresSeparatorWhenFollowed(string ai)
        => Gs1AiRegistry.TryGetDefinition(ai, out var definition)
            && definition.BoundaryKind == Gs1AiBoundaryKind.SeparatorRequired;

    private static bool IsLinear(BarcodeSymbology symbology)
        => symbology is BarcodeSymbology.Code128
            or BarcodeSymbology.Code39
            or BarcodeSymbology.Code93
            or BarcodeSymbology.Ean13
            or BarcodeSymbology.Ean8
            or BarcodeSymbology.UpcA
            or BarcodeSymbology.UpcE
            or BarcodeSymbology.ITF
            or BarcodeSymbology.Codabar
            or BarcodeSymbology.MSI
            or BarcodeSymbology.Plessey;

    private readonly record struct Gs1Segment(string ApplicationIdentifier, string Value);
}
