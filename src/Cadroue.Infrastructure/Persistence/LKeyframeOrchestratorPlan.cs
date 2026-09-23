using Cadroue.Media;

using Cadroue.Core;

namespace Cadroue.Infrastructure;

public sealed partial class LKeyframeOrchestrator
{
    private sealed record LKeyframeBounds(
        int LKeyframeBoundsFirst, long LKeyframeBoundsCursor, int LKeyframeBoundsLast);

    private void LKeyframePlanStart(string sourcePath, CancellationToken cancellationToken)
    {
        int worker;
        lock (lKeyframeLock)
        {
            if (lKeyframeWorkerActive
                || lKeyframeKind != LKeyframeKind.LKeyframeKindInter
                || cancellationToken.IsCancellationRequested)
            {
                return;
            }

            lKeyframeWorkerActive = true;
            worker = ++lKeyframeWorkerSerial;
        }

        _ = Task.Run(
            () => LKeyframePlanRun(sourcePath, worker, cancellationToken),
            CancellationToken.None);
    }

    private void LKeyframePlanRun(string sourcePath, int worker, CancellationToken cancellationToken)
    {
        try
        {
            while (true)
            {
                int spanIndex;
                lock (lKeyframeLock)
                {
                    spanIndex = lKeyframePaused
                        || cancellationToken.IsCancellationRequested
                        || lKeyframeKind != LKeyframeKind.LKeyframeKindInter
                        ? -1
                        : LKeyframeSpanFind(lKeyframeDuration, lKeyframeCursor);
                    if (spanIndex < 0)
                    {
                        LKeyframeWorkerStop(worker);
                        break;
                    }
                }

                LKeyframeSpanRun(sourcePath, spanIndex, cancellationToken);
            }
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
            lock (lKeyframeLock)
            {
                LKeyframeWorkerStop(worker);
            }

            if (!cancellationToken.IsCancellationRequested)
            {
                LKeyframeSidecarPersist();
            }
        }
    }

    private void LKeyframeWorkerStop(int worker)
    {
        if (worker == lKeyframeWorkerSerial)
        {
            lKeyframeWorkerActive = false;
        }
    }

    private int LKeyframeSpanFind(TimeSpan duration, TimeSpan cursor)
    {
        (int first, long cursorMs, int last) = LKeyframeBoundsCreate(duration, cursor);
        int found = -1;
        long foundDistance = long.MaxValue;
        for (int spanIndex = first; spanIndex <= last; spanIndex++)
        {
            long distance = LKeyframeDistanceResolve(spanIndex, cursorMs);
            if (distance < foundDistance && LKeyframeSpanCheck(spanIndex))
            {
                found = spanIndex;
                foundDistance = distance;
            }
        }

        return found;
    }

    private static long LKeyframeDistanceResolve(int spanIndex, long cursorMs)
    {
        long spanStartMs = (long)spanIndex * LKeyframeGridMilliseconds;
        long spanLastMs = spanStartMs + LKeyframeGridMilliseconds - 1;
        return Math.Max(0, Math.Max(spanStartMs - cursorMs, cursorMs - spanLastMs));
    }

    private bool LKeyframeSpanCheck(int spanIndex) =>
        !lKeyframeScannedSpans.Contains(spanIndex)
        && !LKeyframeRetryCheck(spanIndex)
        && !(lKeyframeAttempts.TryGetValue(spanIndex, out int attempted) && attempted == lKeyframeRequestSerial);

    private void LKeyframeSpanRun(string sourcePath, int spanIndex, CancellationToken cancellationToken)
    {
        TimeSpan duration;
        double startSeconds;
        lock (lKeyframeLock)
        {
            duration = lKeyframeDuration;
            startSeconds = lKeyframeStartSeconds;
            lKeyframeAttempts[spanIndex] = lKeyframeRequestSerial;
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
            LKeyframeSpanResult result = lKeyframeScanner(sourcePath, startSeconds, start, end, lKeyframeLimit.Token);
            cancellationToken.ThrowIfCancellationRequested();
            IReadOnlyList<LKeyframeEntry> entries = result.LKeyframeSpanEntries;
            int lKeyframeNewCount = 0;
            bool lKeyframePromoted;
            lock (lKeyframeLock)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                lKeyframePromoted = result.LKeyframeSpanIntra && lKeyframeKind == LKeyframeKind.LKeyframeKindInter;
                if (lKeyframePromoted)
                {
                    lKeyframeKind = LKeyframeKind.LKeyframeKindIntra;
                    lKeyframeStorage.Clear();
                    lKeyframeScannedSpans.Clear();
                    lKeyframeFailedCounts.Clear();
                }
                else
                {
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
            }

            LTrace.LTraceRecord(
                LTraceKind.LTraceWork,
                lKeyframePromoted
                    ? $"Keyframe span {spanIndex} scanned ({start:hh\\:mm\\:ss}-{end:hh\\:mm\\:ss}); "
                        + "every frame is a keyframe"
                    : $"Keyframe span {spanIndex} scanned ({start:hh\\:mm\\:ss}-{end:hh\\:mm\\:ss})",
                lKeyframePromoted
                    ? $"{entries.Count} packet(s) all flagged keyframe by ffprobe; scan stopped for this source"
                    : $"{lKeyframeNewCount} new keyframe(s) of {entries.Count} found by ffprobe",
                lKeyframeClock.Elapsed.TotalMilliseconds);

            if (!lKeyframePromoted && LKeyframeSaveCheck(lKeyframeNewCount))
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
            if (!LKeyframeFailureRecord(
                    spanIndex, cancellationToken, exception, lKeyframeClock.Elapsed.TotalMilliseconds))
            {
                return;
            }
        }

        LKeyframeNoticePublish(LKeyframeCurrentSerial);
    }

    private bool LKeyframeFailureRecord(
        int spanIndex, CancellationToken cancellationToken, Exception exception, double milliseconds)
    {
        int lFailedSpanCount;
        lock (lKeyframeLock)
        {
            if (cancellationToken.IsCancellationRequested)
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
        long cursorMs = (long)Math.Clamp(cursor.TotalMilliseconds, 0d, (double)durationMs);
        return new LKeyframeBounds(first, cursorMs, last);
    }
}
