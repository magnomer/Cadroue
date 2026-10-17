using System;
using System.Collections.Generic;

namespace Cadroue.Core;

public static class LPreferenceDefault
{
    public static LPreferenceState LPreferenceDefaultCreate()
    {
        return new LPreferenceState
        {
            LPreferenceStartupMode = "LastSession",
            LPreferenceStartupTabs = new List<string> { "Split" },
            LPreferenceMediaAutomatic = false,
            LPreferenceConfirmDestructive = false,
            LPreferenceRelayEmpty = true,
            LPreferenceLanguage = "en",
            LPreferenceLogVerbose = false,
            LPreferenceRecordWorkspace = false,
            LPreferenceVerticalTabs = false,
            LPreferenceDeveloperActive = false,
            LPreferenceFold = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase),
            LPreferenceVolume = 100,
            LPreferenceVolumeMode = "Unified",
            LPreferenceAutoplay = false,
            LPreferenceWheelAction = "Seek",
            LPreferenceDragPaused = true,
            LPreferencePreviewEngine = "Flyleaf",
            LPreferenceLoupeFloat = "Owner",
            LPreferenceTimelineOrder = "MapFirst",
            LPreferenceKeyframePixels = 5,
            LPreferenceKeyframeDelay = 1000,
            LPreferenceSectionPalette = LPreferenceState.LPreferencePaletteDefault,
            LPreferenceOverlapAllowed = true,
            LPreferenceWaveform = true,
            LPreferenceFailurePaused = false,
            LPreferenceRetryAllowed = false,
            LPreferenceRetryMaximum = 3,
            LPreferenceEncodeLevel = "Low",
            LPreferenceKeyframeLevel = "Normal",
            LPreferenceWaveformLevel = "Low",
            LPreferenceAutoActive = false,
            LPreferenceWorklistShared = false,
            LPreferenceCollapseDone = false,
            LPreferenceCleanupActive = false,
            LPreferenceCleanupDays = 30,
            LPreferenceWorkspaceFolder = string.Empty,
            LPreferenceFfmpegFolder = string.Empty,
            LPreferenceMediaPath = string.Empty
        };
    }
}
