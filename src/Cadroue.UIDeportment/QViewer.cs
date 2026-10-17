using Cadroue.Application;
using Cadroue.Conduct;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.Media;

namespace Cadroue.UIDeportment;

public sealed class QViewer
{
    private bool qViewerLoupeActive;
    private LPreviewEngine qViewerEngine = LPreviewEngine.LPreviewEngineFlyleaf;
    private bool qViewerEndReached;
    private bool qViewerResumeInactive;
    private bool qViewerAudioAllowed;
    private bool qViewerAudioEligible;
    private bool qViewerEditEligible;
    private bool qViewerColorPreview;
    private LMediaInfo? qViewerMediaInfo;
    private LPreviewState qViewerPreview = LPreviewState.LPreviewDefaultCreate();

    public QViewer(CUsher lUsher, CWorkspace lWorkspace)
    {
        QViewerWorkspace = lWorkspace;
        QViewerTrace = lWorkspace.CWorkspaceViewer;
        QViewerAudio = new QViewerAudio(QPlayer);
        QViewerHost = new QViewerHost(this);
        QViewerPlayback = new QViewerPlayback(this);
        QViewerMedia = new QViewerMedia(this);
        QViewerSource = new QViewerSource(this, lUsher);
        QViewerRenderer = new QViewerRenderer(this);
        QViewerMpv = new QViewerMpv(this);
        QViewerNeutral = new QViewerNeutral(this);
        QViewerFrame = new QViewerFrame(this);
        QCrop = new QCrop(this);
        QCropDrag = new QCropDrag(this);
        QViewerHandlersAttach(lWorkspace);
    }

    // The viewer's handlers: its audio tool and the workspace's open, restore, playback and commit notices.
    private void QViewerHandlersAttach(CWorkspace lWorkspace)
    {
        QViewerAudio.QViewerAudioChanged += QViewerAudioHandle;
        lWorkspace.CWorkspaceOpened += QViewerOpenHandle;
        lWorkspace.CWorkspaceRestored += QViewerMedia.QViewerLoadRestore;
        lWorkspace.CWorkspacePlayed += QViewerPlayback.QViewerPlayApply;
        lWorkspace.CWorkspacePaused += QViewerPlayback.QViewerPauseApply;
        lWorkspace.CWorkspaceSought += QViewerPlayback.QViewerSeekApply;
        lWorkspace.CWorkspaceIntentSought += lPosition => QViewerPlaybackUpdate(null, lPosition);
        lWorkspace.CWorkspaceCommitPlayed += QViewerMedia.QViewerCommitPlay;
        lWorkspace.CWorkspaceCommitPaused += QViewerMedia.QViewerCommitPause;
    }

    // The workspace that holds this viewer's tenure; drivers and tabs ask it directly.
    public CWorkspace QViewerWorkspace { get; }

    // The viewer's trace gate, built with its workspace; every viewer driver writes its records through it.
    public CViewer QViewerTrace { get; }

    public QPlayer QPlayer { get; } = new();

    public QViewerAudio QViewerAudio { get; }

    public QViewerHost QViewerHost { get; }

    public QViewerPlayback QViewerPlayback { get; }

    public QViewerMedia QViewerMedia { get; }

    public QViewerSource QViewerSource { get; }

    public QViewerRenderer QViewerRenderer { get; }

    public QViewerMpv QViewerMpv { get; }

    public QViewerNeutral QViewerNeutral { get; }

    public QViewerFrame QViewerFrame { get; }

    public QCrop QCrop { get; }

    public QCropDrag QCropDrag { get; }

    public event Action<LCargo>? QViewerMediaChanged;

    public LCargo? QViewerCargo { get; private set; }
    public event Action<bool>? QViewerPlayingChanged;
    public event Action? QViewerEngineChanged;
    public event Action? QViewerPreviewChanged;
    public event Action<string>? QViewerOpenNotice;
    public event Action<string>? QViewerOpenRequest;

    // Kept for the surface (PViewer); the drivers read the gate.
    public bool QViewerMpvActive => QViewerTrace.CViewerMpvActive;

    // Kept for the surface (PViewer); the drivers read the gate.
    public bool QViewerHostVisible => QViewerTrace.CViewerHostVisible;

    public bool QViewerMpvShown => QViewerTrace.CViewerHostVisible && QViewerTrace.CViewerMpvActive;

    public bool QViewerFlyleafShown => QViewerTrace.CViewerHostVisible && !QViewerTrace.CViewerMpvActive;

    public bool QViewerAudioShown => QViewerTrace.CViewerHostVisible && qViewerAudioEligible;

    public bool QViewerAudioCapable => qViewerEngine == LPreviewEngine.LPreviewEngineMpv;

    public bool QViewerLoupeActive => qViewerLoupeActive;

    public LPreviewEngine QViewerEngine => qViewerEngine;

    public bool QViewerEndReached => qViewerEndReached;

    public bool QViewerResumeInactive => qViewerResumeInactive;

    public bool QViewerAudioAllowed => qViewerAudioAllowed;

    public bool QViewerAudioEligible => qViewerAudioEligible;

    public bool QViewerEditEligible => qViewerEditEligible;

    public bool QViewerColorPreview => qViewerColorPreview;

    public bool QViewerProcessorForced => qViewerColorPreview && LFlyleaf.LFlyleafActive;

    // The committed media's details, read only while the workspace holds the media: presence has one home.
    public LMediaInfo? QViewerMediaInfo => QViewerWorkspace.CWorkspaceMedia ? qViewerMediaInfo : null;

    public LPreviewState QViewerPreview => qViewerPreview;

    public bool QViewerPlaying => qViewerPreview.LPlaybackState.LPlaybackStatePlaying;

    public TimeSpan QViewerPosition => qViewerPreview.LPlaybackState.LPlaybackPosition;

    public TimeSpan QViewerDuration => QViewerMediaInfo?.LMediaInfoDuration ?? TimeSpan.Zero;

    public bool QViewerVideoPresent => QViewerMediaInfo is { LMediaVideoPresent: true };

    public void QViewerLoupeSet(bool lLoupeActive) => qViewerLoupeActive = lLoupeActive;

    public void QViewerEndSet(bool lEndReached) => qViewerEndReached = lEndReached;

    public void QViewerResumeSet(bool lResumeInactive) => qViewerResumeInactive = lResumeInactive;

    public void QViewerAllowSet(bool lAudioAllowed) => qViewerAudioAllowed = lAudioAllowed;

    public void QViewerEligibleSet(bool lAudioEligible, bool lEditEligible, bool lColorPreview)
    {
        qViewerAudioEligible = lAudioEligible;
        qViewerEditEligible = lEditEligible;
        qViewerColorPreview = lColorPreview;
    }

    // The open in order: the notice lets a tab settle the source it leaves, then the request opens the path
    // (QViewerSource first, subscribed when the viewer is built, then the tabs' own follow-up).
    private void QViewerOpenHandle(string lPath)
    {
        QViewerOpenNotice?.Invoke(lPath);
        QViewerOpenRequest?.Invoke(lPath);
    }

    public bool QViewerSurfaceMatch(nint lForeground, nint lSurface, nint lOverlay) =>
        lForeground != nint.Zero && (lForeground == lSurface || lForeground == lOverlay);

    public void QViewerEngineSet(LPreviewEngine lEngine)
    {
        if (!QViewerEngineChange(lEngine))
        {
            return;
        }

        QViewerAudio.QViewerCapableSet(QViewerAudioCapable);
        QViewerEngineChanged?.Invoke();
    }

    // Stores the engine; true when it differs from the one held.
    private bool QViewerEngineChange(LPreviewEngine lEngine)
    {
        if (qViewerEngine == lEngine)
        {
            return false;
        }

        qViewerEngine = lEngine;
        return true;
    }

    private void QViewerAudioHandle()
    {
        QViewerAudio.QViewerAudioApply(QViewerTrace.CViewerMpvActive);
        QViewerPreviewRaise();
    }

    public void QViewerPreviewSet(LPreviewState lPreview) => qViewerPreview = lPreview;

    public void QViewerPreviewRaise() => QViewerPreviewChanged?.Invoke();

    public LPreviewState QViewerRenderRead() =>
        QCrop.QCropActive
            ? qViewerPreview
            : qViewerPreview
                .LRotateFlipChange(LRotateFlip.LRotateDefaultCreate())
                .LCropboxChange(null);

    public void QViewerFilterUpdate()
    {
        if (!QViewerFilterApply())
        {
            return;
        }

        QViewerRefreshRun();
    }

    // The paused player redraws with the new filter; a refused refresh is recorded.
    private void QViewerRefreshRun()
    {
        try
        {
            QPlayer.QPlayerUpdate();
        }
        catch (Exception lRefreshException)
        {
            QViewerTrace.CViewerRefreshRecord(lRefreshException);
        }
    }

    // The preview filter goes to mpv; true when the filter changed on a paused player, which then needs a refresh.
    public bool QViewerFilterApply()
    {
        if (!QViewerTrace.CViewerMpvActive || !QPlayer.QPlayerReady)
        {
            return false;
        }

        string lFilter = LPreview.LPreviewFilterResolve(QViewerRenderRead());
        bool lChanged = lFilter != QPlayer.QPlayerFilterApplied;
        if (!QPlayer.QPlayerFilterApply(lFilter) || !lChanged)
        {
            return false;
        }

        return !QViewerPlaying;
    }

    public void QViewerPreviewApply()
    {
        if (QViewerTrace.CViewerMpvActive)
        {
            QViewerFilterUpdate();
            QViewerPreviewRaise();
            return;
        }

        QPlayer.QPlayerPreviewApply(QViewerRenderRead(), "preview color/geometry");
        QPlayer.QPlayerFactsRecord("Preview color applied");
        QViewerPreviewRaise();
    }

    public void QViewerRotateSet(LRotateFlip lRotateFlip)
    {
        QViewerPreviewSet(qViewerPreview.LRotateFlipChange(lRotateFlip));
        QViewerTrace.CViewerRotatedRecord(
            lRotateFlip.LRotateKind.ToString(),
            lRotateFlip.LRotateFlipHorizontal,
            lRotateFlip.LRotateFlipVertical,
            QPlayer.QPlayerReady);
        QViewerPreviewApply();
        QCrop.QCropOverlayUpdate();
    }

    public void QViewerColorSet(LColor lColor)
    {
        QViewerPreviewSet(qViewerPreview.LColorChange(lColor));
        QViewerPreviewApply();
    }

    public void QViewerMediaRaise(LCargo lCargo)
    {
        QViewerCargoSet(lCargo);
        QViewerMediaDispatch(lCargo);
    }

    private void QViewerCargoSet(LCargo lCargo) => QViewerCargo = lCargo;

    // Each media handler hears the cargo on its own: one handler's failure is recorded and the rest still hear it.
    private void QViewerMediaDispatch(LCargo lCargo)
    {
        Delegate[] lHandlers = QViewerMediaChanged?.GetInvocationList() ?? Array.Empty<Delegate>();
        foreach (Action<LCargo> lHandler in lHandlers.Cast<Action<LCargo>>())
        {
            try
            {
                lHandler(lCargo);
            }
            catch (Exception lException)
            {
                QViewerTrace.CViewerNoticeRecord(lException);
            }
        }
    }

    public void QViewerPlaybackUpdate(bool? lPlaying, TimeSpan? lPosition)
    {
        LPlaybackState lState = qViewerPreview.LPlaybackState;
        LPlaybackState lNext = QViewerPlaybackResolve(lState, lPlaying, lPosition);
        QViewerPreviewSet(qViewerPreview.LPlaybackStateChange(lNext));
        if (lNext.LPlaybackStatePlaying != lState.LPlaybackStatePlaying)
        {
            QViewerPlayingChanged?.Invoke(lNext.LPlaybackStatePlaying);
        }
    }

    // The playback state after an update: each part not given keeps its held value.
    private static LPlaybackState QViewerPlaybackResolve(LPlaybackState lState, bool? lPlaying, TimeSpan? lPosition) =>
        new(lPlaying ?? lState.LPlaybackStatePlaying, lPosition ?? lState.LPlaybackPosition);

    public void QViewerMediaCommit(LCargo lCargo, bool lCropPersistent)
    {
        QViewerInfoSet(lCargo.LCargoMediaInfo);
        QViewerWorkspace.CWorkspaceMediaSet(lCargo.LCargoSourcePath, lCargo.LCargoMediaInfo is not null);
        QViewerCommitSet(QViewerCommitResolve(qViewerPreview, lCropPersistent));
    }

    private void QViewerInfoSet(LMediaInfo? lMediaInfo) => qViewerMediaInfo = lMediaInfo;

    // The preview a fresh media starts from: stopped, its crop kept only when the crop is persistent.
    private static LPreviewState QViewerCommitResolve(LPreviewState lPreview, bool lCropPersistent)
    {
        LPreviewState lStopped = lPreview.LPlaybackStateChange(LPlaybackState.LPlaybackStoppedCreate());
        return lCropPersistent ? lStopped : lStopped.LCropboxChange(null);
    }

    private void QViewerCommitSet(LPreviewState lPreview)
    {
        qViewerPreview = lPreview;
        qViewerEndReached = false;
    }

    public void QViewerMediaClose()
    {
        QViewerInfoSet(null);
        QViewerWorkspace.CWorkspaceMediaClose();
        QViewerPreviewSet(qViewerPreview
            .LCropboxChange(null)
            .LPlaybackStateChange(LPlaybackState.LPlaybackStoppedCreate()));
    }

    public TimeSpan QViewerTimeRead() => QPlayer.QPlayerReady ? QPlayer.QPlayerTimeRead() : QViewerPosition;
}
