using System.Collections.Immutable;

namespace ANLAbel.Core.Barcode;

/// <summary>
/// Boundary rule for a concatenated GS1 element string. This is intentionally
/// not a fixed-vs-variable data-length flag: a fixed-length AI can still need a
/// separator unless it belongs to GS1's immutable pre-defined-length table.
/// </summary>
public enum Gs1AiBoundaryKind
{
    PredefinedLength,
    SeparatorRequired
}

/// <summary>
/// One maintained GS1 AI family. <c>x</c> in <see cref="Pattern"/> matches one
/// digit, allowing fixed-boundary families such as 31xx to be represented once.
/// Value semantic checks live in <see cref="BarcodeApplicationContract"/>;
/// this registry owns the crucial element-string boundary decision.
/// </summary>
public sealed record Gs1AiDefinition(
    string Pattern,
    Gs1AiBoundaryKind BoundaryKind,
    string? ValuePattern = null);

/// <summary>
/// Versioned application-identifier registry shipped by ANLAbel. This is an
/// explicitly curated industrial subset, not an assertion of complete GS1
/// General Specifications coverage.
/// </summary>
public static class Gs1AiRegistry
{
    public const string Version = "ANL-industrial-subset-2026.08-p6";

    private static readonly ImmutableArray<Gs1AiDefinition> Definitions =
    [
        new("00", Gs1AiBoundaryKind.PredefinedLength), new("01", Gs1AiBoundaryKind.PredefinedLength), new("02", Gs1AiBoundaryKind.PredefinedLength),
        new("10", Gs1AiBoundaryKind.SeparatorRequired), new("11", Gs1AiBoundaryKind.PredefinedLength), new("12", Gs1AiBoundaryKind.PredefinedLength),
        new("13", Gs1AiBoundaryKind.PredefinedLength), new("15", Gs1AiBoundaryKind.PredefinedLength), new("16", Gs1AiBoundaryKind.PredefinedLength),
        new("17", Gs1AiBoundaryKind.PredefinedLength), new("20", Gs1AiBoundaryKind.SeparatorRequired), new("21", Gs1AiBoundaryKind.SeparatorRequired),
        new("22", Gs1AiBoundaryKind.SeparatorRequired), new("30", Gs1AiBoundaryKind.SeparatorRequired), new("37", Gs1AiBoundaryKind.SeparatorRequired),
        new("240", Gs1AiBoundaryKind.SeparatorRequired), new("241", Gs1AiBoundaryKind.SeparatorRequired), new("250", Gs1AiBoundaryKind.SeparatorRequired),
        new("251", Gs1AiBoundaryKind.SeparatorRequired),
        new("400", Gs1AiBoundaryKind.SeparatorRequired), new("401", Gs1AiBoundaryKind.SeparatorRequired), new("402", Gs1AiBoundaryKind.SeparatorRequired),
        new("403", Gs1AiBoundaryKind.SeparatorRequired), new("410", Gs1AiBoundaryKind.PredefinedLength), new("411", Gs1AiBoundaryKind.PredefinedLength),
        new("412", Gs1AiBoundaryKind.PredefinedLength), new("413", Gs1AiBoundaryKind.PredefinedLength), new("414", Gs1AiBoundaryKind.PredefinedLength),
        new("415", Gs1AiBoundaryKind.PredefinedLength), new("416", Gs1AiBoundaryKind.PredefinedLength), new("417", Gs1AiBoundaryKind.PredefinedLength),
        new("420", Gs1AiBoundaryKind.SeparatorRequired), new("421", Gs1AiBoundaryKind.SeparatorRequired), new("422", Gs1AiBoundaryKind.SeparatorRequired),
        new("423", Gs1AiBoundaryKind.SeparatorRequired), new("424", Gs1AiBoundaryKind.SeparatorRequired), new("425", Gs1AiBoundaryKind.SeparatorRequired),
        new("426", Gs1AiBoundaryKind.SeparatorRequired), new("427", Gs1AiBoundaryKind.SeparatorRequired),
        new("31xx", Gs1AiBoundaryKind.PredefinedLength), new("32xx", Gs1AiBoundaryKind.PredefinedLength), new("33xx", Gs1AiBoundaryKind.PredefinedLength),
        new("34xx", Gs1AiBoundaryKind.PredefinedLength), new("35xx", Gs1AiBoundaryKind.PredefinedLength), new("36xx", Gs1AiBoundaryKind.PredefinedLength),
        new("390x", Gs1AiBoundaryKind.SeparatorRequired), new("391x", Gs1AiBoundaryKind.SeparatorRequired),
        new("392x", Gs1AiBoundaryKind.SeparatorRequired), new("393x", Gs1AiBoundaryKind.SeparatorRequired),
        new("7001", Gs1AiBoundaryKind.SeparatorRequired), new("7002", Gs1AiBoundaryKind.SeparatorRequired),
        new("7003", Gs1AiBoundaryKind.SeparatorRequired), new("7004", Gs1AiBoundaryKind.SeparatorRequired),
        new("8003", Gs1AiBoundaryKind.SeparatorRequired), new("8004", Gs1AiBoundaryKind.SeparatorRequired),
        new("8006", Gs1AiBoundaryKind.SeparatorRequired), new("8018", Gs1AiBoundaryKind.SeparatorRequired),
        new("8019", Gs1AiBoundaryKind.SeparatorRequired),
        new("9x", Gs1AiBoundaryKind.SeparatorRequired)
    ];

    public static IReadOnlyList<Gs1AiDefinition> SupportedDefinitions => Definitions;

    public static bool TryGetDefinition(string applicationIdentifier, out Gs1AiDefinition definition)
    {
        definition = Definitions.FirstOrDefault(candidate => Matches(candidate.Pattern, applicationIdentifier))!;
        return definition is not null
            || Gs1OfficialRegistryBundle.Load().TryGetDefinition(applicationIdentifier, out definition!);
    }

    private static bool Matches(string pattern, string value)
    {
        if (pattern.Length != value.Length)
        {
            return false;
        }

        for (var index = 0; index < pattern.Length; index++)
        {
            if (pattern[index] != 'x' && pattern[index] != value[index])
            {
                return false;
            }

            if (pattern[index] == 'x' && !char.IsDigit(value[index]))
            {
                return false;
            }
        }

        return true;
    }
}
