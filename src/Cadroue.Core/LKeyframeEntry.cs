namespace Cadroue.Core;

public sealed record LKeyframeEntry
{
    public LKeyframeEntry(TimeSpan presentationTime)
    {
        if (presentationTime < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(presentationTime), "Keyframe presentation time cannot be negative.");
        }

        LKeyframePresentationTime = presentationTime;
    }

    public TimeSpan LKeyframePresentationTime { get; }
}
