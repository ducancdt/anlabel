using System.Text.RegularExpressions;

namespace ANLAbel.Core.Expressions;

public static partial class FieldNameResolver
{
    public static bool TryResolveFieldName(string? requestedFieldName, IEnumerable<string> availableFieldNames, out string resolvedFieldName)
    {
        // One ranking for printing and for the tie check: a second copy of it
        // could drift, and then the check would vouch for a different column
        // than the one printed.
        var ties = FindTies(requestedFieldName, availableFieldNames);
        resolvedFieldName = ties.Count > 0 ? ties[0] : string.Empty;
        return ties.Count > 0;
    }

    /// <summary>
    /// The available names that tie for <paramref name="requestedFieldName"/>, in the
    /// given order; the first is the one <see cref="TryResolveFieldName"/> picks. An
    /// exact name (ignoring case) wins, then the same words, then the same letters and
    /// digits, preferring a name with separators like the request and the closest
    /// length. More than one means only the order of the names (the column order of
    /// the file) decides which field is read.
    /// </summary>
    public static IReadOnlyList<string> FindTies(string? requestedFieldName, IEnumerable<string> availableFieldNames)
    {
        if (string.IsNullOrWhiteSpace(requestedFieldName))
        {
            return Array.Empty<string>();
        }

        var candidates = availableFieldNames
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var exact = candidates.FirstOrDefault(name => string.Equals(name, requestedFieldName, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(exact))
        {
            return new[] { exact };
        }

        var normalizedRequested = Normalize(requestedFieldName);
        if (string.IsNullOrWhiteSpace(normalizedRequested))
        {
            return Array.Empty<string>();
        }

        var requestedHasSeparators = ContainsSeparator(requestedFieldName);
        if (requestedHasSeparators)
        {
            var requestedTokenKey = string.Join("|", Tokenize(requestedFieldName));
            var tokenMatches = candidates
                .Where(name => string.Join("|", Tokenize(name)) == requestedTokenKey)
                .ToArray();
            if (tokenMatches.Length > 0)
            {
                return ClosestInLength(tokenMatches, requestedFieldName);
            }
        }

        var normalized = candidates
            .Where(name => string.Equals(Normalize(name), normalizedRequested, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (normalized.Length == 0)
        {
            return Array.Empty<string>();
        }

        var sameSeparators = normalized.Where(name => ContainsSeparator(name) == requestedHasSeparators).ToArray();
        return ClosestInLength(sameSeparators.Length > 0 ? sameSeparators : normalized, requestedFieldName);
    }

    private static string[] ClosestInLength(string[] names, string requested)
    {
        var best = names.Min(name => Math.Abs(name.Length - requested.Length));
        return names.Where(name => Math.Abs(name.Length - requested.Length) == best).ToArray();
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

        // Preserve the raw value (no Trim) so this path matches the {field} brace
        // path (TryGetValue). Leading/trailing characters can be meaningful for a
        // traceability/label app, and the two syntaxes must yield identical output.
        value = row.TryGetValue(resolvedFieldName, out var exactValue) ? exactValue ?? string.Empty : string.Empty;
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
