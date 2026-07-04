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

    public void LSpoolCorrect()
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

        var rangeMinimum = LSpoolRangeMinimumGet();
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

        LSpoolCorrect();
    }

    public void LSpoolStartResize(TimeSpan newStart)
    {
        LSpoolWorkingRangeStart = newStart;
        LSpoolCorrect();
    }

    public void LSpoolEndResize(TimeSpan newEnd)
    {
        LSpoolWorkingRangeEnd = newEnd;
        LSpoolCorrect();
    }

    public void LSpoolCenterSet(TimeSpan center)
    {
        var halfSpan = (LSpoolWorkingRangeEnd - LSpoolWorkingRangeStart) / 2;
        LSpoolWorkingRangeStart = center - halfSpan;
        LSpoolWorkingRangeEnd = center + halfSpan;
        LSpoolCorrect();
    }

    public TimeSpan LSpoolRatioDurationConvert(double ratio)
        => TimeSpan.FromSeconds(ratio * LSpoolDuration.TotalSeconds);

    public double LSpoolDurationRatioConvert(TimeSpan t)
        => LSpoolDuration.TotalSeconds > 0 ? t.TotalSeconds / LSpoolDuration.TotalSeconds : 0;

    public void LSpoolZoomIn(TimeSpan cursor)
    {
        var span = LSpoolWorkingRangeEnd - LSpoolWorkingRangeStart;
        if (span <= TimeSpan.Zero)
        {
            LSpoolCorrect();
            span = LSpoolWorkingRangeEnd - LSpoolWorkingRangeStart;
        }

        var newSpan = span / 2;
        var rangeMinimum = LSpoolRangeMinimumGet();
        if (newSpan < rangeMinimum) newSpan = rangeMinimum;
        double ratio = span.TotalSeconds > 0 ? (cursor - LSpoolWorkingRangeStart).TotalSeconds / span.TotalSeconds : 0.5;
        ratio = Math.Clamp(ratio, 0, 1);
        LSpoolWorkingRangeStart = cursor - newSpan * ratio;
        LSpoolWorkingRangeEnd = cursor + newSpan * (1 - ratio);
        LSpoolCorrect();
    }

    public void LSpoolZoomOut(TimeSpan cursor)
    {
        var span = LSpoolWorkingRangeEnd - LSpoolWorkingRangeStart;
        if (span <= TimeSpan.Zero)
        {
            LSpoolCorrect();
            span = LSpoolWorkingRangeEnd - LSpoolWorkingRangeStart;
        }

        var newSpan = span * 2;
        if (newSpan > LSpoolDuration) newSpan = LSpoolDuration;
        double ratio = span.TotalSeconds > 0 ? (cursor - LSpoolWorkingRangeStart).TotalSeconds / span.TotalSeconds : 0.5;
        ratio = Math.Clamp(ratio, 0, 1);
        LSpoolWorkingRangeStart = cursor - newSpan * ratio;
        LSpoolWorkingRangeEnd = cursor + newSpan * (1 - ratio);
        LSpoolCorrect();
    }

    private TimeSpan LSpoolRangeMinimumGet()
        => LSpoolDuration < lSpoolRangeMinimum ? LSpoolDuration : lSpoolRangeMinimum;

    private TimeSpan LSpoolTimeClamp(TimeSpan time)
        => time < TimeSpan.Zero ? TimeSpan.Zero : time > LSpoolDuration ? LSpoolDuration : time;
}
