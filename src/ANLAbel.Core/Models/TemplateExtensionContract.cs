using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ANLAbel.Core.Models;

/// <summary>
/// Produces a deterministic identity for extension members preserved on a
/// template. Older builds do not interpret these members, but they must still
/// distinguish documents that carry different extension metadata.
/// </summary>
public static class TemplateExtensionContract
{
    public static string ComputeFingerprint(IReadOnlyDictionary<string, JsonElement>? extensions)
    {
        if (extensions is null || extensions.Count == 0)
        {
            return string.Empty;
        }

        var canonical = new StringBuilder();
        foreach (var pair in extensions.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            canonical.Append(pair.Key).Append('=');
            AppendElement(canonical, pair.Value);
            canonical.Append(';');
        }

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical.ToString())));
    }

    private static void AppendElement(StringBuilder builder, JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                builder.Append('{');
                foreach (var property in element.EnumerateObject().OrderBy(property => property.Name, StringComparer.Ordinal))
                {
                    builder.Append(property.Name).Append(':');
                    AppendElement(builder, property.Value);
                    builder.Append(',');
                }

                builder.Append('}');
                break;
            case JsonValueKind.Array:
                builder.Append('[');
                foreach (var item in element.EnumerateArray())
                {
                    AppendElement(builder, item);
                    builder.Append(',');
                }

                builder.Append(']');
                break;
            case JsonValueKind.String:
                builder.Append(JsonSerializer.Serialize(element.GetString()));
                break;
            case JsonValueKind.True:
                builder.Append("true");
                break;
            case JsonValueKind.False:
                builder.Append("false");
                break;
            case JsonValueKind.Null:
                builder.Append("null");
                break;
            default:
                builder.Append(element.GetRawText());
                break;
        }
    }
}
