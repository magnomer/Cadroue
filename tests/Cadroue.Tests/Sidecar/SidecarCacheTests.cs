using Xunit;

namespace Cadroue.Tests;

[Collection("Sidecar")]
public sealed class SidecarCacheTests
{
    [Fact]
    public void LoadingTwice_DoesNotDuplicateCachedEntries()
    {
        using var sidecar = new TSidecar();
        string source = sidecar.SourceCreate("repeat.mp4", "repeat source");
        long[] expected = { 0, 750, 1500 };
        Assert.True(sidecar.Save(source, TimeSpan.FromSeconds(2), expected, new[] { 0, 1 }));

        TSidecar.TSidecarData? first = sidecar.Load(source, TimeSpan.FromSeconds(2));
        TSidecar.TSidecarData? second = sidecar.Load(source, TimeSpan.FromSeconds(2));

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Equal(expected, first.Keyframes);
        Assert.Equal(expected, second.Keyframes);
        Assert.Equal(new[] { 0, 1 }, second.ScannedSpans);
    }

    [Fact]
    public void CorruptCache_DoesNotDestroyIndependentCoreMetadata()
    {
        using var sidecar = new TSidecar();
        string source = sidecar.SourceCreate("corrupt.mp4", "corrupt cache source");
        Assert.True(sidecar.Save(source, TimeSpan.FromSeconds(5), new long[] { 0, 1250 }));
        Assert.True(sidecar.LoudnessSave(source, -17.5));

        sidecar.CacheCorrupt(source, "{ broken cache");

        TSidecar.TSidecarData? loaded = sidecar.Load(source, TimeSpan.FromSeconds(5));
        Assert.NotNull(loaded);
        Assert.Equal(-17.5, loaded.Loudness);
        Assert.Empty(loaded.Keyframes);
        Assert.Null(loaded.Waveform);
    }
}
