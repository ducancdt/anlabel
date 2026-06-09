namespace ANLAbel.Core.Barcode;

public interface IQrCapacityProvider
{
    bool CanEncodeByteMode(string data, int version, QrErrorCorrection errorCorrection);
    int GetByteModeCapacity(int version, QrErrorCorrection errorCorrection);
}