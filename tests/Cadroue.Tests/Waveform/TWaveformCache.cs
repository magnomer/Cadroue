using System.Text;

using Xunit;

namespace Cadroue.Tests;

[Collection("Sidecar")]
public sealed class TWaveformCache
{
    [Fact]
    public void SaveAndLoad_PreserveSamplesAndSourceIdentity()
    {
        using var waveform = new TWaveform();
        const string content = "waveform source";
        string source = waveform.TSourceCreate("roundtrip.wav", content);
        TimeSpan duration = TimeSpan.FromMilliseconds(4_321);
        byte[] peaks = { 1, 2, 127, 255 };
        byte[] rms = { 3, 4, 64, 128 };

        TWaveformCacheData? saved = waveform.TKeyframeCacheSave(source, duration, peaks, rms);
        TWaveformCacheData? loaded = waveform.TKeyframeCacheLoad(source, duration);

        Assert.NotNull(saved);
        Assert.NotNull(loaded);
        Assert.Equal(saved.TWaveformFileName, loaded.TWaveformFileName);
        Assert.Equal(saved.TWaveformSourceLength, loaded.TWaveformSourceLength);
        Assert.Equal(saved.TWaveformSourceTicks, loaded.TWaveformSourceTicks);
        Assert.Equal(saved.TWaveformSourceDuration, loaded.TWaveformSourceDuration);
        Assert.Equal(saved.TWaveformSourceHash, loaded.TWaveformSourceHash);
        Assert.Equal(saved.TWaveformPeaks, loaded.TWaveformPeaks);
        Assert.Equal(saved.TWaveformRms, loaded.TWaveformRms);
        Assert.Equal("roundtrip.wav", loaded.TWaveformFileName);
        Assert.Equal(Encoding.UTF8.GetByteCount(content) + Encoding.UTF8.GetPreamble().Length, loaded.TWaveformSourceLength);
        Assert.True(loaded.TWaveformSourceTicks > 0);
        Assert.Equal(4_321, loaded.TWaveformSourceDuration);
        Assert.False(string.IsNullOrWhiteSpace(loaded.TWaveformSourceHash));
        Assert.Equal(peaks, loaded.TWaveformPeaks);
        Assert.Equal(rms, loaded.TWaveformRms);
    }

    [Fact]
    public void ChangedSourceIdentity_InvalidatesStaleWaveform()
    {
        using var waveform = new TWaveform();
        string source = waveform.TSourceCreate("changing.wav", "before");
        TimeSpan duration = TimeSpan.FromSeconds(2);
        Assert.NotNull(waveform.TKeyframeCacheSave(source, duration, new byte[] { 10, 20 }, new byte[] { 5, 10 }));

        waveform.TWaveformSourceSet(source, "after!");

        Assert.Null(waveform.TKeyframeCacheLoad(source, duration));
    }

    [Fact]
    public void MissingCache_ProducesCleanAbsence()
    {
        using var waveform = new TWaveform();
        string source = waveform.TSourceCreate("missing.wav", "no waveform cache");

        Assert.Null(waveform.TKeyframeCacheLoad(source, TimeSpan.FromSeconds(1)));
    }

    [Fact]
    public void CorruptCache_FailsSafelyWithoutFabricatedWaveform()
    {
        using var waveform = new TWaveform();
        string source = waveform.TSourceCreate("corrupt.wav", "waveform cache source");
        TimeSpan duration = TimeSpan.FromSeconds(2);
        Assert.NotNull(waveform.TKeyframeCacheSave(source, duration, new byte[] { 10, 20 }, new byte[] { 5, 10 }));

        waveform.TWaveformCorruptSave(source, "{ definitely not a cache");

        Assert.Null(waveform.TKeyframeCacheLoad(source, duration));
    }
}
