namespace ANLAbel.Core.Enums;

/// <summary>
/// The handle that owns a resize gesture.  Keeping the handle in the
/// document contract lets the WPF adorner and any future editor apply the
/// same aspect-lock and centre-anchor rules.
/// </summary>
public enum ResizeHandle
{
    None,
    TopLeft,
    Top,
    TopRight,
    Right,
    BottomRight,
    Bottom,
    BottomLeft,
    Left
}
