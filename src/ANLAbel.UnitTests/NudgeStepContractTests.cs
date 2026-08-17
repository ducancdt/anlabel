using ANLAbel.Core.Enums;
using ANLAbel.Core.Geometry;
using Xunit;

namespace ANLAbel.UnitTests;

public sealed class NudgeStepContractTests
{
    [Fact]
    public void PublicStepsArePhysicalMillimetres()
    {
        Assert.Equal(0.01, NudgeStepContract.FineStepMm);
        Assert.Equal(0.1, NudgeStepContract.StandardStepMm);
        Assert.Equal(1.0, NudgeStepContract.CoarseStepMm);
        Assert.True(NudgeStepContract.FineStepMm < NudgeStepContract.StandardStepMm);
        Assert.True(NudgeStepContract.StandardStepMm < NudgeStepContract.CoarseStepMm);
    }

    [Fact]
    public void ResolveStepMmUsesTheNamedConstant()
    {
        Assert.Equal(NudgeStepContract.FineStepMm, NudgeStepContract.ResolveStepMm(NudgeStepMode.Fine));
        Assert.Equal(NudgeStepContract.StandardStepMm, NudgeStepContract.ResolveStepMm(NudgeStepMode.Standard));
        Assert.Equal(NudgeStepContract.CoarseStepMm, NudgeStepContract.ResolveStepMm(NudgeStepMode.Coarse));
    }

    [Fact]
    public void UnknownModeFallsBackToStandard()
    {
        Assert.Equal(
            NudgeStepContract.StandardStepMm,
            NudgeStepContract.ResolveStepMm((NudgeStepMode)99));
    }

    [Theory]
    [InlineData(NudgeStepMode.Fine)]
    [InlineData(NudgeStepMode.Standard)]
    [InlineData(NudgeStepMode.Coarse)]
    public void ResolveDeltaMovesOneAxisByTheResolvedStep(NudgeStepMode mode)
    {
        var step = NudgeStepContract.ResolveStepMm(mode);

        Assert.Equal((-step, 0d), NudgeStepContract.ResolveDelta(NudgeDirection.Left, mode));
        Assert.Equal((step, 0d), NudgeStepContract.ResolveDelta(NudgeDirection.Right, mode));
        Assert.Equal((0d, -step), NudgeStepContract.ResolveDelta(NudgeDirection.Up, mode));
        Assert.Equal((0d, step), NudgeStepContract.ResolveDelta(NudgeDirection.Down, mode));
    }

    [Fact]
    public void UnknownDirectionDoesNotMove()
    {
        Assert.Equal((0d, 0d), NudgeStepContract.ResolveDelta((NudgeDirection)99, NudgeStepMode.Coarse));
        Assert.Equal((0d, 0d), NudgeStepContract.ResolveDelta((NudgeDirection)99, NudgeStepMode.Fine));
    }
}
