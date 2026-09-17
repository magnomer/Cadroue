using System.IO;
using Cadroue.Media;

using Cadroue.Core;

namespace Cadroue.Infrastructure;

public sealed partial class LKeyframeOrchestrator
{
    private void LKeyframeNoticePublish(int serial)
    {
        LKeyframeNotice notice;
        long lKeyframeStamp;
        lock (lKeyframeLock)
        {
            if (serial != lKeyframeRequestSerial)
            {
                return;
            }

            LKeyframeEntry[] keyframes;
            LKeyframeScanRange[] scanned;
            if (lKeyframeKind == LKeyframeKind.LKeyframeKindInter)
            {
                keyframes = lKeyframeStorage
                    .Select(ms => new LKeyframeEntry(TimeSpan.FromMilliseconds(ms)))
                    .ToArray();
                scanned = lKeyframeScannedSpans
                    .OrderBy(index => index)
                    .Select(index => new LKeyframeScanRange(
                        TimeSpan.FromMilliseconds(index * LKeyframeGridMilliseconds),
                        TimeSpan.FromMilliseconds(Math.Min(
                            lKeyframeDuration.TotalMilliseconds,
                            (index + 1) * LKeyframeGridMilliseconds))))
                    .ToArray();
            }
            else
            {
                keyframes = Array.Empty<LKeyframeEntry>();
                scanned = lKeyframeDuration > TimeSpan.Zero
                    ? new[] { new LKeyframeScanRange(TimeSpan.Zero, lKeyframeDuration) }
                    : Array.Empty<LKeyframeScanRange>();
            }

            notice = new LKeyframeNotice(serial, keyframes, scanned, lKeyframeKind);
            lKeyframeStamp = ++lKeyframeNoticeSerial;
        }
        LKeyframeNoticeDispatch(notice, lKeyframeStamp);
    }

    private bool LKeyframeRetryCheck(int spanIndex)
        => lKeyframeFailedCounts.TryGetValue(spanIndex, out int lFailedSpanCount)
            && lFailedSpanCount >= LKeyframeRetryLimit;

    private bool LKeyframeSourceCheck(string sourcePath, TimeSpan duration)
    {
        try
        {
            return lKeyframeSourceIdentity?.LKeyframeIdentityMatch(sourcePath, duration) != true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return true;
        }
    }

    private void LKeyframeStateClear()
    {
        lKeyframeStorage.Clear();
        lKeyframeScannedSpans.Clear();
        lKeyframeFailedCounts.Clear();
        lKeyframeAttempts.Clear();
        lKeyframeWorkerActive = false;
        lKeyframeKind = LKeyframeKind.LKeyframeKindInter;
        lKeyframeRate = 0;
        lKeyframeStartSeconds = 0;
        lKeyframeUnsavedCount = 0;
        lKeyframeSavedSignature = new LKeyframeSignature(-1, -1);
        lKeyframeSourceIdentity = null;
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

        (long[] keyframes, int[] scannedSpans) = LKeyframeCacheLoad(identity);
        lock (lKeyframeLock)
        {
            if (serial != lKeyframeRequestSerial)
            {
                return null;
            }

            lKeyframeSourceIdentity = identity;
            lKeyframeStorage.UnionWith(keyframes);
            lKeyframeScannedSpans.UnionWith(scannedSpans);
            lKeyframeSavedSignature = new LKeyframeSignature(lKeyframeStorage.Count, lKeyframeScannedSpans.Count);
        }

        return identity;
    }

    private static (long[] LKeyframeList, int[] LKeyframeSpans) LKeyframeCacheLoad(LKeyframeSourceIdentity identity)
    {
        if (LSidecarStore.LSidecarLoad(identity) is { } lSidecar)
        {
            return (
                lSidecar.LSidecarKeyframesRead().ToArray(),
                lSidecar.LSidecarSpansRead(LKeyframeGridMilliseconds).ToArray());
        }

        return LKeyframeCacheStore.LKeyframeCacheLoad(identity, out var keyframes, out var scannedSpans)
            ? (keyframes.ToArray(), scannedSpans.ToArray())
            : (Array.Empty<long>(), Array.Empty<int>());
    }

    public void LKeyframeSidecarSave()
    {
        lock (lKeyframeLock)
        {
            lKeyframeUnsavedCount = 0;
        }

        LKeyframeCacheSave();
    }

    private void LKeyframeSidecarPersist()
    {
        lock (lKeyframeLock)
        {
            if (lKeyframeSavedSignature == new LKeyframeSignature(lKeyframeStorage.Count, lKeyframeScannedSpans.Count))
            {
                return;
            }
        }

        LKeyframeSidecarSave();
    }

    private void LKeyframeCacheSave()
    {
        LKeyframeSourceIdentity? identity;
        long[] keyframes;
        int[] scannedSpans;
        lock (lKeyframeLock)
        {
            identity = lKeyframeSourceIdentity;
            if (identity is null || lKeyframeKind != LKeyframeKind.LKeyframeKindInter)
            {
                return;
            }

            keyframes = lKeyframeStorage.ToArray();
            scannedSpans = lKeyframeScannedSpans.ToArray();
        }

        var lKeyframeClock = System.Diagnostics.Stopwatch.StartNew();
        bool lKeyframeSidecarWritten = LSidecarStore.LSidecarSave(
            identity,
            keyframes,
            scannedSpans,
            LKeyframeGridMilliseconds);
        bool lKeyframePersisted = lKeyframeSidecarWritten
            || LKeyframeCacheStore.LKeyframeCacheSave(identity, keyframes, scannedSpans);

        if (lKeyframePersisted)
        {
            lock (lKeyframeLock)
            {
                lKeyframeSavedSignature = new LKeyframeSignature(keyframes.Length, scannedSpans.Length);
            }
        }

        LTrace.LTraceRecord(
            LTraceKind.LTraceWork,
            lKeyframeSidecarWritten
                ? "Sidecar written"
                : lKeyframePersisted
                    ? "Keyframe cache written (sidecar refused)"
                    : "Keyframe save failed (sidecar and cache both refused); will retry",
            $"{keyframes.Length} keyframe(s), {scannedSpans.Length} scanned span(s)\n"
            + $"for {identity.LKeyframeSourcePath}",
            lKeyframeClock.Elapsed.TotalMilliseconds);
    }

    private bool LKeyframeSaveCheck(int lKeyframeNewCount)
    {
        lock (lKeyframeLock)
        {
            lKeyframeUnsavedCount += lKeyframeNewCount + 1;
            if (lKeyframeUnsavedCount < LKeyframeSaveCount)
            {
                return false;
            }

            lKeyframeUnsavedCount = 0;
            return true;
        }
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
                    System.Diagnostics.Debug.WriteLine(ex);
                }
            }
        }
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
}
