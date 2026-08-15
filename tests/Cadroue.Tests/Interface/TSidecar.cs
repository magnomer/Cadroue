using System.Text;

using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.Media;

using Xunit;

namespace Cadroue.Tests;

[CollectionDefinition("Sidecar", DisableParallelization = true)]
public sealed class TSidecarCollection;

internal sealed class TSidecar : IDisposable
{
    internal sealed record TSidecarWaveform(
        int BucketMilliseconds,
        long DurationMilliseconds,
        string Peaks,
        string Rms);

    internal sealed record TSidecarData(
        int Version,
        string FileName,
        long SourceLength,
        long DurationMilliseconds,
        double Loudness,
        IReadOnlyList<long> Keyframes,
        IReadOnlyList<int> ScannedSpans,
        TSidecarWaveform? Waveform);

    private readonly string tSidecarRoot = Path.Combine(
        Path.GetTempPath(),
        $"Cadroue-Sidecar-{Guid.NewGuid():N}");
    private bool tSidecarDisposed;

    internal TSidecar()
    {
        Directory.CreateDirectory(tSidecarRoot);
        LSidecarStore.LSidecarFolderSet(tSidecarRoot, true);
    }

    internal static TSidecarData? CoreParse(string json)
    {
        LSidecarCoreRecord? core = LSidecarParse.LSidecarCoreParse(json);
        return core is null ? null : DataCreate(LSidecarParse.LSidecarCreate(core, null));
    }

    internal string SourceCreate(string name, string content)
    {
        string path = Path.Combine(tSidecarRoot, name);
        File.WriteAllText(path, content, Encoding.UTF8);
        return path;
    }

    internal bool Save(
        string sourcePath,
        TimeSpan duration,
        IReadOnlyCollection<long> keyframes,
        IReadOnlyCollection<int>? scannedSpans = null,
        int spanGridMilliseconds = 1_000) =>
        LSidecarStore.LSidecarSave(
            LKeyframeSourceIdentity.LKeyframeIdentityCreate(sourcePath, duration),
            keyframes,
            scannedSpans ?? Array.Empty<int>(),
            spanGridMilliseconds);

    internal TSidecarData? Load(string sourcePath, TimeSpan duration)
    {
        LSidecar? sidecar = LSidecarStore.LSidecarLoad(
            LKeyframeSourceIdentity.LKeyframeIdentityCreate(sourcePath, duration));
        return sidecar is null ? null : DataCreate(sidecar);
    }

    internal TSidecarData? Read(string sourcePath)
    {
        LSidecar? sidecar = LSidecarStore.LSidecarRead(LSidecarStore.LSidecarPathRead(sourcePath));
        return sidecar is null ? null : DataCreate(sidecar);
    }

    internal bool LoudnessSave(string sourcePath, double loudness) =>
        LSidecarStore.LSidecarLoudnessSave(sourcePath, loudness);

    internal bool WaveformSave(
        string sourcePath,
        int bucketMilliseconds,
        long durationMilliseconds,
        string peaks,
        string rms) =>
        LSidecarStore.LSidecarWaveformSave(
            sourcePath,
            new LSidecarWaveformRecord
            {
                LSidecarBucketMilliseconds = bucketMilliseconds,
                LSidecarDurationMilliseconds = durationMilliseconds,
                LSidecarPeaks = peaks,
                LSidecarRms = rms
            });

    internal void PersistedCopy(string sourcePath, string destinationPath)
    {
        string sourceCore = LSidecarStore.LSidecarPathRead(sourcePath);
        string destinationCore = LSidecarStore.LSidecarPathRead(destinationPath);
        File.Copy(sourceCore, destinationCore, overwrite: true);

        string sourceCache = LSidecarStore.LSidecarCacheResolve(sourceCore);
        if (File.Exists(sourceCache))
        {
            File.Copy(sourceCache, LSidecarStore.LSidecarCacheResolve(destinationCore), overwrite: true);
        }
    }

    internal void SourceReplace(string sourcePath, string content) =>
        File.WriteAllText(sourcePath, content, Encoding.UTF8);

    internal void CacheCorrupt(string sourcePath, string content) =>
        File.WriteAllText(
            LSidecarStore.LSidecarCacheResolve(LSidecarStore.LSidecarPathRead(sourcePath)),
            content,
            Encoding.UTF8);

    private static TSidecarData DataCreate(LSidecar sidecar) =>
        new(
            sidecar.LSidecarVersion,
            sidecar.LSidecarSource.LSidecarFileName,
            sidecar.LSidecarSource.LSidecarLength,
            sidecar.LSidecarSource.LSidecarDurationMilliseconds,
            sidecar.LSidecarLoudness,
            sidecar.LSidecarKeyframesRead().ToArray(),
            sidecar.LSidecarScannedSpans.ToArray(),
            sidecar.LSidecarWaveform is { } waveform
                ? new TSidecarWaveform(
                    waveform.LSidecarBucketMilliseconds,
                    waveform.LSidecarDurationMilliseconds,
                    waveform.LSidecarPeaks,
                    waveform.LSidecarRms)
                : null);

    public void Dispose()
    {
        if (tSidecarDisposed)
        {
            return;
        }

        tSidecarDisposed = true;
        LSidecarStore.LSidecarFolderSet(null, false);
        try
        {
            Directory.Delete(tSidecarRoot, recursive: true);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
        }
    }
}
