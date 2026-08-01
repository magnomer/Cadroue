using Cadroue.Core;

namespace Cadroue.Infrastructure;

public sealed class LRelaySectionRecord
{
    public long LRelayStartTicks { get; set; }
    public long LRelayEndTicks { get; set; }
    public int LRelayColorIndex { get; set; }
    public string LRelayName { get; set; } = string.Empty;
    public string LRelayPrefix { get; set; } = string.Empty;
    public string LRelaySuffix { get; set; } = string.Empty;
    public bool LRelayHidden { get; set; }
}

public sealed class LRelay
{
    public string LRelayLayoutKey { get; set; } = "Split";
    public string LRelayCustomName { get; set; } = string.Empty;
    public LPresetRecord LRelayExport { get; set; } = new();
    public LSceneTabRecord LRelayLayout { get; set; } = new();
    public string LRelaySourcePath { get; set; } = string.Empty;
    public List<LRelaySectionRecord> LRelaySections { get; set; } = new();
    public int? LRelaySectionIndex { get; set; }

    public double LRelayDropLeft { get; set; }
    public double LRelayDropTop { get; set; }

    public int LRelaySenderProcess { get; set; }

    public string LRelayId { get; set; } = string.Empty;
}
