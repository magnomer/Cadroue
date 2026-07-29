using Cadroue.Core;
using Cadroue.UIShell.PPanels;

namespace Cadroue.UIShell.PMainArea;

public sealed record LConvertWorkDescription(
    IReadOnlyList<string> LConvertSourcePaths,
    LWorkOutput LConvertOutput,
    IReadOnlyDictionary<string, LWorkMedia>? LConvertMedia = null);

public static partial class LConvert
{
    public static async Task<int> LConvertDescribe(
        LWorkPriority lWorkPriority,
        IReadOnlyList<string> lConvertSourcePaths,
        LExportSpecificState lExportSpecificState)
    {
        LWorkOutput lConvertOutput = lExportSpecificState.LPresetOutputCreate();
        LConvertWorkDescription lConvertWorkDescription = new(lConvertSourcePaths, lConvertOutput);

        IReadOnlyList<LWorkItem> lConvertWorkItems =
            LConvert.LConvertInterpret(lWorkPriority, lConvertWorkDescription);
        int lConvertAdded = LSchedule.LScheduleCurrent.LScheduleAdd(lConvertWorkItems);
        LAppLog.LInfo(
            $"Convert queued {lConvertAdded} job(s) at {lWorkPriority} from {lConvertSourcePaths.Count} listed file(s)");

        await LConvertDurationFill(lConvertWorkItems).ConfigureAwait(true);
        return lConvertAdded;
    }

    private static Task LConvertDurationFill(IReadOnlyList<LWorkItem> lConvertWorkItems)
    {
        LWorkItem[] lConvertUnknown = lConvertWorkItems
            .Where(lWorkItem => lWorkItem.LWorkEnd <= TimeSpan.Zero)
            .ToArray();
        if (lConvertUnknown.Length == 0)
        {
            return Task.CompletedTask;
        }

        return Task.Run(() => Parallel.ForEach(
            lConvertUnknown,
            new ParallelOptions { MaxDegreeOfParallelism = LConvertParallelRead() },
            lWorkItem => LSchedule.LScheduleCurrent.LScheduleDurationSet(
                lWorkItem.LWorkId,
                Cadroue.Media.LSidecarStore.LSidecarDurationResolve(lWorkItem.LWorkSourcePath))));
    }

    internal static int LConvertParallelRead() => Math.Clamp(Environment.ProcessorCount, 1, 8);
}
