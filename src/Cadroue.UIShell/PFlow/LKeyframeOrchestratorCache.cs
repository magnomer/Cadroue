using System.IO;
using Cadroue.Media;
using Cadroue.UIShell;

using Cadroue.Core;

using Cadroue.Infrastructure;

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
        => lKeyframeFailedCounts.TryGetValue(spanIndex, out int lFailedSpanCount)
            && lFailedSpanCount >= LKeyframeRetryLimit;

    private bool LKeyframeSourceCheck(string sourcePath, TimeSpan duration)
    {
        if (lKeyframeSourceIdentity is null)
        {
            return true;
        }

        string fullPath = Path.GetFullPath(sourcePath);
        long durationMs = (long)Math.Round(duration.TotalMilliseconds);
        return !string.Equals(lKeyframeSourceIdentity.LKeyframeSourcePath, fullPath, StringComparison.OrdinalIgnoreCase)
            || lKeyframeSourceIdentity.LKeyframeSourceDuration != durationMs;
    }

    private void LKeyframeCacheLoad(LKeyframeSourceIdentity identity)
    {
        if (LSidecarStore.LSidecarLoad(identity) is { } lSidecar)
        {
            foreach (long keyframe in lSidecar.LSidecarKeyframesRead())
            {
                lKeyframeStorage.Add(keyframe);
            }

            foreach (int scannedSpan in lSidecar.LSidecarSpansRead(LKeyframeGridMilliseconds))
            {
                lKeyframeScannedSpans.Add(scannedSpan);
            }

            return;
        }

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
            if (lKeyframeSavedSignature == (lKeyframeStorage.Count, lKeyframeScannedSpans.Count))
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
            if (identity is null)
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
            LKeyframeGridMilliseconds,
            LKeyframeSectionsRead());
        bool lKeyframePersisted = lKeyframeSidecarWritten
            || LKeyframeCacheStore.LKeyframeCacheSave(identity, keyframes, scannedSpans);

        if (lKeyframePersisted)
        {
            lock (lKeyframeLock)
            {
                lKeyframeSavedSignature = (keyframes.Length, scannedSpans.Length);
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

    public Func<IReadOnlyList<LSidecarSectionRecord>>? LKeyframeSectionsSource { get; set; }

    private IReadOnlyList<LSidecarSectionRecord> LKeyframeSectionsRead()
    {
        try
        {
            return LKeyframeSectionsSource?.Invoke() ?? Array.Empty<LSidecarSectionRecord>();
        }
        catch (Exception lException)
        {
            LTraceLog.LTraceErrorRecord("Sidecar sections could not be read", lException);
            return Array.Empty<LSidecarSectionRecord>();
        }
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
        if (lKeyframeDisposed)
        {
            return;
        }

        lKeyframeDisposed = true;
        lKeyframeCancelSource?.Cancel();
        lKeyframeCancelSource?.Dispose();
    }
}
