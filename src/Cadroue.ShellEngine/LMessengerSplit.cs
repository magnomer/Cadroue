using Cadroue.Core;

namespace Cadroue.ShellEngine;

public static partial class LMessenger
{
    public static int LMessengerSplitDescribe(
        LWorkPriority lMessengerPriority,
        string? lMessengerSourcePath,
        IReadOnlyList<LSplitSectionDescription> lMessengerSections,
        LEncoding? lMessengerEncoding,
        Guid lMessengerRelayTarget,
        Guid lMessengerRelaySource,
        Guid lMessengerBatchId)
    {
        if (lMessengerEncoding is not { } lMessengerOutput)
        {
            return 0;
        }

        LSplitWorkDescription lMessengerDescription = new(
            lMessengerSourcePath, lMessengerSections, lMessengerOutput);
        string lMessengerTab = LMessengerTitleRead(lMessengerRelaySource);
        IReadOnlyList<LWorkItem> lMessengerItems = Cadroue.Application.LSplit.LSplitItemsCreate(
            lMessengerPriority, lMessengerDescription, lMessengerTab,
            lMessengerMessage => LTraceEngine.LTraceInfoSeam(lMessengerMessage),
            lMessengerMessage => LTraceEngine.LTraceErrorSeam(lMessengerMessage, null),
            lMessengerBatchId);
        if (lMessengerItems.Count == 0)
        {
            return 0;
        }

        LCartographerPlanDocument? lMessengerPlan = LCartographer.LCartographerPlanPrepare(lMessengerRelayTarget);
        int lMessengerAdded = LMessengerDispatch(
            lMessengerItems, lMessengerRelayTarget, lMessengerRelaySource, lMessengerPlan);
        LTraceEngine.LTraceInfoSeam(
            $"Split queued {lMessengerAdded} of {lMessengerItems.Count} job(s) at {lMessengerPriority} " +
            $"from '{System.IO.Path.GetFileName(lMessengerSourcePath)}'");
        LMessengerSourceResolve(lMessengerItems);
        return lMessengerAdded;
    }
    public static async Task<int> LMessengerSplitDescribe(
        LWorkPriority lMessengerPriority,
        IReadOnlyList<LWorkSource> lMessengerSources,
        LEncoding? lMessengerEncoding,
        Guid lMessengerRelayTarget = default,
        Guid lMessengerRelaySource = default)
    {
        try
        {
            var lMessengerRelays = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
            foreach (LWorkSource lMessengerSource in lMessengerSources)
            {
                lMessengerRelays[lMessengerSource.LWorkSourcePath] = lMessengerSource.LWorkSourceBatch;
            }
            string[] lMessengerSourcePaths = lMessengerSources
                .Select(lMessengerSource => lMessengerSource.LWorkSourcePath)
                .ToArray();

            IReadOnlyList<LSplitPlanDescription> lMessengerPlans =
                await LMessengerSplitCreate(lMessengerSourcePaths).ConfigureAwait(false);

            int lMessengerAdded = 0;
            LMessengerDefer(() =>
            {
                foreach (LSplitPlanDescription lMessengerPlan in lMessengerPlans)
                {
                    lMessengerRelays.TryGetValue(lMessengerPlan.LSplitSourcePath, out Guid lMessengerBatch);
                    lMessengerAdded += LMessengerSplitDescribe(
                        lMessengerPriority, lMessengerPlan.LSplitSourcePath, lMessengerPlan.LSplitPlanSections,
                        lMessengerEncoding, lMessengerRelayTarget, lMessengerRelaySource, lMessengerBatch);
                }
            });
            return lMessengerAdded;
        }
        catch (Exception lMessengerException)
        {
            LTraceEngine.LTraceErrorSeam("Split could not be queued", lMessengerException);
            return 0;
        }
    }

    private static async Task<IReadOnlyList<LSplitPlanDescription>> LMessengerSplitCreate(
        IReadOnlyList<string> lMessengerSourcePaths)
    {
        var lMessengerPlans = new List<LSplitPlanDescription>();
        foreach (string lMessengerSourcePath in lMessengerSourcePaths)
        {
            IReadOnlyList<LSplitSectionDescription> lMessengerSections =
                await LMessengerSplitRead(lMessengerSourcePath).ConfigureAwait(false);
            if (lMessengerSections.Count > 0)
            {
                lMessengerPlans.Add(new LSplitPlanDescription(lMessengerSourcePath, lMessengerSections));
            }
        }
        return lMessengerPlans;
    }

    internal static async Task<IReadOnlyList<LSplitSectionDescription>> LMessengerSplitRead(
        string lMessengerSourcePath)
    {
        try
        {
            if (await Task.Run(() => Cadroue.Application.LLibrarian.LLibrarianLoad(lMessengerSourcePath))
                    .ConfigureAwait(false) is not { } lMessengerSidecar)
            {
                return Array.Empty<LSplitSectionDescription>();
            }

            TimeSpan lMessengerDuration = await Cadroue.Application.LLibrarian
                .LLibrarianDurationResolve(lMessengerSourcePath).ConfigureAwait(false);
            if (lMessengerDuration <= TimeSpan.Zero)
            {
                LTraceEngine.LTraceWarningSeam(
                    $"Split plan for '{lMessengerSourcePath}' has no known duration; " +
                    "sections are bounded by their own ends");
                lMessengerDuration = TimeSpan.MaxValue;
            }

            IReadOnlyList<LPiece> lMessengerPieces = LPiecePlan.LPieceValidSelect(
                lMessengerSidecar.LSidecarSections.Select(LPiece.LPieceCreate).ToArray(),
                lMessengerDuration);
            if (lMessengerPieces.Count < lMessengerSidecar.LSidecarSections.Count)
            {
                LTraceEngine.LTraceWarningSeam(
                    $"Split plan for '{lMessengerSourcePath}' dropped "
                    + $"{lMessengerSidecar.LSidecarSections.Count - lMessengerPieces.Count} invalid section(s)");
            }

            return lMessengerPieces.Select(lMessengerPiece => lMessengerPiece.LPieceDescribe()).ToArray();
        }
        catch (Exception lMessengerException)
        {
            LTraceEngine.LTraceErrorSeam(
                $"Split plan could not be read for '{lMessengerSourcePath}'",
                lMessengerException);
            return Array.Empty<LSplitSectionDescription>();
        }
    }
}
