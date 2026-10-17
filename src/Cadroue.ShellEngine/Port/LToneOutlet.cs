namespace Cadroue.ShellEngine;

// The engine's face for the tone gate: every member forwards to the same-named member of the engine's tone facade.
// It rejects a null engine and keeps it for every forward.
public sealed class LToneOutlet : LTonePort
{
    private readonly LEngine _lToneOutletEngine;

    public LToneOutlet(LEngine lEngine)
    {
        ArgumentNullException.ThrowIfNull(lEngine);
        _lToneOutletEngine = lEngine;
    }

    public (bool LToneFlyleaf, bool LToneMpv) LTonePreviewRead() =>
        _lToneOutletEngine.LEngineTone.LTonePreviewRead();
}
