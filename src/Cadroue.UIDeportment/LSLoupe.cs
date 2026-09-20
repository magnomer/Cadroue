using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Infrastructure;

namespace Cadroue.UIDeportment;

public enum LSLoupeFloat
{
    LSLoupeFloatOff,
    LSLoupeFloatOwner,
    LSLoupeFloatTop
}

public sealed record LSLoupeButton(
    LSLoupeFloat LSLoupeButtonKind,
    string LSLoupeButtonText,
    bool LSLoupeButtonSelected);

public sealed class LSLoupe
{
    private const string LSLoupeStartIcon = "/PAsset/PCompass/PCompassPlay.svg";
    private const string LSLoupePauseIcon = "/PAsset/PCompass/PCompassPause.svg";

    private static readonly IReadOnlyDictionary<string, LSLoupeFloat> lLoupeFloatKinds =
        new Dictionary<string, LSLoupeFloat>
        {
            ["Off"] = LSLoupeFloat.LSLoupeFloatOff,
            ["Owner"] = LSLoupeFloat.LSLoupeFloatOwner,
            ["Top"] = LSLoupeFloat.LSLoupeFloatTop,
        };

    private static readonly IReadOnlyDictionary<LSLoupeFloat, string> lLoupeFloatTokens =
        new Dictionary<LSLoupeFloat, string>
        {
            [LSLoupeFloat.LSLoupeFloatOff] = "Off",
            [LSLoupeFloat.LSLoupeFloatOwner] = "Owner",
            [LSLoupeFloat.LSLoupeFloatTop] = "Top",
        };

    private static readonly (LSLoupeFloat, string)[] lLoupeButtons =
    [
        (LSLoupeFloat.LSLoupeFloatOff, "Loupe.Float.Off"),
        (LSLoupeFloat.LSLoupeFloatOwner, "Loupe.Float.Owner"),
        (LSLoupeFloat.LSLoupeFloatTop, "Loupe.Float.Top"),
    ];

    private bool lLoupePlaying;
    private bool lLoupeEnded;
    private bool lLoupeClosed;
    private string? lLoupeSource;
    private LSLoupeFloat lLoupeFloat = LSLoupeFloat.LSLoupeFloatOwner;
    private LViewer? lLoupeViewer;
    private TimeSpan lLoupeFinal;
    private bool lLoupePlayingFinal;

    public LPlayer LPlayer { get; } = new();

    public event Action<bool>? LSLoupePlayingChange;
    public event Action? LSLoupeFloatChange;
    public event Action? LSLoupeEngineCreate;
    public event Action? LSLoupeCloseApply;

    public bool LSLoupePlaying => lLoupePlaying;

    public bool LSLoupeEnded => lLoupeEnded;

    public bool LSLoupeClosed => lLoupeClosed;

    public string? LSLoupeSource => lLoupeSource;

    public LSLoupeFloat LSLoupeFloat => lLoupeFloat;

    public bool LSLoupeOwned => lLoupeFloat != LSLoupeFloat.LSLoupeFloatOff;

    public bool LSLoupeTopmost => lLoupeFloat == LSLoupeFloat.LSLoupeFloatTop;

    public bool LSLoupeMpvActive => LRenderer.LRendererEngineRead() == LPreviewEngine.LPreviewEngineMpv;

    public bool LSLoupeFlyleafActive => !LSLoupeMpvActive;

    public string LSLoupePlayIcon => lLoupePlaying ? LSLoupePauseIcon : LSLoupeStartIcon;

    public bool LSLoupePlayTinted => !lLoupePlaying;

    public string LSLoupePlayTip =>
        LLocalization.LLocalizationTextRead(lLoupePlaying ? "Loupe.Pause.Tooltip" : "Loupe.Play.Tooltip");

    public bool LSLoupeResumeCheck() => lLoupePlaying && !lLoupeEnded;

    public IReadOnlyList<LSLoupeButton> LSLoupeButtonsRead() =>
        lLoupeButtons.Select(LSLoupeButtonResolve).ToList();

    public void LSLoupeFloatRestore() =>
        lLoupeFloat = lLoupeFloatKinds.GetValueOrDefault(
            LPreference.LPreferenceStateCurrent.LPreferenceLoupeFloat, LSLoupeFloat.LSLoupeFloatOwner);

    public void LSLoupeFloatSet(LSLoupeFloat lFloat)
    {
        lLoupeFloat = lFloat;
        LPreferenceState lPreference = LPreference.LPreferenceStateCurrent.LPreferenceClone();
        lPreference.LPreferenceLoupeFloat = lLoupeFloatTokens[lFloat];
        LPreference.LPreferenceStateSet(lPreference);
        LSLoupeFloatChange?.Invoke();
    }

    public void LSLoupeViewerAttach(LViewer lViewer)
    {
        lLoupeViewer = lViewer;
        lViewer.LViewerPreviewChange += LSLoupePreviewHandle;
        lViewer.LViewerPlayback.LViewerLoupePlay += LSLoupePlay;
        lViewer.LViewerPlayback.LViewerLoupePause += LSLoupePause;
        lViewer.LViewerPlayback.LViewerLoupeSeek += LSLoupeSeek;
        lViewer.LViewerPlayback.LViewerLoupeVolume += LSLoupeVolumeSet;
        lViewer.LViewerMedia.LViewerLoupeClose += LSLoupeCloseHandle;
        lViewer.LViewerMedia.LViewerLoupeAttach();
    }

    public async Task LSLoupeStart()
    {
        if (lLoupeViewer is not { } lViewer)
        {
            return;
        }

        lLoupeSource = lViewer.LViewerSourcePath;
        if (string.IsNullOrWhiteSpace(lLoupeSource))
        {
            return;
        }

        try
        {
            LSLoupeEngineCreate?.Invoke();
            await LPlayer.LPlayerOpenStart(lLoupeSource);
            if (lLoupeClosed)
            {
                return;
            }

            LSLoupePreviewApply(lViewer);
            LPlayer.LPlayerVolumeSet(lViewer.LViewerVolume);
            TimeSpan lInherit = lViewer.LViewerPosition;
            if (lInherit > TimeSpan.Zero)
            {
                LPlayer.LPlayerSeek(lInherit);
            }

            if (lViewer.LViewerPlaying)
            {
                LPlayer.LPlayerPlay();
                LSLoupePlayingSet(true);
                return;
            }

            LPlayer.LPlayerPause();
            LSLoupePlayingSet(false);
        }
        catch
        {
            LSLoupePlayingSet(false);
        }
    }

    public void LSLoupeClose()
    {
        lLoupeClosed = true;
        if (lLoupeViewer is { } lViewer)
        {
            lViewer.LViewerPreviewChange -= LSLoupePreviewHandle;
            lViewer.LViewerPlayback.LViewerLoupePlay -= LSLoupePlay;
            lViewer.LViewerPlayback.LViewerLoupePause -= LSLoupePause;
            lViewer.LViewerPlayback.LViewerLoupeSeek -= LSLoupeSeek;
            lViewer.LViewerPlayback.LViewerLoupeVolume -= LSLoupeVolumeSet;
            lViewer.LViewerMedia.LViewerLoupeClose -= LSLoupeCloseHandle;
        }

        lLoupeFinal = LPlayer.LPlayerTimeRead();
        lLoupePlayingFinal = LSLoupeResumeCheck();
        LPlayer.LPlayerDispose();
    }

    public void LSLoupeDetach()
    {
        lLoupeViewer?.LViewerMedia.LViewerLoupeDetach(lLoupeFinal, lLoupePlayingFinal);
        lLoupeViewer = null;
    }

    public void LSLoupePlayToggle()
    {
        if (lLoupeViewer is not { } lViewer)
        {
            return;
        }

        if (lLoupePlaying)
        {
            lViewer.LViewerPlayback.LViewerPause();
            return;
        }

        lViewer.LViewerPlayback.LViewerPlay();
    }

    public void LSLoupePlay()
    {
        if (lLoupeClosed || !LPlayer.LPlayerReady)
        {
            return;
        }

        if (lLoupeEnded)
        {
            LPlayer.LPlayerSeek(TimeSpan.Zero);
            lLoupeEnded = false;
        }

        LPlayer.LPlayerPlay();
        LSLoupePlayingSet(true);
    }

    public void LSLoupePause()
    {
        if (lLoupeClosed || !LPlayer.LPlayerReady)
        {
            return;
        }

        LPlayer.LPlayerPause();
        LSLoupePlayingSet(false);
    }

    public void LSLoupeSeek(TimeSpan lPosition)
    {
        if (lLoupeClosed || !LPlayer.LPlayerReady)
        {
            return;
        }

        lLoupeEnded = false;
        LPlayer.LPlayerSeek(lPosition);
    }

    public void LSLoupeVolumeSet(double lVolume)
    {
        if (lLoupeClosed || !LPlayer.LPlayerReady)
        {
            return;
        }

        LPlayer.LPlayerVolumeSet(lVolume);
    }

    public void LSLoupeTick()
    {
        if (lLoupeClosed || !lLoupePlaying)
        {
            return;
        }

        if (LPlayer.LPlayerEndedRead())
        {
            lLoupeEnded = true;
            LSLoupePlayingSet(false);
            return;
        }

        if (LPlayer.LPlayerReady)
        {
            lLoupeViewer?.LViewerPlayback.LViewerLoupeSync(LPlayer.LPlayerTimeRead(), true);
        }
    }

    public void LSLoupeEndSet(bool lEnded) => lLoupeEnded = lEnded;

    public void LSLoupePlayingSet(bool lPlaying)
    {
        lLoupePlaying = lPlaying;
        if (lPlaying)
        {
            lLoupeEnded = false;
        }

        LSLoupePlayingChange?.Invoke(lPlaying);
        if (!lLoupeClosed)
        {
            lLoupeViewer?.LViewerPlayback.LViewerLoupeSync(LPlayer.LPlayerTimeRead(), lPlaying);
        }
    }

    private LSLoupeButton LSLoupeButtonResolve((LSLoupeFloat, string) lButton) => new(
        lButton.Item1,
        LLocalization.LLocalizationTextRead(lButton.Item2),
        lButton.Item1 == lLoupeFloat);

    private void LSLoupePreviewHandle()
    {
        if (lLoupeClosed || !LPlayer.LPlayerReady || lLoupeViewer is not { } lViewer)
        {
            return;
        }

        LSLoupePreviewApply(lViewer);
    }

    private void LSLoupePreviewApply(LViewer lViewer)
    {
        LPreviewState lState = lViewer.LViewerRenderRead();
        LPlayer.LPlayerFilterApply(LPreview.LPreviewFilterResolve(lState));
        LPlayer.LPlayerAudioApply(lViewer.LViewerAudioResolve());
        LPlayer.LPlayerPreviewApply(lState, "preview color/geometry");
    }

    private void LSLoupeCloseHandle() => LSLoupeCloseApply?.Invoke();
}
