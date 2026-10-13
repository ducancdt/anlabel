namespace ANLAbel.Data.Preferences;

public enum UiLanguage
{
    English,
    Vietnamese
}

public sealed class UiPreferences
{
    public string Language { get; set; } = string.Empty;
}
