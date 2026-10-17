using Cadroue.Core;

namespace Cadroue.Infrastructure;

public static class LRigFactoryPreview
{
    public static LRigPreview LRigPreviewCreate()
    {
        return new LRigPreview(
            () => LFlyleaf.LFlyleafActive,
            LRenderer.LRendererEngineRead);
    }
}
