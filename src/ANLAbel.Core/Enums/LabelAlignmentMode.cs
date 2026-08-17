namespace ANLAbel.Core.Enums;

/// <summary>
/// Alignment of an object's layout bounds along one axis.  The names are
/// intentionally axis-aware so a caller cannot confuse horizontal center with
/// vertical middle when constructing an arrange command.
/// </summary>
public enum LabelAlignmentMode
{
    Left,
    HorizontalCenter,
    Right,
    Top,
    VerticalCenter,
    Bottom
}
