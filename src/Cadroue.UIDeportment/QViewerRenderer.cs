using Cadroue.Application;
using Cadroue.Conduct;
using Cadroue.Core;
using Cadroue.Infrastructure;

namespace Cadroue.UIDeportment;

public sealed record QViewerEngineChoice(
    string QViewerChoiceKey,
    string QViewerChoiceText,
    string QViewerChoiceTip,
    bool QViewerChoiceActive,
    bool QViewerChoiceEnabled);

public sealed record QViewerEnginePlan(
    QViewerEngineChoice QViewerChoiceFlyleaf,
    QViewerEngineChoice QViewerChoiceMpv);

public sealed class QViewerRenderer
{
    private readonly QViewer qViewer;
    private readonly CViewer qViewerTrace;

    public QViewerRenderer(QViewer lOwner)
    {
        qViewer = lOwner;
        qViewerTrace = lOwner.QViewerTrace;
    }

    public event Action? QViewerHostApplied;

    private QPlayer QPlayer => qViewer.QPlayer;

    public void QViewerHostRaise() => QViewerHostApplied?.Invoke();

    public bool QViewerOverlayHandle(bool lHostVisible, double lWidth, double lHeight)
    {
        return qViewerTrace.CViewerOverlayCheck(lHostVisible, qViewer.QViewerMpvShown, lWidth, lHeight);
    }

    public QViewerEnginePlan QViewerEngineRead()
    {
        bool lMpv = LRenderer.LRendererEngineRead() == LPreviewEngine.LPreviewEngineMpv;
        bool lInstalled = LMpv.LMpvAvailableCheck();
        return new QViewerEnginePlan(
            new QViewerEngineChoice(
                LRenderer.LRendererFlyleafToken,
                QLocalizationCatalog.QLocalizationTextRead("Processing.Engine.Flyleaf"),
                QLocalizationCatalog.QLocalizationTextRead("Processing.Engine.FlyleafTooltip"),
                !lMpv,
                true),
            new QViewerEngineChoice(
                LRenderer.LRendererMpvToken,
                QLocalizationCatalog.QLocalizationTextRead("Processing.Engine.Mpv"),
                QLocalizationCatalog.QLocalizationTextRead(
                    lInstalled ? "Processing.Engine.MpvTooltip" : "Processing.Engine.MpvMissing"),
                lMpv,
                lInstalled));
    }

    public void QViewerEngineSelect(string lKey)
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

        QViewerEngineSave(lKey);
        LRenderer.LRendererEngineSet(lMpv ? LPreviewEngine.LPreviewEngineMpv : LPreviewEngine.LPreviewEngineFlyleaf);
    }

    // The chosen engine's key goes into a fresh preference record, which replaces the current one.
    private static void QViewerEngineSave(string lKey)
    {
        LPreferenceState lPreference = LPreference.LPreferenceStateCurrent.LPreferenceClone();
        lPreference.LPreferencePreviewEngine = lKey;
        LPreference.LPreferenceStateSet(lPreference);
    }

    public void QViewerEngineHandle()
    {
        if (qViewerTrace.CViewerUnloaded || !qViewerTrace.CViewerCommandActive)
        {
            return;
        }

        if (qViewerTrace.CViewerMpvActive == (LRenderer.LRendererEngineRead() == LPreviewEngine.LPreviewEngineMpv))
        {
            return;
        }

        QViewerEngineRestore();
    }

    public bool QViewerEngineUpdate()
    {
        bool lWantMpv = LRenderer.LRendererEngineRead() == LPreviewEngine.LPreviewEngineMpv;
        if (lWantMpv == qViewerTrace.CViewerMpvActive)
        {
            return false;
        }

        qViewer.QViewerMedia.QViewerPlayerStop();
        QViewerEngineApply(lWantMpv);
        return true;
    }

    public void QViewerEngineApply(bool lMpv)
    {
        qViewerTrace.CViewerMpvSet(lMpv);
        QPlayer.QPlayerAppliedReset();
        qViewer.QViewerEngineSet(lMpv ? LPreviewEngine.LPreviewEngineMpv : LPreviewEngine.LPreviewEngineFlyleaf);
        QViewerHostApplied?.Invoke();
    }

    public bool QViewerEngineRestore()
    {
        bool lPlaying = qViewer.QViewerResumeInactive || qViewer.QViewerPlaying;
        TimeSpan lPosition = qViewer.QViewerTimeRead();
        bool lSwapped = QViewerEngineUpdate();
        return (lSwapped || qViewer.QViewerWorkspace.CWorkspaceIntent is not null)
            && qViewer.QViewerWorkspace.CWorkspaceRestore(lPosition, lPlaying);
    }

}
