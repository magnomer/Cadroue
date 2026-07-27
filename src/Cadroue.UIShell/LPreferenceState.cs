using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Cadroue.UIShell.PPanels;

namespace Cadroue.UIShell;

public sealed class LPreferenceState
{
    public double LPreferenceVolume { get; set; }
    public string LPreferenceVolumeMode { get; set; } = "Single global volume";
    public double LPreferenceProgramWidth { get; set; }
    public double LPreferenceProgramHeight { get; set; }
    public double? LPreferenceProgramLeft { get; set; }
    public double? LPreferenceProgramTop { get; set; }
    public double LPreferenceFlowHeight { get; set; }
    public double LPreferenceFontSize { get; set; }
    public double LPreferenceKeyframeMinimumPixels { get; set; }
    public double LPreferenceImmediateKeyframeWindowMilliseconds { get; set; }
    public double LPreferenceSectionOpacity { get; set; }
    public double LPreferenceHistoryMaximum { get; set; }
    public bool LPreferenceAutoplayOnLoad { get; set; }
    public bool LPreferenceGroupDuplicateAllowed { get; set; }
    public string LPreferenceTimelineOrder { get; set; } = "OverviewFirst";
    public string LPreferenceHistoryMode { get; set; } = "LastUsed";
    public List<string> LPreferenceTabLayoutKeys { get; set; } = new();
    public int LPreferenceTabSelectIndex { get; set; }

    /// <summary>
    /// Each tab's own export settings, index-aligned with <see cref="LPreferenceTabLayoutKeys"/>.
    /// Tabs keep independent settings, so two Split tabs restore with the settings each
    /// of them had. A short list (older preferences file) leaves the extra tabs on defaults.
    /// </summary>
    public List<LExportSpecificPresetRecord> LPreferenceTabExports { get; set; } = new();

    public static LPreferenceState LPreferenceDefaultCreate()
    {
        return new LPreferenceState
        {
            LPreferenceVolume = 100,
            LPreferenceVolumeMode = "Single global volume",
            LPreferenceProgramWidth = 1280,
            LPreferenceProgramHeight = 760,
            LPreferenceProgramLeft = null,
            LPreferenceProgramTop = null,
            LPreferenceFlowHeight = 280,
            LPreferenceFontSize = 13,
            LPreferenceKeyframeMinimumPixels = 5,
            LPreferenceImmediateKeyframeWindowMilliseconds = 20000,
            LPreferenceSectionOpacity = 0.65,
            LPreferenceHistoryMaximum = 100,
            LPreferenceAutoplayOnLoad = false,
            LPreferenceGroupDuplicateAllowed = false,
            LPreferenceTimelineOrder = "OverviewFirst",
            LPreferenceHistoryMode = "LastUsed",
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
            LPreferenceVolume = LPreferenceVolume,
            LPreferenceVolumeMode = LPreferenceVolumeMode,
            LPreferenceProgramWidth = LPreferenceProgramWidth,
            LPreferenceProgramHeight = LPreferenceProgramHeight,
            LPreferenceProgramLeft = LPreferenceProgramLeft,
            LPreferenceProgramTop = LPreferenceProgramTop,
            LPreferenceFlowHeight = LPreferenceFlowHeight,
            LPreferenceFontSize = LPreferenceFontSize,
            LPreferenceKeyframeMinimumPixels = LPreferenceKeyframeMinimumPixels,
            LPreferenceImmediateKeyframeWindowMilliseconds = LPreferenceImmediateKeyframeWindowMilliseconds,
            LPreferenceSectionOpacity = LPreferenceSectionOpacity,
            LPreferenceHistoryMaximum = LPreferenceHistoryMaximum,
            LPreferenceAutoplayOnLoad = LPreferenceAutoplayOnLoad,
            LPreferenceGroupDuplicateAllowed = LPreferenceGroupDuplicateAllowed,
            LPreferenceTimelineOrder = LPreferenceTimelineOrder,
            LPreferenceHistoryMode = LPreferenceHistoryMode,
            LPreferenceTabLayoutKeys = new List<string>(LPreferenceTabLayoutKeys),
            LPreferenceTabSelectIndex = LPreferenceTabSelectIndex,
            LPreferenceTabExports = new List<LExportSpecificPresetRecord>(LPreferenceTabExports)
        };
    }

    public void LPreferenceNormalize()
    {
        LPreferenceVolume = LPreferenceVolumeClamp(LPreferenceVolume);
        LPreferenceProgramWidth = LPreferenceNumberClamp(LPreferenceProgramWidth, 800, 4000, 1280);
        LPreferenceProgramHeight = LPreferenceNumberClamp(LPreferenceProgramHeight, 400, 3000, 760);
        LPreferenceFlowHeight = LPreferenceNumberClamp(LPreferenceFlowHeight, 200, 520, 280);
        LPreferenceFontSize = LPreferenceNumberClamp(LPreferenceFontSize, 9, 18, 13);
        LPreferenceKeyframeMinimumPixels = LPreferenceNumberClamp(LPreferenceKeyframeMinimumPixels, 1, 50, 5);
        LPreferenceImmediateKeyframeWindowMilliseconds = LPreferenceNumberClamp(LPreferenceImmediateKeyframeWindowMilliseconds, 1000, 600000, 20000);
        LPreferenceSectionOpacity = LPreferenceNumberClamp(LPreferenceSectionOpacity, 0.10, 0.95, 0.65);
        LPreferenceHistoryMaximum = LPreferenceNumberClamp(LPreferenceHistoryMaximum, 0, 1000000, 100);
        if (LPreferenceVolumeMode is not "Single global volume" and not "Per-tab volume") LPreferenceVolumeMode = "Single global volume";
        if (LPreferenceTimelineOrder is not "OverviewFirst" and not "WorkingFirst") LPreferenceTimelineOrder = "OverviewFirst";
        if (LPreferenceHistoryMode is not "Hover" and not "LastUsed") LPreferenceHistoryMode = "LastUsed";
        LPreferenceTabSelectIndex = Math.Max(0, LPreferenceTabSelectIndex);
        if (LPreferenceTabLayoutKeys is null || LPreferenceTabLayoutKeys.Count == 0)
            LPreferenceTabLayoutKeys = new List<string> { "Split" };
    }

    [JsonIgnore]
    public bool LPreferenceVolumeSingleGlobal => LPreferenceVolumeMode == "Single global volume";

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
