using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.ShellEngine;

// One plan read for the inspector gate: each tool's step as plain values, or the Core default step when the plan has
// none. A video plan answers the audio tools from an empty audio plan, and the reverse. An edit plan is a video plan
// with its crop, ratio and skip; any other plan answers the empty edit plan's crop and ratio.
public sealed class LInspectorOutlet : LInspectorPort
{
    private readonly LWorkVideo _lInspectorVideo;
    private readonly LWorkAudio _lInspectorAudio;
    private readonly LEditPlan _lInspectorEdit;
    private readonly bool _lInspectorSkip;
    private readonly (bool?, bool?) _lInspectorKeep;

    public LInspectorOutlet(LWorkVideo lVideo)
    {
        ArgumentNullException.ThrowIfNull(lVideo);
        _lInspectorVideo = lVideo;
        _lInspectorAudio = new LWorkAudio(new List<LWorkAudioStep>());
        _lInspectorEdit = LEditPlan.LEditEmptyCreate();
    }

    public LInspectorOutlet(LWorkAudio lAudio)
    {
        ArgumentNullException.ThrowIfNull(lAudio);
        _lInspectorVideo = LWorkVideo.LWorkVideoCreate();
        _lInspectorAudio = lAudio;
        _lInspectorEdit = LEditPlan.LEditEmptyCreate();
        _lInspectorSkip = lAudio.LWorkAudioSkip;
    }

    public LInspectorOutlet(LEditPlan lPlan)
    {
        ArgumentNullException.ThrowIfNull(lPlan);
        _lInspectorVideo = lPlan.LEditVideo;
        _lInspectorAudio = new LWorkAudio(new List<LWorkAudioStep>());
        _lInspectorEdit = lPlan;
        _lInspectorSkip = lPlan.LEditSkip;
    }

    // A kept layout's edit plan: its crop counts only when the crop is kept, and the skip switch is kept as told.
    public LInspectorOutlet(LEditPlan lPlan, bool lCropKept, bool lSkipKept)
        : this(lPlan)
    {
        _lInspectorKeep = (lCropKept, lSkipKept);
    }

    // What the plan keeps: a null crop applies without keeping, true applies and keeps, false leaves the crop as it
    // is; a null skip leaves the kept switch as it is.
    public (bool? LInspectorCrop, bool? LInspectorSkip) LInspectorKeepRead() => _lInspectorKeep;

    // The edit plan's crop and whether it applies.
    public (LWorkCrop LInspectorCrop, bool LInspectorActive) LInspectorCropRead() =>
        (_lInspectorEdit.LEditCrop, _lInspectorEdit.LEditCropActive);

    // The edit plan's ratio as stored; the gate decides whether it holds.
    public (bool LInspectorFixed, bool LInspectorLenient, int LInspectorWidth, int LInspectorHeight)
        LInspectorRatioRead() =>
        (_lInspectorEdit.LEditRatioFixed, _lInspectorEdit.LEditRatioLenient, _lInspectorEdit.LEditRatioWidth,
            _lInspectorEdit.LEditRatioHeight);

    // Whether the plan skips processing; a bare video plan never does.
    public bool LInspectorSkipRead() => _lInspectorSkip;

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

    // The leveling values in the leveling gate's slot order, held inside their ranges; a plan without a leveling step
    // reads as the default step switched off, and a step that is not a leveling step as that default with its switch.
    public (bool LInspectorActive, bool LInspectorDynamic, IReadOnlyList<double> LInspectorValues,
        bool LInspectorTwoPass, string? LInspectorPreset) LInspectorLevelingRead()
    {
        LWorkAudioStep lStep = LInspectorAudioFind(LAudioKind.LAudioKindLeveling)
            ?? LLoudnessOutlet.LLoudnessDefaultCreate(false);
        LWorkNormalizeStep lSource = lStep as LWorkNormalizeStep
            ?? LLoudnessOutlet.LLoudnessDefaultCreate(lStep.LWorkStepActive);
        var lLoudness = new LLoudnessOutlet();
        bool lDynamic = lSource.LWorkNormalizeMode == LLeveling.LLevelingDynamic;
        IReadOnlyList<double> lValues = lLoudness.LLoudnessNormalize(LLoudnessOutlet.LLoudnessValuesRead(lSource));
        return (lSource.LWorkStepActive, lDynamic, lValues, lSource.LWorkTwoPass,
            lLoudness.LLoudnessMatchFind(lDynamic, lValues));
    }

    // Which of the gated audio tools the plan carries a step for.
    public (bool LInspectorHighpass, bool LInspectorLowpass, bool LInspectorNoise, bool LInspectorVolume,
        bool LInspectorEqualizer, bool LInspectorLeveling) LInspectorPresentRead() =>
        (LInspectorAudioFind(LAudioKind.LAudioKindHighpass) is not null,
            LInspectorAudioFind(LAudioKind.LAudioKindLowpass) is not null,
            LInspectorAudioFind(LAudioKind.LAudioKindDenoise) is not null,
            LInspectorAudioFind(LAudioKind.LAudioKindVolume) is not null,
            LInspectorAudioFind(LAudioKind.LAudioKindEqualizer) is not null,
            LInspectorAudioFind(LAudioKind.LAudioKindLeveling) is not null);

    // The audio plan as the read holds it.
    public LWorkAudio LInspectorAudioRead() => _lInspectorAudio;

    private LWorkVideoStep? LInspectorVideoFind(LColorKind lKind) =>
        _lInspectorVideo.LWorkVideoSteps.FirstOrDefault(lStep => lStep.LWorkStepKind == lKind);

    private LWorkAudioStep? LInspectorAudioFind(LAudioKind lKind) =>
        _lInspectorAudio.LWorkAudioSteps.FirstOrDefault(lStep => lStep.LWorkStepKind == lKind);
}
