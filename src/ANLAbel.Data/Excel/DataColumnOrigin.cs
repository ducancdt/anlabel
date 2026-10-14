using System.Data;

namespace ANLAbel.Data.Excel;

public enum DataHeaderKind
{
    Unique,
    Blank,
    Repeated
}

/// <summary>
/// Where an imported column came from (CORE_SOFTWARE_STANDARD DB03). A blank or
/// repeated header gets a generated name (<c>Column3</c>, <c>Part No_2</c>) that
/// only says where the column sits, so a binding to it can read another column
/// once the file gains, loses or reorders columns.
/// </summary>
public sealed record DataColumnOrigin(string FieldName, int SourceColumn, string SourceHeader, DataHeaderKind Kind)
{
    private const string PropertyKey = "ANLAbel.DataColumnOrigin";

    /// <summary>Spreadsheet letter of <see cref="SourceColumn"/> (1 = A, 27 = AA).</summary>
    public string ColumnLetter => ToColumnLetter(SourceColumn);

    /// <summary>True when only the column's position identifies it.</summary>
    public bool DependsOnPosition => Kind != DataHeaderKind.Unique;

    public static DataColumnOrigin? Of(DataColumn column)
    {
        ArgumentNullException.ThrowIfNull(column);
        return column.ExtendedProperties[PropertyKey] as DataColumnOrigin;
    }

    /// <summary>Origins of every imported column, in column order; empty for other tables.</summary>
    public static IReadOnlyList<DataColumnOrigin> Describe(DataTable? table)
        => table is null
            ? Array.Empty<DataColumnOrigin>()
            : table.Columns.Cast<DataColumn>().Select(Of).OfType<DataColumnOrigin>().ToArray();

    public static string ToColumnLetter(int oneBasedColumn)
    {
        var letters = string.Empty;
        for (var value = oneBasedColumn; value > 0; value = (value - 1) / 26)
        {
            letters = (char)('A' + (value - 1) % 26) + letters;
        }

        return letters;
    }

    internal static void Attach(DataColumn column, DataColumnOrigin origin)
        => column.ExtendedProperties[PropertyKey] = origin;
}
