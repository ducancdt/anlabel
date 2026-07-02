using ANLAbel.Core.Licensing;
using Xunit;

namespace ANLAbel.UnitTests;

public sealed class TrialPolicyTests
{
    private static readonly DateTimeOffset Start = new(2026, 6, 1, 8, 0, 0, TimeSpan.Zero);

    [Fact] public void FirstSevenDays_AreAllowed() => Assert.True(TrialPolicy.Evaluate(Start, Start.AddDays(6), Start.AddDays(6).AddHours(23)).IsAllowed);

    [Fact] public void ExactlySevenDays_IsExpired() => Assert.Equal(TrialStatus.Expired, TrialPolicy.Evaluate(Start, Start.AddDays(6), Start.AddDays(7)).Status);

    [Fact] public void RollingClockBack_IsRejected() => Assert.Equal(TrialStatus.ClockTampered, TrialPolicy.Evaluate(Start, Start.AddDays(3), Start.AddDays(2)).Status);

    [Fact] public void SmallClockCorrection_IsTolerated() => Assert.True(TrialPolicy.Evaluate(Start, Start.AddHours(1), Start.AddHours(1).AddMinutes(-3)).IsAllowed);
}
