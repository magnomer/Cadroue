using System.Collections.Concurrent;

using Cadroue.Core;
using Cadroue.Media;

using Xunit;

namespace Cadroue.Tests;

[CollectionDefinition("MediaLoad", DisableParallelization = true)]
public sealed class TMediaLoadCollection;

internal sealed record TMediaLoadOutcome(
    string Kind,
    string Path,
    long? DurationMilliseconds,
    string? Error)
{
    internal bool Success => Kind == "Success";
    internal bool Failure => Kind == "Failure";
    internal bool Cancelled => Kind == "Cancelled";
    internal bool Obsolete => Kind == "Obsolete";
}

internal sealed class TMediaLoad : IDisposable
{
    private sealed class TMediaSource
    {
        internal required LMediaInfo Info { get; init; }
        internal Exception? Failure { get; init; }
        internal TaskCompletionSource<LMediaInfo>? Gate { get; init; }
        internal bool ObserveCancellation { get; init; }
    }

    private readonly string tMediaLoadRoot = Path.Combine(
        Path.GetTempPath(),
        $"Cadroue-MediaLoad-{Guid.NewGuid():N}");
    private readonly ConcurrentDictionary<string, TMediaSource> tMediaLoadSources =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentQueue<TMediaLoadOutcome> tMediaLoadEvents = new();
    private readonly LMediaLoad tMediaLoad;
    private bool tMediaLoadDisposed;

    internal TMediaLoad()
    {
        Directory.CreateDirectory(tMediaLoadRoot);
        tMediaLoad = new LMediaLoad(SourceReadAsync);
        tMediaLoad.LMediaLoadCompleted += OutcomeHandle;
    }

    internal string SourceCreate(string name, long durationMilliseconds = 1_000)
    {
        string path = Path.Combine(tMediaLoadRoot, name);
        File.WriteAllText(path, name);
        tMediaLoadSources[path] = new TMediaSource { Info = InfoCreate(durationMilliseconds) };
        return path;
    }

    internal string FailingSourceCreate(string name, string message = "probe failed")
    {
        string path = SourceCreate(name);
        tMediaLoadSources[path] = new TMediaSource
        {
            Info = InfoCreate(1_000),
            Failure = new InvalidOperationException(message)
        };
        return path;
    }

    internal string GatedSourceCreate(
        string name,
        long durationMilliseconds,
        bool observeCancellation)
    {
        string path = SourceCreate(name, durationMilliseconds);
        tMediaLoadSources[path] = new TMediaSource
        {
            Info = InfoCreate(durationMilliseconds),
            Gate = new TaskCompletionSource<LMediaInfo>(TaskCreationOptions.RunContinuationsAsynchronously),
            ObserveCancellation = observeCancellation
        };
        return path;
    }

    internal string MissingPath(string name) => Path.Combine(tMediaLoadRoot, name);

    internal async Task<TMediaLoadOutcome> LoadAsync(
        string path,
        CancellationToken cancellationToken = default) =>
        OutcomeCreate(await tMediaLoad.LMediaLoadAsync(path, cancellationToken));

    internal bool Unload() => tMediaLoad.LMediaUnload();

    internal string? CurrentPath => tMediaLoad.LMediaLoadCurrentPath;

    internal long? CurrentDurationMilliseconds =>
        (long?)tMediaLoad.LMediaLoadCurrentInfo?.LMediaInfoDuration.TotalMilliseconds;

    internal IReadOnlyList<TMediaLoadOutcome> Events => tMediaLoadEvents.ToArray();

    internal void GateComplete(string path)
    {
        TMediaSource source = tMediaLoadSources[path];
        source.Gate?.TrySetResult(source.Info);
    }

    private async Task<LMediaInfo> SourceReadAsync(string path, CancellationToken cancellationToken)
    {
        TMediaSource source = tMediaLoadSources[path];
        if (source.Failure is not null)
        {
            throw source.Failure;
        }

        if (source.Gate is null)
        {
            return source.Info;
        }

        return source.ObserveCancellation
            ? await source.Gate.Task.WaitAsync(cancellationToken)
            : await source.Gate.Task;
    }

    private void OutcomeHandle(LMediaLoadOutcome outcome) =>
        tMediaLoadEvents.Enqueue(OutcomeCreate(outcome));

    private static TMediaLoadOutcome OutcomeCreate(LMediaLoadOutcome outcome) =>
        new(
            outcome.LMediaLoadKind switch
            {
                LMediaLoadKind.LMediaLoadSuccess => "Success",
                LMediaLoadKind.LMediaLoadFailure => "Failure",
                LMediaLoadKind.LMediaLoadCancelled => "Cancelled",
                LMediaLoadKind.LMediaLoadUnloaded => "Unloaded",
                _ => "Obsolete"
            },
            outcome.LMediaLoadPath,
            (long?)outcome.LMediaLoadInfo?.LMediaInfoDuration.TotalMilliseconds,
            outcome.LMediaLoadError);

    private static LMediaInfo InfoCreate(long durationMilliseconds) =>
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
        tMediaLoad.LMediaLoadCompleted -= OutcomeHandle;
        tMediaLoad.Dispose();
        try
        {
            Directory.Delete(tMediaLoadRoot, recursive: true);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
        }
    }
}
