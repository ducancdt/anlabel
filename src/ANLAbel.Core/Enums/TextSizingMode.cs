namespace ANLAbel.Core.Enums;

/// <summary>
/// Defines layout remediation inside a persisted text frame.
/// ObjectType.Text: <see cref="AutoFit"/> grows the selection with content
/// (NiceLabel Text default). <see cref="FixedFrame"/> on Text means the user
/// locked the selection by border-drag — glyphs compress into that frame via
/// shared layout scale (ANLAbel WYSIWYG); it is still not TextBox wrap/clip
/// ownership. ObjectType.TextBox owns a user-authored Width/Height frame:
/// FixedFrame wraps at the authored font, ShrinkFont fits by point size, and
/// ScaleWidth fits by horizontal scaling. None of these modes may rewrite the
/// TextBox frame from its content.
/// </summary>
public enum TextSizingMode
{
    AutoFit,
    FixedFrame,
    ShrinkFont,
    ScaleWidth,
    // Compatibility only for files saved by the abandoned development build.
    // Runtime/UI normalize this to FixedFrame; it must never resize TextBox.
    AdjustHeight
}
