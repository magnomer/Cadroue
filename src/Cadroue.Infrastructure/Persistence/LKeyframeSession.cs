using System.IO;
using Cadroue.Media;

using Cadroue.Core;

namespace Cadroue.Infrastructure;

internal sealed class LKeyframeSession
{
    private sealed record LKeyframeSignature(int LKeyframeSignatureCount, int LKeyframeSignatureSpans);

    internal const int LKeyframeGridMilliseconds = 10000;
    internal const int LKeyframeRetryLimit = 3;
    private const int LKeyframeSaveCount = 10;

    private readonly SortedSet<long> lKeyframeStorage = new();
    private readonly HashSet<int> lKeyframeScannedSpans = new();
    private readonly Dictionary<int, int> lKeyframeFailedCounts = new();
    private readonly Dictionary<int, int> lKeyframeAttempts = new();
    private int lKeyframeUnsavedCount;
    private LKeyframeSignature lKeyframeSavedSignature = new(-1, -1);
    private bool lKeyframeReachBefore;
    private bool lKeyframeReachAfter;
    private int lKeyframeLead = 1;

    internal LKeyframeSourceIdentity? LKeyframeSessionSource { get; private set; }

    internal TimeSpan LKeyframeSessionDuration { get; private set; }

    internal LKeyframeKind LKeyframeSessionKind { get; private set; }

    internal double LKeyframeSessionOrigin { get; private set; }

    internal bool LKeyframeSessionPaused { get; private set; }

    internal int LKeyframeSessionSerial { get; private set; }

    private double LKeyframeSessionRate { get; set; }

    private TimeSpan LKeyframeSessionCursor { get; set; }

    internal int LKeyframeRequestStart(TimeSpan cursor)
    {
        int serial = ++LKeyframeSessionSerial;
        if (cursor != LKeyframeSessionCursor)
        {
            lKeyframeReachBefore = false;
            lKeyframeReachAfter = false;
        }

        LKeyframeSessionCursor = cursor;
        LKeyframeSessionPaused = false;
        return serial;
    }

    internal void LKeyframeSessionSuspend()
    {
        LKeyframeSessionPaused = true;
    }

    internal void LKeyframeSessionSync(TimeSpan cursor)
    {
        if (cursor == LKeyframeSessionCursor)
        {
            return;
        }

        LKeyframeSessionCursor = cursor;
        LKeyframeSessionPaused = true;
        lKeyframeReachBefore = false;
        lKeyframeReachAfter = false;
    }

    internal bool LKeyframeChangeCheck(string sourcePath, TimeSpan duration)
    {
        try
        {
            return LKeyframeSessionSource?.LKeyframeIdentityMatch(sourcePath, duration) != true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return true;
        }
    }

    internal void LKeyframeSessionOpen(TimeSpan duration, LKeyframeKind kind, double rate, double startSeconds)
    {
        lKeyframeStorage.Clear();
        lKeyframeScannedSpans.Clear();
        lKeyframeFailedCounts.Clear();
        lKeyframeAttempts.Clear();
        lKeyframeUnsavedCount = 0;
        lKeyframeSavedSignature = new LKeyframeSignature(-1, -1);
        LKeyframeSessionSource = null;
        LKeyframeSessionDuration = duration;
        LKeyframeSessionKind = kind;
        LKeyframeSessionRate = rate;
        LKeyframeSessionOrigin = startSeconds;
    }

    internal void LKeyframeSourceSet(LKeyframeSourceIdentity identity, long[] keyframes, int[] scannedSpans)
    {
        LKeyframeSessionSource = identity;
        lKeyframeStorage.UnionWith(keyframes);
        lKeyframeScannedSpans.UnionWith(scannedSpans);
        lKeyframeSavedSignature = new LKeyframeSignature(lKeyframeStorage.Count, lKeyframeScannedSpans.Count);
    }

    internal LKeyframeMoveResult LKeyframeTargetFind(TimeSpan cursor, int direction)
    {
        LKeyframeMoveResult result = LKeyframeStep.LKeyframeStepFind(
            lKeyframeStorage,
            lKeyframeScannedSpans,
            LKeyframeSessionDuration,
            cursor,
            direction,
            LKeyframeFailureRead(),
            LKeyframeSessionKind,
            LKeyframeSessionRate);
        bool reach = !result.LKeyframeReady && !result.LKeyframeFailed;
        lKeyframeLead = direction;
        if (direction < 0)
        {
            lKeyframeReachBefore = reach;
        }
        else
        {
            lKeyframeReachAfter = reach;
        }

        return result;
    }

    internal LKeyframeNotice LKeyframeNoticeCreate(int serial)
    {
        LKeyframeEntry[] keyframes;
        LKeyframeScanRange[] scanned;
        if (LKeyframeSessionKind == LKeyframeKind.LKeyframeKindInter)
        {
            keyframes = lKeyframeStorage
                .Select(ms => new LKeyframeEntry(TimeSpan.FromMilliseconds(ms)))
                .ToArray();
            scanned = lKeyframeScannedSpans
                .OrderBy(index => index)
                .Select(index => new LKeyframeScanRange(
                    TimeSpan.FromMilliseconds(index * LKeyframeGridMilliseconds),
                    TimeSpan.FromMilliseconds(Math.Min(
                        LKeyframeSessionDuration.TotalMilliseconds,
                        (index + 1) * LKeyframeGridMilliseconds))))
                .ToArray();
        }
        else
        {
            keyframes = Array.Empty<LKeyframeEntry>();
            scanned = LKeyframeSessionDuration > TimeSpan.Zero
                ? new[] { new LKeyframeScanRange(TimeSpan.Zero, LKeyframeSessionDuration) }
                : Array.Empty<LKeyframeScanRange>();
        }

        return new LKeyframeNotice(serial, keyframes, scanned, LKeyframeSessionKind);
    }

    internal int LKeyframeSpanFind()
    {
        TimeSpan duration = LKeyframeSessionDuration;
        TimeSpan cursor = LKeyframeSessionCursor;
        int last = (int)(Math.Max(0, (long)Math.Ceiling(duration.TotalMilliseconds) - 1) / LKeyframeGridMilliseconds);
        int center = (int)Math.Clamp((long)cursor.TotalMilliseconds / LKeyframeGridMilliseconds, 0, last);
        int steps = (int)Math.Max(
            LKeyframeView.LKeyframeRangeBefore.TotalMilliseconds,
            LKeyframeView.LKeyframeRangeAfter.TotalMilliseconds) / LKeyframeGridMilliseconds;
        for (int step = 1; step <= steps; step++)
        {
            int before = center - step;
            int after = center + step - 1;
            foreach (int spanIndex in lKeyframeLead < 0 ? new[] { before, after } : new[] { after, before })
            {
                if (spanIndex >= 0 && spanIndex <= last && LKeyframeSpanCheck(spanIndex))
                {
                    return spanIndex;
                }
            }
        }

        int found = lKeyframeReachAfter ? LKeyframeReachFind(center, 1, last) : -1;
        return found < 0 && lKeyframeReachBefore ? LKeyframeReachFind(center, -1, last) : found;
    }

    internal void LKeyframeAttemptSet(int spanIndex)
    {
        lKeyframeAttempts[spanIndex] = LKeyframeSessionSerial;
    }

    internal (bool LKeyframePromoted, int LKeyframeNewCount) LKeyframeSpanApply(
        int spanIndex, LKeyframeSpanResult result)
    {
        bool promoted = result.LKeyframeSpanIntra && LKeyframeSessionKind == LKeyframeKind.LKeyframeKindInter;
        int newCount = 0;
        if (promoted)
        {
            LKeyframeSessionKind = LKeyframeKind.LKeyframeKindIntra;
            lKeyframeStorage.Clear();
            lKeyframeScannedSpans.Clear();
            lKeyframeFailedCounts.Clear();
        }
        else
        {
            foreach (var entry in result.LKeyframeSpanEntries)
            {
                if (lKeyframeStorage.Add((long)Math.Round(entry.LKeyframePresentationTime.TotalMilliseconds)))
                {
                    newCount++;
                }
            }

            lKeyframeScannedSpans.Add(spanIndex);
            lKeyframeFailedCounts.Remove(spanIndex);
        }

        return (promoted, newCount);
    }

    internal int LKeyframeFailureAdd(int spanIndex)
    {
        lKeyframeFailedCounts.TryGetValue(spanIndex, out int failedCount);
        lKeyframeFailedCounts[spanIndex] = ++failedCount;
        return failedCount;
    }

    internal bool LKeyframeUnsavedCheck(int newCount)
    {
        lKeyframeUnsavedCount += newCount + 1;
        if (lKeyframeUnsavedCount < LKeyframeSaveCount)
        {
            return false;
        }

        lKeyframeUnsavedCount = 0;
        return true;
    }

    internal void LKeyframeUnsavedClear()
    {
        lKeyframeUnsavedCount = 0;
    }

    internal bool LKeyframeSavedCheck() =>
        lKeyframeSavedSignature == new LKeyframeSignature(lKeyframeStorage.Count, lKeyframeScannedSpans.Count);

    internal void LKeyframeSavedSet(int keyframeCount, int spanCount)
    {
        lKeyframeSavedSignature = new LKeyframeSignature(keyframeCount, spanCount);
    }

    internal (LKeyframeSourceIdentity LKeyframeSource, long[] LKeyframeList, int[] LKeyframeSpans)?
        LKeyframeSaveRead()
    {
        LKeyframeSourceIdentity? identity = LKeyframeSessionSource;
        if (identity is null || LKeyframeSessionKind != LKeyframeKind.LKeyframeKindInter)
        {
            return null;
        }

        return (identity, lKeyframeStorage.ToArray(), lKeyframeScannedSpans.ToArray());
    }

    private HashSet<int> LKeyframeFailureRead() =>
        lKeyframeFailedCounts
            .Where(pair => pair.Value >= LKeyframeRetryLimit)
            .Select(pair => pair.Key)
            .ToHashSet();

    private bool LKeyframeRetryCheck(int spanIndex)
        => lKeyframeFailedCounts.TryGetValue(spanIndex, out int failedCount)
            && failedCount >= LKeyframeRetryLimit;

    private int LKeyframeReachFind(int spanIndex, int direction, int last)
    {
        for (; spanIndex >= 0 && spanIndex <= last; spanIndex += direction)
        {
            if (!lKeyframeScannedSpans.Contains(spanIndex))
            {
                return LKeyframeSpanCheck(spanIndex) ? spanIndex : -1;
            }
        }

        return -1;
    }

    private bool LKeyframeSpanCheck(int spanIndex) =>
        !lKeyframeScannedSpans.Contains(spanIndex)
        && !LKeyframeRetryCheck(spanIndex)
        && !(lKeyframeAttempts.TryGetValue(spanIndex, out int attempted) && attempted == LKeyframeSessionSerial);
}
