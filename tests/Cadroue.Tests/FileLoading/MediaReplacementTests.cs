using Xunit;

namespace Cadroue.Tests;

[Collection("MediaLoad")]
public sealed class MediaReplacementTests
{
    [Fact]
    public async Task SuccessfulReplacement_RemovesPreviousIdentity()
    {
        using var media = new TMediaLoad();
        string previous = media.SourceCreate("previous.mp4");
        string replacement = media.SourceCreate("replacement.mp4");

        await media.LoadAsync(previous);
        await media.LoadAsync(replacement);

        Assert.Equal(replacement, media.CurrentPath);
        Assert.NotEqual(previous, media.CurrentPath);
    }

    [Fact]
    public async Task FailedReplacement_PreservesCompletePreviousState()
    {
        using var media = new TMediaLoad();
        string previous = media.SourceCreate("previous.mp4", 3_000);
        string replacement = media.FailingSourceCreate("broken.mp4");
        await media.LoadAsync(previous);

        TMediaLoadOutcome outcome = await media.LoadAsync(replacement);

        Assert.True(outcome.Failure);
        Assert.Equal(previous, media.CurrentPath);
        Assert.Equal(3_000, media.CurrentDurationMilliseconds);
    }

    [Fact]
    public async Task ObsoleteEarlierCompletion_CannotReplaceNewerSource()
    {
        using var media = new TMediaLoad();
        string earlier = media.GatedSourceCreate("earlier.mp4", 1_000, observeCancellation: false);
        string newer = media.SourceCreate("newer.mp4", 8_000);

        Task<TMediaLoadOutcome> earlierLoad = media.LoadAsync(earlier);
        TMediaLoadOutcome newerOutcome = await media.LoadAsync(newer);
        media.GateComplete(earlier);
        TMediaLoadOutcome earlierOutcome = await earlierLoad;

        Assert.True(newerOutcome.Success);
        Assert.True(earlierOutcome.Obsolete);
        Assert.Equal(newer, media.CurrentPath);
        Assert.Equal(8_000, media.CurrentDurationMilliseconds);
        Assert.Single(media.Events, item => item.Success);
    }
}
