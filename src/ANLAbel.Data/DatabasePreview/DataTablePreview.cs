namespace ANLAbel.Data.DatabasePreview;

public sealed class DataTablePreview
{
    public IReadOnlyList<string> Headers { get; init; } = Array.Empty<string>();
    public IReadOnlyList<IReadOnlyDictionary<string, string>> Rows { get; init; } = Array.Empty<IReadOnlyDictionary<string, string>>();
}
