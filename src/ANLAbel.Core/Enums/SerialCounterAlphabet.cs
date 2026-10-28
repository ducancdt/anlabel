namespace ANLAbel.Core.Enums;

/// <summary>
/// Character sequence a serial counter value is written in. The reserved
/// sequence stays numeric; the alphabet only decides how a value is shown,
/// so jobs can never receive the same printed serial twice. The predefined
/// sequences mirror the counter data formats of the reference designer
/// (Numeric, Hex, Letters, Digits&amp;Capitals and a custom Sequence).
/// </summary>
public enum SerialCounterAlphabet
{
    /// <summary>Decimal digits, as counters saved before this choice printed.</summary>
    Numeric,

    /// <summary>Hexadecimal with capital letters: 0-9 then A-F.</summary>
    Hexadecimal,

    /// <summary>Capital letters A-Z.</summary>
    Letters,

    /// <summary>Digits then capital letters: 0-9 then A-Z.</summary>
    DigitsAndLetters,

    /// <summary>Operator-defined character sequence.</summary>
    Custom
}
