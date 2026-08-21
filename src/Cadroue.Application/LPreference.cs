using System;

using Cadroue.Core;

namespace Cadroue.Application;

public static class LPreference
{
    public static LPreferenceState LPreferenceStateCurrent { get; private set; } = LPreferenceState.LPreferenceDefaultCreate();

    public static Action? LPreferenceDepotCallback { get; set; }

    public static Action? LPreferenceDebounceSeam { get; set; }

    public static Func<string?, string>? LPreferenceLanguageSeam { get; set; }

    public static Func<LPreferenceState>? LPreferenceLoadSeam { get; set; }

    public static Func<LPreferenceState, bool>? LPreferenceSaveSeam { get; set; }

    public static Action<string>? LPreferenceTraceSeam { get; set; }

    private static LPreferenceState? lPreferenceBaseline;

    public static void LPreferenceLoad()
    {
        LPreferenceStateCurrent = LPreferenceLoadSeam?.Invoke() ?? LPreferenceState.LPreferenceDefaultCreate();
        LPreferenceStateCurrent.LPreferenceLanguage =
            LPreferenceLanguageNormalize(LPreferenceStateCurrent.LPreferenceLanguage);
    }

    public static bool LPreferenceStateSet(LPreferenceState lPreferenceState)
    {
        lPreferenceState.LPreferenceNormalize();
        lPreferenceState.LPreferenceLanguage = LPreferenceLanguageNormalize(lPreferenceState.LPreferenceLanguage);
        foreach (string lPreferenceChange in lPreferenceState.LPreferenceDifferenceRead(LPreferenceStateCurrent))
        {
            LPreferenceTraceSeam?.Invoke($"Preference changed — {lPreferenceChange}");
        }

        LPreferenceStateCurrent = lPreferenceState;
        bool lPreferenceSaved = LPreferenceSaveSeam?.Invoke(LPreferenceStateCurrent) ?? false;
        LPreferenceDepotCallback?.Invoke();
        return lPreferenceSaved;
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

    public static void LPreferenceDeveloperSet(bool lPreferenceDeveloperActive)
    {
        if (lPreferenceDeveloperActive == LPreferenceStateCurrent.LPreferenceDeveloperActive)
        {
            return;
        }

        LPreferenceState lPreferenceNext = LPreferenceStateCurrent.LPreferenceClone();
        lPreferenceNext.LPreferenceDeveloperActive = lPreferenceDeveloperActive;
        LPreferenceStateSet(lPreferenceNext);
    }

    public static void LPreferenceSaveCommit()
    {
        LPreferenceSaveSeam?.Invoke(LPreferenceStateCurrent);
        if (lPreferenceBaseline is { } lPreferenceWas)
        {
            foreach (string lPreferenceChange in LPreferenceStateCurrent.LPreferenceDifferenceRead(lPreferenceWas))
            {
                LPreferenceTraceSeam?.Invoke($"Preference saved — {lPreferenceChange}");
            }

            lPreferenceBaseline = null;
        }
    }

    private static string LPreferenceLanguageNormalize(string? lPreferenceLanguage) =>
        LPreferenceLanguageSeam?.Invoke(lPreferenceLanguage) ?? (lPreferenceLanguage ?? string.Empty);

    private static void LPreferenceDefer() => LPreferenceDebounceSeam?.Invoke();
}
