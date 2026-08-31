using Xunit;

namespace Cadroue.Tests;

[Collection("Sidecar")]
public sealed class TSidecarCache
{
    [Fact]
    public void LoadingTwice_DoesNotDuplicateCachedEntries()
    {
        using var sidecar = new TSidecar();
        string source = sidecar.TSourceCreate("repeat.mp4", "repeat source");
        long[] expected = { 0, 750, 1500 };
        Assert.True(sidecar.TSidecarSave(source, TimeSpan.FromSeconds(2), expected, new[] { 0, 1 }));

        TSidecar.TSidecarData? first = sidecar.TSidecarLoad(source, TimeSpan.FromSeconds(2));
        TSidecar.TSidecarData? second = sidecar.TSidecarLoad(source, TimeSpan.FromSeconds(2));

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Equal(expected, first.TSidecarKeyframes);
        Assert.Equal(expected, second.TSidecarKeyframes);
        Assert.Equal(new[] { 0, 1 }, second.TSidecarScannedSpans);
    }

    [Fact]
    public void CorruptCache_DoesNotDestroyIndependentCoreMetadata()
    {
        using var sidecar = new TSidecar();
        string source = sidecar.TSourceCreate("corrupt.mp4", "corrupt cache source");
        Assert.True(sidecar.TSidecarSave(source, TimeSpan.FromSeconds(5), new long[] { 0, 1250 }));
        Assert.True(sidecar.TLoudnessSave(source, -17.5));

        sidecar.TSidecarCorruptSave(source, "{ broken cache");

        TSidecar.TSidecarData? loaded = sidecar.TSidecarLoad(source, TimeSpan.FromSeconds(5));
        Assert.NotNull(loaded);
        Assert.Equal(-17.5, loaded.TSidecarLoudness);
        Assert.Empty(loaded.TSidecarKeyframes);
        Assert.Null(loaded.TSidecarWave);
    }
}
