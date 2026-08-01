using Cadroue.UIShell.PFlow;
using Cadroue.UIShell.PPanels;

namespace Cadroue.UIShell.PControlBar;

public sealed class LRelaySectionRecord
{
    public long LRelayStartTicks { get; set; }
    public long LRelayEndTicks { get; set; }
    public int LRelayColorIndex { get; set; }
    public string LRelayName { get; set; } = string.Empty;
    public string LRelayPrefix { get; set; } = string.Empty;
    public string LRelaySuffix { get; set; } = string.Empty;
    public bool LRelayHidden { get; set; }

    public static LRelaySectionRecord LRelaySectionCreate(LSegment lSegment) => new()
    {
        LRelayStartTicks = lSegment.LSegmentStart.Ticks,
        LRelayEndTicks = lSegment.LSegmentEnd.Ticks,
        LRelayColorIndex = lSegment.LSegmentColorIndex,
        LRelayName = lSegment.LSegmentName,
        LRelayPrefix = lSegment.LSegmentPrefix,
        LRelaySuffix = lSegment.LSegmentSuffix,
        LRelayHidden = lSegment.LSegmentHidden
    };

    public LSegment LRelaySegmentCreate() => new(
        TimeSpan.FromTicks(LRelayStartTicks),
        TimeSpan.FromTicks(LRelayEndTicks),
        LRelayColorIndex,
        LRelayName)
    {
        LSegmentPrefix = LRelayPrefix ?? string.Empty,
        LSegmentSuffix = LRelaySuffix ?? string.Empty,
        LSegmentHidden = LRelayHidden
    };
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

    public static LRelay LRelayTabCreate(PTabRecord pTabRecord, double lDropLeft, double lDropTop)
    {
        PWorkspace pWorkspace = pTabRecord.PTabWorkspace;
        var lRelay = new LRelay
        {
            LRelayLayoutKey = pTabRecord.PTabLayoutKey,
            LRelayCustomName = pTabRecord.PTabNameCustom,
            LRelayExport = LPresetRecord.LPresetRecordCreate(pWorkspace.PWorkspaceExportState),
            LRelayLayout = pWorkspace.PWorkspaceLayoutRead(),
            LRelaySourcePath = pWorkspace.PWorkspaceViewer?.PViewerSourcePath ?? string.Empty,
            LRelayDropLeft = lDropLeft,
            LRelayDropTop = lDropTop,
            LRelaySenderProcess = Environment.ProcessId,
            LRelayId = Guid.NewGuid().ToString("N")
        };

        if (pWorkspace.PWorkspaceFlow is { } pFlow)
        {
            lRelay.LRelaySections = pFlow.PFlowSectionsRead()
                .Select(LRelaySectionRecord.LRelaySectionCreate)
                .ToList();
            lRelay.LRelaySectionIndex = pFlow.PFlowSelectionRead();
        }

        return lRelay;
    }

    public LPreset LRelayExportCreate() => LRelayExport.LPresetStateCreate();

    public IReadOnlyList<LSegment> LRelaySectionsCreate() =>
        LRelaySections.Select(lSection => lSection.LRelaySegmentCreate()).ToArray();
}
