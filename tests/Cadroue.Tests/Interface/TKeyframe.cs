using System.Collections.Concurrent;
using System.Text;

using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.Media;

namespace Cadroue.Tests;

internal sealed record TKeyframeRange(long TKeyframeStartMilliseconds, long TKeyframeEndMilliseconds);

internal sealed record TKeyframeState(
    int TKeyframeSerial,
    IReadOnlyList<long> TKeyframeList,
    IReadOnlyList<TKeyframeRange> TKeyframeCoverage);

internal sealed record TKeyframeCacheData(
    IReadOnlyList<long> TKeyframeList,
    IReadOnlyList<int> TKeyframeScannedSpans);

internal sealed class TKeyframe : IDisposable
{
    internal const int TKeyframeSpanMilliseconds = 20_000;

    private sealed class TKeyframeControl
    {
        internal ManualResetEventSlim TKeyframeGate { get; } = new(false);
        internal bool TKeyframeHonorCancellation { get; init; }
    }

    private readonly string tKeyframeRoot = Path.Combine(
        Path.GetTempPath(),
        $"Cadroue-Keyframes-{Guid.NewGuid():N}");
    private readonly ConcurrentDictionary<string, long[]> tKeyframeResults = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, TKeyframeControl> tKeyframeControls = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentQueue<TKeyframeRange> tKeyframeScans = new();
    private readonly ConcurrentQueue<TKeyframeState> tKeyframeNotices = new();
    private LKeyframeOrchestrator tKeyframeOrchestrator;
    private bool tKeyframeDisposed;

    internal TKeyframe()
    {
        Directory.CreateDirectory(tKeyframeRoot);
        LSidecarStore.LSidecarFolderSet(tKeyframeRoot, true);
        tKeyframeOrchestrator = TKeyframeOrchestratorCreate();
    }

    internal int TKeyframeScanCount => tKeyframeScans.Count;

    internal IReadOnlyList<TKeyframeRange> TKeyframeScans => tKeyframeScans.ToArray();

    internal IReadOnlyList<TKeyframeState> TKeyframeNotices => tKeyframeNotices.ToArray();

    internal TKeyframeState? TKeyframeLatest => tKeyframeNotices.LastOrDefault();

    internal string TSourceCreate(string name, string content)
    {
        string path = Path.Combine(tKeyframeRoot, name);
        File.WriteAllText(path, content, Encoding.UTF8);
        return path;
    }

    internal void TKeyframeSourceSet(string sourcePath, string content) =>
        File.WriteAllText(sourcePath, content, Encoding.UTF8);

    internal bool TKeyframeCacheSave(
        string sourcePath,
        TimeSpan duration,
        IReadOnlyCollection<long> keyframes,
        IReadOnlyCollection<int> scannedSpans) =>
        LSidecarStore.LSidecarSave(
            LKeyframeSourceIdentity.LKeyframeIdentityCreate(sourcePath, duration),
            keyframes,
            scannedSpans,
            TKeyframeSpanMilliseconds);

    internal TKeyframeCacheData? TKeyframeCacheLoad(string sourcePath, TimeSpan duration)
    {
        LSidecar? sidecar = LSidecarStore.LSidecarLoad(
            LKeyframeSourceIdentity.LKeyframeIdentityCreate(sourcePath, duration));
        return sidecar is null
            ? null
            : new TKeyframeCacheData(
                sidecar.LSidecarKeyframesRead().ToArray(),
                sidecar.LSidecarSpansRead(TKeyframeSpanMilliseconds).ToArray());
    }

    internal void TKeyframeResultSet(string sourcePath, params long[] keyframes) =>
        tKeyframeResults[sourcePath] = keyframes;

    internal void TKeyframeScanSuspend(string sourcePath, bool honorCancellation) =>
        tKeyframeControls[sourcePath] = new TKeyframeControl { TKeyframeHonorCancellation = honorCancellation };

    internal void TKeyframeScanRelease(string sourcePath)
    {
        if (tKeyframeControls.TryGetValue(sourcePath, out TKeyframeControl? control))
        {
            control.TKeyframeGate.Set();
        }
    }

    internal void TKeyframeStart(string sourcePath, TimeSpan duration, TimeSpan cursor) =>
        tKeyframeOrchestrator.LKeyframeStart(sourcePath, duration, cursor);

    internal void TKeyframeSuspend() => tKeyframeOrchestrator.LKeyframeSuspend();

    internal async Task TKeyframeScanRead(int count) =>
        await TKeyframeWaitRead(() => TKeyframeScanCount >= count, () => $"Expected at least {count} scan(s). scans={TKeyframeScanCount}");

    internal async Task TKeyframeCoverageRead(int count) =>
        await TKeyframeWaitRead(
            () => TKeyframeLatest is { } latest && latest.TKeyframeCoverage.Count >= count,
            () => $"Expected at least {count} covered span(s). "
                + $"scans={TKeyframeScanCount} notices={tKeyframeNotices.Count} "
                + $"latestCoverage={TKeyframeLatest?.TKeyframeCoverage.Count ?? -1} "
                + $"scanRanges=[{string.Join(";", TKeyframeScans.Select(s => $"{s.TKeyframeStartMilliseconds}-{s.TKeyframeEndMilliseconds}"))}]");

    internal static async Task TKeyframeSettleRun() => await Task.Delay(150);

    public void Dispose()
    {
        if (tKeyframeDisposed)
        {
            return;
        }

        tKeyframeDisposed = true;
        tKeyframeOrchestrator.Dispose();
        foreach (TKeyframeControl control in tKeyframeControls.Values)
        {
            control.TKeyframeGate.Set();
            control.TKeyframeGate.Dispose();
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

    private LKeyframeOrchestrator TKeyframeOrchestratorCreate()
    {
        var orchestrator = new LKeyframeOrchestrator(TKeyframeScan);
        orchestrator.LKeyframeNoticeReady += TKeyframeNoticeRead;
        return orchestrator;
    }

    private IReadOnlyList<LKeyframeEntry> TKeyframeScan(
        string sourcePath,
        TimeSpan start,
        TimeSpan end,
        CancellationToken cancellationToken)
    {
        tKeyframeScans.Enqueue(new TKeyframeRange(
            (long)start.TotalMilliseconds,
            (long)end.TotalMilliseconds));

        bool ignoreCancellation = false;
        if (tKeyframeControls.TryGetValue(sourcePath, out TKeyframeControl? control))
        {
            if (control.TKeyframeHonorCancellation)
            {
                control.TKeyframeGate.Wait(cancellationToken);
            }
            else
            {
                ignoreCancellation = true;
                control.TKeyframeGate.Wait();
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

    private void TKeyframeNoticeRead(LKeyframeNotice notice) =>
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

    private static async Task TKeyframeWaitRead(Func<bool> condition, Func<string> failure)
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
