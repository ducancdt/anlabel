using ANLAbel.Core.Models;
using ANLAbel.Core.Printing;
using ANLAbel.Core.Scene;
using ANLAbel.Data.PrintLogs;
using ANLAbel.Project.SaveLoad;
using Xunit;

namespace ANLAbel.UnitTests;

public sealed class SerialCounterTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"anl-serial-{Guid.NewGuid():N}");

    public SerialCounterTests() => Directory.CreateDirectory(_directory);

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    private SerialCounterStore NewStore() => new(Path.Combine(_directory, "reservations.jsonl"));

    private static SerialCounterDefinition Counter(
        long start = 1,
        long step = 1,
        int changeEvery = 1,
        long min = 0,
        long max = 999999,
        bool rollover = false,
        int digits = 0,
        string prefix = "",
        string suffix = "") => new()
    {
        Enabled = true,
        StartValue = start,
        Step = step,
        ChangeEvery = changeEvery,
        MinimumValue = min,
        MaximumValue = max,
        Rollover = rollover,
        Digits = digits,
        Prefix = prefix,
        Suffix = suffix
    };

    private static IReadOnlyDictionary<string, string>?[] Rows(int count)
        => Enumerable.Range(1, count)
            .Select(i => (IReadOnlyDictionary<string, string>?)new Dictionary<string, string> { ["Part"] = $"P{i}" })
            .ToArray();

    [Fact]
    public void EnablingCreatesAnIdentityAndResetReplacesIt()
    {
        var counter = new SerialCounterDefinition();
        Assert.Equal(string.Empty, counter.CounterId);

        counter.Enabled = true;
        var first = counter.CounterId;
        Assert.Equal(32, first.Length);

        counter.Reset();
        Assert.NotEqual(first, counter.CounterId);
    }

    [Fact]
    public void ApplyNumbersLabelsInPrintOrderWithChangeEveryPaddingAndAffixes()
    {
        var counter = Counter(start: 7, step: 2, changeEvery: 2, digits: 4, prefix: "SN-", suffix: "/A");

        Assert.True(SerialCounterContract.TryApply(counter, 7, Rows(5), out var rows, out var error), error);

        Assert.Equal(new[] { "SN-0007/A", "SN-0007/A", "SN-0009/A", "SN-0009/A", "SN-0011/A" },
            rows.Select(row => row![SerialCounterContract.Token]));
        Assert.Equal("P3", rows[2]!["Part"]);
        Assert.Equal(3, SerialCounterContract.ValueCount(counter, 5));
        Assert.Equal(13, SerialCounterContract.NextFirstValue(counter, 7, 3));
    }

    [Fact]
    public void ApplyGivesNullRowsTheirOwnSerial()
    {
        var counter = Counter();

        Assert.True(SerialCounterContract.TryApply(counter, 1, new IReadOnlyDictionary<string, string>?[] { null, null }, out var rows, out _));

        Assert.Equal("1", rows[0]![SerialCounterContract.Token]);
        Assert.Equal("2", rows[1]![SerialCounterContract.Token]);
    }

    [Theory]
    [InlineData(1, 3, 98, 1, true, new long[] { 98, 99, 3, 4 })]
    [InlineData(-1, 1, 2, 1, true, new long[] { 2, 1, 99, 98 })]
    public void RolloverWrapsInsideTheLimits(long step, long min, long first, int changeEvery, bool rollover, long[] expected)
    {
        var counter = Counter(start: first, step: step, changeEvery: changeEvery, min: min, max: 99, rollover: rollover);

        var values = Enumerable.Range(0, expected.Length).Select(i => SerialCounterContract.ValueAt(counter, first, i)).ToArray();

        Assert.Equal(expected.Select(v => (long?)v), values);
    }

    [Fact]
    public void WithoutRolloverRunningPastTheLimitBlocksAndNamesTheLabel()
    {
        var counter = Counter(start: 98, max: 99);

        Assert.False(SerialCounterContract.TryApply(counter, 98, Rows(3), out _, out var error));
        Assert.Contains("at label 3 of 3", error);
    }

    [Theory]
    [InlineData(0, 1, 0, 9, 1, 0, "step must not be 0")]
    [InlineData(1, 0, 0, 9, 1, 0, "change every 1 to")]
    [InlineData(1, 1, 5, 4, 5, 0, "greater than its maximum")]
    [InlineData(1, 1, 0, 9, 10, 0, "start value 10 is outside")]
    [InlineData(1, 1, 0, 9, 1, 19, "digits must be")]
    public void InvalidDefinitionsAreExplained(long step, int changeEvery, long min, long max, long start, int digits, string expected)
    {
        var counter = Counter(start: start, step: step, changeEvery: changeEvery, min: min, max: max, digits: digits);

        var errors = SerialCounterContract.ValidateDefinition(counter);

        Assert.Contains(errors, message => message.Contains(expected, StringComparison.Ordinal));
    }

    [Fact]
    public void ReprintJobIdsResolveToTheOriginalJob()
    {
        Assert.Equal("abc", SerialCounterContract.RootJobId("abc:reprint:123"));
        Assert.Equal("abc", SerialCounterContract.RootJobId("abc"));
    }

    [Fact]
    public void ReservationsAreSequentialAndAReprintFindsTheOriginalNumbers()
    {
        var store = NewStore();
        var counter = Counter(start: 100);

        Assert.Equal(100, store.PeekNextValue(counter));
        Assert.True(store.TryReserve(counter, "job-a", 100, 3, out var a, out var error), error);
        Assert.Equal(103, store.PeekNextValue(counter));
        Assert.True(store.TryReserve(counter, "job-b", 103, 2, out _, out error), error);
        Assert.Equal(105, store.PeekNextValue(counter));

        var reprint = store.FindReservation(counter.CounterId, "job-a:reprint:ffff");
        Assert.NotNull(reprint);
        Assert.Equal(100, reprint!.FirstValue);
        Assert.Equal(a, reprint);
    }

    [Fact]
    public void AReservationThatNoLongerMatchesThePreviewIsRefused()
    {
        var store = NewStore();
        var counter = Counter(start: 1);
        Assert.True(store.TryReserve(counter, "job-a", 1, 5, out _, out _));

        Assert.False(store.TryReserve(counter, "job-b", 1, 5, out var reservation, out var error));
        Assert.Null(reservation);
        Assert.Contains("the next number is now 6", error);
        Assert.Equal(6, store.PeekNextValue(counter));
    }

    [Fact]
    public void RepeatingAReservationForTheSameJobIsIdempotent()
    {
        var store = NewStore();
        var counter = Counter(start: 1);
        Assert.True(store.TryReserve(counter, "job-a", 1, 5, out var first, out _));

        Assert.True(store.TryReserve(counter, "job-a", 1, 5, out var again, out _));
        Assert.Equal(first, again);
        Assert.Equal(6, store.PeekNextValue(counter));
    }

    [Fact]
    public void RaisingTheStartSkipsForwardButLoweringItNeverGoesBack()
    {
        var store = NewStore();
        var counter = Counter(start: 1);
        Assert.True(store.TryReserve(counter, "job-a", 1, 10, out _, out _));

        counter.StartValue = 5;
        Assert.Equal(11, store.PeekNextValue(counter));
        counter.StartValue = 500;
        Assert.Equal(500, store.PeekNextValue(counter));

        counter.Reset();
        counter.StartValue = 1;
        Assert.Equal(1, store.PeekNextValue(counter));
    }

    [Fact]
    public async Task ConcurrentReservationsNeverShareANumber()
    {
        var path = Path.Combine(_directory, "reservations.jsonl");
        var counter = Counter(start: 1);
        var tasks = Enumerable.Range(0, 8).Select(worker => Task.Run(() =>
        {
            var store = new SerialCounterStore(path);
            var mine = new List<SerialCounterReservation>();
            for (var attempt = 0; attempt < 40 && mine.Count < 5; attempt++)
            {
                var next = store.PeekNextValue(counter);
                if (store.TryReserve(counter, $"w{worker}-{mine.Count}", next, 3, out var reservation, out _))
                {
                    mine.Add(reservation!);
                }
            }

            return mine;
        })).ToArray();

        var all = (await Task.WhenAll(tasks)).SelectMany(list => list).ToArray();
        var numbers = all.SelectMany(r => Enumerable.Range((int)r.FirstValue, r.LabelCount)).ToArray();

        Assert.Equal(numbers.Length, numbers.Distinct().Count());
        Assert.Equal(all.Length * 3 + 1, new SerialCounterStore(path).PeekNextValue(counter));
    }

    [Fact]
    public void ASharedFolderGivesSeveralPcsOneSequence()
    {
        var shared = Path.Combine(_directory, "share");
        var counter = Counter(start: 1);
        counter.SharedFolder = shared;
        var pcA = SerialCounterStore.ForCounter(counter, Path.Combine(_directory, "pc-a-local.jsonl"));
        var pcB = SerialCounterStore.ForCounter(counter, Path.Combine(_directory, "pc-b-local.jsonl"));

        Assert.Equal(Path.Combine(shared, SerialCounterStore.SharedFileName), pcA.FilePath);
        Assert.True(pcA.TryReserve(counter, "job-a", 1, 5, out _, out var errorA), errorA);
        Assert.Equal(6, pcB.PeekNextValue(counter));
        Assert.False(pcB.TryReserve(counter, "job-b", 1, 5, out _, out _));
        Assert.True(pcB.TryReserve(counter, "job-b", 6, 5, out _, out var errorB), errorB);
        Assert.False(File.Exists(Path.Combine(_directory, "pc-a-local.jsonl")));
    }

    [Fact]
    public void AnotherPcHoldingTheSharedLockMakesTheReservationWaitThenFailClearly()
    {
        var shared = Path.Combine(_directory, "share");
        Directory.CreateDirectory(shared);
        var counter = Counter(start: 1);
        var store = new SerialCounterStore(Path.Combine(shared, SerialCounterStore.SharedFileName), crossMachineLock: true, lockTimeout: TimeSpan.FromMilliseconds(300));

        using (new FileStream(Path.Combine(shared, SerialCounterStore.SharedFileName) + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
        {
            var error = Assert.Throws<TimeoutException>(() => store.TryReserve(counter, "job-a", 1, 2, out _, out _));
            Assert.Contains("still reserving serial numbers", error.Message);
        }

        Assert.True(store.TryReserve(counter, "job-a", 1, 2, out _, out var reserveError), reserveError);
    }

    [Fact]
    public void ACutOffLastRecordIsIgnoredAndTrimmedBeforeTheNextWrite()
    {
        var path = Path.Combine(_directory, "reservations.jsonl");
        var store = new SerialCounterStore(path);
        var counter = Counter(start: 1);
        Assert.True(store.TryReserve(counter, "job-a", 1, 2, out _, out _));
        File.AppendAllText(path, "{\"counterId\":\"" + counter.CounterId + "\",\"jobId\":\"torn\",\"firstVal");

        Assert.Equal(3, store.PeekNextValue(counter));
        Assert.True(store.TryReserve(counter, "job-b", 3, 2, out _, out var error), error);
        Assert.Equal(5, new SerialCounterStore(path).PeekNextValue(counter));
        Assert.DoesNotContain("torn", File.ReadAllText(path));
    }

    [Fact]
    public void ADamagedRecordInTheMiddleBlocksInsteadOfGuessing()
    {
        var path = Path.Combine(_directory, "reservations.jsonl");
        var counter = Counter(start: 1);
        File.WriteAllText(path, "not json\n");

        var error = Assert.Throws<InvalidDataException>(() => new SerialCounterStore(path).PeekNextValue(counter));
        Assert.Contains("is damaged at line 1", error.Message);
    }

    [Fact]
    public void AReprintRebuildsTheSameRowsFingerprint()
    {
        var store = NewStore();
        var counter = Counter(start: 41, digits: 6);
        var sourceRows = Rows(4);
        var first = store.PeekNextValue(counter);
        Assert.True(SerialCounterContract.TryApply(counter, first, sourceRows, out var printed, out _));
        Assert.True(store.TryReserve(counter, "job-1", first, printed.Length, out _, out _));
        var original = PrintJobManifest.ComputeRowsFingerprint(printed);

        // A later job moves the counter on; the reprint must still use job-1's numbers.
        Assert.True(store.TryReserve(counter, "job-2", store.PeekNextValue(counter), 10, out _, out _));
        var reservation = store.FindReservation(counter.CounterId, "job-1:reprint:abc");
        Assert.True(SerialCounterContract.TryApply(counter, reservation!.FirstValue, sourceRows, out var reprinted, out _));

        Assert.Equal(original, PrintJobManifest.ComputeRowsFingerprint(reprinted));
        Assert.Equal("000041", reprinted[0]![SerialCounterContract.Token]);
    }

    [Fact]
    public void DocumentHashOnlyChangesWhileTheCounterIsOn()
    {
        var template = new LabelTemplate { Name = "Serial" };
        var before = DocumentSnapshot.Capture(template).DocumentHash;

        template.SerialCounter.Enabled = true;
        var on = DocumentSnapshot.Capture(template).DocumentHash;
        template.SerialCounter.StartValue = 2;
        var changed = DocumentSnapshot.Capture(template).DocumentHash;
        template.SerialCounter.Enabled = false;
        var off = DocumentSnapshot.Capture(template).DocumentHash;

        Assert.NotEqual(before, on);
        Assert.NotEqual(on, changed);
        Assert.Equal(before, off);
    }

    [Fact]
    public async Task TheCounterDefinitionIsSavedWithTheTemplate()
    {
        var path = Path.Combine(_directory, "serial.anlabel");
        var template = new LabelTemplate { Name = "Serial" };
        template.SerialCounter = Counter(start: 9, step: -3, changeEvery: 2, min: -50, max: 50, rollover: true, digits: 3, prefix: "A", suffix: "Z");
        var service = new ProjectFileService();

        await service.SaveAsync(template, path);
        var loaded = (await service.LoadAsync(path)).SerialCounter;

        Assert.Equal(
            (true, template.SerialCounter.CounterId, 9L, -3L, 2, -50L, 50L, true, 3, "A", "Z"),
            (loaded.Enabled, loaded.CounterId, loaded.StartValue, loaded.Step, loaded.ChangeEvery,
             loaded.MinimumValue, loaded.MaximumValue, loaded.Rollover, loaded.Digits, loaded.Prefix, loaded.Suffix));
        Assert.Equal(template.SerialCounter.CounterId, PrintSessionTemplateContract.Capture(template).SerialCounter.CounterId);
    }
}
