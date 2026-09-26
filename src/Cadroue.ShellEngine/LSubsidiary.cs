using Cadroue.Core;

namespace Cadroue.ShellEngine;

internal static class LSubsidiary
{
    private const int LSubsidiaryIdleMilliseconds = 500;

    private sealed record LSubsidiaryTask(Func<CancellationToken, Task> LSubsidiaryWork);

    private sealed record LSubsidiarySample(LWorkMedia? LSubsidiaryMedia, long? LSubsidiaryBytes)
    {
        public static readonly LSubsidiarySample LSubsidiaryEmpty = new(null, null);
    }

    private static readonly System.Collections.Concurrent.ConcurrentQueue<LSubsidiaryTask> lSubsidiaryHigh = new();
    private static readonly System.Collections.Concurrent.ConcurrentQueue<LSubsidiaryTask> lSubsidiaryLow = new();
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<
        string,
        LSubsidiarySample> lSubsidiaryCache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly object lSubsidiaryGate = new();
    private static bool lSubsidiaryBusy;
    private static CancellationTokenSource lSubsidiaryCancellation = new();

    public static void LSubsidiaryOutputDefer(LWorkItem lSubsidiaryItem, string lSubsidiaryOutputPath)
    {
        LWorkItem lSubsidiaryCaptured = lSubsidiaryItem;
        string lSubsidiaryPath = lSubsidiaryOutputPath;
        lSubsidiaryHigh.Enqueue(new LSubsidiaryTask(
            lSubsidiaryToken => LSubsidiaryOutputRun(lSubsidiaryCaptured, lSubsidiaryPath, lSubsidiaryToken)));
        LSubsidiaryStart();
    }

    public static void LSubsidiarySourceDefer(IReadOnlyList<LWorkItem> lSubsidiaryItems)
    {
        foreach (LWorkItem lSubsidiaryItem in lSubsidiaryItems)
        {
            LWorkItem lSubsidiaryCaptured = lSubsidiaryItem;
            lSubsidiaryLow.Enqueue(new LSubsidiaryTask(
                lSubsidiaryToken => LSubsidiarySourceRun(lSubsidiaryCaptured, lSubsidiaryToken)));
        }

        LSubsidiaryStart();
    }

    public static void LSubsidiaryCancel()
    {
        CancellationTokenSource lSubsidiaryRetired;
        lock (lSubsidiaryGate)
        {
            while (lSubsidiaryHigh.TryDequeue(out _))
            {
            }

            while (lSubsidiaryLow.TryDequeue(out _))
            {
            }

            lSubsidiaryRetired = lSubsidiaryCancellation;
            lSubsidiaryCancellation = new CancellationTokenSource();
        }

        lSubsidiaryRetired.Cancel();
    }

    private static void LSubsidiaryStart()
    {
        lock (lSubsidiaryGate)
        {
            if (lSubsidiaryBusy || (lSubsidiaryHigh.IsEmpty && lSubsidiaryLow.IsEmpty))
            {
                return;
            }

            lSubsidiaryBusy = true;
        }

        _ = Task.Run(LSubsidiaryRun, CancellationToken.None);
    }

    private static async Task LSubsidiaryRun()
    {
        try
        {
            while (LSubsidiaryNextRead() is { } lSubsidiaryTask)
            {
                CancellationToken lSubsidiaryToken = lSubsidiaryCancellation.Token;
                while (LStation.LStationActiveCheck())
                {
                    lSubsidiaryToken.ThrowIfCancellationRequested();
                    await Task.Delay(LSubsidiaryIdleMilliseconds, lSubsidiaryToken).ConfigureAwait(false);
                }

                await lSubsidiaryTask.LSubsidiaryWork(lSubsidiaryCancellation.Token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            lock (lSubsidiaryGate)
            {
                lSubsidiaryBusy = false;
            }

            if (!lSubsidiaryHigh.IsEmpty || !lSubsidiaryLow.IsEmpty)
            {
                LSubsidiaryStart();
            }
        }
    }

    private static LSubsidiaryTask? LSubsidiaryNextRead()
    {
        if (lSubsidiaryHigh.TryDequeue(out LSubsidiaryTask? lSubsidiaryHighTask))
        {
            return lSubsidiaryHighTask;
        }

        return lSubsidiaryLow.TryDequeue(out LSubsidiaryTask? lSubsidiaryLowTask) ? lSubsidiaryLowTask : null;
    }

    private static async Task LSubsidiaryOutputRun(
        LWorkItem lSubsidiaryItem, string lSubsidiaryOutputPath, CancellationToken lSubsidiaryToken)
    {
        if (await LScout.LScoutLoudnessRead(lSubsidiaryOutputPath, lSubsidiaryToken).ConfigureAwait(false)
            is not { } lSubsidiaryLoudness)
        {
            return;
        }

        if (LStation.LStationSchedule is not { } lSubsidiarySchedule)
        {
            return;
        }

        LSubsidiaryDefer(() => lSubsidiarySchedule.LScheduleLoudnessSet(
            lSubsidiaryItem.LWorkId, lSubsidiaryLoudness));
    }

    private static async Task LSubsidiarySourceRun(LWorkItem lSubsidiaryItem, CancellationToken lSubsidiaryToken)
    {
        bool lSubsidiaryMerge = lSubsidiaryItem.LWorkMergeSources.Count > 1;
        LWorkMedia? lSubsidiaryMedia = null;
        long? lSubsidiaryBytes = null;
        var lSubsidiaryMergeBytes = new List<long>();
        TimeSpan lSubsidiaryMeasured = TimeSpan.Zero;

        foreach (string lSubsidiarySource in LSubsidiarySourcesRead(lSubsidiaryItem))
        {
            LSubsidiarySample lSubsidiarySample =
                await LSubsidiarySampleRead(lSubsidiarySource, lSubsidiaryToken).ConfigureAwait(false);
            if (lSubsidiaryMerge)
            {
                lSubsidiaryMergeBytes.Add(lSubsidiarySample.LSubsidiaryBytes ?? 0);
            }
            else
            {
                lSubsidiaryMedia = lSubsidiarySample.LSubsidiaryMedia;
                lSubsidiaryBytes = lSubsidiarySample.LSubsidiaryBytes;
            }

            if (lSubsidiarySample.LSubsidiaryMedia is { } lSubsidiaryProbed)
            {
                lSubsidiaryMeasured += lSubsidiaryProbed.LWorkMediaDuration;
            }
        }

        if (LStation.LStationSchedule is not { } lSubsidiarySchedule)
        {
            return;
        }

        TimeSpan lSubsidiaryDuration = lSubsidiaryItem.LWorkEnd > TimeSpan.Zero
            ? lSubsidiaryItem.LWorkEnd
            : lSubsidiaryMeasured;
        LSubsidiaryDefer(() => lSubsidiarySchedule.LScheduleSourceSet(
            lSubsidiaryItem.LWorkId,
            lSubsidiaryDuration,
            lSubsidiaryMerge ? null : lSubsidiaryMedia,
            lSubsidiaryMerge ? null : lSubsidiaryBytes,
            lSubsidiaryMerge ? lSubsidiaryMergeBytes : Array.Empty<long>()));
    }

    private static async Task<LSubsidiarySample> LSubsidiarySampleRead(
        string lSubsidiarySource, CancellationToken lSubsidiaryToken)
    {
        if (string.IsNullOrWhiteSpace(lSubsidiarySource))
        {
            return LSubsidiarySample.LSubsidiaryEmpty;
        }

        string? lSubsidiaryKey = LSubsidiaryKeyRead(lSubsidiarySource);
        if (lSubsidiaryKey is not null
            && lSubsidiaryCache.TryGetValue(lSubsidiaryKey, out LSubsidiarySample? lSubsidiaryCached))
        {
            return lSubsidiaryCached;
        }

        var lSubsidiarySample = new LSubsidiarySample(
            await LScout.LScoutSourceRead(lSubsidiarySource, lSubsidiaryToken).ConfigureAwait(false),
            LScout.LScoutBytesRead(lSubsidiarySource));
        if (lSubsidiaryKey is not null)
        {
            lSubsidiaryCache[lSubsidiaryKey] = lSubsidiarySample;
        }

        return lSubsidiarySample;
    }

    private static string? LSubsidiaryKeyRead(string lSubsidiarySource)
    {
        try
        {
            var lSubsidiaryInfo = new System.IO.FileInfo(lSubsidiarySource);
            if (!lSubsidiaryInfo.Exists)
            {
                return null;
            }

            return string.Join(
                "|",
                System.IO.Path.GetFullPath(lSubsidiarySource).ToUpperInvariant(),
                lSubsidiaryInfo.Length,
                lSubsidiaryInfo.LastWriteTimeUtc.Ticks);
        }
        catch (Exception lSubsidiaryException)
            when (lSubsidiaryException is System.IO.IOException
                or UnauthorizedAccessException
                or ArgumentException
                or NotSupportedException)
        {
            return null;
        }
    }

    private static IEnumerable<string> LSubsidiarySourcesRead(LWorkItem lSubsidiaryItem) =>
        lSubsidiaryItem.LWorkMergeSources.Count > 1
            ? lSubsidiaryItem.LWorkMergeSources
            : new[] { lSubsidiaryItem.LWorkSourcePath };

    private static void LSubsidiaryDefer(Action lSubsidiaryAction)
    {
        if (LStation.LStationPost is { } lSubsidiaryPost)
        {
            lSubsidiaryPost(lSubsidiaryAction);
            return;
        }

        lSubsidiaryAction();
    }
}
