using System.Diagnostics;
using System.Text;

using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.Media;

namespace Cadroue.Tests;

internal sealed record TWaveformScanData(byte[] Peaks, byte[] Rms);

internal sealed record TWaveformRecord(
    int BucketMilliseconds,
    long DurationMilliseconds,
    string Peaks,
    string Rms);

internal sealed record TWaveformCacheData(
    string FileName,
    long SourceLength,
    long SourceWriteTicks,
    long SourceDurationMilliseconds,
    string SourcePartialHash,
    byte[] Peaks,
    byte[] Rms);

internal sealed class TWaveform : IDisposable
{
    private readonly string tWaveformRoot = Path.Combine(
        Path.GetTempPath(),
        $"Cadroue-Waveform-{Guid.NewGuid():N}");
    private bool tWaveformDisposed;

    internal TWaveform()
    {
        Directory.CreateDirectory(tWaveformRoot);
        LSidecarStore.LSidecarFolderSet(tWaveformRoot, true);
    }

    internal static int BucketMilliseconds => LWaveform.LWaveformBucketMilliseconds;

    internal static int PeakMaximum => LWaveform.LWaveformPeakMaximum;

    internal static double[] RangeRead(
        byte[] peaks,
        TimeSpan rangeStart,
        TimeSpan rangeEnd,
        int columnCount) =>
        LWaveform.LWaveformRangeRead(peaks, rangeStart, rangeEnd, columnCount);

    internal static TWaveformRecord RecordCreate(
        IReadOnlyCollection<byte> peaks,
        IReadOnlyCollection<byte> rms,
        TimeSpan duration) =>
        Snapshot(LWaveform.LWaveformRecordCreate(peaks, rms, duration));

    internal static byte[] PeaksRead(TWaveformRecord? record) =>
        LWaveform.LWaveformPeaksRead(ProductionRecordCreate(record));

    internal static byte[] RmsRead(TWaveformRecord? record) =>
        LWaveform.LWaveformRmsRead(ProductionRecordCreate(record));

    internal static bool RecordMatch(TWaveformRecord? record, TimeSpan duration) =>
        LWaveform.LWaveformRecordMatch(ProductionRecordCreate(record), duration);

    internal string SourceCreate(string name, string content)
    {
        string path = Path.Combine(tWaveformRoot, name);
        File.WriteAllText(path, content, Encoding.UTF8);
        return path;
    }

    internal TWaveformCacheData? CacheSave(
        string sourcePath,
        TimeSpan duration,
        IReadOnlyCollection<byte> peaks,
        IReadOnlyCollection<byte> rms)
    {
        LKeyframeSourceIdentity identity = LKeyframeSourceIdentity.LKeyframeIdentityCreate(sourcePath, duration);
        if (!LSidecarStore.LSidecarSave(identity, Array.Empty<long>(), Array.Empty<int>(), 1_000)
            || !LSidecarStore.LSidecarWaveformSave(sourcePath, LWaveform.LWaveformRecordCreate(peaks, rms, duration)))
        {
            return null;
        }

        return CacheLoad(sourcePath, duration);
    }

    internal TWaveformCacheData? CacheLoad(string sourcePath, TimeSpan duration)
    {
        LKeyframeSourceIdentity identity = LKeyframeSourceIdentity.LKeyframeIdentityCreate(sourcePath, duration);
        LSidecar? sidecar = LSidecarStore.LSidecarLoad(identity);
        if (sidecar?.LSidecarWaveform is not { } waveform)
        {
            return null;
        }

        return new TWaveformCacheData(
            sidecar.LSidecarSource.LSidecarFileName,
            sidecar.LSidecarSource.LSidecarLength,
            sidecar.LSidecarSource.LSidecarWriteTicks,
            sidecar.LSidecarSource.LSidecarDurationMilliseconds,
            sidecar.LSidecarSource.LSidecarPartialHash,
            LWaveform.LWaveformPeaksRead(waveform),
            LWaveform.LWaveformRmsRead(waveform));
    }

    internal string? MediaCreate(string name, string lavfi)
    {
        string path = Path.Combine(tWaveformRoot, name);
        var start = new ProcessStartInfo(LTool.LToolFfmpegRead())
        {
            Arguments = "-v quiet -nostdin -f lavfi -i \"" + lavfi + "\" -y \"" + path + "\"",
            UseShellExecute = false,
            CreateNoWindow = true
        };

        try
        {
            using Process? process = Process.Start(start);
            if (process is null)
            {
                return null;
            }

            process.WaitForExit();
            return process.ExitCode == 0 && File.Exists(path) ? path : null;
        }
        catch
        {
            return null;
        }
    }

    internal static TWaveformScanData Scan(string sourcePath, TimeSpan duration, string? filterGraph = null)
    {
        LWaveformScanResult result = LWaveformScanner.LWaveformScan(sourcePath, duration, default, filterGraph);
        return new TWaveformScanData(result.LWaveformPeaks, result.LWaveformRms);
    }

    internal void SourceReplace(string sourcePath, string content) =>
        File.WriteAllText(sourcePath, content, Encoding.UTF8);

    internal void CacheCorrupt(string sourcePath, string content) =>
        File.WriteAllText(
            LSidecarStore.LSidecarCacheResolve(LSidecarStore.LSidecarPathRead(sourcePath)),
            content,
            Encoding.UTF8);

    public void Dispose()
    {
        if (tWaveformDisposed)
        {
            return;
        }

        tWaveformDisposed = true;
        LSidecarStore.LSidecarFolderSet(null, false);
        try
        {
            Directory.Delete(tWaveformRoot, recursive: true);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
        }
    }

    private static TWaveformRecord Snapshot(LSidecarWaveformRecord record) =>
        new(
            record.LSidecarBucketMilliseconds,
            record.LSidecarDurationMilliseconds,
            record.LSidecarPeaks,
            record.LSidecarRms);

    private static LSidecarWaveformRecord? ProductionRecordCreate(TWaveformRecord? record) =>
        record is null
            ? null
            : new LSidecarWaveformRecord
            {
                LSidecarBucketMilliseconds = record.BucketMilliseconds,
                LSidecarDurationMilliseconds = record.DurationMilliseconds,
                LSidecarPeaks = record.Peaks,
                LSidecarRms = record.Rms
            };
}
