using System;

using Cadroue.Core;
using Cadroue.Infrastructure;

namespace Cadroue.MigrationInterface;

public static class LPreference
{
    public static LPreferenceState LPreferenceStateCurrent { get; private set; } = LPreferenceState.LPreferenceDefaultCreate();

    public static Action? LPreferenceDepotCallback { get; set; }

    public static Action? LPreferenceDebounceSeam { get; set; }

    public static Func<string?, string>? LPreferenceLanguageNormalizeSeam { get; set; }

    private static LPreferenceState? lPreferenceBaseline;

    public static void LPreferenceLoad()
    {
        LPreferenceStateCurrent = LPreferenceStateStore.LPreferenceStateLoad();
        LPreferenceStateCurrent.LPreferenceLanguage =
            LPreferenceLanguageNormalize(LPreferenceStateCurrent.LPreferenceLanguage);
    }

    public static void LPreferenceStateSet(LPreferenceState lPreferenceState)
    {
        lPreferenceState.LPreferenceNormalize();
        lPreferenceState.LPreferenceLanguage = LPreferenceLanguageNormalize(lPreferenceState.LPreferenceLanguage);
        foreach (string lPreferenceChange in lPreferenceState.LPreferenceDifferenceRead(LPreferenceStateCurrent))
        {
            LTraceLog.LTraceInfoRecord($"Preference changed — {lPreferenceChange}");
        }

        LPreferenceStateCurrent = lPreferenceState;
        LPreferenceStateStore.LPreferenceStateSave(LPreferenceStateCurrent);
        LPreferenceDepotCallback?.Invoke();
    }

    public static void LPreferenceVolumeSet(double lPreferenceVolume)
    {
        double lVolume = LPreferenceState.LPreferenceVolumeClamp(lPreferenceVolume);
        if (Math.Abs(lVolume - LPreferenceStateCurrent.LPreferenceVolume) < 0.0001)
        {
            return;
        }

        lPreferenceBaseline ??= LPreferenceStateCurrent.LPreferenceClone();
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

        lPreferenceBaseline ??= LPreferenceStateCurrent.LPreferenceClone();
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

        lPreferenceBaseline ??= LPreferenceStateCurrent.LPreferenceClone();
        LPreferenceState lPreferenceNext = LPreferenceStateCurrent.LPreferenceClone();
        lPreferenceNext.LPreferenceAutoActive = lPreferenceAutoResume;
        LPreferenceStateCurrent = lPreferenceNext;
        LPreferenceDefer();
    }

    public static void LPreferenceSaveCommit()
    {
        LPreferenceStateStore.LPreferenceStateSave(LPreferenceStateCurrent);
        if (lPreferenceBaseline is { } lPreferenceWas)
        {
            foreach (string lPreferenceChange in LPreferenceStateCurrent.LPreferenceDifferenceRead(lPreferenceWas))
            {
                LTraceLog.LTraceInfoRecord($"Preference saved — {lPreferenceChange}");
            }

            lPreferenceBaseline = null;
        }
    }

    private static string LPreferenceLanguageNormalize(string? lPreferenceLanguage) =>
        LPreferenceLanguageNormalizeSeam?.Invoke(lPreferenceLanguage) ?? (lPreferenceLanguage ?? string.Empty);

    private static void LPreferenceDefer() => LPreferenceDebounceSeam?.Invoke();
}
