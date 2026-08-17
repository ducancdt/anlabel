using System.Security.Cryptography;
using System.Text;
using ANLAbel.Core.Data;

namespace ANLAbel.Core.Automation;

/// <summary>
/// Immutable, payload-free identity for a prepared local automation batch.
/// It binds the claimed source and trigger configuration to the exact template
/// snapshot and ordered record values used by any later preflight stage.
/// </summary>
public sealed record FileDropPreparedBatchIdentity(
    string PreparedBatchId,
    string EventId,
    string TriggerId,
    string ConfigurationFingerprint,
    string SourceFingerprint,
    string TemplateFingerprint,
    string DataFingerprint,
    int RecordCount);

public static class FileDropPreparedBatchContract
{
    public static FileDropPreparedBatchIdentity Create(
        FileDropEventIdentity eventIdentity,
        string templateFingerprint,
        IReadOnlyList<DataRecord> records)
    {
        ArgumentNullException.ThrowIfNull(eventIdentity);
        ArgumentNullException.ThrowIfNull(records);
        var expectedEvent = FileDropClaimContract.CreateIdentity(
            eventIdentity.TriggerId,
            eventIdentity.ConfigurationFingerprint,
            eventIdentity.SourceFingerprint);
        if (!string.Equals(eventIdentity.EventId, expectedEvent.EventId, StringComparison.Ordinal))
            throw new ArgumentException("Prepared batches require a valid claimed event identity.", nameof(eventIdentity));
        if (string.IsNullOrWhiteSpace(templateFingerprint))
            throw new ArgumentException("A template fingerprint is required.", nameof(templateFingerprint));

        var dataFingerprint = ComputeDataFingerprint(records);
        var canonical = string.Join("|",
            eventIdentity.EventId,
            eventIdentity.TriggerId,
            eventIdentity.ConfigurationFingerprint,
            eventIdentity.SourceFingerprint,
            templateFingerprint.Trim(),
            dataFingerprint,
            records.Count.ToString(System.Globalization.CultureInfo.InvariantCulture));
        return new FileDropPreparedBatchIdentity(
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical))),
            eventIdentity.EventId,
            eventIdentity.TriggerId,
            eventIdentity.ConfigurationFingerprint,
            eventIdentity.SourceFingerprint,
            templateFingerprint.Trim(),
            dataFingerprint,
            records.Count);
    }

    public static string ComputeDataFingerprint(IReadOnlyList<DataRecord> records)
    {
        ArgumentNullException.ThrowIfNull(records);
        var canonical = new StringBuilder();
        canonical.Append("records:").Append(records.Count).Append(';');
        for (var rowIndex = 0; rowIndex < records.Count; rowIndex++)
        {
            var record = records[rowIndex] ?? throw new ArgumentException("Prepared records cannot contain null rows.", nameof(records));
            canonical.Append("row:").Append(rowIndex).Append(':').Append(record.Values.Count).Append(';');
            foreach (var pair in record.Values.OrderBy(item => item.Key, StringComparer.OrdinalIgnoreCase))
            {
                AppendValue(canonical, pair.Key.ToUpperInvariant());
                canonical.Append(pair.Value is null ? "null;" : "text;");
                if (pair.Value is not null) AppendValue(canonical, pair.Value);
            }
        }
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical.ToString())));
    }

    private static void AppendValue(StringBuilder builder, string value) =>
        builder.Append(value.Length).Append(':').Append(value).Append(';');
}
