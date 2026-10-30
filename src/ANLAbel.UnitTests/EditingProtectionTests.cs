using ANLAbel.Core.Models;
using Xunit;

namespace ANLAbel.UnitTests;

/// <summary>NiceLabel 10 Protection: the unlock password is kept as a salted hash.</summary>
public sealed class EditingProtectionTests
{
    [Fact]
    public void TheRightPasswordUnlocksAndTheFileNeverHoldsIt()
    {
        var hash = EditingProtection.CreatePasswordHash("Kho-2026");
        Assert.DoesNotContain("Kho-2026", hash, StringComparison.Ordinal);
        Assert.True(EditingProtection.VerifyPassword(hash, "Kho-2026"));
        Assert.False(EditingProtection.VerifyPassword(hash, "kho-2026"));
        Assert.False(EditingProtection.VerifyPassword(hash, string.Empty));
        Assert.NotEqual(hash, EditingProtection.CreatePasswordHash("Kho-2026"));
    }

    [Fact]
    public void AnEmptyOrDamagedHashUnlocksNothing()
    {
        Assert.False(EditingProtection.VerifyPassword(string.Empty, "x"));
        Assert.False(EditingProtection.VerifyPassword("plain-text", "plain-text"));
        Assert.False(EditingProtection.VerifyPassword("pbkdf2-sha256$100000$not base64$abc", "x"));
    }
}
