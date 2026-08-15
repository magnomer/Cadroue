using Xunit;

namespace Cadroue.Tests;

public sealed class WaveformScannerTests : IDisposable
{
    private static readonly TimeSpan ScanDuration = TimeSpan.FromSeconds(1);

    private readonly TWaveform scannerRelay = new();

    [Fact]
    public void EmptyGraph_MatchesRawScanPeaks()
    {
        string? source = scannerRelay.MediaCreate("tone.wav", "sine=frequency=440:duration=1");
        if (source is null)
        {
            return;
        }

        TWaveformScanData raw = TWaveform.Scan(source, ScanDuration);
        TWaveformScanData empty = TWaveform.Scan(source, ScanDuration, string.Empty);

        Assert.NotEmpty(raw.Peaks);
        Assert.Equal(raw.Peaks, empty.Peaks);
        Assert.Equal(raw.Rms, empty.Rms);
    }

    [Fact]
    public void SilencingGraph_DrivesEnvelopeToNearSilence()
    {
        string? source = scannerRelay.MediaCreate("loud.wav", "sine=frequency=440:duration=1");
        if (source is null)
        {
            return;
        }

        TWaveformScanData raw = TWaveform.Scan(source, ScanDuration);
        TWaveformScanData silenced = TWaveform.Scan(source, ScanDuration, "volume=-120dB");

        Assert.Contains(raw.Peaks, peak => peak > 0);
        Assert.All(silenced.Peaks, peak => Assert.True(peak <= 1));
    }

    [Fact]
    public void FilteredResult_KeepsPeakAndRmsLengthEqual()
    {
        string? source = scannerRelay.MediaCreate("equal.wav", "sine=frequency=440:duration=1");
        if (source is null)
        {
            return;
        }

        TWaveformScanData filtered = TWaveform.Scan(source, ScanDuration, "volume=-6dB");

        Assert.Equal(filtered.Peaks.Length, filtered.Rms.Length);
    }

    public void Dispose() => scannerRelay.Dispose();
}
