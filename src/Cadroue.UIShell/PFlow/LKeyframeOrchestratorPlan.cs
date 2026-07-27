using Cadroue.Media;

namespace Cadroue.UIShell.PFlow;

public sealed partial class LKeyframeOrchestrator
{
    private void LKeyframePlanStart(
        string sourcePath,
        TimeSpan duration,
        TimeSpan cursor,
        int serial,
        CancellationToken cancellationToken)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await LKeyframePlanRun(sourcePath, duration, cursor, serial, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }, CancellationToken.None);
    }

    private async Task LKeyframePlanRun(
        string sourcePath,
        TimeSpan duration,
        TimeSpan cursor,
        int serial,
        CancellationToken cancellationToken)
    {
        try
        {
            (int first, int center, int last) = LKeyframeBoundsCreate(duration, cursor);
            var tasks = new List<Task>();
            if (center >= first && center <= last)
            {
                tasks.Add(Task.Run(
                    () => LKeyframeSpanRun(sourcePath, duration, center, serial, cancellationToken),
                    CancellationToken.None));
            }

            tasks.Add(Task.Run(
                () => LKeyframeDirectionRun(sourcePath, duration, center - 1, first, -1, serial, cancellationToken),
                CancellationToken.None));
            tasks.Add(Task.Run(
                () => LKeyframeDirectionRun(sourcePath, duration, center + 1, last, 1, serial, cancellationToken),
                CancellationToken.None));
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
    }

    private void LKeyframeDirectionRun(
        string sourcePath,
        TimeSpan duration,
        int startSpanIndex,
        int endSpanIndex,
        int direction,
        int serial,
        CancellationToken cancellationToken)
    {
        for (int spanIndex = startSpanIndex;
             direction < 0 ? spanIndex >= endSpanIndex : spanIndex <= endSpanIndex;
             spanIndex += direction)
        {
            cancellationToken.ThrowIfCancellationRequested();
            LKeyframeSpanRun(sourcePath, duration, spanIndex, serial, cancellationToken);
        }
    }

    private void LKeyframeSpanRun(
        string sourcePath,
        TimeSpan duration,
        int spanIndex,
        int serial,
        CancellationToken cancellationToken)
    {
        lock (lKeyframeLock)
        {
            if (serial != lKeyframeRequestSerial
                || lKeyframeScannedSpans.Contains(spanIndex)
                || LKeyframeRetryCheck(spanIndex))
            {
                return;
            }
        }

        var start = TimeSpan.FromMilliseconds(spanIndex * LKeyframeGridMilliseconds);
        var end = start + TimeSpan.FromMilliseconds(LKeyframeGridMilliseconds);
        if (end > duration)
        {
            end = duration;
        }

        try
        {
            var entries = LKeyframeSeeker.LKeyframeRangeScan(sourcePath, start, end, cancellationToken);
            lock (lKeyframeLock)
            {
                if (serial != lKeyframeRequestSerial || cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                foreach (var entry in entries)
                {
                    lKeyframeStorage.Add((long)Math.Round(entry.LKeyframePresentationTime.TotalMilliseconds));
                }

                lKeyframeScannedSpans.Add(spanIndex);
                lKeyframeFailedSpanCounts.Remove(spanIndex);
            }
            LKeyframeCacheSave();
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch
        {
            lock (lKeyframeLock)
            {
                if (serial != lKeyframeRequestSerial)
                {
                    return;
                }

                lKeyframeFailedSpanCounts.TryGetValue(spanIndex, out int lFailedSpanCount);
                lKeyframeFailedSpanCounts[spanIndex] = lFailedSpanCount + 1;
            }
        }

        LKeyframeNoticePublish(serial);
    }

    private static (int First, int Center, int Last) LKeyframeBoundsCreate(TimeSpan duration, TimeSpan cursor)
    {
        long durationMs = Math.Max(0, (long)Math.Ceiling(duration.TotalMilliseconds));
        long startMs = Math.Max(0, (long)(cursor - lKeyframeRangeBefore).TotalMilliseconds);
        long endMs = Math.Min(durationMs, (long)(cursor + lKeyframeRangeAfter).TotalMilliseconds);
        int first = (int)(startMs / LKeyframeGridMilliseconds);
        int last = (int)(Math.Max(0, endMs - 1) / LKeyframeGridMilliseconds);
        int center = (int)(Math.Clamp(cursor.TotalMilliseconds, 0d, (double)durationMs) / LKeyframeGridMilliseconds);
        return (first, center, last);
    }
}
