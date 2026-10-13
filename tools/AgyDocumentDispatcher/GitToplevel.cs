using System.Diagnostics;

namespace AgyDocumentDispatcher;

public static class GitToplevel
{
    public static string Resolve(string startingDirectory)
    {
        var start = Path.GetFullPath(startingDirectory);
        var psi = new ProcessStartInfo("git", "rev-parse --show-toplevel")
        {
            WorkingDirectory = start,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using var process = Process.Start(psi)
                            ?? throw new InvalidOperationException("Failed to start git rev-parse --show-toplevel.");
        var stdout = process.StandardOutput.ReadToEnd().Trim();
        var stderr = process.StandardError.ReadToEnd().Trim();
        process.WaitForExit();
        if (process.ExitCode != 0 || string.IsNullOrWhiteSpace(stdout))
        {
            throw new InvalidOperationException(
                "git rev-parse --show-toplevel failed from " + start + ": " + stderr);
        }

        return Path.GetFullPath(stdout.Replace('/', Path.DirectorySeparatorChar));
    }
}
