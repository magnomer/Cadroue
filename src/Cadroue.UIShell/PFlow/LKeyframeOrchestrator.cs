using System.IO;
using Cadroue.Media;
using Cadroue.UIShell;

namespace Cadroue.UIShell.PFlow;

public sealed class LKeyframeOrchestrator : IDisposable
{
    private const int LKeyframeGridMilliseconds = 20000;
    private static readonly TimeSpan lKeyframeRangeBefore = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan lKeyframeRangeAfter = TimeSpan.FromMinutes(10);
    private readonly object lKeyframeLock = new();
    private readonly SortedSet<long> lKeyframeStorage = new();
    private readonly HashSet<int> lKeyframeScannedSpans = new();
    private const int LKeyframeFailedSpanRetryLimit = 3;
    private readonly Dictionary<int, int> lKeyframeFailedSpanCounts = new();
    private CancellationTokenSource? lKeyframeCancel;
    private LKeyframeSourceIdentity? lKeyframeSourceIdentity;
    private string? lKeyframeSourcePath;
    private TimeSpan lKeyframeDuration;
    private int lKeyframeRequestSerial;
    private bool lDisposed;

    public event Action<LKeyframeNotice>? LKeyframeNoticeReady;

    public int LKeyframeCurrentSerial => lKeyframeRequestSerial;

    public static TimeSpan LKeyframeRangeBefore => lKeyframeRangeBefore;

    public static TimeSpan LKeyframeRangeAfter => lKeyframeRangeAfter;

    public static TimeSpan LKeyframeSearchDuration => lKeyframeRangeBefore + lKeyframeRangeAfter;


    public void LKeyframeRequest(string sourcePath, TimeSpan duration, TimeSpan cursor)
    {
        if (lDisposed || string.IsNullOrWhiteSpace(sourcePath) || duration <= TimeSpan.Zero)
        {
            return;
        }

        CancellationTokenSource cancel;
        int serial;
        LKeyframeSourceIdentity identity;
        lock (lKeyframeLock)
        {
            if (LKeyframeSourceChangeCheck(sourcePath, duration))
            {
                try
                {
                    identity = LKeyframeSourceIdentity.LKeyframeSourceIdentityCreate(sourcePath, duration);
                }
                catch
                {
                    return;
                }

                lKeyframeStorage.Clear();
                lKeyframeScannedSpans.Clear();
                lKeyframeFailedSpanCounts.Clear();
                lKeyframeSourceIdentity = identity;
                lKeyframeSourcePath = identity.LKeyframeSourcePath;
                lKeyframeDuration = duration;
                LKeyframeCacheLoad(identity);
            }
            else
            {
                identity = lKeyframeSourceIdentity!;
            }

            lKeyframeCancel?.Cancel();
            lKeyframeCancel?.Dispose();
            lKeyframeCancel = new CancellationTokenSource();
            cancel = lKeyframeCancel;
            serial = ++lKeyframeRequestSerial;
        }

        LKeyframeNoticePublish(serial);
        LKeyframePlanStart(identity.LKeyframeSourcePath, duration, cursor, serial, cancel.Token);
    }

    public TimeSpan? LKeyframeMovePrevious(TimeSpan cursor)
        => LKeyframeMoveFind(cursor, -1);

    public TimeSpan? LKeyframeMoveNext(TimeSpan cursor)
        => LKeyframeMoveFind(cursor, 1);

    public TimeSpan? LKeyframeMoveNearest(TimeSpan cursor)
    {
        var previous = LKeyframeMovePrevious(cursor);
        var next = LKeyframeMoveNext(cursor);
        if (previous is null) return next;
        if (next is null) return previous;
        return cursor - previous.Value <= next.Value - cursor ? previous : next;
    }

    private TimeSpan? LKeyframeMoveFind(TimeSpan cursor, int direction)
    {
        long cursorMs = (long)Math.Round(cursor.TotalMilliseconds);
        long limit = (long)Math.Round(App.LPreferenceStateCurrent.LPreferenceImmediateKeyframeWindowMilliseconds);
        lock (lKeyframeLock)
        {
            IEnumerable<long> query = direction < 0
                ? lKeyframeStorage.Where(ms => ms < cursorMs).Reverse()
                : lKeyframeStorage.Where(ms => ms > cursorMs);
            foreach (long ms in query)
            {
                if (Math.Abs(ms - cursorMs) > limit) return null;
                return TimeSpan.FromMilliseconds(ms);
            }
        }
        return null;
    }

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
            (int first, int center, int last) = LKeyframePlanBoundsMake(duration, cursor);
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
                || LKeyframeSpanRetryLimitReached(spanIndex))
            {
                return;
            }
        }

        var start = TimeSpan.FromMilliseconds(spanIndex * LKeyframeGridMilliseconds);
        var end = start + TimeSpan.FromMilliseconds(LKeyframeGridMilliseconds);
        if (end > duration) end = duration;

        try
        {
            var entries = LKeyframeSeeker.LKeyframeSeekerScanRange(sourcePath, start, end, cancellationToken);
            lock (lKeyframeLock)
            {
                if (serial != lKeyframeRequestSerial || cancellationToken.IsCancellationRequested) return;
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
                if (serial != lKeyframeRequestSerial) return;
                lKeyframeFailedSpanCounts.TryGetValue(spanIndex, out int lFailedSpanCount);
                lKeyframeFailedSpanCounts[spanIndex] = lFailedSpanCount + 1;
            }
        }

        LKeyframeNoticePublish(serial);
    }

    private static (int First, int Center, int Last) LKeyframePlanBoundsMake(TimeSpan duration, TimeSpan cursor)
    {
        long durationMs = Math.Max(0, (long)Math.Ceiling(duration.TotalMilliseconds));
        long startMs = Math.Max(0, (long)(cursor - lKeyframeRangeBefore).TotalMilliseconds);
        long endMs = Math.Min(durationMs, (long)(cursor + lKeyframeRangeAfter).TotalMilliseconds);
        int first = (int)(startMs / LKeyframeGridMilliseconds);
        int last = (int)(Math.Max(0, endMs - 1) / LKeyframeGridMilliseconds);
        int center = (int)(Math.Clamp(cursor.TotalMilliseconds, 0d, (double)durationMs) / LKeyframeGridMilliseconds);
        return (first, center, last);
    }

    private void LKeyframeNoticePublish(int serial)
    {
        LKeyframeNotice notice;
        lock (lKeyframeLock)
        {
            if (serial != lKeyframeRequestSerial) return;
            var keyframes = lKeyframeStorage
                .Select(ms => new LKeyframeEntry(TimeSpan.FromMilliseconds(ms)))
                .ToArray();
            var scanned = lKeyframeScannedSpans
                .OrderBy(index => index)
                .Select(index => new LKeyframeScanRange(
                    TimeSpan.FromMilliseconds(index * LKeyframeGridMilliseconds),
                    TimeSpan.FromMilliseconds(Math.Min(
                        lKeyframeDuration.TotalMilliseconds,
                        (index + 1) * LKeyframeGridMilliseconds))))
                .ToArray();
            notice = new LKeyframeNotice(serial, keyframes, scanned);
        }
        LKeyframeNoticeDispatch(notice);
    }

    private bool LKeyframeSpanRetryLimitReached(int spanIndex)
        => lKeyframeFailedSpanCounts.TryGetValue(spanIndex, out int lFailedSpanCount)
            && lFailedSpanCount >= LKeyframeFailedSpanRetryLimit;

    private bool LKeyframeSourceChangeCheck(string sourcePath, TimeSpan duration)
    {
        if (lKeyframeSourceIdentity is null)
        {
            return true;
        }

        string fullPath = Path.GetFullPath(sourcePath);
        long durationMs = (long)Math.Round(duration.TotalMilliseconds);
        return !string.Equals(lKeyframeSourceIdentity.LKeyframeSourcePath, fullPath, StringComparison.OrdinalIgnoreCase)
            || lKeyframeSourceIdentity.LKeyframeSourceDurationMilliseconds != durationMs;
    }

    private void LKeyframeCacheLoad(LKeyframeSourceIdentity identity)
    {
        if (!LKeyframeCacheStore.LKeyframeCacheLoad(identity, out var keyframes, out var scannedSpans))
        {
            return;
        }

        foreach (long keyframe in keyframes)
        {
            lKeyframeStorage.Add(keyframe);
        }

        foreach (int scannedSpan in scannedSpans)
        {
            lKeyframeScannedSpans.Add(scannedSpan);
        }
    }

    private void LKeyframeCacheSave()
    {
        LKeyframeSourceIdentity? identity;
        long[] keyframes;
        int[] scannedSpans;
        lock (lKeyframeLock)
        {
            identity = lKeyframeSourceIdentity;
            if (identity is null)
            {
                return;
            }

            keyframes = lKeyframeStorage.ToArray();
            scannedSpans = lKeyframeScannedSpans.ToArray();
        }

        LKeyframeCacheStore.LKeyframeCacheSave(identity, keyframes, scannedSpans);
    }

    private void LKeyframeNoticeDispatch(LKeyframeNotice notice)
    {
        var noticeReady = LKeyframeNoticeReady;
        if (noticeReady is null)
        {
            return;
        }

        foreach (var handler in noticeReady.GetInvocationList())
        {
            try
            {
                ((Action<LKeyframeNotice>)handler)(notice);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }
    }

    public void Dispose()
    {
        if (lDisposed) return;
        lDisposed = true;
        lKeyframeCancel?.Cancel();
        lKeyframeCancel?.Dispose();
    }
}
