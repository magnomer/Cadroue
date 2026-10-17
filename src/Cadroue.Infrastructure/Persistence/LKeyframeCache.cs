using Cadroue.Media;

using Cadroue.Core;

namespace Cadroue.Infrastructure;

internal sealed class LKeyframeCache
{
    private const int LKeyframeLegacyMilliseconds = 20000;

    private readonly object lKeyframeLock;
    private readonly LKeyframeSession lKeyframeSession;

    internal LKeyframeCache(object keyframeLock, LKeyframeSession keyframeSession)
    {
        lKeyframeLock = keyframeLock;
        lKeyframeSession = keyframeSession;
    }

    internal static (long[] LKeyframeList, int[] LKeyframeSpans) LKeyframeCacheLoad(LKeyframeSourceIdentity identity)
    {
        if (LSidecarStore.LSidecarLoad(identity) is { } lSidecar)
        {
            return (
                lSidecar.LSidecarKeyframesRead().ToArray(),
                lSidecar.LSidecarSpansRead(LKeyframeSession.LKeyframeGridMilliseconds)
                    .Concat(lSidecar.LSidecarSpansRead(LKeyframeLegacyMilliseconds)
                        .SelectMany(index => new[] { index * 2, index * 2 + 1 }))
                    .Distinct()
                    .ToArray());
        }

        return LKeyframeCacheStore.LKeyframeCacheLoad(
                identity, LKeyframeSession.LKeyframeGridMilliseconds, out var keyframes, out var scannedSpans)
            ? (keyframes.ToArray(), scannedSpans.ToArray())
            : (Array.Empty<long>(), Array.Empty<int>());
    }

    internal void LKeyframeCacheCommit()
    {
        lock (lKeyframeLock)
        {
            lKeyframeSession.LKeyframeUnsavedClear();
        }

        LKeyframeCacheSave();
    }

    internal void LKeyframeCachePersist()
    {
        lock (lKeyframeLock)
        {
            if (lKeyframeSession.LKeyframeSavedCheck())
            {
                return;
            }
        }

        LKeyframeCacheCommit();
    }

    internal void LKeyframeCacheSave()
    {
        LKeyframeSourceIdentity identity;
        long[] keyframes;
        int[] scannedSpans;
        lock (lKeyframeLock)
        {
            if (lKeyframeSession.LKeyframeSaveRead() is not { } snapshot)
            {
                return;
            }

            (identity, keyframes, scannedSpans) = snapshot;
        }

        var lKeyframeClock = System.Diagnostics.Stopwatch.StartNew();
        bool lKeyframeSidecarWritten = LSidecarStore.LSidecarSave(
            identity,
            keyframes,
            scannedSpans,
            LKeyframeSession.LKeyframeGridMilliseconds);
        bool lKeyframePersisted = lKeyframeSidecarWritten
            || LKeyframeCacheStore.LKeyframeCacheSave(
                identity, keyframes, scannedSpans, LKeyframeSession.LKeyframeGridMilliseconds);

        if (lKeyframePersisted)
        {
            lock (lKeyframeLock)
            {
                lKeyframeSession.LKeyframeSavedSet(keyframes.Length, scannedSpans.Length);
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
}
