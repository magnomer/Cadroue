using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using Cadroue.UIShell.PFlow;
using Cadroue.UIShell.PPanels;

namespace Cadroue.UIShell;

public sealed class LPreferenceState
{
    public string LPreferenceStartupMode { get; set; } = "LastSession";
    public List<string> LPreferenceStartupTabs { get; set; } = new() { "Split" };
    public bool LPreferenceMediaAutomatic { get; set; }
    public bool LPreferenceConfirmDestructive { get; set; }
    public bool LPreferenceRelayEmpty { get; set; } = true;
    public string LPreferenceLanguage { get; set; } = "en";
    public bool LPreferenceLogVerbose { get; set; }
    public bool LPreferenceRecordWorkspace { get; set; }

    public double LPreferenceVolume { get; set; }
    public string LPreferenceVolumeMode { get; set; } = "Unified";
    public bool LPreferenceAutoplay { get; set; }
    public string LPreferenceWheelAction { get; set; } = "Seek";
    public bool LPreferenceDragPaused { get; set; } = true;

    public string LPreferenceTimelineOrder { get; set; } = "MapFirst";
    public double LPreferenceKeyframePixels { get; set; }
    public string LPreferenceSectionPalette { get; set; } = PSectionPalette.PSectionPaletteDefault;
    public bool LPreferenceOverlapAllowed { get; set; } = true;
    public bool LPreferenceWaveform { get; set; } = true;

    public double LPreferenceParallelMaximum { get; set; }
    public bool LPreferenceFailurePaused { get; set; }
    public bool LPreferenceRetryAllowed { get; set; }
    public double LPreferenceRetryMaximum { get; set; }
    public bool LPreferenceAutoActive { get; set; }

    public string LPreferenceWorkspaceFolder { get; set; } = string.Empty;
    public string LPreferenceFfmpegFolder { get; set; } = string.Empty;
    public string LPreferenceMediaPath { get; set; } = string.Empty;

    public double LPreferenceProgramWidth { get; set; }
    public double LPreferenceProgramHeight { get; set; }
    public double? LPreferenceProgramLeft { get; set; }
    public double? LPreferenceProgramTop { get; set; }
    public double LPreferenceFlowHeight { get; set; }

    public List<string> LPreferenceLayoutKeys { get; set; } = new();
    public int LPreferenceTabIndex { get; set; }

    public List<LPresetRecord> LPreferenceTabExports { get; set; } = new();
    public List<LPreferenceTabLayoutRecord> LPreferenceTabLayouts { get; set; } = new();
    public List<int> LPreferenceTabRelays { get; set; } = new();
    public List<string> LPreferenceTabNames { get; set; } = new();

    public List<LBindingRecord> LPreferenceShortcuts { get; set; } = new();

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
            LPreferenceVolume = 100,
            LPreferenceVolumeMode = "Unified",
            LPreferenceAutoplay = false,
            LPreferenceWheelAction = "Seek",
            LPreferenceDragPaused = true,
            LPreferenceTimelineOrder = "MapFirst",
            LPreferenceKeyframePixels = 5,
            LPreferenceSectionPalette = PSectionPalette.PSectionPaletteDefault,
            LPreferenceOverlapAllowed = true,
            LPreferenceWaveform = true,
            LPreferenceParallelMaximum = 1,
            LPreferenceFailurePaused = false,
            LPreferenceRetryAllowed = false,
            LPreferenceRetryMaximum = 3,
            LPreferenceAutoActive = false,
            LPreferenceWorkspaceFolder = string.Empty,
            LPreferenceFfmpegFolder = string.Empty,
            LPreferenceMediaPath = string.Empty,
            LPreferenceProgramWidth = 1280,
            LPreferenceProgramHeight = 760,
            LPreferenceProgramLeft = null,
            LPreferenceProgramTop = null,
            LPreferenceFlowHeight = 280,
            LPreferenceLayoutKeys = new List<string> { "Split", "Edit", "Audio", "Convert", "Merge", "Worklist" },
            LPreferenceTabIndex = 0,
            LPreferenceShortcuts = LBinding.LBindingDefaultCreate()
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
            LPreferenceVolume = LPreferenceVolume,
            LPreferenceVolumeMode = LPreferenceVolumeMode,
            LPreferenceAutoplay = LPreferenceAutoplay,
            LPreferenceWheelAction = LPreferenceWheelAction,
            LPreferenceDragPaused = LPreferenceDragPaused,
            LPreferenceTimelineOrder = LPreferenceTimelineOrder,
            LPreferenceKeyframePixels = LPreferenceKeyframePixels,
            LPreferenceSectionPalette = LPreferenceSectionPalette,
            LPreferenceOverlapAllowed = LPreferenceOverlapAllowed,
            LPreferenceWaveform = LPreferenceWaveform,
            LPreferenceParallelMaximum = LPreferenceParallelMaximum,
            LPreferenceFailurePaused = LPreferenceFailurePaused,
            LPreferenceRetryAllowed = LPreferenceRetryAllowed,
            LPreferenceRetryMaximum = LPreferenceRetryMaximum,
            LPreferenceAutoActive = LPreferenceAutoActive,
            LPreferenceWorkspaceFolder = LPreferenceWorkspaceFolder,
            LPreferenceFfmpegFolder = LPreferenceFfmpegFolder,
            LPreferenceMediaPath = LPreferenceMediaPath,
            LPreferenceProgramWidth = LPreferenceProgramWidth,
            LPreferenceProgramHeight = LPreferenceProgramHeight,
            LPreferenceProgramLeft = LPreferenceProgramLeft,
            LPreferenceProgramTop = LPreferenceProgramTop,
            LPreferenceFlowHeight = LPreferenceFlowHeight,
            LPreferenceLayoutKeys = new List<string>(LPreferenceLayoutKeys),
            LPreferenceTabIndex = LPreferenceTabIndex,
            LPreferenceTabExports = new List<LPresetRecord>(LPreferenceTabExports),
            LPreferenceTabLayouts = LPreferenceTabLayouts
                .Select(lPreferenceTabLayout => lPreferenceTabLayout.LPreferenceLayoutClone())
                .ToList(),
            LPreferenceTabRelays = new List<int>(LPreferenceTabRelays),
            LPreferenceTabNames = new List<string>(LPreferenceTabNames),
            LPreferenceShortcuts = LPreferenceShortcuts
                .Select(lPreferenceShortcut => lPreferenceShortcut.LBindingRecordClone())
                .ToList()
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
            ("Volume mode", lPreferenceOther.LPreferenceVolumeMode, LPreferenceVolumeMode),
            ("Default volume", lPreferenceOther.LPreferenceVolume, LPreferenceVolume),
            ("Autoplay on load", lPreferenceOther.LPreferenceAutoplay, LPreferenceAutoplay),
            ("Mousewheel", lPreferenceOther.LPreferenceWheelAction, LPreferenceWheelAction),
            ("Pause while dragging", lPreferenceOther.LPreferenceDragPaused, LPreferenceDragPaused),
            ("Timeline order", lPreferenceOther.LPreferenceTimelineOrder, LPreferenceTimelineOrder),
            ("Keyframe minimum spacing", lPreferenceOther.LPreferenceKeyframePixels, LPreferenceKeyframePixels),
            ("Section colour palette", lPreferenceOther.LPreferenceSectionPalette, LPreferenceSectionPalette),
            ("Allow overlapping sections", lPreferenceOther.LPreferenceOverlapAllowed, LPreferenceOverlapAllowed),
            ("Show waveforms", lPreferenceOther.LPreferenceWaveform, LPreferenceWaveform),
            ("Maximum parallel jobs", lPreferenceOther.LPreferenceParallelMaximum, LPreferenceParallelMaximum),
            ("Pause queue on failure", lPreferenceOther.LPreferenceFailurePaused, LPreferenceFailurePaused),
            ("Retry", lPreferenceOther.LPreferenceRetryAllowed, LPreferenceRetryAllowed),
            ("Retry limit", lPreferenceOther.LPreferenceRetryMaximum, LPreferenceRetryMaximum),
            ("Workspace folder", lPreferenceOther.LPreferenceWorkspaceFolder, LPreferenceWorkspaceFolder),
            ("FFmpeg folder", lPreferenceOther.LPreferenceFfmpegFolder, LPreferenceFfmpegFolder)
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
        LPreferenceLanguage = LLocalization.LLocalizationLanguageNormalize(LPreferenceLanguage);
        if (LPreferenceVolumeMode is not "Unified" and not "PerTab") LPreferenceVolumeMode = "Unified";
        if (LPreferenceWheelAction is not "Seek" and not "Zoom" and not "Volume") LPreferenceWheelAction = "Seek";
        if (LPreferenceTimelineOrder is not "MapFirst" and not "ViewfinderFirst") LPreferenceTimelineOrder = "MapFirst";
        if (string.IsNullOrWhiteSpace(LPreferenceSectionPalette))
            LPreferenceSectionPalette = PSectionPalette.PSectionPaletteDefault;

        LPreferenceVolume = LPreferenceVolumeClamp(LPreferenceVolume);
        LPreferenceKeyframePixels = LPreferenceNumberClamp(LPreferenceKeyframePixels, 1, 50, 5);
        LPreferenceParallelMaximum = Math.Round(LPreferenceNumberClamp(LPreferenceParallelMaximum, 1, 8, 1));
        LPreferenceRetryMaximum = Math.Round(LPreferenceNumberClamp(LPreferenceRetryMaximum, 0, 10, 3));

        LPreferenceProgramWidth = LPreferenceNumberClamp(LPreferenceProgramWidth, 800, 4000, 1280);
        LPreferenceProgramHeight = LPreferenceNumberClamp(LPreferenceProgramHeight, 400, 3000, 760);
        LPreferenceFlowHeight = LPreferenceNumberClamp(LPreferenceFlowHeight, 200, 520, 280);
        LPreferenceWorkspaceFolder = (LPreferenceWorkspaceFolder ?? string.Empty).Trim();
        LPreferenceFfmpegFolder = (LPreferenceFfmpegFolder ?? string.Empty).Trim();
        LPreferenceMediaPath = (LPreferenceMediaPath ?? string.Empty).Trim();
        LPreferenceTabIndex = Math.Max(0, LPreferenceTabIndex);
        if (LPreferenceLayoutKeys is null || LPreferenceLayoutKeys.Count == 0)
            LPreferenceLayoutKeys = new List<string> { "Split" };
        LPreferenceTabLayouts ??= new List<LPreferenceTabLayoutRecord>();
        LPreferenceTabRelays ??= new List<int>();
        LPreferenceTabNames ??= new List<string>();
        LPreferenceShortcuts = LBinding.LBindingNormalize(LPreferenceShortcuts);
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

public sealed class LPreferenceTabLayoutRecord
{
    public List<double> LPreferencePanelWidths { get; set; } = new();

    public bool LPreferenceExportHidden { get; set; }

    public List<int> LPreferencePanelsCollapsed { get; set; } = new();

    public List<LPreferenceFunnelRuleRecord> LPreferenceFunnelRules { get; set; } = new();

    public LPreferenceInspectorPersistentRecord? LPreferenceInspectorPersistent { get; set; }

    public LPreferenceTabLayoutRecord LPreferenceLayoutClone()
    {
        return new LPreferenceTabLayoutRecord
        {
            LPreferencePanelWidths = new List<double>(LPreferencePanelWidths),
            LPreferenceExportHidden = LPreferenceExportHidden,
            LPreferencePanelsCollapsed = new List<int>(LPreferencePanelsCollapsed),
            LPreferenceFunnelRules = LPreferenceFunnelRules.Select(pRule => pRule.LPreferenceFunnelRuleClone()).ToList(),
            LPreferenceInspectorPersistent = LPreferenceInspectorPersistent?.LPreferenceInspectorClone()
        };
    }
}

public sealed class LPreferenceInspectorPersistentRecord
{
    public Cadroue.Media.LSidecarAudioRecord? LPreferenceAudioPersistent { get; set; }

    public Cadroue.Media.LSidecarEditRecord? LPreferenceEditPersistent { get; set; }

    public bool LPreferenceCropPersistent { get; set; }

    public bool LPreferenceSkipPersistent { get; set; }

    public LPreferenceInspectorPersistentRecord LPreferenceInspectorClone()
    {
        return new LPreferenceInspectorPersistentRecord
        {
            LPreferenceAudioPersistent = LPreferenceAudioPersistent,
            LPreferenceEditPersistent = LPreferenceEditPersistent,
            LPreferenceCropPersistent = LPreferenceCropPersistent,
            LPreferenceSkipPersistent = LPreferenceSkipPersistent
        };
    }
}

public sealed class LPreferenceFunnelRuleRecord
{
    public string LPreferenceFunnelStartsWith { get; set; } = string.Empty;

    public string LPreferenceFunnelEndsWith { get; set; } = string.Empty;

    public bool LPreferenceFunnelAndMode { get; set; }

    public int LPreferenceFunnelTargetIndex { get; set; } = -1;

    public LPreferenceFunnelRuleRecord LPreferenceFunnelRuleClone()
    {
        return new LPreferenceFunnelRuleRecord
        {
            LPreferenceFunnelStartsWith = LPreferenceFunnelStartsWith,
            LPreferenceFunnelEndsWith = LPreferenceFunnelEndsWith,
            LPreferenceFunnelAndMode = LPreferenceFunnelAndMode,
            LPreferenceFunnelTargetIndex = LPreferenceFunnelTargetIndex
        };
    }
}
