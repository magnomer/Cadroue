using System.Collections.Concurrent;
using System.Diagnostics;

using Cadroue.Core;
using Cadroue.Media;
using Cadroue.ShellEngine;

using Xunit;

namespace Cadroue.Tests;

[CollectionDefinition("MediaProbe", DisableParallelization = true)]
public sealed class TScoutCollection;

internal sealed class TScout : IDisposable
{
    private readonly string tScoutRoot = Path.Combine(Path.GetTempPath(), $"Cadroue-Scout-{Guid.NewGuid():N}");

    internal TScout()
    {
        Directory.CreateDirectory(tScoutRoot);
    }

    internal static LMediaInfo TScoutProbeParse(string output) => LMedia.LMediaFfprobeParse(output);

    internal static ProcessStartInfo TScoutProbeCreate(string sourcePath) =>
        LMedia.LMediaFfprobeStart(sourcePath);

    internal static ProcessStartInfo TScoutStreamCreate(string sourcePath) =>
        LScoutStream.LScoutStreamStart(sourcePath);

    internal static TimeSpan? TScoutEndParse(string output, TimeSpan start) => LMedia.LMediaEndParse(output, start);

    internal LWorkMedia? TMediaRead(string path, CancellationToken cancellationToken = default) =>
        LScout.LScoutMediaRead(path, cancellationToken);

    internal long? TScoutInputRead(string sourcePath, string outputPath)
    {
        LWorkItem work = TEncodeCommand.TWorkCreate(
            LWorkKind.LWorkKindConvert,
            sourcePath,
            outputPath,
            TEncodeCommand.TOutputCreate());
        return LScout.LScoutInputRead(work);
    }

    internal long? TOutputBytesRead(string outputPath) => LScout.LScoutBytesRead(outputPath);

    internal string TScoutFileCreate(string name, int length)
    {
        string path = Path.Combine(tScoutRoot, name);
        File.WriteAllBytes(path, new byte[length]);
        return path;
    }

    internal string TMediaMissingRead(string name) => Path.Combine(tScoutRoot, name);

    public void Dispose()
    {
        try
        {
            Directory.Delete(tScoutRoot, recursive: true);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
        }
    }
}

internal sealed class TScoutProbe : IDisposable
{
    private readonly ConcurrentQueue<LMediaProbeResult> tScoutResults = new();
    private readonly ManualResetEventSlim tScoutIdle = new(initialState: true);
    private readonly Func<string, CancellationToken, LMediaInfo> tScoutPreviousReader;
    private readonly Func<string, CancellationToken, string> tScoutReader;
    private int tScoutActive;
    private bool tScoutDisposed;

    internal TScoutProbe(Func<string, CancellationToken, string> outputRead)
    {
        tScoutReader = outputRead;
        tScoutPreviousReader = LMediaProbe.LMediaProbeReader;
        LMediaProbe.LMediaProbeReader = TScoutProbeRead;
        LMediaProbe.LMediaProbeReady += TScoutProbeHandle;
    }

    internal void TScoutStart(string sourcePath, CancellationToken cancellationToken = default) =>
        LMediaProbe.LMediaProbeDefer(sourcePath, cancellationToken);

    internal IReadOnlyList<LMediaProbeResult> TScoutCountRead(int count, TimeSpan? timeout = null)
    {
        var stopwatch = Stopwatch.StartNew();
        TimeSpan limit = timeout ?? TimeSpan.FromSeconds(5);
        while (tScoutResults.Count < count && stopwatch.Elapsed < limit)
        {
            Thread.Sleep(10);
        }

        return tScoutResults.ToArray();
    }

    internal void TScoutIdleRead(TimeSpan? timeout = null)
    {
        if (!tScoutIdle.Wait(timeout ?? TimeSpan.FromSeconds(5)))
        {
            throw new TimeoutException("Media probe did not become idle.");
        }
    }

    internal IReadOnlyList<LMediaProbeResult> TScoutResultsRead() => tScoutResults.ToArray();

    private LMediaInfo TScoutProbeRead(string sourcePath, CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref tScoutActive);
        tScoutIdle.Reset();
        try
        {
            string output = tScoutReader(sourcePath, cancellationToken);
            return LMedia.LMediaFfprobeParse(output);
        }
        finally
        {
            if (Interlocked.Decrement(ref tScoutActive) == 0)
            {
                tScoutIdle.Set();
            }
        }
    }

    private void TScoutProbeHandle(LMediaProbeResult result) => tScoutResults.Enqueue(result);

    public void Dispose()
    {
        if (tScoutDisposed)
        {
            return;
        }

        tScoutDisposed = true;
        TScoutIdleRead();
        LMediaProbe.LMediaProbeReady -= TScoutProbeHandle;
        LMediaProbe.LMediaProbeReader = tScoutPreviousReader;
        tScoutIdle.Dispose();
    }
}
