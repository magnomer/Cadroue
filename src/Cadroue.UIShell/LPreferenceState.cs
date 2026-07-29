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
    public string LPreferenceLanguage { get; set; } = "English";
    public bool LPreferenceLogVerbose { get; set; }
    public bool LPreferenceRecordWorkspace { get; set; }

    public double LPreferenceVolume { get; set; }
    public string LPreferenceVolumeMode { get; set; } = "Unified";
    public bool LPreferenceAutoplayOnLoad { get; set; }
    public string LPreferenceWheelAction { get; set; } = "Seek";
    public bool LPreferenceDragPaused { get; set; } = true;

    public string LPreferenceTimelineOrder { get; set; } = "MapFirst";
    public double LPreferenceKeyframeMinimumPixels { get; set; }
    public string LPreferenceSectionPalette { get; set; } = PSectionPalette.PSectionPaletteDefaultName;
    public bool LPreferenceOverlapAllowed { get; set; } = true;

    public double LPreferenceParallelMaximum { get; set; }
    public bool LPreferenceFailurePaused { get; set; }
    public bool LPreferenceRetryAllowed { get; set; }
    public double LPreferenceRetryMaximum { get; set; }
    public bool LPreferenceAutoResume { get; set; }

    public string LPreferenceWorkspaceFolder { get; set; } = string.Empty;
    public string LPreferenceFfmpegFolder { get; set; } = string.Empty;
    public string LPreferenceMediaPath { get; set; } = string.Empty;

    public double LPreferenceProgramWidth { get; set; }
    public double LPreferenceProgramHeight { get; set; }
    public double? LPreferenceProgramLeft { get; set; }
    public double? LPreferenceProgramTop { get; set; }
    public double LPreferenceFlowHeight { get; set; }

    public List<string> LPreferenceTabLayoutKeys { get; set; } = new();
    public int LPreferenceTabSelectIndex { get; set; }

    public List<LExportSpecificPresetRecord> LPreferenceTabExports { get; set; } = new();
    public List<LPreferenceTabLayoutRecord> LPreferenceTabLayouts { get; set; } = new();

    public static LPreferenceState LPreferenceDefaultCreate()
    {
        return new LPreferenceState
        {
            LPreferenceStartupMode = "LastSession",
            LPreferenceStartupTabs = new List<string> { "Split" },
            LPreferenceMediaAutomatic = false,
            LPreferenceConfirmDestructive = false,
            LPreferenceLanguage = "English",
            LPreferenceLogVerbose = false,
            LPreferenceRecordWorkspace = false,
            LPreferenceVolume = 100,
            LPreferenceVolumeMode = "Unified",
            LPreferenceAutoplayOnLoad = false,
            LPreferenceWheelAction = "Seek",
            LPreferenceDragPaused = true,
            LPreferenceTimelineOrder = "MapFirst",
            LPreferenceKeyframeMinimumPixels = 5,
            LPreferenceSectionPalette = PSectionPalette.PSectionPaletteDefaultName,
            LPreferenceOverlapAllowed = true,
            LPreferenceParallelMaximum = 1,
            LPreferenceFailurePaused = false,
            LPreferenceRetryAllowed = false,
            LPreferenceRetryMaximum = 3,
            LPreferenceAutoResume = false,
            LPreferenceWorkspaceFolder = string.Empty,
            LPreferenceFfmpegFolder = string.Empty,
            LPreferenceMediaPath = string.Empty,
            LPreferenceProgramWidth = 1280,
            LPreferenceProgramHeight = 760,
            LPreferenceProgramLeft = null,
            LPreferenceProgramTop = null,
            LPreferenceFlowHeight = 280,
            LPreferenceTabLayoutKeys = new List<string> { "Split", "Edit", "Audio", "Convert", "Merge", "Worklist" },
            LPreferenceTabSelectIndex = 0
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
            LPreferenceLanguage = LPreferenceLanguage,
            LPreferenceLogVerbose = LPreferenceLogVerbose,
            LPreferenceRecordWorkspace = LPreferenceRecordWorkspace,
            LPreferenceVolume = LPreferenceVolume,
            LPreferenceVolumeMode = LPreferenceVolumeMode,
            LPreferenceAutoplayOnLoad = LPreferenceAutoplayOnLoad,
            LPreferenceWheelAction = LPreferenceWheelAction,
            LPreferenceDragPaused = LPreferenceDragPaused,
            LPreferenceTimelineOrder = LPreferenceTimelineOrder,
            LPreferenceKeyframeMinimumPixels = LPreferenceKeyframeMinimumPixels,
            LPreferenceSectionPalette = LPreferenceSectionPalette,
            LPreferenceOverlapAllowed = LPreferenceOverlapAllowed,
            LPreferenceParallelMaximum = LPreferenceParallelMaximum,
            LPreferenceFailurePaused = LPreferenceFailurePaused,
            LPreferenceRetryAllowed = LPreferenceRetryAllowed,
            LPreferenceRetryMaximum = LPreferenceRetryMaximum,
            LPreferenceAutoResume = LPreferenceAutoResume,
            LPreferenceWorkspaceFolder = LPreferenceWorkspaceFolder,
            LPreferenceFfmpegFolder = LPreferenceFfmpegFolder,
            LPreferenceMediaPath = LPreferenceMediaPath,
            LPreferenceProgramWidth = LPreferenceProgramWidth,
            LPreferenceProgramHeight = LPreferenceProgramHeight,
            LPreferenceProgramLeft = LPreferenceProgramLeft,
            LPreferenceProgramTop = LPreferenceProgramTop,
            LPreferenceFlowHeight = LPreferenceFlowHeight,
            LPreferenceTabLayoutKeys = new List<string>(LPreferenceTabLayoutKeys),
            LPreferenceTabSelectIndex = LPreferenceTabSelectIndex,
            LPreferenceTabExports = new List<LExportSpecificPresetRecord>(LPreferenceTabExports),
            LPreferenceTabLayouts = LPreferenceTabLayouts
                .Select(lPreferenceTabLayout => lPreferenceTabLayout.LPreferenceTabLayoutClone())
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
            ("Language", lPreferenceOther.LPreferenceLanguage, LPreferenceLanguage),
            ("Verbose logging", lPreferenceOther.LPreferenceLogVerbose, LPreferenceLogVerbose),
            ("File record location", lPreferenceOther.LPreferenceRecordWorkspace, LPreferenceRecordWorkspace),
            ("Volume mode", lPreferenceOther.LPreferenceVolumeMode, LPreferenceVolumeMode),
            ("Default volume", lPreferenceOther.LPreferenceVolume, LPreferenceVolume),
            ("Autoplay on load", lPreferenceOther.LPreferenceAutoplayOnLoad, LPreferenceAutoplayOnLoad),
            ("Mousewheel", lPreferenceOther.LPreferenceWheelAction, LPreferenceWheelAction),
            ("Pause while dragging", lPreferenceOther.LPreferenceDragPaused, LPreferenceDragPaused),
            ("Timeline order", lPreferenceOther.LPreferenceTimelineOrder, LPreferenceTimelineOrder),
            ("Keyframe minimum spacing", lPreferenceOther.LPreferenceKeyframeMinimumPixels, LPreferenceKeyframeMinimumPixels),
            ("Section colour palette", lPreferenceOther.LPreferenceSectionPalette, LPreferenceSectionPalette),
            ("Allow overlapping sections", lPreferenceOther.LPreferenceOverlapAllowed, LPreferenceOverlapAllowed),
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
        if (string.IsNullOrWhiteSpace(LPreferenceLanguage)) LPreferenceLanguage = "English";
        if (LPreferenceVolumeMode is not "Unified" and not "PerTab") LPreferenceVolumeMode = "Unified";
        if (LPreferenceWheelAction is not "Seek" and not "Zoom" and not "Volume") LPreferenceWheelAction = "Seek";
        if (LPreferenceTimelineOrder is not "MapFirst" and not "ViewfinderFirst") LPreferenceTimelineOrder = "MapFirst";
        if (string.IsNullOrWhiteSpace(LPreferenceSectionPalette))
            LPreferenceSectionPalette = PSectionPalette.PSectionPaletteDefaultName;

        LPreferenceVolume = LPreferenceVolumeClamp(LPreferenceVolume);
        LPreferenceKeyframeMinimumPixels = LPreferenceNumberClamp(LPreferenceKeyframeMinimumPixels, 1, 50, 5);
        LPreferenceParallelMaximum = Math.Round(LPreferenceNumberClamp(LPreferenceParallelMaximum, 1, 8, 1));
        LPreferenceRetryMaximum = Math.Round(LPreferenceNumberClamp(LPreferenceRetryMaximum, 0, 10, 3));

        LPreferenceProgramWidth = LPreferenceNumberClamp(LPreferenceProgramWidth, 800, 4000, 1280);
        LPreferenceProgramHeight = LPreferenceNumberClamp(LPreferenceProgramHeight, 400, 3000, 760);
        LPreferenceFlowHeight = LPreferenceNumberClamp(LPreferenceFlowHeight, 200, 520, 280);
        LPreferenceWorkspaceFolder = (LPreferenceWorkspaceFolder ?? string.Empty).Trim();
        LPreferenceFfmpegFolder = (LPreferenceFfmpegFolder ?? string.Empty).Trim();
        LPreferenceMediaPath = (LPreferenceMediaPath ?? string.Empty).Trim();
        LPreferenceTabSelectIndex = Math.Max(0, LPreferenceTabSelectIndex);
        if (LPreferenceTabLayoutKeys is null || LPreferenceTabLayoutKeys.Count == 0)
            LPreferenceTabLayoutKeys = new List<string> { "Split" };
        LPreferenceTabLayouts ??= new List<LPreferenceTabLayoutRecord>();
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
    public List<double> PanelWidths { get; set; } = new();

    public bool ExportHidden { get; set; }

    public List<int> PanelsCollapsed { get; set; } = new();

    public LPreferenceTabLayoutRecord LPreferenceTabLayoutClone()
    {
        return new LPreferenceTabLayoutRecord
        {
            PanelWidths = new List<double>(PanelWidths),
            ExportHidden = ExportHidden,
            PanelsCollapsed = new List<int>(PanelsCollapsed)
        };
    }
}
