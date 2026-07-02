using System.Security.Cryptography;
using ANLAbel.Core.Licensing;
using Xunit;

namespace ANLAbel.UnitTests;

public sealed class ActivationLicenseTests
{
    private const string MachineA = "0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF";
    private const string MachineB = "1123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF";

    [Fact]
    public void SignedKey_IsValidOnlyForTargetMachine()
    {
        using var signer = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var key = ActivationLicense.Create(MachineA, "Test Customer", null, signer.ExportPkcs8PrivateKey());
        var publicKey = Convert.ToBase64String(signer.ExportSubjectPublicKeyInfo());

        Assert.True(ActivationLicense.Validate(key, MachineA, publicKeyBase64: publicKey).IsValid);
        Assert.Equal(ActivationValidationStatus.WrongMachine, ActivationLicense.Validate(key, MachineB, publicKeyBase64: publicKey).Status);
    }

    [Fact]
    public void ModifiedKey_IsRejected()
    {
        using var signer = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var key = ActivationLicense.Create(MachineA, "Test Customer", null, signer.ExportPkcs8PrivateKey());
        var publicKey = Convert.ToBase64String(signer.ExportSubjectPublicKeyInfo());
        var changed = key[..^1] + (key[^1] == 'A' ? 'B' : 'A');

        Assert.Equal(ActivationValidationStatus.Invalid, ActivationLicense.Validate(changed, MachineA, publicKeyBase64: publicKey).Status);
    }

    [Fact]
    public void ExpiredSignedKey_IsRejected()
    {
        using var signer = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var key = ActivationLicense.Create(MachineA, "Test Customer", DateTimeOffset.UtcNow.AddDays(-1), signer.ExportPkcs8PrivateKey());
        var publicKey = Convert.ToBase64String(signer.ExportSubjectPublicKeyInfo());

        Assert.Equal(ActivationValidationStatus.Expired, ActivationLicense.Validate(key, MachineA, publicKeyBase64: publicKey).Status);
    }
}
