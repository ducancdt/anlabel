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
}
