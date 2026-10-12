using System;
using System.Linq;
using Cadroue.Core;

namespace Cadroue.Application;

public sealed record LScheduleLineageStep(
    Guid LScheduleStepId,
    Guid LScheduleStepBatch,
    Guid LScheduleStepRelay,
    string LScheduleStepTab,
    LWorkKind LScheduleStepKind,
    bool LScheduleStepSubject,
    string? LScheduleStepName,
    long? LScheduleStepSource,
    double? LScheduleStepRatio,
    string LScheduleSourceExtension,
    string LScheduleOutputExtension)
{
    internal static LScheduleLineageStep LScheduleStepCreate(
        Func<string, string> lNameRead,
        Func<string, string> lExtensionRead,
        LWorkItem lWorkItem,
        bool lOutputMatch,
        long? lOriginBytes)
    {
        string? lSplitName = lWorkItem.LWorkKind == LWorkKind.LWorkKindSplit && !lOutputMatch
            ? LScheduleLineagePlan.LScheduleNameRead(lNameRead, lWorkItem.LWorkOutputPath)
            : null;
        return new LScheduleLineageStep(
            lWorkItem.LWorkId,
            lWorkItem.LWorkBatchId,
            lWorkItem.LWorkRelaySource,
            lWorkItem.LWorkTab,
            lWorkItem.LWorkKind,
            lOutputMatch,
            lSplitName,
            LScheduleSourceRead(lWorkItem),
            LScheduleRatioRead(lWorkItem, lOutputMatch, lOriginBytes),
            lExtensionRead(lWorkItem.LWorkSourcePath),
            lExtensionRead(lWorkItem.LWorkOutputPath));
    }

    private static double? LScheduleRatioRead(LWorkItem lWorkItem, bool lOutputMatch, long? lOriginBytes)
    {
        if (lOutputMatch)
        {
            return LScheduleRatioRead(lWorkItem);
        }

        if (lOriginBytes is not { } lOriginWhole || lOriginWhole <= 0 || lWorkItem.LWorkOutputBytes is not { } lOutput)
        {
            return null;
        }

        return (double)lOutput / lOriginWhole;
    }

    private static double? LScheduleRatioRead(LWorkItem lWorkItem)
    {
        if (lWorkItem.LWorkOutputBytes is not { } lOutputWhole
            || LScheduleSourceRead(lWorkItem) is not { } lSourceWhole
            || lSourceWhole <= 0)
        {
            return null;
        }

        return (double)lOutputWhole / lSourceWhole;
    }

    private static long? LScheduleSourceRead(LWorkItem lWorkItem)
    {
        if (lWorkItem.LWorkMergeSources.Count > 1 && lWorkItem.LWorkMergeBytes.Count > 0)
        {
            long lMergeTotal = lWorkItem.LWorkMergeBytes.Sum();
            if (lMergeTotal > 0)
            {
                return lMergeTotal;
            }
        }

        return lWorkItem.LWorkSourceBytes;
    }
}
