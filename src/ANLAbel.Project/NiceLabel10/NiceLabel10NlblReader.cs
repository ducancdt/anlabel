using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace ANLAbel.Project.NiceLabel10;

/// <summary>
/// Reads a NiceLabel 10 <c>.nlbl</c> package. Reverse-engineered layout
/// (see <c>docs/NICELABEL_10_REVERSE_ENGINEERING.md</c>): the file is a
/// ZIP whose entries are WinZip-AES-256 encrypted with the fixed package
/// password NiceLabel's own loader derives in
/// <c>EuroPlus.NiceLabel.Core</c>'s zip wrapper; inside are an XML
/// <c>Formats/&lt;name&gt;</c> entry describing the label and a companion
/// <c>.nsln</c> solution entry declaring variables. Geometry integers are
/// 1/1000 mm. Only facts present in the XML are reported; items whose
/// semantics are not established stay
/// <see cref="NiceLabel10ObjectKind.Unsupported"/> so the caller can count
/// them honestly instead of guessing.
/// </summary>
public static class NiceLabel10NlblReader
{
    // The fixed package password NiceLabel 10's own loader applies to every
    // .nlbl/.nsln package (recovered from its zip wrapper; see the RE doc).
    // It protects the package layout, not user data: user-password-protected
    // labels fail the AES integrity check and are rejected, never guessed.
    private const string PackagePassword = ",^_A5Fus&!?j='Epiq*e";

    private const double ThousandthsPerMm = 1000.0;

    public static NiceLabel10Document Read(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        return Parse(WinZipAesArchive.ReadAll(stream, PackagePassword));
    }

    /// <summary>
    /// Parses already-decrypted package entries (entry name to XML bytes).
    /// Exposed for tests that exercise the XML mapping without a package.
    /// </summary>
    internal static NiceLabel10Document Parse(IReadOnlyDictionary<string, byte[]> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        var formatEntry = entries
            .Where(pair => pair.Key.StartsWith("Formats/", StringComparison.OrdinalIgnoreCase))
            .OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
        if (formatEntry.Value is null)
        {
            throw new InvalidDataException("The .nlbl package does not contain a Formats entry.");
        }

        var variables = new Dictionary<string, NiceLabel10Variable>(StringComparer.OrdinalIgnoreCase);
        // The solution entry is "<label name>.slnx" in every vendor-produced
        // package (207/207 sampled); ".nsln" is accepted for packages named
        // after the on-disk solution extension.
        var solutionEntry = entries
            .FirstOrDefault(pair => pair.Key.EndsWith(".slnx", StringComparison.OrdinalIgnoreCase)
                || pair.Key.EndsWith(".nsln", StringComparison.OrdinalIgnoreCase));
        if (solutionEntry.Value is not null)
        {
            ReadVariables(solutionEntry.Value, variables);
        }

        return ParseFormat(formatEntry.Value, formatEntry.Key, variables);
    }

    private static void ReadVariables(byte[] solutionXml, Dictionary<string, NiceLabel10Variable> variables)
    {
        var document = XDocument.Parse(DecodeXml(solutionXml));
        // Scripts name their inputs; every declared data source by its id.
        var namesById = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var declared in document.Descendants("Item"))
        {
            if (ChildText(declared, "Id")?.Trim() is { Length: > 0 } declaredId
                && ChildText(declared, "Name")?.Trim() is { Length: > 0 } declaredName)
            {
                namesById[declaredId] = declaredName;
            }
        }

        foreach (var item in document.Descendants("Item"))
        {
            var itemType = item.Attribute("Type")?.Value;
            var id = ChildText(item, "Id")?.Trim();
            var name = ChildText(item, "Name")?.Trim();
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            switch (itemType)
            {
                case "Variable":
                    break;
                case "DBField" or "CsvDBField":
                    // A database column; ANLAbel binds it to the linked data
                    // row by column name.
                    variables[id] = new NiceLabel10Variable(
                        id, name, null, string.Empty, 0, false, false, true, null, 0, null,
                        DataFieldName: ChildText(item, "FieldName")?.Trim() is { Length: > 0 } field ? field : name)
                    {
                        SampleValue = ChildText(item.Element("SampleValue"), "StringValue")
                    };
                    continue;
                case "ConcatenateFunction":
                    variables[id] = new NiceLabel10Variable(
                        id, name, null, string.Empty, 0, false, false, true, null, 0,
                        ReadConcatenation(item));
                    continue;
                case "GS1128Function":
                    if (ReadGs1Composition(item) is { } gs1Template)
                    {
                        variables[id] = new NiceLabel10Variable(
                            id, name, null, string.Empty, 0, false, false, true, null, 0, gs1Template);
                    }

                    continue;
                case "SubsetFunction":
                    if (ReadSubset(item) is { } subsetTemplate)
                    {
                        variables[id] = new NiceLabel10Variable(
                            id, name, null, string.Empty, 0, false, false, true, null, 0, subsetTemplate);
                    }

                    continue;
                case "ExecuteScriptFunction":
                    if (ReadScriptConcatenation(item, namesById) is { } scriptTemplate)
                    {
                        variables[id] = new NiceLabel10Variable(
                            id, name, null, string.Empty, 0, false, false, true, null, 0, scriptTemplate);
                    }

                    continue;
                case "AscFactFunction":
                    if (ReadAscComposition(item) is { } ascTemplate)
                    {
                        variables[id] = new NiceLabel10Variable(
                            id, name, null, string.Empty, 0, false, false, true, null, 0, ascTemplate);
                    }

                    continue;
                case "TransferDataSyntaxFunction":
                    if (ReadTransferDataSyntax(item) is { } envelopeTemplate)
                    {
                        variables[id] = new NiceLabel10Variable(
                            id, name, null, string.Empty, 0, false, false, true, null, 0, envelopeTemplate);
                    }

                    continue;
                case "DateAddFunction":
                {
                    var format = ChildText(item, "OutputFormat");
                    var input = ChildText(item.Element("InputDataSourceReference"), "Id")?.Trim();
                    if (!string.IsNullOrWhiteSpace(format) && !string.IsNullOrWhiteSpace(input))
                    {
                        variables[id] = new NiceLabel10Variable(
                            id, name, null, string.Empty, 0, false, false, true, format,
                            (int)ParseDouble(item.Element("Day")?.Element("FixedValue"), "Int32Value"),
                            null,
                            SystemDateOffsetMonths: (int)ParseDouble(item.Element("Month")?.Element("FixedValue"), "Int32Value"),
                            SystemDateOffsetYears: (int)ParseDouble(item.Element("Year")?.Element("FixedValue"), "Int32Value"),
                            DateInputId: input)
                        {
                            // An amount may come from another data source ("Expiry (days)").
                            DateOffsetDaysSourceId = ChildText(item.Element("Day")?.Element("DataSourceReference"), "Id")?.Trim(),
                            DateOffsetMonthsSourceId = ChildText(item.Element("Month")?.Element("DataSourceReference"), "Id")?.Trim(),
                            DateOffsetYearsSourceId = ChildText(item.Element("Year")?.Element("DataSourceReference"), "Id")?.Trim()
                        };
                    }

                    continue;
                }
                default:
                    // Other solution functions (HIBC composition, other
                    // script kinds) have no ANLAbel equivalent; objects
                    // referencing them stay unconverted.
                    continue;
            }

            var defaultValue = string.Empty;
            IReadOnlyList<string>? pickList = null;
            string? systemDateFormat = null;
            string? promptDateFormat = null;
            string? promptDateInputFormat = null;
            var systemDateOffsetDays = 0;
            var defaultElement = item.Element("DefaultValue");
            switch (defaultElement?.Attribute("Type")?.Value)
            {
                case "StringContents":
                    defaultValue = NiceLabel10RichText.DecodeStoredText(ChildText(defaultElement, "StringValue"));
                    break;
                case "PickListContents":
                    defaultValue = ChildText(defaultElement, "SelectedValue") ?? string.Empty;
                    pickList = defaultElement.Element("Values")?.Elements("Item").Select(value => value.Value).Where(value => value.Length > 0).ToArray();
                    break;
                case "DateTimeContents":
                {
                    // A prompted date (NiceLabel Date variable): typed in the
                    // input format, printed in the output format. A value linked
                    // to another source ("[id]") is kept for the mirror rule below.
                    promptDateInputFormat = ChildText(defaultElement, "InputFormat")?.Trim();
                    promptDateFormat = (ChildText(defaultElement, "OutputFormat") ?? promptDateInputFormat)?.Trim();
                    var userValue = ChildText(defaultElement, "UserValue")?.Trim() ?? string.Empty;
                    defaultValue = userValue.StartsWith('[') ? userValue : PromptedDateDefault(defaultElement, userValue, promptDateInputFormat);
                    break;
                }
                case "SystemDateTimeContents":
                    systemDateFormat = ChildText(defaultElement, "OutputFormat")
                        ?? ChildText(defaultElement, "InputFormat");
                    systemDateOffsetDays = (int)ParseDouble(defaultElement.Element("Offset"), "Days");
                    break;
            }

            var constraints = item.Element("Constraints");
            string? functionTemplate = null;
            var function = item.Element("InternalFunction");
            if (function is not null
                && string.Equals(function.Attribute("Type")?.Value, "ExtendedDataValueFunction", StringComparison.Ordinal))
            {
                functionTemplate = ChildText(function.Element("DataValue")?.Element("FixedValue"), "StringValue");
            }

            // A variable linked to another data source whose value is just that
            // source ("[id]") mirrors it: the database wizard's "Table.Field"
            // variables. Read as a prompt it printed the literal "[id]".
            if (functionTemplate is null
                && ChildText(item.Element("DataSourceReference"), "Id")?.Trim() is { Length: > 0 } linkedId
                && string.Equals(defaultValue.Trim(), "[" + linkedId + "]", StringComparison.OrdinalIgnoreCase))
            {
                functionTemplate = "[" + linkedId + "]";
                defaultValue = string.Empty;
            }

            var allowed = ChildText(item, "AllowedCharactersForCustomFormat");
            variables[id] = new NiceLabel10Variable(
                id,
                name,
                ChildText(item, "PromptText"),
                defaultValue,
                (int)ParseDouble(constraints, "Length"),
                ParseBool(constraints, "IsRequired"),
                allowed == "0123456789",
                ParseBool(item, "IsNotPrompted"),
                string.IsNullOrWhiteSpace(systemDateFormat) ? null : systemDateFormat,
                systemDateOffsetDays,
                functionTemplate,
                PickList: pickList is { Count: > 0 } ? pickList : null,
                // Output rules keep their spaces ("Weight: ", " kg").
                Prefix: item.Element("Prefix")?.Value,
                Suffix: item.Element("Suffix")?.Value,
                Padding: (int)ParseDouble(item, "Padding"),
                PaddingCharacter: item.Element("PaddingValue")?.Value,
                FixedLength: ParseBool(constraints, "FixedLength"),
                // A custom character set applies only with a data format (FormatId); without one the
                // variable takes any character and the stored set is a leftover (46 such in the samples).
                AllowedCharacters: string.IsNullOrEmpty(allowed) || allowed == "0123456789" || string.IsNullOrWhiteSpace(ChildText(item, "FormatId")) ? null : allowed)
            {
                DateFormat = string.IsNullOrWhiteSpace(promptDateFormat) ? null : promptDateFormat,
                DateInputFormat = string.IsNullOrWhiteSpace(promptDateInputFormat) ? null : promptDateInputFormat
            };
        }
    }

    /// <summary>
    /// A prompted date's stored default as yyyy-MM-dd: the user value in the
    /// input format, else the stored OLE date. "." (no value), 0 and the
    /// far-future placeholder 9999-12-31 give no default.
    /// </summary>
    private static string PromptedDateDefault(XElement value, string userValue, string? inputFormat)
    {
        if (userValue.Length > 0
            && userValue != "."
            && !string.IsNullOrEmpty(inputFormat)
            && DateTime.TryParseExact(userValue, inputFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var typed))
        {
            return typed.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        var stored = ParseDouble(value, "DateTimeValue");
        return stored >= 1 && stored < 2958465
            ? DateTime.FromOADate(stored).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
            : string.Empty;
    }

    /// <summary>
    /// A <c>ConcatenateFunction</c> joins data values (references or fixed
    /// strings) with an optional separator; written here in the same
    /// <c>[id]</c> template form an <c>ExtendedDataValue</c> uses.
    /// </summary>
    private static string ReadConcatenation(XElement function)
    {
        var separator = ChildText(function, "Separator") ?? string.Empty;
        var parts = new List<string>();
        foreach (var value in function.Element("DataValues")?.Elements("Item") ?? Enumerable.Empty<XElement>())
        {
            var referenceId = ChildText(value.Element("DataSourceReference"), "Id")?.Trim();
            if (!string.IsNullOrWhiteSpace(referenceId))
            {
                parts.Add("[" + referenceId + "]");
                continue;
            }

            parts.Add(ChildText(value.Element("FixedValue"), "StringValue") ?? string.Empty);
        }

        return string.Join(separator, parts);
    }

    /// <summary>
    /// A <c>GS1128Function</c> composes <c>(AI)value</c> segments from
    /// <c>GS1128DataValue</c> parts (vendor AI table id, decimal multiplier,
    /// auto check digit, data source). ANLAbel's GS1 notation is the same
    /// <c>(AI)value</c> form, so the template becomes
    /// <c>(AI)[id]</c> per part, with date AIs asking the referenced date for
    /// the GS1 <c>yyMMdd</c> form. Parts whose check digit NiceLabel would
    /// compute at print time have no ANLAbel equivalent, as have AIs outside
    /// the vendor table; such a function is left unresolved (null).
    /// </summary>
    private static string? ReadGs1Composition(XElement function)
    {
        var builder = new System.Text.StringBuilder();
        foreach (var value in function.Element("DataValues")?.Elements("Item") ?? Enumerable.Empty<XElement>())
        {
            if (!string.Equals(value.Attribute("Type")?.Value, "GS1128DataValue", StringComparison.Ordinal))
            {
                return null;
            }

            var id = (int)ParseDouble(value, "ApplicationIdentifierId");
            var multiplier = (int)ParseDouble(value, "Multiplier");
            if (!NiceLabel10Gs1ApplicationIdentifiers.TryGetApplicationIdentifier(id, multiplier, out var ai, out var carriesCheckDigit))
            {
                return null;
            }

            // AutoCheckDigitCalculation: NiceLabel appends the GS1 modulo-10
            // check digit at print time; ANLAbel's GS1CHECKDIGIT formula
            // function does the same, so the part is wrapped for it.
            var autoCheckDigit = ParseBool(value, "AutoCheckDigitCalculation");
            if (autoCheckDigit && !carriesCheckDigit)
            {
                return null;
            }

            var referenceId = ChildText(value.Element("DataSourceReference"), "Id")?.Trim();
            builder.Append('(').Append(ai).Append(')');
            if (autoCheckDigit)
            {
                builder.Append(NiceLabel10FunctionMarkers.Start).Append("GS1CHECKDIGIT").Append(NiceLabel10FunctionMarkers.Body);
            }

            if (!string.IsNullOrWhiteSpace(referenceId))
            {
                builder.Append('[').Append(referenceId);
                if (NiceLabel10Gs1ApplicationIdentifiers.IsDateApplicationIdentifier(ai))
                {
                    builder.Append(":yyMMdd");
                }

                builder.Append(']');
            }
            else
            {
                builder.Append(ChildText(value.Element("FixedValue"), "StringValue") ?? string.Empty);
            }

            if (autoCheckDigit)
            {
                builder.Append(NiceLabel10FunctionMarkers.End);
            }
        }

        return builder.Length == 0 ? null : builder.ToString();
    }

    /// <summary>
    /// A <c>SubsetFunction</c> takes <c>SubstringLength</c> characters after
    /// skipping <c>Offset</c> characters of its input (ANLAbel's MID with a
    /// 1-based start). The delimited mode (field number between separators)
    /// has no ANLAbel equivalent and stays unresolved.
    /// </summary>
    private static string? ReadSubset(XElement function)
    {
        var input = ChildText(function.Element("InputDataSourceReference"), "Id")?.Trim();
        var offset = (int)ParseDouble(function, "Offset");
        var length = (int)ParseDouble(function, "SubstringLength");
        if (ParseBool(function, "Delimited") || string.IsNullOrWhiteSpace(input) || offset < 0 || length <= 0)
        {
            return null;
        }

        return NiceLabel10FunctionMarkers.Start + "MID:" + (offset + 1).ToString(CultureInfo.InvariantCulture)
            + ":" + length.ToString(CultureInfo.InvariantCulture)
            + NiceLabel10FunctionMarkers.Body + "[" + input + "]" + NiceLabel10FunctionMarkers.End;
    }

    private const char GroupSeparator = '\u001D';
    private const char RecordSeparator = '\u001E';
    private const char EndOfTransmission = '\u0004';

    /// <summary>
    /// An <c>ExecuteScriptFunction</c> whose VBScript is one expression,
    /// <c>Result = "UN" &amp; SupplierID &amp; [Package Number]</c> (52 of the 68
    /// scripts in the vendor samples, the license plates and data-identifier
    /// fields of GTL, VDA and automotive labels): string literals and the
    /// function's own inputs by name (VBScript names ignore case), joined by
    /// <c>&amp;</c>, also inside UCase, LCase, Trim, Left(x, n), Right(x, n) and
    /// Mid(x, start, n). Plain text becomes the same <c>[id]</c> template a
    /// <c>ConcatenateFunction</c> gives; a call becomes a function marker the
    /// importer turns into the formula of the same name. Any other script
    /// (arithmetic, other calls, several statements, names that are not its
    /// inputs) is not translated, so objects using it stay unconverted.
    /// </summary>
    private static string? ReadScriptConcatenation(XElement function, IReadOnlyDictionary<string, string> namesById)
    {
        var script = ChildText(function, "Script")?.Trim();
        var statement = script is null ? null : ScriptStatement.Match(script);
        if (statement is not { Success: true })
        {
            return null;
        }

        var inputs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var reference in function.Element("InputDataSourceReferences")?.Elements("Item") ?? Enumerable.Empty<XElement>())
        {
            if (ChildText(reference, "Id")?.Trim() is { Length: > 0 } inputId
                && namesById.TryGetValue(inputId, out var inputName))
            {
                inputs[inputName] = inputId;
            }
        }

        var parser = new ScriptExpressionParser(statement.Groups["expression"].Value, inputs);
        return parser.Parse();
    }

    private static readonly Regex ScriptStatement = new(
        @"^Result\s*=\s*(?<expression>[^\r\n]+)$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    /// <summary>
    /// Reads the one VBScript expression of a translatable script into the
    /// importer's template form; null for anything outside that subset.
    /// </summary>
    private sealed class ScriptExpressionParser(string text, IReadOnlyDictionary<string, string> inputs)
    {
        private static readonly Dictionary<string, (string Formula, int Numbers)> Calls = new(StringComparer.OrdinalIgnoreCase)
        {
            ["UCase"] = ("UPPER", 0),
            ["LCase"] = ("LOWER", 0),
            ["Trim"] = ("TRIM", 0),
            ["Left"] = ("LEFT", 1),
            ["Right"] = ("RIGHT", 1),
            ["Mid"] = ("MID", 2)
        };

        private int _position;

        public string? Parse()
        {
            var expression = Expression();
            SkipSpaces();
            return expression is not null && _position == text.Length ? expression : null;
        }

        private string? Expression()
        {
            var builder = new System.Text.StringBuilder();
            do
            {
                if (Term() is not { } term)
                {
                    return null;
                }

                builder.Append(term);
                SkipSpaces();
            }
            while (Take('&'));

            return builder.ToString();
        }

        private string? Term()
        {
            SkipSpaces();
            if (_position >= text.Length)
            {
                return null;
            }

            var ch = text[_position];
            if (ch == '"')
            {
                return Literal();
            }

            if (ch == '[')
            {
                var close = text.IndexOf(']', _position + 1);
                if (close < 0)
                {
                    return null;
                }

                var bracketed = text[(_position + 1)..close].Trim();
                _position = close + 1;
                return Reference(bracketed);
            }

            if (!char.IsAsciiLetter(ch) && ch != '_')
            {
                return null;
            }

            var start = _position;
            while (_position < text.Length && (char.IsAsciiLetterOrDigit(text[_position]) || text[_position] == '_'))
            {
                _position++;
            }

            var name = text[start.._position];
            SkipSpaces();
            if (!Take('('))
            {
                return Reference(name);
            }

            if (!Calls.TryGetValue(name, out var call) || Expression() is not { } inner)
            {
                return null;
            }

            var numbers = new List<string>();
            for (var index = 0; index < call.Numbers; index++)
            {
                SkipSpaces();
                if (!Take(',') || Number() is not { } number)
                {
                    return null;
                }

                numbers.Add(number);
            }

            SkipSpaces();
            if (!Take(')'))
            {
                return null;
            }

            var head = string.Join(':', new[] { call.Formula }.Concat(numbers));
            return NiceLabel10FunctionMarkers.Start + head + NiceLabel10FunctionMarkers.Body + inner + NiceLabel10FunctionMarkers.End;
        }

        private string? Literal()
        {
            var builder = new System.Text.StringBuilder();
            _position++;
            while (_position < text.Length)
            {
                var ch = text[_position++];
                if (ch != '"')
                {
                    builder.Append(ch);
                    continue;
                }

                if (_position < text.Length && text[_position] == '"')
                {
                    builder.Append('"');
                    _position++;
                    continue;
                }

                // A bracket or brace in a literal would read as a reference in the template.
                var literal = builder.ToString();
                return literal.IndexOfAny(new[] { '[', ']', '{', '}' }) >= 0 ? null : literal;
            }

            return null;
        }

        private string? Number()
        {
            SkipSpaces();
            var start = _position;
            while (_position < text.Length && char.IsAsciiDigit(text[_position]))
            {
                _position++;
            }

            return _position > start ? text[start.._position] : null;
        }

        private string? Reference(string name)
            => inputs.TryGetValue(name, out var id) ? "[" + id + "]" : null;

        private bool Take(char expected)
        {
            if (_position < text.Length && text[_position] == expected)
            {
                _position++;
                return true;
            }

            return false;
        }

        private void SkipSpaces()
        {
            while (_position < text.Length && text[_position] is ' ' or '\t')
            {
                _position++;
            }
        }
    }

    /// <summary>
    /// An <c>AscFactFunction</c> composes ANSI MH10.8.2 data-identifier
    /// fields: each <c>AscFactDataValue</c> is its identifier (0-2 digits and
    /// a capital letter, e.g. <c>1S</c>) immediately followed by the value;
    /// several fields are separated by the group separator (U+001D), the
    /// form the ISO/IEC 15434 envelope (<see cref="ReadTransferDataSyntax"/>)
    /// and single-field linear barcodes both expect. A field that prints its
    /// short title instead of the identifier has no ANLAbel form (null).
    /// </summary>
    private static string? ReadAscComposition(XElement function)
    {
        var parts = new List<string>();
        foreach (var value in function.Element("DataValues")?.Elements("Item") ?? Enumerable.Empty<XElement>())
        {
            var identifier = ChildText(value, "AscFactIdentifierName")?.Trim();
            if (!string.Equals(value.Attribute("Type")?.Value, "AscFactDataValue", StringComparison.Ordinal)
                || identifier is null
                || !IsDataIdentifier(identifier)
                || ParseBool(value, "IsUsingShortTitle"))
            {
                return null;
            }

            var referenceId = ChildText(value.Element("DataSourceReference"), "Id")?.Trim();
            parts.Add(identifier + (string.IsNullOrWhiteSpace(referenceId)
                ? ChildText(value.Element("FixedValue"), "StringValue") ?? string.Empty
                : "[" + referenceId + "]"));
        }

        return parts.Count == 0 ? null : string.Join(GroupSeparator, parts);

        static bool IsDataIdentifier(string text) =>
            text.Length is >= 1 and <= 3
            && text[^1] is >= 'A' and <= 'Z'
            && text[..^1].All(char.IsAsciiDigit);
    }

    /// <summary>
    /// A <c>TransferDataSyntaxFunction</c> wraps data in the ISO/IEC 15434
    /// (ANSI MH10.8.3) envelope: <c>[)&gt;</c> RS format GS field GS field …
    /// RS EOT, the structure the vendor code builds for its fixed-field
    /// formats and the one format 06 (data identifiers) uses with the group
    /// separator between fields. Only format 06 is mapped; other formats
    /// stay unresolved (null).
    /// </summary>
    private static string? ReadTransferDataSyntax(XElement function)
    {
        var formats = (function.Element("Formats")?.Elements("Item") ?? Enumerable.Empty<XElement>()).ToList();
        if (formats.Count != 1 || (int)ParseDouble(formats[0], "FormatId") != 6)
        {
            return null;
        }

        var fields = new List<string>();
        foreach (var value in formats[0].Element("DataValues")?.Elements("Item") ?? Enumerable.Empty<XElement>())
        {
            var referenceId = ChildText(value.Element("DataSourceReference"), "Id")?.Trim();
            fields.Add(string.IsNullOrWhiteSpace(referenceId)
                ? ChildText(value.Element("FixedValue"), "StringValue") ?? string.Empty
                : "[" + referenceId + "]");
        }

        if (fields.Count == 0)
        {
            return null;
        }

        return "[)>" + RecordSeparator + "06" + GroupSeparator
            + string.Join(GroupSeparator, fields)
            + RecordSeparator + EndOfTransmission;
    }

    private static NiceLabel10Document ParseFormat(
        byte[] formatXml,
        string entryName,
        IReadOnlyDictionary<string, NiceLabel10Variable> variables)
    {
        var document = XDocument.Parse(DecodeXml(formatXml));
        var root = document.Root
            ?? throw new InvalidDataException("The .nlbl format entry is empty.");
        if (!string.Equals(root.Attribute("Type")?.Value, "Format", StringComparison.Ordinal))
        {
            throw new InvalidDataException("The .nlbl format entry is not a NiceLabel label format.");
        }

        var media = root.Element("Media")
            ?? throw new InvalidDataException("The .nlbl format entry has no Media element.");
        // Media is the page (sheet) size. The label itself is the page
        // layout element: equal to the page for a SingleFormatLayout, the
        // repeated cell of a multi-label sheet (WebFormatLayout, with
        // HorizontalCount/VerticalCount, offsets and gaps); object
        // coordinates are relative to that element (corpus: 20 sheet
        // layouts, every object inside its element).
        var pageLayout = root.Element("PageLayout");
        var width = Thousandths(pageLayout, "ElementWidth");
        var height = Thousandths(pageLayout, "ElementHeight");
        if (width <= 0 || height <= 0)
        {
            width = Thousandths(media, "Width");
            height = Thousandths(media, "Height");
        }

        if (width <= 0 || height <= 0)
        {
            throw new InvalidDataException("The .nlbl format entry stores a non-positive label size.");
        }

        var name = ChildText(root, "Name");
        if (string.IsNullOrWhiteSpace(name))
        {
            name = Path.GetFileNameWithoutExtension(entryName[(entryName.LastIndexOf('/') + 1)..]);
        }

        var objects = new List<NiceLabel10ObjectRecord>();
        var layerConditions = LayerConditions(root);
        foreach (var item in root.Descendants("Item"))
        {
            var record = ParseItem(item, variables);
            if (record is not null)
            {
                var conditions = new List<NiceLabel10VisibilityCondition>();
                if (ReadVisibilityCondition(item) is { } own)
                {
                    conditions.Add(own);
                }

                if (ChildText(item, "Id")?.Trim() is { Length: > 0 } itemId
                    && layerConditions.TryGetValue(itemId, out var layer))
                {
                    conditions.Add(layer);
                }

                objects.Add(conditions.Count == 0 ? record : record with { VisibilityConditions = conditions });
            }
        }

        return new NiceLabel10Document(name.Trim(), width, height, objects, variables)
        {
            PrintSetup = ReadPrintSetup(root, media, pageLayout),
            Groups = ReadGroups(root)
        };
    }

    /// <summary>
    /// Printer/media facts (§10 of the RE record). The serialized members
    /// were recovered from the vendor classes <c>PageMedia</c>
    /// (<c>FormatMedia</c>), <c>AbstractPageLayout</c>/<c>WebPageLayout</c>,
    /// <c>PrintScenario</c> and <c>Printer</c>; <c>DevModeBuffer</c> is the
    /// driver's base64 DEVMODEW.
    /// </summary>
    private static NiceLabel10PrintSetup ReadPrintSetup(XElement root, XElement media, XElement? pageLayout)
    {
        var scenario = root.Element("PrintScenarioCollection")?.Element("PrintScenarioCollection");
        var printer = scenario?.Element("Printer");
        var mediaKindText = ChildText(media, "Type");
        var dpi = 0;
        var devMode = ChildText(printer, "DevModeBuffer")?.Trim();
        if (!string.IsNullOrEmpty(devMode))
        {
            try
            {
                dpi = DevModeResolution(Convert.FromBase64String(devMode));
            }
            catch (FormatException)
            {
                dpi = 0;
            }
        }

        return new NiceLabel10PrintSetup(
            Thousandths(media, "Width"),
            Thousandths(media, "Height"),
            (int)ParseDouble(media, "PaperFormat"),
            ChildText(media.Element("PaperDetails"), "Name")?.Trim(),
            string.IsNullOrWhiteSpace(mediaKindText) ? 2 : (int)ParseDouble(media, "Type"),
            Thousandths(pageLayout, "HorizontalOffset"),
            Thousandths(pageLayout, "VerticalOffset"),
            Math.Max(1, (int)ParseDouble(pageLayout, "HorizontalCount")),
            Math.Max(1, (int)ParseDouble(pageLayout, "VerticalCount")),
            ChildText(pageLayout, "StockName")?.Trim() is { Length: > 0 } stock ? stock : null,
            ChildText(printer, "Name")?.Trim() is { Length: > 0 } printerName ? printerName : null,
            ChildText(printer, "DriverName")?.Trim() is { Length: > 0 } driverName ? driverName : null,
            ParseBool(scenario, "UseDefaultPrinter"),
            ParseBool(printer, "UseDefaultPrinterSettings"),
            (int)ParseDouble(scenario, "PrintRotation") == 2,
            dpi,
            Thousandths(pageLayout, "HorizontalGap"),
            Thousandths(pageLayout, "VerticalGap"),
            (int)ParseDouble(pageLayout, "WebSettings"));
    }

    /// <summary>
    /// Resolution from a Windows DEVMODEW (public wingdi.h layout):
    /// <c>dmFields</c> at byte 72, <c>dmPrintQuality</c> at 90 when
    /// <c>DM_PRINTQUALITY</c> (0x400) is set. Negative values are the
    /// device-independent DMRES_* draft/high constants, not a DPI.
    /// </summary>
    internal static int DevModeResolution(ReadOnlySpan<byte> devMode)
    {
        const int FieldsOffset = 72;
        const int PrintQualityOffset = 90;
        const uint DmPrintQuality = 0x400;
        if (devMode.Length < PrintQualityOffset + 2)
        {
            return 0;
        }

        var fields = System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(devMode[FieldsOffset..]);
        if ((fields & DmPrintQuality) == 0)
        {
            return 0;
        }

        var quality = System.Buffers.Binary.BinaryPrimitives.ReadInt16LittleEndian(devMode[PrintQualityOffset..]);
        return quality > 0 ? quality : 0;
    }

    private static NiceLabel10ObjectRecord? ParseItem(
        XElement item,
        IReadOnlyDictionary<string, NiceLabel10Variable> variables)
    {
        var type = item.Attribute("Type")?.Value;
        if (type is null || !type.EndsWith("DocumentItem", StringComparison.Ordinal))
        {
            return null;
        }

        // The background placeholder exists in every file at 0 x 0 and is
        // not a stored label object.
        if (type == "BackgroundDocumentItem")
        {
            return null;
        }

        var name = ChildText(item, "Name") ?? string.Empty;
        var geometry = item.Element("Geometry");
        var geometryType = geometry?.Attribute("Type")?.Value;
        var richText = ParseBool(item, "RichTextFormatting");
        // Multi-line values are stored base64; rich-text items store raw or
        // base64 RTF (or base64 text) and only their plain text carries over.
        var content = richText
            ? NiceLabel10RichText.ToPlainText(ChildText(item, "FixedContents"))
            : NiceLabel10RichText.DecodeStoredText(ChildText(item, "FixedContents"));

        var (variableId, variableName, unresolved) = ResolveVariable(item, variables);
        var textAlignment = (int)ParseDouble(item, "TextBoxAlignment");
        var fillStyle = (int)ParseDouble(item, "FillStyle");
        var fillColor = ChildText(item, "FillColor");
        var strokeThickness = ParseDouble(item, "Thickness") / ThousandthsPerMm;
        var itemColor = ChildText(item, "Color");

        var (fontFamily, fontSizePt, bold, italic, underline) = ParseFont(item);

        switch (type)
        {
            case "TextDocumentItem" when geometryType == "RectGeometry":
                return Base(NiceLabel10ObjectKind.FramedText) with
                {
                    XMm = Thousandths(geometry!, "Left"),
                    YMm = Thousandths(geometry!, "Top"),
                    WidthMm = Thousandths(geometry!, "Width"),
                    HeightMm = Thousandths(geometry!, "Height")
                };
            case "TextDocumentItem" when geometryType == "PositionGeometry":
                return Base(NiceLabel10ObjectKind.Text) with
                {
                    XMm = Thousandths(geometry!, "X"),
                    YMm = Thousandths(geometry!, "Y")
                };
            case "BarcodeDocumentItem" when geometryType == "PositionGeometry":
            {
                var barcodeData = item.Element("BarcodeData");
                return Base(NiceLabel10ObjectKind.Barcode) with
                {
                    XMm = Thousandths(geometry!, "X"),
                    YMm = Thousandths(geometry!, "Y"),
                    BarcodeDataType = barcodeData?.Attribute("Type")?.Value,
                    BarcodeModuleWidthMm = ParseDouble(barcodeData, "BaseBarWidth") / ThousandthsPerMm,
                    BarcodeModuleHeightMm = ParseDouble(barcodeData, "ModuleHeight") / ThousandthsPerMm,
                    BarcodeRatio = ParseDouble(barcodeData, "Ratio"),
                    BarcodeFormatValue = (int)ParseDouble(barcodeData,
                        barcodeData?.Attribute("Type")?.Value == "QRBarcodeData"
                            ? "QRBarcodeVersion"
                            : "DatamatrixFormat"),
                    BarcodeErrorLevel = (int)ParseDouble(barcodeData,
                        barcodeData?.Element("QRBarcodeErrorCorrectionLevel") is not null
                            ? "QRBarcodeErrorCorrectionLevel"
                            : "SecurityLevel"),
                    BarcodeHriFontSizePt = ParseDouble(
                        barcodeData?.Element("HumanInterpretationFontDescriptor"), "Height"),
                    BarcodeHriPosition = barcodeData?.Element("HumanInterpretationPosition") is null
                        ? -1
                        : (int)ParseDouble(barcodeData, "HumanInterpretationPosition"),
                    BarcodeHriFontName = ChildText(barcodeData?.Element("HumanInterpretationFontDescriptor"), "Name")?.Trim(),
                    BarcodeHriBold = HriFontIsBold(barcodeData?.Element("HumanInterpretationFontDescriptor")),
                    BarcodeHriAutoFontScaling = barcodeData?.Element("AutoFontScaling") is null
                        || ParseBool(barcodeData, "AutoFontScaling"),
                    BarcodeElementWidthMm = ParseDouble(barcodeData, "ElementWidth") / ThousandthsPerMm,
                    BarcodeElementHeightMm = ParseDouble(barcodeData, "ElementHeight") / ThousandthsPerMm,
                    BarcodeTruncated = ParseBool(barcodeData, "TruncatedBarcode"),
                    BarcodeHasCheckDigit = ParseBool(barcodeData, "HasCheckDigit"),
                    BarcodeDisplayCheckDigit = ParseBool(barcodeData, "DisplayCheckDigit"),
                    BarcodeIntercharacterGap = (int)ParseDouble(barcodeData, "InterCharacterGap")
                };
            }
            case "LineDocumentItem" when geometryType == "PointGeometry":
                return Base(NiceLabel10ObjectKind.Line) with
                {
                    XMm = Thousandths(geometry!, "StartPointX"),
                    YMm = Thousandths(geometry!, "StartPointY"),
                    LineEndXMm = Thousandths(geometry!, "EndPointX"),
                    LineEndYMm = Thousandths(geometry!, "EndPointY")
                };
            case "RectangleDocumentItem" when geometryType == "RectGeometry":
                return Base(NiceLabel10ObjectKind.Rectangle) with
                {
                    XMm = Thousandths(geometry!, "Left"),
                    YMm = Thousandths(geometry!, "Top"),
                    WidthMm = Thousandths(geometry!, "Width"),
                    HeightMm = Thousandths(geometry!, "Height"),
                    CornerRadiusMm = Thousandths(item, "Radius")
                };
            case "GraphicDocumentItem" when geometryType == "RectGeometry":
            {
                var payload = ChildText(item.Element("EmbeddedGraphicContents"), "Base64StringValue")?.Trim();
                return Base(NiceLabel10ObjectKind.Graphic) with
                {
                    XMm = Thousandths(geometry!, "Left"),
                    YMm = Thousandths(geometry!, "Top"),
                    WidthMm = Thousandths(geometry!, "Width"),
                    HeightMm = Thousandths(geometry!, "Height"),
                    GraphicBase64 = string.IsNullOrEmpty(payload) ? null : payload,
                    GraphicFileName = ChildText(item, "GraphicFileName")?.Trim() is { Length: > 0 } fileName ? fileName : null,
                    GraphicResizeMode = (int)ParseDouble(item, "ResizeMode"),
                    GraphicKeepAspectRatio = ParseBool(item, "KeepAspectRatio"),
                    GraphicFlipHorizontal = ParseBool(item, "HorizontalMirror"),
                    GraphicFlipVertical = ParseBool(item, "VerticalMirror")
                };
            }
            default:
                return Base(NiceLabel10ObjectKind.Unsupported);
        }

        NiceLabel10ObjectRecord Base(NiceLabel10ObjectKind kind) => new(
            kind,
            name,
            type,
            0,
            0,
            0,
            0,
            0,
            0,
            content,
            variableName,
            fontFamily,
            fontSizePt,
            bold,
            italic,
            null,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            false,
            textAlignment,
            fillStyle,
            fillColor,
            strokeThickness,
            itemColor)
        {
            VariableId = variableId,
            HasUnresolvedReference = unresolved,
            ZOrder = (int)ParseDouble(item, "ZOrder"),
            IsPrintable = item.Element("IsPrintable") is null || ParseBool(item, "IsPrintable"),
            ItemId = ChildText(item, "Id")?.Trim(),
            PositionLocked = ParseBool(item, "PositionLocked"),
            InverseText = ParseBool(item, "InverseText"),
            RotationDegrees = ParseDouble(geometry, "Rotation"),
            AnchoringPoint = (int)ParseDouble(geometry, "AnchoringPoint"),
            Underline = underline,
            LineStyleValue = (int)ParseDouble(item, "LineStyle"),
            ContentTemplate = ReadContentTemplate(item) is { } contentTemplate
                ? (richText
                    ? NiceLabel10RichText.ToPlainText(contentTemplate)
                    : NiceLabel10RichText.DecodeStoredText(contentTemplate))
                : null,
            IsRichText = richText,
            ScaleFactorPercent = item.Element("ScaleFactor") is null ? 100 : ParseDouble(item, "ScaleFactor"),
            BestFitValue = (int)ParseDouble(item, "BestFit"),
            BestFitMinimumFontSizePt = ParseDouble(item, "BestFitMinimumFontSize"),
            BestFitMaximumFontSizePt = ParseDouble(item, "BestFitMaximumFontSize"),
            BestFitMinimumScalePercent = ParseDouble(item, "BestFitMinimumFontScaling"),
            BestFitMaximumScalePercent = ParseDouble(item, "BestFitMaximumFontScaling"),
            ContentMask = ParseBool(item, "HasMaskedContents") && ChildText(item, "ContentsMask") is { Length: > 0 } mask ? mask : null,
            ContentMaskCharacter = ChildText(item, "MaskValue") is { Length: > 0 } maskCharacter ? maskCharacter : "*"
        };
    }

    /// <summary>
    /// Mixed contents: fixed text with inline data-source references, each
    /// written as <c>[id]</c> in the stored string. Plain contents (no
    /// references) are reported through <c>FixedContents</c> instead.
    /// </summary>
    private static string? ReadContentTemplate(XElement item)
    {
        var contents = item.Element("Contents");
        if (contents is null
            || !string.Equals(contents.Attribute("Type")?.Value, "ExtendedDataValue", StringComparison.Ordinal))
        {
            return null;
        }

        var references = contents.Element("DataSourceReferences");
        if (references is null || !references.Elements("Item").Any())
        {
            return null;
        }

        return ChildText(contents.Element("FixedValue"), "StringValue");
    }

    private static (string? Id, string? Name, bool Unresolved) ResolveVariable(
        XElement item,
        IReadOnlyDictionary<string, NiceLabel10Variable> variables)
    {
        var reference = item.Element("DataSourceReference");
        var id = reference is null ? null : ChildText(reference, "Id")?.Trim();
        if (string.IsNullOrWhiteSpace(id))
        {
            return (null, null, false);
        }

        return variables.TryGetValue(id, out var variable)
            ? (id, variable.Name, false)
            : (id, null, true);
    }

    private static (string FontFamily, double FontSizePt, bool Bold, bool Italic, bool Underline) ParseFont(XElement item)
    {
        var descriptor = item.Element("FontDescriptor");
        if (descriptor is null)
        {
            return (string.Empty, 0, false, false, false);
        }

        var family = ChildText(descriptor, "Name") ?? string.Empty;
        var sizePt = ParseDouble(descriptor, "Height");
        var logFont = descriptor.Element("LogFontWrapper");
        var weight = ParseDouble(logFont, "Weight");
        var italic = ParseBool(logFont, "Italic");
        var underline = ParseBool(logFont, "Underline");
        if (string.IsNullOrWhiteSpace(family))
        {
            family = ChildText(logFont, "FaceName") ?? string.Empty;
        }

        return (family.Trim(), sizePt, weight >= 700, italic, underline);
    }

    private static string DecodeXml(byte[] bytes)
    {
        // GetString keeps a UTF-8 BOM as U+FEFF, which XDocument rejects.
        using var reader = new StreamReader(new MemoryStream(bytes), System.Text.Encoding.UTF8);
        return reader.ReadToEnd();
    }

    private static string? ChildText(XElement? parent, string name) =>
        parent?.Element(name)?.Value;

    /// <summary>
    /// NiceLabel object groups: <c>GroupedItems</c> holds one
    /// <c>SolutionItemReferenceCollectionWrapper</c> per group, listing its
    /// members' ids.
    /// </summary>
    private static IReadOnlyList<IReadOnlyList<string>> ReadGroups(XElement root)
        => (root.Element("GroupedItems")?.Elements("Item") ?? Enumerable.Empty<XElement>())
            .Select(group => (IReadOnlyList<string>)(group.Element("SolutionItemReferences")?.Elements("Item") ?? Enumerable.Empty<XElement>())
                .Select(member => ChildText(member, "Id")?.Trim())
                .OfType<string>()
                .Where(id => id.Length > 0)
                .ToArray())
            .Where(members => members.Count > 1)
            .ToArray();

    /// <summary>
    /// Items of a NiceLabel 10 layer (Document Design › Layers, its
    /// ChildReferences) that has a visibility condition, by item id.
    /// </summary>
    private static Dictionary<string, NiceLabel10VisibilityCondition> LayerConditions(XElement root)
    {
        var conditions = new Dictionary<string, NiceLabel10VisibilityCondition>(StringComparer.OrdinalIgnoreCase);
        foreach (var layer in root.Descendants("Layer"))
        {
            if (ReadVisibilityCondition(layer) is not { } condition)
            {
                continue;
            }

            foreach (var reference in layer.Element("ChildReferences")?.Elements("Item") ?? Enumerable.Empty<XElement>())
            {
                if (ChildText(reference, "Id")?.Trim() is { Length: > 0 } id)
                {
                    conditions[id] = condition;
                }
            }
        }

        return conditions;
    }

    /// <summary>
    /// A stored visibility condition (vendor DocumentItem): the left and right
    /// values (a data source or fixed text) and the operator. Null when the
    /// item has none or a value is missing, which NiceLabel also shows.
    /// </summary>
    private static NiceLabel10VisibilityCondition? ReadVisibilityCondition(XElement element)
    {
        if (!ParseBool(element, "IsVisibilityConditional")
            || ReadConditionValue(element.Element("VisibilityConditionLeftValue")) is not { } left
            || ReadConditionValue(element.Element("VisibilityConditionRightValue")) is not { } right)
        {
            return null;
        }

        var comparison = ChildText(element, "VisibilityConditionOperator")?.Trim();
        var unequal = comparison == "1" || string.Equals(comparison, "Unequals", StringComparison.OrdinalIgnoreCase);
        return new NiceLabel10VisibilityCondition(left, right, unequal);
    }

    private static NiceLabel10ConditionValue? ReadConditionValue(XElement? value)
    {
        if (value is null)
        {
            return null;
        }

        if (ChildText(value.Element("DataSourceReference"), "Id")?.Trim() is { Length: > 0 } id)
        {
            return new NiceLabel10ConditionValue(id, null);
        }

        return value.Element("FixedValue") is { } fixedValue
            ? new NiceLabel10ConditionValue(null, ChildText(fixedValue, "StringValue") ?? string.Empty)
            : null;
    }

    /// <summary>
    /// A stored HRI font is bold when its LOGFONT weight is 600 or more; no
    /// stored font is the vendor default, Arial bold.
    /// </summary>
    private static bool HriFontIsBold(XElement? descriptor) =>
        descriptor is null || ParseDouble(descriptor.Element("LogFontWrapper"), "Weight") >= 600;

    private static double Thousandths(XElement? parent, string name) =>
        ParseDouble(parent, name) / ThousandthsPerMm;

    private static double ParseDouble(XElement? parent, string name)
    {
        var text = ChildText(parent, name);
        return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
            ? value
            : 0;
    }

    private static bool ParseBool(XElement? parent, string name)
    {
        var text = ChildText(parent, name);
        if (bool.TryParse(text, out var value))
        {
            return value;
        }

        return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number)
            && number != 0;
    }
}
