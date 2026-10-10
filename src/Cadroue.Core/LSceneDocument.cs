using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Cadroue.Core;

public sealed class LSceneDocument
{
    [JsonIgnore]
    public bool LSceneDefaultTabs { get; set; }

    public int LSceneVersion { get; set; }
    public string LSceneName { get; set; } = string.Empty;
    public List<string> LSceneLayoutKeys { get; set; } = new();
    public List<string> LSceneTabNames { get; set; } = new();
    public List<LPresetDocument> LSceneTabExports { get; set; } = new();
    public List<LSceneTabEntry> LSceneTabLayouts { get; set; } = new();
    public List<int> LSceneTabRelays { get; set; } = new();
    public int LSceneTabIndex { get; set; }
}
