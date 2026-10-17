using Cadroue.Core;

namespace Cadroue.ShellEngine;

// The audio plan rules for the inspector gate. Each tool arrives as its plain values, or null when the plan leaves it
// out; filter, noise and leveling values run in their gates' slot orders. The skip switch rides on the plan. A plan in
// the caller's order takes every tool and keeps a step for each processing step token that names one.
public interface LAudioPort
{
    LInspectorPort LAudioPlanCreate(
        IReadOnlyList<string> lProcessing,
        (bool LAudioActive, IReadOnlyList<double> LAudioValues, int LAudioPoles) lHighpass,
        (bool LAudioActive, IReadOnlyList<double> LAudioValues, int LAudioPoles) lLowpass,
        (bool LAudioActive, IReadOnlyList<double> LAudioValues, bool LAudioTrack, int LAudioType) lNoise,
        (bool LAudioActive, double LAudioGain) lVolume,
        (bool LAudioActive, bool LAudioDynamic, IReadOnlyList<double> LAudioValues, bool LAudioTwoPass) lLeveling,
        (bool LAudioActive, IReadOnlyList<(double, double)> LAudioBands) lEqualizer,
        bool lSkip);

    LInspectorPort LAudioPersistentCreate(
        (bool LAudioActive, IReadOnlyList<double> LAudioValues, int LAudioPoles)? lHighpass,
        (bool LAudioActive, IReadOnlyList<double> LAudioValues, int LAudioPoles)? lLowpass,
        (bool LAudioActive, IReadOnlyList<double> LAudioValues, bool LAudioTrack, int LAudioType)? lNoise,
        (bool LAudioActive, double LAudioGain)? lVolume,
        (bool LAudioActive, bool LAudioDynamic, IReadOnlyList<double> LAudioValues, bool LAudioTwoPass)? lLeveling,
        (bool LAudioActive, IReadOnlyList<(double, double)> LAudioBands)? lEqualizer,
        bool lSkip);

    // The audio tool a processing step token names; none for a token that names no audio tool. Static, since it reads
    // only the token given.
    static LAudioKind? LAudioKindRead(string lToken) => LAudioFacade.LAudioKindRead(lToken);
}
