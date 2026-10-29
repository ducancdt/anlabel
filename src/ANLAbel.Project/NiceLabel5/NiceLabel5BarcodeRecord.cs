namespace ANLAbel.Project.NiceLabel5;

/// <summary>
/// One barcode object record decoded from a NiceLabel 5 <c>.lbl</c> file.
/// Geometry is the authored object rectangle in 1/100 mm taken from the
/// four stored long fields, cross-checked against the stored inch
/// doubles (evidence: docs/NICELABEL_5_DEEP_REVERSE_ENGINEERING.md
/// §48-52). Only records whose data is a literal value are reported;
/// variable-linked records carry a <c>?</c> mask instead of data and
/// are not converted yet.
/// </summary>
public sealed record NiceLabel5BarcodeRecord(
    int InternalSymbologyId,
    string SymbologyName,
    string Data,
    int LeftHundredthsMm,
    int TopHundredthsMm,
    int RightHundredthsMm,
    int BottomHundredthsMm,
    string? VariableName = null);
