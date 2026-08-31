using Xunit;

namespace Cadroue.Tests;

[Collection("MediaLoad")]
public sealed class TCargoReplacement
{
    [Fact]
    public async Task SuccessfulReplacement_RemovesPreviousIdentity()
    {
        using var media = new TMediaLoad();
        string previous = media.TSourceCreate("previous.mp4");
        string replacement = media.TSourceCreate("replacement.mp4");

        await media.TMediaLoadRun(previous);
        await media.TMediaLoadRun(replacement);

        Assert.Equal(replacement, media.TMediaCurrentPath);
        Assert.NotEqual(previous, media.TMediaCurrentPath);
    }

    [Fact]
    public async Task FailedReplacement_PreservesCompletePreviousState()
    {
        using var media = new TMediaLoad();
        string previous = media.TSourceCreate("previous.mp4", 3_000);
        string replacement = media.TMediaFailCreate("broken.mp4");
        await media.TMediaLoadRun(previous);

        TMediaLoadOutcome outcome = await media.TMediaLoadRun(replacement);

        Assert.True(outcome.TMediaFailure);
        Assert.Equal(previous, media.TMediaCurrentPath);
        Assert.Equal(3_000, media.TMediaCurrentDuration);
    }

    [Fact]
    public async Task ObsoleteEarlierCompletion_CannotReplaceNewerSource()
    {
        using var media = new TMediaLoad();
        string earlier = media.TMediaGatedCreate("earlier.mp4", 1_000, observeCancellation: false);
        string newer = media.TSourceCreate("newer.mp4", 8_000);

        Task<TMediaLoadOutcome> earlierLoad = media.TMediaLoadRun(earlier);
        TMediaLoadOutcome newerOutcome = await media.TMediaLoadRun(newer);
        media.TMediaGateCommit(earlier);
        TMediaLoadOutcome earlierOutcome = await earlierLoad;

        Assert.True(newerOutcome.TMediaSuccess);
        Assert.True(earlierOutcome.TMediaObsolete);
        Assert.Equal(newer, media.TMediaCurrentPath);
        Assert.Equal(8_000, media.TMediaCurrentDuration);
        Assert.Single(media.TMediaEvents, item => item.TMediaSuccess);
    }
}
