namespace ANLAbel.Project.NiceLabel5;

/// <summary>
/// One fixed text object record decoded from a NiceLabel 5 <c>.lbl</c>
/// file: literal content (no variable mask), the stored font face name
/// and the authored rectangle in 1/100 mm, cross-checked against the
/// stored inch doubles like barcode records (evidence:
/// docs/NICELABEL_5_DEEP_REVERSE_ENGINEERING.md §48, §60).
/// </summary>
public sealed record NiceLabel5TextRecord(
    string Content,
    string FontFamily,
    int LeftHundredthsMm,
    int TopHundredthsMm,
    int RightHundredthsMm,
    int BottomHundredthsMm,
    string? VariableName = null,
    double FontSizePt = 0,
    bool IsBold = false,
    NiceLabel5HorizontalAnchor Anchor = NiceLabel5HorizontalAnchor.Left);

/// <summary>
/// Horizontal anchor of a stored object rectangle: the first stored
/// double equals the left edge, the horizontal centre or the right
/// edge of the long rectangle, matching the object's text alignment
/// (evidence: docs/NICELABEL_5_DEEP_REVERSE_ENGINEERING.md §76).
/// </summary>
public enum NiceLabel5HorizontalAnchor
{
    Left,
    Center,
    Right
}
