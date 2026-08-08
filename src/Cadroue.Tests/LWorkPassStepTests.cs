using Cadroue.Core;

using Xunit;

namespace Cadroue.Tests;

public sealed class LWorkPassStepTests
{
    [Fact]
    public void HighCreate_AboveRange_ClampsToBound()
    {
        var step = (LWorkPassStep)LWorkAudioStep.LWorkHighCreate(true, 80, 12, 3, 5);

        Assert.Equal(8, step.LWorkPassStages);
        Assert.Equal(2, step.LWorkPassPoles);
        Assert.Equal(2, step.LWorkPassResonance);
    }

    [Fact]
    public void LowCreate_BelowRange_ClampsToBound()
    {
        var step = (LWorkPassStep)LWorkAudioStep.LWorkLowCreate(true, 9000, 0, 0, 0);

        Assert.Equal(1, step.LWorkPassStages);
        Assert.Equal(1, step.LWorkPassPoles);
        Assert.Equal(0.1, step.LWorkPassResonance);
    }
}
