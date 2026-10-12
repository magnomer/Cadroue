using Cadroue.Core;
using Cadroue.Media;

namespace Cadroue.Infrastructure;

public static class LRigFactoryTool
{
    public static LRigTool LRigToolCreate()
    {
        return new LRigTool(
            LTool.LToolFfmpegRead,
            LTool.LToolFfprobeRead,
            () => LRenderer.LRendererProgramCurrent);
    }
}
