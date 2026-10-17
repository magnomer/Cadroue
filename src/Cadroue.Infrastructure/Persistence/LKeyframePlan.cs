using Cadroue.Media;

using Cadroue.Core;

namespace Cadroue.Infrastructure;

internal sealed class LKeyframePlan
{
    private static readonly TimeSpan LKeyframeScanLimit = TimeSpan.FromMinutes(1);

    private readonly object lKeyframeLock;
    private readonly LKeyframeSession lKeyframeSession;
    private readonly LKeyframeCache lKeyframeCache;
    private readonly Func<string, double, TimeSpan, TimeSpan, CancellationToken, Task<LKeyframeSpanResult>>
        lKeyframeScanner;
    private readonly Action<int> lKeyframePlanPublisher;
    private bool lKeyframeWorkerActive;
    private int lKeyframeWorkerSerial;

    internal LKeyframePlan(
        object keyframeLock,
        LKeyframeSession keyframeSession,
        LKeyframeCache keyframeCache,
        Func<string, double, TimeSpan, TimeSpan, CancellationToken, Task<LKeyframeSpanResult>> scanner,
        Action<int> noticePublish)
    {
        lKeyframeLock = keyframeLock;
        lKeyframeSession = keyframeSession;
        lKeyframeCache = keyframeCache;
        lKeyframeScanner = scanner;
        lKeyframePlanPublisher = noticePublish;
    }

    internal void LKeyframePlanReset()
    {
        lKeyframeWorkerActive = false;
    }

    internal void LKeyframePlanStart(string sourcePath, CancellationToken cancellationToken)
    {
        int worker;
        lock (lKeyframeLock)
        {
            if (lKeyframeWorkerActive
                || lKeyframeSession.LKeyframeSessionKind != LKeyframeKind.LKeyframeKindInter
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

    private async Task LKeyframePlanRun(string sourcePath, int worker, CancellationToken cancellationToken)
    {
        try
        {
            while (true)
            {
                int spanIndex;
                lock (lKeyframeLock)
                {
                    spanIndex = lKeyframeSession.LKeyframeSessionPaused
                        || cancellationToken.IsCancellationRequested
                        || lKeyframeSession.LKeyframeSessionKind != LKeyframeKind.LKeyframeKindInter
                        ? -1
                        : lKeyframeSession.LKeyframeSpanFind();
                    if (spanIndex < 0)
                    {
                        LKeyframeWorkerStop(worker);
                        break;
                    }
                }

                await LKeyframeSpanRun(sourcePath, spanIndex, cancellationToken).ConfigureAwait(false);
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
                lKeyframeCache.LKeyframeCachePersist();
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

    private async Task LKeyframeSpanRun(string sourcePath, int spanIndex, CancellationToken cancellationToken)
    {
        TimeSpan duration;
        double startSeconds;
        lock (lKeyframeLock)
        {
            if (lKeyframeSession.LKeyframeSessionPaused)
            {
                return;
            }

            duration = lKeyframeSession.LKeyframeSessionDuration;
            startSeconds = lKeyframeSession.LKeyframeSessionOrigin;
            lKeyframeSession.LKeyframeAttemptSet(spanIndex);
        }

        var start = TimeSpan.FromMilliseconds(spanIndex * LKeyframeSession.LKeyframeGridMilliseconds);
        var end = start + TimeSpan.FromMilliseconds(LKeyframeSession.LKeyframeGridMilliseconds);
        if (end > duration)
        {
            end = duration;
        }

        var lKeyframeClock = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            using var lKeyframeLimit = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            lKeyframeLimit.CancelAfter(LKeyframeScanLimit);
            LKeyframeSpanResult result = await lKeyframeScanner(
                sourcePath, startSeconds, start, end, lKeyframeLimit.Token).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            IReadOnlyList<LKeyframeEntry> entries = result.LKeyframeSpanEntries;
            int lKeyframeNewCount;
            bool lKeyframePromoted;
            lock (lKeyframeLock)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                (lKeyframePromoted, lKeyframeNewCount) =
                    lKeyframeSession.LKeyframeSpanApply(spanIndex, result);
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
                lKeyframeCache.LKeyframeCacheSave();
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

        lKeyframePlanPublisher(lKeyframeSession.LKeyframeSessionSerial);
    }

    private bool LKeyframeSaveCheck(int lKeyframeNewCount)
    {
        lock (lKeyframeLock)
        {
            return lKeyframeSession.LKeyframeUnsavedCheck(lKeyframeNewCount);
        }
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

            lFailedSpanCount = lKeyframeSession.LKeyframeFailureAdd(spanIndex);
        }

        bool lKeyframeExhausted = lFailedSpanCount >= LKeyframeSession.LKeyframeRetryLimit;
        LTrace.LTraceRecord(
            lKeyframeExhausted ? LTraceKind.LTraceError : LTraceKind.LTraceWarning,
            exception is OperationCanceledException
                ? $"Keyframe span {spanIndex} scan timed out after {LKeyframeScanLimit.TotalSeconds:0}s"
                : $"Keyframe span {spanIndex} scan failed",
            $"Attempt {lFailedSpanCount} of {LKeyframeSession.LKeyframeRetryLimit}"
            + (lKeyframeExhausted ? "; span abandoned for this source.\n" : ".\n")
            + exception.Message,
            milliseconds);
        return true;
    }
}
