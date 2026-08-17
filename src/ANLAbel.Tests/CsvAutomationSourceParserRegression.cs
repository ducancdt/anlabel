using System.Text;
using ANLAbel.Data.Automation;

internal static class CsvAutomationSourceParserRegression
{
    public static Task Run()
    {
        using var source = new MemoryStream(Encoding.UTF8.GetBytes("Code,Name\nA,One\nB,\"Two, Inc.\"\nC\n"));
        var result = CsvAutomationSourceParser.Parse(source);
        Require(result.Records.Count == 2 && result.Records[1].Values["Name"] == "Two, Inc.", "Header CSV parser must preserve quoted values.");
        Require(result.Diagnostics.Single().Contains("row 4", StringComparison.OrdinalIgnoreCase), "Malformed field count must retain its source-row diagnostic.");
        using var duplicate = new MemoryStream(Encoding.UTF8.GetBytes("Code,code\nA,B\n"));
        Require(CsvAutomationSourceParser.Parse(duplicate).Diagnostics.Single().Contains("duplicate", StringComparison.OrdinalIgnoreCase), "Duplicate headers must fail closed.");
        return Task.CompletedTask;
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
