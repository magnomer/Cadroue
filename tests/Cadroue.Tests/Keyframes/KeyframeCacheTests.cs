using Xunit;

namespace Cadroue.Tests;

[Collection("Sidecar")]
public sealed class KeyframeCacheTests
{
    [Fact]
    public async Task NewlyObtainedKeyframes_ExtendCacheWithoutLosingExistingValues()
    {
        using var keyframes = new TKeyframes();
        string source = keyframes.SourceCreate("merge.mp4", "merge source");
        Assert.True(keyframes.CacheSave(source, TimeSpan.FromSeconds(60), new long[] { 1_000 }, new[] { 0 }));
        keyframes.ScanResultsSet(source, 25_000, 45_000);

        keyframes.Start(source, TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(30));
        await keyframes.WaitForCoverageCountAsync(3);

        Assert.Equal(new long[] { 1_000, 25_000, 45_000 }, keyframes.Latest!.Keyframes);
    }

    [Fact]
    public async Task OverlappingScanResults_AreDistinctAndSortedAfterMerging()
    {
        using var keyframes = new TKeyframes();
        string source = keyframes.SourceCreate("overlap.mp4", "overlap source");
        keyframes.ScanResultsSet(source, 40_000, 5_000, 40_000, 25_000);

        keyframes.Start(source, TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(30));
        await keyframes.WaitForCoverageCountAsync(3);

        Assert.Equal(new long[] { 5_000, 25_000, 40_000 }, keyframes.Latest!.Keyframes);
    }

    [Fact]
    public void ChangedSourceIdentity_InvalidatesOldCache()
    {
        using var keyframes = new TKeyframes();
        string source = keyframes.SourceCreate("changed.mp4", "before");
        Assert.True(keyframes.CacheSave(source, TimeSpan.FromSeconds(60), new long[] { 1_000 }, new[] { 0 }));

        keyframes.SourceReplace(source, "different-length-content");

        Assert.Null(keyframes.CacheLoad(source, TimeSpan.FromSeconds(60)));
    }

    [Fact]
    public void DurationMismatch_InvalidatesOldCache()
    {
        using var keyframes = new TKeyframes();
        string source = keyframes.SourceCreate("duration.mp4", "duration source");
        Assert.True(keyframes.CacheSave(source, TimeSpan.FromSeconds(60), new long[] { 1_000 }, new[] { 0 }));

        Assert.Null(keyframes.CacheLoad(source, TimeSpan.FromSeconds(61)));
    }

    [Fact]
    public void SaveAndLoad_PreserveKeyframesAndCoverageMetadata()
    {
        using var keyframes = new TKeyframes();
        string source = keyframes.SourceCreate("roundtrip.mp4", "roundtrip source");

        Assert.True(keyframes.CacheSave(
            source,
            TimeSpan.FromSeconds(80),
            new long[] { 45_000, 5_000, 25_000 },
            new[] { 2, 0, 1 }));
        TKeyframeCacheData? loaded = keyframes.CacheLoad(source, TimeSpan.FromSeconds(80));

        Assert.NotNull(loaded);
        Assert.Equal(new long[] { 5_000, 25_000, 45_000 }, loaded.Keyframes);
        Assert.Equal(new[] { 0, 1, 2 }, loaded.ScannedSpans);
    }
}
