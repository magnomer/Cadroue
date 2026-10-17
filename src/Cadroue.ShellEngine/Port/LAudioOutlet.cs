namespace Cadroue.ShellEngine;

// The engine's face for the audio plans: every member forwards to the same-named member of the engine's audio
// facade. It rejects a null engine and keeps it for every forward.
public sealed class LAudioOutlet : LAudioPort
{
    private readonly LEngine _lAudioOutletEngine;

    public LAudioOutlet(LEngine lEngine)
    {
        ArgumentNullException.ThrowIfNull(lEngine);
        _lAudioOutletEngine = lEngine;
    }

    public LInspectorPort LAudioPlanCreate(
        IReadOnlyList<string> lProcessing,
        (bool LAudioActive, IReadOnlyList<double> LAudioValues, int LAudioPoles) lHighpass,
        (bool LAudioActive, IReadOnlyList<double> LAudioValues, int LAudioPoles) lLowpass,
        (bool LAudioActive, IReadOnlyList<double> LAudioValues, bool LAudioTrack, int LAudioType) lNoise,
        (bool LAudioActive, double LAudioGain) lVolume,
        (bool LAudioActive, bool LAudioDynamic, IReadOnlyList<double> LAudioValues, bool LAudioTwoPass) lLeveling,
        (bool LAudioActive, IReadOnlyList<(double, double)> LAudioBands) lEqualizer,
        bool lSkip) =>
        _lAudioOutletEngine.LEngineAudio.LAudioPlanCreate(
            lProcessing, lHighpass, lLowpass, lNoise, lVolume, lLeveling, lEqualizer, lSkip);

    public LInspectorPort LAudioPersistentCreate(
        (bool LAudioActive, IReadOnlyList<double> LAudioValues, int LAudioPoles)? lHighpass,
        (bool LAudioActive, IReadOnlyList<double> LAudioValues, int LAudioPoles)? lLowpass,
        (bool LAudioActive, IReadOnlyList<double> LAudioValues, bool LAudioTrack, int LAudioType)? lNoise,
        (bool LAudioActive, double LAudioGain)? lVolume,
        (bool LAudioActive, bool LAudioDynamic, IReadOnlyList<double> LAudioValues, bool LAudioTwoPass)? lLeveling,
        (bool LAudioActive, IReadOnlyList<(double, double)> LAudioBands)? lEqualizer,
        bool lSkip) =>
        _lAudioOutletEngine.LEngineAudio.LAudioPersistentCreate(
            lHighpass, lLowpass, lNoise, lVolume, lLeveling, lEqualizer, lSkip);
}
