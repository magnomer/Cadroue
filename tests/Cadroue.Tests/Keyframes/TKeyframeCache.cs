using Xunit;

namespace Cadroue.Tests;

[Collection("Sidecar")]
public sealed class TKeyframeCache
{
    [Fact]
    public async Task NewlyObtainedKeyframes_ExtendCacheWithoutLosingExistingValues()
    {
        using var keyframes = new TKeyframe();
        string source = keyframes.TSourceCreate("merge.mp4", "merge source");
        Assert.True(keyframes.TKeyframeCacheSave(source, TimeSpan.FromSeconds(60), new long[] { 1_000 }, new[] { 0 }));
        keyframes.TKeyframeResultSet(source, 25_000, 45_000);

        keyframes.TKeyframeStart(source, TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(30));
        await keyframes.TKeyframeCoverageRead(3);

        Assert.Equal(new long[] { 1_000, 25_000, 45_000 }, keyframes.TKeyframeLatest!.TKeyframeList);
    }

    [Fact]
    public async Task OverlappingScanResults_AreDistinctAndSortedAfterMerging()
    {
        using var keyframes = new TKeyframe();
        string source = keyframes.TSourceCreate("overlap.mp4", "overlap source");
        keyframes.TKeyframeResultSet(source, 40_000, 5_000, 40_000, 25_000);

        keyframes.TKeyframeStart(source, TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(30));
        await keyframes.TKeyframeCoverageRead(3);

        Assert.Equal(new long[] { 5_000, 25_000, 40_000 }, keyframes.TKeyframeLatest!.TKeyframeList);
    }

    [Fact]
    public void ChangedSourceIdentity_InvalidatesOldCache()
    {
        using var keyframes = new TKeyframe();
        string source = keyframes.TSourceCreate("changed.mp4", "before");
        Assert.True(keyframes.TKeyframeCacheSave(source, TimeSpan.FromSeconds(60), new long[] { 1_000 }, new[] { 0 }));

        keyframes.TKeyframeSourceSet(source, "different-length-content");

        Assert.Null(keyframes.TKeyframeCacheLoad(source, TimeSpan.FromSeconds(60)));
    }

    [Fact]
    public void DurationMismatch_InvalidatesOldCache()
    {
        using var keyframes = new TKeyframe();
        string source = keyframes.TSourceCreate("duration.mp4", "duration source");
        Assert.True(keyframes.TKeyframeCacheSave(source, TimeSpan.FromSeconds(60), new long[] { 1_000 }, new[] { 0 }));

        Assert.Null(keyframes.TKeyframeCacheLoad(source, TimeSpan.FromSeconds(61)));
    }

    [Fact]
    public void SaveAndLoad_PreserveKeyframesAndCoverageMetadata()
    {
        using var keyframes = new TKeyframe();
        string source = keyframes.TSourceCreate("roundtrip.mp4", "roundtrip source");

        Assert.True(keyframes.TKeyframeCacheSave(
            source,
            TimeSpan.FromSeconds(80),
            new long[] { 45_000, 5_000, 25_000 },
            new[] { 2, 0, 1 }));
        TKeyframeCacheData? loaded = keyframes.TKeyframeCacheLoad(source, TimeSpan.FromSeconds(80));

        Assert.NotNull(loaded);
        Assert.Equal(new long[] { 5_000, 25_000, 45_000 }, loaded.TKeyframeList);
        Assert.Equal(new[] { 0, 1, 2 }, loaded.TKeyframeScannedSpans);
    }
}
