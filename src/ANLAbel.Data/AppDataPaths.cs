namespace ANLAbel.Data;

/// <summary>
/// The one place that decides where ANLAbel keeps local state.
/// <para>
/// Production paths are unchanged from earlier releases:
/// <c>%LOCALAPPDATA%\ANLAbel</c> holds machine state, logs and preferences, and
/// <c>%APPDATA%\ANLAbel</c> holds the roaming data-source registry and print history.
/// </para>
/// <para>
/// When <see cref="OverrideVariable"/> is set, both roots move under that folder.
/// Test runners set it so that a test run never writes into an operator's logs or
/// preferences. Paths are resolved on every call, so the override only has to be
/// set before the first service is created.
/// </para>
/// </summary>
public static class AppDataPaths
{
    public const string OverrideVariable = "ANLABEL_DATA_ROOT";
    private const string ProductFolder = "ANLAbel";

    /// <summary>Machine-local root, <c>%LOCALAPPDATA%\ANLAbel</c> by default.</summary>
    public static string LocalRoot => ResolveRoot(Environment.SpecialFolder.LocalApplicationData, "Local");

    /// <summary>Roaming root, <c>%APPDATA%\ANLAbel</c> by default.</summary>
    public static string RoamingRoot => ResolveRoot(Environment.SpecialFolder.ApplicationData, "Roaming");

    /// <summary>Folder for the operation and job logs under <see cref="LocalRoot"/>.</summary>
    public static string LogsDirectory => Path.Combine(LocalRoot, "logs");

    public static string Local(string fileName) => Path.Combine(LocalRoot, fileName);

    public static string Roaming(string fileName) => Path.Combine(RoamingRoot, fileName);

    public static string Log(string fileName) => Path.Combine(LogsDirectory, fileName);

    /// <summary>True when the roots are redirected (tests or a portable setup).</summary>
    public static bool IsOverridden => !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(OverrideVariable));

    private static string ResolveRoot(Environment.SpecialFolder folder, string overrideSubfolder)
    {
        var overrideRoot = Environment.GetEnvironmentVariable(OverrideVariable);
        if (!string.IsNullOrWhiteSpace(overrideRoot))
        {
            return Path.Combine(Path.GetFullPath(overrideRoot), overrideSubfolder, ProductFolder);
        }

        return Path.Combine(Environment.GetFolderPath(folder), ProductFolder);
    }
}
