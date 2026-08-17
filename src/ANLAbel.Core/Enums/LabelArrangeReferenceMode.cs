namespace ANLAbel.Core.Enums;

/// <summary>
/// Reference frame for an align command. Selection bounds is the familiar
/// frame-align behavior; key-object preserves the explicitly selected primary;
/// canvas aligns to the label artboard.
/// </summary>
public enum LabelArrangeReferenceMode
{
    SelectionBounds,
    KeyObject,
    Canvas
}
