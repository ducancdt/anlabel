using ANLAbel.Barcode.Renderers;
using ANLAbel.Core.Barcode;
using ANLAbel.Core.Enums;
using Xunit;

namespace ANLAbel.UnitTests;

/// <summary>
/// CORE B07 / plan 13 QĐ-8: every barcode type ANLAbel offers must carry a
/// positive and a negative payload fixture and a decode round-trip. The list is
/// taken from the runtime enum, so a new type without fixtures fails here.
/// </summary>
public sealed class BarcodeEvidenceMatrixTests
{
    private static readonly IReadOnlyDictionary<BarcodeSymbology, (string Valid, string Invalid)> Fixtures =
        new Dictionary<BarcodeSymbology, (string, string)>
        {
            [BarcodeSymbology.Code128] = ("CODE-128_TEST!", "CODE128\u00A0NBSP"),
            [BarcodeSymbology.QRCode] = (" 000123\nLô A ", " "),
            [BarcodeSymbology.DataMatrix] = ("LOT-2026-X", " "),
            [BarcodeSymbology.Code39] = ("CODE-39", "CODE39@BAD"),
            [BarcodeSymbology.Code93] = ("CODE93", "CODE93\u00A0NBSP"),
            [BarcodeSymbology.Ean13] = ("5901234123457", "5901234123458"),
            [BarcodeSymbology.Ean8] = ("96385074", "96385075"),
            [BarcodeSymbology.UpcA] = ("036000291452", "036000291453"),
            [BarcodeSymbology.UpcE] = ("01234565", "01234A"),
            [BarcodeSymbology.ITF] = ("1234567890", "12345"),
            [BarcodeSymbology.Codabar] = ("A123456B", "123@456"),
            [BarcodeSymbology.Pdf417] = ("PDF417-2026", " "),
            [BarcodeSymbology.Aztec] = ("AZTEC-2026", " "),
            [BarcodeSymbology.MSI] = ("123456", "12345A"),
            [BarcodeSymbology.Plessey] = ("98765", "9876F"),
        };

    public static IEnumerable<object[]> Symbologies() =>
        Enum.GetValues<BarcodeSymbology>().Select(symbology => new object[] { symbology });

    [Theory]
    [MemberData(nameof(Symbologies))]
    public void Every_offered_type_rejects_its_negative_fixture_and_accepts_its_positive_one(BarcodeSymbology symbology)
    {
        Assert.True(Fixtures.TryGetValue(symbology, out var fixture),
            $"{symbology} has no payload fixtures; add them before offering the type.");

        Assert.Empty(BarcodeApplicationContract.ValidateData(BarcodeApplicationProfile.General, symbology, fixture.Valid));
        Assert.NotEmpty(BarcodeApplicationContract.ValidateData(BarcodeApplicationProfile.General, symbology, fixture.Invalid));
    }

    [Theory]
    [MemberData(nameof(Symbologies))]
    public void Every_offered_type_has_a_decode_round_trip_from_csv_and_xlsx_at_each_dpi(BarcodeSymbology symbology)
    {
        var type = Enum.Parse<BarcodeType>(symbology.ToString());
        var cases = ImportedBarcodeRoundTripTests.Cases()
            .Where(item => (BarcodeType)item[2] == type)
            .Select(item => ((string)item[0], (int)item[1]))
            .ToHashSet();

        foreach (var extension in new[] { ".csv", ".xlsx" })
        foreach (var dpi in new[] { 203, 300, 600 })
        {
            Assert.Contains((extension, dpi), cases);
        }
    }
}
