using Cadroue.Tests.Interface;

using Xunit;

namespace Cadroue.Tests.Engine;

[CollectionDefinition("MediaLoad", DisableParallelization = true)]
public sealed class TMediaLoadCollection;

[Collection("MediaLoad")]
public sealed class TCargoFailure
{
    [Fact]
    public async Task UnsupportedSource_FailsWithoutSuccessEvent()
    {
        using var media = new TMediaLoader();
        string source = media.TSourceCreate("unsupported.txt");

        TMediaLoadOutcome outcome = await media.TMediaLoadRun(source);

        Assert.True(outcome.TMediaFailure);
        Assert.Null(media.TMediaCurrentPath);
        Assert.DoesNotContain(media.TMediaEvents, item => item.TMediaSuccess);
    }

    [Fact]
    public async Task MissingSource_FailsSafely()
    {
        using var media = new TMediaLoader();

        TMediaLoadOutcome outcome = await media.TMediaLoadRun(media.TMediaMissingRead("missing.mp4"));

        Assert.True(outcome.TMediaFailure);
        Assert.NotNull(outcome.TMediaError);
        Assert.Null(media.TMediaCurrentPath);
    }

    [Fact]
    public async Task CancelledLoad_DoesNotCorruptLaterSuccess()
    {
        using var media = new TMediaLoader();
        string cancelled = media.TMediaGatedCreate("cancelled.mp4", 2_000, observeCancellation: true);
        string later = media.TSourceCreate("later.mp4", 7_000);
        using var cancellation = new CancellationTokenSource();

        Task<TMediaLoadOutcome> cancelledLoad = media.TMediaLoadRun(cancelled, cancellation.Token);
        cancellation.Cancel();
        TMediaLoadOutcome cancelledOutcome = await cancelledLoad;
        TMediaLoadOutcome laterOutcome = await media.TMediaLoadRun(later);

        Assert.True(cancelledOutcome.TMediaCancelled);
        Assert.True(laterOutcome.TMediaSuccess);
        Assert.Equal(later, media.TMediaCurrentPath);
        Assert.Equal(7_000, media.TMediaCurrentDuration);
    }
}
