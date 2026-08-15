using System.Diagnostics;

using Cadroue.Media;

using Xunit;

namespace Cadroue.Tests;

public sealed class WaveformScannerTests : IDisposable
{
    private static readonly TimeSpan ScanDuration = TimeSpan.FromSeconds(1);

    private readonly string scannerRoot = Path.Combine(
        Path.GetTempPath(),
        $"Cadroue-WaveformScanner-{Guid.NewGuid():N}");

    public WaveformScannerTests() => Directory.CreateDirectory(scannerRoot);

    [Fact]
    public void EmptyGraph_MatchesRawScanPeaks()
    {
        string? source = FixtureCreate("tone.wav", "sine=frequency=440:duration=1");
        if (source is null)
        {
            return;
        }

        LWaveformScanResult raw = LWaveformScanner.LWaveformScan(source, ScanDuration);
        LWaveformScanResult empty = LWaveformScanner.LWaveformScan(source, ScanDuration, default, string.Empty);

        Assert.NotEmpty(raw.LWaveformPeaks);
        Assert.Equal(raw.LWaveformPeaks, empty.LWaveformPeaks);
        Assert.Equal(raw.LWaveformRms, empty.LWaveformRms);
    }

    [Fact]
    public void SilencingGraph_DrivesEnvelopeToNearSilence()
    {
        string? source = FixtureCreate("loud.wav", "sine=frequency=440:duration=1");
        if (source is null)
        {
            return;
        }

        LWaveformScanResult raw = LWaveformScanner.LWaveformScan(source, ScanDuration);
        LWaveformScanResult silenced = LWaveformScanner.LWaveformScan(source, ScanDuration, default, "volume=-120dB");

        Assert.Contains(raw.LWaveformPeaks, peak => peak > 0);
        Assert.All(silenced.LWaveformPeaks, peak => Assert.True(peak <= 1));
    }

    [Fact]
    public void FilteredResult_KeepsPeakAndRmsLengthEqual()
    {
        string? source = FixtureCreate("equal.wav", "sine=frequency=440:duration=1");
        if (source is null)
        {
            return;
        }

        LWaveformScanResult filtered = LWaveformScanner.LWaveformScan(source, ScanDuration, default, "volume=-6dB");

        Assert.Equal(filtered.LWaveformPeaks.Length, filtered.LWaveformRms.Length);
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(scannerRoot, recursive: true);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
        }
    }

    private string? FixtureCreate(string name, string lavfi)
    {
        string path = Path.Combine(scannerRoot, name);
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
}
