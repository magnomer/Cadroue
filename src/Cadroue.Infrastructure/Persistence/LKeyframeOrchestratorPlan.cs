using Cadroue.Media;

using Cadroue.Core;

namespace Cadroue.Infrastructure;

public sealed partial class LKeyframeOrchestrator
{
    private sealed record LKeyframeBounds(int LKeyframeBoundsFirst, int LKeyframeBoundsCenter, int LKeyframeBoundsLast);

    private void LKeyframePlanStart(
        string sourcePath,
        TimeSpan duration,
        TimeSpan cursor,
        int serial,
        CancellationToken cancellationToken)
    {
        _ = Task.Run(
            () => LKeyframePlanRun(sourcePath, duration, cursor, serial, cancellationToken),
            CancellationToken.None);
    }

    private void LKeyframePlanRun(
        string sourcePath,
        TimeSpan duration,
        TimeSpan cursor,
        int serial,
        CancellationToken cancellationToken)
    {
        try
        {
            (int first, int center, int last) = LKeyframeBoundsCreate(duration, cursor);
            if (center >= first && center <= last)
            {
                LKeyframeSpanRun(sourcePath, duration, center, serial, cancellationToken);
            }

            LKeyframeDirectionRun(sourcePath, duration, center - 1, first, -1, serial, cancellationToken);
            LKeyframeDirectionRun(sourcePath, duration, center + 1, last, 1, serial, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            LTraceLog.LTraceErrorRecord("Keyframe scan plan failed", exception);
        }
        finally
        {
            if (serial == lKeyframeRequestSerial)
            {
                LKeyframeSidecarPersist();
            }
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

        var lKeyframeClock = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            using var lKeyframeLimit = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            lKeyframeLimit.CancelAfter(LKeyframeScanLimit);
            var entries = lKeyframeScanner(sourcePath, start, end, lKeyframeLimit.Token);
            cancellationToken.ThrowIfCancellationRequested();
            int lKeyframeNewCount = 0;
            lock (lKeyframeLock)
            {
                if (serial != lKeyframeRequestSerial)
                {
                    return;
                }

                foreach (var entry in entries)
                {
                    if (lKeyframeStorage.Add((long)Math.Round(entry.LKeyframePresentationTime.TotalMilliseconds)))
                    {
                        lKeyframeNewCount++;
                    }
                }

                lKeyframeScannedSpans.Add(spanIndex);
                lKeyframeFailedCounts.Remove(spanIndex);
            }

            LTrace.LTraceRecord(
                LTraceKind.LTraceWork,
                $"Keyframe span {spanIndex} scanned ({start:hh\\:mm\\:ss}-{end:hh\\:mm\\:ss})",
                $"{lKeyframeNewCount} new keyframe(s) of {entries.Count} found by ffprobe",
                lKeyframeClock.Elapsed.TotalMilliseconds);

            if (LKeyframeSaveCheck(lKeyframeNewCount))
            {
                LKeyframeCacheSave();
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return;
        }
        catch (Exception exception)
        {
            if (!LKeyframeFailureRecord(spanIndex, serial, exception, lKeyframeClock.Elapsed.TotalMilliseconds))
            {
                return;
            }
        }

        LKeyframeNoticePublish(serial);
    }

    private bool LKeyframeFailureRecord(int spanIndex, int serial, Exception exception, double milliseconds)
    {
        int lFailedSpanCount;
        lock (lKeyframeLock)
        {
            if (serial != lKeyframeRequestSerial)
            {
                return false;
            }

            lKeyframeFailedCounts.TryGetValue(spanIndex, out lFailedSpanCount);
            lKeyframeFailedCounts[spanIndex] = ++lFailedSpanCount;
        }

        bool lKeyframeExhausted = lFailedSpanCount >= LKeyframeRetryLimit;
        LTrace.LTraceRecord(
            lKeyframeExhausted ? LTraceKind.LTraceError : LTraceKind.LTraceWarning,
            exception is OperationCanceledException
                ? $"Keyframe span {spanIndex} scan timed out after {LKeyframeScanLimit.TotalSeconds:0}s"
                : $"Keyframe span {spanIndex} scan failed",
            $"Attempt {lFailedSpanCount} of {LKeyframeRetryLimit}"
            + (lKeyframeExhausted ? "; span abandoned for this source.\n" : ".\n")
            + exception.Message,
            milliseconds);
        return true;
    }

    private static LKeyframeBounds LKeyframeBoundsCreate(TimeSpan duration, TimeSpan cursor)
    {
        long durationMs = Math.Max(0, (long)Math.Ceiling(duration.TotalMilliseconds));
        long startMs = Math.Max(0, (long)(cursor - LKeyframeView.LKeyframeRangeBefore).TotalMilliseconds);
        long endMs = Math.Min(durationMs, (long)(cursor + LKeyframeView.LKeyframeRangeAfter).TotalMilliseconds);
        int first = (int)(startMs / LKeyframeGridMilliseconds);
        int last = (int)(Math.Max(0, endMs - 1) / LKeyframeGridMilliseconds);
        int center = (int)(Math.Clamp(cursor.TotalMilliseconds, 0d, (double)durationMs) / LKeyframeGridMilliseconds);
        return new LKeyframeBounds(first, center, last);
    }
}
