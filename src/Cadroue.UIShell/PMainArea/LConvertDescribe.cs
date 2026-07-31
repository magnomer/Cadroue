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
        LPreset lExportSpecificState,
        Guid lConvertRelayTarget = default)
    {
        LWorkOutput lConvertOutput = lExportSpecificState.LPresetOutputCreate();
        LConvertWorkDescription lConvertWorkDescription = new(lConvertSourcePaths, lConvertOutput);

        IReadOnlyList<LWorkItem> lConvertWorkItems =
            LConvert.LConvertInterpret(lWorkPriority, lConvertWorkDescription);
        int lConvertAdded = LSchedule.LScheduleCurrent.LScheduleAdd(lConvertWorkItems, lConvertRelayTarget);
        LTraceLog.LTraceInfoRecord(
            $"Convert queued {lConvertAdded} job(s) at {lWorkPriority} from {lConvertSourcePaths.Count} listed file(s)");

        await LConvertDurationResolve(lConvertWorkItems).ConfigureAwait(true);
        return lConvertAdded;
    }

    private static async Task LConvertDurationResolve(IReadOnlyList<LWorkItem> lConvertWorkItems)
    {
        LWorkItem[] lConvertUnknown = lConvertWorkItems
            .Where(lWorkItem => lWorkItem.LWorkEnd <= TimeSpan.Zero)
            .ToArray();
        if (lConvertUnknown.Length == 0)
        {
            return;
        }

        var lConvertResolved = new TimeSpan[lConvertUnknown.Length];
        await Task.Run(() => Parallel.For(
            0,
            lConvertUnknown.Length,
            new ParallelOptions { MaxDegreeOfParallelism = LConvertParallelRead() },
            lConvertIndex => lConvertResolved[lConvertIndex] =
                Cadroue.Media.LSidecarStore.LSidecarDurationResolve(lConvertUnknown[lConvertIndex].LWorkSourcePath)))
            .ConfigureAwait(true);

        for (int lConvertIndex = 0; lConvertIndex < lConvertUnknown.Length; lConvertIndex++)
        {
            LSchedule.LScheduleCurrent.LScheduleDurationSet(
                lConvertUnknown[lConvertIndex].LWorkId, lConvertResolved[lConvertIndex]);
        }
    }

    internal static int LConvertParallelRead() => Math.Clamp(Environment.ProcessorCount, 1, 8);
}
