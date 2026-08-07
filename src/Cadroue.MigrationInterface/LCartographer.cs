using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using Cadroue.Core;
using Cadroue.Infrastructure;

namespace Cadroue.MigrationInterface;

public sealed record LCartographerTab(
    Guid LCartographerTabId,
    string LCartographerLayoutKey,
    string LCartographerTitle,
    LPresetRecord LCartographerExport,
    LSceneTabRecord LCartographerLayout,
    bool LCartographerFunnel);

public sealed record LCartographerStagePlan(
    string LCartographerLayoutKey,
    LPresetRecord LCartographerExport,
    LSceneTabRecord LCartographerLayout,
    Guid LCartographerStageId,
    Guid LCartographerNextStage,
    Guid LCartographerBatch,
    IReadOnlyList<string> LCartographerPaths,
    bool LCartographerMerge);

public sealed record LCartographerDelivery(
    Func<Guid, string, Guid, bool> LCartographerTabAdd,
    Action<Guid, string, Guid> LCartographerTabPlace,
    Action<Guid, string, Guid> LCartographerTabTrack,
    Action<LWorkItem, bool> LCartographerSourceDrop,
    Func<LCartographerStagePlan, bool> LCartographerStageRun,
    Action<Guid, string, Guid> LCartographerTabArrive);

public static class LCartographer
{
    public static readonly Guid LCartographerFinishTarget = new("feed0000-0000-0000-0000-0000000ffff0");

    private static readonly HashSet<Guid> lCartographerDelivered = new();
    private static bool lCartographerDispatching;
    private static bool lCartographerDispatchPending;

    public static Func<IReadOnlyList<LCartographerTab>>? LCartographerTabsSource { get; set; }

    public static LCartographerPlanRecord? LCartographerPlanCreate(Guid lCartographerPlanId, Guid lCartographerTarget)
    {
        IReadOnlyList<LCartographerTab> lCartographerTabs =
            LCartographerTabsSource?.Invoke() ?? Array.Empty<LCartographerTab>();
        if (lCartographerTabs.Count == 0)
        {
            return null;
        }

        var lCartographerPlan = new LCartographerPlanRecord { LCartographerPlanId = lCartographerPlanId };
        var lCartographerStages = new Dictionary<Guid, LCartographerStageRecord>();

        Guid LCartographerStageCreate(Guid lCartographerTabId)
        {
            if (lCartographerTabId == Guid.Empty || lCartographerTabId == LCartographerFinishTarget)
            {
                return lCartographerTabId;
            }

            if (lCartographerStages.TryGetValue(lCartographerTabId, out LCartographerStageRecord? lCartographerExisting))
            {
                return lCartographerExisting.LCartographerStageId;
            }

            LCartographerTab? lCartographerTab = lCartographerTabs
                .FirstOrDefault(lCartographerItem => lCartographerItem.LCartographerTabId == lCartographerTabId);
            if (lCartographerTab is null)
            {
                return Guid.Empty;
            }

            var lCartographerStage = new LCartographerStageRecord
            {
                LCartographerStageId = Guid.NewGuid(),
                LCartographerOriginalTab = lCartographerTab.LCartographerTabId,
                LCartographerLayoutKey = lCartographerTab.LCartographerLayoutKey,
                LCartographerTitle = lCartographerTab.LCartographerTitle,
                LCartographerExport = lCartographerTab.LCartographerExport,
                LCartographerLayout = lCartographerTab.LCartographerLayout
            };
            lCartographerStages.Add(lCartographerTabId, lCartographerStage);

            if (lCartographerTab.LCartographerFunnel)
            {
                foreach (LSceneFunnelRule lCartographerRule in lCartographerStage.LCartographerLayout.LSceneFunnelRules)
                {
                    Guid lCartographerRuleTarget = lCartographerRule.LSceneFunnelTarget >= 0
                        && lCartographerRule.LSceneFunnelTarget < lCartographerTabs.Count
                        ? LCartographerStageCreate(lCartographerTabs[lCartographerRule.LSceneFunnelTarget].LCartographerTabId)
                        : Guid.Empty;
                    lCartographerStage.LCartographerFunnelRules.Add(new LCartographerFunnelRule
                    {
                        LCartographerRule = lCartographerRule.LSceneFunnelClone(),
                        LCartographerTargetStage = lCartographerRuleTarget
                    });
                }
            }
            else
            {
                lCartographerStage.LCartographerNextStage = LCartographerStageCreate(LCartographerTargetRead(lCartographerTabId));
            }

            return lCartographerStage.LCartographerStageId;
        }

        lCartographerPlan.LCartographerEntryStage = LCartographerStageCreate(lCartographerTarget);
        lCartographerPlan.LCartographerStages = lCartographerStages.Values.ToList();
        return lCartographerPlan.LCartographerEntryStage == Guid.Empty ? null : lCartographerPlan;
    }

    public static LCartographerPlanRecord LCartographerPlanCopy(LCartographerPlanRecord lCartographerTemplate, Guid lCartographerPlanId)
    {
        string lCartographerJson = JsonSerializer.Serialize(lCartographerTemplate);
        LCartographerPlanRecord lCartographerCopy = JsonSerializer.Deserialize<LCartographerPlanRecord>(lCartographerJson)!;
        lCartographerCopy.LCartographerPlanId = lCartographerPlanId;
        lCartographerCopy.LCartographerCreated = DateTimeOffset.Now;
        lCartographerCopy.LCartographerDeliveredWork.Clear();
        foreach (LCartographerStageRecord lCartographerStage in lCartographerCopy.LCartographerStages)
        {
            lCartographerStage.LCartographerPendingInputs.Clear();
        }
        return lCartographerCopy;
    }

    public static Guid LCartographerRouteRead(LCartographerStageRecord lCartographerStage, string lCartographerPath)
    {
        string lCartographerName = Path.GetFileName(lCartographerPath);
        foreach (LCartographerFunnelRule lCartographerRule in lCartographerStage.LCartographerFunnelRules)
        {
            if (LCartographerRuleMatch(lCartographerRule.LCartographerRule, lCartographerName))
            {
                return lCartographerRule.LCartographerTargetStage;
            }
        }

        return Guid.Empty;
    }

    private static bool LCartographerRuleMatch(LSceneFunnelRule lCartographerRule, string lCartographerName)
    {
        if (lCartographerRule.LSceneFunnelType == (int)LSceneFunnelForm.LSceneFunnelFormRegex)
        {
            if (string.IsNullOrWhiteSpace(lCartographerRule.LSceneFunnelRegex)) return false;
            try
            {
                string lCartographerSubject = lCartographerRule.LSceneFunnelWhole
                    ? lCartographerName
                    : Path.GetFileNameWithoutExtension(lCartographerName);
                return Regex.IsMatch(lCartographerSubject, lCartographerRule.LSceneFunnelRegex, RegexOptions.IgnoreCase);
            }
            catch (ArgumentException) { return false; }
        }

        var lCartographerParts = new[]
        {
            (lCartographerRule.LSceneFunnelContains, 0),
            (lCartographerRule.LSceneFunnelStart, 1),
            (lCartographerRule.LSceneFunnelEnd, 2),
            (lCartographerRule.LSceneFunnelExtension, 3)
        };
        bool lCartographerHasResult = false;
        bool lCartographerResult = false;
        foreach ((LSceneFunnelMatch lCartographerMatch, int lCartographerKind) in lCartographerParts)
        {
            if (string.IsNullOrWhiteSpace(lCartographerMatch.LSceneFunnelText)) continue;
            StringComparison lCartographerComparison = lCartographerMatch.LSceneFunnelCase
                ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
            bool lCartographerCurrent = lCartographerKind switch
            {
                0 => lCartographerName.Contains(lCartographerMatch.LSceneFunnelText, lCartographerComparison),
                1 => lCartographerName.StartsWith(lCartographerMatch.LSceneFunnelText, lCartographerComparison),
                2 => lCartographerName.EndsWith(lCartographerMatch.LSceneFunnelText, lCartographerComparison),
                _ => string.Equals(Path.GetExtension(lCartographerName).TrimStart('.'),
                    lCartographerMatch.LSceneFunnelText.TrimStart('.'), lCartographerComparison)
            };
            lCartographerResult = !lCartographerHasResult
                ? lCartographerCurrent
                : lCartographerMatch.LSceneFunnelJoin ? lCartographerResult && lCartographerCurrent : lCartographerResult || lCartographerCurrent;
            lCartographerHasResult = true;
        }

        return lCartographerHasResult && lCartographerResult;
    }

    private static readonly Dictionary<Guid, Guid> lCartographerTargets = new();

    public static Guid LCartographerTargetRead(Guid lCartographerSourceTab) =>
        lCartographerTargets.TryGetValue(lCartographerSourceTab, out Guid lCartographerTarget) ? lCartographerTarget : Guid.Empty;

    public static void LCartographerTargetSet(Guid lCartographerSourceTab, Guid lCartographerTarget)
    {
        if (lCartographerTarget == Guid.Empty)
        {
            lCartographerTargets.Remove(lCartographerSourceTab);
            return;
        }

        if (lCartographerTarget == lCartographerSourceTab)
        {
            LTraceLog.LTraceWarningRecord("Relay target refused: a tab cannot relay into itself");
            return;
        }

        lCartographerTargets[lCartographerSourceTab] = lCartographerTarget;
    }

    public static void LCartographerTabRemove(Guid lCartographerTabId)
    {
        lCartographerTargets.Remove(lCartographerTabId);
        foreach (Guid lCartographerSourceTab in lCartographerTargets
            .Where(lCartographerEntry => lCartographerEntry.Value == lCartographerTabId)
            .Select(lCartographerEntry => lCartographerEntry.Key)
            .ToArray())
        {
            lCartographerTargets.Remove(lCartographerSourceTab);
        }
    }

    public static bool LCartographerOwnershipCheck(LWorkItem lCartographerItem) =>
        LCartographerPlanStore.LCartographerPlanRead(lCartographerItem.LWorkBatchId, out LCartographerPlanRecord lCartographerPlan)
        && lCartographerPlan.LCartographerStages.Any(
            lCartographerStage => lCartographerStage.LCartographerStageId == lCartographerItem.LWorkRelayTarget);

    public static bool LCartographerDeliveredCheck(Guid lCartographerWorkId) =>
        lCartographerDelivered.Contains(lCartographerWorkId);

    public static void LCartographerDeliveredRemove(IReadOnlySet<Guid> lCartographerLiveWork) =>
        lCartographerDelivered.RemoveWhere(lCartographerWorkId => !lCartographerLiveWork.Contains(lCartographerWorkId));

    public static void LCartographerDispatch(IReadOnlyList<LWorkItem> lCartographerSchedule)
    {
        if (lCartographerDispatching)
        {
            lCartographerDispatchPending = true;
            return;
        }

        lCartographerDispatching = true;
        try
        {
            do
            {
                lCartographerDispatchPending = false;
                foreach (LWorkItem lCartographerItem in lCartographerSchedule.ToArray())
                {
                    if (!LCartographerDeliverableCheck(lCartographerItem))
                    {
                        continue;
                    }

                    lCartographerDelivered.Add(lCartographerItem.LWorkId);
                    try
                    {
                        if (!LCartographerDeliver(lCartographerItem))
                        {
                            lCartographerDelivered.Remove(lCartographerItem.LWorkId);
                        }
                    }
                    catch
                    {
                        lCartographerDelivered.Remove(lCartographerItem.LWorkId);
                        throw;
                    }
                }
            }
            while (lCartographerDispatchPending);
        }
        finally
        {
            lCartographerDispatching = false;
        }

        LSeal.LSealSweep();
    }

    public static bool LCartographerDeliverableCheck(LWorkItem lCartographerItem)
    {
        if (lCartographerItem.LWorkStateCurrent != LWorkState.LWorkStateDone
            || lCartographerItem.LWorkRelayTarget == Guid.Empty
            || lCartographerDelivered.Contains(lCartographerItem.LWorkId))
        {
            return false;
        }

        bool lCartographerPlanOwned = LCartographerOwnershipCheck(lCartographerItem);
        return lCartographerItem.LWorkOwnerProcess == Environment.ProcessId
            || lCartographerPlanOwned && !LSentinel.LSentinelOwnerCheck(
                lCartographerItem.LWorkOwnerProcess, lCartographerItem.LWorkOwnerStamp);
    }

    public static LCartographerDelivery? LCartographerDeliverySeam { get; set; }

    public static Func<IReadOnlyList<LWorkItem>>? LCartographerScheduleSource { get; set; }

    private static IReadOnlyList<LWorkItem> LCartographerScheduleRead() =>
        LCartographerScheduleSource?.Invoke() ?? Array.Empty<LWorkItem>();

    public static bool LCartographerDeliver(LWorkItem lCartographerItem)
    {
        if (LCartographerDeliverySeam is not { } lCartographerSeam)
        {
            return false;
        }

        if (lCartographerItem.LWorkRelayTarget == LCartographerFinishTarget)
        {
            LTraceLog.LTraceInfoRecord(
                $"Relay finished '{lCartographerItem.LWorkOutputName}': removed at source, delivered to no tab");
            lCartographerSeam.LCartographerSourceDrop(lCartographerItem, true);
            return true;
        }

        if (string.IsNullOrWhiteSpace(lCartographerItem.LWorkOutputPath) || !File.Exists(lCartographerItem.LWorkOutputPath))
        {
            LTraceLog.LTraceWarningRecord($"Relay skipped '{lCartographerItem.LWorkOutputName}': the output file is missing");
            return false;
        }

        if (LCartographerPlanStore.LCartographerPlanRead(lCartographerItem.LWorkBatchId, out LCartographerPlanRecord lCartographerPlan)
            && lCartographerPlan.LCartographerStages.FirstOrDefault(
                lCartographerStage => lCartographerStage.LCartographerStageId == lCartographerItem.LWorkRelayTarget) is { } lCartographerStage)
        {
            if (!LCartographerStageDeliver(lCartographerItem, lCartographerPlan, lCartographerStage, lCartographerSeam))
            {
                return false;
            }

            lCartographerSeam.LCartographerSourceDrop(lCartographerItem, false);
            return true;
        }

        if (!lCartographerSeam.LCartographerTabAdd(
            lCartographerItem.LWorkRelayTarget, lCartographerItem.LWorkOutputPath, lCartographerItem.LWorkBatchId))
        {
            return false;
        }

        lCartographerSeam.LCartographerSourceDrop(lCartographerItem, false);
        lCartographerSeam.LCartographerTabArrive(
            lCartographerItem.LWorkRelayTarget, lCartographerItem.LWorkOutputPath, lCartographerItem.LWorkBatchId);
        return true;
    }

    private static bool LCartographerStageDeliver(
        LWorkItem lCartographerItem,
        LCartographerPlanRecord lCartographerPlan,
        LCartographerStageRecord lCartographerStage,
        LCartographerDelivery lCartographerSeam)
    {
        if (lCartographerPlan.LCartographerDeliveredWork.Contains(lCartographerItem.LWorkId))
        {
            return true;
        }

        LCartographerStageArrive(
            lCartographerPlan, lCartographerStage, lCartographerItem.LWorkOutputPath,
            lCartographerItem.LWorkRelaySource, lCartographerItem.LWorkBatchId, lCartographerSeam);
        lCartographerPlan.LCartographerDeliveredWork.Add(lCartographerItem.LWorkId);
        return LCartographerPlanStore.LCartographerPlanSave(lCartographerPlan);
    }

    private static void LCartographerStageArrive(
        LCartographerPlanRecord lCartographerPlan,
        LCartographerStageRecord lCartographerStage,
        string lCartographerPath,
        Guid lCartographerSourceStage,
        Guid lCartographerBatch,
        LCartographerDelivery lCartographerSeam)
    {
        lCartographerStage.LCartographerPendingInputs.Add(new LCartographerInputRecord
        {
            LCartographerPath = lCartographerPath,
            LCartographerSourceStage = lCartographerSourceStage
        });
        LCartographerPlanStore.LCartographerPlanSave(lCartographerPlan);

        if (!lCartographerStage.LCartographerLayout.LSceneAutoRelay)
        {
            lCartographerSeam.LCartographerTabPlace(lCartographerStage.LCartographerOriginalTab, lCartographerPath, lCartographerBatch);
            LTraceLog.LTraceInfoRecord(
                $"Relay plan {lCartographerPlan.LCartographerPlanId:N} paused at stage '{lCartographerStage.LCartographerTitle}'");
            return;
        }

        if (string.Equals(lCartographerStage.LCartographerLayoutKey, "Funnel", StringComparison.Ordinal))
        {
            Guid lCartographerTargetId = LCartographerRouteRead(lCartographerStage, lCartographerPath);
            if (lCartographerPlan.LCartographerStages.FirstOrDefault(
                lCartographerCandidate => lCartographerCandidate.LCartographerStageId == lCartographerTargetId) is { } lCartographerTarget)
            {
                LCartographerStageArrive(
                    lCartographerPlan, lCartographerTarget, lCartographerPath,
                    lCartographerStage.LCartographerStageId, lCartographerBatch, lCartographerSeam);
            }
            lCartographerStage.LCartographerPendingInputs.Clear();
            return;
        }

        lCartographerSeam.LCartographerTabTrack(lCartographerStage.LCartographerOriginalTab, lCartographerPath, lCartographerBatch);

        bool lCartographerMerge = string.Equals(lCartographerStage.LCartographerLayoutKey, "Merge", StringComparison.Ordinal);
        if (lCartographerMerge
            && LCartographerMergeCheck(
                lCartographerPlan, lCartographerStage, lCartographerBatch, LCartographerScheduleRead()))
        {
            return;
        }

        string[] lCartographerPaths = lCartographerStage.LCartographerPendingInputs
            .Select(lCartographerInput => lCartographerInput.LCartographerPath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        LCartographerStageSet(lCartographerStage.LCartographerStageId, lCartographerStage.LCartographerTitle);
        bool lCartographerRan = lCartographerSeam.LCartographerStageRun(new LCartographerStagePlan(
            lCartographerStage.LCartographerLayoutKey,
            lCartographerStage.LCartographerExport,
            lCartographerStage.LCartographerLayout.LSceneTabClone(),
            lCartographerStage.LCartographerStageId,
            lCartographerStage.LCartographerNextStage,
            lCartographerBatch,
            lCartographerPaths,
            lCartographerMerge));
        if (lCartographerRan)
        {
            lCartographerStage.LCartographerPendingInputs.Clear();
            LCartographerPlanStore.LCartographerPlanSave(lCartographerPlan);
        }
    }

    public static bool LCartographerMergeCheck(
        LCartographerPlanRecord lCartographerPlan,
        LCartographerStageRecord lCartographerMerge,
        Guid lCartographerBatch,
        IReadOnlyList<LWorkItem> lCartographerSchedule)
    {
        foreach (LWorkItem lCartographerItem in lCartographerSchedule)
        {
            if (lCartographerItem.LWorkBatchId != lCartographerBatch
                || lCartographerItem.LWorkStateCurrent is LWorkState.LWorkStateFailed or LWorkState.LWorkStateCancelled)
            {
                continue;
            }

            if (lCartographerItem.LWorkStateCurrent == LWorkState.LWorkStateDone
                && (lCartographerPlan.LCartographerDeliveredWork.Contains(lCartographerItem.LWorkId)
                    || lCartographerMerge.LCartographerPendingInputs.Any(lCartographerInput => string.Equals(
                        lCartographerInput.LCartographerPath, lCartographerItem.LWorkOutputPath, StringComparison.OrdinalIgnoreCase))))
            {
                continue;
            }

            if (LCartographerReachCheck(lCartographerPlan, lCartographerItem.LWorkRelayTarget, lCartographerMerge.LCartographerStageId))
            {
                return true;
            }
        }

        return false;
    }

    private static bool LCartographerReachCheck(LCartographerPlanRecord lCartographerPlan, Guid lCartographerFrom, Guid lCartographerTarget)
    {
        var lCartographerSeen = new HashSet<Guid>();
        var lCartographerPending = new Queue<Guid>();
        lCartographerPending.Enqueue(lCartographerFrom);
        while (lCartographerPending.Count > 0)
        {
            Guid lCartographerCurrent = lCartographerPending.Dequeue();
            if (lCartographerCurrent == lCartographerTarget) return true;
            if (!lCartographerSeen.Add(lCartographerCurrent)) continue;
            if (lCartographerPlan.LCartographerStages.FirstOrDefault(
                lCartographerStage => lCartographerStage.LCartographerStageId == lCartographerCurrent) is not { } lCartographerStage) continue;
            if (lCartographerStage.LCartographerNextStage != Guid.Empty) lCartographerPending.Enqueue(lCartographerStage.LCartographerNextStage);
            foreach (LCartographerFunnelRule lCartographerRule in lCartographerStage.LCartographerFunnelRules)
            {
                if (lCartographerRule.LCartographerTargetStage != Guid.Empty) lCartographerPending.Enqueue(lCartographerRule.LCartographerTargetStage);
            }
        }

        return false;
    }

    public static void LCartographerRelaySet(
        IReadOnlyList<LWorkItem> lCartographerItems,
        Guid lCartographerTarget,
        Guid lCartographerSource,
        LCartographerPlanRecord? lCartographerPreparedPlan)
    {
        foreach (IGrouping<Guid, LWorkItem> lCartographerBatch in lCartographerItems.GroupBy(lCartographerItem => lCartographerItem.LWorkBatchId))
        {
            if (LCartographerPlanStore.LCartographerPlanRead(lCartographerBatch.Key, out LCartographerPlanRecord lCartographerExisting))
            {
                LCartographerStageRecord? lCartographerSourceStage = lCartographerExisting.LCartographerStages.FirstOrDefault(
                    lCartographerStage => lCartographerStage.LCartographerStageId == lCartographerSource
                        || lCartographerStage.LCartographerOriginalTab == lCartographerSource);
                LCartographerStageRecord? lCartographerTargetStage = lCartographerExisting.LCartographerStages.FirstOrDefault(
                    lCartographerStage => lCartographerStage.LCartographerStageId == lCartographerTarget
                        || lCartographerStage.LCartographerOriginalTab == lCartographerTarget);
                Guid lCartographerStableSource = lCartographerSourceStage?.LCartographerStageId ?? lCartographerSource;
                Guid lCartographerStableTarget = lCartographerSourceStage?.LCartographerNextStage
                    ?? lCartographerTargetStage?.LCartographerStageId
                    ?? lCartographerTarget;
                foreach (LWorkItem lCartographerItem in lCartographerBatch)
                {
                    lCartographerItem.LWorkRelayTarget = lCartographerStableTarget;
                    lCartographerItem.LWorkRelaySource = lCartographerStableSource;
                }
                continue;
            }

            if (lCartographerTarget == Guid.Empty || lCartographerTarget == LCartographerFinishTarget)
            {
                foreach (LWorkItem lCartographerItem in lCartographerBatch)
                {
                    lCartographerItem.LWorkRelayTarget = lCartographerTarget;
                    lCartographerItem.LWorkRelaySource = lCartographerSource;
                }
                continue;
            }

            LCartographerPlanRecord? lCartographerPlan = lCartographerPreparedPlan is null
                ? LCartographerPlanCreate(lCartographerBatch.Key, lCartographerTarget)
                : LCartographerPlanCopy(lCartographerPreparedPlan, lCartographerBatch.Key);
            if (lCartographerPlan is null || !LCartographerPlanStore.LCartographerPlanSave(lCartographerPlan))
            {
                foreach (LWorkItem lCartographerItem in lCartographerBatch)
                {
                    lCartographerItem.LWorkRelayTarget = lCartographerTarget;
                    lCartographerItem.LWorkRelaySource = lCartographerSource;
                }
                continue;
            }

            foreach (LWorkItem lCartographerItem in lCartographerBatch)
            {
                lCartographerItem.LWorkRelayTarget = lCartographerPlan.LCartographerEntryStage;
                lCartographerItem.LWorkRelaySource = lCartographerSource;
            }
            LTraceLog.LTraceInfoRecord(
                $"Relay plan {lCartographerPlan.LCartographerPlanId:N} captured {lCartographerPlan.LCartographerStages.Count} stable stage(s)");
        }
    }

    public static Func<Guid, string>? LCartographerTitleSource { get; set; }

    private static readonly Dictionary<Guid, string> lCartographerStageTitles = new();

    public static string LCartographerStageRead(Guid lCartographerStageId) =>
        lCartographerStageTitles.TryGetValue(lCartographerStageId, out string? lCartographerTitle)
            ? lCartographerTitle
            : string.Empty;

    public static void LCartographerStageSet(Guid lCartographerStageId, string lCartographerTitle) =>
        lCartographerStageTitles[lCartographerStageId] = lCartographerTitle;

    public static string LCartographerTitleRead(LWorkItem lCartographerItem)
    {
        string lCartographerTitle = LCartographerTitleSource?.Invoke(lCartographerItem.LWorkRelaySource) ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(lCartographerTitle))
        {
            return lCartographerTitle;
        }

        if (LCartographerPlanStore.LCartographerPlanRead(lCartographerItem.LWorkBatchId, out LCartographerPlanRecord lCartographerPlan)
            && lCartographerPlan.LCartographerStages.FirstOrDefault(
                lCartographerStage => lCartographerStage.LCartographerStageId == lCartographerItem.LWorkRelaySource)
                is { } lCartographerSourceStage)
        {
            lCartographerStageTitles[lCartographerSourceStage.LCartographerStageId] = lCartographerSourceStage.LCartographerTitle;
            return lCartographerSourceStage.LCartographerTitle;
        }

        return lCartographerItem.LWorkTab;
    }
}
