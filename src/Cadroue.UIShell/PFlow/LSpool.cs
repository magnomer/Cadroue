namespace Cadroue.UIShell.PFlow;

public sealed class LSpool
{
    private static readonly TimeSpan lSpoolRangeMinimum = TimeSpan.FromSeconds(5);

    public TimeSpan LSpoolWorkingRangeStart { get; private set; }
    public TimeSpan LSpoolWorkingRangeEnd { get; private set; }
    public TimeSpan LSpoolDuration { get; private set; }

    public LSpool(TimeSpan duration)
    {
        LSpoolDuration = duration < TimeSpan.Zero ? TimeSpan.Zero : duration;
        LSpoolWorkingRangeStart = TimeSpan.Zero;
        LSpoolWorkingRangeEnd = LSpoolDuration;
    }

    public void LSpoolReset()
    {
        LSpoolWorkingRangeStart = TimeSpan.Zero;
        LSpoolWorkingRangeEnd = LSpoolDuration;
    }

    public void LSpoolNormalize()
    {
        if (LSpoolDuration <= TimeSpan.Zero)
        {
            LSpoolWorkingRangeStart = TimeSpan.Zero;
            LSpoolWorkingRangeEnd = TimeSpan.Zero;
            return;
        }

        LSpoolWorkingRangeStart = LSpoolTimeClamp(LSpoolWorkingRangeStart);
        LSpoolWorkingRangeEnd = LSpoolTimeClamp(LSpoolWorkingRangeEnd);

        if (LSpoolWorkingRangeEnd < LSpoolWorkingRangeStart)
        {
            var center = LSpoolWorkingRangeStart + (LSpoolWorkingRangeEnd - LSpoolWorkingRangeStart) / 2;
            LSpoolWorkingRangeStart = center;
            LSpoolWorkingRangeEnd = center;
        }

        var rangeMinimum = LSpoolMinimumRead();
        if (LSpoolWorkingRangeEnd - LSpoolWorkingRangeStart < rangeMinimum)
        {
            var center = LSpoolWorkingRangeStart + (LSpoolWorkingRangeEnd - LSpoolWorkingRangeStart) / 2;
            LSpoolWorkingRangeStart = center - rangeMinimum / 2;
            LSpoolWorkingRangeEnd = center + rangeMinimum / 2;

            if (LSpoolWorkingRangeStart < TimeSpan.Zero)
            {
                LSpoolWorkingRangeStart = TimeSpan.Zero;
                LSpoolWorkingRangeEnd = rangeMinimum;
            }

            if (LSpoolWorkingRangeEnd > LSpoolDuration)
            {
                LSpoolWorkingRangeEnd = LSpoolDuration;
                LSpoolWorkingRangeStart = LSpoolDuration - rangeMinimum;
            }
        }
    }

    public void LSpoolMove(TimeSpan delta)
    {
        var span = LSpoolWorkingRangeEnd - LSpoolWorkingRangeStart;
        if (span > LSpoolDuration)
        {
            span = LSpoolDuration;
        }

        LSpoolWorkingRangeStart += delta;
        LSpoolWorkingRangeEnd = LSpoolWorkingRangeStart + span;

        if (LSpoolWorkingRangeStart < TimeSpan.Zero)
        {
            LSpoolWorkingRangeStart = TimeSpan.Zero;
            LSpoolWorkingRangeEnd = span;
        }

        if (LSpoolWorkingRangeEnd > LSpoolDuration)
        {
            LSpoolWorkingRangeEnd = LSpoolDuration;
            LSpoolWorkingRangeStart = LSpoolDuration - span;
        }

        LSpoolNormalize();
    }

    public void LSpoolStartResize(TimeSpan newStart)
    {
        LSpoolWorkingRangeStart = newStart;
        LSpoolNormalize();
    }

    public void LSpoolEndResize(TimeSpan newEnd)
    {
        LSpoolWorkingRangeEnd = newEnd;
        LSpoolNormalize();
    }

    public void LSpoolCenterSet(TimeSpan center)
    {
        var halfSpan = (LSpoolWorkingRangeEnd - LSpoolWorkingRangeStart) / 2;
        LSpoolWorkingRangeStart = center - halfSpan;
        LSpoolWorkingRangeEnd = center + halfSpan;
        LSpoolNormalize();
    }

    public TimeSpan LSpoolTimeConvert(double ratio)
        => TimeSpan.FromSeconds(ratio * LSpoolDuration.TotalSeconds);

    public double LSpoolRatioConvert(TimeSpan t)
        => LSpoolDuration.TotalSeconds > 0 ? t.TotalSeconds / LSpoolDuration.TotalSeconds : 0;

    public void LSpoolInZoom(TimeSpan cursor)
    {
        var span = LSpoolWorkingRangeEnd - LSpoolWorkingRangeStart;
        if (span <= TimeSpan.Zero)
        {
            LSpoolNormalize();
            span = LSpoolWorkingRangeEnd - LSpoolWorkingRangeStart;
        }

        var newSpan = span / 2;
        var rangeMinimum = LSpoolMinimumRead();
        if (newSpan < rangeMinimum) newSpan = rangeMinimum;
        double ratio = span.TotalSeconds > 0 ? (cursor - LSpoolWorkingRangeStart).TotalSeconds / span.TotalSeconds : 0.5;
        ratio = Math.Clamp(ratio, 0, 1);
        LSpoolWorkingRangeStart = cursor - newSpan * ratio;
        LSpoolWorkingRangeEnd = cursor + newSpan * (1 - ratio);
        LSpoolNormalize();
    }

    public void LSpoolOutZoom(TimeSpan cursor)
    {
        var span = LSpoolWorkingRangeEnd - LSpoolWorkingRangeStart;
        if (span <= TimeSpan.Zero)
        {
            LSpoolNormalize();
            span = LSpoolWorkingRangeEnd - LSpoolWorkingRangeStart;
        }

        var newSpan = span * 2;
        if (newSpan > LSpoolDuration) newSpan = LSpoolDuration;
        double ratio = span.TotalSeconds > 0 ? (cursor - LSpoolWorkingRangeStart).TotalSeconds / span.TotalSeconds : 0.5;
        ratio = Math.Clamp(ratio, 0, 1);
        LSpoolWorkingRangeStart = cursor - newSpan * ratio;
        LSpoolWorkingRangeEnd = cursor + newSpan * (1 - ratio);
        LSpoolNormalize();
    }

    private TimeSpan LSpoolMinimumRead()
        => LSpoolDuration < lSpoolRangeMinimum ? LSpoolDuration : lSpoolRangeMinimum;

    private TimeSpan LSpoolTimeClamp(TimeSpan time)
        => time < TimeSpan.Zero ? TimeSpan.Zero : time > LSpoolDuration ? LSpoolDuration : time;
}
