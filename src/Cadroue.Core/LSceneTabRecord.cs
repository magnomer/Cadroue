using System.Collections.Generic;
using System.Linq;

namespace Cadroue.Core;

public sealed class LSceneTabRecord
{
    public List<double> LScenePanelWidths { get; set; } = new();

    public bool LSceneExportHidden { get; set; }

    public List<int> LScenePanelsCollapsed { get; set; } = new();

    public List<LSceneFunnelRule> LSceneFunnelRules { get; set; } = new();

    public LSceneInspectorRecord? LSceneInspector { get; set; }

    public bool LSceneGroupAuto { get; set; }

    public bool LSceneGroupStrict { get; set; } = true;

    public LSeriesNameMode LSceneGroupMode { get; set; } = LSeriesNameMode.LSeriesNameRemove;

    public bool LSceneAutoRelay { get; set; }

    public LSceneTabRecord LSceneTabClone()
    {
        return new LSceneTabRecord
        {
            LScenePanelWidths = new List<double>(LScenePanelWidths),
            LSceneExportHidden = LSceneExportHidden,
            LScenePanelsCollapsed = new List<int>(LScenePanelsCollapsed),
            LSceneFunnelRules = LSceneFunnelRules.Select(pRule => pRule.LSceneFunnelClone()).ToList(),
            LSceneInspector = LSceneInspector?.LSceneInspectorClone(),
            LSceneGroupAuto = LSceneGroupAuto,
            LSceneGroupStrict = LSceneGroupStrict,
            LSceneGroupMode = LSceneGroupMode,
            LSceneAutoRelay = LSceneAutoRelay
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

public sealed class LSceneFunnelRule
{
    public LSceneFunnelMatch LSceneFunnelContains { get; set; } = new();

    public LSceneFunnelMatch LSceneFunnelStart { get; set; } = new();

    public LSceneFunnelMatch LSceneFunnelEnd { get; set; } = new();

    public LSceneFunnelMatch LSceneFunnelExtension { get; set; } = new();

    public int LSceneFunnelType { get; set; }

    public string LSceneFunnelRegex { get; set; } = string.Empty;

    public bool LSceneFunnelWhole { get; set; }

    public int LSceneFunnelTarget { get; set; } = -1;

    public LSceneFunnelRule LSceneFunnelClone()
    {
        return new LSceneFunnelRule
        {
            LSceneFunnelContains = LSceneFunnelContains.LSceneFunnelClone(),
            LSceneFunnelStart = LSceneFunnelStart.LSceneFunnelClone(),
            LSceneFunnelEnd = LSceneFunnelEnd.LSceneFunnelClone(),
            LSceneFunnelExtension = LSceneFunnelExtension.LSceneFunnelClone(),
            LSceneFunnelType = LSceneFunnelType,
            LSceneFunnelRegex = LSceneFunnelRegex,
            LSceneFunnelWhole = LSceneFunnelWhole,
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
