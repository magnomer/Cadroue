using Cadroue.Core;

namespace Cadroue.ShellEngine;

// One plan read for the inspector gate: each tool's step as plain values, or the Core default step when the plan has
// none. A video plan answers the audio tools from an empty audio plan, and the reverse.
public sealed class LInspectorOutlet : LInspectorPort
{
    private readonly LWorkVideo _lInspectorVideo;
    private readonly LWorkAudio _lInspectorAudio;

    public LInspectorOutlet(LWorkVideo lVideo)
    {
        ArgumentNullException.ThrowIfNull(lVideo);
        _lInspectorVideo = lVideo;
        _lInspectorAudio = new LWorkAudio(new List<LWorkAudioStep>());
    }

    public LInspectorOutlet(LWorkAudio lAudio)
    {
        ArgumentNullException.ThrowIfNull(lAudio);
        _lInspectorVideo = LWorkVideo.LWorkVideoCreate();
        _lInspectorAudio = lAudio;
    }

    public (bool LInspectorActive, IReadOnlyList<double> LInspectorValues) LInspectorGammaRead()
    {
        LWorkVideoStep lStep = LInspectorVideoFind(LColorKind.LColorKindGamma)
            ?? LWorkVideoStep.LWorkGammaCreate(false, 0);
        return (lStep.LWorkStepActive, lStep.LWorkGammaRead().LWorkSlotRead());
    }

    public (bool LInspectorActive, double LInspectorValue) LInspectorExposureRead()
    {
        LWorkVideoStep lStep = LInspectorVideoFind(LColorKind.LColorKindExposure)
            ?? LWorkVideoStep.LWorkExposureCreate(false, 0);
        return (lStep.LWorkStepActive, lStep.LWorkStepValue);
    }

    public (bool LInspectorActive, double LInspectorValue) LInspectorVolumeRead()
    {
        LWorkAudioStep lStep = LInspectorAudioFind(LAudioKind.LAudioKindVolume)
            ?? LWorkAudioStep.LWorkVolumeCreate(false, 0);
        return (lStep.LWorkStepActive, lStep is LWorkVolumeStep lVolume ? lVolume.LWorkVolumeGain : 0);
    }

    // Brightness, contrast and saturation in slot order.
    public (IReadOnlyList<bool> LInspectorActives, IReadOnlyList<double> LInspectorValues) LInspectorToneRead()
    {
        LWorkVideoStep[] lSteps =
        {
            LInspectorVideoFind(LColorKind.LColorKindBrightness) ?? LWorkVideoStep.LWorkBrightnessCreate(false, 0),
            LInspectorVideoFind(LColorKind.LColorKindContrast) ?? LWorkVideoStep.LWorkContrastCreate(false, 100),
            LInspectorVideoFind(LColorKind.LColorKindSaturation) ?? LWorkVideoStep.LWorkSaturationCreate(false, 100)
        };
        return (lSteps.Select(lStep => lStep.LWorkStepActive).ToList(),
            lSteps.Select(lStep => lStep.LWorkStepValue).ToList());
    }

    // A step that is not an equalizer carries the default bands.
    public (bool LInspectorActive, IReadOnlyList<(double, double)> LInspectorBands, string? LInspectorPreset)
        LInspectorEqualizerRead()
    {
        LWorkAudioStep lStep = LInspectorAudioFind(LAudioKind.LAudioKindEqualizer)
            ?? LWorkAudioStep.LWorkEqualizerCreate(false, LWorkEqualizerStep.LWorkBandsCreate());
        IReadOnlyList<(double, double)> lBands = LEqualizerOutlet.LEqualizerPairsRead(
            lStep is LWorkEqualizerStep lEqualizer
                ? lEqualizer.LWorkEqualizerBands
                : LWorkEqualizerStep.LWorkBandsCreate());
        return (lStep.LWorkStepActive, lBands, LEqualizerOutlet.LEqualizerMatchFind(lBands));
    }

    // Noise values in the noise gate's slot order, held inside their ranges; a step that is not a noise step reads
    // as the default noise step with its switch.
    public (bool LInspectorActive, IReadOnlyList<double> LInspectorValues, bool LInspectorTrack, int LInspectorType,
        string? LInspectorPreset) LInspectorNoiseRead()
    {
        LWorkAudioStep lStep = LInspectorAudioFind(LAudioKind.LAudioKindDenoise)
            ?? LWorkAudioStep.LWorkNoiseCreate(false, 12, -50, false, LGrain.LGrainWhite, 6, 0.5, -38);
        var lSource = lStep as LWorkNoiseStep
            ?? (LWorkNoiseStep)LWorkAudioStep.LWorkNoiseCreate(
                lStep.LWorkStepActive, 12, -50, false, LGrain.LGrainWhite, 6, 0.5, -38);
        var lNoise = new LNoiseOutlet();
        IReadOnlyList<double> lValues = lNoise.LNoiseNormalize(LNoiseOutlet.LNoiseValuesRead(lSource));
        int lType = LNoiseOutlet.LNoiseTypeRead(lSource);
        return (
            lSource.LWorkStepActive, lValues, lSource.LWorkNoiseTrack, lType, lNoise.LNoiseMatchFind(lValues, lType));
    }

    public LWhitebalanceAnswer LInspectorWhitebalanceRead() =>
        LWhitebalanceOutlet.LWhitebalanceAnswerRead(
            LInspectorVideoFind(LColorKind.LColorKindWhitebalance) ?? LWorkVideoStep.LWorkWhitebalanceCreate(false));
    // A side's values in the filter gate's slot order, held inside their ranges; a plan without the side's step reads
    // as the side's default step switched off, and a step that is not a pass step as that default with its switch.
    public (bool LInspectorActive, IReadOnlyList<double> LInspectorValues, int LInspectorPoles,
        string? LInspectorPreset) LInspectorFilterRead(bool lHigh)
    {
        LAudioKind lKind = lHigh ? LAudioKind.LAudioKindHighpass : LAudioKind.LAudioKindLowpass;
        LWorkAudioStep lStep = LInspectorAudioFind(lKind) ?? LPassband.LPassbandStepCreate(lHigh, false);
        var lSource = lStep as LWorkPassStep
            ?? (LWorkPassStep)LPassband.LPassbandStepCreate(lHigh, lStep.LWorkStepActive);
        var lFilter = new LFilterOutlet();
        (IReadOnlyList<double> lValues, int lPoles) =
            lFilter.LFilterNormalize(lHigh, LFilterOutlet.LFilterValuesRead(lSource), lSource.LWorkPassPoles);
        return (lSource.LWorkStepActive, lValues, lPoles, lFilter.LFilterMatchFind(lHigh, lValues, lPoles));
    }

    private LWorkVideoStep? LInspectorVideoFind(LColorKind lKind) =>
        _lInspectorVideo.LWorkVideoSteps.FirstOrDefault(lStep => lStep.LWorkStepKind == lKind);

    private LWorkAudioStep? LInspectorAudioFind(LAudioKind lKind) =>
        _lInspectorAudio.LWorkAudioSteps.FirstOrDefault(lStep => lStep.LWorkStepKind == lKind);
}
