using Cadroue.Core;

namespace Cadroue.ShellEngine;

// The preview facts the tone gate decides its notice from: whether Flyleaf runs and whether the preview engine is mpv,
// each from a platform read the host hands in.
public sealed class LToneOutlet : LTonePort
{
    private readonly Func<bool> _lToneOutletFlyleaf;

    private readonly Func<LPreviewEngine> _lToneOutletEngine;

    public LToneOutlet(Func<bool> lFlyleaf, Func<LPreviewEngine> lEngine)
    {
        ArgumentNullException.ThrowIfNull(lFlyleaf);
        ArgumentNullException.ThrowIfNull(lEngine);
        _lToneOutletFlyleaf = lFlyleaf;
        _lToneOutletEngine = lEngine;
    }

    public (bool LToneFlyleaf, bool LToneMpv) LTonePreviewRead() =>
        (_lToneOutletFlyleaf(), _lToneOutletEngine() == LPreviewEngine.LPreviewEngineMpv);
}
