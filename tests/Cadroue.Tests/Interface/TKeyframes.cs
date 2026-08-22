using System.Collections.Concurrent;
using System.Text;

using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.Media;

namespace Cadroue.Tests;

internal sealed record TKeyframeRange(long StartMilliseconds, long EndMilliseconds);

internal sealed record TKeyframeState(
    int Serial,
    IReadOnlyList<long> Keyframes,
    IReadOnlyList<TKeyframeRange> Coverage);

internal sealed record TKeyframeCacheData(
    IReadOnlyList<long> Keyframes,
    IReadOnlyList<int> ScannedSpans);

internal sealed class TKeyframes : IDisposable
{
    internal const int SpanMilliseconds = 20_000;

    private sealed class TScanControl
    {
        internal ManualResetEventSlim Gate { get; } = new(false);
        internal bool HonorCancellation { get; init; }
    }

    private readonly string tKeyframeRoot = Path.Combine(
        Path.GetTempPath(),
        $"Cadroue-Keyframes-{Guid.NewGuid():N}");
    private readonly ConcurrentDictionary<string, long[]> tKeyframeResults = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, TScanControl> tKeyframeControls = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentQueue<TKeyframeRange> tKeyframeScans = new();
    private readonly ConcurrentQueue<TKeyframeState> tKeyframeNotices = new();
    private LKeyframeOrchestrator tKeyframeOrchestrator;
    private bool tKeyframeDisposed;

    internal TKeyframes()
    {
        Directory.CreateDirectory(tKeyframeRoot);
        LSidecarStore.LSidecarFolderSet(tKeyframeRoot, true);
        tKeyframeOrchestrator = OrchestratorCreate();
    }

    internal int ScanCount => tKeyframeScans.Count;

    internal IReadOnlyList<TKeyframeRange> Scans => tKeyframeScans.ToArray();

    internal IReadOnlyList<TKeyframeState> Notices => tKeyframeNotices.ToArray();

    internal TKeyframeState? Latest => tKeyframeNotices.LastOrDefault();

    internal string SourceCreate(string name, string content)
    {
        string path = Path.Combine(tKeyframeRoot, name);
        File.WriteAllText(path, content, Encoding.UTF8);
        return path;
    }

    internal void SourceReplace(string sourcePath, string content) =>
        File.WriteAllText(sourcePath, content, Encoding.UTF8);

    internal bool CacheSave(
        string sourcePath,
        TimeSpan duration,
        IReadOnlyCollection<long> keyframes,
        IReadOnlyCollection<int> scannedSpans) =>
        LSidecarStore.LSidecarSave(
            LKeyframeSourceIdentity.LKeyframeIdentityCreate(sourcePath, duration),
            keyframes,
            scannedSpans,
            SpanMilliseconds);

    internal TKeyframeCacheData? CacheLoad(string sourcePath, TimeSpan duration)
    {
        LSidecar? sidecar = LSidecarStore.LSidecarLoad(
            LKeyframeSourceIdentity.LKeyframeIdentityCreate(sourcePath, duration));
        return sidecar is null
            ? null
            : new TKeyframeCacheData(
                sidecar.LSidecarKeyframesRead().ToArray(),
                sidecar.LSidecarSpansRead(SpanMilliseconds).ToArray());
    }

    internal void ScanResultsSet(string sourcePath, params long[] keyframes) =>
        tKeyframeResults[sourcePath] = keyframes;

    internal void ScanBlock(string sourcePath, bool honorCancellation) =>
        tKeyframeControls[sourcePath] = new TScanControl { HonorCancellation = honorCancellation };

    internal void ScanRelease(string sourcePath)
    {
        if (tKeyframeControls.TryGetValue(sourcePath, out TScanControl? control))
        {
            control.Gate.Set();
        }
    }

    internal void Start(string sourcePath, TimeSpan duration, TimeSpan cursor) =>
        tKeyframeOrchestrator.LKeyframeStart(sourcePath, duration, cursor);

    internal void Suspend() => tKeyframeOrchestrator.LKeyframeSuspend();

    internal async Task WaitForScanCountAsync(int count) =>
        await WaitUntilAsync(() => ScanCount >= count, () => $"Expected at least {count} scan(s). scans={ScanCount}");

    internal async Task WaitForCoverageCountAsync(int count) =>
        await WaitUntilAsync(
            () => Latest is { } latest && latest.Coverage.Count >= count,
            () => $"Expected at least {count} covered span(s). "
                + $"scans={ScanCount} notices={tKeyframeNotices.Count} "
                + $"latestCoverage={Latest?.Coverage.Count ?? -1} "
                + $"scanRanges=[{string.Join(";", Scans.Select(s => $"{s.StartMilliseconds}-{s.EndMilliseconds}"))}]");

    internal static async Task SettleAsync() => await Task.Delay(150);

    public void Dispose()
    {
        if (tKeyframeDisposed)
        {
            return;
        }

        tKeyframeDisposed = true;
        tKeyframeOrchestrator.Dispose();
        foreach (TScanControl control in tKeyframeControls.Values)
        {
            control.Gate.Set();
            control.Gate.Dispose();
        }

        LSidecarStore.LSidecarFolderSet(null, false);
        try
        {
            Directory.Delete(tKeyframeRoot, recursive: true);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
        }
    }

    private LKeyframeOrchestrator OrchestratorCreate()
    {
        var orchestrator = new LKeyframeOrchestrator(Scan);
        orchestrator.LKeyframeNoticeReady += NoticeCapture;
        return orchestrator;
    }

    private IReadOnlyList<LKeyframeEntry> Scan(
        string sourcePath,
        TimeSpan start,
        TimeSpan end,
        CancellationToken cancellationToken)
    {
        tKeyframeScans.Enqueue(new TKeyframeRange(
            (long)start.TotalMilliseconds,
            (long)end.TotalMilliseconds));

        bool ignoreCancellation = false;
        if (tKeyframeControls.TryGetValue(sourcePath, out TScanControl? control))
        {
            if (control.HonorCancellation)
            {
                control.Gate.Wait(cancellationToken);
            }
            else
            {
                ignoreCancellation = true;
                control.Gate.Wait();
            }
        }

        if (!ignoreCancellation)
        {
            cancellationToken.ThrowIfCancellationRequested();
        }
        return tKeyframeResults.GetValueOrDefault(sourcePath, Array.Empty<long>())
            .Where(milliseconds => milliseconds >= start.TotalMilliseconds
                && milliseconds <= end.TotalMilliseconds)
            .Select(milliseconds => new LKeyframeEntry(TimeSpan.FromMilliseconds(milliseconds)))
            .ToArray();
    }

    private void NoticeCapture(LKeyframeNotice notice) =>
        tKeyframeNotices.Enqueue(new TKeyframeState(
            notice.LKeyframeSerial,
            notice.LKeyframeList
                .Select(entry => (long)entry.LKeyframePresentationTime.TotalMilliseconds)
                .ToArray(),
            notice.LKeyframeRanges
                .Select(range => new TKeyframeRange(
                    (long)range.LKeyframeRangeOrigin.TotalMilliseconds,
                    (long)range.LKeyframeRangeLimit.TotalMilliseconds))
                .ToArray()));

    private static async Task WaitUntilAsync(Func<bool> condition, Func<string> failure)
    {
        DateTime deadline = DateTime.UtcNow.AddSeconds(5);
        while (!condition())
        {
            if (DateTime.UtcNow >= deadline)
            {
                throw new TimeoutException(failure());
            }

            await Task.Delay(10);
        }
    }
}
