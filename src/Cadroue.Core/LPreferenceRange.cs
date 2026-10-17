using System;
using System.Collections.Generic;

namespace Cadroue.Core;

public static class LPreferenceRange
{
    public static void LPreferenceNormalize(LPreferenceState lPreferenceState)
    {
        if (lPreferenceState.LPreferenceStartupMode is not "LastSession" and not "DefaultTab")
            lPreferenceState.LPreferenceStartupMode = "LastSession";
        if (lPreferenceState.LPreferenceStartupTabs is null)
            lPreferenceState.LPreferenceStartupTabs = new List<string> { "Split" };
        if (lPreferenceState.LPreferenceVolumeMode is not "Unified" and not "PerTab")
            lPreferenceState.LPreferenceVolumeMode = "Unified";
        if (lPreferenceState.LPreferenceWheelAction is not "Seek" and not "Zoom" and not "Volume")
            lPreferenceState.LPreferenceWheelAction = "Seek";
        if (lPreferenceState.LPreferencePreviewEngine is not "Flyleaf" and not "Mpv")
            lPreferenceState.LPreferencePreviewEngine = "Flyleaf";
        if (lPreferenceState.LPreferenceLoupeFloat is not "Off" and not "Owner" and not "Top")
            lPreferenceState.LPreferenceLoupeFloat = "Owner";
        if (lPreferenceState.LPreferenceTimelineOrder is not "MapFirst" and not "ViewfinderFirst")
            lPreferenceState.LPreferenceTimelineOrder = "MapFirst";
        if (string.IsNullOrWhiteSpace(lPreferenceState.LPreferenceSectionPalette))
            lPreferenceState.LPreferenceSectionPalette = LPreferenceState.LPreferencePaletteDefault;

        var lPreferencePresetGroups = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        if (lPreferenceState.LPreferenceFold is not null)
        {
            foreach ((string lGroupName, bool lGroupFolded) in lPreferenceState.LPreferenceFold)
            {
                string lGroup = (lGroupName ?? string.Empty).Trim();
                if (!string.IsNullOrEmpty(lGroup))
                {
                    lPreferencePresetGroups[lGroup] = lGroupFolded;
                }
            }
        }

        lPreferenceState.LPreferenceFold = lPreferencePresetGroups;

        lPreferenceState.LPreferenceVolume = LPreferenceVolumeClamp(lPreferenceState.LPreferenceVolume);
        lPreferenceState.LPreferenceKeyframePixels =
            LPreferenceNumberClamp(lPreferenceState.LPreferenceKeyframePixels, 1, 50, 5);
        lPreferenceState.LPreferenceKeyframeDelay =
            LPreferenceNumberClamp(lPreferenceState.LPreferenceKeyframeDelay, 0, 5000, 1000);
        lPreferenceState.LPreferenceRetryMaximum =
            Math.Round(LPreferenceNumberClamp(lPreferenceState.LPreferenceRetryMaximum, 0, 10, 3));
        foreach (LCustodyFamily lPreferenceFamily in Enum.GetValues<LCustodyFamily>())
        {
            lPreferenceState.LPreferenceLevelSet(
                lPreferenceFamily,
                lPreferenceState.LPreferenceLevelRead(lPreferenceFamily));
        }
        lPreferenceState.LPreferenceCleanupDays =
            (int)Math.Round(LPreferenceNumberClamp(lPreferenceState.LPreferenceCleanupDays, 1, 365, 30));

        lPreferenceState.LPreferenceWorkspaceFolder =
            (lPreferenceState.LPreferenceWorkspaceFolder ?? string.Empty).Trim();
        lPreferenceState.LPreferenceFfmpegFolder =
            (lPreferenceState.LPreferenceFfmpegFolder ?? string.Empty).Trim();
        lPreferenceState.LPreferenceMediaPath =
            (lPreferenceState.LPreferenceMediaPath ?? string.Empty).Trim();
    }

    public static double LPreferenceVolumeClamp(double lPreferenceVolume)
        => LPreferenceNumberClamp(lPreferenceVolume, 0, 100, 100);

    public static double LPreferenceNumberClamp(
        double lPreferenceValue,
        double lPreferenceMinimum,
        double lPreferenceMaximum,
        double lPreferenceFallback)
    {
        if (double.IsNaN(lPreferenceValue) || double.IsInfinity(lPreferenceValue))
        {
            return lPreferenceFallback;
        }

        return Math.Clamp(lPreferenceValue, lPreferenceMinimum, lPreferenceMaximum);
    }
}
