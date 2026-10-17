using Cadroue.Core;

namespace Cadroue.Infrastructure;

public sealed class LRelaySectionEntry
{
    public long LRelayStartTicks { get; set; }
    public long LRelayEndTicks { get; set; }
    public int LRelayColorIndex { get; set; }
    public string LRelayName { get; set; } = string.Empty;
    public string LRelayPrefix { get; set; } = string.Empty;
    public string LRelaySuffix { get; set; } = string.Empty;
    public bool LRelayHidden { get; set; }
    public bool LRelayDetected { get; set; }
}

public sealed class LRelay
{
    public string LRelayLayoutKey { get; set; } = "Split";
    public string LRelayCustomName { get; set; } = string.Empty;
    public LPresetDocument LRelayExport { get; set; } = new();
    public LSceneTabEntry LRelayLayout { get; set; } = new();
    public List<string> LRelayPaths { get; set; } = new();
    public string LRelaySourcePath { get; set; } = string.Empty;
    public long LRelayPositionTicks { get; set; }
    public double? LRelayVolume { get; set; }
    public long? LRelayOriginTicks { get; set; }
    public long? LRelayLimitTicks { get; set; }
    public List<LRelaySectionEntry> LRelaySections { get; set; } = new();
    public int? LRelaySectionIndex { get; set; }

    public double LRelayDropLeft { get; set; }
    public double LRelayDropTop { get; set; }

    public int LRelaySenderProcess { get; set; }

    public string LRelayId { get; set; } = string.Empty;

    public string LRelayVersion { get; set; } = string.Empty;
}
