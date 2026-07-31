using System.Collections.Generic;
using System.Linq;

namespace Cadroue.UIShell;

public sealed class LPreferenceTabLayoutRecord
{
    public List<double> LPreferencePanelWidths { get; set; } = new();

    public bool LPreferenceExportHidden { get; set; }

    public List<int> LPreferencePanelsCollapsed { get; set; } = new();

    public List<LPreferenceFunnelRuleRecord> LPreferenceFunnelRules { get; set; } = new();

    public LPreferenceInspectorPersistentRecord? LPreferenceInspectorPersistent { get; set; }

    public bool LPreferenceGroupAuto { get; set; }

    public bool LPreferenceGroupStrict { get; set; } = true;

    public bool LPreferenceAutoRelay { get; set; }

    public LPreferenceTabLayoutRecord LPreferenceLayoutClone()
    {
        return new LPreferenceTabLayoutRecord
        {
            LPreferencePanelWidths = new List<double>(LPreferencePanelWidths),
            LPreferenceExportHidden = LPreferenceExportHidden,
            LPreferencePanelsCollapsed = new List<int>(LPreferencePanelsCollapsed),
            LPreferenceFunnelRules = LPreferenceFunnelRules.Select(pRule => pRule.LPreferenceFunnelClone()).ToList(),
            LPreferenceInspectorPersistent = LPreferenceInspectorPersistent?.LPreferenceInspectorClone(),
            LPreferenceGroupAuto = LPreferenceGroupAuto,
            LPreferenceGroupStrict = LPreferenceGroupStrict,
            LPreferenceAutoRelay = LPreferenceAutoRelay
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
    public string LPreferenceFunnelStart { get; set; } = string.Empty;

    public string LPreferenceFunnelEnd { get; set; } = string.Empty;

    public bool LPreferenceFunnelJoin { get; set; }

    public int LPreferenceFunnelTarget { get; set; } = -1;

    public LPreferenceFunnelRuleRecord LPreferenceFunnelClone()
    {
        return new LPreferenceFunnelRuleRecord
        {
            LPreferenceFunnelStart = LPreferenceFunnelStart,
            LPreferenceFunnelEnd = LPreferenceFunnelEnd,
            LPreferenceFunnelJoin = LPreferenceFunnelJoin,
            LPreferenceFunnelTarget = LPreferenceFunnelTarget
        };
    }
}
