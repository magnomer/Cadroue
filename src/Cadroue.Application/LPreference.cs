using System;

using Cadroue.Core;

namespace Cadroue.Application;

public static class LPreference
{
    public static LPreferenceState LPreferenceStateCurrent { get; private set; } =
        LPreferenceState.LPreferenceDefaultCreate();

    public static Func<bool>? LPreferenceDepotCallback { get; set; }

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

        LPreferenceState lPreferencePrevious = LPreferenceStateCurrent;
        LPreferenceStateCurrent = lPreferenceState;
        if (LPreferenceDepotCallback?.Invoke() == false)
        {
            LPreferenceTraceSeam?.Invoke("Preference kept — the workspace could not be prepared");
            LPreferenceStateRestore(lPreferencePrevious);
            return false;
        }

        if (LPreferenceSaveSeam?.Invoke(LPreferenceStateCurrent) ?? true)
        {
            return true;
        }

        LPreferenceTraceSeam?.Invoke("Preference kept — the change could not be saved");
        LPreferenceStateRestore(lPreferencePrevious);
        return false;
    }

    private static void LPreferenceStateRestore(LPreferenceState lPreferencePrevious)
    {
        LPreferenceStateCurrent = lPreferencePrevious;
        if (LPreferenceDepotCallback?.Invoke() == false)
        {
            LPreferenceTraceSeam?.Invoke("Preference restore incomplete — the workspace stayed where it was moved");
        }
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

    public static void LPreferenceFoldSet(
        string lPreferenceGroupName,
        bool lPreferenceGroupFolded,
        bool lPreferenceFallback = true)
    {
        string lGroupName = (lPreferenceGroupName ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(lGroupName)
            || LPreferenceStateCurrent.LPreferenceFoldRead(lGroupName, lPreferenceFallback) == lPreferenceGroupFolded)
        {
            return;
        }

        LPreferenceState lPreferenceNext = LPreferenceStateCurrent.LPreferenceClone();
        lPreferenceNext.LPreferenceFold[lGroupName] = lPreferenceGroupFolded;
        LPreferenceStateSet(lPreferenceNext);
    }

    public static void LPreferenceSaveCommit()
    {
        if (LPreferenceSaveSeam?.Invoke(LPreferenceStateCurrent) == false)
        {
            LPreferenceTraceSeam?.Invoke("Preference kept — the deferred change could not be saved");
            return;
        }

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
