using Cadroue.Application;
using Cadroue.Conduct;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.Media;

namespace Cadroue.UIDeportment;

public sealed class QViewer
{
    private int qViewerLoadSerial;
    private bool qViewerMpvActive;
    private bool qViewerHostVisible;
    private bool qViewerLoupeActive;
    private LPreviewEngine qViewerEngine = LPreviewEngine.LPreviewEngineFlyleaf;
    private bool qViewerCommandActive;
    private bool qViewerUnloaded;
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
        QViewerAudio = new QViewerAudio(QPlayer);
        QViewerAudio.QViewerAudioChanged += QViewerAudioHandle;
        QViewerHost = new QViewerHost(this);
        QViewerPlayback = new QViewerPlayback(this);
        QViewerMedia = new QViewerMedia(this);
        QViewerSource = new QViewerSource(this, lUsher);
        QViewerRenderer = new QViewerRenderer(this);
        QViewerMpv = new QViewerMpv(this);
        QViewerNeutral = new QViewerNeutral(this);
        QCrop = new QCrop(this);
        QCropDrag = new QCropDrag(this);
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

    public QPlayer QPlayer { get; } = new();

    public QViewerAudio QViewerAudio { get; }

    public QViewerHost QViewerHost { get; }

    public QViewerPlayback QViewerPlayback { get; }

    public QViewerMedia QViewerMedia { get; }

    public QViewerSource QViewerSource { get; }

    public QViewerRenderer QViewerRenderer { get; }

    public QViewerMpv QViewerMpv { get; }

    public QViewerNeutral QViewerNeutral { get; }

    public QCrop QCrop { get; }

    public QCropDrag QCropDrag { get; }

    public event Action<LCargo>? QViewerMediaChanged;
    public event Action<bool>? QViewerPlayingChanged;
    public event Action? QViewerEngineChanged;
    public event Action? QViewerPreviewChanged;
    public event Action<string>? QViewerOpenNotice;
    public event Action<string>? QViewerOpenRequest;

    public int QViewerLoadSerial => qViewerLoadSerial;

    public bool QViewerMpvActive => qViewerMpvActive;

    public bool QViewerHostVisible => qViewerHostVisible;

    public bool QViewerMpvShown => qViewerHostVisible && qViewerMpvActive;

    public bool QViewerFlyleafShown => qViewerHostVisible && !qViewerMpvActive;

    public bool QViewerAudioShown => qViewerHostVisible && qViewerAudioEligible;

    public bool QViewerAudioCapable => qViewerEngine == LPreviewEngine.LPreviewEngineMpv;

    public bool QViewerLoupeActive => qViewerLoupeActive;

    public LPreviewEngine QViewerEngine => qViewerEngine;

    public bool QViewerCommandActive => qViewerCommandActive;

    public bool QViewerUnloaded => qViewerUnloaded;

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

    public int QViewerSerialChange() => ++qViewerLoadSerial;

    public bool QViewerSerialCheck(int lSerial) =>
        !qViewerUnloaded && lSerial == qViewerLoadSerial && qViewerCommandActive;

    public void QViewerMpvSet(bool lMpvActive) => qViewerMpvActive = lMpvActive;

    public void QViewerHostSet(bool lHostVisible) => qViewerHostVisible = lHostVisible;

    public void QViewerLoupeSet(bool lLoupeActive) => qViewerLoupeActive = lLoupeActive;

    public void QViewerCommandSet(bool lCommandActive) => qViewerCommandActive = lCommandActive;

    // A closing viewer ends its tenure: the workspace forgets the media it held.
    public void QViewerUnloadSet()
    {
        qViewerUnloaded = true;
        QViewerWorkspace.CWorkspaceMediaClear();
    }

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
        if (qViewerEngine == lEngine)
        {
            return;
        }

        qViewerEngine = lEngine;
        QViewerAudio.QViewerCapableSet(QViewerAudioCapable);
        QViewerEngineChanged?.Invoke();
    }

    private void QViewerAudioHandle()
    {
        QViewerAudio.QViewerAudioApply(qViewerMpvActive);
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
        if (!qViewerMpvActive || !QPlayer.QPlayerReady)
        {
            return;
        }

        string lFilter = LPreview.LPreviewFilterResolve(QViewerRenderRead());
        bool lChanged = lFilter != QPlayer.QPlayerFilterApplied;
        if (!QPlayer.QPlayerFilterApply(lFilter) || !lChanged)
        {
            return;
        }

        if (QViewerPlaying)
        {
            return;
        }

        try
        {
            QPlayer.QPlayerUpdate();
        }
        catch (Exception lRefreshException)
        {
            LTraceLog.LTraceErrorRecord($"mpv rejected paused preview refresh: {lRefreshException.Message}");
        }
    }

    public void QViewerPreviewApply()
    {
        if (qViewerMpvActive)
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
        qViewerPreview = qViewerPreview.LRotateFlipChange(lRotateFlip);
        LTraceLog.LTraceInfoRecord(
            $"Viewer rotate/flip set: rotate {lRotateFlip.LRotateKind}, "
            + $"H {lRotateFlip.LRotateFlipHorizontal}, V {lRotateFlip.LRotateFlipVertical}, "
            + $"player {(QPlayer.QPlayerReady ? "ready" : "none")}, overlay remapped");
        QViewerPreviewApply();
        QCrop.QCropOverlayUpdate();
    }

    public void QViewerColorSet(LColor lColor)
    {
        qViewerPreview = qViewerPreview.LColorChange(lColor);
        QViewerPreviewApply();
    }

    public void QViewerMediaRaise(LCargo lCargo)
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
                LTraceLog.LTraceErrorRecord("Viewer media notice handler failed", lException);
            }
        }
    }

    public void QViewerPlaybackUpdate(bool? lPlaying, TimeSpan? lPosition)
    {
        LPlaybackState lState = qViewerPreview.LPlaybackState;
        bool lPlayingNow = lPlaying ?? lState.LPlaybackStatePlaying;
        qViewerPreview = qViewerPreview.LPlaybackStateChange(
            new LPlaybackState(lPlayingNow, lPosition ?? lState.LPlaybackPosition));
        if (lPlayingNow != lState.LPlaybackStatePlaying)
        {
            QViewerPlayingChanged?.Invoke(lPlayingNow);
        }
    }

    public void QViewerMediaCommit(LCargo lCargo, bool lCropPersistent)
    {
        qViewerMediaInfo = lCargo.LCargoMediaInfo;
        QViewerWorkspace.CWorkspaceMediaSet(lCargo.LCargoSourcePath, lCargo.LCargoMediaInfo is not null);
        qViewerPreview = qViewerPreview.LPlaybackStateChange(LPlaybackState.LPlaybackStoppedCreate());
        qViewerEndReached = false;
        if (!lCropPersistent)
        {
            qViewerPreview = qViewerPreview.LCropboxChange(null);
        }
    }

    public void QViewerMediaClose()
    {
        qViewerLoadSerial++;
        qViewerMediaInfo = null;
        QViewerWorkspace.CWorkspaceMediaClear();
        qViewerPreview = qViewerPreview
            .LCropboxChange(null)
            .LPlaybackStateChange(LPlaybackState.LPlaybackStoppedCreate());
    }

    public TimeSpan QViewerTimeRead() => QPlayer.QPlayerReady ? QPlayer.QPlayerTimeRead() : QViewerPosition;

    public async void QViewerFrameRead(Action<LMediaFrame?> lFrameApply)
    {
        if (QViewerMediaInfo is not { LMediaVideoPresent: true } lMediaInfo)
        {
            lFrameApply(null);
            return;
        }

        int lWidth = lMediaInfo.LMediaVideoWidth;
        int lHeight = lMediaInfo.LMediaVideoHeight;
        string? lPath = QViewerWorkspace.CWorkspaceSource;
        if (lWidth <= 0 || lHeight <= 0 || string.IsNullOrWhiteSpace(lPath))
        {
            lFrameApply(null);
            return;
        }

        TimeSpan lTime = QViewerTimeRead();
        int lClaim = qViewerLoadSerial;
        LMediaFrame? lFrame = await LMedia.LMediaFrameStart(lPath, lTime, lWidth, lHeight);
        if (qViewerUnloaded || lClaim != qViewerLoadSerial)
        {
            return;
        }

        lFrameApply(lFrame);
    }
}
