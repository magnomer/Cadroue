using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Infrastructure;

namespace Cadroue.UIDeportment;

public sealed record LViewerEngineChoice(
    string LViewerChoiceKey,
    string LViewerChoiceText,
    string LViewerChoiceTip,
    bool LViewerChoiceActive,
    bool LViewerChoiceEnabled);

public sealed record LViewerEnginePlan(
    LViewerEngineChoice LViewerChoiceFlyleaf,
    LViewerEngineChoice LViewerChoiceMpv);

public sealed class LViewerRenderer
{
    private readonly LViewer lViewer;
    private bool lViewerOverlayShown;

    public LViewerRenderer(LViewer lOwner)
    {
        lViewer = lOwner;
    }

    public event Action? LViewerHostApply;

    private LPlayer LPlayer => lViewer.LPlayer;

    public void LViewerHostRaise() => LViewerHostApply?.Invoke();

    public bool LViewerOverlayHandle(bool lHostVisible, double lWidth, double lHeight)
    {
        bool lShow = lViewer.LViewerMpvShown && lHostVisible && lWidth > 0 && lHeight > 0;
        if (!lShow)
        {
            if (lViewerOverlayShown)
            {
                LTraceLog.LTraceInfoRecord(
                    $"mpv overlay closed (host {(lHostVisible ? "visible" : "hidden")}, "
                    + $"shown={lViewer.LViewerMpvShown}, size {lWidth:0}x{lHeight:0})");
            }

            lViewerOverlayShown = false;
            return false;
        }

        if (!lViewerOverlayShown)
        {
            LTraceLog.LTraceInfoRecord(
                "mpv overlay opened",
                $"top-level transparent window over {lWidth:0}x{lHeight:0}");
        }

        lViewerOverlayShown = true;
        return true;
    }

    public LViewerEnginePlan LViewerEngineRead()
    {
        bool lMpv = LRenderer.LRendererEngineRead() == LPreviewEngine.LPreviewEngineMpv;
        bool lInstalled = LMpv.LMpvAvailableCheck();
        return new LViewerEnginePlan(
            new LViewerEngineChoice(
                LRenderer.LRendererFlyleafToken,
                LLocalization.LLocalizationTextRead("Processing.Engine.Flyleaf"),
                LLocalization.LLocalizationTextRead("Processing.Engine.FlyleafTooltip"),
                !lMpv,
                true),
            new LViewerEngineChoice(
                LRenderer.LRendererMpvToken,
                LLocalization.LLocalizationTextRead("Processing.Engine.Mpv"),
                LLocalization.LLocalizationTextRead(
                    lInstalled ? "Processing.Engine.MpvTooltip" : "Processing.Engine.MpvMissing"),
                lMpv,
                lInstalled));
    }

    public void LViewerEngineSelect(string lKey)
    {
        bool lMpv = string.Equals(lKey, LRenderer.LRendererMpvToken, StringComparison.Ordinal);
        if (lMpv && !LMpv.LMpvAvailableCheck())
        {
            return;
        }

        if (lMpv == (LRenderer.LRendererEngineRead() == LPreviewEngine.LPreviewEngineMpv))
        {
            return;
        }

        LPreferenceState lPreference = LPreference.LPreferenceStateCurrent.LPreferenceClone();
        lPreference.LPreferencePreviewEngine = lKey;
        LPreference.LPreferenceStateSet(lPreference);
        LRenderer.LRendererEngineSet(lMpv ? LPreviewEngine.LPreviewEngineMpv : LPreviewEngine.LPreviewEngineFlyleaf);
    }

    public void LViewerEngineHandle()
    {
        if (lViewer.LViewerUnloaded || !lViewer.LViewerCommandActive)
        {
            return;
        }

        if (lViewer.LViewerMpvActive == (LRenderer.LRendererEngineRead() == LPreviewEngine.LPreviewEngineMpv))
        {
            return;
        }

        LViewerEngineRestore();
    }

    public bool LViewerEngineUpdate()
    {
        bool lWantMpv = LRenderer.LRendererEngineRead() == LPreviewEngine.LPreviewEngineMpv;
        if (lWantMpv == lViewer.LViewerMpvActive)
        {
            return false;
        }

        lViewer.LViewerMedia.LViewerPlayerStop();
        LViewerEngineApply(lWantMpv);
        return true;
    }

    public void LViewerEngineApply(bool lMpv)
    {
        lViewer.LViewerMpvSet(lMpv);
        LPlayer.LPlayerAppliedReset();
        lViewer.LViewerEngineSet(lMpv ? LPreviewEngine.LPreviewEngineMpv : LPreviewEngine.LPreviewEngineFlyleaf);
        LViewerHostApply?.Invoke();
    }

    public bool LViewerEngineRestore()
    {
        LViewerIntent? lPending = lViewer.LViewerIntent;
        string? lSourcePath = lViewer.LViewerSourcePath;
        bool lPlaying = lViewer.LViewerResumeInactive || lViewer.LViewerPlaying;
        TimeSpan lPosition = lViewer.LViewerTimeRead();
        bool lSwapped = LViewerEngineUpdate();
        if (lPending is { } lRequest)
        {
            _ = lViewer.LViewerMedia.LViewerLoadStart(lRequest);
            return true;
        }

        if (!lSwapped || lSourcePath is null)
        {
            return false;
        }

        _ = lViewer.LViewerMedia.LViewerLoadStart(new LViewerIntent(lSourcePath, lPosition, lPlaying));
        return true;
    }

}
