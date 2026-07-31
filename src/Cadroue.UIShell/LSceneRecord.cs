using System.Collections.Generic;
using System.Linq;
using Cadroue.UIShell.PPanels;

namespace Cadroue.UIShell;

public sealed class LSceneRecord
{
    public string LSceneName { get; set; } = string.Empty;
    public List<string> LSceneLayoutKeys { get; set; } = new();
    public List<string> LSceneTabNames { get; set; } = new();
    public List<LPresetRecord> LSceneTabExports { get; set; } = new();
    public List<LPreferenceTabLayoutRecord> LSceneTabLayouts { get; set; } = new();
    public List<int> LSceneTabRelays { get; set; } = new();
    public int LSceneTabIndex { get; set; }

    public static LSceneRecord LSceneRecordCreate(string lSceneName, LPreferenceState lState) => new()
    {
        LSceneName = lSceneName,
        LSceneLayoutKeys = new List<string>(lState.LPreferenceLayoutKeys),
        LSceneTabNames = new List<string>(lState.LPreferenceTabNames),
        LSceneTabExports = new List<LPresetRecord>(lState.LPreferenceTabExports),
        LSceneTabLayouts = lState.LPreferenceTabLayouts
            .Select(lSceneTabLayout => lSceneTabLayout.LPreferenceLayoutClone())
            .ToList(),
        LSceneTabRelays = new List<int>(lState.LPreferenceTabRelays),
        LSceneTabIndex = lState.LPreferenceTabIndex
    };

    public void LSceneStateApply(LPreferenceState lState)
    {
        lState.LPreferenceLayoutKeys = new List<string>(LSceneLayoutKeys);
        lState.LPreferenceTabNames = new List<string>(LSceneTabNames);
        lState.LPreferenceTabExports = new List<LPresetRecord>(LSceneTabExports);
        lState.LPreferenceTabLayouts = LSceneTabLayouts
            .Select(lSceneTabLayout => lSceneTabLayout.LPreferenceLayoutClone())
            .ToList();
        lState.LPreferenceTabRelays = new List<int>(LSceneTabRelays);
        lState.LPreferenceTabIndex = LSceneTabIndex;
    }
}
