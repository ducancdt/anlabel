using ANLAbel.Data;
using ANLAbel.Data.DataLogs;
using ANLAbel.Data.PrintLogs;
using Xunit;

namespace ANLAbel.UnitTests;

public sealed class AppDataPathsTests
{
    private static readonly string OperatorLocal = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ANLAbel");

    private static readonly string OperatorRoaming = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ANLAbel");

    [Fact]
    public void Test_assembly_redirects_app_data_away_from_operator_profile()
    {
        Assert.True(AppDataPaths.IsOverridden);
        Assert.StartsWith(TestDataRoot.Path, AppDataPaths.LocalRoot, StringComparison.OrdinalIgnoreCase);
        Assert.StartsWith(TestDataRoot.Path, AppDataPaths.RoamingRoot, StringComparison.OrdinalIgnoreCase);
        Assert.False(AppDataPaths.LocalRoot.StartsWith(OperatorLocal, StringComparison.OrdinalIgnoreCase));
        Assert.False(AppDataPaths.RoamingRoot.StartsWith(OperatorRoaming, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Local_and_roaming_roots_stay_separate_under_override()
    {
        Assert.NotEqual(AppDataPaths.LocalRoot, AppDataPaths.RoamingRoot, StringComparer.OrdinalIgnoreCase);
        Assert.Equal(Path.Combine(AppDataPaths.LocalRoot, "logs"), AppDataPaths.LogsDirectory);
    }

    [Fact]
    public void Default_services_resolve_their_files_under_the_redirected_roots()
    {
        Assert.Equal(AppDataPaths.Log("data-operations.jsonl"), new DataOperationLogService().LogFilePath);
        Assert.Equal(AppDataPaths.Log("print-operations.jsonl"), new PrintOperationLogService().LogFilePath);
        Assert.Equal(AppDataPaths.Log("print-job-events.jsonl"), new PrintJobStateStore().LogFilePath);
        Assert.StartsWith(TestDataRoot.Path, new PrintLogService().LogFilePath, StringComparison.OrdinalIgnoreCase);
    }
}
