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
}
