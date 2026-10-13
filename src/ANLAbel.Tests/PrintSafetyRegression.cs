/// <summary>
/// Plan 13 GĐ5 source contracts for print-side safety that cannot be exercised
/// without a real spooler. Each method is one named gate in Program.cs.
/// </summary>
internal static class PrintSafetyRegression
{
    public static Task CalibrationPrintUsesAnIsolatedCopy()
    {
        var source = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "src", "ANLAbel.App", "PrinterSetupWindow.xaml.cs"));
        var start = source.IndexOf("private async void PrintCalibration_Click", StringComparison.Ordinal);
        Require(start >= 0, "PrintCalibration_Click must exist.");
        var end = source.IndexOf("\n    private ", start + 1, StringComparison.Ordinal);
        var method = source[start..(end < 0 ? source.Length : end)];
        Require(method.Contains("PrintSessionTemplateContract.Capture(_workingTemplate)", StringComparison.Ordinal),
            "The calibration print must run on an isolated copy of the open label.");
        Require(!method.Contains("_workingTemplate.PrinterProfile.", StringComparison.Ordinal),
            "The calibration print must not write printer settings into the open label before OK (Cancel would keep them).");
        return Task.CompletedTask;
    }

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "ANLAbel.slnx")))
            {
                return directory.FullName;
            }
        }

        throw new InvalidOperationException("Could not locate the repository root.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
