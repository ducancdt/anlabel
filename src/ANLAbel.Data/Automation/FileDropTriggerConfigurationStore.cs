using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ANLAbel.Core.Automation;

namespace ANLAbel.Data.Automation;

public sealed record FileDropTriggerConfigurationSnapshot(
    FileDropTriggerConfiguration Configuration,
    string ConfigurationFingerprint,
    string IntegrityHash);

/// <summary>One explicit local trigger setting, persisted separately from claim outcomes.</summary>
public sealed class FileDropTriggerConfigurationStore
{
    private readonly string _path;
    public FileDropTriggerConfigurationStore(string path) => _path = path;

    public FileDropTriggerConfigurationSnapshot? Read(out string? diagnostic)
    {
        diagnostic = null;
        if (!File.Exists(_path)) return null;
        try
        {
            var snapshot = JsonSerializer.Deserialize<FileDropTriggerConfigurationSnapshot>(File.ReadAllText(_path))
                ?? throw new InvalidDataException("Configuration file is empty.");
            if (!FileDropTriggerConfigurationContract.TryValidate(snapshot.Configuration, out var validationError))
                throw new InvalidDataException(validationError);
            if (!string.Equals(snapshot.ConfigurationFingerprint, snapshot.Configuration.ConfigurationFingerprint, StringComparison.Ordinal) ||
                !string.Equals(snapshot.IntegrityHash, ComputeIntegrityHash(snapshot), StringComparison.Ordinal))
            {
                if (!IsLegacySnapshot(snapshot)) throw new InvalidDataException("Automation configuration integrity mismatch.");
                var migrated = snapshot with { ConfigurationFingerprint = snapshot.Configuration.ConfigurationFingerprint, IntegrityHash = string.Empty };
                return migrated with { IntegrityHash = ComputeIntegrityHash(migrated) };
            }
            return snapshot;
        }
        catch (Exception ex)
        {
            diagnostic = ex.Message;
            return null;
        }
    }

    public FileDropTriggerConfigurationSnapshot Save(FileDropTriggerConfiguration configuration)
    {
        if (!FileDropTriggerConfigurationContract.TryValidate(configuration, out var error))
            throw new ArgumentException(error, nameof(configuration));
        var snapshot = new FileDropTriggerConfigurationSnapshot(configuration, configuration.ConfigurationFingerprint, string.Empty);
        snapshot = snapshot with { IntegrityHash = ComputeIntegrityHash(snapshot) };
        var directory = Path.GetDirectoryName(_path);
        if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
        var temporary = _path + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(snapshot), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        File.Move(temporary, _path, overwrite: true);
        return snapshot;
    }

    private static string ComputeIntegrityHash(FileDropTriggerConfigurationSnapshot snapshot) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{snapshot.Configuration.TriggerId}|{snapshot.Configuration.Name}|{snapshot.Configuration.WatchRoot}|{snapshot.Configuration.Pattern}|{snapshot.Configuration.Recursive}|{snapshot.Configuration.Enabled}|{snapshot.Configuration.TargetTemplatePath}|{snapshot.Configuration.QueueName}|{snapshot.Configuration.PrintPolicyMode}|{snapshot.ConfigurationFingerprint}")));

    private static bool IsLegacySnapshot(FileDropTriggerConfigurationSnapshot snapshot)
    {
        var configuration = snapshot.Configuration;
        if (!string.IsNullOrEmpty(configuration.TargetTemplatePath) || !string.IsNullOrEmpty(configuration.QueueName) || configuration.PrintPolicyMode != default) return false;
        var legacyFingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join("|", configuration.TriggerId.Trim(), configuration.Name.Trim(), Path.GetFullPath(configuration.WatchRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).ToUpperInvariant(), configuration.Pattern.Trim().ToUpperInvariant(), configuration.Recursive ? "recursive" : "flat", configuration.Enabled ? "enabled" : "disabled"))));
        var legacyIntegrity = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{configuration.TriggerId}|{configuration.Name}|{configuration.WatchRoot}|{configuration.Pattern}|{configuration.Recursive}|{configuration.Enabled}|{legacyFingerprint}")));
        return string.Equals(snapshot.ConfigurationFingerprint, legacyFingerprint, StringComparison.Ordinal) && string.Equals(snapshot.IntegrityHash, legacyIntegrity, StringComparison.Ordinal);
    }
}
