using Cadroue.Application;
using Cadroue.Core;

using Xunit;

namespace Cadroue.Tests;

public sealed class EditPersistenceTests
{
    [Fact]
    public void KindParse_KnownTokens_ResolveKinds()
    {
        Assert.Equal(LColorKind.LColorKindContrast, TInterface.ColorKindParse("Contrast"));
        Assert.Equal(LColorKind.LColorKindBrightness, TInterface.ColorKindParse("Brightness"));
    }

    [Fact]
    public void KindParse_UnknownToken_ResolvesNull()
    {
        Assert.Null(TInterface.ColorKindParse("Rubbish"));
    }

    [Fact]
    public void KindFormat_RoundTripsBothKinds()
    {
        Assert.Equal("Contrast", TInterface.ColorKindFormat(LColorKind.LColorKindContrast));
        Assert.Equal("Brightness", TInterface.ColorKindFormat(LColorKind.LColorKindBrightness));
        Assert.Equal(LColorKind.LColorKindContrast, TInterface.ColorKindParse(TInterface.ColorKindFormat(LColorKind.LColorKindContrast)));
        Assert.Equal(LColorKind.LColorKindBrightness, TInterface.ColorKindParse(TInterface.ColorKindFormat(LColorKind.LColorKindBrightness)));
    }

    [Fact]
    public void EditPersistentRead_UnknownStepToken_CreatesBrightnessStep()
    {
        LSidecarEditRecord record = TInterface.SidecarEditRecordCreate("Rubbish", true, 40);

        LEditPlan plan = TInterface.EditPersistentRead(record);

        LWorkVideoStep step = Assert.Single(plan.LEditVideo.LWorkVideoSteps);
        Assert.Equal(LColorKind.LColorKindBrightness, step.LWorkStepKind);
    }
}
