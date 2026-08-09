using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ANLAbel.Core.Licensing;
using Microsoft.Win32;

namespace ANLAbel.App.Services;

internal enum TrialCheckStatus { Valid, Expired, ClockTampered, StorageError }

internal sealed record TrialCheckResult(
    TrialCheckStatus Status,
    TimeSpan Remaining,
    bool IsFirstRun,
    string? ErrorMessage = null,
    bool IsActivated = false,
    DateTimeOffset? ActivationExpiresUtc = null,
    string? ActivationCustomer = null)
{
    public bool IsAllowed => Status == TrialCheckStatus.Valid;
}

internal sealed class TrialLicenseService
{
    private const string RegistryPath = @"Software\ANLAbel\Runtime";
    private const string RegistryValue = "State";
    private const string ActivationRegistryValue = "License";
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("ANLAbel.Trial.v1.2026");
    private readonly string[] _stateFiles =
    [
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ANLAbel", ".runtime"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ANLAbel", ".runtime"),
    ];

    public string MachineCode => FormatMachineCode(GetMachineId());

    public TrialCheckResult Check(DateTimeOffset? utcNow = null)
    {
        var now = (utcNow ?? DateTimeOffset.UtcNow).ToUniversalTime();
        var activation = LoadActivationKey();
        if (!string.IsNullOrWhiteSpace(activation))
        {
            var validation = ActivationLicense.Validate(activation, GetMachineId(), now);
            if (validation.IsValid)
                return new(TrialCheckStatus.Valid, TimeSpan.MaxValue, false, null, true,
                    validation.Payload?.ExpiresUtc, validation.Payload?.Customer);
        }

        var records = new List<TrialRecord>();
        var sawStoredState = false;
        var sawInvalidState = false;

        foreach (var file in _stateFiles)
        {
            if (!File.Exists(file)) continue;
            sawStoredState = true;
            if (TryUnprotect(File.ReadAllBytes(file), out var record)) records.Add(record!);
            else sawInvalidState = true;
        }

        using (var key = Registry.CurrentUser.OpenSubKey(RegistryPath, writable: false))
        {
            if (key?.GetValue(RegistryValue) is byte[] data)
            {
                sawStoredState = true;
                if (TryUnprotect(data, out var record)) records.Add(record!);
                else sawInvalidState = true;
            }
        }

        if (sawInvalidState || (sawStoredState && records.Count == 0))
            return new(TrialCheckStatus.ClockTampered, TimeSpan.Zero, false, "Dữ liệu dùng thử đã bị thay đổi hoặc không còn hợp lệ.");

        var machineId = GetMachineId();
        if (records.Any(x => !string.Equals(x.MachineId, machineId, StringComparison.Ordinal)))
            return new(TrialCheckStatus.ClockTampered, TimeSpan.Zero, false, "Dữ liệu dùng thử không thuộc máy tính này.");

        var isFirstRun = records.Count == 0;
        var firstRun = isFirstRun ? now : records.Min(x => x.FirstRunUtc);
        var lastSeen = isFirstRun ? now : records.Max(x => x.LastSeenUtc);
        var decision = TrialPolicy.Evaluate(firstRun, lastSeen, now);
        if (!decision.IsAllowed)
            return new(decision.Status == TrialStatus.Expired ? TrialCheckStatus.Expired : TrialCheckStatus.ClockTampered, TimeSpan.Zero, false);

        try
        {
            PersistEverywhere(new TrialRecord(firstRun, now > lastSeen ? now : lastSeen, machineId));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or CryptographicException)
        {
            return new(TrialCheckStatus.StorageError, TimeSpan.Zero, isFirstRun, "Không thể lưu trạng thái dùng thử an toàn.");
        }

        return new(TrialCheckStatus.Valid, decision.Remaining, isFirstRun);
    }

    public ActivationValidation Activate(string activationKey)
    {
        var validation = ActivationLicense.Validate(activationKey, GetMachineId());
        if (!validation.IsValid) return validation;

        var data = ProtectedData.Protect(Encoding.UTF8.GetBytes(activationKey.Trim()), Entropy, DataProtectionScope.CurrentUser);
        var successes = 0;
        foreach (var file in _stateFiles.Select(x => x + ".license"))
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(file)!);
                File.WriteAllBytes(file, data);
                File.SetAttributes(file, File.GetAttributes(file) | FileAttributes.Hidden);
                successes++;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
        }

        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(RegistryPath, writable: true);
            key.SetValue(ActivationRegistryValue, data, RegistryValueKind.Binary);
            successes++;
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or System.Security.SecurityException) { }

        if (successes == 0) throw new IOException("Không thể lưu key kích hoạt trên máy này.");
        return validation;
    }

    private string? LoadActivationKey()
    {
        var candidates = new List<byte[]>();
        foreach (var file in _stateFiles.Select(x => x + ".license"))
            if (File.Exists(file)) candidates.Add(File.ReadAllBytes(file));
        using (var key = Registry.CurrentUser.OpenSubKey(RegistryPath, writable: false))
            if (key?.GetValue(ActivationRegistryValue) is byte[] data) candidates.Add(data);

        foreach (var protectedData in candidates)
        {
            try
            {
                var value = Encoding.UTF8.GetString(ProtectedData.Unprotect(protectedData, Entropy, DataProtectionScope.CurrentUser));
                if (ActivationLicense.Validate(value, GetMachineId()).IsValid) return value;
            }
            catch (CryptographicException) { }
        }
        return null;
    }

    private void PersistEverywhere(TrialRecord record)
    {
        var data = Protect(record);
        var successes = 0;
        foreach (var file in _stateFiles)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(file)!);
                var temp = file + ".tmp";
                File.WriteAllBytes(temp, data);
                File.Move(temp, file, overwrite: true);
                File.SetAttributes(file, File.GetAttributes(file) | FileAttributes.Hidden);
                successes++;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
        }

        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(RegistryPath, writable: true);
            key.SetValue(RegistryValue, data, RegistryValueKind.Binary);
            successes++;
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or System.Security.SecurityException) { }

        if (successes == 0) throw new IOException("No protected trial state store is writable.");
    }

    private static byte[] Protect(TrialRecord record) => ProtectedData.Protect(
        JsonSerializer.SerializeToUtf8Bytes(record), Entropy, DataProtectionScope.CurrentUser);

    private static bool TryUnprotect(byte[] data, out TrialRecord? record)
    {
        try
        {
            record = JsonSerializer.Deserialize<TrialRecord>(ProtectedData.Unprotect(data, Entropy, DataProtectionScope.CurrentUser));
            return record is not null && record.FirstRunUtc != default && record.LastSeenUtc != default && record.MachineId.Length == 64;
        }
        catch (Exception ex) when (ex is CryptographicException or JsonException or FormatException)
        {
            record = null;
            return false;
        }
    }

    private static string GetMachineId()
    {
        using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Cryptography");
        var raw = key?.GetValue("MachineGuid")?.ToString();
        if (string.IsNullOrWhiteSpace(raw)) raw = $"{Environment.MachineName}|{Environment.SystemDirectory}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));
    }

    private static string FormatMachineCode(string value) => string.Join("-", Enumerable.Range(0, 8).Select(i => value.Substring(i * 8, 8)));

    private sealed record TrialRecord(DateTimeOffset FirstRunUtc, DateTimeOffset LastSeenUtc, string MachineId);
}
