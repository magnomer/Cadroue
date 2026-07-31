namespace Cadroue.Media;

public sealed record LKeyframeScanRange
{
    public LKeyframeScanRange(TimeSpan startTime, TimeSpan endTime)
    {
        if (startTime < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(startTime), "Keyframe scan range start time cannot be negative.");
        }

        if (endTime <= startTime)
        {
            throw new ArgumentOutOfRangeException(nameof(endTime), "Keyframe scan range end time must be greater than start time.");
        }

        LKeyframeRangeOrigin = startTime;
        LKeyframeRangeLimit = endTime;
    }

    public TimeSpan LKeyframeRangeOrigin { get; }

    public TimeSpan LKeyframeRangeLimit { get; }
}
