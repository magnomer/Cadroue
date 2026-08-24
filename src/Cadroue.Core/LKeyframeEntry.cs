namespace Cadroue.Core;

public sealed record LKeyframeEntry
{
    public LKeyframeEntry(TimeSpan presentationTime, TimeSpan? decodeTime = null)
    {
        if (presentationTime < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(presentationTime), "Keyframe presentation time cannot be negative.");
        }

        LKeyframePresentationTime = presentationTime;
        LKeyframeDecodeTime = decodeTime;
    }

    public TimeSpan LKeyframePresentationTime { get; }

    public TimeSpan? LKeyframeDecodeTime { get; }
}
