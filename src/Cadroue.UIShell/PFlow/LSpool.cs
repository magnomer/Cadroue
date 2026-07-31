namespace Cadroue.UIShell.PFlow;

public sealed class LSpool
{
    private static readonly TimeSpan lSpoolRangeMinimum = TimeSpan.FromSeconds(5);

    public TimeSpan LSpoolRangeOrigin { get; private set; }
    public TimeSpan LSpoolRangeLimit { get; private set; }
    public TimeSpan LSpoolDuration { get; private set; }

    public LSpool(TimeSpan duration)
    {
        LSpoolDuration = duration < TimeSpan.Zero ? TimeSpan.Zero : duration;
        LSpoolRangeOrigin = TimeSpan.Zero;
        LSpoolRangeLimit = LSpoolDuration;
    }

    public void LSpoolReset()
    {
        LSpoolRangeOrigin = TimeSpan.Zero;
        LSpoolRangeLimit = LSpoolDuration;
    }

    public void LSpoolNormalize()
    {
        if (LSpoolDuration <= TimeSpan.Zero)
        {
            LSpoolRangeOrigin = TimeSpan.Zero;
            LSpoolRangeLimit = TimeSpan.Zero;
            return;
        }

        LSpoolRangeOrigin = LSpoolTimeClamp(LSpoolRangeOrigin);
        LSpoolRangeLimit = LSpoolTimeClamp(LSpoolRangeLimit);

        if (LSpoolRangeLimit < LSpoolRangeOrigin)
        {
            var center = LSpoolRangeOrigin + (LSpoolRangeLimit - LSpoolRangeOrigin) / 2;
            LSpoolRangeOrigin = center;
            LSpoolRangeLimit = center;
        }

        var rangeMinimum = LSpoolMinimumRead();
        if (LSpoolRangeLimit - LSpoolRangeOrigin < rangeMinimum)
        {
            var center = LSpoolRangeOrigin + (LSpoolRangeLimit - LSpoolRangeOrigin) / 2;
            LSpoolRangeOrigin = center - rangeMinimum / 2;
            LSpoolRangeLimit = center + rangeMinimum / 2;

            if (LSpoolRangeOrigin < TimeSpan.Zero)
            {
                LSpoolRangeOrigin = TimeSpan.Zero;
                LSpoolRangeLimit = rangeMinimum;
            }

            if (LSpoolRangeLimit > LSpoolDuration)
            {
                LSpoolRangeLimit = LSpoolDuration;
                LSpoolRangeOrigin = LSpoolDuration - rangeMinimum;
            }
        }
    }

    public void LSpoolMove(TimeSpan delta)
    {
        var span = LSpoolRangeLimit - LSpoolRangeOrigin;
        if (span > LSpoolDuration)
        {
            span = LSpoolDuration;
        }

        LSpoolRangeOrigin += delta;
        LSpoolRangeLimit = LSpoolRangeOrigin + span;

        if (LSpoolRangeOrigin < TimeSpan.Zero)
        {
            LSpoolRangeOrigin = TimeSpan.Zero;
            LSpoolRangeLimit = span;
        }

        if (LSpoolRangeLimit > LSpoolDuration)
        {
            LSpoolRangeLimit = LSpoolDuration;
            LSpoolRangeOrigin = LSpoolDuration - span;
        }

        LSpoolNormalize();
    }

    public void LSpoolStartSet(TimeSpan newStart)
    {
        LSpoolRangeOrigin = newStart;
        LSpoolNormalize();
    }

    public void LSpoolEndSet(TimeSpan newEnd)
    {
        LSpoolRangeLimit = newEnd;
        LSpoolNormalize();
    }

    public void LSpoolCenterSet(TimeSpan center)
    {
        var halfSpan = (LSpoolRangeLimit - LSpoolRangeOrigin) / 2;
        LSpoolRangeOrigin = center - halfSpan;
        LSpoolRangeLimit = center + halfSpan;
        LSpoolNormalize();
    }

    public TimeSpan LSpoolTimeResolve(double ratio)
        => TimeSpan.FromSeconds(ratio * LSpoolDuration.TotalSeconds);

    public double LSpoolRatioResolve(TimeSpan t)
        => LSpoolDuration.TotalSeconds > 0 ? t.TotalSeconds / LSpoolDuration.TotalSeconds : 0;

    public void LSpoolInZoom(TimeSpan cursor)
    {
        var span = LSpoolRangeLimit - LSpoolRangeOrigin;
        if (span <= TimeSpan.Zero)
        {
            LSpoolNormalize();
            span = LSpoolRangeLimit - LSpoolRangeOrigin;
        }

        var newSpan = span / 2;
        var rangeMinimum = LSpoolMinimumRead();
        if (newSpan < rangeMinimum) newSpan = rangeMinimum;
        double ratio = span.TotalSeconds > 0 ? (cursor - LSpoolRangeOrigin).TotalSeconds / span.TotalSeconds : 0.5;
        ratio = Math.Clamp(ratio, 0, 1);
        LSpoolRangeOrigin = cursor - newSpan * ratio;
        LSpoolRangeLimit = cursor + newSpan * (1 - ratio);
        LSpoolNormalize();
    }

    public void LSpoolOutZoom(TimeSpan cursor)
    {
        var span = LSpoolRangeLimit - LSpoolRangeOrigin;
        if (span <= TimeSpan.Zero)
        {
            LSpoolNormalize();
            span = LSpoolRangeLimit - LSpoolRangeOrigin;
        }

        var newSpan = span * 2;
        if (newSpan > LSpoolDuration) newSpan = LSpoolDuration;
        double ratio = span.TotalSeconds > 0 ? (cursor - LSpoolRangeOrigin).TotalSeconds / span.TotalSeconds : 0.5;
        ratio = Math.Clamp(ratio, 0, 1);
        LSpoolRangeOrigin = cursor - newSpan * ratio;
        LSpoolRangeLimit = cursor + newSpan * (1 - ratio);
        LSpoolNormalize();
    }

    private TimeSpan LSpoolMinimumRead()
        => LSpoolDuration < lSpoolRangeMinimum ? LSpoolDuration : lSpoolRangeMinimum;

    private TimeSpan LSpoolTimeClamp(TimeSpan time)
        => time < TimeSpan.Zero ? TimeSpan.Zero : time > LSpoolDuration ? LSpoolDuration : time;
}
