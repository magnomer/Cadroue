using System.Collections.Generic;
using Cadroue.ShellEngine;

namespace Cadroue.Tests;

internal sealed class TInspectorPort : LInspectorPort
{
    public (bool, IReadOnlyList<double>) TInspectorGamma { get; init; } = (false, [0, 0, 0, 0, 0]);

    public (bool, double) TInspectorExposure { get; init; }

    public (bool, double) TInspectorVolume { get; init; }

    public List<string> TInspectorCalls { get; } = [];

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
}
