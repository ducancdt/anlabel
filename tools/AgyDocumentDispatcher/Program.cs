namespace AgyDocumentDispatcher;

public static class Program
{
    public static int Main(string[] args)
    {
        var repo = Directory.GetCurrentDirectory();
        var maxCycles = 4;
        var dryRun = true;
        string? agyPath = null;
        string? startAt = null;
        string? printTimeout = null;

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            switch (arg)
            {
                case "--dry-run":
                    dryRun = true;
                    break;
                case "--live":
                    dryRun = false;
                    break;
                case "--max-cycles":
                    maxCycles = int.Parse(RequireValue(args, ref i, arg), System.Globalization.CultureInfo.InvariantCulture);
                    break;
                case "--repo":
                    repo = RequireValue(args, ref i, arg);
                    break;
                case "--agy":
                    agyPath = RequireValue(args, ref i, arg);
                    break;
                case "--start-at":
                    startAt = RequireValue(args, ref i, arg);
                    break;
                case "--print-timeout":
                    printTimeout = RequireValue(args, ref i, arg);
                    break;
                case "--help":
                case "-h":
                    Console.WriteLine("AgyDocumentDispatcher — dispatch document-derived work to agy (Gemini) in this git tree.");
                    Console.WriteLine("  --dry-run          Assemble argv + assignment without starting Gemini (default).");
                    Console.WriteLine("  --live             Run agy --print for each cycle.");
                    Console.WriteLine("  --max-cycles N     Stop after N assignments (default 4).");
                    Console.WriteLine("  --repo PATH        Repository root (default: current directory).");
                    Console.WriteLine("  --agy PATH         agy.exe path.");
                    Console.WriteLine("  --start-at TEXT    Skip open items until one whose verbatim contains TEXT.");
                    Console.WriteLine("  --print-timeout D  Forward agy --print-timeout (e.g. 20m). agy default is 5m.");
                    return 0;
                default:
                    Console.Error.WriteLine("Unknown argument: " + arg);
                    return 2;
            }
        }

        var options = new DispatchOptions(
            RepoRoot: repo,
            MaxCycles: maxCycles,
            DryRun: dryRun,
            AgyPath: agyPath,
            StartAt: startAt,
            PrintTimeout: printTimeout);
        var cycles = AgyDispatchLoop.Run(options);
        Console.Write(AgyDispatchLoop.FormatCycles(cycles));
        if (cycles.Count == 0)
        {
            Console.WriteLine("no_open_items=true");
        }

        return 0;
    }

    private static string RequireValue(string[] args, ref int index, string flag)
    {
        if (index + 1 >= args.Length)
        {
            throw new ArgumentException("Missing value for " + flag);
        }

        index++;
        return args[index];
    }
}
