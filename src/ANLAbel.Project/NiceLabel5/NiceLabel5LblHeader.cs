namespace ANLAbel.Project.NiceLabel5;

/// <summary>
/// Header facts read from a NiceLabel 5 <c>.lbl</c> file. This is a foundation
/// reader only: it validates the compound-file container and the
/// <c>Contents</c> header (magic, format version, label size) and lists the
/// embedded streams. It deliberately does not convert label objects yet;
/// layouts that do not match the verified NiceLabel 5 version 29 fixed-header
/// shape are rejected instead of guessed.
/// </summary>
public sealed record NiceLabel5LblHeader(
    int FormatVersion,
    int LabelWidthHundredthsMm,
    int LabelHeightHundredthsMm,
    int ThirdDimensionHundredthsMm,
    IReadOnlyList<string> StreamNames);
