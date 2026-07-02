using System.Security.Cryptography;
using System.Text;
using ANLAbel.Core.Licensing;

namespace ANLAbel.LicenseGenerator;

internal static class MasterLicenseSigner
{
    private const string SealedPrivateKey = "AQAAANCMnd8BFdERjHoAwE/Cl+sBAAAACC90xZCqMUiebelQyg8tHAAAAAACAAAAAAAQZgAAAAEAACAAAAAJLHrJFoiqZiJprUVG3x2DTQtPrWCLsTJZx/IpB0laTQAAAAAOgAAAAAIAACAAAAAIokkBdfVcuEBf2lDKfaHGyUDQDrSe+ezN12iNui2f65AAAAD+F6DcoYsJ/DzJFrjY9HtcoU9TpY2IAhoLuf9QaK6edShge0ZnGAc9UpgxDs9o8u6c8+so0tUmg5imdFvtvpWsCpYKv3eRDJLVrt5CwA8gRkws6h2PT0C2GeikQeh41wevf/ZrsOzO4ZNSuyanYuQCl4q8HSeK8YCzw+TAyXB6cimXJOY4OxVUrlpXNKsAmCVAAAAAYFF4uqxGn5t+/+3rhkQykp4WDpBts1DqwgsVUJHvfvJ6cG/BQ/Yg8IklExsj4K4a3FtlUR/jBjxD1364rM1++Q==";
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("ANLAbel.MasterKey.v1");

    public static string Create(string machineId, string customer, DateTimeOffset? expiry)
    {
        var privateKey = ProtectedData.Unprotect(Convert.FromBase64String(SealedPrivateKey), Entropy, DataProtectionScope.CurrentUser);
        try { return ActivationLicense.Create(machineId, customer, expiry, privateKey); }
        finally { CryptographicOperations.ZeroMemory(privateKey); }
    }

    public static bool SelfTest()
    {
        const string machine = "0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF";
        var key = Create(machine, "SELF TEST", null);
        return ActivationLicense.Validate(key, machine).IsValid;
    }
}
