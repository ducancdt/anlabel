using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using ANLAbel.Core.Enums;

namespace ANLAbel.Core.Printing;

/// <summary>
/// Immutable, value-only description of the output contract used to compile a
/// device render plan.  It deliberately stores the hashes of the requested and
/// driver-validated PrintTickets rather than the WPF ticket object itself, so a
/// job log can prove which effective settings were reviewed without retaining a
/// thread-affine printer object.
/// </summary>
public sealed record EffectiveOutputContract
{
    public const string ContractVersion = "effective-output-contract/v1";

    public string PrinterName { get; init; } = string.Empty;
    public string RequestedTicketHash { get; init; } = string.Empty;
    public string EffectiveTicketHash { get; init; } = string.Empty;
    public int DpiX { get; init; }
    public int DpiY { get; init; }
    public int LabelWidthDots { get; init; }
    public int LabelHeightDots { get; init; }
    public int PrintableOriginXDots { get; init; }
    public int PrintableOriginYDots { get; init; }
    public int PrintableWidthDots { get; init; }
    public int PrintableHeightDots { get; init; }
    public double LabelWidthMm { get; init; }
    public double LabelHeightMm { get; init; }
    public double GapMm { get; init; }
    public double MarginMm { get; init; }
    public double OffsetXMm { get; init; }
    public double OffsetYMm { get; init; }
    public double ScaleX { get; init; } = 1;
    public double ScaleY { get; init; } = 1;
    public LabelMediaType MediaType { get; init; } = LabelMediaType.Gap;
    public FeedDirection FeedDirection { get; init; } = FeedDirection.TopToBottom;
    public bool Rotated180 { get; init; }
    public double PrintableOriginXDip { get; init; }
    public double PrintableOriginYDip { get; init; }
    public double PrintableWidthDip { get; init; }
    public double PrintableHeightDip { get; init; }
    public bool PrintableAreaVerified { get; init; }

    public bool IsTicketValidated => !string.IsNullOrWhiteSpace(EffectiveTicketHash);

    /// <summary>
    /// Stable SHA-256 fingerprint of every value that affects the physical
    /// output contract.  Invariant formatting makes it independent of the
    /// operator's locale and process culture.
    /// </summary>
    public string Fingerprint => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(CanonicalForm()))).ToLowerInvariant();

    public string CanonicalForm()
    {
        static string Number(double value) => value.ToString("R", CultureInfo.InvariantCulture);
        static string Flag(bool value) => value ? "1" : "0";

        return string.Join("|", new[]
        {
            ContractVersion,
            PrinterName.Trim(),
            RequestedTicketHash.Trim().ToLowerInvariant(),
            EffectiveTicketHash.Trim().ToLowerInvariant(),
            DpiX.ToString(CultureInfo.InvariantCulture),
            DpiY.ToString(CultureInfo.InvariantCulture),
            LabelWidthDots.ToString(CultureInfo.InvariantCulture),
            LabelHeightDots.ToString(CultureInfo.InvariantCulture),
            PrintableOriginXDots.ToString(CultureInfo.InvariantCulture),
            PrintableOriginYDots.ToString(CultureInfo.InvariantCulture),
            PrintableWidthDots.ToString(CultureInfo.InvariantCulture),
            PrintableHeightDots.ToString(CultureInfo.InvariantCulture),
            Number(LabelWidthMm),
            Number(LabelHeightMm),
            Number(GapMm),
            Number(MarginMm),
            Number(OffsetXMm),
            Number(OffsetYMm),
            Number(ScaleX),
            Number(ScaleY),
            MediaType.ToString(),
            FeedDirection.ToString(),
            Flag(Rotated180),
            Number(PrintableOriginXDip),
            Number(PrintableOriginYDip),
            Number(PrintableWidthDip),
            Number(PrintableHeightDip),
            Flag(PrintableAreaVerified)
        });
    }
}
