namespace ANLAbel.Core.Barcode;

public static class QrVersionHelper
{
    public const int MinVersion = 1;
    public const int MaxVersion = 40;

    public static int GetModuleCount(int version)
    {
        ValidateVersion(version);
        return 21 + (version - 1) * 4;
    }

    public static void ValidateVersion(int version)
    {
        if (version is < MinVersion or > MaxVersion)
        {
            throw new ArgumentOutOfRangeException(nameof(version), version, "QR version must be from 1 to 40.");
        }
    }

    public static bool IsValidVersion(int version) => version is >= MinVersion and <= MaxVersion;
}