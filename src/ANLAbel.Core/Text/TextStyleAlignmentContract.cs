namespace ANLAbel.Core.Text;

/// <summary>
/// Exclusive Excel-style alignment icon selection. Horizontal and vertical
/// groups each keep exactly one on-state; turning an icon on applies that
/// enum value and never invents a different mode.
/// </summary>
public static class TextStyleAlignmentContract
{
    public static bool IsOn<T>(T current, T icon) where T : struct, Enum
        => EqualityComparer<T>.Default.Equals(current, icon);

    public static bool IsOn(Enum? current, Enum? icon)
        => current is not null && icon is not null && current.Equals(icon);

    public static T Apply<T>(T current, T icon, bool turnOn) where T : struct, Enum
        => turnOn ? icon : current;
}
