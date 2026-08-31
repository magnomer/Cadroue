using Xunit;

namespace Cadroue.Tests;

public sealed class TWaveformScanner : IDisposable
{
    private static readonly TimeSpan TWaveformScanDuration = TimeSpan.FromSeconds(1);

    private readonly TWaveform tWaveformRelay = new();

    [Fact]
    public void EmptyGraph_MatchesRawScanPeaks()
    {
        string? source = tWaveformRelay.TMediaCreate("tone.wav", "sine=frequency=440:duration=1");
        if (source is null)
        {
            return;
        }

        TWaveformScanData raw = TWaveform.TWaveformScan(source, TWaveformScanDuration);
        TWaveformScanData empty = TWaveform.TWaveformScan(source, TWaveformScanDuration, string.Empty);

        Assert.NotEmpty(raw.TWaveformPeaks);
        Assert.Equal(raw.TWaveformPeaks, empty.TWaveformPeaks);
        Assert.Equal(raw.TWaveformRms, empty.TWaveformRms);
    }

    [Fact]
    public void SilencingGraph_DrivesEnvelopeToNearSilence()
    {
        string? source = tWaveformRelay.TMediaCreate("loud.wav", "sine=frequency=440:duration=1");
        if (source is null)
        {
            return;
        }

        TWaveformScanData raw = TWaveform.TWaveformScan(source, TWaveformScanDuration);
        TWaveformScanData silenced = TWaveform.TWaveformScan(source, TWaveformScanDuration, "volume=-120dB");

        Assert.Contains(raw.TWaveformPeaks, peak => peak > 0);
        Assert.All(silenced.TWaveformPeaks, peak => Assert.True(peak <= 1));
    }

    [Fact]
    public void FilteredResult_KeepsPeakAndRmsLengthEqual()
    {
        string? source = tWaveformRelay.TMediaCreate("equal.wav", "sine=frequency=440:duration=1");
        if (source is null)
        {
            return;
        }

        TWaveformScanData filtered = TWaveform.TWaveformScan(source, TWaveformScanDuration, "volume=-6dB");

        Assert.Equal(filtered.TWaveformPeaks.Length, filtered.TWaveformRms.Length);
    }

    public void Dispose() => tWaveformRelay.Dispose();
}
