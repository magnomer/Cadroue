using Cadroue.Core;

using Xunit;

namespace Cadroue.Tests;

public sealed class LWorkNormalizeBoundTests
{
    private static LWorkNormalizeStep Create(
        double target = -21, double peak = -2, double range = 6,
        double frame = 300, double gauss = 21, double maxGain = 10, double compress = 6) =>
        (LWorkNormalizeStep)LWorkAudioStep.LWorkNormalizeCreate(
            true, LLeveling.LLevelingLoudness, target, peak, range, true,
            frame, gauss, maxGain, compress);

    [Fact]
    public void NormalizeCreate_AboveMost_ClampsDown()
    {
        LWorkNormalizeStep step = Create(
            target: 0, peak: 5, range: 40,
            frame: 5000, gauss: 200, maxGain: 100, compress: 60);

        Assert.Equal(LLevelingCatalog.LLevelingTargetMost, step.LWorkNormalizeTarget);
        Assert.Equal(LLevelingCatalog.LLevelingPeakMost, step.LWorkNormalizePeak);
        Assert.Equal(LLevelingCatalog.LLevelingRangeMost, step.LWorkNormalizeRange);
        Assert.Equal(LLevelingCatalog.LLevelingFrameMost, step.LWorkNormalizeFrame);
        Assert.Equal(LLevelingCatalog.LLevelingGaussMost, step.LWorkNormalizeGauss);
        Assert.Equal(LLevelingCatalog.LLevelingGainMost, step.LWorkNormalizeGain);
        Assert.Equal(LLevelingCatalog.LLevelingCompressMost, step.LWorkNormalizeCompress);
    }

    [Fact]
    public void NormalizeCreate_BelowLeast_ClampsUp()
    {
        LWorkNormalizeStep step = Create(
            target: -100, peak: -50, range: -5,
            frame: 0, gauss: 0, maxGain: 0, compress: -5);

        Assert.Equal(LLevelingCatalog.LLevelingTargetLeast, step.LWorkNormalizeTarget);
        Assert.Equal(LLevelingCatalog.LLevelingPeakLeast, step.LWorkNormalizePeak);
        Assert.Equal(LLevelingCatalog.LLevelingRangeLeast, step.LWorkNormalizeRange);
        Assert.Equal(LLevelingCatalog.LLevelingFrameLeast, step.LWorkNormalizeFrame);
        Assert.Equal(LLevelingCatalog.LLevelingGaussLeast, step.LWorkNormalizeGauss);
        Assert.Equal(LLevelingCatalog.LLevelingGainLeast, step.LWorkNormalizeGain);
        Assert.Equal(LLevelingCatalog.LLevelingCompressLeast, step.LWorkNormalizeCompress);
    }

    [Fact]
    public void NormalizeCreate_InRange_Unchanged()
    {
        LWorkNormalizeStep step = Create(
            target: -21, peak: -2, range: 6,
            frame: 300, gauss: 21, maxGain: 10, compress: 6);

        Assert.Equal(-21, step.LWorkNormalizeTarget);
        Assert.Equal(-2, step.LWorkNormalizePeak);
        Assert.Equal(6, step.LWorkNormalizeRange);
        Assert.Equal(300, step.LWorkNormalizeFrame);
        Assert.Equal(21, step.LWorkNormalizeGauss);
        Assert.Equal(10, step.LWorkNormalizeGain);
        Assert.Equal(6, step.LWorkNormalizeCompress);
    }
}
