using System.Globalization;
using ANLAbel.Core.Enums;
using ANLAbel.Core.Models;

namespace ANLAbel.Core.Barcode;

/// <summary>Facts about <see cref="DataMatrixSymbolSize"/> shared by the model, encoder, designer and import.</summary>
public static class DataMatrixSymbolSizes
{
    /// <summary>Rows and columns of a chosen size (its name is C&lt;rows&gt;x&lt;columns&gt;); null for Auto.</summary>
    public static (int Rows, int Columns)? Cells(DataMatrixSymbolSize size)
    {
        if (size == DataMatrixSymbolSize.Auto || !Enum.IsDefined(size))
        {
            return null;
        }

        var parts = size.ToString()[1..].Split('x');
        return (int.Parse(parts[0], CultureInfo.InvariantCulture), int.Parse(parts[1], CultureInfo.InvariantCulture));
    }

    public static bool IsRectangular(DataMatrixSymbolSize size)
        => Cells(size) is { } cells && cells.Rows != cells.Columns;

    /// <summary>
    /// Vendor <c>DatamatrixFormat</c> 1-30 are the same sizes; Auto, the DMRE
    /// sizes (ISO/IEC 21471, not encoded here) and unknown values become Auto.
    /// </summary>
    public static DataMatrixSymbolSize FromVendorFormat(int format)
        => format is >= 1 and <= 30 ? (DataMatrixSymbolSize)format : DataMatrixSymbolSize.Auto;

    /// <summary>The object draws a Data Matrix, so its symbol size applies.</summary>
    public static bool AppliesTo(LabelObject item)
        => item.Type == ObjectType.DataMatrix
            || item.Type == ObjectType.BarcodeCode128 && item.BarcodeSymbology == BarcodeSymbology.DataMatrix;

    /// <summary>A Data Matrix with a chosen size: the size, not the frame or the data, decides its modules.</summary>
    public static bool HasChosenSize(LabelObject item)
        => AppliesTo(item) && item.DataMatrixSymbolSize != DataMatrixSymbolSize.Auto;
}
