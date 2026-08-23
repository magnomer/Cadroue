using System.Collections.Generic;
using System.Linq;

namespace Cadroue.Core;

public sealed class LSceneTabRecord
{
    public List<double> LScenePanelWidths { get; set; } = new();

    public bool LSceneExportHidden { get; set; }

    public List<int> LScenePanelsCollapsed { get; set; } = new();

    public List<LSceneFunnelRule> LSceneFunnelRules { get; set; } = new();

    public List<LSceneDetector> LSceneDetectors { get; set; } = new();

    public LSceneInspectorRecord? LSceneInspector { get; set; }

    public bool LSceneGroupAuto { get; set; }

    public bool LSceneGroupStrict { get; set; } = true;

    public LSeriesNameMode LSceneGroupMode { get; set; } = LSeriesNameMode.LSeriesNameBase;

    public bool LSceneAutoRelay { get; set; }

    public bool LSceneDetectPersistent { get; set; }

    public LSceneTabRecord LSceneTabClone()
    {
        return new LSceneTabRecord
        {
            LScenePanelWidths = new List<double>(LScenePanelWidths),
            LSceneExportHidden = LSceneExportHidden,
            LScenePanelsCollapsed = new List<int>(LScenePanelsCollapsed),
            LSceneFunnelRules = LSceneFunnelRules.Select(pRule => pRule.LSceneFunnelClone()).ToList(),
            LSceneDetectors = LSceneDetectors.Select(pDetector => pDetector.LSceneDetectorClone()).ToList(),
            LSceneInspector = LSceneInspector?.LSceneInspectorClone(),
            LSceneGroupAuto = LSceneGroupAuto,
            LSceneGroupStrict = LSceneGroupStrict,
            LSceneGroupMode = LSceneGroupMode,
            LSceneAutoRelay = LSceneAutoRelay,
            LSceneDetectPersistent = LSceneDetectPersistent
        };
    }
}

public sealed class LSceneInspectorRecord
{
    public Cadroue.Core.LSidecarAudioRecord? LSceneInspectorAudio { get; set; }

    public Cadroue.Core.LSidecarEditRecord? LSceneInspectorEdit { get; set; }

    public bool LSceneInspectorCrop { get; set; }

    public bool LSceneInspectorSkip { get; set; }

    public LSceneInspectorRecord LSceneInspectorClone()
    {
        return new LSceneInspectorRecord
        {
            LSceneInspectorAudio = LSceneInspectorAudio,
            LSceneInspectorEdit = LSceneInspectorEdit,
            LSceneInspectorCrop = LSceneInspectorCrop,
            LSceneInspectorSkip = LSceneInspectorSkip
        };
    }
}

public sealed class LSceneDetector
{
    public int LSceneDetectorKind { get; set; }

    public bool LSceneDetectorEnabled { get; set; }

    public double LSceneDetectorThreshold { get; set; }

    public double LSceneDetectorMinimum { get; set; }

    public double LSceneDetectorWindow { get; set; } = LDetector.LDetectorWindowRead(LDetectorKind.LDetectorKindLuminance).LDetectorBoundDefault;

    public int LSceneDetectorType { get; set; }

    public double LSceneDetectorHue { get; set; }

    public double LSceneDetectorSaturation { get; set; }

    public double LSceneDetectorBrightness { get; set; } = LDetectorBlank.LDetectorBlankValue;

    public double LSceneDetectorTolerance { get; set; } = LDetector.LDetectorToleranceRead().LDetectorBoundDefault;

    public double LSceneDetectorCoverage { get; set; } = LDetector.LDetectorCoverageRead().LDetectorBoundDefault;

    public LSceneDetector LSceneDetectorClone()
    {
        return new LSceneDetector
        {
            LSceneDetectorKind = LSceneDetectorKind,
            LSceneDetectorEnabled = LSceneDetectorEnabled,
            LSceneDetectorThreshold = LSceneDetectorThreshold,
            LSceneDetectorMinimum = LSceneDetectorMinimum,
            LSceneDetectorWindow = LSceneDetectorWindow,
            LSceneDetectorType = LSceneDetectorType,
            LSceneDetectorHue = LSceneDetectorHue,
            LSceneDetectorSaturation = LSceneDetectorSaturation,
            LSceneDetectorBrightness = LSceneDetectorBrightness,
            LSceneDetectorTolerance = LSceneDetectorTolerance,
            LSceneDetectorCoverage = LSceneDetectorCoverage
        };
    }
}

public sealed class LSceneFunnelRule
{
    public LSceneFunnelMatch LSceneFunnelContains { get; set; } = new();

    public LSceneFunnelMatch LSceneFunnelPrefix { get; set; } = new();

    public LSceneFunnelMatch LSceneFunnelEnd { get; set; } = new();

    public LSceneFunnelMatch LSceneFunnelExtension { get; set; } = new();

    public int LSceneFunnelType { get; set; }

    public string LSceneFunnelRegex { get; set; } = string.Empty;

    public bool LSceneFunnelWhole { get; set; }

    public bool LSceneFunnelRemainder { get; set; }

    public int LSceneFunnelTarget { get; set; } = -1;

    public LSceneFunnelRule LSceneFunnelClone()
    {
        return new LSceneFunnelRule
        {
            LSceneFunnelContains = LSceneFunnelContains.LSceneFunnelClone(),
            LSceneFunnelPrefix = LSceneFunnelPrefix.LSceneFunnelClone(),
            LSceneFunnelEnd = LSceneFunnelEnd.LSceneFunnelClone(),
            LSceneFunnelExtension = LSceneFunnelExtension.LSceneFunnelClone(),
            LSceneFunnelType = LSceneFunnelType,
            LSceneFunnelRegex = LSceneFunnelRegex,
            LSceneFunnelWhole = LSceneFunnelWhole,
            LSceneFunnelRemainder = LSceneFunnelRemainder,
            LSceneFunnelTarget = LSceneFunnelTarget
        };
    }
}

public enum LSceneFunnelForm
{
    LSceneFunnelFilename,
    LSceneFunnelRegex
}

public sealed class LSceneFunnelMatch
{
    public string LSceneFunnelText { get; set; } = string.Empty;

    public bool LSceneFunnelCase { get; set; }

    public bool LSceneFunnelJoin { get; set; } = true;

    public LSceneFunnelMatch LSceneFunnelClone()
    {
        return new LSceneFunnelMatch
        {
            LSceneFunnelText = LSceneFunnelText,
            LSceneFunnelCase = LSceneFunnelCase,
            LSceneFunnelJoin = LSceneFunnelJoin
        };
    }
}
