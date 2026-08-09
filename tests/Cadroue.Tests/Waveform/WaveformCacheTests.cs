using System.Text;

using Xunit;

namespace Cadroue.Tests;

[Collection("Sidecar")]
public sealed class WaveformCacheTests
{
    [Fact]
    public void SaveAndLoad_PreserveSamplesAndSourceIdentity()
    {
        using var waveform = new TWaveform();
        const string content = "waveform source";
        string source = waveform.SourceCreate("roundtrip.wav", content);
        TimeSpan duration = TimeSpan.FromMilliseconds(4_321);
        byte[] peaks = { 1, 2, 127, 255 };
        byte[] rms = { 3, 4, 64, 128 };

        TWaveformCacheData? saved = waveform.CacheSave(source, duration, peaks, rms);
        TWaveformCacheData? loaded = waveform.CacheLoad(source, duration);

        Assert.NotNull(saved);
        Assert.NotNull(loaded);
        Assert.Equal(saved.FileName, loaded.FileName);
        Assert.Equal(saved.SourceLength, loaded.SourceLength);
        Assert.Equal(saved.SourceWriteTicks, loaded.SourceWriteTicks);
        Assert.Equal(saved.SourceDurationMilliseconds, loaded.SourceDurationMilliseconds);
        Assert.Equal(saved.SourcePartialHash, loaded.SourcePartialHash);
        Assert.Equal(saved.Peaks, loaded.Peaks);
        Assert.Equal(saved.Rms, loaded.Rms);
        Assert.Equal("roundtrip.wav", loaded.FileName);
        Assert.Equal(Encoding.UTF8.GetByteCount(content) + Encoding.UTF8.GetPreamble().Length, loaded.SourceLength);
        Assert.True(loaded.SourceWriteTicks > 0);
        Assert.Equal(4_321, loaded.SourceDurationMilliseconds);
        Assert.False(string.IsNullOrWhiteSpace(loaded.SourcePartialHash));
        Assert.Equal(peaks, loaded.Peaks);
        Assert.Equal(rms, loaded.Rms);
    }

    [Fact]
    public void ChangedSourceIdentity_InvalidatesStaleWaveform()
    {
        using var waveform = new TWaveform();
        string source = waveform.SourceCreate("changing.wav", "before");
        TimeSpan duration = TimeSpan.FromSeconds(2);
        Assert.NotNull(waveform.CacheSave(source, duration, new byte[] { 10, 20 }, new byte[] { 5, 10 }));

        waveform.SourceReplace(source, "after!");

        Assert.Null(waveform.CacheLoad(source, duration));
    }

    [Fact]
    public void MissingCache_ProducesCleanAbsence()
    {
        using var waveform = new TWaveform();
        string source = waveform.SourceCreate("missing.wav", "no waveform cache");

        Assert.Null(waveform.CacheLoad(source, TimeSpan.FromSeconds(1)));
    }

    [Fact]
    public void CorruptCache_FailsSafelyWithoutFabricatedWaveform()
    {
        using var waveform = new TWaveform();
        string source = waveform.SourceCreate("corrupt.wav", "waveform cache source");
        TimeSpan duration = TimeSpan.FromSeconds(2);
        Assert.NotNull(waveform.CacheSave(source, duration, new byte[] { 10, 20 }, new byte[] { 5, 10 }));

        waveform.CacheCorrupt(source, "{ definitely not a cache");

        Assert.Null(waveform.CacheLoad(source, duration));
    }
}
