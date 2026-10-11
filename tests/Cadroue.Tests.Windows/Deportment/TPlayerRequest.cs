using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests.Windows;

public sealed class TPlayerRequest
{
    [Fact]
    public void Accurate_ReportsPriorRun_UntilCommit()
    {
        LPlayer player = TInterfacePlayer.TPlayerCreate();

        Assert.False(TInterfacePlayer.TPlayerAccurateSet(player));
        Assert.True(player.LPlayerAccurateActive);
        Assert.True(TInterfacePlayer.TPlayerAccurateSet(player));

        TInterfacePlayer.TPlayerSeekCommit(player, 500);

        Assert.False(player.LPlayerAccurateActive);
        Assert.False(TInterfacePlayer.TPlayerAccurateSet(player));

        TInterfacePlayer.TPlayerAccurateReset(player);

        Assert.False(player.LPlayerAccurateActive);
    }

    [Fact]
    public void Renderer_ResolvesOnFirstCompletedSeek_Only()
    {
        LPlayer player = TInterfacePlayer.TPlayerCreate();

        Assert.False(TInterfacePlayer.TPlayerSeekCommit(player, 0));

        TInterfacePlayer.TPlayerRendererSet(player, true);

        Assert.False(TInterfacePlayer.TPlayerSeekCommit(player, -1));
        Assert.True(player.LPlayerRendererPending);
        Assert.True(TInterfacePlayer.TPlayerSeekCommit(player, 0));
        Assert.False(player.LPlayerRendererPending);
        Assert.False(TInterfacePlayer.TPlayerSeekCommit(player, 10));
    }

    [Fact]
    public async Task OpenStart_RunsOpen_WithPath()
    {
        LPlayer player = TInterfacePlayer.TPlayerCreate();
        string? opened = null;
        TInterfacePlayer.TPlayerEngineSet(player, TInterfacePlayer.TPlayerSeamCreate(open: path => opened = path));

        Task open = TInterfacePlayer.TPlayerOpenStart(player, "clip.mp4");
        await open;

        Assert.Equal("clip.mp4", opened);
        Assert.True(open.IsCompletedSuccessfully);
    }

    [Fact]
    public async Task OpenStart_PropagatesFailure()
    {
        LPlayer player = TInterfacePlayer.TPlayerCreate();
        TInterfacePlayer.TPlayerEngineSet(
            player,
            TInterfacePlayer.TPlayerSeamCreate(open: _ => throw new InvalidOperationException("no")));

        await Assert.ThrowsAsync<InvalidOperationException>(() => TInterfacePlayer.TPlayerOpenStart(
            player, "clip.mp4"));
    }

    [Fact]
    public void Applied_TracksFilter_ResetClears()
    {
        LPlayer player = TInterfacePlayer.TPlayerCreate();
        List<string> filters = [];
        TInterfacePlayer.TPlayerEngineSet(player, TInterfacePlayer.TPlayerSeamCreate(filter: filters.Add));

        Assert.True(TInterfacePlayer.TPlayerFilterApply(player, "eq=contrast=1.2"));
        Assert.True(TInterfacePlayer.TPlayerFilterApply(player, "eq=contrast=1.2"));

        Assert.Equal(["eq=contrast=1.2"], filters);
        Assert.Equal("eq=contrast=1.2", player.LPlayerFilterApplied);

        TInterfacePlayer.TPlayerAppliedReset(player);

        Assert.Equal(string.Empty, player.LPlayerFilterApplied);
        Assert.Null(player.LPlayerAudioApplied);
    }

    [Fact]
    public void End_Set_ReadsBack()
    {
        LPlayer player = TInterfacePlayer.TPlayerCreate();

        Assert.Null(player.LPlayerVideoEnd);
        TInterfacePlayer.TPlayerEndSet(player, TimeSpan.FromSeconds(3));
        Assert.Equal(TimeSpan.FromSeconds(3), player.LPlayerVideoEnd);
    }

    [Fact]
    public void Seek_ClampsToVideoEnd_WhenKnown()
    {
        LPlayer player = TInterfacePlayer.TPlayerCreate();
        List<TimeSpan> seeks = [];
        TInterfacePlayer.TPlayerEngineSet(player, TInterfacePlayer.TPlayerSeamCreate(seek: seeks.Add));

        TInterfacePlayer.TPlayerSeek(player, TimeSpan.FromSeconds(9));
        TInterfacePlayer.TPlayerEndSet(player, TimeSpan.FromSeconds(5));
        TInterfacePlayer.TPlayerSeek(player, TimeSpan.FromSeconds(9));
        TInterfacePlayer.TPlayerSeek(player, TimeSpan.FromSeconds(2));

        Assert.Equal([TimeSpan.FromSeconds(9), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(2)], seeks);
    }

    [Fact]
    public void EngineSet_DisposesPrevious_AndResetsApplied()
    {
        LPlayer player = TInterfacePlayer.TPlayerCreate();
        int disposed = 0;
        TInterfacePlayer.TPlayerEngineSet(player, TInterfacePlayer.TPlayerSeamCreate(dispose: () => disposed++));
        TInterfacePlayer.TPlayerFilterApply(player, "eq=gamma=1.1");
        TInterfacePlayer.TPlayerAudioApply(player, "volume=2");

        Assert.Equal("volume=2", player.LPlayerAudioApplied);

        TInterfacePlayer.TPlayerEngineSet(player, TInterfacePlayer.TPlayerSeamCreate());

        Assert.Equal(1, disposed);
        Assert.Equal(string.Empty, player.LPlayerFilterApplied);
        Assert.Null(player.LPlayerAudioApplied);

        TInterfacePlayer.TPlayerEngineSet(player, null);

        Assert.False(player.LPlayerReady);
        Assert.Equal(TimeSpan.Zero, TInterfacePlayer.TPlayerTimeRead(player));
        Assert.False(TInterfacePlayer.TPlayerEndedRead(player));
    }

    [Fact]
    public void PreviewApply_HandsResolvedValues_ContrastOnlyWhenFlyleafActive()
    {
        LPlayer player = TInterfacePlayer.TPlayerCreate();
        List<LPreviewApplication> applied = [];
        TInterfacePlayer.TPlayerEngineSet(player, TInterfacePlayer.TPlayerSeamCreate(preview: applied.Add));
        LPreviewState state = Interface.TInterface.TPreviewColorChange(
            Interface.TInterface.TPreviewDefaultCreate(), Interface.TInterface.TColorCreate(0.5, 1.5, 1, 0));

        TInterfacePlayer.TPlayerPreviewApply(player, state);

        Assert.Single(applied);
        Assert.Equal(50, applied[0].LPreviewBrightness);
        Assert.Equal(LFlyleaf.LFlyleafActive ? 50 : 0, applied[0].LPreviewContrast);
        Assert.Equal("test", applied[0].LPreviewReason);
    }
}
