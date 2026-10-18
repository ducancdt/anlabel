using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ANLAbel.Core.Models;
using ANLAbel.Core.Printing;

namespace ANLAbel.Data.PrintLogs;

/// <summary>One print job's claim on a run of serial counter values.</summary>
public sealed record SerialCounterReservation(
    string CounterId,
    string JobId,
    long FirstValue,
    int LabelCount,
    long ValueCount,
    long NextValue,
    DateTimeOffset ReservedAtUtc);

/// <summary>
/// Append-only record of serial counter reservations for this Windows user.
/// A job reserves its values before it is dispatched and never gives them
/// back, so an uncertain or failed job leaves a gap rather than a duplicate.
/// A named mutex serializes the read-check-append across ANLAbel processes.
/// </summary>
public sealed class SerialCounterStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    public const string SharedFileName = "anlabel-serial-counters.jsonl";
    private readonly string _filePath;
    private readonly string _mutexName;
    private readonly bool _crossMachineLock;
    private readonly TimeSpan _lockTimeout;

    /// <summary>
    /// The store for <paramref name="counter"/>: its shared folder when one is
    /// set (locked across PCs with an exclusive lock file), otherwise the local one.
    /// </summary>
    public static SerialCounterStore ForCounter(SerialCounterDefinition counter, string? localFilePath = null)
    {
        ArgumentNullException.ThrowIfNull(counter);
        return string.IsNullOrWhiteSpace(counter.SharedFolder)
            ? new SerialCounterStore(localFilePath)
            : new SerialCounterStore(Path.Combine(counter.SharedFolder, SharedFileName), crossMachineLock: true);
    }

    public SerialCounterStore(string? filePath = null, bool crossMachineLock = false, TimeSpan? lockTimeout = null)
    {
        _filePath = filePath ?? AppDataPaths.Local("serial-counter-reservations.jsonl");
        _crossMachineLock = crossMachineLock;
        _lockTimeout = lockTimeout ?? TimeSpan.FromSeconds(10);
        // One mutex per store file, so test stores never contend with the real one.
        var pathHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Path.GetFullPath(_filePath).ToUpperInvariant())))[..16];
        _mutexName = $@"Local\ANLAbel.SerialCounter.{pathHash}";
    }

    public string FilePath => _filePath;

    /// <summary>The first value the next job would receive, without reserving it.</summary>
    public long PeekNextValue(SerialCounterDefinition counter)
    {
        ArgumentNullException.ThrowIfNull(counter);
        return WithLock(() => NextValue(counter, ReadAll()));
    }

    /// <summary>The reservation made by <paramref name="jobId"/> (or the job a reprint repeats).</summary>
    public SerialCounterReservation? FindReservation(string counterId, string jobId)
    {
        var rootJobId = SerialCounterContract.RootJobId(jobId);
        return WithLock(() => ReadAll().LastOrDefault(item =>
            string.Equals(item.CounterId, counterId, StringComparison.Ordinal)
            && string.Equals(item.JobId, rootJobId, StringComparison.Ordinal)));
    }

    /// <summary>
    /// Reserves the values for a job that was previewed starting at
    /// <paramref name="expectedFirstValue"/>. Fails without reserving when
    /// another job took those values first, so the printed numbers are always
    /// the ones the operator saw and preflight checked. Repeating the call for
    /// the same job returns its existing reservation.
    /// </summary>
    public bool TryReserve(
        SerialCounterDefinition counter,
        string jobId,
        long expectedFirstValue,
        int labelCount,
        out SerialCounterReservation? reservation,
        out string error)
    {
        ArgumentNullException.ThrowIfNull(counter);
        ArgumentException.ThrowIfNullOrWhiteSpace(jobId);
        SerialCounterReservation? reserved = null;
        var failure = string.Empty;
        WithLock(() =>
        {
            var all = ReadAll();
            var existing = all.LastOrDefault(item =>
                string.Equals(item.CounterId, counter.CounterId, StringComparison.Ordinal)
                && string.Equals(item.JobId, jobId, StringComparison.Ordinal));
            if (existing is not null)
            {
                if (existing.FirstValue == expectedFirstValue && existing.LabelCount == labelCount)
                {
                    reserved = existing;
                }
                else
                {
                    failure = $"Serial numbers for job {jobId} were already reserved from {existing.FirstValue}.";
                }

                return 0;
            }

            var next = NextValue(counter, all);
            if (next != expectedFirstValue)
            {
                failure = $"Serial numbers changed since the preview: another print took them, the next number is now {SerialCounterContract.Format(counter, next)}. Check the preview again before printing.";
                return 0;
            }

            var valueCount = SerialCounterContract.ValueCount(counter, labelCount);
            reserved = new SerialCounterReservation(
                counter.CounterId,
                jobId,
                expectedFirstValue,
                labelCount,
                valueCount,
                SerialCounterContract.NextFirstValue(counter, expectedFirstValue, valueCount),
                DateTimeOffset.UtcNow);
            Append(reserved);
            return 0;
        });

        reservation = reserved;
        error = failure;
        return reserved is not null;
    }

    private static long NextValue(SerialCounterDefinition counter, IReadOnlyList<SerialCounterReservation> all)
    {
        var last = all.LastOrDefault(item => string.Equals(item.CounterId, counter.CounterId, StringComparison.Ordinal));
        if (last is null)
        {
            return counter.StartValue;
        }

        // Raising the start value skips forward; without a Reset it never goes back.
        if (!counter.Rollover
            && ((counter.Step > 0 && counter.StartValue > last.NextValue)
                || (counter.Step < 0 && counter.StartValue < last.NextValue)))
        {
            return counter.StartValue;
        }

        return last.NextValue;
    }

    private List<SerialCounterReservation> ReadAll()
    {
        var result = new List<SerialCounterReservation>();
        if (!File.Exists(_filePath))
        {
            return result;
        }

        var text = File.ReadAllText(_filePath, Encoding.UTF8);
        var lines = text.Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i].TrimEnd('\r');
            if (line.Length == 0)
            {
                continue;
            }

            // Every reservation ends with a newline. A last line without one
            // was cut off before its job could be dispatched, so it is ignored.
            var isUnterminatedTail = i == lines.Length - 1;
            try
            {
                var item = JsonSerializer.Deserialize<SerialCounterReservation>(line, JsonOptions);
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
                    $"The serial counter record {_filePath} is damaged at line {i + 1}, so the next serial number cannot be trusted. Restore the file from a backup before printing serial numbers.",
                    exception);
            }
        }

        return result;
    }

    private void Append(SerialCounterReservation reservation)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
        var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(reservation, JsonOptions) + "\n");
        using var stream = new FileStream(
            _filePath,
            FileMode.OpenOrCreate,
            FileAccess.ReadWrite,
            FileShare.Read,
            bufferSize: 4096,
            FileOptions.WriteThrough);
        TrimUnterminatedTail(stream);
        stream.Seek(0, SeekOrigin.End);
        stream.Write(bytes);
        stream.Flush(flushToDisk: true);
    }

    /// <summary>
    /// Drops a record that was cut off mid-write (it is ignored on read), so the
    /// next record starts on its own line instead of joining the broken one.
    /// </summary>
    private static void TrimUnterminatedTail(FileStream stream)
    {
        var length = stream.Length;
        if (length == 0)
        {
            return;
        }

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
                throw new TimeoutException("Another ANLAbel window is still reserving serial numbers. Try again in a moment.");
            }
        }
        catch (AbandonedMutexException)
        {
            // The previous owner exited; the append-only file is still consistent.
        }

        try
        {
            if (!_crossMachineLock)
            {
                return action();
            }

            // Named mutexes are per PC; an exclusive lock file next to the shared
            // record also stops another PC from reserving at the same moment.
            using var lockFile = AcquireLockFile();
            return action();
        }
        finally
        {
            mutex.ReleaseMutex();
        }
    }

    private FileStream AcquireLockFile()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
        var lockPath = _filePath + ".lock";
        var deadline = DateTime.UtcNow + _lockTimeout;
        while (true)
        {
            try
            {
                return new FileStream(lockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            }
            catch (IOException exception) when (AtomicFile.IsSharingViolation(exception) && DateTime.UtcNow < deadline)
            {
                Thread.Sleep(100);
            }
            catch (IOException exception) when (AtomicFile.IsSharingViolation(exception))
            {
                throw new TimeoutException("Another ANLAbel window is still reserving serial numbers. Try again in a moment.", exception);
            }
        }
    }
}
