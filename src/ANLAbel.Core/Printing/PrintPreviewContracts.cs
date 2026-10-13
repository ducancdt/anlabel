namespace ANLAbel.Core.Printing;

/// <summary>
/// Trust is explicit preview-session state. A bitmap or a successful content
/// render alone never means that the selected printer contract is ready.
/// </summary>
public enum PreviewTrustLevel
{
    DesignOnly,
    EffectiveUnverified,
    EffectiveBlocked,
    EffectiveReady,
    Stale
}

/// <summary>
/// One selected source record and its requested label copies. RecordId is a
/// session-stable identity; SourceRowNumber is the operator-facing Excel row.
/// </summary>
public sealed record PrintRecordQuantity(
    string RecordId,
    int SourceRowNumber,
    int Copies);

/// <summary>
/// Deliberately small product scope: selected Excel/CSV records and copies per
/// record. Label sets, skipped slots, unlimited mode and multi-label sheets are
/// not part of this contract.
/// </summary>
public sealed record QuantityPlan(IReadOnlyList<PrintRecordQuantity> Records)
{
    public const int MaximumCopiesPerRecord = 999;
    public const int MaximumPreparedLabelCount = 100_000;

    public int SelectedRecordCount => Records.Count(record => record.Copies > 0);

    public long TotalLabelCount => Records.Sum(record => (long)Math.Max(0, record.Copies));
}

public sealed record PrintIntent(QuantityPlan Quantity);

/// <summary>
/// Stable identity for one label in the prepared sequence. CopyOrdinal is
/// one-based within the source record; SequenceOrdinal is one-based globally.
/// </summary>
public sealed record LabelInstance(
    string InstanceId,
    string RecordId,
    int SourceRowNumber,
    int CopyOrdinal,
    int SequenceOrdinal);

/// <summary>
/// Integer identity of one prepared label. Field values never belong here.
/// </summary>
public readonly record struct PrintDispatchCopy(int SourceRowNumber, int CopyOrdinal, int SequenceOrdinal);

/// <summary>
/// Current product layout is one label per thermal feed/page. The descriptor
/// names that scope explicitly without introducing sheet/slot semantics.
/// </summary>
public sealed record PreviewPageDescriptor(
    int LabelNumber,
    double LabelWidthMm,
    double LabelHeightMm,
    LabelInstance Instance);

public static class PrintSequenceBuilder
{
    public static IReadOnlyList<LabelInstance> Build(PrintIntent intent)
    {
        ArgumentNullException.ThrowIfNull(intent);
        ArgumentNullException.ThrowIfNull(intent.Quantity);
        ArgumentNullException.ThrowIfNull(intent.Quantity.Records);

        Validate(intent.Quantity.Records);

        if (intent.Quantity.TotalLabelCount > QuantityPlan.MaximumPreparedLabelCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(intent),
                $"A prepared print session cannot exceed {QuantityPlan.MaximumPreparedLabelCount:n0} labels.");
        }

        var sequence = new List<LabelInstance>((int)intent.Quantity.TotalLabelCount);
        foreach (var record in intent.Quantity.Records)
        {
            for (var copyOrdinal = 1; copyOrdinal <= record.Copies; copyOrdinal++)
            {
                var sequenceOrdinal = sequence.Count + 1;
                sequence.Add(new LabelInstance(
                    CreateInstanceId(record.RecordId, record.SourceRowNumber, copyOrdinal),
                    record.RecordId,
                    record.SourceRowNumber,
                    copyOrdinal,
                    sequenceOrdinal));
            }
        }

        return sequence;
    }

    public static IReadOnlyList<PreviewPageDescriptor> CreateOneLabelPerFeedPages(
        IReadOnlyList<LabelInstance> sequence,
        double labelWidthMm,
        double labelHeightMm)
    {
        ArgumentNullException.ThrowIfNull(sequence);
        if (!IsFinitePositive(labelWidthMm) || !IsFinitePositive(labelHeightMm))
        {
            throw new ArgumentOutOfRangeException(
                nameof(labelWidthMm),
                "Preview label dimensions must be finite and greater than zero.");
        }

        return sequence.Select((instance, index) => new PreviewPageDescriptor(
            index + 1,
            labelWidthMm,
            labelHeightMm,
            instance)).ToArray();
    }

    private static void Validate(IReadOnlyList<PrintRecordQuantity> records)
    {
        var recordIds = new HashSet<string>(StringComparer.Ordinal);
        var sourceRows = new HashSet<int>();
        foreach (var record in records)
        {
            if (string.IsNullOrWhiteSpace(record.RecordId))
            {
                throw new ArgumentException("Every selected record requires a stable record identity.", nameof(records));
            }

            if (record.SourceRowNumber <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(records), "Excel source row numbers must be greater than zero.");
            }

            if (record.Copies is < 0 or > QuantityPlan.MaximumCopiesPerRecord)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(records),
                    $"Copies per record must stay between 0 and {QuantityPlan.MaximumCopiesPerRecord}.");
            }

            if (!recordIds.Add(record.RecordId))
            {
                throw new ArgumentException($"Duplicate print record identity '{record.RecordId}'.", nameof(records));
            }

            if (!sourceRows.Add(record.SourceRowNumber))
            {
                throw new ArgumentException($"Duplicate source row number {record.SourceRowNumber}.", nameof(records));
            }
        }
    }

    private static string CreateInstanceId(string recordId, int sourceRowNumber, int copyOrdinal)
        => $"{recordId}:row-{sourceRowNumber}:copy-{copyOrdinal}";

    private static bool IsFinitePositive(double value) => double.IsFinite(value) && value > 0;
}
