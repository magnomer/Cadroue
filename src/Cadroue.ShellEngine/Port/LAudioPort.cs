namespace Cadroue.ShellEngine;

// The audio plan rules for the inspector gate. Each tool arrives as its plain values, or null when the plan leaves it
// out; filter, noise and leveling values run in their gates' slot orders. The skip switch rides on the plan.
public interface LAudioPort
{
    LInspectorPort LAudioPersistentCreate(
        (bool LAudioActive, IReadOnlyList<double> LAudioValues, int LAudioPoles)? lHighpass,
        (bool LAudioActive, IReadOnlyList<double> LAudioValues, int LAudioPoles)? lLowpass,
        (bool LAudioActive, IReadOnlyList<double> LAudioValues, bool LAudioTrack, int LAudioType)? lNoise,
        (bool LAudioActive, double LAudioGain)? lVolume,
        (bool LAudioActive, bool LAudioDynamic, IReadOnlyList<double> LAudioValues, bool LAudioTwoPass)? lLeveling,
        (bool LAudioActive, IReadOnlyList<(double, double)> LAudioBands)? lEqualizer,
        bool lSkip);
}
