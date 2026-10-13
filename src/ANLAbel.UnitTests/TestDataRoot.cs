using System.Runtime.CompilerServices;
using ANLAbel.Data;

namespace ANLAbel.UnitTests;

/// <summary>
/// Redirects ANLAbel app data for the whole test assembly before any test runs,
/// so unit tests (and mutation runs) never write into the operator's real logs,
/// preferences or data-source registry. Plan 13 item 0.4.
/// </summary>
internal static class TestDataRoot
{
    internal static string Path { get; private set; } = string.Empty;

#pragma warning disable CA2255 // Test assembly setup must run before the first test touches app data.
    [ModuleInitializer]
#pragma warning restore CA2255
    internal static void Redirect()
    {
        Path = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            "ANLAbel.UnitTests.Data",
            Environment.ProcessId.ToString());
        Environment.SetEnvironmentVariable(AppDataPaths.OverrideVariable, Path);
    }
}
