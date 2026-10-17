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
    public string LPreferenceEncodeLevel { get; set; } = "Low";
    public string LPreferenceKeyframeLevel { get; set; } = "Normal";
    public string LPreferenceWaveformLevel { get; set; } = "Low";
    public bool LPreferenceAutoActive { get; set; }
    public bool LPreferenceWorklistShared { get; set; }
    public bool LPreferenceCollapseDone { get; set; }

    public bool LPreferenceCleanupActive { get; set; }
    public int LPreferenceCleanupDays { get; set; } = 30;

    public string LPreferenceWorkspaceFolder { get; set; } = string.Empty;
    public string LPreferenceFfmpegFolder { get; set; } = string.Empty;
    public string LPreferenceMediaPath { get; set; } = string.Empty;

    public LPreferenceState LPreferenceVolumeChange(double lPreferenceVolume)
    {
        LPreferenceState lPreferenceState = LPreferenceClone();
        lPreferenceState.LPreferenceVolume = LPreferenceRange.LPreferenceVolumeClamp(lPreferenceVolume);
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
            LPreferenceEncodeLevel = LPreferenceEncodeLevel,
            LPreferenceKeyframeLevel = LPreferenceKeyframeLevel,
            LPreferenceWaveformLevel = LPreferenceWaveformLevel,
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

    public int LPreferenceLevelRead(LCustodyFamily lPreferenceFamily) => lPreferenceFamily switch
    {
        LCustodyFamily.LCustodyFamilyEncode => LCustody.LCustodyLevelResolve(LPreferenceEncodeLevel, 0),
        LCustodyFamily.LCustodyFamilyKeyframe => LCustody.LCustodyLevelResolve(LPreferenceKeyframeLevel, 1),
        LCustodyFamily.LCustodyFamilyWaveform => LCustody.LCustodyLevelResolve(LPreferenceWaveformLevel, 0),
        _ => 0
    };

    public void LPreferenceLevelSet(LCustodyFamily lPreferenceFamily, int lPreferenceLevel)
    {
        string lPreferenceToken = LCustody.LCustodyLevelFormat(lPreferenceLevel);
        if (lPreferenceFamily == LCustodyFamily.LCustodyFamilyEncode) LPreferenceEncodeLevel = lPreferenceToken;
        if (lPreferenceFamily == LCustodyFamily.LCustodyFamilyKeyframe) LPreferenceKeyframeLevel = lPreferenceToken;
        if (lPreferenceFamily == LCustodyFamily.LCustodyFamilyWaveform) LPreferenceWaveformLevel = lPreferenceToken;
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
}
