using ANLAbel.Core.Enums;
using ANLAbel.Core.Mvvm;

namespace ANLAbel.Core.Models;

/// <summary>
/// Template serial counter. Objects print it through the binding token
/// <c>{#SERIAL}</c>. Values are reserved per print job in the local counter
/// store, so two jobs never receive the same number and a reprint reuses the
/// numbers of the job it repeats.
/// </summary>
public sealed class SerialCounterDefinition : ObservableObject
{
    private bool _enabled;
    private string _counterId = string.Empty;
    private long _startValue = 1;
    private long _step = 1;
    private int _changeEvery = 1;
    private long _minimumValue;
    private long _maximumValue = 999999;
    private bool _rollover;
    private int _digits;
    private string _prefix = string.Empty;
    private string _suffix = string.Empty;
    private string _sharedFolder = string.Empty;
    private SerialCounterAlphabet _alphabet = SerialCounterAlphabet.Numeric;
    private string _customAlphabet = string.Empty;

    public bool Enabled
    {
        get => _enabled;
        set
        {
            if (value && string.IsNullOrEmpty(_counterId))
            {
                CounterId = NewCounterId();
            }

            SetProperty(ref _enabled, value);
        }
    }

    /// <summary>
    /// Identity of the counter's number sequence in the counter store. A new
    /// identity (Reset) starts again from <see cref="StartValue"/>.
    /// </summary>
    public string CounterId
    {
        get => _counterId;
        set => SetProperty(ref _counterId, value ?? string.Empty);
    }

    /// <summary>First value of a new sequence; raising it later skips forward, it never goes back.</summary>
    public long StartValue
    {
        get => _startValue;
        set => SetProperty(ref _startValue, value);
    }

    /// <summary>Change per value; negative counts down. Must not be 0.</summary>
    public long Step
    {
        get => _step;
        set => SetProperty(ref _step, value);
    }

    /// <summary>Labels that share one value before it changes (1 - 999).</summary>
    public int ChangeEvery
    {
        get => _changeEvery;
        set => SetProperty(ref _changeEvery, value);
    }

    public long MinimumValue
    {
        get => _minimumValue;
        set => SetProperty(ref _minimumValue, value);
    }

    public long MaximumValue
    {
        get => _maximumValue;
        set => SetProperty(ref _maximumValue, value);
    }

    /// <summary>When true, a value past the limit wraps to the other limit; otherwise printing stops.</summary>
    public bool Rollover
    {
        get => _rollover;
        set => SetProperty(ref _rollover, value);
    }

    /// <summary>Minimum number of digits, padded with leading zeros (0 = no padding).</summary>
    public int Digits
    {
        get => _digits;
        set => SetProperty(ref _digits, value);
    }

    public string Prefix
    {
        get => _prefix;
        set => SetProperty(ref _prefix, value ?? string.Empty);
    }

    public string Suffix
    {
        get => _suffix;
        set => SetProperty(ref _suffix, value ?? string.Empty);
    }

    /// <summary>
    /// Optional folder (for example a network share) holding the counter's
    /// reservations so several PCs share one number sequence without
    /// duplicates. Empty keeps the sequence on this PC for this Windows user.
    /// </summary>
    public string SharedFolder
    {
        get => _sharedFolder;
        set => SetProperty(ref _sharedFolder, (value ?? string.Empty).Trim());
    }

    /// <summary>
    /// Character sequence values are written in. The default
    /// <see cref="SerialCounterAlphabet.Numeric"/> keeps the decimal printing
    /// of existing templates; reservations stay numeric either way.
    /// </summary>
    public SerialCounterAlphabet Alphabet
    {
        get => _alphabet;
        set => SetProperty(ref _alphabet, Enum.IsDefined(value) ? value : SerialCounterAlphabet.Numeric);
    }

    /// <summary>Characters of the sequence when <see cref="Alphabet"/> is Custom, lowest first.</summary>
    public string CustomAlphabet
    {
        get => _customAlphabet;
        set => SetProperty(ref _customAlphabet, value ?? string.Empty);
    }

    /// <summary>Starts a new number sequence at <see cref="StartValue"/>.</summary>
    public void Reset() => CounterId = NewCounterId();

    /// <summary>A copy to edit without touching this counter (Quản lý dữ liệu works on a draft).</summary>
    public SerialCounterDefinition Clone()
    {
        var copy = new SerialCounterDefinition();
        copy.CopyFrom(this);
        return copy;
    }

    /// <summary>
    /// Takes every setting of <paramref name="other"/>, including its sequence identity,
    /// so applying an unchanged draft leaves the number sequence where it is.
    /// </summary>
    public void CopyFrom(SerialCounterDefinition other)
    {
        ArgumentNullException.ThrowIfNull(other);
        CounterId = other.CounterId;
        StartValue = other.StartValue;
        Step = other.Step;
        ChangeEvery = other.ChangeEvery;
        MinimumValue = other.MinimumValue;
        MaximumValue = other.MaximumValue;
        Rollover = other.Rollover;
        Digits = other.Digits;
        Prefix = other.Prefix;
        Suffix = other.Suffix;
        SharedFolder = other.SharedFolder;
        Alphabet = other.Alphabet;
        CustomAlphabet = other.CustomAlphabet;
        // Last: enabling a counter without an identity would give it a new one.
        Enabled = other.Enabled;
    }

    private static string NewCounterId() => Guid.NewGuid().ToString("N");
}
