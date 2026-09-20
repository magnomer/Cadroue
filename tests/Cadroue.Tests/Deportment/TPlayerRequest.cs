using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Infrastructure;
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
        TInterface.TPlayerEngineSet(player, TInterface.TPlayerSeamCreate(open: path => opened = path));

        Task open = TInterface.TPlayerOpenStart(player, "clip.mp4");
        await open;

        Assert.Equal("clip.mp4", opened);
        Assert.True(open.IsCompletedSuccessfully);
    }

    [Fact]
    public async Task OpenStart_PropagatesFailure()
    {
        LPlayer player = TInterface.TPlayerCreate();
        TInterface.TPlayerEngineSet(
            player,
            TInterface.TPlayerSeamCreate(open: _ => throw new InvalidOperationException("no")));

        await Assert.ThrowsAsync<InvalidOperationException>(() => TInterface.TPlayerOpenStart(player, "clip.mp4"));
    }

    [Fact]
    public void Applied_TracksFilter_ResetClears()
    {
        LPlayer player = TInterface.TPlayerCreate();
        List<string> filters = [];
        TInterface.TPlayerEngineSet(player, TInterface.TPlayerSeamCreate(filter: filters.Add));

        Assert.True(TInterface.TPlayerFilterApply(player, "eq=contrast=1.2"));
        Assert.True(TInterface.TPlayerFilterApply(player, "eq=contrast=1.2"));

        Assert.Equal(["eq=contrast=1.2"], filters);
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

    [Fact]
    public void Seek_ClampsToVideoEnd_WhenKnown()
    {
        LPlayer player = TInterface.TPlayerCreate();
        List<TimeSpan> seeks = [];
        TInterface.TPlayerEngineSet(player, TInterface.TPlayerSeamCreate(seek: seeks.Add));

        TInterface.TPlayerSeek(player, TimeSpan.FromSeconds(9));
        TInterface.TPlayerEndSet(player, TimeSpan.FromSeconds(5));
        TInterface.TPlayerSeek(player, TimeSpan.FromSeconds(9));
        TInterface.TPlayerSeek(player, TimeSpan.FromSeconds(2));

        Assert.Equal([TimeSpan.FromSeconds(9), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(2)], seeks);
    }

    [Fact]
    public void EngineSet_DisposesPrevious_AndResetsApplied()
    {
        LPlayer player = TInterface.TPlayerCreate();
        int disposed = 0;
        TInterface.TPlayerEngineSet(player, TInterface.TPlayerSeamCreate(dispose: () => disposed++));
        TInterface.TPlayerFilterApply(player, "eq=gamma=1.1");
        TInterface.TPlayerAudioApply(player, "volume=2");

        Assert.Equal("volume=2", player.LPlayerAudioApplied);

        TInterface.TPlayerEngineSet(player, TInterface.TPlayerSeamCreate());

        Assert.Equal(1, disposed);
        Assert.Equal(string.Empty, player.LPlayerFilterApplied);
        Assert.Null(player.LPlayerAudioApplied);

        TInterface.TPlayerEngineSet(player, null);

        Assert.False(player.LPlayerReady);
        Assert.Equal(TimeSpan.Zero, TInterface.TPlayerTimeRead(player));
        Assert.False(TInterface.TPlayerEndedRead(player));
    }

    [Fact]
    public void PreviewApply_HandsResolvedValues_ContrastOnlyWhenFlyleafActive()
    {
        LPlayer player = TInterface.TPlayerCreate();
        List<LPreviewApplication> applied = [];
        TInterface.TPlayerEngineSet(player, TInterface.TPlayerSeamCreate(preview: applied.Add));
        LPreviewState state = TInterface.TPreviewColorChange(
            TInterface.TPreviewDefaultCreate(), TInterface.TColorCreate(0.5, 1.5, 1, 0));

        TInterface.TPlayerPreviewApply(player, state);

        Assert.Single(applied);
        Assert.Equal(50, applied[0].LPreviewBrightness);
        Assert.Equal(LFlyleaf.LFlyleafActive ? 50 : 0, applied[0].LPreviewContrast);
        Assert.Equal("test", applied[0].LPreviewReason);
    }
}
