using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ANLAbel.Core.Barcode;

/// <summary>
/// Immutable import of the official GS1 Application Identifiers JSON-LD file.
/// It deliberately retains only the fields needed for safe element-string
/// boundary decisions; callers may persist the original source separately for
/// legal/version review.
/// </summary>
public sealed record Gs1OfficialRegistrySnapshot(
    string SourceVersion,
    string SourceLastModified,
    string ContentSha256,
    ImmutableDictionary<string, Gs1AiDefinition> Definitions)
{
    public bool TryGetDefinition(string applicationIdentifier, out Gs1AiDefinition definition)
        => Definitions.TryGetValue(applicationIdentifier, out definition!);

    public static bool TryParse(string json, out Gs1OfficialRegistrySnapshot? snapshot, out IReadOnlyList<string> errors)
    {
        snapshot = null;
        var diagnostics = new List<string>();
        if (string.IsNullOrWhiteSpace(json))
        {
            diagnostics.Add("GS1 registry JSON is empty.");
            errors = diagnostics;
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            if (!document.RootElement.TryGetProperty("applicationIdentifiers", out var items)
                || items.ValueKind != JsonValueKind.Array)
            {
                diagnostics.Add("GS1 registry JSON is missing the applicationIdentifiers array.");
                errors = diagnostics;
                return false;
            }

            var definitions = ImmutableDictionary.CreateBuilder<string, Gs1AiDefinition>(StringComparer.Ordinal);
            var version = "unknown";
            var lastModified = "unknown";
            foreach (var item in items.EnumerateArray())
            {
                if (item.TryGetProperty("owl:versionInfo", out var versionElement)
                    && versionElement.ValueKind == JsonValueKind.String)
                {
                    version = versionElement.GetString() ?? version;
                }

                if (item.TryGetProperty("dc:lastModified", out var modifiedElement)
                    && modifiedElement.ValueKind == JsonValueKind.Object
                    && modifiedElement.TryGetProperty("@value", out var dateElement)
                    && dateElement.ValueKind == JsonValueKind.String)
                {
                    lastModified = dateElement.GetString() ?? lastModified;
                }

                if (!item.TryGetProperty("applicationIdentifier", out var aiElement)
                    || aiElement.ValueKind != JsonValueKind.String)
                {
                    continue;
                }

                var ai = aiElement.GetString() ?? string.Empty;
                if (ai.Length is < 2 or > 4 || !ai.All(char.IsDigit))
                {
                    diagnostics.Add($"Official registry contains invalid AI '{ai}'.");
                    continue;
                }

                if (!item.TryGetProperty("separatorRequired", out var separatorElement)
                    || (separatorElement.ValueKind is not JsonValueKind.True and not JsonValueKind.False))
                {
                    diagnostics.Add($"Official registry AI {ai} has no boolean separatorRequired value.");
                    continue;
                }

                var boundary = separatorElement.GetBoolean()
                    ? Gs1AiBoundaryKind.SeparatorRequired
                    : Gs1AiBoundaryKind.PredefinedLength;
                var valuePattern = item.TryGetProperty("regex", out var regexElement)
                    && regexElement.ValueKind == JsonValueKind.String
                    ? regexElement.GetString()
                    : null;
                if (string.IsNullOrWhiteSpace(valuePattern))
                {
                    diagnostics.Add($"Official registry AI {ai} has no regex value pattern.");
                    continue;
                }

                if (!definitions.TryAdd(ai, new Gs1AiDefinition(ai, boundary, valuePattern)))
                {
                    diagnostics.Add($"Official registry defines AI {ai} more than once.");
                }
            }

            if (definitions.Count == 0)
            {
                diagnostics.Add("GS1 registry contains no application identifier definitions.");
            }

            if (diagnostics.Count > 0)
            {
                errors = diagnostics;
                return false;
            }

            snapshot = new Gs1OfficialRegistrySnapshot(
                version,
                lastModified,
                Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json))),
                definitions.ToImmutable());
            errors = Array.Empty<string>();
            return true;
        }
        catch (JsonException exception)
        {
            diagnostics.Add($"GS1 registry JSON is invalid: {exception.Message}");
            errors = diagnostics;
            return false;
        }
    }
}
