/// <summary>
/// Plan 13 GĐ5 source contracts for print-side safety that cannot be exercised
/// without a real spooler. Each method is one named gate in Program.cs.
/// </summary>
internal static class PrintSafetyRegression
{
    public static Task CalibrationPrintUsesAnIsolatedCopy()
    {
        var method = Method(Path.Combine("src", "ANLAbel.App", "PrinterSetupWindow.xaml.cs"), "private async void PrintCalibration_Click(");
        Require(method.Contains("PrintSessionTemplateContract.Capture(_workingTemplate)", StringComparison.Ordinal),
            "The calibration print must run on an isolated copy of the open label.");
        Require(!method.Contains("_workingTemplate.PrinterProfile.", StringComparison.Ordinal),
            "The calibration print must not write printer settings into the open label before OK (Cancel would keep them).");
        return Task.CompletedTask;
    }

    public static Task QuickPrintUsesOneSnapshotAndAsyncContract()
    {
        var method = Method(Path.Combine("src", "ANLAbel.App", "ViewModels", "MainViewModel.cs"), "private async Task<TrackedPrintResult> DispatchTrackedPrintAsync(");
        Require(method.Contains("PrintSessionTemplateContract.Capture(Template)", StringComparison.Ordinal),
            "Quick print must snapshot the label once and use it for contract, preflight, manifest and dispatch.");
        Require(method.Contains("CreateEffectivePlanAsync(printTemplate", StringComparison.Ordinal)
                && method.Contains("ValidateRowsAsync(printTemplate", StringComparison.Ordinal),
            "Quick print must resolve the driver contract and run preflight off the UI thread.");
        Require(!method.Contains("CreateEffectivePlan(Template", StringComparison.Ordinal)
                && !method.Contains("ValidateRows(Template", StringComparison.Ordinal),
            "Quick print must not call the blocking contract/preflight overloads on the live label.");
        var dispatchCall = method.IndexOf("PrintRowsWithResultAsync(", StringComparison.Ordinal);
        Require(dispatchCall > 0 && method.IndexOf("printTemplate", dispatchCall, StringComparison.Ordinal) - dispatchCall < 80,
            "Quick print must dispatch the same snapshot that was validated.");
        return Task.CompletedTask;
    }

    public static Task FailureAfterSpoolAcceptanceIsUnknownNotFailed()
    {
        var quick = Method(Path.Combine("src", "ANLAbel.App", "ViewModels", "MainViewModel.cs"), "private async Task<TrackedPrintResult> DispatchTrackedPrintAsync(");
        Require(quick.Contains("dispatched?.IsAccepted == true", StringComparison.Ordinal)
                && quick.Contains("? PrintJobLifecycleState.Unknown", StringComparison.Ordinal),
            "Quick print: a failure after the spooler accepted the job must be recorded as Unknown (P07).");
        var preview = Method(Path.Combine("src", "ANLAbel.App", "PrintPreviewWindow.xaml.cs"), "private async void Print_Click(");
        Require(preview.Contains("dispatchedPrintResult?.IsAccepted == true", StringComparison.Ordinal)
                && preview.Contains("? PrintJobLifecycleState.Unknown", StringComparison.Ordinal),
            "Print Preview: a failure after the spooler accepted the job must be recorded as Unknown (P07).");
        Require(preview.Contains("Chưa rõ kết quả in", StringComparison.Ordinal),
            "Print Preview must tell the operator the outcome is uncertain instead of failed.");
        return Task.CompletedTask;
    }

    private static string Method(string relativePath, string signature)
    {
        var source = File.ReadAllText(Path.Combine(FindRepositoryRoot(), relativePath)).Replace("\r\n", "\n");
        var start = source.IndexOf(signature, StringComparison.Ordinal);
        Require(start >= 0, $"{signature} must exist in {relativePath}.");
        var end = source.IndexOf("\n    private ", start + 1, StringComparison.Ordinal);
        var nextPublic = source.IndexOf("\n    public ", start + 1, StringComparison.Ordinal);
        if (nextPublic > 0 && (end < 0 || nextPublic < end))
        {
            end = nextPublic;
        }

        return source[start..(end < 0 ? source.Length : end)];
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
