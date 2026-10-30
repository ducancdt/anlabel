using System.Globalization;
using System.Text.RegularExpressions;
using ANLAbel.Core.Data;
using ANLAbel.Core.Models;

namespace ANLAbel.Core.Printing;

/// <summary>
/// Values fixed once per print job and shared by all its labels: the job time
/// (<c>{#NOW...}</c>) and operator-entered prompts (<c>{?Name}</c>). They are
/// written into every label row, so the manifest hashes them, and stored with
/// the job so a reprint prints the same date and the same answers.
/// </summary>
public static partial class JobVariableContract
{
    public const string DefaultDateFormat = "dd/MM/yyyy";
    public const int MaximumPromptLength = 200;

    /// <summary>How a date prompt's answer is kept with the job (and replayed by a reprint).</summary>
    public const string DateAnswerFormat = "yyyy-MM-dd";

    /// <summary>
    /// The job-level tokens (text inside braces) used by visible objects, in
    /// their contents or their visibility condition: <c>#NOW...</c> date
    /// tokens and <c>?Name</c> prompts.
    /// </summary>
    public static IReadOnlyList<string> FindTokens(LabelTemplate template)
    {
        ArgumentNullException.ThrowIfNull(template);
        var functions = FunctionsByName(template);
        var tokens = new List<string>();
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        void Visit(IEnumerable<string> names)
        {
            foreach (var name in names)
            {
                if (IsDateToken(name) || IsPromptToken(name))
                {
                    if (!tokens.Contains(name, StringComparer.Ordinal))
                    {
                        tokens.Add(name);
                    }
                }
                else if (functions.TryGetValue(name, out var function) && visited.Add(name))
                {
                    // A function the label prints reads its own fields: the
                    // prompts and dates among them are asked for this job too.
                    Visit(ExpressionTokens(function.Formula));
                }
            }
        }

        foreach (var expression in template.Objects
                     .Where(item => item.IsVisible)
                     .SelectMany(item => new[] { item.BindingExpression, item.VisibilityCondition })
                     .Where(expression => !string.IsNullOrWhiteSpace(expression)))
        {
            Visit(ExpressionTokens(expression));
        }

        return tokens.ToArray();
    }

    /// <summary>
    /// The template's functions (Data Manager "Hàm") that read a print-time
    /// value, a prompt, the job date or the serial number, directly or through
    /// another function, in template order. NiceLabel functions read prompted
    /// variables and counters; these run once the values are known
    /// (<see cref="TryApply"/>, <see cref="ApplyForDesign"/>, and
    /// <see cref="TryApplySerialTransforms"/> for the serial), the others on
    /// the data row.
    /// </summary>
    public static IReadOnlyList<DataTransformDefinition> JobDependentTransforms(LabelTemplate template)
    {
        ArgumentNullException.ThrowIfNull(template);
        return ClassifyTransforms(template.DataTransforms).PrintTime;
    }

    /// <summary>
    /// Once each row has its serial number: evaluates the functions that read
    /// <c>{#SERIAL}</c>, directly or through another function, and writes
    /// their outputs (the job's prompts and date are already in the rows).
    /// </summary>
    public static bool TryApplySerialTransforms(
        LabelTemplate template,
        IReadOnlyList<IReadOnlyDictionary<string, string>?> rows,
        out IReadOnlyDictionary<string, string>?[] withValues,
        out string error)
    {
        ArgumentNullException.ThrowIfNull(template);
        ArgumentNullException.ThrowIfNull(rows);
        withValues = rows.ToArray();
        error = string.Empty;
        var serial = ClassifyTransforms(template.DataTransforms).Serial;
        if (serial.Length == 0)
        {
            return true;
        }

        var prepared = new IReadOnlyDictionary<string, string>?[rows.Count];
        for (var i = 0; i < rows.Count; i++)
        {
            var copy = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (rows[i] is { } row)
            {
                foreach (var pair in row)
                {
                    copy[pair.Key] = pair.Value;
                }
            }

            if (!TryApplyJobTransforms(serial, copy, out error))
            {
                withValues = Array.Empty<IReadOnlyDictionary<string, string>?>();
                return false;
            }

            prepared[i] = copy;
        }

        withValues = prepared;
        return true;
    }

    /// <summary>
    /// The functions that read a print-time value, and among them those that
    /// read the serial number (given after the job values), each closed over
    /// functions that read them.
    /// </summary>
    private static (DataTransformDefinition[] PrintTime, DataTransformDefinition[] Serial) ClassifyTransforms(IEnumerable<DataTransformDefinition> transforms)
    {
        var all = transforms.ToArray();
        var reads = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        foreach (var transform in all.Where(transform => !string.IsNullOrWhiteSpace(transform.Name)))
        {
            reads.TryAdd(transform.Name.Trim(), ExpressionTokens(transform.Formula).ToArray());
        }

        HashSet<string> Readers(Func<string, bool> reads0)
        {
            var found = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            bool grew;
            do
            {
                grew = false;
                foreach (var (name, fields) in reads)
                {
                    if (!found.Contains(name) && fields.Any(field => reads0(field) || found.Contains(field)))
                    {
                        grew |= found.Add(name);
                    }
                }
            }
            while (grew);

            return found;
        }

        DataTransformDefinition[] Pick(HashSet<string> names) => all.Where(transform => names.Contains(transform.Name?.Trim() ?? string.Empty)).ToArray();
        return (
            Pick(Readers(field => IsDateToken(field) || IsPromptToken(field) || IsSerialToken(field))),
            Pick(Readers(field => IsSerialToken(field))));
    }

    /// <summary>The functions evaluated with the job values: print-time ones not waiting for the serial.</summary>
    private static DataTransformDefinition[] JobValueTransforms(LabelTemplate template)
    {
        var (printTime, serial) = ClassifyTransforms(template.DataTransforms);
        return printTime.Where(transform => !serial.Contains(transform)).ToArray();
    }

    /// <summary>
    /// A design-time value for every prompt and date the functions read (the
    /// last answer, the default, today, else "‹Name›"), so a function can be
    /// tried before the job is printed.
    /// </summary>
    public static IReadOnlyDictionary<string, string> DesignValuesForTransforms(
        IReadOnlyList<PromptVariableDefinition> prompts,
        IEnumerable<DataTransformDefinition> transforms,
        DateTimeOffset now,
        IReadOnlyDictionary<string, string> knownPromptValues,
        string? serialSample = null)
    {
        ArgumentNullException.ThrowIfNull(prompts);
        ArgumentNullException.ThrowIfNull(transforms);
        ArgumentNullException.ThrowIfNull(knownPromptValues);
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var token in transforms
                     .Where(transform => !string.IsNullOrWhiteSpace(transform.Formula))
                     .SelectMany(transform => ExpressionTokens(transform.Formula))
                     .Where(token => IsDateToken(token) || IsPromptToken(token) || (serialSample is not null && IsSerialToken(token))))
        {
            // The serial only when the label numbers itself: otherwise the
            // function cannot get one and its check says so.
            values.TryAdd(token, IsSerialToken(token) ? serialSample! : DesignTokenValue(token, prompts, now, knownPromptValues));
        }

        return values;
    }

    private static Dictionary<string, DataTransformDefinition> FunctionsByName(LabelTemplate template)
    {
        var functions = new Dictionary<string, DataTransformDefinition>(StringComparer.OrdinalIgnoreCase);
        foreach (var transform in template.DataTransforms.Where(transform => !string.IsNullOrWhiteSpace(transform.Name)))
        {
            functions.TryAdd(transform.Name.Trim(), transform);
        }

        return functions;
    }

    /// <summary>
    /// Evaluates the functions that read print-time values over a row that
    /// already holds those values and writes their outputs into it.
    /// </summary>
    private static bool TryApplyJobTransforms(IReadOnlyList<DataTransformDefinition> dependent, Dictionary<string, string> row, out string error)
    {
        error = string.Empty;
        if (dependent.Count == 0)
        {
            return true;
        }

        foreach (var transform in dependent)
        {
            row.Remove(transform.Name.Trim());
        }

        var result = DataTransformPipeline.Evaluate(
            DataRecord.Create(row.Select(pair => new KeyValuePair<string, string?>(pair.Key, pair.Value))),
            dependent);
        if (!result.IsValid)
        {
            error = string.Join(" ", result.Errors);
            return false;
        }

        foreach (var transform in dependent)
        {
            if (result.Record.Values.TryGetValue(transform.Name.Trim(), out var value))
            {
                row[transform.Name.Trim()] = value ?? string.Empty;
            }
        }

        return true;
    }

    private static readonly IReadOnlyDictionary<string, string> EmptyRow =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// The names an expression reads: <c>{Name}</c> placeholders, or the
    /// FIELD names of a formula (which may name prompts and date fields too).
    /// </summary>
    private static IEnumerable<string> ExpressionTokens(string expression)
    {
        if (Expressions.Formulas.FormulaBindingEvaluator.LooksLikeFormula(expression))
        {
            return Expressions.Formulas.FormulaBindingEvaluator.Evaluate(expression, EmptyRow).UsedFields
                .Select(field => field.Trim());
        }

        return TokenPattern().Matches(expression).Select(match => match.Groups[1].Value.Trim());
    }

    public static bool UsesJobVariables(LabelTemplate template) => FindTokens(template).Count > 0;

    public static bool IsDateToken(string token)
        => token.StartsWith("#NOW", StringComparison.OrdinalIgnoreCase);

    public static bool IsPromptToken(string token)
        => token.Length > 1 && token[0] == '?';

    /// <summary>The label's serial number field (<c>{#SERIAL}</c>), given each label at print time.</summary>
    public static bool IsSerialToken(string? token)
        => string.Equals(token?.Trim(), SerialCounterContract.Token, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// The prompts the operator must answer. Defined variables come in the order of
    /// <see cref="LabelTemplate.PromptVariables"/> (Quản lý dữ liệu → Thứ tự nhắc, as
    /// NiceLabel 10's prompt order); a token without a definition follows as a required
    /// free-text prompt named after it, in order of first use.
    /// </summary>
    public static IReadOnlyList<PromptVariableDefinition> RequiredPrompts(LabelTemplate template)
    {
        ArgumentNullException.ThrowIfNull(template);
        var result = new List<(PromptVariableDefinition Prompt, int Order)>();
        foreach (var token in FindTokens(template).Where(IsPromptToken))
        {
            var name = token[1..].Trim();
            var order = -1;
            for (var index = 0; index < template.PromptVariables.Count; index++)
            {
                if (string.Equals(template.PromptVariables[index].Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    order = index;
                    break;
                }
            }

            result.Add(order >= 0
                ? (template.PromptVariables[order], order)
                : (new PromptVariableDefinition { Name = name }, int.MaxValue));
        }

        // OrderBy is stable: undefined prompts keep their order of first use.
        return result.OrderBy(entry => entry.Order).Select(entry => entry.Prompt).ToArray();
    }

    /// <summary>Problem with one entered value, or empty when it is acceptable.</summary>
    /// <summary>
    /// Problems in a prompt's own rules that would turn back every answer
    /// (NiceLabel 10 refuses a maximum length below the minimum); empty when
    /// the operator can answer it.
    /// </summary>
    public static IReadOnlyList<string> ValidateDefinition(PromptVariableDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        return !IsDatePrompt(definition) && definition.MinLength > 0 && definition.MaxLength > 0 && definition.MinLength > definition.MaxLength
            ? [$"Prompt '{definition.Name}' needs at least {definition.MinLength} characters but allows at most {definition.MaxLength}."]
            : [];
    }

    public static string ValidatePromptValue(PromptVariableDefinition definition, string? value)
    {
        ArgumentNullException.ThrowIfNull(definition);
        var text = value ?? string.Empty;
        var label = string.IsNullOrWhiteSpace(definition.Prompt) ? definition.Name : definition.Prompt;
        if (definition.Required && string.IsNullOrWhiteSpace(text))
        {
            return $"Prompt '{label}' needs a value.";
        }

        if (IsDatePrompt(definition))
        {
            // A date is picked or typed and kept as yyyy-MM-dd; the text rules do not apply.
            return string.IsNullOrWhiteSpace(text) || TryParsePromptDate(definition, text, out _)
                ? string.Empty
                : $"Prompt '{label}' needs a date such as {DateExample(definition)}.";
        }

        var limit = definition.MaxLength > 0 ? Math.Min(definition.MaxLength, MaximumPromptLength) : MaximumPromptLength;
        if (text.Length > limit)
        {
            return $"Prompt '{label}' allows at most {limit} characters; {text.Length} were entered.";
        }

        if (definition.DigitsOnly && !text.All(char.IsAsciiDigit))
        {
            return $"Prompt '{label}' accepts digits only.";
        }

        // NiceLabel 10 input rules: allowed characters, pick list, fixed length
        // (a padded value may be typed shorter; padding fills it).
        if (definition.AllowedCharacters.Length > 0 && text.FirstOrDefault(c => !definition.AllowedCharacters.Contains(c)) is var refused and not '\0')
        {
            return $"Prompt '{label}' does not accept '{refused}'; allowed characters: {definition.AllowedCharacters}.";
        }

        var pickList = definition.PickListValues;
        if (pickList.Count > 0 && text.Length > 0 && !pickList.Contains(text, StringComparer.Ordinal))
        {
            return $"Prompt '{label}' must be one of: {string.Join(", ", pickList)}.";
        }

        if (definition.FixedLength
            && definition.MaxLength > 0
            && definition.Padding == ANLAbel.Core.Enums.PromptPadding.None
            && text.Length > 0
            && text.Length != definition.MaxLength)
        {
            return $"Prompt '{label}' needs exactly {definition.MaxLength} characters; {text.Length} were entered.";
        }

        // Minimum length catches a value scanned or typed short; padding up to
        // the maximum length fills a shorter one, as for a fixed length.
        var padded = definition.Padding != ANLAbel.Core.Enums.PromptPadding.None && definition.MaxLength >= definition.MinLength;
        if (definition.MinLength > 0 && !padded && text.Length > 0 && text.Length < definition.MinLength)
        {
            return $"Prompt '{label}' needs at least {definition.MinLength} characters; {text.Length} were entered.";
        }

        return string.Empty;
    }

    /// <summary>
    /// The printed value of a prompt answer: NiceLabel 10 output rules, padding
    /// up to the maximum length then the prefix and suffix, for a non-empty
    /// value. The answer itself is what a reprint stores and replays.
    /// </summary>
    public static string FormatPromptValue(PromptVariableDefinition definition, string? answer)
    {
        ArgumentNullException.ThrowIfNull(definition);
        var text = answer ?? string.Empty;
        if (text.Length == 0)
        {
            return text;
        }

        if (IsDatePrompt(definition) && TryParsePromptDate(definition, text, out var date))
        {
            try
            {
                text = date.ToString(definition.DateFormat, CultureInfo.InvariantCulture);
            }
            catch (FormatException)
            {
                // A format .NET cannot read prints the date as kept.
            }
        }

        if (definition.MaxLength > text.Length)
        {
            var pad = definition.PadCharacter[0];
            text = definition.Padding switch
            {
                ANLAbel.Core.Enums.PromptPadding.Left => text.PadLeft(definition.MaxLength, pad),
                ANLAbel.Core.Enums.PromptPadding.Right => text.PadRight(definition.MaxLength, pad),
                _ => text
            };
        }

        return definition.Prefix + text + definition.Suffix;
    }

    /// <summary>A NiceLabel 10 date variable: the operator gives a date that prints in <see cref="PromptVariableDefinition.DateFormat"/>.</summary>
    public static bool IsDatePrompt(PromptVariableDefinition definition)
        => definition.DateFormat.Length > 0;

    /// <summary>
    /// Reads a date prompt's answer: yyyy-MM-dd as the job keeps it, the
    /// variable's own input format, or a Vietnamese dd/MM/yyyy.
    /// </summary>
    public static bool TryParsePromptDate(PromptVariableDefinition definition, string? text, out DateTime date)
    {
        ArgumentNullException.ThrowIfNull(definition);
        var formats = new List<string> { DateAnswerFormat, "dd/MM/yyyy", "d/M/yyyy" };
        if (definition.DateInputFormat.Length > 0)
        {
            formats.Insert(1, definition.DateInputFormat);
        }

        try
        {
            return DateTime.TryParseExact((text ?? string.Empty).Trim(), formats.ToArray(), CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
        }
        catch (FormatException)
        {
            // An input format .NET cannot read accepts the other forms only.
            formats.Remove(definition.DateInputFormat);
            return DateTime.TryParseExact((text ?? string.Empty).Trim(), formats.ToArray(), CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
        }
    }

    /// <summary>The answer as the job keeps it: a date prompt's date as yyyy-MM-dd, any other value as given.</summary>
    public static string NormalizePromptAnswer(PromptVariableDefinition definition, string? answer)
        => IsDatePrompt(definition) && TryParsePromptDate(definition, answer, out var date)
            ? date.ToString(DateAnswerFormat, CultureInfo.InvariantCulture)
            : answer ?? string.Empty;

    private static string DateExample(PromptVariableDefinition definition)
    {
        var example = new DateTime(2026, 12, 31);
        try
        {
            return example.ToString(definition.DateInputFormat.Length > 0 ? definition.DateInputFormat : "dd/MM/yyyy", CultureInfo.InvariantCulture);
        }
        catch (FormatException)
        {
            return example.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
        }
    }

    /// <summary>Formats a <c>#NOW[+-n unit]...[:format]</c> token at the job time.</summary>
    public static bool TryFormatDate(string token, DateTimeOffset jobTime, out string value, out string error)
    {
        value = string.Empty;
        error = string.Empty;
        var match = DatePattern().Match(token.Trim());
        if (!match.Success)
        {
            error = $"Date field '{{{token}}}' is not understood. Use {{#NOW}}, {{#NOW:dd/MM/yyyy}} or {{#NOW+30d:dd/MM/yyyy}} (units d, w, mo, y, h).";
            return false;
        }

        var time = jobTime.LocalDateTime;
        foreach (Capture offset in match.Groups["offset"].Captures)
        {
            var part = OffsetPattern().Match(offset.Value);
            var amount = int.Parse(part.Groups["n"].Value, CultureInfo.InvariantCulture) * (part.Groups["sign"].Value == "-" ? -1 : 1);
            time = part.Groups["unit"].Value.ToLowerInvariant() switch
            {
                "d" => time.AddDays(amount),
                "w" => time.AddDays(7 * amount),
                "mo" => time.AddMonths(amount),
                "y" => time.AddYears(amount),
                _ => time.AddHours(amount),
            };
        }

        var format = match.Groups["format"].Success && match.Groups["format"].Value.Length > 0
            ? match.Groups["format"].Value
            : DefaultDateFormat;
        try
        {
            value = time.ToString(format, CultureInfo.InvariantCulture);
            return true;
        }
        catch (FormatException)
        {
            error = $"Date field '{{{token}}}' has an invalid format '{format}'.";
            return false;
        }
    }

    /// <summary>
    /// Copies of <paramref name="rows"/> with every job token's value. Fails on
    /// an unreadable date token or a prompt value that breaks its definition.
    /// </summary>
    public static bool TryApply(
        LabelTemplate template,
        IReadOnlyList<IReadOnlyDictionary<string, string>?> rows,
        DateTimeOffset jobTime,
        IReadOnlyDictionary<string, string> promptValues,
        out IReadOnlyDictionary<string, string>?[] withValues,
        out string error)
    {
        ArgumentNullException.ThrowIfNull(template);
        ArgumentNullException.ThrowIfNull(rows);
        ArgumentNullException.ThrowIfNull(promptValues);
        withValues = Array.Empty<IReadOnlyDictionary<string, string>?>();
        if (!TryResolveValues(template, jobTime, promptValues, out var values, out error))
        {
            return false;
        }

        var dependent = JobValueTransforms(template);
        var prepared = new IReadOnlyDictionary<string, string>?[rows.Count];
        for (var i = 0; i < rows.Count; i++)
        {
            var copy = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (rows[i] is { } row)
            {
                foreach (var pair in row)
                {
                    copy[pair.Key] = pair.Value;
                }
            }

            foreach (var pair in values)
            {
                copy[pair.Key] = pair.Value;
            }

            if (!TryApplyJobTransforms(dependent, copy, out error))
            {
                return false;
            }

            prepared[i] = copy;
        }

        withValues = prepared;
        return true;
    }

    /// <summary>
    /// Designer-only row: date fields at <paramref name="now"/> and prompts from
    /// the last answers or defaults, with ‹Name› where nothing is known yet.
    /// Nothing is validated because nothing is printed.
    /// </summary>
    public static IReadOnlyDictionary<string, string>? ApplyForDesign(
        LabelTemplate template,
        IReadOnlyDictionary<string, string>? row,
        DateTimeOffset now,
        IReadOnlyDictionary<string, string> knownPromptValues)
    {
        ArgumentNullException.ThrowIfNull(template);
        var tokens = FindTokens(template);
        if (tokens.Count == 0)
        {
            return row;
        }

        var copy = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (row is not null)
        {
            foreach (var pair in row)
            {
                copy[pair.Key] = pair.Value;
            }
        }

        var prompts = RequiredPrompts(template);
        foreach (var token in tokens)
        {
            copy[token] = DesignTokenValue(token, prompts, now, knownPromptValues);
        }

        // A function reading a prompt shows its design value too.
        TryApplyJobTransforms(JobValueTransforms(template), copy, out _);
        return copy;
    }

    private static string DesignTokenValue(
        string token,
        IReadOnlyList<PromptVariableDefinition> prompts,
        DateTimeOffset now,
        IReadOnlyDictionary<string, string> knownPromptValues)
    {
        if (IsDateToken(token))
        {
            return TryFormatDate(token, now, out var date, out _) ? date : "{" + token + "}";
        }

        var name = token[1..].Trim();
        var definition = prompts.FirstOrDefault(item => string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase))
            ?? new PromptVariableDefinition { Name = name };
        return knownPromptValues.TryGetValue(definition.Name, out var known) && !string.IsNullOrEmpty(known)
            ? FormatPromptValue(definition, known)
            : !string.IsNullOrEmpty(definition.DefaultValue) ? FormatPromptValue(definition, definition.DefaultValue)
            // A date prompt shows the job day in its print format until a date is given.
            : IsDatePrompt(definition) ? FormatPromptValue(definition, now.LocalDateTime.ToString(DateAnswerFormat, CultureInfo.InvariantCulture))
            : $"‹{definition.Name}›";
    }

    /// <summary>The value of every job token, keyed by the token text.</summary>
    public static bool TryResolveValues(
        LabelTemplate template,
        DateTimeOffset jobTime,
        IReadOnlyDictionary<string, string> promptValues,
        out IReadOnlyDictionary<string, string> values,
        out string error)
    {
        var resolved = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        values = resolved;
        error = string.Empty;
        var prompts = RequiredPrompts(template);
        var tokens = FindTokens(template);
        var caseClash = tokens.GroupBy(token => token, StringComparer.OrdinalIgnoreCase).FirstOrDefault(group => group.Count() > 1);
        if (caseClash is not null)
        {
            // Rows look fields up without regard to case, but MM (month) and mm (minute) differ.
            error = $"Fields {string.Join(" and ", caseClash.Select(token => "{" + token + "}"))} differ only in letter case, so they cannot both be printed. Rename one (for a date, add a different offset or format).";
            return false;
        }

        foreach (var token in tokens)
        {
            if (IsDateToken(token))
            {
                if (!TryFormatDate(token, jobTime, out var date, out error))
                {
                    return false;
                }

                resolved[token] = date;
                continue;
            }

            var definition = prompts.First(item => string.Equals(item.Name, token[1..].Trim(), StringComparison.OrdinalIgnoreCase));
            promptValues.TryGetValue(definition.Name, out var answer);
            error = ValidatePromptValue(definition, answer);
            if (!string.IsNullOrEmpty(error))
            {
                return false;
            }

            resolved[token] = FormatPromptValue(definition, answer);
        }

        return true;
    }

    /// <summary>
    /// The expression with renamed prompts: <c>{?Old}</c> becomes <c>{?New}</c>
    /// and a formula's <c>FIELD("?Old")</c> becomes <c>FIELD("?New")</c>, every
    /// name in one pass (so two prompts can swap names); everything else stays
    /// as written. <paramref name="renames"/> maps old to new names and should
    /// ignore case, as prompts are looked up. NiceLabel binds a variable by its
    /// id, so a renamed variable keeps every object that uses it.
    /// </summary>
    public static string RenamePrompts(string? expression, IReadOnlyDictionary<string, string> renames)
    {
        ArgumentNullException.ThrowIfNull(renames);
        if (string.IsNullOrEmpty(expression) || renames.Count == 0)
        {
            return expression ?? string.Empty;
        }

        string Rename(Match match) => renames.TryGetValue(match.Groups["name"].Value.Trim(), out var renamed)
            ? match.Groups["open"].Value + renamed + match.Groups["close"].Value
            : match.Value;
        return PromptFieldPattern().Replace(PromptPlaceholderPattern().Replace(expression, Rename), Rename);
    }

    [GeneratedRegex(@"(?<open>\{\s*\?)(?<name>[^{}]+?)(?<close>\s*\})")]
    private static partial Regex PromptPlaceholderPattern();

    [GeneratedRegex(@"(?<open>FIELD\s*\(\s*""\?)(?<name>[^""]+)(?<close>""\s*\))", RegexOptions.IgnoreCase)]
    private static partial Regex PromptFieldPattern();

    [GeneratedRegex(@"\{([^{}]+)\}")]
    private static partial Regex TokenPattern();

    [GeneratedRegex(@"^#NOW(?<offset>[+-]\d{1,5}(?:mo|d|w|y|h))*(?::(?<format>.*))?$", RegexOptions.IgnoreCase)]
    private static partial Regex DatePattern();

    [GeneratedRegex(@"^(?<sign>[+-])(?<n>\d{1,5})(?<unit>mo|d|w|y|h)$", RegexOptions.IgnoreCase)]
    private static partial Regex OffsetPattern();
}
