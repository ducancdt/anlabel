using System.Globalization;
using ANLAbel.Core.Enums;
using ANLAbel.Core.Models;

namespace ANLAbel.Core.Printing;

/// <summary>
/// Pure serial counter arithmetic. Label i (0-based, in print sequence order)
/// of a job whose first value is F prints value index i / ChangeEvery, i.e.
/// F + index * Step, wrapped into [Minimum, Maximum] when Rollover is on.
/// </summary>
public static class SerialCounterContract
{
    /// <summary>Row key that carries the label's serial; objects bind it as <c>{#SERIAL}</c>.</summary>
    public const string Token = "#SERIAL";
    public const int MaximumChangeEvery = 999;
    public const int MaximumDigits = 18;
    public const int MaximumAffixLength = 32;
    public const int MinimumCustomAlphabetLength = 2;
    public const int MaximumCustomAlphabetLength = 64;

    /// <summary>
    /// The characters values are written in, lowest first. Null means plain
    /// decimal (the historical behaviour); a custom sequence that cannot be
    /// used also yields its raw text, and <see cref="ValidateDefinition"/>
    /// reports why before any job relies on it.
    /// </summary>
    public static string? AlphabetCharacters(SerialCounterDefinition counter)
    {
        ArgumentNullException.ThrowIfNull(counter);
        return counter.Alphabet switch
        {
            SerialCounterAlphabet.Hexadecimal => "0123456789ABCDEF",
            SerialCounterAlphabet.Letters => "ABCDEFGHIJKLMNOPQRSTUVWXYZ",
            SerialCounterAlphabet.DigitsAndLetters => "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ",
            SerialCounterAlphabet.Custom => counter.CustomAlphabet,
            _ => null
        };
    }

    public static bool IsActive(LabelTemplate template)
        => template?.SerialCounter is { Enabled: true } counter && !string.IsNullOrEmpty(counter.CounterId);

    public static IReadOnlyList<string> ValidateDefinition(SerialCounterDefinition counter)
    {
        ArgumentNullException.ThrowIfNull(counter);
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(counter.CounterId))
        {
            errors.Add("Serial counter has no identity; turn it off and on again.");
        }

        if (counter.Step == 0)
        {
            errors.Add("Serial counter step must not be 0.");
        }

        if (counter.ChangeEvery is < 1 or > MaximumChangeEvery)
        {
            errors.Add($"Serial counter must change every 1 to {MaximumChangeEvery} labels; it is set to {counter.ChangeEvery}.");
        }

        if (counter.MinimumValue > counter.MaximumValue)
        {
            errors.Add($"Serial counter minimum {counter.MinimumValue} is greater than its maximum {counter.MaximumValue}.");
        }
        else if (counter.StartValue < counter.MinimumValue || counter.StartValue > counter.MaximumValue)
        {
            errors.Add($"Serial counter start value {counter.StartValue} is outside {counter.MinimumValue} to {counter.MaximumValue}.");
        }

        if (counter.Digits is < 0 or > MaximumDigits)
        {
            errors.Add($"Serial counter digits must be 0 to {MaximumDigits}; it is {counter.Digits}.");
        }

        if (counter.Prefix.Length > MaximumAffixLength || counter.Suffix.Length > MaximumAffixLength)
        {
            errors.Add($"Serial counter prefix and suffix are limited to {MaximumAffixLength} characters.");
        }

        if (counter.Alphabet != SerialCounterAlphabet.Numeric && counter.MinimumValue < 0)
        {
            errors.Add($"Serial counter minimum {counter.MinimumValue} must not be negative with a character sequence alphabet.");
        }

        if (counter.Alphabet == SerialCounterAlphabet.Custom)
        {
            var alphabet = counter.CustomAlphabet;
            if (alphabet.Length is < MinimumCustomAlphabetLength or > MaximumCustomAlphabetLength)
            {
                errors.Add($"Serial counter custom alphabet must have {MinimumCustomAlphabetLength} to {MaximumCustomAlphabetLength} characters; it has {alphabet.Length}.");
            }
            else if (alphabet.Distinct().Count() != alphabet.Length)
            {
                errors.Add("Serial counter custom alphabet repeats a character, so two values could print the same serial.");
            }
            else if (alphabet.Any(character => char.IsWhiteSpace(character) || char.IsControl(character)))
            {
                errors.Add("Serial counter custom alphabet must not contain spaces or control characters.");
            }
        }

        return errors;
    }

    /// <summary>Number of counter values a job of <paramref name="labelCount"/> labels uses.</summary>
    public static long ValueCount(SerialCounterDefinition counter, int labelCount)
        => labelCount <= 0 ? 0 : ((labelCount - 1L) / Math.Max(1, counter.ChangeEvery)) + 1;

    /// <summary>
    /// The value <paramref name="index"/> steps after <paramref name="first"/>,
    /// or null when it passes a limit and rollover is off.
    /// </summary>
    public static long? ValueAt(SerialCounterDefinition counter, long first, long index)
    {
        ArgumentNullException.ThrowIfNull(counter);
        var raw = (decimal)first + ((decimal)index * counter.Step);
        if (raw >= counter.MinimumValue && raw <= counter.MaximumValue)
        {
            return (long)raw;
        }

        if (!counter.Rollover)
        {
            return null;
        }

        var range = (decimal)counter.MaximumValue - counter.MinimumValue + 1;
        var offset = (raw - counter.MinimumValue) % range;
        if (offset < 0)
        {
            offset += range;
        }

        return (long)(counter.MinimumValue + offset);
    }

    /// <summary>
    /// The first value of the job after one that started at <paramref name="first"/>
    /// and used <paramref name="valueCount"/> values. Without rollover it can lie past
    /// a limit, which marks the counter as used up.
    /// </summary>
    public static long NextFirstValue(SerialCounterDefinition counter, long first, long valueCount)
    {
        var wrapped = ValueAt(counter, first, valueCount);
        if (wrapped is long value)
        {
            return value;
        }

        var raw = (decimal)first + ((decimal)valueCount * counter.Step);
        return raw > long.MaxValue ? long.MaxValue : raw < long.MinValue ? long.MinValue : (long)raw;
    }

    public static string Format(SerialCounterDefinition counter, long value)
    {
        ArgumentNullException.ThrowIfNull(counter);
        var alphabet = AlphabetCharacters(counter);
        var width = Math.Clamp(counter.Digits, 0, MaximumDigits);
        string body;
        if (alphabet is null)
        {
            body = Math.Abs(value).ToString(CultureInfo.InvariantCulture).PadLeft(width, '0');
        }
        else if (alphabet.Length >= MinimumCustomAlphabetLength)
        {
            body = RenderInAlphabet(alphabet, Math.Abs(value)).PadLeft(width, alphabet[0]);
        }
        else
        {
            // A sequence too short to count in is a definition error reported
            // by ValidateDefinition before printing; refusing here as well
            // keeps a broken definition from ever reaching a label as if it
            // were decimal.
            throw new InvalidOperationException(
                "Serial counter custom alphabet is too short to write values in; fix the counter definition before printing.");
        }

        return counter.Prefix + (value < 0 ? "-" : string.Empty) + body + counter.Suffix;
    }

    /// <summary>Value 0 is the first character; higher values count up the sequence with carry, like positional digits.</summary>
    private static string RenderInAlphabet(string alphabet, long value)
    {
        if (value == 0)
        {
            return alphabet[0].ToString();
        }

        var characters = new List<char>();
        var remaining = (decimal)value;
        while (remaining > 0)
        {
            characters.Add(alphabet[(int)(remaining % alphabet.Length)]);
            remaining = Math.Floor(remaining / alphabet.Length);
        }

        characters.Reverse();
        return new string(characters.ToArray());
    }

    /// <summary>
    /// Copies of <paramref name="rows"/> (one per label, in print sequence order)
    /// with the label's serial under <see cref="Token"/>. Fails when a value
    /// passes a limit without rollover, naming the first label affected.
    /// </summary>
    public static bool TryApply(
        SerialCounterDefinition counter,
        long first,
        IReadOnlyList<IReadOnlyDictionary<string, string>?> rows,
        out IReadOnlyDictionary<string, string>?[] withSerials,
        out string error)
    {
        ArgumentNullException.ThrowIfNull(counter);
        ArgumentNullException.ThrowIfNull(rows);
        withSerials = new IReadOnlyDictionary<string, string>?[rows.Count];
        error = string.Empty;
        var changeEvery = Math.Max(1, counter.ChangeEvery);
        for (var i = 0; i < rows.Count; i++)
        {
            var value = ValueAt(counter, first, i / changeEvery);
            if (value is null)
            {
                error = $"Serial counter runs past {(counter.Step > 0 ? $"its maximum {counter.MaximumValue}" : $"its minimum {counter.MinimumValue}")} at label {i + 1} of {rows.Count}. Print fewer labels, raise the limit, turn on rollover, or reset the counter.";
                withSerials = Array.Empty<IReadOnlyDictionary<string, string>?>();
                return false;
            }

            var copy = rows[i] is { } row
                ? new Dictionary<string, string>(row.Count + 1, StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, string>(1, StringComparer.OrdinalIgnoreCase);
            if (rows[i] is { } source)
            {
                foreach (var pair in source)
                {
                    copy[pair.Key] = pair.Value;
                }
            }

            copy[Token] = Format(counter, value.Value);
            withSerials[i] = copy;
        }

        return true;
    }

    /// <summary>The job that owns a reprint's numbers: the original job, not the reprint.</summary>
    public static string RootJobId(string jobId)
    {
        var marker = (jobId ?? string.Empty).IndexOf(":reprint:", StringComparison.Ordinal);
        return marker < 0 ? jobId ?? string.Empty : jobId![..marker];
    }
}
