using System.Security.Cryptography;
using System.Text;
using ANLAbel.Core.Workflow;

namespace ANLAbel.Core.Automation;

/// <summary>Portable local trigger settings. Runtime filesystem checks remain with a future runner.</summary>
public sealed record FileDropTriggerConfiguration(
    string TriggerId,
    string Name,
    string WatchRoot,
    string Pattern,
    bool Recursive,
    bool Enabled,
    string TargetTemplatePath = "",
    string QueueName = "",
    DocumentWorkflowPrintPolicyMode PrintPolicyMode = DocumentWorkflowPrintPolicyMode.Off)
{
    public string ConfigurationFingerprint => FileDropTriggerConfigurationContract.ComputeFingerprint(this);
}

public static class FileDropTriggerConfigurationContract
{
    public static bool TryValidate(FileDropTriggerConfiguration configuration, out string error)
    {
        if (string.IsNullOrWhiteSpace(configuration.TriggerId))
        {
            error = "A stable trigger ID is required.";
            return false;
        }
        if (string.IsNullOrWhiteSpace(configuration.Name))
        {
            error = "A trigger name is required.";
            return false;
        }
        if (string.IsNullOrWhiteSpace(configuration.WatchRoot) || !Path.IsPathFullyQualified(configuration.WatchRoot))
        {
            error = "Watch root must be an absolute local path.";
            return false;
        }
        if (string.IsNullOrWhiteSpace(configuration.Pattern) ||
            configuration.Pattern.IndexOfAny(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }) >= 0 ||
            configuration.Pattern.Contains("..", StringComparison.Ordinal))
        {
            error = "Pattern must be a file-name pattern, not a path.";
            return false;
        }
        error = string.Empty;
        return true;
    }

    public static string ComputeFingerprint(FileDropTriggerConfiguration configuration)
    {
        if (!TryValidate(configuration, out var error)) throw new ArgumentException(error, nameof(configuration));
        var canonical = string.Join("|",
            configuration.TriggerId.Trim(),
            configuration.Name.Trim(),
            Path.GetFullPath(configuration.WatchRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).ToUpperInvariant(),
            configuration.Pattern.Trim().ToUpperInvariant(),
            configuration.Recursive ? "recursive" : "flat",
            configuration.Enabled ? "enabled" : "disabled",
            string.IsNullOrWhiteSpace(configuration.TargetTemplatePath) ? "no-template" : Path.GetFullPath(configuration.TargetTemplatePath).ToUpperInvariant(),
            string.IsNullOrWhiteSpace(configuration.QueueName) ? "no-queue" : configuration.QueueName.Trim().ToUpperInvariant(),
            configuration.PrintPolicyMode.ToString());
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }

    /// <summary>Future dispatch must call this additional gate; watcher/detection does not.</summary>
    public static bool TryValidateDispatchBinding(FileDropTriggerConfiguration configuration, out string error)
    {
        if (!TryValidate(configuration, out error)) return false;
        if (string.IsNullOrWhiteSpace(configuration.TargetTemplatePath) || !Path.IsPathFullyQualified(configuration.TargetTemplatePath)) { error = "Dispatch requires an absolute target template path."; return false; }
        if (string.IsNullOrWhiteSpace(configuration.QueueName)) { error = "Dispatch requires one explicit queue name."; return false; }
        if (!Enum.IsDefined(configuration.PrintPolicyMode)) { error = "Dispatch requires a recognized document workflow policy."; return false; }
        error = string.Empty;
        return true;
    }
}
