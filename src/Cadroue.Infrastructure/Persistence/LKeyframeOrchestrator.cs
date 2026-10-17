using System.IO;
using Cadroue.Media;

using Cadroue.Core;

namespace Cadroue.Infrastructure;

public sealed class LKeyframeOrchestrator : IDisposable
{
    private readonly object lKeyframeLock = new();
    private readonly object lKeyframeDispatchGate = new();
    private readonly LKeyframeSession lKeyframeSession = new();
    private readonly LKeyframeCache lKeyframeCache;
    private readonly LKeyframePlan lKeyframePlan;
    private CancellationTokenSource? lKeyframeCancelSource;
    private long lKeyframeNoticeSerial;
    private long lKeyframeNoticeCeiling = -1;
    private bool lKeyframeDisposed;

    public LKeyframeOrchestrator()
        : this(LKeyframeSeeker.LKeyframeLaneScan)
    {
    }

    internal LKeyframeOrchestrator(
        Func<string, double, TimeSpan, TimeSpan, CancellationToken, Task<LKeyframeSpanResult>> scanner)
    {
        ArgumentNullException.ThrowIfNull(scanner);
        lKeyframeCache = new LKeyframeCache(lKeyframeLock, lKeyframeSession);
        lKeyframePlan = new LKeyframePlan(
            lKeyframeLock, lKeyframeSession, lKeyframeCache, scanner, LKeyframeNoticePublish);
    }

    public event Action<LKeyframeNotice>? LKeyframeNoticeReady;

    public int LKeyframeCurrentSerial => lKeyframeSession.LKeyframeSessionSerial;

    public static TimeSpan LKeyframeSearchDuration =>
        LKeyframeView.LKeyframeRangeBefore + LKeyframeView.LKeyframeRangeAfter;

    public void LKeyframeStart(string sourcePath, LMediaInfo mediaInfo, TimeSpan cursor)
    {
        TimeSpan duration = mediaInfo.LMediaInfoDuration;
        if (lKeyframeDisposed || string.IsNullOrWhiteSpace(sourcePath) || duration <= TimeSpan.Zero)
        {
            return;
        }

        CancellationTokenSource cancel;
        int serial;
        LKeyframeSourceIdentity? identity;
        lock (lKeyframeLock)
        {
            serial = lKeyframeSession.LKeyframeRequestStart(cursor);
            identity = lKeyframeSession.LKeyframeSessionSource;
            if (lKeyframeSession.LKeyframeChangeCheck(sourcePath, duration))
            {
                lKeyframeCancelSource?.Cancel();
                lKeyframeCancelSource?.Dispose();
                lKeyframeCancelSource = new CancellationTokenSource();
                identity = null;
                lKeyframePlan.LKeyframePlanReset();
                lKeyframeSession.LKeyframeSessionOpen(
                    duration,
                    LKeyframeCodec.LKeyframeKindResolve(mediaInfo),
                    mediaInfo.LMediaVideoRate,
                    mediaInfo.LMediaStartTime.TotalSeconds);
            }

            lKeyframeCancelSource ??= new CancellationTokenSource();
            cancel = lKeyframeCancelSource;
        }

        identity ??= LKeyframeIdentityLoad(sourcePath, duration, serial);
        LKeyframeNoticePublish(serial);
        if (identity is not null)
        {
            lKeyframePlan.LKeyframePlanStart(identity.LKeyframeSourcePath, cancel.Token);
        }
    }

    public void LKeyframeSuspend()
    {
        lock (lKeyframeLock)
        {
            lKeyframeSession.LKeyframeSessionSuspend();
        }
    }

    public void LKeyframeSync(TimeSpan cursor)
    {
        lock (lKeyframeLock)
        {
            lKeyframeSession.LKeyframeSessionSync(cursor);
        }
    }

    public LKeyframeMoveResult LKeyframePreviousMove(TimeSpan cursor)
        => LKeyframeMoveFind(cursor, -1);

    public LKeyframeMoveResult LKeyframeNextMove(TimeSpan cursor)
        => LKeyframeMoveFind(cursor, 1);

    public LKeyframeMoveResult LKeyframeNearestMove(TimeSpan cursor)
    {
        var previous = LKeyframePreviousMove(cursor);
        var next = LKeyframeNextMove(cursor);
        if (previous.LKeyframeFailed || next.LKeyframeFailed)
        {
            return LKeyframeMoveResult.LKeyframeFailedCreate();
        }

        if (!previous.LKeyframeReady || !next.LKeyframeReady)
        {
            return LKeyframeMoveResult.LKeyframePending;
        }

        if (previous.LKeyframeTarget is null) return next;
        if (next.LKeyframeTarget is null) return previous;
        return cursor - previous.LKeyframeTarget.Value <= next.LKeyframeTarget.Value - cursor ? previous : next;
    }

    public void LKeyframeSidecarSave()
    {
        lKeyframeCache.LKeyframeCacheCommit();
    }

    public void Dispose()
    {
        if (lKeyframeDisposed)
        {
            return;
        }

        lKeyframeDisposed = true;
        lKeyframeCancelSource?.Cancel();
        lKeyframeCancelSource?.Dispose();
        lKeyframeCancelSource = null;
    }

    private LKeyframeMoveResult LKeyframeMoveFind(TimeSpan cursor, int direction)
    {
        lock (lKeyframeLock)
        {
            return lKeyframeSession.LKeyframeTargetFind(cursor, direction);
        }
    }

    private LKeyframeSourceIdentity? LKeyframeIdentityLoad(string sourcePath, TimeSpan duration, int serial)
    {
        LKeyframeSourceIdentity identity;
        try
        {
            identity = LKeyframeSourceIdentity.LKeyframeIdentityCreate(sourcePath, duration);
        }
        catch (Exception exception)
        {
            LTraceLog.LTraceWarningRecord(
                $"Keyframe scan skipped: source identity could not be read for '{Path.GetFileName(sourcePath)}'",
                exception.Message);
            return null;
        }

        (long[] keyframes, int[] scannedSpans) = LKeyframeCache.LKeyframeCacheLoad(identity);
        lock (lKeyframeLock)
        {
            if (serial != lKeyframeSession.LKeyframeSessionSerial)
            {
                return null;
            }

            lKeyframeSession.LKeyframeSourceSet(identity, keyframes, scannedSpans);
        }

        return identity;
    }

    private void LKeyframeNoticePublish(int serial)
    {
        LKeyframeNotice notice;
        long lKeyframeStamp;
        lock (lKeyframeLock)
        {
            if (serial != lKeyframeSession.LKeyframeSessionSerial)
            {
                return;
            }

            notice = lKeyframeSession.LKeyframeNoticeCreate(serial);
            lKeyframeStamp = ++lKeyframeNoticeSerial;
        }
        LKeyframeNoticeDispatch(notice, lKeyframeStamp);
    }

    private void LKeyframeNoticeDispatch(LKeyframeNotice notice, long stamp)
    {
        lock (lKeyframeDispatchGate)
        {
            if (stamp <= lKeyframeNoticeCeiling)
            {
                return;
            }

            lKeyframeNoticeCeiling = stamp;

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
                    LTraceLog.LTraceErrorRecord("Keyframe notice handler failed", ex);
                }
            }
        }
    }
}
