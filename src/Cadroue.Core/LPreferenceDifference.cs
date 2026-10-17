using System;
using System.Collections.Generic;

namespace Cadroue.Core;

public static class LPreferenceDifference
{
    public static IEnumerable<string> LPreferenceDifferenceRead(
        LPreferenceState lPreferenceNow,
        LPreferenceState lPreferenceOther)
    {
        (string, object, object)[] lPreferenceFields =
        {
            ("Startup", lPreferenceOther.LPreferenceStartupMode, lPreferenceNow.LPreferenceStartupMode),
            ("Default tabs",
                string.Join(", ", lPreferenceOther.LPreferenceStartupTabs),
                string.Join(", ", lPreferenceNow.LPreferenceStartupTabs)),
            ("Auto-open last media",
                lPreferenceOther.LPreferenceMediaAutomatic,
                lPreferenceNow.LPreferenceMediaAutomatic),
            ("Confirm destructive actions",
                lPreferenceOther.LPreferenceConfirmDestructive,
                lPreferenceNow.LPreferenceConfirmDestructive),
            ("Clear source files on relay",
                lPreferenceOther.LPreferenceRelayEmpty,
                lPreferenceNow.LPreferenceRelayEmpty),
            ("Language", lPreferenceOther.LPreferenceLanguage, lPreferenceNow.LPreferenceLanguage),
            ("Verbose logging", lPreferenceOther.LPreferenceLogVerbose, lPreferenceNow.LPreferenceLogVerbose),
            ("File record location",
                lPreferenceOther.LPreferenceRecordWorkspace,
                lPreferenceNow.LPreferenceRecordWorkspace),
            ("Vertical tabs", lPreferenceOther.LPreferenceVerticalTabs, lPreferenceNow.LPreferenceVerticalTabs),
            ("Developer mode",
                lPreferenceOther.LPreferenceDeveloperActive,
                lPreferenceNow.LPreferenceDeveloperActive),
            ("Preset groups",
                LPreferenceFoldFormat(lPreferenceOther.LPreferenceFold),
                LPreferenceFoldFormat(lPreferenceNow.LPreferenceFold)),
            ("Volume mode", lPreferenceOther.LPreferenceVolumeMode, lPreferenceNow.LPreferenceVolumeMode),
            ("Default volume", lPreferenceOther.LPreferenceVolume, lPreferenceNow.LPreferenceVolume),
            ("Autoplay on load", lPreferenceOther.LPreferenceAutoplay, lPreferenceNow.LPreferenceAutoplay),
            ("Mousewheel", lPreferenceOther.LPreferenceWheelAction, lPreferenceNow.LPreferenceWheelAction),
            ("Pause while dragging", lPreferenceOther.LPreferenceDragPaused, lPreferenceNow.LPreferenceDragPaused),
            ("Preview engine",
                lPreferenceOther.LPreferencePreviewEngine,
                lPreferenceNow.LPreferencePreviewEngine),
            ("Timeline order",
                lPreferenceOther.LPreferenceTimelineOrder,
                lPreferenceNow.LPreferenceTimelineOrder),
            ("Keyframe minimum spacing",
                lPreferenceOther.LPreferenceKeyframePixels,
                lPreferenceNow.LPreferenceKeyframePixels),
            ("Keyframe scan delay",
                lPreferenceOther.LPreferenceKeyframeDelay,
                lPreferenceNow.LPreferenceKeyframeDelay),
            ("Section colour palette",
                lPreferenceOther.LPreferenceSectionPalette,
                lPreferenceNow.LPreferenceSectionPalette),
            ("Allow overlapping sections",
                lPreferenceOther.LPreferenceOverlapAllowed,
                lPreferenceNow.LPreferenceOverlapAllowed),
            ("Show waveforms", lPreferenceOther.LPreferenceWaveform, lPreferenceNow.LPreferenceWaveform),
            ("Pause queue on failure",
                lPreferenceOther.LPreferenceFailurePaused,
                lPreferenceNow.LPreferenceFailurePaused),
            ("Retry", lPreferenceOther.LPreferenceRetryAllowed, lPreferenceNow.LPreferenceRetryAllowed),
            ("Retry limit", lPreferenceOther.LPreferenceRetryMaximum, lPreferenceNow.LPreferenceRetryMaximum),
            ("Encoding priority", lPreferenceOther.LPreferenceEncodeLevel, lPreferenceNow.LPreferenceEncodeLevel),
            ("Keyframe priority",
                lPreferenceOther.LPreferenceKeyframeLevel,
                lPreferenceNow.LPreferenceKeyframeLevel),
            ("Waveform priority",
                lPreferenceOther.LPreferenceWaveformLevel,
                lPreferenceNow.LPreferenceWaveformLevel),
            ("Workspace folder",
                lPreferenceOther.LPreferenceWorkspaceFolder,
                lPreferenceNow.LPreferenceWorkspaceFolder),
            ("FFmpeg folder", lPreferenceOther.LPreferenceFfmpegFolder, lPreferenceNow.LPreferenceFfmpegFolder),
            ("Resume queue at launch", lPreferenceOther.LPreferenceAutoActive, lPreferenceNow.LPreferenceAutoActive),
            ("Show other worklists",
                lPreferenceOther.LPreferenceWorklistShared,
                lPreferenceNow.LPreferenceWorklistShared),
            ("Collapse completed batch",
                lPreferenceOther.LPreferenceCollapseDone,
                lPreferenceNow.LPreferenceCollapseDone),
            ("Delete old records",
                lPreferenceOther.LPreferenceCleanupActive,
                lPreferenceNow.LPreferenceCleanupActive),
            ("Record retention days",
                lPreferenceOther.LPreferenceCleanupDays,
                lPreferenceNow.LPreferenceCleanupDays),
            ("Last media", lPreferenceOther.LPreferenceMediaPath, lPreferenceNow.LPreferenceMediaPath)
        };

        foreach ((string lName, object lWas, object lNow) in lPreferenceFields)
        {
            if (!Equals(lWas, lNow))
            {
                yield return $"{lName}: {lWas} -> {lNow}";
            }
        }
    }

    private static string LPreferenceFoldFormat(Dictionary<string, bool>? lPreferenceGroups) =>
        lPreferenceGroups is null
            ? string.Empty
            : string.Join(", ", lPreferenceGroups
                .OrderBy(lGroup => lGroup.Key, StringComparer.OrdinalIgnoreCase)
                .Select(lGroup => $"{lGroup.Key}={(lGroup.Value ? "folded" : "expanded")}"));
}
