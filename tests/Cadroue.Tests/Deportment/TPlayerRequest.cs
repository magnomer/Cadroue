using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests;

public sealed class TPlayerRequest
{
    [Fact]
    public void Accurate_ReportsPriorRun_UntilCommit()
    {
        LPlayer player = TInterface.TPlayerCreate();

        Assert.False(TInterface.TPlayerAccurateSet(player));
        Assert.True(player.LPlayerAccurateActive);
        Assert.True(TInterface.TPlayerAccurateSet(player));

        TInterface.TPlayerSeekCommit(player, 500);

        Assert.False(player.LPlayerAccurateActive);
        Assert.False(TInterface.TPlayerAccurateSet(player));

        TInterface.TPlayerAccurateReset(player);

        Assert.False(player.LPlayerAccurateActive);
    }

    [Fact]
    public void Renderer_ResolvesOnFirstCompletedSeek_Only()
    {
        LPlayer player = TInterface.TPlayerCreate();

        Assert.False(TInterface.TPlayerSeekCommit(player, 0));

        TInterface.TPlayerRendererSet(player, true);

        Assert.False(TInterface.TPlayerSeekCommit(player, -1));
        Assert.True(player.LPlayerRendererPending);
        Assert.True(TInterface.TPlayerSeekCommit(player, 0));
        Assert.False(player.LPlayerRendererPending);
        Assert.False(TInterface.TPlayerSeekCommit(player, 10));
    }

    [Fact]
    public async Task OpenStart_RunsOpen_WithPath()
    {
        LPlayer player = TInterface.TPlayerCreate();
        string? opened = null;

        Task open = TInterface.TPlayerOpenStart(player, "clip.mp4", path => opened = path);
        await open;

        Assert.Equal("clip.mp4", opened);
        Assert.True(open.IsCompletedSuccessfully);
    }

    [Fact]
    public async Task OpenStart_PropagatesFailure()
    {
        LPlayer player = TInterface.TPlayerCreate();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            TInterface.TPlayerOpenStart(player, "clip.mp4", _ => throw new InvalidOperationException("no")));
    }

    [Fact]
    public void Applied_TracksFilter_ResetClears()
    {
        LPlayer player = TInterface.TPlayerCreate();

        TInterface.TPlayerFilterSet(player, "eq=contrast=1.2");

        Assert.Equal("eq=contrast=1.2", player.LPlayerFilterApplied);

        TInterface.TPlayerAppliedReset(player);

        Assert.Equal(string.Empty, player.LPlayerFilterApplied);
        Assert.Null(player.LPlayerAudioApplied);
    }

    [Fact]
    public void End_Set_ReadsBack()
    {
        LPlayer player = TInterface.TPlayerCreate();

        Assert.Null(player.LPlayerVideoEnd);
        TInterface.TPlayerEndSet(player, TimeSpan.FromSeconds(3));
        Assert.Equal(TimeSpan.FromSeconds(3), player.LPlayerVideoEnd);
    }
}
