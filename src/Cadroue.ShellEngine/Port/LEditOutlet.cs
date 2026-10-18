namespace Cadroue.ShellEngine;

// The engine's face for the edit gate: every member forwards to the same-named member of the engine's edit dispatch
// or edit audio facade. It rejects a null engine and keeps it for every forward.
public sealed class LEditOutlet : LEditPort
{
    private readonly LEngine _lEditOutletEngine;

    public LEditOutlet(LEngine lEngine)
    {
        ArgumentNullException.ThrowIfNull(lEngine);
        _lEditOutletEngine = lEngine;
    }

    public void LEditAllRun(
        string lPresetName,
        IReadOnlyList<(string LEditPath, Guid LEditBatch)> lSources,
        Guid lRelayTarget,
        Guid lSourceTab) =>
        _lEditOutletEngine.LEngineEdit.LEditAllRun(lPresetName, lSources, lRelayTarget, lSourceTab);

    public void LEditAudioSave(
        LInspectorPort lPersistent,
        IReadOnlyList<string> lPaths,
        bool lSkipPersistent,
        bool lSkipApply) =>
        _lEditOutletEngine.LEngineEditAudio.LEditAudioSave(lPersistent, lPaths, lSkipPersistent, lSkipApply);
}
