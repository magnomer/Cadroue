using Cadroue.UIShell.PFlow;
using Cadroue.UIShell.PPanels;

namespace Cadroue.UIShell.PControlBar;

public sealed class LRelaySectionRecord
{
    public long StartTicks { get; set; }
    public long EndTicks { get; set; }
    public int ColorIndex { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Prefix { get; set; } = string.Empty;
    public string Suffix { get; set; } = string.Empty;
    public bool Hidden { get; set; }

    public static LRelaySectionRecord LRelaySectionCreate(LSegment lSegment) => new()
    {
        StartTicks = lSegment.LSegmentStart.Ticks,
        EndTicks = lSegment.LSegmentEnd.Ticks,
        ColorIndex = lSegment.LSegmentColorIndex,
        Name = lSegment.LSegmentName,
        Prefix = lSegment.LSegmentPrefix,
        Suffix = lSegment.LSegmentSuffix,
        Hidden = lSegment.LSegmentHidden
    };

    public LSegment LRelaySegmentCreate() => new(
        TimeSpan.FromTicks(StartTicks),
        TimeSpan.FromTicks(EndTicks),
        ColorIndex,
        Name)
    {
        LSegmentPrefix = Prefix ?? string.Empty,
        LSegmentSuffix = Suffix ?? string.Empty,
        LSegmentHidden = Hidden
    };
}

public sealed class LRelay
{
    public string LayoutKey { get; set; } = "Split";
    public string CustomName { get; set; } = string.Empty;
    public LExportSpecificPresetRecord Export { get; set; } = new();
    public LPreferenceTabLayoutRecord Layout { get; set; } = new();
    public string SourcePath { get; set; } = string.Empty;
    public List<LRelaySectionRecord> Sections { get; set; } = new();
    public int? SectionSelectIndex { get; set; }

    public double DropLeft { get; set; }
    public double DropTop { get; set; }

    public int SenderProcessId { get; set; }

    public string RelayId { get; set; } = string.Empty;

    public static LRelay LRelayTabCreate(PTabRecord pTabRecord, double lDropLeft, double lDropTop)
    {
        PWorkspace pWorkspace = pTabRecord.PTabWorkspace;
        var lRelay = new LRelay
        {
            LayoutKey = pTabRecord.PTabLayoutKey,
            CustomName = pTabRecord.PTabNameCustom,
            Export = LExportSpecificPresetRecord.LPresetRecordCreate(pWorkspace.PWorkspaceExportState),
            Layout = pWorkspace.PWorkspaceLayoutRead(),
            SourcePath = pWorkspace.PWorkspaceViewer?.PViewerSourcePath ?? string.Empty,
            DropLeft = lDropLeft,
            DropTop = lDropTop,
            SenderProcessId = Environment.ProcessId,
            RelayId = Guid.NewGuid().ToString("N")
        };

        if (pWorkspace.PWorkspaceFlow is { } pFlow)
        {
            lRelay.Sections = pFlow.PFlowSectionsRead()
                .Select(LRelaySectionRecord.LRelaySectionCreate)
                .ToList();
            lRelay.SectionSelectIndex = pFlow.PFlowSectionSelectRead();
        }

        return lRelay;
    }

    public LExportSpecificState LRelayExportCreate() => Export.LPresetStateCreate();

    public IReadOnlyList<LSegment> LRelaySectionsCreate() =>
        Sections.Select(lSection => lSection.LRelaySegmentCreate()).ToArray();
}
