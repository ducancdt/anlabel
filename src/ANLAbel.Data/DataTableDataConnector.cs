using System.Collections.Immutable;
using System.Data;
using ANLAbel.Core.Data;

namespace ANLAbel.Data;

/// <summary>
/// Compatibility adapter that exposes the current Excel/CSV <see cref="DataTable"/>
/// result through the typed connector contract. It is intentionally read-only:
/// existing import/binding behavior remains the source of truth while R4 moves
/// consumers away from mutable <see cref="DataRow"/> instances.
/// </summary>
public sealed class DataTableDataConnector : IDataConnector
{
    private readonly ImmutableArray<DataFieldSchema> _schema;
    private readonly ImmutableArray<DataRecord> _records;

    public DataTableDataConnector(DataConnectorDescriptor descriptor, DataTable table)
    {
        Descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
        ArgumentNullException.ThrowIfNull(table);

        // The import UI may keep its DataTable for preview/editing. Capture a value
        // snapshot here so pages consumed by print/binding have deterministic
        // contents for the connector lifetime and never leak mutable DataRow state.
        var columns = table.Columns.Cast<DataColumn>().ToArray();
        _schema = columns
            .Select(column => new DataFieldSchema(
                column.ColumnName,
                column.Caption.Length == 0 ? column.ColumnName : column.Caption,
                ResolveKind(column.DataType),
                column.AllowDBNull,
                column.ColumnName))
            .ToImmutableArray();
        _records = table.Rows.Cast<DataRow>()
            .Select(row => DataRecord.Create(columns.Select(column =>
                new KeyValuePair<string, string?>(
                    column.ColumnName,
                    row.IsNull(column) ? null : Convert.ToString(row[column], System.Globalization.CultureInfo.InvariantCulture)))))
            .ToImmutableArray();
    }

    public DataConnectorDescriptor Descriptor { get; }

    public Task<DataPage> ReadPageAsync(DataReadRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var offset = request.ResolveOffset();
        var rows = _records
            .Skip(offset)
            .Take(request.NormalizedLimit)
            .ToImmutableArray();
        var nextOffset = offset + rows.Length;
        var complete = nextOffset >= _records.Length;
        return Task.FromResult(new DataPage(_schema, rows, complete ? null : nextOffset.ToString(System.Globalization.CultureInfo.InvariantCulture), complete));
    }

    private static DataValueKind ResolveKind(Type type)
    {
        if (type == typeof(bool)) return DataValueKind.Boolean;
        if (type == typeof(DateTime)) return DataValueKind.DateTime;
        if (type == typeof(decimal) || type == typeof(double) || type == typeof(float)) return DataValueKind.Decimal;
        if (type == typeof(byte) || type == typeof(short) || type == typeof(int) || type == typeof(long)) return DataValueKind.Integer;
        return DataValueKind.Text;
    }
}
