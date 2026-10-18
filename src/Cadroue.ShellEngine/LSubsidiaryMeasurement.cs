using Cadroue.Core;

namespace Cadroue.ShellEngine;

internal static class LSubsidiaryMeasurement
{
    private sealed record LSubsidiarySample(LWorkMedia? LSubsidiaryMedia, long? LSubsidiaryBytes)
    {
        public static readonly LSubsidiarySample LSubsidiaryEmpty = new(null, null);
    }

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<
        string,
        LSubsidiarySample> lSubsidiaryCache = new(StringComparer.OrdinalIgnoreCase);

    internal static async Task LSubsidiaryOutputRun(
        LWorkItem lSubsidiaryItem, string lSubsidiaryOutputPath, CancellationToken lSubsidiaryToken)
    {
        if (await LScout.LScoutLoudnessRead(lSubsidiaryOutputPath, lSubsidiaryToken).ConfigureAwait(false)
            is not { } lSubsidiaryLoudness)
        {
            return;
        }

        if (LStationEngine.LStationMeasurement is not { } lSubsidiarySchedule)
        {
            return;
        }

        LSubsidiaryDefer(() => lSubsidiarySchedule.LScheduleLoudnessSet(
            lSubsidiaryItem.LWorkId, lSubsidiaryLoudness));
    }

    internal static async Task LSubsidiarySourceRun(LWorkItem lSubsidiaryItem, CancellationToken lSubsidiaryToken)
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

        if (LStationEngine.LStationMeasurement is not { } lSubsidiarySchedule)
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
            LScoutBytes.LScoutBytesRead(lSubsidiarySource));
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
        if (LStationEngine.LStationPost is { } lSubsidiaryPost)
        {
            lSubsidiaryPost(lSubsidiaryAction);
            return;
        }

        lSubsidiaryAction();
    }
}
