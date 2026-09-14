using ANLAbel.Core.Enums;
using ANLAbel.Core.Printing;
using Xunit;

namespace ANLAbel.UnitTests;

public sealed class EffectiveOutputContractTests
{
    [Fact]
    public void FingerprintIsDeterministicAndLocaleIndependent()
    {
        var contract = NewContract();
        var original = System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("vi-VN");
            var first = contract.Fingerprint;
            System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("en-US");
            Assert.Equal(first, contract.Fingerprint);
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentCulture = original;
        }
    }

    [Fact]
    public void FingerprintChangesWhenEffectiveDpiOrPrintableBoundsChange()
    {
        var contract = NewContract();
        var dpiChanged = contract with { DpiX = 305 };
        var boundsChanged = contract with { PrintableWidthDip = contract.PrintableWidthDip + 1 };

        Assert.NotEqual(contract.Fingerprint, dpiChanged.Fingerprint);
        Assert.NotEqual(contract.Fingerprint, boundsChanged.Fingerprint);
    }

    [Fact]
    public void UnvalidatedTicketIsExplicitlyDistinguishable()
    {
        var contract = NewContract() with { EffectiveTicketHash = string.Empty };

        Assert.False(contract.IsTicketValidated);
        Assert.Contains(EffectiveOutputContract.ContractVersion, contract.CanonicalForm(), StringComparison.Ordinal);
    }

    [Fact]
    public void CanonicalFormPinsEveryFieldSeparatorAndFlag()
    {
        var canonical = NewContract().CanonicalForm();

        Assert.Equal(
            "effective-output-contract/v1|Queue-A|requested|effective|203|305|0|0|0|0|0|0|100|50|2|1|0.1|-0.2|1|1|Gap|TopToBottom|0|2|3|370|180|1",
            canonical);
    }

    [Fact]
    public void DefaultContractLeavesTheTextIdentityFieldsEmpty()
    {
        // Version then three empty string fields (printer/requested/effective),
        // pinning the string.Empty init defaults.
        Assert.StartsWith(
            "effective-output-contract/v1||||0",
            new EffectiveOutputContract().CanonicalForm(),
            StringComparison.Ordinal);
    }

    private static EffectiveOutputContract NewContract() => new()
    {
        PrinterName = "Queue-A",
        RequestedTicketHash = "requested",
        EffectiveTicketHash = "effective",
        DpiX = 203,
        DpiY = 305,
        LabelWidthMm = 100,
        LabelHeightMm = 50,
        GapMm = 2,
        MarginMm = 1,
        OffsetXMm = 0.1,
        OffsetYMm = -0.2,
        ScaleX = 1,
        ScaleY = 1,
        MediaType = LabelMediaType.Gap,
        FeedDirection = FeedDirection.TopToBottom,
        PrintableOriginXDip = 2,
        PrintableOriginYDip = 3,
        PrintableWidthDip = 370,
        PrintableHeightDip = 180,
        PrintableAreaVerified = true
    };
}
