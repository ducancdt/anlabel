using ANLAbel.Core.Enums;
using ANLAbel.Core.Mvvm;

namespace ANLAbel.Core.Models;

/// <summary>
/// A value the operator types when printing, used by objects as
/// <c>{?Name}</c>. It is recorded with the print job so a reprint repeats it.
/// </summary>
public sealed class PromptVariableDefinition : ObservableObject
{
    private string _name = string.Empty;
    private string _prompt = string.Empty;
    private string _defaultValue = string.Empty;
    private int _maxLength;
    private bool _digitsOnly;
    private bool _required = true;
    private bool _fixedLength;
    private string _allowedCharacters = string.Empty;
    private string _pickList = string.Empty;
    private string _prefix = string.Empty;
    private string _suffix = string.Empty;
    private PromptPadding _padding;
    private string _padCharacter = "0";
    private string _dateFormat = string.Empty;
    private string _dateInputFormat = string.Empty;

    /// <summary>Name used in bindings without the leading '?', e.g. Lot for {?Lot}.</summary>
    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, (value ?? string.Empty).Trim().TrimStart('?'));
    }

    /// <summary>Question shown to the operator; the name is used when empty.</summary>
    public string Prompt
    {
        get => _prompt;
        set => SetProperty(ref _prompt, value ?? string.Empty);
    }

    public string DefaultValue
    {
        get => _defaultValue;
        set => SetProperty(ref _defaultValue, value ?? string.Empty);
    }

    /// <summary>Maximum number of characters; 0 means no limit.</summary>
    public int MaxLength
    {
        get => _maxLength;
        set => SetProperty(ref _maxLength, Math.Max(0, value));
    }

    public bool DigitsOnly
    {
        get => _digitsOnly;
        set => SetProperty(ref _digitsOnly, value);
    }

    public bool Required
    {
        get => _required;
        set => SetProperty(ref _required, value);
    }

    /// <summary>NiceLabel 10 "Fixed length": the value has exactly <see cref="MaxLength"/> characters.</summary>
    public bool FixedLength
    {
        get => _fixedLength;
        set => SetProperty(ref _fixedLength, value);
    }

    /// <summary>NiceLabel 10 "Allowed characters": when set, the value uses only these characters.</summary>
    public string AllowedCharacters
    {
        get => _allowedCharacters;
        set => SetProperty(ref _allowedCharacters, value ?? string.Empty);
    }

    /// <summary>NiceLabel 10 "Pick list": one value per line; the operator picks one of them.</summary>
    public string PickList
    {
        get => _pickList;
        set => SetProperty(ref _pickList, value ?? string.Empty);
    }

    /// <summary>The pick list's values, blank lines left out.</summary>
    public IReadOnlyList<string> PickListValues => PickList
        .Split('\n')
        .Select(line => line.TrimEnd('\r'))
        .Where(line => line.Length > 0)
        .ToArray();

    /// <summary>NiceLabel 10 output rule: printed before a non-empty value.</summary>
    public string Prefix
    {
        get => _prefix;
        set => SetProperty(ref _prefix, value ?? string.Empty);
    }

    /// <summary>NiceLabel 10 output rule: printed after a non-empty value.</summary>
    public string Suffix
    {
        get => _suffix;
        set => SetProperty(ref _suffix, value ?? string.Empty);
    }

    /// <summary>NiceLabel 10 output rule: a shorter value is padded up to <see cref="MaxLength"/>.</summary>
    public PromptPadding Padding
    {
        get => _padding;
        set => SetProperty(ref _padding, Enum.IsDefined(value) ? value : PromptPadding.None);
    }

    /// <summary>The padding character (one character; 0 by default).</summary>
    public string PadCharacter
    {
        get => _padCharacter;
        set => SetProperty(ref _padCharacter, string.IsNullOrEmpty(value) ? "0" : value[..1]);
    }

    /// <summary>
    /// NiceLabel 10 date variable: the format the date prints in (dd/MM/yyyy,
    /// yyMMdd…). Set, the operator picks a date and the job keeps it as
    /// yyyy-MM-dd; empty for a text value.
    /// </summary>
    public string DateFormat
    {
        get => _dateFormat;
        set => SetProperty(ref _dateFormat, (value ?? string.Empty).Trim());
    }

    /// <summary>For a date: a format the operator may also type it in (NiceLabel input format).</summary>
    public string DateInputFormat
    {
        get => _dateInputFormat;
        set => SetProperty(ref _dateInputFormat, (value ?? string.Empty).Trim());
    }

    /// <summary>A copy to edit without touching this variable (Quản lý dữ liệu works on a draft).</summary>
    public PromptVariableDefinition Clone() => new()
    {
        Name = Name,
        Prompt = Prompt,
        DefaultValue = DefaultValue,
        MaxLength = MaxLength,
        DigitsOnly = DigitsOnly,
        Required = Required,
        FixedLength = FixedLength,
        AllowedCharacters = AllowedCharacters,
        PickList = PickList,
        Prefix = Prefix,
        Suffix = Suffix,
        Padding = Padding,
        PadCharacter = PadCharacter,
        DateFormat = DateFormat,
        DateInputFormat = DateInputFormat
    };
}
