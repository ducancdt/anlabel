using ANLAbel.Core.Automation;
using ANLAbel.Data.Automation;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

internal static class FileDropTriggerConfigurationRegression
{
    public static Task Run()
    {
        var config = new FileDropTriggerConfiguration("receiving-csv", "Receiving CSV", Path.GetTempPath(), "*.csv", false, false);
        Require(FileDropTriggerConfigurationContract.TryValidate(config, out _), "A local absolute root and simple file pattern must be valid.");
        Require(!FileDropTriggerConfigurationContract.TryValidate(config with { WatchRoot = "relative" }, out _), "A trigger must never use an ambiguous relative root.");
        Require(!FileDropTriggerConfigurationContract.TryValidate(config with { Pattern = "..\\*.csv" }, out _), "A trigger pattern must never escape its configured root.");
        Require(config.ConfigurationFingerprint == config.ConfigurationFingerprint, "Configuration fingerprint must be deterministic.");
        Require(config.ConfigurationFingerprint != (config with { Pattern = "*.xlsx" }).ConfigurationFingerprint, "A configuration change must invalidate the prior fingerprint.");
        Require(!FileDropTriggerConfigurationContract.TryValidateDispatchBinding(config, out _), "Detection-only configuration must not be treated as dispatch-ready.");
        Require(FileDropTriggerConfigurationContract.TryValidateDispatchBinding(config with { TargetTemplatePath = Path.Combine(Path.GetTempPath(), "target.anlabel"), QueueName = "Named queue" }, out _), "Explicit template and named queue are required before a future dispatch binding can be considered valid.");

        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");
        try
        {
            var store = new FileDropTriggerConfigurationStore(path);
            var saved = store.Save(config);
            var loaded = store.Read(out var diagnostic);
            Require(diagnostic is null && loaded == saved, "Configuration snapshot must round-trip with integrity.");
            var legacyConfig = config;
            var legacyFingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join("|", legacyConfig.TriggerId, legacyConfig.Name, Path.GetFullPath(legacyConfig.WatchRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).ToUpperInvariant(), legacyConfig.Pattern.ToUpperInvariant(), legacyConfig.Recursive ? "recursive" : "flat", legacyConfig.Enabled ? "enabled" : "disabled"))));
            var legacyIntegrity = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{legacyConfig.TriggerId}|{legacyConfig.Name}|{legacyConfig.WatchRoot}|{legacyConfig.Pattern}|{legacyConfig.Recursive}|{legacyConfig.Enabled}|{legacyFingerprint}")));
            File.WriteAllText(path, JsonSerializer.Serialize(new FileDropTriggerConfigurationSnapshot(legacyConfig, legacyFingerprint, legacyIntegrity)));
            Require(store.Read(out diagnostic) is { } migrated && diagnostic is null && migrated.ConfigurationFingerprint == legacyConfig.ConfigurationFingerprint, "Legacy configuration snapshot must migrate without becoming a corrupt audit.");
            File.WriteAllText(path, "{}");
            Require(store.Read(out diagnostic) is null && diagnostic is not null, "Invalid configuration data must fail closed.");
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
        return Task.CompletedTask;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
