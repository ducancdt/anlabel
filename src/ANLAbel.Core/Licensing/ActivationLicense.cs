using System.Security.Cryptography;
using System.Text.Json;

namespace ANLAbel.Core.Licensing;

public sealed record ActivationPayload(
    int Version,
    string Product,
    string MachineId,
    string Customer,
    DateTimeOffset IssuedUtc,
    DateTimeOffset? ExpiresUtc,
    string LicenseId);

public enum ActivationValidationStatus { Valid, Invalid, WrongMachine, Expired }

public sealed record ActivationValidation(ActivationValidationStatus Status, ActivationPayload? Payload = null)
{
    public bool IsValid => Status == ActivationValidationStatus.Valid;
}

public static class ActivationLicense
{
    public const string ProductId = "ANLABEL-DESKTOP";
    public const string PublicKeyBase64 = "MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAEyKxwGJ9ydobO857ZHr7nw+GEHqcdPH0x0JVYHN1VLg0dXN3nitMztrPfdvUnwNqjYpjNwBjpMkvUClkrsnhBGg==";

    public static string Create(string machineId, string customer, DateTimeOffset? expiresUtc, ReadOnlySpan<byte> privateKeyPkcs8)
    {
        machineId = NormalizeMachineId(machineId);
        if (machineId.Length != 64) throw new ArgumentException("Machine code must contain 64 hexadecimal characters.", nameof(machineId));

        var payload = new ActivationPayload(1, ProductId, machineId, customer.Trim(), DateTimeOffset.UtcNow,
            expiresUtc?.ToUniversalTime(), Guid.NewGuid().ToString("N").ToUpperInvariant());
        var payloadBytes = JsonSerializer.SerializeToUtf8Bytes(payload);
        using var signer = ECDsa.Create();
        signer.ImportPkcs8PrivateKey(privateKeyPkcs8, out _);
        var signature = signer.SignData(payloadBytes, HashAlgorithmName.SHA256, DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
        return $"ANL1.{Base64UrlEncode(payloadBytes)}.{Base64UrlEncode(signature)}";
    }

    public static ActivationValidation Validate(string key, string machineId, DateTimeOffset? utcNow = null, string? publicKeyBase64 = null)
    {
        try
        {
            var parts = key.Trim().Split('.');
            if (parts.Length != 3 || parts[0] != "ANL1") return new(ActivationValidationStatus.Invalid);
            var payloadBytes = Base64UrlDecode(parts[1]);
            var signature = Base64UrlDecode(parts[2]);
            if (payloadBytes.Length > 4096 || signature.Length != 64) return new(ActivationValidationStatus.Invalid);

            using var verifier = ECDsa.Create();
            verifier.ImportSubjectPublicKeyInfo(Convert.FromBase64String(publicKeyBase64 ?? PublicKeyBase64), out _);
            if (!verifier.VerifyData(payloadBytes, signature, HashAlgorithmName.SHA256, DSASignatureFormat.IeeeP1363FixedFieldConcatenation))
                return new(ActivationValidationStatus.Invalid);

            var payload = JsonSerializer.Deserialize<ActivationPayload>(payloadBytes);
            if (payload is null || payload.Version != 1 || payload.Product != ProductId)
                return new(ActivationValidationStatus.Invalid);
            if (!string.Equals(payload.MachineId, NormalizeMachineId(machineId), StringComparison.Ordinal))
                return new(ActivationValidationStatus.WrongMachine, payload);
            if (payload.ExpiresUtc is { } expiry && (utcNow ?? DateTimeOffset.UtcNow).ToUniversalTime() >= expiry)
                return new(ActivationValidationStatus.Expired, payload);
            return new(ActivationValidationStatus.Valid, payload);
        }
        catch (Exception ex) when (ex is FormatException or JsonException or CryptographicException or ArgumentException)
        {
            return new(ActivationValidationStatus.Invalid);
        }
    }

    public static string NormalizeMachineId(string value) =>
        new(value.Where(Uri.IsHexDigit).Select(char.ToUpperInvariant).ToArray());

    private static string Base64UrlEncode(byte[] value) => Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    private static byte[] Base64UrlDecode(string value)
    {
        var padded = value.Replace('-', '+').Replace('_', '/');
        padded += new string('=', (4 - padded.Length % 4) % 4);
        var decoded = Convert.FromBase64String(padded);
        if (!string.Equals(Base64UrlEncode(decoded), value, StringComparison.Ordinal))
            throw new FormatException("Non-canonical activation key encoding.");
        return decoded;
    }
}
