using System.Reflection;
using Cadroue.Core;

namespace Cadroue.Infrastructure;

public static class LRelayPayload
{
    public static List<LRelaySectionEntry> LRelayRecordsCreate(IReadOnlyList<LPiece> lRelaySegments) =>
        lRelaySegments
            .Select(lRelaySegment => new LRelaySectionEntry
            {
                LRelayStartTicks = lRelaySegment.LPieceOrigin.Ticks,
                LRelayEndTicks = lRelaySegment.LPieceEnd.Ticks,
                LRelayColorIndex = lRelaySegment.LPieceColorIndex,
                LRelayName = lRelaySegment.LPieceName,
                LRelayPrefix = lRelaySegment.LPiecePrefix,
                LRelaySuffix = lRelaySegment.LPieceSuffix,
                LRelayHidden = lRelaySegment.LPieceHidden,
                LRelayDetected = lRelaySegment.LPieceDetected
            })
            .ToList();

    public static LRelay LRelayCreate(
        string layoutKey, string customName, LPresetDocument export,
        LSceneTabEntry layout, double dropLeft, double dropTop) =>
        new()
        {
            LRelayLayoutKey = layoutKey,
            LRelayCustomName = customName,
            LRelayExport = export,
            LRelayLayout = layout,
            LRelayDropLeft = dropLeft,
            LRelayDropTop = dropTop,
            LRelaySenderProcess = Environment.ProcessId,
            LRelayId = Guid.NewGuid().ToString("N"),
            LRelayVersion = LRelayVersionRead()
        };

    public static bool LRelayVersionMatch(LRelay lRelay) =>
        string.Equals(lRelay.LRelayVersion, LRelayVersionRead(), StringComparison.Ordinal);

    public static string LRelayVersionRead()
    {
        Assembly lAssembly = Assembly.GetEntryAssembly() ?? typeof(LRelay).Assembly;
        string? lInformational = lAssembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        return string.IsNullOrWhiteSpace(lInformational)
            ? lAssembly.GetName().Version?.ToString() ?? "unknown"
            : lInformational;
    }

    internal static LRelay? LRelayPayloadLoad(string lRelayFilePath)
    {
        LRelay? lRelay = LRelayStore.LRelayFileLoad(lRelayFilePath);
        if (lRelay is null || LRelayVersionMatch(lRelay))
        {
            return lRelay;
        }

        LTraceLog.LTraceErrorRecord(
            $"Relayed tab refused: sender version '{lRelay.LRelayVersion}' "
            + $"differs from '{LRelayVersionRead()}'",
            null);
        return null;
    }

    public static IReadOnlyList<LPiece> LRelaySegmentsCreate(IReadOnlyList<LRelaySectionEntry> lRelaySections) =>
        lRelaySections
            .Select(lRelaySection => new LPiece(
                TimeSpan.FromTicks(lRelaySection.LRelayStartTicks),
                TimeSpan.FromTicks(lRelaySection.LRelayEndTicks),
                lRelaySection.LRelayColorIndex,
                lRelaySection.LRelayName)
            {
                LPiecePrefix = lRelaySection.LRelayPrefix,
                LPieceSuffix = lRelaySection.LRelaySuffix,
                LPieceHidden = lRelaySection.LRelayHidden,
                LPieceDetected = lRelaySection.LRelayDetected
            })
            .ToList();
}
