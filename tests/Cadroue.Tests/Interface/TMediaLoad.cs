using System.Collections.Concurrent;

using Cadroue.Core;
using Cadroue.Media;

using Xunit;

namespace Cadroue.Tests;

[CollectionDefinition("MediaLoad", DisableParallelization = true)]
public sealed class TMediaLoadCollection;

internal sealed record TMediaLoadOutcome(
    string TMediaKind,
    string TMediaPath,
    long? TMediaDurationMilliseconds,
    string? TMediaError)
{
    internal bool TMediaSuccess => TMediaKind == "Success";
    internal bool TMediaFailure => TMediaKind == "TMediaFailure";
    internal bool TMediaCancelled => TMediaKind == "Cancelled";
    internal bool TMediaObsolete => TMediaKind == "Obsolete";
}

internal sealed class TMediaLoad : IDisposable
{
    private sealed class TMediaSource
    {
        internal required LMediaInfo TMediaInfo { get; init; }
        internal Exception? TMediaFailure { get; init; }
        internal TaskCompletionSource<LMediaInfo>? TMediaGate { get; init; }
        internal bool TMediaObserveCancellation { get; init; }
    }

    private readonly string tMediaLoadRoot = Path.Combine(
        Path.GetTempPath(),
        $"Cadroue-MediaLoad-{Guid.NewGuid():N}");
    private readonly ConcurrentDictionary<string, TMediaSource> tMediaLoadSources =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentQueue<TMediaLoadOutcome> tMediaLoadEvents = new();
    private readonly LMediaLoad tMediaBackend;
    private bool tMediaLoadDisposed;

    internal TMediaLoad()
    {
        Directory.CreateDirectory(tMediaLoadRoot);
        tMediaBackend = new LMediaLoad(TMediaSourceRead);
        tMediaBackend.LMediaLoadCompleted += TMediaOutcomeHandle;
    }

    internal string TSourceCreate(string name, long durationMilliseconds = 1_000)
    {
        string path = Path.Combine(tMediaLoadRoot, name);
        File.WriteAllText(path, name);
        tMediaLoadSources[path] = new TMediaSource { TMediaInfo = TInfoCreate(durationMilliseconds) };
        return path;
    }

    internal string TMediaFailCreate(string name, string message = "probe failed")
    {
        string path = TSourceCreate(name);
        tMediaLoadSources[path] = new TMediaSource
        {
            TMediaInfo = TInfoCreate(1_000),
            TMediaFailure = new InvalidOperationException(message)
        };
        return path;
    }

    internal string TMediaGatedCreate(
        string name,
        long durationMilliseconds,
        bool observeCancellation)
    {
        string path = TSourceCreate(name, durationMilliseconds);
        tMediaLoadSources[path] = new TMediaSource
        {
            TMediaInfo = TInfoCreate(durationMilliseconds),
            TMediaGate = new TaskCompletionSource<LMediaInfo>(TaskCreationOptions.RunContinuationsAsynchronously),
            TMediaObserveCancellation = observeCancellation
        };
        return path;
    }

    internal string TMediaMissingRead(string name) => Path.Combine(tMediaLoadRoot, name);

    internal async Task<TMediaLoadOutcome> TMediaLoadRun(
        string path,
        CancellationToken cancellationToken = default) =>
        TMediaOutcomeCreate(await tMediaBackend.LMediaLoadStart(path, cancellationToken));

    internal bool TMediaClose() => tMediaBackend.LMediaLoadClose();

    internal string? TMediaCurrentPath => tMediaBackend.LMediaCurrentPath;

    internal long? TMediaCurrentDuration =>
        (long?)tMediaBackend.LMediaCurrentInfo?.LMediaInfoDuration.TotalMilliseconds;

    internal IReadOnlyList<TMediaLoadOutcome> TMediaEvents => tMediaLoadEvents.ToArray();

    internal void TMediaGateCommit(string path)
    {
        TMediaSource source = tMediaLoadSources[path];
        source.TMediaGate?.TrySetResult(source.TMediaInfo);
    }

    private async Task<LMediaInfo> TMediaSourceRead(string path, CancellationToken cancellationToken)
    {
        TMediaSource source = tMediaLoadSources[path];
        if (source.TMediaFailure is not null)
        {
            throw source.TMediaFailure;
        }

        if (source.TMediaGate is null)
        {
            return source.TMediaInfo;
        }

        return source.TMediaObserveCancellation
            ? await source.TMediaGate.Task.WaitAsync(cancellationToken)
            : await source.TMediaGate.Task;
    }

    private void TMediaOutcomeHandle(LMediaLoadOutcome outcome) =>
        tMediaLoadEvents.Enqueue(TMediaOutcomeCreate(outcome));

    private static TMediaLoadOutcome TMediaOutcomeCreate(LMediaLoadOutcome outcome) =>
        new(
            outcome.LMediaLoadKind switch
            {
                LMediaLoadKind.LMediaLoadSuccess => "Success",
                LMediaLoadKind.LMediaLoadFailure => "TMediaFailure",
                LMediaLoadKind.LMediaLoadCancelled => "Cancelled",
                LMediaLoadKind.LMediaLoadUnloaded => "Unloaded",
                _ => "Obsolete"
            },
            outcome.LMediaLoadPath,
            (long?)outcome.LMediaLoadInfo?.LMediaInfoDuration.TotalMilliseconds,
            outcome.LMediaLoadError);

    private static LMediaInfo TInfoCreate(long durationMilliseconds) =>
        new(
            TimeSpan.FromMilliseconds(durationMilliseconds),
            1920,
            1080,
            30,
            "test",
            false,
            string.Empty,
            0,
            0);

    public void Dispose()
    {
        if (tMediaLoadDisposed)
        {
            return;
        }

        tMediaLoadDisposed = true;
        tMediaBackend.LMediaLoadCompleted -= TMediaOutcomeHandle;
        tMediaBackend.Dispose();
        try
        {
            Directory.Delete(tMediaLoadRoot, recursive: true);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
        }
    }
}
