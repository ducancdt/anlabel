using System.Text;

namespace ANLAbel.Core.Expressions.Formulas;

public sealed class FormulaParser
{
    /// <summary>
    /// Decodes the body of a string literal (the text between the quotes) with
    /// the escape rules of <see cref="Parse"/>: a backslash before a quote, a
    /// backslash, n, r or t is an escape; any other backslash is kept. Unlike
    /// Regex.Unescape this never throws.
    /// </summary>
    public static string UnescapeStringLiteral(string? body)
    {
        var text = body ?? string.Empty;
        var builder = new StringBuilder(text.Length);
        for (var index = 0; index < text.Length; index++)
        {
            var character = text[index];
            if (character != '\\' || index == text.Length - 1)
            {
                builder.Append(character);
                continue;
            }

            var escaped = text[++index];
            switch (escaped)
            {
                case '"':
                    builder.Append('"');
                    break;
                case '\\':
                    builder.Append('\\');
                    break;
                case 'n':
                    builder.Append('\n');
                    break;
                case 'r':
                    builder.Append('\r');
                    break;
                case 't':
                    builder.Append('\t');
                    break;
                default:
                    builder.Append('\\');
                    builder.Append(escaped);
                    break;
            }
        }

        return builder.ToString();
    }

    private readonly List<string> _errors = new();
    private string _text = string.Empty;
    private int _position;

    public FormulaParseResult Parse(string? formula)
    {
        _text = formula ?? string.Empty;
        _position = 0;
        _errors.Clear();

        SkipWhitespace();
        if (IsAtEnd)
        {
            _errors.Add("Formula is empty.");
            return new FormulaParseResult(null, _errors.ToArray());
        }

        var root = ParseExpression();
        SkipWhitespace();
        if (!IsAtEnd)
        {
            _errors.Add($"Unexpected token '{Current}' at position {_position}.");
        }

        return new FormulaParseResult(_errors.Count == 0 ? root : null, _errors.ToArray());
    }

    private FormulaNode? ParseExpression()
    {
        SkipWhitespace();
        if (IsAtEnd)
        {
            _errors.Add("Expected expression but reached end of formula.");
            return null;
        }

        if (Current == '"')
        {
            return ParseStringLiteral();
        }

        if (IsIdentifierStart(Current))
        {
            return ParseFunctionCall();
        }

        _errors.Add($"Unexpected token '{Current}' at position {_position}.");
        return null;
    }

    private FormulaNode? ParseFunctionCall()
    {
        var name = ParseIdentifier();
        SkipWhitespace();
        if (!Consume('('))
        {
            _errors.Add($"Expected '(' after function name '{name}'.");
            return null;
        }

        var arguments = new List<FormulaNode>();
        SkipWhitespace();
        if (Consume(')'))
        {
            return new FormulaFunctionCallNode(name, arguments);
        }

        while (!IsAtEnd)
        {
            var argument = ParseExpression();
            if (argument is not null)
            {
                arguments.Add(argument);
            }

            SkipWhitespace();
            if (Consume(')'))
            {
                return new FormulaFunctionCallNode(name, arguments);
            }

            if (!Consume(','))
            {
                _errors.Add($"Expected ',' or ')' after argument in function '{name}'.");
                return null;
            }

            SkipWhitespace();
        }

        _errors.Add($"Expected ')' to close function '{name}'.");
        return null;
    }

    private FormulaNode? ParseStringLiteral()
    {
        if (!Consume('"'))
        {
            _errors.Add("Expected string literal.");
            return null;
        }

        var builder = new StringBuilder();
        while (!IsAtEnd)
        {
            var character = Advance();
            if (character == '"')
            {
                return new FormulaStringLiteralNode(builder.ToString());
            }

            if (character != '\\')
            {
                builder.Append(character);
                continue;
            }

            if (IsAtEnd)
            {
                _errors.Add("Unterminated escape sequence in string literal.");
                return null;
            }

            var escaped = Advance();
            switch (escaped)
            {
                case '"':
                    builder.Append('"');
                    break;
                case '\\':
                    builder.Append('\\');
                    break;
                case 'n':
                    builder.Append('\n');
                    break;
                case 'r':
                    builder.Append('\r');
                    break;
                case 't':
                    builder.Append('\t');
                    break;
                default:
                    // Unknown escape: preserve the backslash so paths/label text like
                    // "C:\data" survive intact instead of silently dropping the '\'.
                    builder.Append('\\');
                    builder.Append(escaped);
                    break;
            }
        }

        _errors.Add("Unterminated string literal.");
        return null;
    }

    private string ParseIdentifier()
    {
        var start = _position;
        while (!IsAtEnd && IsIdentifierPart(Current))
        {
            _position++;
        }

        return _text[start.._position];
    }

    private void SkipWhitespace()
    {
        while (!IsAtEnd && char.IsWhiteSpace(Current))
        {
            _position++;
        }
    }

    private bool Consume(char expected)
    {
        if (IsAtEnd || Current != expected)
        {
            return false;
        }

        _position++;
        return true;
    }

    private char Advance()
    {
        return _text[_position++];
    }

    private char Current => _text[_position];
    private bool IsAtEnd => _position >= _text.Length;

    private static bool IsIdentifierStart(char character)
    {
        return char.IsLetter(character) || character == '_';
    }

    private static bool IsIdentifierPart(char character)
    {
        return char.IsLetterOrDigit(character) || character == '_';
    }
}
