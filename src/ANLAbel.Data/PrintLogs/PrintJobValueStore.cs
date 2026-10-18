using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ANLAbel.Core.Printing;

namespace ANLAbel.Data.PrintLogs;

/// <summary>The job time and prompt answers a print job printed with.</summary>
public sealed record PrintJobValues(
    string JobId,
    DateTimeOffset JobTime,
    IReadOnlyDictionary<string, string> PromptValues,
    DateTimeOffset RecordedAtUtc);

/// <summary>
/// Append-only record of each job's job-level values (time and prompts), so a
/// reprint can rebuild exactly the labels the original job printed. Written
/// before the manifest, under a named mutex shared by ANLAbel processes.
/// </summary>
public sealed class PrintJobValueStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly string _filePath;
    private readonly string _mutexName;

    public PrintJobValueStore(string? filePath = null)
    {
        _filePath = filePath ?? AppDataPaths.Local("print-job-values.jsonl");
        var pathHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Path.GetFullPath(_filePath).ToUpperInvariant())))[..16];
        _mutexName = $@"Local\ANLAbel.JobValues.{pathHash}";
    }

    /// <summary>Records a job's values; recording the same job again keeps the first record.</summary>
    public PrintJobValues Record(string jobId, DateTimeOffset jobTime, IReadOnlyDictionary<string, string> promptValues)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jobId);
        ArgumentNullException.ThrowIfNull(promptValues);
        return WithLock(() =>
        {
            var existing = ReadAll().LastOrDefault(item => string.Equals(item.JobId, jobId, StringComparison.Ordinal));
            if (existing is not null)
            {
                return existing;
            }

            var values = new PrintJobValues(
                jobId,
                jobTime,
                new Dictionary<string, string>(promptValues, StringComparer.OrdinalIgnoreCase),
                DateTimeOffset.UtcNow);
            Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
            var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(values, JsonOptions) + "\n");
            using var stream = new FileStream(_filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read, 4096, FileOptions.WriteThrough);
            TrimUnterminatedTail(stream);
            stream.Seek(0, SeekOrigin.End);
            stream.Write(bytes);
            stream.Flush(flushToDisk: true);
            return values;
        });
    }

    /// <summary>The values of <paramref name="jobId"/>, or of the job a reprint repeats.</summary>
    public PrintJobValues? Find(string jobId)
    {
        var rootJobId = SerialCounterContract.RootJobId(jobId);
        return WithLock(() => ReadAll().LastOrDefault(item => string.Equals(item.JobId, rootJobId, StringComparison.Ordinal)));
    }

    private List<PrintJobValues> ReadAll()
    {
        var result = new List<PrintJobValues>();
        if (!File.Exists(_filePath))
        {
            return result;
        }

        var lines = File.ReadAllText(_filePath, Encoding.UTF8).Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i].TrimEnd('\r');
            if (line.Length == 0)
            {
                continue;
            }

            var isUnterminatedTail = i == lines.Length - 1;
            try
            {
                var item = JsonSerializer.Deserialize<PrintJobValues>(line, JsonOptions);
                if (item is not null && !isUnterminatedTail)
                {
                    result.Add(item);
                }
            }
            catch (JsonException) when (isUnterminatedTail)
            {
            }
            catch (JsonException exception)
            {
                throw new InvalidDataException(
                    $"The print job value record {_filePath} is damaged at line {i + 1}, so reprints cannot recover their dates and entered values. Restore the file from a backup.",
                    exception);
            }
        }

        return result;
    }

    private static void TrimUnterminatedTail(FileStream stream)
    {
        var length = stream.Length;
        var position = length;
        var buffer = new byte[1];
        while (position > 0)
        {
            stream.Seek(position - 1, SeekOrigin.Begin);
            stream.ReadExactly(buffer);
            if (buffer[0] == (byte)'\n')
            {
                break;
            }

            position--;
        }

        if (position < length)
        {
            stream.SetLength(position);
        }
    }

    private T WithLock<T>(Func<T> action)
    {
        using var mutex = new Mutex(initiallyOwned: false, _mutexName);
        try
        {
            if (!mutex.WaitOne(TimeSpan.FromSeconds(10)))
            {
                throw new TimeoutException("Another ANLAbel window is still recording print job values. Try again in a moment.");
            }
        }
        catch (AbandonedMutexException)
        {
        }

        try
        {
            return action();
        }
        finally
        {
            mutex.ReleaseMutex();
        }
    }
}
