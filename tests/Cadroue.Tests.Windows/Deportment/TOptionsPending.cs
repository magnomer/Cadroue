using Cadroue.Core;
using Cadroue.UIDeportment;
using Xunit;
namespace Cadroue.Tests.Windows;
[CollectionDefinition("Preference", DisableParallelization = true)]
public sealed class TPreferenceCollection;
[Collection("Preference")]
public class TOptionsPending
{
    [Fact] public void Draft_EditsStayPending_UntilApply()
    {
        LPreferenceState before = TInterface.TPreferenceCurrentRead();
        LPreferenceState draft = Interface.TInterface.TPreferenceClone(before);
        draft.LPreferenceAutoplay = !before.LPreferenceAutoplay;
        QSOptions options = TInterface.TOptionsCreate(draft, false);
        Assert.Equal(before.LPreferenceAutoplay, TInterface.TPreferenceCurrentRead().LPreferenceAutoplay);
        Assert.NotEqual(before.LPreferenceAutoplay, options.QSOptionsDraft.LPreferenceAutoplay);
    }
    [Fact] public void Apply_CommitsOnce_ThroughPreference()
    {
        Assert.Empty(typeof(QSOptions).GetInterfaces());
        LPreferenceState before = TInterface.TPreferenceCurrentRead();
        int saves = 0;
        TInterface.TPreferenceSaveAttach(_ => { saves++; return true; });
        try
        {
            LPreferenceState draft = Interface.TInterface.TPreferenceClone(before);
            draft.LPreferenceAutoplay = !before.LPreferenceAutoplay;
            QSOptions options = TInterface.TOptionsCreate(draft, false);
            Assert.True(TInterface.TOptionsApply(options));
            Assert.Equal(1, saves);
            Assert.Equal(draft.LPreferenceAutoplay, TInterface.TPreferenceCurrentRead().LPreferenceAutoplay);
            Assert.NotSame(draft, TInterface.TPreferenceCurrentRead());
        }
        finally
        {
            TInterface.TPreferenceSaveAttach(null);
            TInterface.TPreferenceRestore(before);
        }
    }
    [Fact] public void Engine_FallsBackToFlyleaf_WithoutMpv()
    {
        LPreferenceState draft = Interface.TInterface.TPreferenceClone(TInterface.TPreferenceCurrentRead());
        draft.LPreferencePreviewEngine = "Mpv";
        QSOptions options = TInterface.TOptionsCreate(draft, false);
        bool? notified = null;
        TInterface.TOptionsMpvAttach(options, enabled => notified = enabled);
        Assert.Equal("Flyleaf", TInterface.TOptionsEngineRead(options));
        TInterface.TOptionsMpvSet(options, true);
        Assert.True(notified);
        Assert.Equal("Mpv", TInterface.TOptionsEngineRead(options));
    }
    [Fact] public void Page_SelectTracksShownSheet()
    {
        QSOptions options = TInterface.TOptionsCreate(
            Interface.TInterface.TPreferenceClone(TInterface.TPreferenceCurrentRead()), false);
        Assert.Equal(QSOptionsPage.QSOptionsPageGeneral, options.QSOptionsPage);
        TInterface.TOptionsPageSelect(options, QSOptionsPage.QSOptionsPageTimeline);
        Assert.Equal(QSOptionsPage.QSOptionsPageTimeline, options.QSOptionsPage);
    }
}
