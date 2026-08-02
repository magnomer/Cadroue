using System.Text.Json.Serialization;

namespace Cadroue.Core;

public enum LAudioKind
{
    LAudioKindVolume,
    LAudioKindNormalize,
    LAudioKindDenoise,
    LAudioKindHighpass,
    LAudioKindLowpass,
    LAudioKindEqualizer
}

public enum LLeveling
{
    LLevelingLoudness,
    LLevelingDynamic
}

public enum LGrain
{
    LGrainWhite,
    LGrainVinyl,
    LGrainShellac
}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "LWorkStepType")]
[JsonDerivedType(typeof(LWorkVolumeStep), "Volume")]
[JsonDerivedType(typeof(LWorkNormalizeStep), "Normalize")]
[JsonDerivedType(typeof(LWorkNoiseStep), "Noise")]
[JsonDerivedType(typeof(LWorkPassStep), "Pass")]
[JsonDerivedType(typeof(LWorkEqualizerStep), "Equalizer")]
public abstract record LWorkAudioStep(LAudioKind LWorkStepKind, bool LWorkStepActive)
{
    [JsonIgnore]
    public virtual bool LWorkStepLoudness => false;

    public static LWorkAudioStep LWorkVolumeCreate(bool lStepActive, double lStepGain) =>
        new LWorkVolumeStep(lStepActive, lStepGain);

    public static LWorkAudioStep LWorkNormalizeCreate(
        bool lStepActive,
        LLeveling lStepMode,
        double lStepTarget,
        double lStepPeak,
        double lStepRange,
        bool lStepTwoPass,
        double lStepFrame = 300,
        double lStepGauss = 21,
        double lStepMaxGain = 10,
        double lStepCompress = 6) =>
        new LWorkNormalizeStep(
            lStepActive, lStepMode, lStepTarget, lStepPeak, lStepRange, lStepTwoPass,
            lStepFrame, lStepGauss, lStepMaxGain, lStepCompress);

    public static LWorkAudioStep LWorkNoiseCreate(
        bool lStepActive,
        double lStepReduction,
        double lStepNoiseFloor,
        bool lStepTrackNoise,
        LGrain lStepNoiseType,
        double lStepGainSmooth,
        double lStepAdaptivity,
        double lStepResidualFloor) =>
        new LWorkNoiseStep(
            lStepActive, lStepReduction, lStepNoiseFloor, lStepTrackNoise,
            lStepNoiseType, lStepGainSmooth, lStepAdaptivity, lStepResidualFloor);

    public static LWorkAudioStep LWorkHighCreate(
        bool lStepActive, double lStepFrequency, int lStepStages, int lStepPoles, double lStepResonance) =>
        new LWorkPassStep(
            LAudioKind.LAudioKindHighpass, lStepActive, true, lStepFrequency, lStepStages, lStepPoles, lStepResonance);

    public static LWorkAudioStep LWorkLowCreate(
        bool lStepActive, double lStepFrequency, int lStepStages, int lStepPoles, double lStepResonance) =>
        new LWorkPassStep(
            LAudioKind.LAudioKindLowpass, lStepActive, false, lStepFrequency, lStepStages, lStepPoles, lStepResonance);

    public static LWorkAudioStep LWorkEqualizerCreate(
        bool lStepActive, IReadOnlyList<LWorkBand> lStepBands) =>
        new LWorkEqualizerStep(lStepActive, lStepBands);
}

public sealed record LWorkAudio(IReadOnlyList<LWorkAudioStep> LWorkAudioSteps)
{
    public bool LWorkAudioSkip { get; init; }

    public static LWorkAudio LWorkAudioCreate() => new(Array.Empty<LWorkAudioStep>());

    public bool LWorkAudioActive => LWorkAudioSkip || LWorkAudioSteps.Any(lStep => lStep.LWorkStepActive);
}
