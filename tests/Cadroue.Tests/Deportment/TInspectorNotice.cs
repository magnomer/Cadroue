using Cadroue.Application;
using Cadroue.Core;
using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests;

public sealed class TInspectorNotice
{
    [Fact]
    public void ToneValue_RaisesOnceOnChange_NeverOnSameValue()
    {
        LTone tone = TInterface.TToneCreate();
        int notices = 0;
        TInterface.TToneAttach(tone, () => notices++);

        TInterface.TToneValueSet(tone, LColorKind.LColorKindBrightness, 12.5);
        TInterface.TToneValueSet(tone, LColorKind.LColorKindBrightness, 12.5);

        Assert.Equal(1, notices);
        Assert.Equal(12.5, TInterface.TToneStepRead(tone, LColorKind.LColorKindBrightness).LWorkStepValue);
    }

    [Fact]
    public void ToneContrast_ClampsToRange_ThenStaysQuiet()
    {
        LTone tone = TInterface.TToneCreate();
        int notices = 0;
        TInterface.TToneAttach(tone, () => notices++);

        TInterface.TToneValueSet(tone, LColorKind.LColorKindContrast, 250);
        TInterface.TToneValueSet(tone, LColorKind.LColorKindContrast, 200);

        Assert.Equal(1, notices);
        Assert.Equal(200, TInterface.TToneStepRead(tone, LColorKind.LColorKindContrast).LWorkStepValue);
    }

    [Fact]
    public void GammaSlot_ChangesOnlyThatSlot_ResetRaisesOnce()
    {
        LGamma gamma = TInterface.TGammaCreate();
        int notices = 0;
        TInterface.TGammaAttach(gamma, () => notices++);

        TInterface.TGammaValueSet(gamma, 2, 40);
        TInterface.TGammaValueSet(gamma, 4, 15);
        LWorkGammaSettings value = TInterface.TGammaValueRead(gamma);

        Assert.Equal(0, value.LWorkGammaGlobal);
        Assert.Equal(40, value.LWorkGammaGreen);
        Assert.Equal(15, value.LWorkGammaHighlight);
        Assert.Equal(2, notices);

        TInterface.TGammaReset(gamma);
        TInterface.TGammaReset(gamma);

        Assert.Equal(3, notices);
        Assert.Equal(TInterface.TGammaSettingsCreate(0, 0, 0, 0, 0), TInterface.TGammaValueRead(gamma));
    }

    [Fact]
    public void ExposureValue_ClampsToStops_SameValueIsSilent()
    {
        LExposure exposure = TInterface.TExposureCreate();
        int notices = 0;
        TInterface.TExposureAttach(exposure, () => notices++);

        TInterface.TExposureValueSet(exposure, 7);
        TInterface.TExposureValueSet(exposure, 3);

        Assert.Equal(1, notices);
        Assert.Equal(3, TInterface.TExposureStepRead(exposure).LWorkStepValue);
    }

    [Fact]
    public void SkipFlags_RaiseOnlyOnChange()
    {
        LSkip skip = TInterface.TSkipCreate();
        int notices = 0;
        TInterface.TSkipAttach(skip, () => notices++);

        TInterface.TSkipActiveSet(skip, true);
        TInterface.TSkipActiveSet(skip, true);
        TInterface.TSkipPersistentSet(skip, false);

        Assert.Equal(1, notices);
    }

    [Fact]
    public void VolumeGain_ClampsAndStaysQuietOnRepeat()
    {
        LVolume volume = TInterface.TVolumeCreate();
        int notices = 0;
        TInterface.TVolumeAttach(volume, () => notices++);

        TInterface.TVolumeGainSet(volume, 30);
        TInterface.TVolumeGainSet(volume, 24);
        TInterface.TVolumeActiveSet(volume, false);

        Assert.Equal(1, notices);
        Assert.Equal(24, TInterface.TVolumeGainRead(volume));
    }

    [Fact]
    public void CropboxState_SameCropAndRatio_RaiseNothing()
    {
        LCropboxState state = TInterface.TCropboxStateCreate();
        int notices = 0;
        TInterface.TCropboxStateAttach(state, () => notices++);

        TInterface.TCropboxCropSet(state, TInterface.TWorkCropCreate(10, 0, 0, 0, 0, false, false));
        TInterface.TCropboxCropSet(state, TInterface.TWorkCropCreate(10, 0, 0, 0, 0, false, false));
        TInterface.TCropboxRatioSet(state, false, false, 0, 0);

        Assert.Equal(1, notices);
    }
}
