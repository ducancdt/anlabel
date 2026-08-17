using ANLAbel.Core.Barcode;
using ANLAbel.Core.Enums;
using Xunit;

namespace ANLAbel.UnitTests;

public sealed class BarcodeVerificationContractTests
{
    [Fact]
    public void ExpectedFingerprintBindsSymbologyProfileAndCanonicalPayload()
    {
        var code128 = BarcodeVerificationContract.CreateExpectation(
            BarcodeSymbology.Code128,
            BarcodeApplicationProfile.General,
            "ABC-123",
            BarcodeVerificationGradeScale.Ansi,
            "B");
        var qr = BarcodeVerificationContract.CreateExpectation(
            BarcodeSymbology.QRCode,
            BarcodeApplicationProfile.General,
            "ABC-123",
            BarcodeVerificationGradeScale.Ansi,
            "B");

        Assert.True(code128.IsValid);
        Assert.True(qr.IsValid);
        Assert.NotEqual(code128.ExpectedContentFingerprint, qr.ExpectedContentFingerprint);
        Assert.NotEqual(code128.Fingerprint, qr.Fingerprint);
    }

    [Fact]
    public void Gs1ExpectationUsesTheSharedApplicationNormalization()
    {
        var expectation = BarcodeVerificationContract.CreateExpectation(
            BarcodeSymbology.Code128,
            BarcodeApplicationProfile.Gs1,
            "(01)09506000134352(17)250101(10)ABC(21)SERIAL",
            BarcodeVerificationGradeScale.Iso15416,
            "3");

        Assert.True(expectation.IsValid);
        Assert.True(BarcodeVerificationContract.TryNormalizePayload(
            BarcodeApplicationProfile.Gs1,
            "(01)09506000134352(17)250101(10)ABC(21)SERIAL",
            out var normalized,
            out var error), error);
        Assert.Contains(BarcodeApplicationContract.GroupSeparator, normalized);
        Assert.True(BarcodeVerificationContract.ValidateObserved(
            expectation,
            expectation.ExpectedContentFingerprint,
            expectation.ExpectedContentFingerprint,
            "ISO15416:4").IsAccepted);
    }

    [Fact]
    public void UnknownOrBelowGradeFailsClosed()
    {
        var expectation = BarcodeVerificationContract.CreateExpectation(
            BarcodeSymbology.Code128,
            BarcodeApplicationProfile.General,
            "ABC",
            BarcodeVerificationGradeScale.Ansi,
            "B");

        var unknown = BarcodeVerificationContract.ValidateObserved(
            expectation,
            expectation.ExpectedContentFingerprint,
            expectation.ExpectedContentFingerprint,
            "vendor-green");
        var below = BarcodeVerificationContract.ValidateObserved(
            expectation,
            expectation.ExpectedContentFingerprint,
            expectation.ExpectedContentFingerprint,
            "C");

        Assert.False(unknown.IsAccepted);
        Assert.Equal("grade-invalid", unknown.Code);
        Assert.False(below.IsAccepted);
        Assert.Equal("grade-below-threshold", below.Code);
    }
}
