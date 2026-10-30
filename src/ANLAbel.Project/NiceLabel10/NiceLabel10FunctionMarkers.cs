using System.Text;

namespace ANLAbel.Project.NiceLabel10;

/// <summary>
/// Marks, inside the importer's <c>[id]</c> templates, a span whose value a
/// formula function must transform at print time:
/// <c>Start NAME[:arg:arg] Body inner End</c> (three control characters
/// NiceLabel never stores). A template that still carries a marker after
/// reference expansion is rewritten into an ANLAbel formula
/// (<c>CONCAT</c>/<c>FIELD</c> plus the named function), because a
/// <c>{Field}</c> placeholder expression cannot call functions.
/// </summary>
internal static class NiceLabel10FunctionMarkers
{
    public const char Start = '\u0001';
    public const char Body = '\u0002';
    public const char End = '\u0003';

    public static bool ContainsMarker(string expression) => expression.IndexOf(Start) >= 0;

    /// <summary>
    /// Rewrites a <c>{Field}</c> expression carrying function markers into a
    /// formula: literal text becomes string literals, placeholders
    /// <c>FIELD("name")</c>, marked spans <c>NAME(inner, args…)</c>; several
    /// terms are joined with <c>CONCAT</c>. Returns null for an unbalanced
    /// marker or placeholder.
    /// </summary>
    public static string? ToFormula(string expression)
    {
        var terms = Terms(expression);
        if (terms is null)
        {
            return null;
        }

        return terms.Count == 1 ? terms[0] : "CONCAT(" + string.Join(", ", terms) + ")";
    }

    private static List<string>? Terms(string text)
    {
        var terms = new List<string>();
        var literal = new StringBuilder();
        var index = 0;
        while (index < text.Length)
        {
            var ch = text[index];
            if (ch == '{')
            {
                var close = text.IndexOf('}', index + 1);
                if (close < 0)
                {
                    return null;
                }

                FlushLiteral();
                terms.Add("FIELD(" + Quote(text[(index + 1)..close].Trim()) + ")");
                index = close + 1;
                continue;
            }

            if (ch == Start)
            {
                var bodyStart = text.IndexOf(Body, index + 1);
                if (bodyStart < 0)
                {
                    return null;
                }

                var head = text[(index + 1)..bodyStart].Split(':');
                var end = FindEnd(text, bodyStart + 1);
                if (end < 0)
                {
                    return null;
                }

                var inner = Terms(text[(bodyStart + 1)..end]);
                if (inner is null)
                {
                    return null;
                }

                FlushLiteral();
                var innerTerm = inner.Count switch
                {
                    0 => Quote(string.Empty),
                    1 => inner[0],
                    _ => "CONCAT(" + string.Join(", ", inner) + ")"
                };
                var arguments = head.Skip(1).Select(Quote);
                terms.Add(head[0] + "(" + string.Join(", ", new[] { innerTerm }.Concat(arguments)) + ")");
                index = end + 1;
                continue;
            }

            if (ch is Body or End or '}')
            {
                return null;
            }

            literal.Append(ch);
            index++;
        }

        FlushLiteral();
        return terms;

        void FlushLiteral()
        {
            if (literal.Length > 0)
            {
                terms.Add(Quote(literal.ToString()));
                literal.Clear();
            }
        }
    }

    /// <summary>Index of the End marker matching a Body position, honouring nesting.</summary>
    private static int FindEnd(string text, int from)
    {
        var depth = 0;
        for (var index = from; index < text.Length; index++)
        {
            switch (text[index])
            {
                case Start:
                    depth++;
                    break;
                case End:
                    if (depth == 0)
                    {
                        return index;
                    }

                    depth--;
                    break;
            }
        }

        return -1;
    }

    /// <summary>A formula string literal holding <paramref name="value"/>.</summary>
    public static string Quote(string value)
    {
        var builder = new StringBuilder("\"");
        foreach (var ch in value)
        {
            switch (ch)
            {
                case '"':
                    builder.Append("\\\"");
                    break;
                case '\\':
                    builder.Append("\\\\");
                    break;
                case '\n':
                    builder.Append("\\n");
                    break;
                case '\r':
                    builder.Append("\\r");
                    break;
                case '\t':
                    builder.Append("\\t");
                    break;
                default:
                    builder.Append(ch);
                    break;
            }
        }

        return builder.Append('"').ToString();
    }
}
