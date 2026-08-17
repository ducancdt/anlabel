using System.Collections.Concurrent;
using System.Security.Cryptography;
using ANLAbel.Core.Automation;

namespace ANLAbel.Data.Automation;

/// <summary>
/// Detect-only local watcher. It fingerprints readable files into the claim ledger
/// and deliberately has no parser, source mutation, queue, manifest, or print API.
/// </summary>
public sealed class FileDropDetectionService : IDisposable
{
    private readonly FileDropTriggerConfiguration _configuration;
    private readonly FileDropClaimLedger _ledger;
    private readonly Action<string>? _report;
    private readonly ConcurrentDictionary<string, Timer> _debounces = new(StringComparer.OrdinalIgnoreCase);
    private FileSystemWatcher? _watcher;

    public FileDropDetectionService(FileDropTriggerConfiguration configuration, FileDropClaimLedger ledger, Action<string>? report = null)
    {
        if (!FileDropTriggerConfigurationContract.TryValidate(configuration, out var error)) throw new ArgumentException(error, nameof(configuration));
        _configuration = configuration;
        _ledger = ledger;
        _report = report;
    }

    public bool IsRunning => _watcher is not null;

    public bool TryStart(out string error)
    {
        if (!_configuration.Enabled) { error = "The local trigger is disabled; no watcher was started."; return false; }
        if (!Directory.Exists(_configuration.WatchRoot)) { error = "Configured watch root does not exist; no watcher was started."; return false; }
        if (_watcher is not null) { error = string.Empty; return true; }
        try
        {
            var watcher = new FileSystemWatcher(_configuration.WatchRoot, _configuration.Pattern)
            {
                IncludeSubdirectories = _configuration.Recursive,
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size,
                EnableRaisingEvents = true
            };
            watcher.Created += OnFileChanged;
            watcher.Changed += OnFileChanged;
            watcher.Renamed += OnFileChanged;
            watcher.Error += (_, eventArgs) => _report?.Invoke($"Local file-drop watcher error: {eventArgs.GetException().Message}");
            _watcher = watcher;
            error = string.Empty;
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            error = ex.Message;
            return false;
        }
    }

    public void Stop()
    {
        var watcher = Interlocked.Exchange(ref _watcher, null);
        if (watcher is not null)
        {
            watcher.EnableRaisingEvents = false;
            watcher.Created -= OnFileChanged;
            watcher.Changed -= OnFileChanged;
            watcher.Renamed -= OnFileChanged;
            watcher.Dispose();
        }
        foreach (var pair in _debounces) if (_debounces.TryRemove(pair.Key, out var timer)) timer.Dispose();
    }

    /// <summary>Testable one-file detection seam; a success means only a durable Detected event.</summary>
    public bool TryDetect(string path, out string result)
    {
        if (!File.Exists(path)) { result = "Source is not readable yet; no claim was recorded."; return false; }
        try
        {
            string fingerprint;
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                fingerprint = Convert.ToHexString(SHA256.HashData(stream));
            var identity = FileDropClaimContract.CreateIdentity(_configuration.TriggerId, _configuration.ConfigurationFingerprint, fingerprint);
            if (_ledger.TryRecordDetection(identity, out _, out var error)) { result = "Detected and recorded; no source was claimed or dispatched."; return true; }
            result = error;
            return false;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            result = $"Source is locked or unreadable; no claim was recorded: {ex.Message}";
            return false;
        }
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        var timer = new Timer(_ =>
        {
            if (_debounces.TryRemove(e.FullPath, out var current)) current.Dispose();
            if (!TryDetect(e.FullPath, out var result)) _report?.Invoke(result);
            else _report?.Invoke(result);
        }, null, TimeSpan.FromMilliseconds(750), Timeout.InfiniteTimeSpan);
        if (_debounces.TryGetValue(e.FullPath, out var prior)) prior.Dispose();
        _debounces[e.FullPath] = timer;
    }

    public void Dispose() => Stop();
}
