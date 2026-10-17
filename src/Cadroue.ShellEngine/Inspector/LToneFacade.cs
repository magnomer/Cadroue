using Cadroue.Core;

namespace Cadroue.ShellEngine;

// The preview facts the tone gate decides its notice from: whether Flyleaf runs and whether the preview engine is mpv,
// each from the rig's preview reads.
public sealed class LToneFacade
{
    private readonly LRigPreview _lToneFacadePreview;

    internal LToneFacade(LRigPreview lPreview)
    {
        ArgumentNullException.ThrowIfNull(lPreview);
        _lToneFacadePreview = lPreview;
    }

    public (bool LToneFlyleaf, bool LToneMpv) LTonePreviewRead() =>
        (_lToneFacadePreview.LRigPreviewFlyleaf(),
            _lToneFacadePreview.LRigPreviewEngine() == LPreviewEngine.LPreviewEngineMpv);
}
