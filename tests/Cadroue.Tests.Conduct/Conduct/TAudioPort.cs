using System.Collections.Generic;
using Cadroue.ShellEngine;

namespace Cadroue.Tests;

// Records the persistent or ordered plan the gate asks for and answers one plan read.
internal sealed class TAudioPort : LAudioPort
{
    public TInspectorPort TAudioPlan { get; } = new();

    public List<string> TAudioCalls { get; } = [];

    public ((bool, IReadOnlyList<double>, int)?, (bool, IReadOnlyList<double>, int)?,
        (bool, IReadOnlyList<double>, bool, int)?, (bool, double)?, (bool, bool, IReadOnlyList<double>, bool)?,
        (bool, IReadOnlyList<(double, double)>)?) TAudioTools { get; private set; }

    public bool TAudioSkip { get; private set; }

    public LInspectorPort LAudioPlanCreate(
        IReadOnlyList<string> lProcessing,
        (bool LAudioActive, IReadOnlyList<double> LAudioValues, int LAudioPoles) lHighpass,
        (bool LAudioActive, IReadOnlyList<double> LAudioValues, int LAudioPoles) lLowpass,
        (bool LAudioActive, IReadOnlyList<double> LAudioValues, bool LAudioTrack, int LAudioType) lNoise,
        (bool LAudioActive, double LAudioGain) lVolume,
        (bool LAudioActive, bool LAudioDynamic, IReadOnlyList<double> LAudioValues, bool LAudioTwoPass) lLeveling,
        (bool LAudioActive, IReadOnlyList<(double, double)> LAudioBands) lEqualizer,
        bool lSkip)
    {
        TAudioCalls.Add("plan");
        TAudioTools = (lHighpass, lLowpass, lNoise, lVolume, lLeveling, lEqualizer);
        TAudioSkip = lSkip;
        return TAudioPlan;
    }

    public LInspectorPort LAudioPersistentCreate(
        (bool LAudioActive, IReadOnlyList<double> LAudioValues, int LAudioPoles)? lHighpass,
        (bool LAudioActive, IReadOnlyList<double> LAudioValues, int LAudioPoles)? lLowpass,
        (bool LAudioActive, IReadOnlyList<double> LAudioValues, bool LAudioTrack, int LAudioType)? lNoise,
        (bool LAudioActive, double LAudioGain)? lVolume,
        (bool LAudioActive, bool LAudioDynamic, IReadOnlyList<double> LAudioValues, bool LAudioTwoPass)? lLeveling,
        (bool LAudioActive, IReadOnlyList<(double, double)> LAudioBands)? lEqualizer,
        bool lSkip)
    {
        TAudioCalls.Add("persistent");
        TAudioTools = (lHighpass, lLowpass, lNoise, lVolume, lLeveling, lEqualizer);
        TAudioSkip = lSkip;
        return TAudioPlan;
    }
}
