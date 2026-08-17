using System.IO.Compression;
using System.Text;

namespace ANLAbel.Core.Barcode;

/// <summary>
/// Offline copy of the official GS1 Application Identifiers JSON-LD registry.
/// The payload is gzip-compressed UTF-8 JSON so normalizer/preflight behavior
/// stays deterministic without network access. Regenerate only from the
/// published source and review the snapshot's provenance/hash in tests.
/// </summary>
public static class Gs1OfficialRegistryBundle
{
    public const string SourceUri = "https://ref.gs1.org/ai/GS1_Application_Identifiers.jsonld";

    private static readonly Lazy<Gs1OfficialRegistrySnapshot> Snapshot = new(LoadCore);

    public static Gs1OfficialRegistrySnapshot Load() => Snapshot.Value;

    private static Gs1OfficialRegistrySnapshot LoadCore()
    {
        var compressed = Convert.FromBase64String(Payload);
        using var source = new MemoryStream(compressed, writable: false);
        using var gzip = new GZipStream(source, CompressionMode.Decompress);
        using var reader = new StreamReader(gzip, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var json = reader.ReadToEnd();
        if (!Gs1OfficialRegistrySnapshot.TryParse(json, out var snapshot, out var errors))
        {
            throw new InvalidOperationException($"Bundled GS1 registry is invalid: {string.Join(" ", errors)}");
        }

        return snapshot!;
    }

    private const string Payload =
        "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";
}
