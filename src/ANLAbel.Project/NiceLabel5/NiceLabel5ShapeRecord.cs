namespace ANLAbel.Project.NiceLabel5;

/// <summary>
/// One rectangle or line object record decoded from a NiceLabel 5
/// <c>.lbl</c> file, identified by the stored object type code 0x131
/// in front of the shared geometry (evidence:
/// docs/NICELABEL_5_DEEP_REVERSE_ENGINEERING.md §65). A record whose
/// authored rectangle is thin in one direction (at most 1 mm) is a
/// line; anything else is a rectangle. Fill and line style fields are
/// not converted yet.
/// </summary>
public sealed record NiceLabel5ShapeRecord(
    bool IsLine,
    int LeftHundredthsMm,
    int TopHundredthsMm,
    int RightHundredthsMm,
    int BottomHundredthsMm);
