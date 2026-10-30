namespace ANLAbel.Core.Enums;

/// <summary>
/// Data Matrix (ECC 200) symbol size as rows x columns (NiceLabel 10 Data
/// Matrix format, vendor <c>DatamatrixFormat</c>, same values). Auto is the
/// smallest square symbol that holds the data; a chosen size is that size or,
/// when the data does not fit, an error.
/// </summary>
public enum DataMatrixSymbolSize
{
    Auto = 0,
    C10x10 = 1,
    C12x12 = 2,
    C14x14 = 3,
    C16x16 = 4,
    C18x18 = 5,
    C20x20 = 6,
    C22x22 = 7,
    C24x24 = 8,
    C26x26 = 9,
    C32x32 = 10,
    C36x36 = 11,
    C40x40 = 12,
    C44x44 = 13,
    C48x48 = 14,
    C52x52 = 15,
    C64x64 = 16,
    C72x72 = 17,
    C80x80 = 18,
    C88x88 = 19,
    C96x96 = 20,
    C104x104 = 21,
    C120x120 = 22,
    C132x132 = 23,
    C144x144 = 24,
    C8x18 = 25,
    C8x32 = 26,
    C12x26 = 27,
    C12x36 = 28,
    C16x36 = 29,
    C16x48 = 30
}
