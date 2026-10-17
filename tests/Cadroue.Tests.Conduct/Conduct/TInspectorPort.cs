using System.Collections.Generic;
using Cadroue.ShellEngine;

namespace Cadroue.Tests;

internal sealed class TInspectorPort : LInspectorPort
{
    public (bool, IReadOnlyList<double>) TInspectorGamma { get; init; } = (false, [0, 0, 0, 0, 0]);

    public (bool, double) TInspectorExposure { get; init; }

    public (bool, double) TInspectorVolume { get; init; }

    public (IReadOnlyList<bool>, IReadOnlyList<double>) TInspectorTone { get; init; } =
        ([false, false, false], [0, 100, 100]);

    public (bool, IReadOnlyList<(double, double)>, string?) TInspectorEqualizer { get; init; } = (false, [], null);

    public (bool, IReadOnlyList<double>, bool, int, string?) TInspectorNoise { get; init; } =
        (false, [12, 6, -50, -38, 0.5], false, 0, "Medium");
    public LWhitebalanceAnswer TInspectorWhitebalance { get; init; } = new(false, "Median", 100, 1, 1, 1, 0, 0, 0);

    public (bool, IReadOnlyList<double>, int, string?) TInspectorFilter { get; init; } =
        (false, [80, 2, 0.707], 2, "Voice");

    public List<string> TInspectorCalls { get; } = [];

    public (bool LInspectorActive, IReadOnlyList<double> LInspectorValues, int LInspectorPoles,
        string? LInspectorPreset) LInspectorFilterRead(bool lHigh)
    {
        TInspectorCalls.Add(lHigh ? "highpass" : "lowpass");
        return TInspectorFilter;
    }

    public (bool LInspectorActive, IReadOnlyList<double> LInspectorValues, bool LInspectorTrack, int LInspectorType,
        string? LInspectorPreset) LInspectorNoiseRead()
    {
        TInspectorCalls.Add("noise");
        return TInspectorNoise;
    }

    public (bool LInspectorActive, IReadOnlyList<(double, double)> LInspectorBands, string? LInspectorPreset)
        LInspectorEqualizerRead()
    {
        TInspectorCalls.Add("equalizer");
        return TInspectorEqualizer;
    }

    public (bool LInspectorActive, IReadOnlyList<double> LInspectorValues) LInspectorGammaRead()
    {
        TInspectorCalls.Add("gamma");
        return TInspectorGamma;
    }

    public (bool LInspectorActive, double LInspectorValue) LInspectorExposureRead()
    {
        TInspectorCalls.Add("exposure");
        return TInspectorExposure;
    }

    public (bool LInspectorActive, double LInspectorValue) LInspectorVolumeRead()
    {
        TInspectorCalls.Add("volume");
        return TInspectorVolume;
    }

    public (IReadOnlyList<bool> LInspectorActives, IReadOnlyList<double> LInspectorValues) LInspectorToneRead()
    {
        TInspectorCalls.Add("tone");
        return TInspectorTone;
    }

    public LWhitebalanceAnswer LInspectorWhitebalanceRead()
    {
        TInspectorCalls.Add("whitebalance");
        return TInspectorWhitebalance;
    }
}
