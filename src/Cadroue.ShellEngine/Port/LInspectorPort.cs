using Cadroue.Core;

namespace Cadroue.ShellEngine;

public interface LInspectorPort
{
    (bool LInspectorActive, IReadOnlyList<double> LInspectorValues) LInspectorGammaRead();

    (bool LInspectorActive, double LInspectorValue) LInspectorExposureRead();

    (bool LInspectorActive, double LInspectorValue) LInspectorVolumeRead();

    (IReadOnlyList<bool> LInspectorActives, IReadOnlyList<double> LInspectorValues) LInspectorToneRead();

    (bool LInspectorActive, IReadOnlyList<(double, double)> LInspectorBands, string? LInspectorPreset)
        LInspectorEqualizerRead();

    (bool LInspectorActive, IReadOnlyList<double> LInspectorValues, bool LInspectorTrack, int LInspectorType,
        string? LInspectorPreset) LInspectorNoiseRead();

    LWhitebalanceAnswer LInspectorWhitebalanceRead();
    (bool LInspectorActive, IReadOnlyList<double> LInspectorValues, int LInspectorPoles, string? LInspectorPreset)
        LInspectorFilterRead(bool lHigh);

    (bool LInspectorActive, bool LInspectorDynamic, IReadOnlyList<double> LInspectorValues, bool LInspectorTwoPass,
        string? LInspectorPreset) LInspectorLevelingRead();

    (bool LInspectorHighpass, bool LInspectorLowpass, bool LInspectorNoise, bool LInspectorVolume,
        bool LInspectorEqualizer, bool LInspectorLeveling) LInspectorPresentRead();

    LWorkAudio LInspectorAudioRead();

    (LWorkCrop LInspectorCrop, bool LInspectorActive) LInspectorCropRead();

    (bool LInspectorFixed, bool LInspectorLenient, int LInspectorWidth, int LInspectorHeight) LInspectorRatioRead();

    bool LInspectorSkipRead();

    (bool? LInspectorCrop, bool? LInspectorSkip) LInspectorKeepRead();
}
