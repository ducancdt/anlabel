namespace ANLAbel.Core.Enums;

/// <summary>
/// How a picture fills its frame (NiceLabel 10 picture Resize options,
/// vendor <c>GraphicResizeMode</c>).
/// </summary>
public enum PictureSizing
{
    /// <summary>The whole picture, centred, keeping its proportions (vendor Uniform).</summary>
    Proportional = 0,

    /// <summary>The picture is stretched to the frame (vendor Fill, "Resize to the designed size").</summary>
    Stretch = 1,

    /// <summary>
    /// The size its pixels and resolution give it, from the frame's top-left
    /// corner and cut at the frame (vendor None, "Keep original picture size").
    /// </summary>
    Original = 2
}
