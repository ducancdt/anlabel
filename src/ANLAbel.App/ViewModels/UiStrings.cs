using ANLAbel.App.Localization;
using ANLAbel.Core.Mvvm;
using ANLAbel.Data.Preferences;

namespace ANLAbel.App.ViewModels;

public sealed class UiStrings : ObservableObject
{
    public static UiStrings Current { get; } = new();

    private readonly UiPreferencesService _preferences;
    private UiLanguage _language;

    public UiStrings()
        : this(new UiPreferencesService())
    {
    }

    public UiStrings(UiPreferencesService preferences)
    {
        _preferences = preferences;
        _language = preferences.Load();
    }

    public UiLanguage Language
    {
        get => _language;
        private set
        {
            if (SetProperty(ref _language, value))
            {
                OnPropertyChanged(string.Empty);
            }
        }
    }

    public bool IsEnglish => Language == UiLanguage.English;
    public bool IsVietnamese => Language == UiLanguage.Vietnamese;

    public string Subtitle => Text("Subtitle");
    public string Settings => Text("Settings");
    public string SettingsTitle => Text("SettingsTitle");
    public string LanguageLabel => Text("Language");
    public string LanguageEnglish => Text("LanguageEnglish");
    public string LanguageVietnamese => Text("LanguageVietnamese");
    public string LanguageHint => Text("LanguageHint");
    public string Close => Text("Close");
    public string Templates => Text("Templates");
    public string Library => Text("Library");
    public string Import => Text("Import");
    public string Excel => Text("Excel");
    public string Update => Text("Update");
    public string Data => Text("Data");
    public string Printer => Text("Printer");
    public string Setup => Text("Setup");
    public string Preview => Text("Preview");
    public string Print => Text("Print");
    public string CurrentRow => Text("Current");
    public string PrintAllRows => Text("PrintAllRows");
    public string PrintHistory => Text("PrintHistory");
    public string ExportExcel => Text("ExportExcel");
    public string TestPrint => Text("TestPrint");
    public string PrintOutput => Text("PrintOutput");
    public string Panels => Text("Panels");
    public string Restore => Text("Restore");
    public string Objects => Text("Objects");
    public string Snap => Text("Snap");
    public string Grid => Text("Grid");
    public string Workspace => Text("Workspace");
    public string Delete => Text("Delete");
    public string Selection => Text("Selection");
    public string Help => Text("Help");
    public string InsertObjects => Text("InsertObjects");
    public string PrinterPrefix => Text("PrinterPrefix");
    public string PrinterNotSelected => Text("PrinterNotSelected");
    public string NewTemplateTitle => Text("NewTemplateTitle");
    public string NewTemplateHeading => Text("NewTemplateHeading");
    public string TemplateName => Text("TemplateName");
    public string WidthMm => Text("WidthMm");
    public string HeightMm => Text("HeightMm");
    public string ResolutionDpi => Text("ResolutionDpi");
    public string Cancel => Text("Cancel");
    public string Create => Text("Create");
    public string HelpTitle => Text("HelpTitle");
    public string UserGuide => Text("UserGuide");

    public void SetLanguage(UiLanguage language)
    {
        Language = language;
        _preferences.Save(language);
    }

    private string Text(string key) => UiCatalog.Get(key, Language);
}
