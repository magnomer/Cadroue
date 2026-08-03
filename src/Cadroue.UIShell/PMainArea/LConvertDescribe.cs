using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.UIShell.PPanels;

namespace Cadroue.UIShell.PMainArea;

public static partial class LConvert
{
    public static async Task<int> LConvertDescribe(
        LWorkPriority lWorkPriority,
        IReadOnlyList<LWorkSource> lConvertSources,
        LPreset lExportSpecificState,
        Guid lConvertRelayTarget = default,
        Guid lConvertRelaySource = default)
    {
        LEncoding lConvertOutput = lExportSpecificState.LPresetOutputCreate();
        string[] lConvertSourcePaths = lConvertSources
            .Select(lConvertSource => lConvertSource.LWorkSourcePath)
            .ToArray();
        var lConvertRelays = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        foreach (LWorkSource lConvertSource in lConvertSources)
        {
            lConvertRelays[lConvertSource.LWorkSourcePath] = lConvertSource.LWorkSourceBatch;
        }

        LConvertWorkDescription lConvertWorkDescription =
            new(lConvertSourcePaths, lConvertOutput, null, lConvertRelays);

        string lConvertTab = PControlBar.LTabset.LTabsetTitleRead(lConvertRelaySource);
        IReadOnlyList<LWorkItem> lConvertWorkItems =
            Cadroue.Application.LConvert.LConvertItemsCreate(
                lWorkPriority,
                lConvertWorkDescription,
                lConvertTab,
                lConvertMessage => LTraceLog.LTraceErrorRecord(lConvertMessage),
                Cadroue.Media.LSidecarStore.LSidecarDurationRead);

        int lConvertAdded = LCourier.LCourierScheduleAdd(lConvertWorkItems, lConvertRelayTarget, lConvertRelaySource);
        LTraceLog.LTraceInfoRecord(
            $"Convert queued {lConvertAdded} job(s) at {lWorkPriority} from {lConvertSourcePaths.Length} listed file(s)");

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
            PProgram.LScheduleCurrent.LScheduleDurationSet(
                lConvertUnknown[lConvertIndex].LWorkId, lConvertResolved[lConvertIndex]);
        }
    }

    internal static int LConvertParallelRead() => Math.Clamp(Environment.ProcessorCount, 1, 8);
}
