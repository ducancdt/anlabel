namespace ANLAbel.Core.Licensing;

public enum TrialStatus { Valid, Expired, ClockTampered }

public sealed record TrialDecision(TrialStatus Status, TimeSpan Remaining)
{
    public bool IsAllowed => Status == TrialStatus.Valid;
}

public static class TrialPolicy
{
    public static readonly TimeSpan TrialDuration = TimeSpan.FromDays(7);
    private static readonly TimeSpan ClockRollbackTolerance = TimeSpan.FromMinutes(5);

    public static TrialDecision Evaluate(DateTimeOffset firstRunUtc, DateTimeOffset lastSeenUtc, DateTimeOffset nowUtc)
    {
        firstRunUtc = firstRunUtc.ToUniversalTime();
        lastSeenUtc = lastSeenUtc.ToUniversalTime();
        nowUtc = nowUtc.ToUniversalTime();

        if (firstRunUtc > nowUtc + ClockRollbackTolerance ||
            lastSeenUtc > nowUtc + ClockRollbackTolerance ||
            lastSeenUtc < firstRunUtc - ClockRollbackTolerance)
        {
            return new TrialDecision(TrialStatus.ClockTampered, TimeSpan.Zero);
        }

        var remaining = firstRunUtc + TrialDuration - nowUtc;
        return remaining > TimeSpan.Zero
            ? new TrialDecision(TrialStatus.Valid, remaining)
            : new TrialDecision(TrialStatus.Expired, TimeSpan.Zero);
    }
}
