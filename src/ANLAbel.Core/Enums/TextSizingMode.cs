namespace ANLAbel.Core.Enums;

/// <summary>
/// Defines layout remediation inside a persisted text frame.
/// ObjectType.Text: <see cref="AutoFit"/> grows the selection with content
/// (NiceLabel Text default). <see cref="FixedFrame"/> on Text means the user
/// locked the selection by border-drag — glyphs compress into that frame via
/// shared layout scale (ANLAbel WYSIWYG); it is still not TextBox wrap/clip
/// ownership. ObjectType.TextBox owns a user-authored Width/Height frame:
/// FixedFrame wraps at the authored font, ShrinkFont fits by point size, and
/// ScaleWidth fits by horizontal scaling. <see cref="AdjustHeight"/> is the
/// NiceLabel 10 framed-text VariableHeight: the TextBox keeps its authored
/// width and its height follows the wrapped content (designer and print).
/// </summary>
public enum TextSizingMode
{
    AutoFit,
    FixedFrame,
    ShrinkFont,
    ScaleWidth,
    // NiceLabel 10 BestFit VariableHeight (owner request 2026-10-29).
    AdjustHeight
}
