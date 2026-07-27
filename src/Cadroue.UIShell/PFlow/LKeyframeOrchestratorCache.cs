using System.IO;
using Cadroue.Media;
using Cadroue.UIShell;

namespace Cadroue.UIShell.PFlow;

public sealed partial class LKeyframeOrchestrator
{
    private void LKeyframeNoticePublish(int serial)
    {
        LKeyframeNotice notice;
        lock (lKeyframeLock)
        {
            if (serial != lKeyframeRequestSerial)
            {
                return;
            }

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

    private bool LKeyframeRetryCheck(int spanIndex)
        => lKeyframeFailedSpanCounts.TryGetValue(spanIndex, out int lFailedSpanCount)
            && lFailedSpanCount >= LKeyframeFailedSpanRetryLimit;

    private bool LKeyframeSourceCheck(string sourcePath, TimeSpan duration)
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
        if (lDisposed)
        {
            return;
        }

        lDisposed = true;
        lKeyframeCancel?.Cancel();
        lKeyframeCancel?.Dispose();
    }
}
