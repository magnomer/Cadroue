using Cadroue.Application;
using Cadroue.Core;

using Xunit;

namespace Cadroue.Tests;

public sealed class EditPersistenceTests
{
    [Fact]
    public void KindParse_KnownTokens_ResolveKinds()
    {
        Assert.Equal(LColorKind.LColorKindContrast, LColor.LColorKindParse("Contrast"));
        Assert.Equal(LColorKind.LColorKindBrightness, LColor.LColorKindParse("Brightness"));
    }

    [Fact]
    public void KindParse_UnknownToken_ResolvesNull()
    {
        Assert.Null(LColor.LColorKindParse("Rubbish"));
    }

    [Fact]
    public void KindFormat_RoundTripsBothKinds()
    {
        Assert.Equal("Contrast", LColor.LColorKindFormat(LColorKind.LColorKindContrast));
        Assert.Equal("Brightness", LColor.LColorKindFormat(LColorKind.LColorKindBrightness));
        Assert.Equal(LColorKind.LColorKindContrast, LColor.LColorKindParse(LColor.LColorKindFormat(LColorKind.LColorKindContrast)));
        Assert.Equal(LColorKind.LColorKindBrightness, LColor.LColorKindParse(LColor.LColorKindFormat(LColorKind.LColorKindBrightness)));
    }

    [Fact]
    public void EditPersistentRead_UnknownStepToken_CreatesBrightnessStep()
    {
        var record = new LSidecarEditRecord
        {
            LSidecarSteps = new List<LSidecarVideoStep>
            {
                new() { LSidecarKind = "Rubbish", LSidecarActive = true, LSidecarValue = 40 }
            }
        };

        LEditPlan plan = LEdit.LEditPersistentRead(record);

        LWorkVideoStep step = Assert.Single(plan.LEditVideo.LWorkVideoSteps);
        Assert.Equal(LColorKind.LColorKindBrightness, step.LWorkStepKind);
    }
}
