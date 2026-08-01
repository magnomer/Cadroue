using System;
using System.Windows.Threading;

using Cadroue.Core;
using Cadroue.Infrastructure;

namespace Cadroue.UIShell;

public static class LPreference
{
    public static LPreferenceState LPreferenceStateCurrent { get; private set; } = LPreferenceState.LPreferenceDefaultCreate();

    public static Action? LPreferenceWorkspaceCallback { get; set; }

    private static DispatcherTimer? lPreferenceSaveTimer;

    public static void LPreferenceLoad()
    {
        LPreferenceStateCurrent = LPreferenceStateStore.LPreferenceStateLoad();
        LPreferenceStateCurrent.LPreferenceLanguage =
            LLocalization.LLocalizationLanguageNormalize(LPreferenceStateCurrent.LPreferenceLanguage);
    }

    public static void LPreferenceStateSet(LPreferenceState lPreferenceState)
    {
        lPreferenceSaveTimer?.Stop();
        lPreferenceState.LPreferenceNormalize();
        lPreferenceState.LPreferenceLanguage = LLocalization.LLocalizationLanguageNormalize(lPreferenceState.LPreferenceLanguage);
        foreach (string lPreferenceChange in lPreferenceState.LPreferenceDifferenceRead(LPreferenceStateCurrent))
        {
            LTraceLog.LTraceInfoRecord($"Preference changed — {lPreferenceChange}");
        }

        LPreferenceStateCurrent = lPreferenceState;
        LPreferenceStateStore.LPreferenceStateSave(LPreferenceStateCurrent);
        LPreferenceWorkspaceCallback?.Invoke();
    }

    public static void LPreferenceVolumeSet(double lPreferenceVolume)
    {
        double lVolume = LPreferenceState.LPreferenceVolumeClamp(lPreferenceVolume);
        if (Math.Abs(lVolume - LPreferenceStateCurrent.LPreferenceVolume) < 0.0001)
        {
            return;
        }

        LPreferenceStateCurrent = LPreferenceStateCurrent.LPreferenceVolumeChange(lVolume);
        LPreferenceDefer();
    }

    public static void LPreferenceMediaSet(string? lPreferenceMediaPath)
    {
        string lMediaPath = (lPreferenceMediaPath ?? string.Empty).Trim();
        if (string.Equals(lMediaPath, LPreferenceStateCurrent.LPreferenceMediaPath, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        LPreferenceState lPreferenceNext = LPreferenceStateCurrent.LPreferenceClone();
        lPreferenceNext.LPreferenceMediaPath = lMediaPath;
        LPreferenceStateCurrent = lPreferenceNext;
        LPreferenceDefer();
    }

    public static void LPreferenceAutoSet(bool lPreferenceAutoResume)
    {
        if (lPreferenceAutoResume == LPreferenceStateCurrent.LPreferenceAutoActive)
        {
            return;
        }

        LPreferenceState lPreferenceNext = LPreferenceStateCurrent.LPreferenceClone();
        lPreferenceNext.LPreferenceAutoActive = lPreferenceAutoResume;
        LPreferenceStateCurrent = lPreferenceNext;
        LPreferenceDefer();
    }

    private static void LPreferenceDefer()
    {
        lPreferenceSaveTimer ??= LPreferenceTimerCreate();
        lPreferenceSaveTimer.Stop();
        lPreferenceSaveTimer.Start();
    }

    private static DispatcherTimer LPreferenceTimerCreate()
    {
        var lPreferenceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(700) };
        lPreferenceTimer.Tick += (_, _) =>
        {
            lPreferenceTimer.Stop();
            LPreferenceStateStore.LPreferenceStateSave(LPreferenceStateCurrent);
        };
        return lPreferenceTimer;
    }
}
