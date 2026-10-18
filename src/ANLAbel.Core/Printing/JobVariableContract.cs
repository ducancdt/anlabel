using System.Globalization;
using System.Text.RegularExpressions;
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

    /// <summary>
    /// The job-level tokens (text inside braces) used by visible objects:
    /// <c>#NOW...</c> date tokens and <c>?Name</c> prompts.
    /// </summary>
    public static IReadOnlyList<string> FindTokens(LabelTemplate template)
    {
        ArgumentNullException.ThrowIfNull(template);
        return template.Objects
            .Where(item => item.IsVisible && !string.IsNullOrWhiteSpace(item.BindingExpression))
            .SelectMany(item => TokenPattern().Matches(item.BindingExpression).Select(match => match.Groups[1].Value.Trim()))
            .Where(token => IsDateToken(token) || IsPromptToken(token))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    public static bool UsesJobVariables(LabelTemplate template) => FindTokens(template).Count > 0;

    public static bool IsDateToken(string token)
        => token.StartsWith("#NOW", StringComparison.OrdinalIgnoreCase);

    public static bool IsPromptToken(string token)
        => token.Length > 1 && token[0] == '?';

    /// <summary>
    /// The prompts the operator must answer, in order of first use. A token
    /// without a definition is a required free-text prompt named after it.
    /// </summary>
    public static IReadOnlyList<PromptVariableDefinition> RequiredPrompts(LabelTemplate template)
    {
        ArgumentNullException.ThrowIfNull(template);
        var result = new List<PromptVariableDefinition>();
        foreach (var token in FindTokens(template).Where(IsPromptToken))
        {
            var name = token[1..].Trim();
            var definition = template.PromptVariables.FirstOrDefault(item =>
                string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase));
            result.Add(definition ?? new PromptVariableDefinition { Name = name });
        }

        return result;
    }

    /// <summary>Problem with one entered value, or empty when it is acceptable.</summary>
    public static string ValidatePromptValue(PromptVariableDefinition definition, string? value)
    {
        ArgumentNullException.ThrowIfNull(definition);
        var text = value ?? string.Empty;
        var label = string.IsNullOrWhiteSpace(definition.Prompt) ? definition.Name : definition.Prompt;
        if (definition.Required && string.IsNullOrWhiteSpace(text))
        {
            return $"Prompt '{label}' needs a value.";
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

        return string.Empty;
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

        withValues = new IReadOnlyDictionary<string, string>?[rows.Count];
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

            withValues[i] = copy;
        }

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
            if (IsDateToken(token))
            {
                copy[token] = TryFormatDate(token, now, out var date, out _) ? date : "{" + token + "}";
                continue;
            }

            var definition = prompts.First(item => string.Equals(item.Name, token[1..].Trim(), StringComparison.OrdinalIgnoreCase));
            copy[token] = knownPromptValues.TryGetValue(definition.Name, out var known) && !string.IsNullOrEmpty(known)
                ? known
                : !string.IsNullOrEmpty(definition.DefaultValue) ? definition.DefaultValue : $"‹{definition.Name}›";
        }

        return copy;
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

            resolved[token] = answer ?? string.Empty;
        }

        return true;
    }

    [GeneratedRegex(@"\{([^{}]+)\}")]
    private static partial Regex TokenPattern();

    [GeneratedRegex(@"^#NOW(?<offset>[+-]\d{1,5}(?:mo|d|w|y|h))*(?::(?<format>.*))?$", RegexOptions.IgnoreCase)]
    private static partial Regex DatePattern();

    [GeneratedRegex(@"^(?<sign>[+-])(?<n>\d{1,5})(?<unit>mo|d|w|y|h)$", RegexOptions.IgnoreCase)]
    private static partial Regex OffsetPattern();
}
