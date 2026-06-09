using System.Text;

namespace ANLAbel.Core.Barcode;

public sealed class QrCapacityTable : IQrCapacityProvider
{
    private static readonly int[] L = [17,32,53,78,106,134,154,192,230,271,321,367,425,458,520,586,644,718,792,858,929,1003,1091,1171,1273,1367,1465,1528,1628,1732,1840,1952,2068,2188,2303,2431,2563,2699,2809,2953];
    private static readonly int[] M = [14,26,42,62,84,106,122,152,180,213,251,287,331,362,412,450,504,560,624,666,711,779,857,911,997,1059,1125,1190,1264,1370,1452,1538,1628,1722,1809,1911,1989,2099,2213,2331];
    private static readonly int[] Q = [11,20,32,46,60,74,86,108,130,151,177,203,241,258,292,322,364,394,442,482,509,565,611,661,715,751,805,868,908,982,1030,1112,1168,1228,1283,1351,1423,1499,1579,1663];
    private static readonly int[] H = [7,14,24,34,44,58,64,84,98,119,137,155,177,194,220,250,280,310,338,382,403,439,461,511,535,593,625,658,698,742,790,842,898,958,983,1051,1093,1139,1219,1273];

    public bool CanEncodeByteMode(string data, int version, QrErrorCorrection errorCorrection)
    {
        var byteCount = Encoding.UTF8.GetByteCount(data ?? string.Empty);
        return byteCount <= GetByteModeCapacity(version, errorCorrection);
    }

    public int GetByteModeCapacity(int version, QrErrorCorrection errorCorrection)
    {
        QrVersionHelper.ValidateVersion(version);
        var index = version - 1;
        return errorCorrection switch
        {
            QrErrorCorrection.L => L[index],
            QrErrorCorrection.M => M[index],
            QrErrorCorrection.Q => Q[index],
            QrErrorCorrection.H => H[index],
            _ => throw new ArgumentOutOfRangeException(nameof(errorCorrection), errorCorrection, null)
        };
    }
}