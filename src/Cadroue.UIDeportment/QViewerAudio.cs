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
        [true] = QLocalizationCatalog.QLocalizationTextRead("Viewer.Audio.SwitchTooltip"),
        [false] = QLocalizationCatalog.QLocalizationTextRead("Viewer.Audio.MpvRequired"),
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
        QLocalizationCatalog.QLocalizationTextRead(qViewerBypass ? "Viewer.Audio.Original" : "Viewer.Audio.Filtered"),
        qViewerSwitchTips[qViewerCapable],
        qViewerCapable);

    public void QViewerCapableSet(bool lCapable) => qViewerCapable = lCapable;

    public void QViewerFilterSet(string? lFilter) => qViewerAudioFilter = QViewerFilterNormalize(lFilter);

    private static string QViewerFilterNormalize(string? lFilter) => lFilter ?? string.Empty;

    public string QViewerAudioResolve() => qViewerBypass ? string.Empty : qViewerAudioFilter;

    public void QViewerGraphSet(string lGraph)
    {
        QViewerFilterSet(lGraph);
        QViewerAudioChanged?.Invoke();
    }

    public void QViewerBypassSet(bool lBypass)
    {
        if (!QViewerBypassChange(lBypass))
        {
            return;
        }

        QViewerBypassChanged?.Invoke(lBypass);
        QViewerAudioChanged?.Invoke();
    }

    // Stores the bypass; true when it differs from the one held.
    private bool QViewerBypassChange(bool lBypass)
    {
        if (qViewerBypass == lBypass)
        {
            return false;
        }

        qViewerBypass = lBypass;
        return true;
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
        QViewerVolumeChange(lVolume);
        if (LPreference.LPreferenceStateCurrent.LPreferenceVolumeUnified)
        {
            LPreference.LPreferenceVolumeSet(qViewerVolume);
        }

        QViewerVolumeChanged?.Invoke(qViewerVolume);
    }

    private void QViewerVolumeChange(double lVolume) =>
        qViewerVolume = LPreferenceState.LPreferenceVolumeClamp(lVolume);
}
