using Cadroue.Core;

namespace Cadroue.UIVeneer.PWing;

public sealed partial class PInspector
{
    public bool PTonePersistentCheck() => LInspector.LInspectorPersistentCheck();

    public void PTonePersistentApply(LWorkVideo pVideo) => LInspector.LInspectorPersistentApply(pVideo);

    public LWorkVideo PTonePersistentRead() => LInspector.LInspectorPersistentRead();
}
