using Cadroue.Core;
using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests.Windows;

[Collection("Preference")]
public sealed class TLoupeTransport
{
    private static (QSLoupe, QViewer) TLoupeBuild(Func<bool>? ended = null)
    {
        QViewer viewer = TInterfaceViewer.TViewerCreate();
        QSLoupe loupe = TInterface.TLoupeViewerCreate(viewer);
        TInterfacePlayer.TPlayerEngineSet(loupe.QPlayer, TInterfacePlayer.TPlayerSeamCreate(ended: ended));
        return (loupe, viewer);
    }

    [Fact]
    public void FloatSet_WritesPreference_SelectsOneButton()
    {
        LPreferenceState before = TInterface.TPreferenceCurrentRead();
        QSLoupe loupe = TInterface.TLoupeCreate();
        int changes = 0;
        TInterface.TLoupeFloatAttach(loupe, () => changes++);
        try
        {
            TInterface.TLoupeFloatSet(loupe, QSLoupeFloat.QSLoupeFloatTop);

            Assert.Equal("Top", TInterface.TPreferenceCurrentRead().LPreferenceLoupeFloat);
            Assert.True(loupe.QSLoupeOwned);
            Assert.True(loupe.QSLoupeTopmost);
            Assert.Equal(1, changes);
            Assert.Equal(
                [false, false, true],
                TInterface.TLoupeButtonsRead(loupe).Select(button => button.QSLoupeButtonSelected));

            TInterface.TLoupeFloatSet(loupe, QSLoupeFloat.QSLoupeFloatOff);
            Assert.False(loupe.QSLoupeOwned);
            Assert.False(loupe.QSLoupeTopmost);

            QSLoupe restored = TInterface.TLoupeCreate();
            TInterface.TLoupeFloatRestore(restored);
            Assert.Equal(QSLoupeFloat.QSLoupeFloatOff, restored.QSLoupeFloat);
        }
        finally
        {
            TInterface.TPreferenceRestore(before);
        }
    }

    [Fact]
    public void PlayToggle_RoutesThroughViewer_SyncsPlaying()
    {
        (QSLoupe loupe, QViewer viewer) = TLoupeBuild();
        var notices = new List<bool>();
        TInterface.TLoupePlayingAttach(loupe, notices.Add);

        Assert.True(viewer.QViewerLoupeActive);
        TInterface.TLoupePlayToggle(loupe);

        Assert.True(loupe.QSLoupePlaying);
        Assert.True(viewer.QViewerPlaying);
        Assert.Equal("/PAsset/PCompass/PCompassPause.svg", loupe.QSLoupePlayIcon);
        Assert.False(loupe.QSLoupePlayTinted);

        TInterface.TLoupePlayToggle(loupe);

        Assert.False(loupe.QSLoupePlaying);
        Assert.False(viewer.QViewerPlaying);
        Assert.True(loupe.QSLoupePlayTinted);
        Assert.Equal([true, false], notices);
    }

    [Fact]
    public void Tick_AtEnd_StopsAndMarksEnded_SeekClears()
    {
        bool ended = false;
        (QSLoupe loupe, _) = TLoupeBuild(() => ended);
        TInterface.TLoupePlayToggle(loupe);

        ended = true;
        TInterface.TLoupeTick(loupe);

        Assert.True(loupe.QSLoupeEnded);
        Assert.False(loupe.QSLoupePlaying);
        Assert.False(TInterface.TLoupeResumeCheck(loupe));

        TInterface.TLoupeSeek(loupe, TimeSpan.FromSeconds(2));

        Assert.False(loupe.QSLoupeEnded);
    }

    [Fact]
    public void Close_ThenDetach_ReleasesPlayerAndViewer()
    {
        (QSLoupe loupe, QViewer viewer) = TLoupeBuild();
        int closes = 0;
        TInterface.TLoupeCloseAttach(loupe, () => closes++);
        TInterface.TLoupePlayToggle(loupe);

        TInterface.TLoupeClose(loupe);

        Assert.True(loupe.QSLoupeClosed);
        Assert.False(loupe.QPlayer.QPlayerReady);
        Assert.True(viewer.QViewerLoupeActive);

        TInterface.TLoupeDetach(loupe);

        Assert.False(viewer.QViewerLoupeActive);
        Assert.Equal(0, closes);
    }
}
