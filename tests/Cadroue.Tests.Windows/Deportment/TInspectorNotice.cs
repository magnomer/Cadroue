using Cadroue.Application;
using Cadroue.Conduct;
using Cadroue.Core;
using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests.Windows;

public sealed class TInspectorNotice
{
    [Fact]
    public void ToneValue_RaisesOnceOnChange_NeverOnSameValue()
    {
        CTone tone = TInterface.TToneCreate();
        int notices = 0;
        TInterface.TToneAttach(tone, () => notices++);

        tone.CToneBrightnessSet(12.5);
        tone.CToneBrightnessSet(12.5);

        Assert.Equal(1, notices);
        Assert.Equal(12.5, TInterface.TToneStepRead(tone, LColorKind.LColorKindBrightness).LWorkStepValue);
    }

    [Fact]
    public void ToneContrast_ClampsToRange_ThenStaysQuiet()
    {
        CTone tone = TInterface.TToneCreate();
        int notices = 0;
        TInterface.TToneAttach(tone, () => notices++);

        tone.CToneContrastSet(250);
        tone.CToneContrastSet(200);

        Assert.Equal(1, notices);
        Assert.Equal(200, TInterface.TToneStepRead(tone, LColorKind.LColorKindContrast).LWorkStepValue);
    }

    [Fact]
    public void GammaSlot_ChangesOnlyThatSlot_ResetRaisesOnce()
    {
        CGamma gamma = TInterface.TGammaCreate();
        int notices = 0;
        TInterface.TGammaAttach(gamma, () => notices++);

        gamma.CGammaMidtoneSet(10);
        gamma.CGammaRedSet(20);
        gamma.CGammaGreenSet(40);
        gamma.CGammaBlueSet(60);
        gamma.CGammaHighlightSet(15);
        LWorkGammaSettings value = TInterface.TGammaValueRead(gamma);

        Assert.Equal(10, value.LWorkGammaGlobal);
        Assert.Equal(20, value.LWorkGammaRed);
        Assert.Equal(40, value.LWorkGammaGreen);
        Assert.Equal(60, value.LWorkGammaBlue);
        Assert.Equal(15, value.LWorkGammaHighlight);
        Assert.Equal(5, notices);

        TInterface.TGammaReset(gamma);
        TInterface.TGammaReset(gamma);

        Assert.Equal(6, notices);
        Assert.Equal(TInterface.TGammaSettingsCreate(0, 0, 0, 0, 0), TInterface.TGammaValueRead(gamma));
    }

    [Fact]
    public void ExposureValue_ClampsToStops_SameValueIsSilent()
    {
        CExposure exposure = TInterface.TExposureCreate();
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
        QSkip skip = TInterface.TSkipCreate();
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
        CVolume volume = TInterface.TVolumeCreate();
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
        LCropboxState state = TInterfaceCrop.TCropboxStateCreate();
        int notices = 0;
        TInterfaceCrop.TCropboxStateAttach(state, () => notices++);

        TInterfaceCrop.TCropboxCropSet(state, Interface.TInterface.TWorkCropCreate(10, 0, 0, 0, 0, false, false));
        TInterfaceCrop.TCropboxCropSet(state, Interface.TInterface.TWorkCropCreate(10, 0, 0, 0, 0, false, false));
        TInterfaceCrop.TCropboxRatioSet(state, false, false, 0, 0);

        Assert.Equal(1, notices);
    }
}
