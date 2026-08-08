namespace Cadroue.Core;

public static class LKeyframeView
{
    public static readonly TimeSpan LKeyframeRangeBefore = TimeSpan.FromMinutes(10);
    public static readonly TimeSpan LKeyframeRangeAfter = TimeSpan.FromMinutes(10);

    public static IReadOnlyList<LKeyframeEntry> LKeyframeVisibleResolve(
        IReadOnlyList<LKeyframeEntry> keyframes,
        TimeSpan cursor,
        LSpool spool)
    {
        TimeSpan windowStart = LKeyframeMaxResolve(spool.LSpoolRangeOrigin, cursor - LKeyframeRangeBefore);
        TimeSpan windowEnd = LKeyframeMinResolve(spool.LSpoolRangeLimit, cursor + LKeyframeRangeAfter);
        if (windowEnd <= windowStart)
        {
            return Array.Empty<LKeyframeEntry>();
        }

        var visible = new List<LKeyframeEntry>();
        foreach (LKeyframeEntry entry in keyframes)
        {
            if (entry.LKeyframePresentationTime >= windowStart && entry.LKeyframePresentationTime <= windowEnd)
            {
                visible.Add(entry);
            }
        }

        return visible;
    }

    private static TimeSpan LKeyframeMinResolve(TimeSpan first, TimeSpan second)
        => first <= second ? first : second;

    private static TimeSpan LKeyframeMaxResolve(TimeSpan first, TimeSpan second)
        => first >= second ? first : second;
}
