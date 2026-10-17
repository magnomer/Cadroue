using Cadroue.Core;
using Cadroue.Media;

namespace Cadroue.Infrastructure;

public static class LRigFactoryMedia
{
    public static LRigMedia LRigMediaCreate()
    {
        return new LRigMedia(
            LMediaFfprobe.LMediaFfprobeRead,
            LMediaLoudness.LMediaLoudnessRead,
            LKeyframeSeeker.LKeyframeRangeScan,
            LMediaSlot.LMediaScanClaim,
            LMediaSlot.LMediaScanRelease,
            LMedia.LMediaFileRead,
            LMedia.LMediaCheck);
    }
}
