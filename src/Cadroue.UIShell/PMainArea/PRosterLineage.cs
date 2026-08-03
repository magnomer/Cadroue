using System.IO;
using Cadroue.Core;
using Cadroue.Infrastructure;

namespace Cadroue.UIShell.PMainArea;

public sealed partial class PRoster
{
    private sealed class PRosterLineageEntry
    {
        public required Guid PRosterLineageId { get; init; }
        public required string PRosterLineageSubject { get; init; }
        public required List<LWorkItem> PRosterLineageItems { get; init; }
        public long? PLineageOriginBytes { get; set; }
    }

    private IReadOnlyList<PRosterLineageEntry> PRosterLineageRead(IReadOnlyList<LWorkItem> pWorkItems)
    {
        var pLineageOrder = new List<PRosterLineageEntry>();
        var pLineageIndex = new Dictionary<Guid, PRosterLineageEntry>();

        foreach (LWorkItem pWorkItem in pWorkItems)
        {
            Guid pLineageId = pRosterSchedule.LScheduleLineageRead(pWorkItem);
            if (!pLineageIndex.TryGetValue(pLineageId, out PRosterLineageEntry? pLineageEntry))
            {
                pLineageEntry = new PRosterLineageEntry
                {
                    PRosterLineageId = pLineageId,
                    PRosterLineageSubject = PLineageSubjectRead(pWorkItem, pLineageId),
                    PRosterLineageItems = new List<LWorkItem>()
                };
                pLineageIndex[pLineageId] = pLineageEntry;
                pLineageOrder.Add(pLineageEntry);
            }

            pLineageEntry.PRosterLineageItems.Add(pWorkItem);
        }

        foreach (PRosterLineageEntry pLineageEntry in pLineageOrder)
        {
            pLineageEntry.PLineageOriginBytes = PLineageOriginRead(pLineageEntry, pWorkItems);
        }

        return pLineageOrder;
    }

    private static string PLineageSubjectRead(LWorkItem pLineageFirst, Guid pLineageId)
    {
        if (pLineageFirst.LWorkKind == LWorkKind.LWorkKindMerge
            || LScheduleLineage.LScheduleFileRead(pLineageFirst.LWorkOutputPath) == pLineageId)
        {
            return pLineageFirst.LWorkOutputPath;
        }

        return pLineageFirst.LWorkSourcePath;
    }

    private static long? PLineageOriginRead(
        PRosterLineageEntry pLineageEntry,
        IReadOnlyList<LWorkItem> pWorkItems)
    {
        string pSubject = pLineageEntry.PRosterLineageSubject;
        foreach (LWorkItem pWorkItem in pWorkItems)
        {
            if (PRosterLineageMatch(pWorkItem.LWorkOutputPath, pSubject) && pWorkItem.LWorkOutputBytes is { } pOutput)
            {
                return pOutput;
            }

            if (PRosterLineageMatch(pWorkItem.LWorkSourcePath, pSubject) && pWorkItem.LWorkSourceBytes is { } pSource)
            {
                return pSource;
            }
        }

        try
        {
            return File.Exists(pSubject) ? new FileInfo(pSubject).Length : null;
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }

    private static string PLineageStepRead(LWorkItem pWorkItem, string pSubject)
    {
        if (pWorkItem.LWorkKind == LWorkKind.LWorkKindSplit
            && !PRosterLineageMatch(pWorkItem.LWorkOutputPath, pSubject))
        {
            return LLocalization.LLocalizationFormat(
                "Roster.Lineage.Split", PLineageFileRead(pWorkItem.LWorkOutputPath));
        }

        return PLineageTabRead(pWorkItem);
    }

    private static string PLineageTabRead(LWorkItem pWorkItem)
    {
        string pTabName = LCourier.LWorkTitleRead(pWorkItem);

        return LLocalization.LLocalizationFormat(
            "Roster.Lineage.Step",
            string.IsNullOrWhiteSpace(pTabName) ? PLineageKindRead(pWorkItem.LWorkKind) : pTabName);
    }

    private static string PLineageKindRead(LWorkKind pWorkKind) =>
        LLocalization.LLocalizationTextRead(pWorkKind switch
        {
            LWorkKind.LWorkKindEdit => "Roster.Kind.Edit",
            LWorkKind.LWorkKindAudio => "Roster.Kind.Audio",
            LWorkKind.LWorkKindConvert => "Roster.Kind.Convert",
            LWorkKind.LWorkKindMerge => "Roster.Kind.Merge",
            _ => "Roster.Kind.Split"
        });

    private static string PLineageRatioFormat(LWorkItem pWorkItem, string pSubject, long? pOriginBytes)
    {
        if (PRosterLineageMatch(pWorkItem.LWorkOutputPath, pSubject))
        {
            return PRosterRatioFormat(pWorkItem);
        }

        if (pOriginBytes is not { } pOriginWhole || pOriginWhole <= 0 || pWorkItem.LWorkOutputBytes is not { } pOutput)
        {
            return "-";
        }

        return $"{(double)pOutput / pOriginWhole:P1}";
    }

    private static bool PRosterLineageMatch(string pLeftPath, string pRightPath) =>
        PLineagePathRead(pLeftPath) is { } pLeftKey
        && PLineagePathRead(pRightPath) is { } pRightKey
        && string.Equals(pLeftKey, pRightKey, StringComparison.OrdinalIgnoreCase);

    private static string? PLineagePathRead(string pPath)
    {
        if (string.IsNullOrWhiteSpace(pPath))
        {
            return null;
        }

        try
        {
            return Path.GetFullPath(pPath);
        }
        catch (Exception pPathError) when (
            pPathError is ArgumentException or IOException or NotSupportedException)
        {
            return pPath;
        }
    }

    private static string PLineageTitleRead(PRosterLineageEntry pLineageEntry)
    {
        string pLineageName = PLineageFileRead(pLineageEntry.PRosterLineageSubject);
        return pLineageEntry.PRosterLineageItems.Count == 1
            ? LLocalization.LLocalizationFormat("Roster.Lineage.One", pLineageName)
            : LLocalization.LLocalizationFormat(
                "Roster.Lineage.Many", pLineageName, pLineageEntry.PRosterLineageItems.Count);
    }

    private static string PLineageFileRead(string pFilePath)
    {
        if (string.IsNullOrWhiteSpace(pFilePath))
        {
            return LLocalization.LLocalizationTextRead("Roster.Lineage.Unknown");
        }

        try
        {
            return Path.GetFileName(pFilePath);
        }
        catch (ArgumentException)
        {
            return pFilePath;
        }
    }
}
