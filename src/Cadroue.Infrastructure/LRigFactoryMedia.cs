using Cadroue.Core;
using Cadroue.Media;

namespace Cadroue.Infrastructure;

public static class LRigFactoryMedia
{
    public static LRigMedia LRigMediaCreate()
    {
        return new LRigMedia(
            LMedia.LMediaFfprobeRead,
            LMedia.LMediaLoudnessRead,
            LKeyframeSeeker.LKeyframeRangeScan,
            LMedia.LMediaScanClaim,
            LMedia.LMediaScanRelease);
    }
}
