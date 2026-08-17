namespace ANLAbel.Core.Enums;

/// <summary>
/// Base direction used by the shared text layout policy.  Auto follows the
/// first strong Unicode letter in the value; it does not reverse the stored
/// string and still lets WPF resolve mixed bidi runs according to UAX #9.
/// </summary>
public enum TextDirectionMode
{
    Auto,
    LeftToRight,
    RightToLeft
}
