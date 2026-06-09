namespace ANLAbel.Barcode.Renderers;

/// <summary>
/// Holds the raw bit matrix data for vector barcode rendering.
/// Each boolean represents whether a column/module is dark (true) or light (false).
/// For 1D barcodes: WidthModules = number of columns, HeightModules = 1.
/// </summary>
public sealed class BarcodeVectorData
{
    public BarcodeVectorData(int widthModules, int heightModules, bool[] rowBits)
    {
        WidthModules = widthModules;
        HeightModules = heightModules;
        RowBits = rowBits;
    }

    public int WidthModules { get; }
    public int HeightModules { get; }
    public bool[] RowBits { get; }
}