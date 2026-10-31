namespace ANLAbel.App.ViewModels;

/// <summary>What a choice on the Source page connects the object to.</summary>
public enum DataSourceChoiceKind
{
    /// <summary>The object prints its own text (NiceLabel 10 "Fixed data").</summary>
    Fixed,

    /// <summary>A prompt, a column of the data, a function or a value given at print time.</summary>
    Source,

    /// <summary>Text and several sources together, built with the composition editor.</summary>
    Compose
}

/// <summary>
/// One entry of the Source page's "Nguồn dữ liệu" list (NiceLabel 10 "Connected data
/// source"), in the Data panel's groups, with the binding the object gets.
/// </summary>
public sealed record DataSourceChoice(string Group, string Label, string Binding, DataSourceChoiceKind Kind)
{
    public static DataSourceChoice Fixed { get; } = new("Cố định", "Chữ cố định", string.Empty, DataSourceChoiceKind.Fixed);

    public static DataSourceChoice Compose { get; } = new("Ghép nhiều nguồn", "Ghép chữ / công thức…", string.Empty, DataSourceChoiceKind.Compose);
}
