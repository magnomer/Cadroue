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

        Assert.True(raw.TWaveformComplete);
        Assert.NotEmpty(raw.TWaveformPeaks);
        Assert.Equal(raw.TWaveformPeaks, empty.TWaveformPeaks);
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
        Assert.True(silenced.TWaveformComplete);
        Assert.All(silenced.TWaveformPeaks, peak => Assert.True(peak <= 1));
    }

    [Fact]
    public void CompleteScan_FillsExactlyOneBucketPerDurationSlice()
    {
        string? source = tWaveformRelay.TMediaCreate("count.wav", "sine=frequency=440:duration=1");
        if (source is null)
        {
            return;
        }

        TimeSpan longer = TimeSpan.FromSeconds(2);
        TWaveformScanData exact = TWaveform.TWaveformScan(source, TWaveformScanDuration);
        TWaveformScanData padded = TWaveform.TWaveformScan(source, longer);

        Assert.Equal(TWaveform.TWaveformBucketsResolve(TWaveformScanDuration), exact.TWaveformPeaks.Length);
        Assert.Equal(TWaveform.TWaveformBucketsResolve(longer), padded.TWaveformPeaks.Length);
        Assert.All(padded.TWaveformPeaks.Skip(exact.TWaveformPeaks.Length), peak => Assert.Equal(0, peak));
    }

    [Fact]
    public void HardPannedChannel_KeepsItsFullPeak()
    {
        string? source = tWaveformRelay.TMediaCreate(
            "panned.wav",
            "aevalsrc=0|sin(440*2*PI*t):s=8000:d=1");
        if (source is null)
        {
            return;
        }

        TWaveformScanData scanned = TWaveform.TWaveformScan(source, TWaveformScanDuration);

        Assert.True(scanned.TWaveformComplete);
        Assert.True(scanned.TWaveformPeaks.Max() > TWaveform.TWaveformPeakMaximum * 3 / 4);
    }

    [Fact]
    public void InvalidGraph_ReportsFailureWithoutPeaks()
    {
        string? source = tWaveformRelay.TMediaCreate("broken.wav", "sine=frequency=440:duration=1");
        if (source is null)
        {
            return;
        }

        TWaveformScanData failed = TWaveform.TWaveformScan(source, TWaveformScanDuration, "nosuchfilter=1");

        Assert.False(failed.TWaveformComplete);
        Assert.Empty(failed.TWaveformPeaks);
        Assert.False(string.IsNullOrWhiteSpace(failed.TWaveformDetail));
    }

    [Fact]
    public void MissingSource_ReportsFailureWithoutPeaks()
    {
        TWaveformScanData failed = TWaveform.TWaveformScan(
            Path.Combine(Path.GetTempPath(), $"Cadroue-missing-{Guid.NewGuid():N}.wav"),
            TWaveformScanDuration);

        Assert.False(failed.TWaveformComplete);
        Assert.Empty(failed.TWaveformPeaks);
        Assert.False(string.IsNullOrWhiteSpace(failed.TWaveformDetail));
    }

    public void Dispose() => tWaveformRelay.Dispose();
}
