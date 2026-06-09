using System.Text.RegularExpressions;

namespace ANLAbel.Core.Expressions;

public static partial class FieldNameResolver
{
    public static bool TryResolveFieldName(string? requestedFieldName, IEnumerable<string> availableFieldNames, out string resolvedFieldName)
    {
        resolvedFieldName = string.Empty;
        if (string.IsNullOrWhiteSpace(requestedFieldName))
        {
            return false;
        }

        var candidates = availableFieldNames
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (candidates.Length == 0)
        {
            return false;
        }

        var exact = candidates.FirstOrDefault(name => string.Equals(name, requestedFieldName, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(exact))
        {
            resolvedFieldName = exact;
            return true;
        }

        var normalizedRequested = Normalize(requestedFieldName);
        if (string.IsNullOrWhiteSpace(normalizedRequested))
        {
            return false;
        }

        var requestedHasSeparators = ContainsSeparator(requestedFieldName);
        if (requestedHasSeparators)
        {
            var requestedTokenKey = string.Join("|", Tokenize(requestedFieldName));
            var tokenMatch = candidates
                .Where(name => string.Join("|", Tokenize(name)) == requestedTokenKey)
                .OrderBy(name => Math.Abs(name.Length - requestedFieldName.Length))
                .FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(tokenMatch))
            {
                resolvedFieldName = tokenMatch;
                return true;
            }
        }

        var normalized = candidates
            .Where(name => string.Equals(Normalize(name), normalizedRequested, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(name => ContainsSeparator(name) == requestedHasSeparators)
            .ThenBy(name => Math.Abs(name.Length - requestedFieldName.Length))
            .FirstOrDefault();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return false;
        }

        resolvedFieldName = normalized;
        return true;
    }

    public static bool TryGetValue(IReadOnlyDictionary<string, string> row, string? requestedFieldName, out string value, out string resolvedFieldName)
    {
        value = string.Empty;
        if (!TryResolveFieldName(requestedFieldName, row.Keys, out resolvedFieldName))
        {
            return false;
        }

        value = row.TryGetValue(resolvedFieldName, out var exactValue) ? exactValue ?? string.Empty : string.Empty;
        return true;
    }

    public static bool TryGetNullableValue(IReadOnlyDictionary<string, string?> row, string? requestedFieldName, out string value, out string resolvedFieldName)
    {
        value = string.Empty;
        if (!TryResolveFieldName(requestedFieldName, row.Keys, out resolvedFieldName))
        {
            return false;
        }

        value = row.TryGetValue(resolvedFieldName, out var exactValue) ? exactValue?.Trim() ?? string.Empty : string.Empty;
        return true;
    }

    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return NonAlphaNumeric().Replace(value, string.Empty).ToUpperInvariant();
    }

    [GeneratedRegex("[^\\p{L}\\p{Nd}]")]
    private static partial Regex NonAlphaNumeric();

    private static bool ContainsSeparator(string? value)
    {
        return !string.IsNullOrWhiteSpace(value) && value.Any(character => !char.IsLetterOrDigit(character));
    }

    private static IReadOnlyList<string> Tokenize(string value)
    {
        return value
            .Split([' ', '\t', '\r', '\n', '-', '_', '/', '\\', '.', ':', ';', ',', '|'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(token => token.ToUpperInvariant())
            .ToArray();
    }
}
