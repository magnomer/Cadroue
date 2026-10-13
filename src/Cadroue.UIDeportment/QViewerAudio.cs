using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.UIDeportment;

public sealed record QViewerSwitch(string QViewerSwitchText, string QViewerSwitchTip, bool QViewerSwitchEnabled);

public sealed class QViewerAudio
{
    private readonly QPlayer qViewerPlayer;
    private bool qViewerBypass;
    private bool qViewerCapable;
    private string qViewerAudioFilter = string.Empty;
    private double qViewerVolume = LPreference.LPreferenceStateCurrent.LPreferenceVolume;
    private readonly IReadOnlyDictionary<bool, string> qViewerSwitchTips = new Dictionary<bool, string>
    {
        [true] = LLocalization.LLocalizationTextRead("Viewer.Audio.SwitchTooltip"),
        [false] = LLocalization.LLocalizationTextRead("Viewer.Audio.MpvRequired"),
    };

    public QViewerAudio(QPlayer lPlayer)
    {
        qViewerPlayer = lPlayer;
    }

    public event Action? QViewerAudioChanged;
    public event Action<bool>? QViewerBypassChanged;
    public event Action<double>? QViewerVolumeChanged;

    public bool QViewerBypass => qViewerBypass;

    public double QViewerVolume => qViewerVolume;

    public QViewerSwitch QViewerSwitchRead() => new(
        LLocalization.LLocalizationTextRead(qViewerBypass ? "Viewer.Audio.Original" : "Viewer.Audio.Filtered"),
        qViewerSwitchTips[qViewerCapable],
        qViewerCapable);

    public void QViewerCapableSet(bool lCapable) => qViewerCapable = lCapable;

    public void QViewerFilterSet(string? lFilter) => qViewerAudioFilter = lFilter ?? string.Empty;

    public string QViewerAudioResolve() => qViewerBypass ? string.Empty : qViewerAudioFilter;

    public void QViewerGraphSet(string lGraph)
    {
        QViewerFilterSet(lGraph);
        QViewerAudioChanged?.Invoke();
    }

    public void QViewerBypassSet(bool lBypass)
    {
        if (qViewerBypass == lBypass)
        {
            return;
        }

        qViewerBypass = lBypass;
        QViewerBypassChanged?.Invoke(lBypass);
        QViewerAudioChanged?.Invoke();
    }

    public void QViewerBypassToggle() => QViewerBypassSet(!qViewerBypass);

    public void QViewerAudioApply(bool lMpvActive)
    {
        if (!lMpvActive || !qViewerPlayer.QPlayerReady)
        {
            return;
        }

        qViewerPlayer.QPlayerAudioApply(QViewerAudioResolve());
    }

    public void QViewerVolumeApply(double lVolume)
    {
        qViewerVolume = LPreferenceState.LPreferenceVolumeClamp(lVolume);
        if (LPreference.LPreferenceStateCurrent.LPreferenceVolumeUnified)
        {
            LPreference.LPreferenceVolumeSet(qViewerVolume);
        }

        QViewerVolumeChanged?.Invoke(qViewerVolume);
    }
}
