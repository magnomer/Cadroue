using Xunit;

namespace Cadroue.Tests;

[Collection("MediaLoad")]
public sealed class MediaLoadFailureTests
{
    [Fact]
    public async Task UnsupportedSource_FailsWithoutSuccessEvent()
    {
        using var media = new TMediaLoad();
        string source = media.SourceCreate("unsupported.txt");

        TMediaLoadOutcome outcome = await media.LoadAsync(source);

        Assert.True(outcome.Failure);
        Assert.Null(media.CurrentPath);
        Assert.DoesNotContain(media.Events, item => item.Success);
    }

    [Fact]
    public async Task MissingSource_FailsSafely()
    {
        using var media = new TMediaLoad();

        TMediaLoadOutcome outcome = await media.LoadAsync(media.MissingPath("missing.mp4"));

        Assert.True(outcome.Failure);
        Assert.NotNull(outcome.Error);
        Assert.Null(media.CurrentPath);
    }

    [Fact]
    public async Task CancelledLoad_DoesNotCorruptLaterSuccess()
    {
        using var media = new TMediaLoad();
        string cancelled = media.GatedSourceCreate("cancelled.mp4", 2_000, observeCancellation: true);
        string later = media.SourceCreate("later.mp4", 7_000);
        using var cancellation = new CancellationTokenSource();

        Task<TMediaLoadOutcome> cancelledLoad = media.LoadAsync(cancelled, cancellation.Token);
        cancellation.Cancel();
        TMediaLoadOutcome cancelledOutcome = await cancelledLoad;
        TMediaLoadOutcome laterOutcome = await media.LoadAsync(later);

        Assert.True(cancelledOutcome.Cancelled);
        Assert.True(laterOutcome.Success);
        Assert.Equal(later, media.CurrentPath);
        Assert.Equal(7_000, media.CurrentDurationMilliseconds);
    }
}
