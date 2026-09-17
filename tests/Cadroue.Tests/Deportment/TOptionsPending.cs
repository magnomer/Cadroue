using Cadroue.Core;
using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests;

public sealed class TOptionsPending
{
    [Fact]
    public void Draft_EditsStayPending_UntilApply()
    {
        LPreferenceState before = TInterface.TPreferenceCurrentRead();
        LPreferenceState draft = TInterface.TPreferenceClone(before);
        draft.LPreferenceAutoplay = !before.LPreferenceAutoplay;
        LSOptions options = TInterface.TOptionsCreate(draft, false);

        Assert.Equal(before.LPreferenceAutoplay, TInterface.TPreferenceCurrentRead().LPreferenceAutoplay);
        Assert.NotEqual(before.LPreferenceAutoplay, options.LSOptionsDraft.LPreferenceAutoplay);
    }

    [Fact]
    public void Apply_CommitsOnce_ThroughPreference()
    {
        LPreferenceState before = TInterface.TPreferenceCurrentRead();
        int saves = 0;
        TInterface.TPreferenceSaveAttach(_ => { saves++; return true; });
        try
        {
            LPreferenceState draft = TInterface.TPreferenceClone(before);
            draft.LPreferenceAutoplay = !before.LPreferenceAutoplay;
            LSOptions options = TInterface.TOptionsCreate(draft, false);

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

    [Fact]
    public void Engine_FallsBackToFlyleaf_WithoutMpv()
    {
        LPreferenceState draft = TInterface.TPreferenceClone(TInterface.TPreferenceCurrentRead());
        draft.LPreferencePreviewEngine = "Mpv";
        LSOptions options = TInterface.TOptionsCreate(draft, false);
        bool? notified = null;
        TInterface.TOptionsMpvAttach(options, enabled => notified = enabled);

        Assert.Equal("Flyleaf", TInterface.TOptionsEngineRead(options));

        TInterface.TOptionsMpvSet(options, true);
        Assert.True(notified);
        Assert.Equal("Mpv", TInterface.TOptionsEngineRead(options));
    }

    [Fact]
    public void Page_SelectTracksShownSheet()
    {
        LSOptions options = TInterface.TOptionsCreate(
            TInterface.TPreferenceClone(TInterface.TPreferenceCurrentRead()), false);
        Assert.Equal(LSOptionsPage.LSOptionsPageGeneral, options.LSOptionsPage);

        TInterface.TOptionsPageSelect(options, LSOptionsPage.LSOptionsPageTimeline);
        Assert.Equal(LSOptionsPage.LSOptionsPageTimeline, options.LSOptionsPage);
    }

    [Fact]
    public void Ffmpeg_BlankFolder_ResolvesBlankKey()
    {
        Assert.Equal("Options.System.FFmpegBlank", TInterface.TOptionsFfmpegResolve("  "));
    }

    [Fact]
    public void Spectrum_NamesSet_FallsBackToDefault()
    {
        LSSpectrum spectrum = TInterface.TSpectrumCreate("Gone", "unused");
        int changes = 0;
        TInterface.TSpectrumAttach(spectrum, () => changes++);

        TInterface.TSpectrumNamesSet(spectrum, ["Cadroue", "Muted"]);
        Assert.Equal("Cadroue", spectrum.LSSpectrumName);

        TInterface.TSpectrumSelect(spectrum, "Muted");
        TInterface.TSpectrumSelect(spectrum, "Muted");
        Assert.Equal("Muted", spectrum.LSSpectrumName);
        Assert.Equal(2, changes);
        Assert.False(TInterface.TSpectrumRemove(spectrum, "Cadroue", true, null));
    }
}
