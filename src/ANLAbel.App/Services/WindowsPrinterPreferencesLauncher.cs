using System.Diagnostics;

namespace ANLAbel.App.Services;

public interface IPrinterPreferencesLauncher
{
    Task OpenAsync(string queueName, CancellationToken cancellationToken = default);
}

/// <summary>
/// Opens the selected queue's Windows printing preferences. ArgumentList keeps
/// queue names as data (including spaces) instead of composing a shell command.
/// This action never submits a print job.
/// </summary>
internal sealed class WindowsPrinterPreferencesLauncher : IPrinterPreferencesLauncher
{
    public async Task OpenAsync(string queueName, CancellationToken cancellationToken = default)
    {
        using var process = Process.Start(CreateStartInfo(queueName))
            ?? throw new InvalidOperationException("Windows không mở được cửa sổ Tùy chọn của trình điều khiển máy in.");
        // rundll32/PrintUIEntry returns a non-zero exit code for benign outcomes
        // such as the operator cancelling the driver property sheet, declining UAC,
        // or driver quirks. Only a failure to START is a genuine error; once the
        // process launched, the exit code is not a reliable success signal, so we
        // wait for the dialog to close and treat any exit code as non-fatal.
        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
    }

    internal static ProcessStartInfo CreateStartInfo(string queueName)
    {
        if (string.IsNullOrWhiteSpace(queueName))
        {
            throw new ArgumentException("A printer queue name is required.", nameof(queueName));
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = "rundll32.exe",
            UseShellExecute = false,
            CreateNoWindow = true
        };
        startInfo.ArgumentList.Add("printui.dll,PrintUIEntry");
        startInfo.ArgumentList.Add("/e");
        startInfo.ArgumentList.Add("/n");
        startInfo.ArgumentList.Add(queueName.Trim());
        return startInfo;
    }
}
