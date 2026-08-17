namespace ANLAbel.Core.Enums;

/// <summary>
/// Explicit behavior when a bounded text object does not fit its authored
/// TextBox frame. Error is the safe production default; Clip is an intentional
/// visual clip without a blocking diagnostic; Ellipsis keeps bounded content
/// readable by shortening the final visible line. AllowOverflow is retained
/// only for project-file compatibility and is resolved to Error for TextBox;
/// free-flowing behavior belongs exclusively to ObjectType.Text.
/// </summary>
public enum TextOverflowMode
{
    Error,
    Clip,
    Ellipsis,
    AllowOverflow
}
