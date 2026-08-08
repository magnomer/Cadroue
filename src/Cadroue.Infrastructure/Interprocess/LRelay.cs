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

public static class LRelayPayload
{
    public static List<LRelaySectionRecord> LRelayRecordsCreate(IReadOnlyList<LPiece> lRelaySegments) =>
        lRelaySegments
            .Select(lRelaySegment => new LRelaySectionRecord
            {
                LRelayStartTicks = lRelaySegment.LPieceStart.Ticks,
                LRelayEndTicks = lRelaySegment.LPieceEnd.Ticks,
                LRelayColorIndex = lRelaySegment.LPieceColorIndex,
                LRelayName = lRelaySegment.LPieceName,
                LRelayPrefix = lRelaySegment.LPiecePrefix,
                LRelaySuffix = lRelaySegment.LPieceSuffix,
                LRelayHidden = lRelaySegment.LPieceHidden
            })
            .ToList();

    public static LRelay LRelayCreate(
        string layoutKey, string customName, LPresetRecord export,
        LSceneTabRecord layout, string sourcePath, double dropLeft, double dropTop,
        IReadOnlyList<LPiece> sections, int? sectionIndex) =>
        new()
        {
            LRelayLayoutKey = layoutKey,
            LRelayCustomName = customName,
            LRelayExport = export,
            LRelayLayout = layout,
            LRelaySourcePath = sourcePath,
            LRelayDropLeft = dropLeft,
            LRelayDropTop = dropTop,
            LRelaySections = LRelayRecordsCreate(sections),
            LRelaySectionIndex = sectionIndex,
            LRelaySenderProcess = Environment.ProcessId,
            LRelayId = Guid.NewGuid().ToString("N")
        };

    public static IReadOnlyList<LPiece> LRelaySegmentsCreate(IReadOnlyList<LRelaySectionRecord> lRelaySections) =>
        lRelaySections
            .Select(lRelaySection => new LPiece(
                TimeSpan.FromTicks(lRelaySection.LRelayStartTicks),
                TimeSpan.FromTicks(lRelaySection.LRelayEndTicks),
                lRelaySection.LRelayColorIndex,
                lRelaySection.LRelayName)
            {
                LPiecePrefix = lRelaySection.LRelayPrefix,
                LPieceSuffix = lRelaySection.LRelaySuffix,
                LPieceHidden = lRelaySection.LRelayHidden
            })
            .ToList();
}
