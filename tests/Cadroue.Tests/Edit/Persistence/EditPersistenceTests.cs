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
        Assert.Equal(LColorKind.LColorKindGamma, TInterface.ColorKindParse("Gamma"));
    }

    [Fact]
    public void KindParse_UnknownToken_ResolvesNull()
    {
        Assert.Null(TInterface.ColorKindParse("Rubbish"));
    }

    [Fact]
    public void KindFormat_RoundTripsKnownKinds()
    {
        Assert.Equal("Contrast", TInterface.ColorKindFormat(LColorKind.LColorKindContrast));
        Assert.Equal("Brightness", TInterface.ColorKindFormat(LColorKind.LColorKindBrightness));
        Assert.Equal("Gamma", TInterface.ColorKindFormat(LColorKind.LColorKindGamma));
        Assert.Equal(LColorKind.LColorKindContrast, TInterface.ColorKindParse(TInterface.ColorKindFormat(LColorKind.LColorKindContrast)));
        Assert.Equal(LColorKind.LColorKindBrightness, TInterface.ColorKindParse(TInterface.ColorKindFormat(LColorKind.LColorKindBrightness)));
        Assert.Equal(LColorKind.LColorKindGamma, TInterface.ColorKindParse(TInterface.ColorKindFormat(LColorKind.LColorKindGamma)));
    }

    [Fact]
    public void Gamma_PersistentRecord_RoundTripsValueAndToken()
    {
        LWorkVideo video = TInterface.WorkVideoCreate(new[] { TInterface.WorkGammaCreate(true, 75) });
        LEditPlan source = TInterface.EditPlanCreate(TInterface.WorkCropCreate(), video, false);

        LSidecarEditRecord record = TInterface.EditPersistentCreate(source);
        LEditPlan restored = TInterface.EditPersistentRead(record);

        LSidecarVideoStep stored = Assert.Single(record.LSidecarSteps);
        Assert.Equal("Gamma", stored.LSidecarKind);
        LWorkVideoStep step = Assert.Single(restored.LEditVideo.LWorkVideoSteps);
        Assert.Equal(LColorKind.LColorKindGamma, step.LWorkStepKind);
        Assert.Equal(75, step.LWorkStepValue);
    }

    [Fact]
    public void Gamma_PersistentRecord_RoundTripsCompletePayload()
    {
        LWorkVideo video = TInterface.WorkVideoCreate(new[]
        {
            TInterface.WorkGammaCreate(true, 20.25, -90.5, 10.75, 30.125, 25.5)
        });

        LSidecarEditRecord record = TInterface.EditPersistentCreate(
            TInterface.EditPlanCreate(TInterface.WorkCropCreate(), video, false));
        LSidecarVideoStep stored = Assert.Single(record.LSidecarSteps);
        LWorkVideoStep restored = Assert.Single(
            TInterface.EditPersistentRead(record).LEditVideo.LWorkVideoSteps);

        Assert.Equal(-90.5, stored.LSidecarGammaRed);
        Assert.Equal(10.75, stored.LSidecarGammaGreen);
        Assert.Equal(30.125, stored.LSidecarGammaBlue);
        Assert.Equal(25.5, stored.LSidecarGammaHighlightProtection);
        Assert.NotNull(restored.LWorkStepGamma);
        Assert.Equal(20.25, restored.LWorkStepGamma.LWorkGammaGlobal);
        Assert.Equal(-90.5, restored.LWorkStepGamma.LWorkGammaRed);
        Assert.Equal(10.75, restored.LWorkStepGamma.LWorkGammaGreen);
        Assert.Equal(30.125, restored.LWorkStepGamma.LWorkGammaBlue);
        Assert.Equal(25.5, restored.LWorkStepGamma.LWorkGammaHighlightProtection);
    }

    [Fact]
    public void Gamma_LegacyRecord_UsesValueAsGlobalAndNeutralAdvancedDefaults()
    {
        LSidecarEditRecord record = TInterface.SidecarEditRecordCreate("Gamma", true, 35);

        LWorkVideoStep restored = Assert.Single(
            TInterface.EditPersistentRead(record).LEditVideo.LWorkVideoSteps);

        Assert.Equal(35, restored.LWorkStepValue);
        Assert.NotNull(restored.LWorkStepGamma);
        Assert.Equal(35, restored.LWorkStepGamma.LWorkGammaGlobal);
        Assert.Equal(0, restored.LWorkStepGamma.LWorkGammaRed);
        Assert.Equal(0, restored.LWorkStepGamma.LWorkGammaGreen);
        Assert.Equal(0, restored.LWorkStepGamma.LWorkGammaBlue);
        Assert.Equal(0, restored.LWorkStepGamma.LWorkGammaHighlightProtection);
    }

    [Fact]
    public void NonGammaRecord_IgnoresGammaFields()
    {
        LSidecarEditRecord record = TInterface.SidecarEditRecordCreate(
            "Contrast", true, 125, 50, null, null, 100);

        LWorkVideoStep restored = Assert.Single(
            TInterface.EditPersistentRead(record).LEditVideo.LWorkVideoSteps);

        Assert.Equal(LColorKind.LColorKindContrast, restored.LWorkStepKind);
        Assert.Null(restored.LWorkStepGamma);
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
