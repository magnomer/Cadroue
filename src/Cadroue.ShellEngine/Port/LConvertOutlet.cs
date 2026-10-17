namespace Cadroue.ShellEngine;

// The engine's face for the convert gate: every member forwards to the same-named member of the engine's convert
// facade. It rejects a null engine and keeps it for every forward.
public sealed class LConvertOutlet : LConvertPort
{
    private readonly LEngine _lConvertOutletEngine;

    public LConvertOutlet(LEngine lEngine)
    {
        ArgumentNullException.ThrowIfNull(lEngine);
        _lConvertOutletEngine = lEngine;
    }

    public void LConvertRun(
        string lPresetName,
        IReadOnlyList<(string LConvertPath, Guid LConvertBatch)> lSources,
        bool lUrgent,
        Guid lRelayTarget,
        Guid lSourceTab) =>
        _lConvertOutletEngine.LEngineConvert.LConvertRun(lPresetName, lSources, lUrgent, lRelayTarget, lSourceTab);
}
