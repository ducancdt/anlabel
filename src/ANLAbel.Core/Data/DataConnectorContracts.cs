using System.Collections.Immutable;

namespace ANLAbel.Core.Data;

/// <summary>Stable, UI-free identity of one local or external data connector.</summary>
public sealed record DataConnectorDescriptor(
    string Id,
    string DisplayName,
    string Kind,
    bool SupportsPaging,
    bool SupportsRefresh);

public enum DataValueKind
{
    Text,
    Integer,
    Decimal,
    Boolean,
    Date,
    DateTime
}

public sealed record DataFieldSchema(
    string Name,
    string DisplayName,
    DataValueKind ValueKind,
    bool IsNullable,
    string SourceName);

/// <summary>One value-only record safe to bind, preview and hash without DataRow/WPF types.</summary>
public sealed record DataRecord(ImmutableDictionary<string, string?> Values)
{
    public static DataRecord Create(IEnumerable<KeyValuePair<string, string?>> values)
        => new(values.ToImmutableDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase));

    public bool TryGetValue(string fieldName, out string? value)
        => Values.TryGetValue(fieldName, out value);
}

public sealed record DataReadRequest(
    int Offset = 0,
    int Limit = 100,
    string? ContinuationToken = null)
{
    public int NormalizedOffset => Math.Max(0, Offset);
    public int NormalizedLimit => Math.Clamp(Limit, 1, 10_000);

    public int ResolveOffset()
    {
        if (string.IsNullOrWhiteSpace(ContinuationToken))
        {
            return NormalizedOffset;
        }

        if (!int.TryParse(ContinuationToken, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var offset)
            || offset < 0)
        {
            throw new ArgumentException("Continuation token is invalid.", nameof(ContinuationToken));
        }

        return offset;
    }
}

public sealed record DataPage(
    ImmutableArray<DataFieldSchema> Schema,
    ImmutableArray<DataRecord> Records,
    string? ContinuationToken,
    bool IsComplete)
{
    public static DataPage Empty { get; } = new(
        ImmutableArray<DataFieldSchema>.Empty,
        ImmutableArray<DataRecord>.Empty,
        null,
        true);
}

/// <summary>
/// Future connectors (Excel, CSV, database or HTTP) implement this async,
/// cancellation-aware contract. It deliberately contains no secret values;
/// connector-specific credentials are resolved outside the document model.
/// </summary>
public interface IDataConnector
{
    DataConnectorDescriptor Descriptor { get; }

    Task<DataPage> ReadPageAsync(DataReadRequest request, CancellationToken cancellationToken = default);
}
