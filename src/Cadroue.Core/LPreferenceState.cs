using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Cadroue.Core;

public sealed class LPreferenceState
{
    public const string LPreferencePaletteDefault = "Cadroue";

    public string LPreferenceStartupMode { get; set; } = "LastSession";
    public List<string> LPreferenceStartupTabs { get; set; } = new() { "Split" };
    public bool LPreferenceMediaAutomatic { get; set; }
    public bool LPreferenceConfirmDestructive { get; set; }
    public bool LPreferenceRelayEmpty { get; set; } = true;
    public string LPreferenceLanguage { get; set; } = "en";
    public bool LPreferenceLogVerbose { get; set; }
    public bool LPreferenceRecordWorkspace { get; set; }
    public bool LPreferenceVerticalTabs { get; set; }
    public bool LPreferenceDeveloperActive { get; set; }
    public Dictionary<string, bool> LPreferenceFold { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public double LPreferenceVolume { get; set; }
    public string LPreferenceVolumeMode { get; set; } = "Unified";
    public bool LPreferenceAutoplay { get; set; }
    public string LPreferenceWheelAction { get; set; } = "Seek";
    public bool LPreferenceDragPaused { get; set; } = true;
    public string LPreferencePreviewEngine { get; set; } = "Flyleaf";
    public string LPreferenceLoupeFloat { get; set; } = "Owner";

    public string LPreferenceTimelineOrder { get; set; } = "MapFirst";
    public double LPreferenceKeyframePixels { get; set; }
    public double LPreferenceKeyframeDelay { get; set; }
    public string LPreferenceSectionPalette { get; set; } = LPreferencePaletteDefault;
    public bool LPreferenceOverlapAllowed { get; set; } = true;
    public bool LPreferenceWaveform { get; set; } = true;

    public bool LPreferenceFailurePaused { get; set; }
    public bool LPreferenceRetryAllowed { get; set; }
    public double LPreferenceRetryMaximum { get; set; }
    public bool LPreferenceAutoActive { get; set; }
    public bool LPreferenceWorklistShared { get; set; }
    public bool LPreferenceCollapseDone { get; set; }

    public bool LPreferenceCleanupActive { get; set; }
    public int LPreferenceCleanupDays { get; set; } = 30;

    public string LPreferenceWorkspaceFolder { get; set; } = string.Empty;
    public string LPreferenceFfmpegFolder { get; set; } = string.Empty;
    public string LPreferenceMediaPath { get; set; } = string.Empty;

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
            LPreferenceSectionPalette = LPreferencePaletteDefault,
            LPreferenceOverlapAllowed = true,
            LPreferenceWaveform = true,
            LPreferenceFailurePaused = false,
            LPreferenceRetryAllowed = false,
            LPreferenceRetryMaximum = 3,
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

    public LPreferenceState LPreferenceVolumeChange(double lPreferenceVolume)
    {
        LPreferenceState lPreferenceState = LPreferenceClone();
        lPreferenceState.LPreferenceVolume = LPreferenceVolumeClamp(lPreferenceVolume);
        return lPreferenceState;
    }

    public LPreferenceState LPreferenceClone()
    {
        return new LPreferenceState
        {
            LPreferenceStartupMode = LPreferenceStartupMode,
            LPreferenceStartupTabs = new List<string>(LPreferenceStartupTabs),
            LPreferenceMediaAutomatic = LPreferenceMediaAutomatic,
            LPreferenceConfirmDestructive = LPreferenceConfirmDestructive,
            LPreferenceRelayEmpty = LPreferenceRelayEmpty,
            LPreferenceLanguage = LPreferenceLanguage,
            LPreferenceLogVerbose = LPreferenceLogVerbose,
            LPreferenceRecordWorkspace = LPreferenceRecordWorkspace,
            LPreferenceVerticalTabs = LPreferenceVerticalTabs,
            LPreferenceDeveloperActive = LPreferenceDeveloperActive,
            LPreferenceFold = new Dictionary<string, bool>(
                LPreferenceFold ?? new Dictionary<string, bool>(),
                StringComparer.OrdinalIgnoreCase),
            LPreferenceVolume = LPreferenceVolume,
            LPreferenceVolumeMode = LPreferenceVolumeMode,
            LPreferenceAutoplay = LPreferenceAutoplay,
            LPreferenceWheelAction = LPreferenceWheelAction,
            LPreferenceDragPaused = LPreferenceDragPaused,
            LPreferencePreviewEngine = LPreferencePreviewEngine,
            LPreferenceLoupeFloat = LPreferenceLoupeFloat,
            LPreferenceTimelineOrder = LPreferenceTimelineOrder,
            LPreferenceKeyframePixels = LPreferenceKeyframePixels,
            LPreferenceKeyframeDelay = LPreferenceKeyframeDelay,
            LPreferenceSectionPalette = LPreferenceSectionPalette,
            LPreferenceOverlapAllowed = LPreferenceOverlapAllowed,
            LPreferenceWaveform = LPreferenceWaveform,
            LPreferenceFailurePaused = LPreferenceFailurePaused,
            LPreferenceRetryAllowed = LPreferenceRetryAllowed,
            LPreferenceRetryMaximum = LPreferenceRetryMaximum,
            LPreferenceAutoActive = LPreferenceAutoActive,
            LPreferenceWorklistShared = LPreferenceWorklistShared,
            LPreferenceCollapseDone = LPreferenceCollapseDone,
            LPreferenceCleanupActive = LPreferenceCleanupActive,
            LPreferenceCleanupDays = LPreferenceCleanupDays,
            LPreferenceWorkspaceFolder = LPreferenceWorkspaceFolder,
            LPreferenceFfmpegFolder = LPreferenceFfmpegFolder,
            LPreferenceMediaPath = LPreferenceMediaPath
        };
    }

    public IEnumerable<string> LPreferenceDifferenceRead(LPreferenceState lPreferenceOther)
    {
        (string Name, object Was, object Now)[] lPreferenceFields =
        {
            ("Startup", lPreferenceOther.LPreferenceStartupMode, LPreferenceStartupMode),
            ("Default tabs", string.Join(", ", lPreferenceOther.LPreferenceStartupTabs), string.Join(", ", LPreferenceStartupTabs)),
            ("Auto-open last media", lPreferenceOther.LPreferenceMediaAutomatic, LPreferenceMediaAutomatic),
            ("Confirm destructive actions", lPreferenceOther.LPreferenceConfirmDestructive, LPreferenceConfirmDestructive),
            ("Clear source files on relay", lPreferenceOther.LPreferenceRelayEmpty, LPreferenceRelayEmpty),
            ("Language", lPreferenceOther.LPreferenceLanguage, LPreferenceLanguage),
            ("Verbose logging", lPreferenceOther.LPreferenceLogVerbose, LPreferenceLogVerbose),
            ("File record location", lPreferenceOther.LPreferenceRecordWorkspace, LPreferenceRecordWorkspace),
            ("Vertical tabs", lPreferenceOther.LPreferenceVerticalTabs, LPreferenceVerticalTabs),
            ("Developer mode", lPreferenceOther.LPreferenceDeveloperActive, LPreferenceDeveloperActive),
            ("Preset groups", LPreferenceFoldFormat(lPreferenceOther.LPreferenceFold), LPreferenceFoldFormat(LPreferenceFold)),
            ("Volume mode", lPreferenceOther.LPreferenceVolumeMode, LPreferenceVolumeMode),
            ("Default volume", lPreferenceOther.LPreferenceVolume, LPreferenceVolume),
            ("Autoplay on load", lPreferenceOther.LPreferenceAutoplay, LPreferenceAutoplay),
            ("Mousewheel", lPreferenceOther.LPreferenceWheelAction, LPreferenceWheelAction),
            ("Pause while dragging", lPreferenceOther.LPreferenceDragPaused, LPreferenceDragPaused),
            ("Preview engine", lPreferenceOther.LPreferencePreviewEngine, LPreferencePreviewEngine),
            ("Timeline order", lPreferenceOther.LPreferenceTimelineOrder, LPreferenceTimelineOrder),
            ("Keyframe minimum spacing", lPreferenceOther.LPreferenceKeyframePixels, LPreferenceKeyframePixels),
            ("Keyframe scan delay", lPreferenceOther.LPreferenceKeyframeDelay, LPreferenceKeyframeDelay),
            ("Section colour palette", lPreferenceOther.LPreferenceSectionPalette, LPreferenceSectionPalette),
            ("Allow overlapping sections", lPreferenceOther.LPreferenceOverlapAllowed, LPreferenceOverlapAllowed),
            ("Show waveforms", lPreferenceOther.LPreferenceWaveform, LPreferenceWaveform),
            ("Pause queue on failure", lPreferenceOther.LPreferenceFailurePaused, LPreferenceFailurePaused),
            ("Retry", lPreferenceOther.LPreferenceRetryAllowed, LPreferenceRetryAllowed),
            ("Retry limit", lPreferenceOther.LPreferenceRetryMaximum, LPreferenceRetryMaximum),
            ("Workspace folder", lPreferenceOther.LPreferenceWorkspaceFolder, LPreferenceWorkspaceFolder),
            ("FFmpeg folder", lPreferenceOther.LPreferenceFfmpegFolder, LPreferenceFfmpegFolder),
            ("Resume queue at launch", lPreferenceOther.LPreferenceAutoActive, LPreferenceAutoActive),
            ("Show other worklists", lPreferenceOther.LPreferenceWorklistShared, LPreferenceWorklistShared),
            ("Collapse completed batch", lPreferenceOther.LPreferenceCollapseDone, LPreferenceCollapseDone),
            ("Delete old records", lPreferenceOther.LPreferenceCleanupActive, LPreferenceCleanupActive),
            ("Record retention days", lPreferenceOther.LPreferenceCleanupDays, LPreferenceCleanupDays),
            ("Last media", lPreferenceOther.LPreferenceMediaPath, LPreferenceMediaPath)
        };

        foreach ((string lName, object lWas, object lNow) in lPreferenceFields)
        {
            if (!Equals(lWas, lNow))
            {
                yield return $"{lName}: {lWas} -> {lNow}";
            }
        }
    }

    public void LPreferenceNormalize()
    {
        if (LPreferenceStartupMode is not "LastSession" and not "DefaultTab") LPreferenceStartupMode = "LastSession";
        if (LPreferenceStartupTabs is null)
            LPreferenceStartupTabs = new List<string> { "Split" };
        if (LPreferenceVolumeMode is not "Unified" and not "PerTab") LPreferenceVolumeMode = "Unified";
        if (LPreferenceWheelAction is not "Seek" and not "Zoom" and not "Volume") LPreferenceWheelAction = "Seek";
        if (LPreferencePreviewEngine is not "Flyleaf" and not "Mpv") LPreferencePreviewEngine = "Flyleaf";
        if (LPreferenceLoupeFloat is not "Off" and not "Owner" and not "Top") LPreferenceLoupeFloat = "Owner";
        if (LPreferenceTimelineOrder is not "MapFirst" and not "ViewfinderFirst") LPreferenceTimelineOrder = "MapFirst";
        if (string.IsNullOrWhiteSpace(LPreferenceSectionPalette))
            LPreferenceSectionPalette = LPreferencePaletteDefault;

        var lPreferencePresetGroups = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        if (LPreferenceFold is not null)
        {
            foreach ((string lGroupName, bool lGroupFolded) in LPreferenceFold)
            {
                string lGroup = (lGroupName ?? string.Empty).Trim();
                if (!string.IsNullOrEmpty(lGroup))
                {
                    lPreferencePresetGroups[lGroup] = lGroupFolded;
                }
            }
        }

        LPreferenceFold = lPreferencePresetGroups;

        LPreferenceVolume = LPreferenceVolumeClamp(LPreferenceVolume);
        LPreferenceKeyframePixels = LPreferenceNumberClamp(LPreferenceKeyframePixels, 1, 50, 5);
        LPreferenceKeyframeDelay = LPreferenceNumberClamp(LPreferenceKeyframeDelay, 0, 5000, 1000);
        LPreferenceRetryMaximum = Math.Round(LPreferenceNumberClamp(LPreferenceRetryMaximum, 0, 10, 3));
        LPreferenceCleanupDays = (int)Math.Round(LPreferenceNumberClamp(LPreferenceCleanupDays, 1, 365, 30));

        LPreferenceWorkspaceFolder = (LPreferenceWorkspaceFolder ?? string.Empty).Trim();
        LPreferenceFfmpegFolder = (LPreferenceFfmpegFolder ?? string.Empty).Trim();
        LPreferenceMediaPath = (LPreferenceMediaPath ?? string.Empty).Trim();
    }

    [JsonIgnore]
    public bool LPreferenceVolumeUnified => LPreferenceVolumeMode == "Unified";

    public bool LPreferenceFoldRead(string lPreferenceGroupName, bool lPreferenceFallback = true)
    {
        string lGroupName = (lPreferenceGroupName ?? string.Empty).Trim();
        return !string.IsNullOrEmpty(lGroupName)
            && LPreferenceFold is not null
            && LPreferenceFold.TryGetValue(lGroupName, out bool lGroupFolded)
                ? lGroupFolded
                : lPreferenceFallback;
    }

    private static string LPreferenceFoldFormat(Dictionary<string, bool>? lPreferenceGroups) =>
        lPreferenceGroups is null
            ? string.Empty
            : string.Join(", ", lPreferenceGroups
                .OrderBy(lGroup => lGroup.Key, StringComparer.OrdinalIgnoreCase)
                .Select(lGroup => $"{lGroup.Key}={(lGroup.Value ? "folded" : "expanded")}"));

    public static double LPreferenceVolumeClamp(double lPreferenceVolume)
        => LPreferenceNumberClamp(lPreferenceVolume, 0, 100, 100);

    public static double LPreferenceNumberClamp(double lPreferenceValue, double lPreferenceMinimum, double lPreferenceMaximum, double lPreferenceFallback)
    {
        if (double.IsNaN(lPreferenceValue) || double.IsInfinity(lPreferenceValue))
        {
            return lPreferenceFallback;
        }

        return Math.Clamp(lPreferenceValue, lPreferenceMinimum, lPreferenceMaximum);
    }
}
