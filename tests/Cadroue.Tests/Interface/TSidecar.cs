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
        int TSidecarBucketMilliseconds,
        long TSidecarDurationMilliseconds,
        string TSidecarPeaks,
        string TSidecarRms);

    internal sealed record TSidecarData(
        int TSidecarVersion,
        string TSidecarFileName,
        long TSidecarSourceLength,
        long TSidecarDurationMilliseconds,
        double TSidecarLoudness,
        IReadOnlyList<long> TSidecarKeyframes,
        IReadOnlyList<int> TSidecarScannedSpans,
        TSidecarWaveform? TSidecarWave);

    private readonly string tSidecarRoot = Path.Combine(
        Path.GetTempPath(),
        $"Cadroue-Sidecar-{Guid.NewGuid():N}");
    private bool tSidecarDisposed;

    internal TSidecar()
    {
        Directory.CreateDirectory(tSidecarRoot);
        LSidecarStore.LSidecarFolderSet(tSidecarRoot, true);
    }

    internal static TSidecarData? TSidecarCoreParse(string json)
    {
        LSidecarCoreRecord? core = LSidecarParse.LSidecarCoreParse(json);
        return core is null ? null : TSidecarDataCreate(LSidecarParse.LSidecarCreate(core, null));
    }

    internal static int TSectionCountParse(string json) =>
        LSidecarParse.LSidecarCoreParse(json)?.LSidecarSections.Count ?? 0;

    internal string TSourceCreate(string name, string content)
    {
        string path = Path.Combine(tSidecarRoot, name);
        File.WriteAllText(path, content, Encoding.UTF8);
        return path;
    }

    internal bool TSidecarSave(
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

    internal TSidecarData? TSidecarLoad(string sourcePath, TimeSpan duration)
    {
        LSidecar? sidecar = LSidecarStore.LSidecarLoad(
            LKeyframeSourceIdentity.LKeyframeIdentityCreate(sourcePath, duration));
        return sidecar is null ? null : TSidecarDataCreate(sidecar);
    }

    internal TSidecarData? TSidecarRead(string sourcePath)
    {
        LSidecar? sidecar = LSidecarStore.LSidecarRead(LSidecarStore.LSidecarPathRead(sourcePath));
        return sidecar is null ? null : TSidecarDataCreate(sidecar);
    }

    internal bool TLoudnessSave(string sourcePath, double loudness) =>
        LSidecarStore.LSidecarLoudnessSave(sourcePath, loudness);

    internal bool TWaveformSave(
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

    internal void TSidecarPersistCopy(string sourcePath, string destinationPath)
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

    internal sealed record TSidecarDossier(string TSidecarDefect, string TSidecarKind);

    internal bool TDiagnosisSave(
        string sourcePath,
        TimeSpan duration,
        IReadOnlyCollection<TSidecarDossier> dossiers) =>
        LSidecarCacheStore.LSidecarDiagnosisSave(
            sourcePath,
            LKeyframeSourceIdentity.LKeyframeIdentityCreate(sourcePath, duration),
            dossiers
                .Select(dossier => new LSidecarDossier
                {
                    LSidecarDefect = dossier.TSidecarDefect,
                    LSidecarKind = Enum.Parse<LFlawKind>(dossier.TSidecarKind)
                })
                .ToList());

    internal IReadOnlyList<TSidecarDossier>? TDiagnosisRead(string sourcePath) =>
        LSidecarCacheStore.LSidecarDiagnosisRead(sourcePath)
            ?.Select(dossier => new TSidecarDossier(dossier.LSidecarDefect, dossier.LSidecarKind.ToString()))
            .ToList();

    internal void TSidecarSourceSet(string sourcePath, string content) =>
        File.WriteAllText(sourcePath, content, Encoding.UTF8);

    internal void TSidecarCorruptSave(string sourcePath, string content) =>
        File.WriteAllText(
            LSidecarStore.LSidecarCacheResolve(LSidecarStore.LSidecarPathRead(sourcePath)),
            content,
            Encoding.UTF8);

    private static TSidecarData TSidecarDataCreate(LSidecar sidecar) =>
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
