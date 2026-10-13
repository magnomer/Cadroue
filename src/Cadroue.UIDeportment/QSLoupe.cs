using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Infrastructure;

namespace Cadroue.UIDeportment;

public enum QSLoupeFloat
{
    QSLoupeFloatOff,
    QSLoupeFloatOwner,
    QSLoupeFloatTop
}

public sealed record QSLoupeButton(
    QSLoupeFloat QSLoupeButtonKind,
    string QSLoupeButtonText,
    bool QSLoupeButtonSelected);

public sealed class QSLoupe
{
    private const string QSLoupeStartIcon = "/PAsset/PCompass/PCompassPlay.svg";
    private const string QSLoupePauseIcon = "/PAsset/PCompass/PCompassPause.svg";

    private static readonly IReadOnlyDictionary<string, QSLoupeFloat> qLoupeFloatKinds =
        new Dictionary<string, QSLoupeFloat>
        {
            ["Off"] = QSLoupeFloat.QSLoupeFloatOff,
            ["Owner"] = QSLoupeFloat.QSLoupeFloatOwner,
            ["Top"] = QSLoupeFloat.QSLoupeFloatTop,
        };

    private static readonly IReadOnlyDictionary<QSLoupeFloat, string> qLoupeFloatTokens =
        new Dictionary<QSLoupeFloat, string>
        {
            [QSLoupeFloat.QSLoupeFloatOff] = "Off",
            [QSLoupeFloat.QSLoupeFloatOwner] = "Owner",
            [QSLoupeFloat.QSLoupeFloatTop] = "Top",
        };

    private static readonly (QSLoupeFloat, string)[] qLoupeButtons =
    [
        (QSLoupeFloat.QSLoupeFloatOff, "Loupe.Float.Off"),
        (QSLoupeFloat.QSLoupeFloatOwner, "Loupe.Float.Owner"),
        (QSLoupeFloat.QSLoupeFloatTop, "Loupe.Float.Top"),
    ];

    private bool qLoupePlaying;
    private bool qLoupeEnded;
    private bool qLoupeClosed;
    private string? qLoupeSource;
    private QSLoupeFloat qLoupeFloat = QSLoupeFloat.QSLoupeFloatOwner;
    private QViewer? qLoupeViewer;
    private TimeSpan qLoupeFinal;
    private bool qLoupePlayingFinal;

    public QSLoupe(QViewer lViewer) => qLoupeViewer = lViewer;

    public QPlayer QPlayer { get; } = new();

    public event Action<bool>? QSLoupePlayingChanged;
    public event Action? QSLoupeFloatChanged;
    public event Action? QSLoupeEngineCreated;
    public event Action? QSLoupeCloseApplied;

    public bool QSLoupePlaying => qLoupePlaying;

    public bool QSLoupeEnded => qLoupeEnded;

    public bool QSLoupeClosed => qLoupeClosed;

    public string? QSLoupeSource => qLoupeSource;

    public QSLoupeFloat QSLoupeFloat => qLoupeFloat;

    public bool QSLoupeOwned => qLoupeFloat != QSLoupeFloat.QSLoupeFloatOff;

    public bool QSLoupeTopmost => qLoupeFloat == QSLoupeFloat.QSLoupeFloatTop;

    public bool QSLoupeMpvActive => LRenderer.LRendererEngineRead() == LPreviewEngine.LPreviewEngineMpv;

    public bool QSLoupeFlyleafActive => !QSLoupeMpvActive;

    public string QSLoupePlayIcon => qLoupePlaying ? QSLoupePauseIcon : QSLoupeStartIcon;

    public bool QSLoupePlayTinted => !qLoupePlaying;

    public string QSLoupePlayTip =>
        LLocalization.LLocalizationTextRead(qLoupePlaying ? "Loupe.Pause.Tooltip" : "Loupe.Play.Tooltip");

    public bool QSLoupeResumeCheck() => qLoupePlaying && !qLoupeEnded;

    public IReadOnlyList<QSLoupeButton> QSLoupeButtonsRead() =>
        qLoupeButtons.Select(QSLoupeButtonResolve).ToList();

    public void QSLoupeFloatRestore() =>
        qLoupeFloat = qLoupeFloatKinds.GetValueOrDefault(
            LPreference.LPreferenceStateCurrent.LPreferenceLoupeFloat, QSLoupeFloat.QSLoupeFloatOwner);

    public void QSLoupeFloatSet(QSLoupeFloat lFloat)
    {
        qLoupeFloat = lFloat;
        LPreferenceState lPreference = LPreference.LPreferenceStateCurrent.LPreferenceClone();
        lPreference.LPreferenceLoupeFloat = qLoupeFloatTokens[lFloat];
        LPreference.LPreferenceStateSet(lPreference);
        QSLoupeFloatChanged?.Invoke();
    }

    public void QSLoupeViewerAttach()
    {
        QViewer lViewer = qLoupeViewer!;
        lViewer.QViewerPreviewChanged += QSLoupePreviewHandle;
        lViewer.QViewerPlayback.QViewerLoupePlayed += QSLoupePlay;
        lViewer.QViewerPlayback.QViewerLoupePaused += QSLoupePause;
        lViewer.QViewerPlayback.QViewerLoupeSought += QSLoupeSeek;
        lViewer.QViewerPlayback.QViewerLoupeVolume += QSLoupeVolumeSet;
        lViewer.QViewerMedia.QViewerLoupeClosed += QSLoupeCloseHandle;
        lViewer.QViewerMedia.QViewerLoupe.QViewerLoupeAttach();
    }

    public async Task QSLoupeStart()
    {
        if (qLoupeViewer is not { } lViewer)
        {
            return;
        }

        qLoupeSource = lViewer.QViewerSourcePath;
        if (string.IsNullOrWhiteSpace(qLoupeSource))
        {
            return;
        }

        try
        {
            QSLoupeEngineCreated?.Invoke();
            await QPlayer.QPlayerOpenStart(qLoupeSource);
            if (qLoupeClosed)
            {
                return;
            }

            QSLoupePreviewApply(lViewer);
            QPlayer.QPlayerVolumeSet(lViewer.QViewerAudio.QViewerVolume);
            TimeSpan lInherit = lViewer.QViewerPosition;
            if (lInherit > TimeSpan.Zero)
            {
                QPlayer.QPlayerSeek(lInherit);
            }

            if (lViewer.QViewerPlaying)
            {
                QPlayer.QPlayerPlay();
                QSLoupePlayingSet(true);
                return;
            }

            QPlayer.QPlayerPause();
            QSLoupePlayingSet(false);
        }
        catch
        {
            QSLoupePlayingSet(false);
        }
    }

    public void QSLoupeClose()
    {
        qLoupeClosed = true;
        if (qLoupeViewer is { } lViewer)
        {
            lViewer.QViewerPreviewChanged -= QSLoupePreviewHandle;
            lViewer.QViewerPlayback.QViewerLoupePlayed -= QSLoupePlay;
            lViewer.QViewerPlayback.QViewerLoupePaused -= QSLoupePause;
            lViewer.QViewerPlayback.QViewerLoupeSought -= QSLoupeSeek;
            lViewer.QViewerPlayback.QViewerLoupeVolume -= QSLoupeVolumeSet;
            lViewer.QViewerMedia.QViewerLoupeClosed -= QSLoupeCloseHandle;
        }

        qLoupeFinal = QPlayer.QPlayerTimeRead();
        qLoupePlayingFinal = QSLoupeResumeCheck();
        QPlayer.QPlayerDispose();
    }

    public void QSLoupeDetach()
    {
        qLoupeViewer?.QViewerMedia.QViewerLoupe.QViewerLoupeDetach(qLoupeFinal, qLoupePlayingFinal);
        qLoupeViewer = null;
    }

    public void QSLoupePlayToggle()
    {
        if (qLoupeViewer is not { } lViewer)
        {
            return;
        }

        if (qLoupePlaying)
        {
            lViewer.QViewerPlayback.QViewerPause();
            return;
        }

        lViewer.QViewerPlayback.QViewerPlay();
    }

    public void QSLoupePlay()
    {
        if (qLoupeClosed || !QPlayer.QPlayerReady)
        {
            return;
        }

        if (qLoupeEnded)
        {
            QPlayer.QPlayerSeek(TimeSpan.Zero);
            qLoupeEnded = false;
        }

        QPlayer.QPlayerPlay();
        QSLoupePlayingSet(true);
    }

    public void QSLoupePause()
    {
        if (qLoupeClosed || !QPlayer.QPlayerReady)
        {
            return;
        }

        QPlayer.QPlayerPause();
        QSLoupePlayingSet(false);
    }

    public void QSLoupeSeek(TimeSpan lPosition)
    {
        if (qLoupeClosed || !QPlayer.QPlayerReady)
        {
            return;
        }

        qLoupeEnded = false;
        QPlayer.QPlayerSeek(lPosition);
    }

    public void QSLoupeVolumeSet(double lVolume)
    {
        if (qLoupeClosed || !QPlayer.QPlayerReady)
        {
            return;
        }

        QPlayer.QPlayerVolumeSet(lVolume);
    }

    public void QSLoupeTick()
    {
        if (qLoupeClosed || !qLoupePlaying)
        {
            return;
        }

        if (QPlayer.QPlayerEndedRead())
        {
            qLoupeEnded = true;
            QSLoupePlayingSet(false);
            return;
        }

        if (QPlayer.QPlayerReady)
        {
            qLoupeViewer?.QViewerPlayback.QViewerLoupeSync(QPlayer.QPlayerTimeRead(), true);
        }
    }

    public void QSLoupeEndSet(bool lEnded) => qLoupeEnded = lEnded;

    public void QSLoupePlayingSet(bool lPlaying)
    {
        qLoupePlaying = lPlaying;
        if (lPlaying)
        {
            qLoupeEnded = false;
        }

        QSLoupePlayingChanged?.Invoke(lPlaying);
        if (!qLoupeClosed)
        {
            qLoupeViewer?.QViewerPlayback.QViewerLoupeSync(QPlayer.QPlayerTimeRead(), lPlaying);
        }
    }

    private QSLoupeButton QSLoupeButtonResolve((QSLoupeFloat, string) lButton) => new(
        lButton.Item1,
        LLocalization.LLocalizationTextRead(lButton.Item2),
        lButton.Item1 == qLoupeFloat);

    private void QSLoupePreviewHandle()
    {
        if (qLoupeClosed || !QPlayer.QPlayerReady || qLoupeViewer is not { } lViewer)
        {
            return;
        }

        QSLoupePreviewApply(lViewer);
    }

    private void QSLoupePreviewApply(QViewer lViewer)
    {
        LPreviewState lState = lViewer.QViewerRenderRead();
        QPlayer.QPlayerFilterApply(LPreview.LPreviewFilterResolve(lState));
        QPlayer.QPlayerAudioApply(lViewer.QViewerAudio.QViewerAudioResolve());
        QPlayer.QPlayerPreviewApply(lState, "preview color/geometry");
    }

    private void QSLoupeCloseHandle() => QSLoupeCloseApplied?.Invoke();
}
