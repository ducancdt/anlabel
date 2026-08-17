namespace ANLAbel.Core.Enums;

/// <summary>
/// Authored Code 39 wide:narrow ratio. Zero preserves the historical ZXing
/// geometry until an operator explicitly selects a P4 ratio.
/// </summary>
public enum Code39WideNarrowRatio
{
    LegacyEngineDefault = 0,
    Ratio2_0 = 20,
    Ratio2_2 = 22,
    Ratio2_5 = 25,
    Ratio3_0 = 30
}
