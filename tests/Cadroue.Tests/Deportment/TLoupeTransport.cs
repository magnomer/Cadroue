using Cadroue.Core;
using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests;

public sealed class TLoupeTransport
{
    private static (LSLoupe, LViewer) TLoupeBuild(Func<bool>? ended = null)
    {
        LSLoupe loupe = TInterface.TLoupeCreate();
        LViewer viewer = TInterface.TViewerCreate();
        TInterface.TLoupeViewerAttach(loupe, viewer);
        TInterface.TPlayerEngineSet(loupe.LPlayer, TInterface.TPlayerSeamCreate(ended: ended));
        return (loupe, viewer);
    }

    [Fact]
    public void FloatSet_WritesPreference_SelectsOneButton()
    {
        LPreferenceState before = TInterface.TPreferenceCurrentRead();
        LSLoupe loupe = TInterface.TLoupeCreate();
        int changes = 0;
        TInterface.TLoupeFloatAttach(loupe, () => changes++);
        try
        {
            TInterface.TLoupeFloatSet(loupe, LSLoupeFloat.LSLoupeFloatTop);

            Assert.Equal("Top", TInterface.TPreferenceCurrentRead().LPreferenceLoupeFloat);
            Assert.True(loupe.LSLoupeOwned);
            Assert.True(loupe.LSLoupeTopmost);
            Assert.Equal(1, changes);
            Assert.Equal(
                [false, false, true],
                TInterface.TLoupeButtonsRead(loupe).Select(button => button.LSLoupeButtonSelected));

            TInterface.TLoupeFloatSet(loupe, LSLoupeFloat.LSLoupeFloatOff);
            Assert.False(loupe.LSLoupeOwned);
            Assert.False(loupe.LSLoupeTopmost);

            LSLoupe restored = TInterface.TLoupeCreate();
            TInterface.TLoupeFloatRestore(restored);
            Assert.Equal(LSLoupeFloat.LSLoupeFloatOff, restored.LSLoupeFloat);
        }
        finally
        {
            TInterface.TPreferenceRestore(before);
        }
    }

    [Fact]
    public void PlayToggle_RoutesThroughViewer_SyncsPlaying()
    {
        (LSLoupe loupe, LViewer viewer) = TLoupeBuild();
        var notices = new List<bool>();
        TInterface.TLoupePlayingAttach(loupe, notices.Add);

        Assert.True(viewer.LViewerLoupeActive);
        TInterface.TLoupePlayToggle(loupe);

        Assert.True(loupe.LSLoupePlaying);
        Assert.True(viewer.LViewerPlaying);
        Assert.Equal("/PAsset/PCompass/PCompassPause.svg", loupe.LSLoupePlayIcon);
        Assert.False(loupe.LSLoupePlayTinted);

        TInterface.TLoupePlayToggle(loupe);

        Assert.False(loupe.LSLoupePlaying);
        Assert.False(viewer.LViewerPlaying);
        Assert.True(loupe.LSLoupePlayTinted);
        Assert.Equal([true, false], notices);
    }

    [Fact]
    public void Tick_AtEnd_StopsAndMarksEnded_SeekClears()
    {
        bool ended = false;
        (LSLoupe loupe, _) = TLoupeBuild(() => ended);
        TInterface.TLoupePlayToggle(loupe);

        ended = true;
        TInterface.TLoupeTick(loupe);

        Assert.True(loupe.LSLoupeEnded);
        Assert.False(loupe.LSLoupePlaying);
        Assert.False(TInterface.TLoupeResumeCheck(loupe));

        TInterface.TLoupeSeek(loupe, TimeSpan.FromSeconds(2));

        Assert.False(loupe.LSLoupeEnded);
    }

    [Fact]
    public void Close_ThenDetach_ReleasesPlayerAndViewer()
    {
        (LSLoupe loupe, LViewer viewer) = TLoupeBuild();
        int closes = 0;
        TInterface.TLoupeCloseAttach(loupe, () => closes++);
        TInterface.TLoupePlayToggle(loupe);

        TInterface.TLoupeClose(loupe);

        Assert.True(loupe.LSLoupeClosed);
        Assert.False(loupe.LPlayer.LPlayerReady);
        Assert.True(viewer.LViewerLoupeActive);

        TInterface.TLoupeDetach(loupe);

        Assert.False(viewer.LViewerLoupeActive);
        Assert.Equal(0, closes);
    }
}
