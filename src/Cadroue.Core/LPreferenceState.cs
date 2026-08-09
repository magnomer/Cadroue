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

    public double LPreferenceVolume { get; set; }
    public string LPreferenceVolumeMode { get; set; } = "Unified";
    public bool LPreferenceAutoplay { get; set; }
    public string LPreferenceWheelAction { get; set; } = "Seek";
    public bool LPreferenceDragPaused { get; set; } = true;

    public string LPreferenceTimelineOrder { get; set; } = "MapFirst";
    public double LPreferenceKeyframePixels { get; set; }
    public double LPreferenceKeyframeDelay { get; set; }
    public string LPreferenceSectionPalette { get; set; } = LPreferencePaletteDefault;
    public bool LPreferenceOverlapAllowed { get; set; } = true;
    public bool LPreferenceWaveform { get; set; } = true;

    public double LPreferenceParallelMaximum { get; set; }
    public bool LPreferenceFailurePaused { get; set; }
    public bool LPreferenceRetryAllowed { get; set; }
    public double LPreferenceRetryMaximum { get; set; }
    public bool LPreferenceAutoActive { get; set; }

    public bool LPreferenceRecordCleanupActive { get; set; }
    public int LPreferenceRecordCleanupDays { get; set; } = 30;

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
            LPreferenceVolume = 100,
            LPreferenceVolumeMode = "Unified",
            LPreferenceAutoplay = false,
            LPreferenceWheelAction = "Seek",
            LPreferenceDragPaused = true,
            LPreferenceTimelineOrder = "MapFirst",
            LPreferenceKeyframePixels = 5,
            LPreferenceKeyframeDelay = 1000,
            LPreferenceSectionPalette = LPreferencePaletteDefault,
            LPreferenceOverlapAllowed = true,
            LPreferenceWaveform = true,
            LPreferenceParallelMaximum = 1,
            LPreferenceFailurePaused = false,
            LPreferenceRetryAllowed = false,
            LPreferenceRetryMaximum = 3,
            LPreferenceAutoActive = false,
            LPreferenceRecordCleanupActive = false,
            LPreferenceRecordCleanupDays = 30,
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
            LPreferenceVolume = LPreferenceVolume,
            LPreferenceVolumeMode = LPreferenceVolumeMode,
            LPreferenceAutoplay = LPreferenceAutoplay,
            LPreferenceWheelAction = LPreferenceWheelAction,
            LPreferenceDragPaused = LPreferenceDragPaused,
            LPreferenceTimelineOrder = LPreferenceTimelineOrder,
            LPreferenceKeyframePixels = LPreferenceKeyframePixels,
            LPreferenceKeyframeDelay = LPreferenceKeyframeDelay,
            LPreferenceSectionPalette = LPreferenceSectionPalette,
            LPreferenceOverlapAllowed = LPreferenceOverlapAllowed,
            LPreferenceWaveform = LPreferenceWaveform,
            LPreferenceParallelMaximum = LPreferenceParallelMaximum,
            LPreferenceFailurePaused = LPreferenceFailurePaused,
            LPreferenceRetryAllowed = LPreferenceRetryAllowed,
            LPreferenceRetryMaximum = LPreferenceRetryMaximum,
            LPreferenceAutoActive = LPreferenceAutoActive,
            LPreferenceRecordCleanupActive = LPreferenceRecordCleanupActive,
            LPreferenceRecordCleanupDays = LPreferenceRecordCleanupDays,
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
            ("Volume mode", lPreferenceOther.LPreferenceVolumeMode, LPreferenceVolumeMode),
            ("Default volume", lPreferenceOther.LPreferenceVolume, LPreferenceVolume),
            ("Autoplay on load", lPreferenceOther.LPreferenceAutoplay, LPreferenceAutoplay),
            ("Mousewheel", lPreferenceOther.LPreferenceWheelAction, LPreferenceWheelAction),
            ("Pause while dragging", lPreferenceOther.LPreferenceDragPaused, LPreferenceDragPaused),
            ("Timeline order", lPreferenceOther.LPreferenceTimelineOrder, LPreferenceTimelineOrder),
            ("Keyframe minimum spacing", lPreferenceOther.LPreferenceKeyframePixels, LPreferenceKeyframePixels),
            ("Keyframe scan delay", lPreferenceOther.LPreferenceKeyframeDelay, LPreferenceKeyframeDelay),
            ("Section colour palette", lPreferenceOther.LPreferenceSectionPalette, LPreferenceSectionPalette),
            ("Allow overlapping sections", lPreferenceOther.LPreferenceOverlapAllowed, LPreferenceOverlapAllowed),
            ("Show waveforms", lPreferenceOther.LPreferenceWaveform, LPreferenceWaveform),
            ("Maximum parallel jobs", lPreferenceOther.LPreferenceParallelMaximum, LPreferenceParallelMaximum),
            ("Pause queue on failure", lPreferenceOther.LPreferenceFailurePaused, LPreferenceFailurePaused),
            ("Retry", lPreferenceOther.LPreferenceRetryAllowed, LPreferenceRetryAllowed),
            ("Retry limit", lPreferenceOther.LPreferenceRetryMaximum, LPreferenceRetryMaximum),
            ("Workspace folder", lPreferenceOther.LPreferenceWorkspaceFolder, LPreferenceWorkspaceFolder),
            ("FFmpeg folder", lPreferenceOther.LPreferenceFfmpegFolder, LPreferenceFfmpegFolder),
            ("Resume queue at launch", lPreferenceOther.LPreferenceAutoActive, LPreferenceAutoActive),
            ("Delete old records", lPreferenceOther.LPreferenceRecordCleanupActive, LPreferenceRecordCleanupActive),
            ("Record retention days", lPreferenceOther.LPreferenceRecordCleanupDays, LPreferenceRecordCleanupDays),
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
        if (LPreferenceStartupTabs is null || LPreferenceStartupTabs.Count == 0)
            LPreferenceStartupTabs = new List<string> { "Split" };
        if (LPreferenceVolumeMode is not "Unified" and not "PerTab") LPreferenceVolumeMode = "Unified";
        if (LPreferenceWheelAction is not "Seek" and not "Zoom" and not "Volume") LPreferenceWheelAction = "Seek";
        if (LPreferenceTimelineOrder is not "MapFirst" and not "ViewfinderFirst") LPreferenceTimelineOrder = "MapFirst";
        if (string.IsNullOrWhiteSpace(LPreferenceSectionPalette))
            LPreferenceSectionPalette = LPreferencePaletteDefault;

        LPreferenceVolume = LPreferenceVolumeClamp(LPreferenceVolume);
        LPreferenceKeyframePixels = LPreferenceNumberClamp(LPreferenceKeyframePixels, 1, 50, 5);
        LPreferenceKeyframeDelay = LPreferenceNumberClamp(LPreferenceKeyframeDelay, 0, 5000, 1000);
        LPreferenceParallelMaximum = Math.Round(LPreferenceNumberClamp(LPreferenceParallelMaximum, 1, 8, 1));
        LPreferenceRetryMaximum = Math.Round(LPreferenceNumberClamp(LPreferenceRetryMaximum, 0, 10, 3));
        LPreferenceRecordCleanupDays = (int)Math.Round(LPreferenceNumberClamp(LPreferenceRecordCleanupDays, 1, 365, 30));

        LPreferenceWorkspaceFolder = (LPreferenceWorkspaceFolder ?? string.Empty).Trim();
        LPreferenceFfmpegFolder = (LPreferenceFfmpegFolder ?? string.Empty).Trim();
        LPreferenceMediaPath = (LPreferenceMediaPath ?? string.Empty).Trim();
    }

    [JsonIgnore]
    public bool LPreferenceVolumeUnified => LPreferenceVolumeMode == "Unified";

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
