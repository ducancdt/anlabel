using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using ANLAbel.Core.Models;

namespace ANLAbel.App.ViewModels;

/// <summary>
/// Keeps each embedded image once for the undo history. History snapshots
/// store a short content key in place of the base64 data, so 200 undo steps
/// of a label with a 1 MB logo no longer hold 200 copies of it (plan 13 H29).
/// Keys are resolved back to the full data whenever a snapshot is read, and
/// the pool lives as long as the history that refers to it.
/// </summary>
internal sealed class ImageHistoryPool
{
    private const string KeyPrefix = "anlabel-history-image-sha256:";
    private const int MinimumPooledLength = 1024;
    private readonly Dictionary<string, string> _images = new(StringComparer.Ordinal);
    private readonly ConditionalWeakTable<string, string> _keys = new();
    private readonly object _gate = new();

    public int Count
    {
        get
        {
            lock (_gate)
            {
                return _images.Count;
            }
        }
    }

    /// <summary>JSON options for history snapshots: LabelObject images become pool keys.</summary>
    public JsonSerializerOptions CreateSnapshotOptions(JsonSerializerOptions template)
    {
        var options = new JsonSerializerOptions(template)
        {
            TypeInfoResolver = new DefaultJsonTypeInfoResolver
            {
                Modifiers = { PoolImages }
            }
        };
        return options;
    }

    public string? ToKey(string? imageData)
    {
        if (string.IsNullOrEmpty(imageData) || imageData.Length < MinimumPooledLength)
        {
            return imageData;
        }

        lock (_gate)
        {
            if (_keys.TryGetValue(imageData, out var known))
            {
                return known;
            }

            var key = KeyPrefix + Convert.ToHexString(SHA256.HashData(Encoding.ASCII.GetBytes(imageData)));
            _images.TryAdd(key, imageData);
            _keys.AddOrUpdate(imageData, key);
            return key;
        }
    }

    public string? FromKey(string? value)
    {
        if (value is null || !value.StartsWith(KeyPrefix, StringComparison.Ordinal))
        {
            return value;
        }

        lock (_gate)
        {
            return _images.TryGetValue(value, out var imageData)
                ? imageData
                : throw new InvalidOperationException("An undo snapshot refers to an image that is no longer in the history pool.");
        }
    }

    private void PoolImages(JsonTypeInfo typeInfo)
    {
        if (typeInfo.Type != typeof(LabelObject))
        {
            return;
        }

        var property = typeInfo.Properties.FirstOrDefault(candidate =>
            string.Equals(candidate.Name, nameof(LabelObject.ImageDataBase64), StringComparison.Ordinal));
        if (property?.Get is not { } get || property.Set is not { } set)
        {
            return;
        }

        property.Get = owner => ToKey((string?)get(owner));
        property.Set = (owner, value) => set(owner, FromKey((string?)value));
    }
}
