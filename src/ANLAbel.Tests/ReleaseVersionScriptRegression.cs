using System.Diagnostics;

internal static class ReleaseVersionScriptRegression
{
    public static Task Run()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        try
        {
            Write(root, "eng/Version.props", "<ANLAbelReleaseVersion>0.221</ANLAbelReleaseVersion>");
            Write(root, "src/ANLAbel.App/MainWindow.xaml", "v0.221");
            Write(root, "src/ANLAbel.App/HelpWindow.xaml.cs", "v0.221");
            Write(root, "installer/ANLAbel-x64.iss", "AppVersion=0.221\nv0.221\nVersionInfoVersion=0.221.0.0");
            Write(root, "docs/VERSIONING.md", "current public version is `0.221`");
            Write(root, "docs/AUTOMATED_QUALITY_LOOP.md", "public version `0.221`");
            Write(root, "docs/audit-2026-07-02.md", "public version `0.221` is canonical");
            Write(root, "docs/reinvention/11-verification-checkpoint-2026-08-13.md", "Display/source version | `0.221` is canonical");
            Write(root, "MASTER_PLAN.md", "product display **v0.221**");
            var script = Path.Combine(FindRepositoryRoot(), "scripts", "Set-ANLAbelReleaseVersion.ps1");
            var psi = new ProcessStartInfo("powershell.exe", $"-NoProfile -ExecutionPolicy Bypass -File \"{script}\" -Version 0.222 -Root \"{root}\"") { UseShellExecute = false, RedirectStandardError = true };
            using var process = Process.Start(psi) ?? throw new InvalidOperationException("Could not start PowerShell version script regression.");
            process.WaitForExit();
            Require(process.ExitCode == 0, process.StandardError.ReadToEnd());
            foreach (var path in Directory.GetFiles(root, "*", SearchOption.AllDirectories)) Require(File.ReadAllText(path).Contains("0.222", StringComparison.Ordinal), $"Script must project 0.222 into {path}.");
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
        return Task.CompletedTask;
    }

    private static void Write(string root, string relative, string content)
    {
        var path = Path.Combine(root, relative); Directory.CreateDirectory(Path.GetDirectoryName(path)!); File.WriteAllText(path, content);
    }
    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(Environment.CurrentDirectory); directory is not null; directory = directory.Parent)
            if (File.Exists(Path.Combine(directory.FullName, "ANLAbel.slnx"))) return directory.FullName;
        throw new InvalidOperationException("Could not locate repository root.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
