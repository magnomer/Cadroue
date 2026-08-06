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

public static class LCartographer
{
    public static readonly Guid LCartographerFinishTarget = new("feed0000-0000-0000-0000-0000000ffff0");

    public static Func<IReadOnlyList<LCartographerTab>>? LCartographerTabsSource { get; set; }

    public static LRelayPlanRecord? LCartographerPlanCreate(Guid lCartographerPlanId, Guid lCartographerTarget)
    {
        IReadOnlyList<LCartographerTab> lCartographerTabs =
            LCartographerTabsSource?.Invoke() ?? Array.Empty<LCartographerTab>();
        if (lCartographerTabs.Count == 0)
        {
            return null;
        }

        var lCartographerPlan = new LRelayPlanRecord { LRelayPlanId = lCartographerPlanId };
        var lCartographerStages = new Dictionary<Guid, LRelayStageRecord>();

        Guid LCartographerStageCreate(Guid lCartographerTabId)
        {
            if (lCartographerTabId == Guid.Empty || lCartographerTabId == LCartographerFinishTarget)
            {
                return lCartographerTabId;
            }

            if (lCartographerStages.TryGetValue(lCartographerTabId, out LRelayStageRecord? lCartographerExisting))
            {
                return lCartographerExisting.LRelayStageId;
            }

            LCartographerTab? lCartographerTab = lCartographerTabs
                .FirstOrDefault(lCartographerItem => lCartographerItem.LCartographerTabId == lCartographerTabId);
            if (lCartographerTab is null)
            {
                return Guid.Empty;
            }

            var lCartographerStage = new LRelayStageRecord
            {
                LRelayStageId = Guid.NewGuid(),
                LRelayOriginalTab = lCartographerTab.LCartographerTabId,
                LRelayLayoutKey = lCartographerTab.LCartographerLayoutKey,
                LRelayTitle = lCartographerTab.LCartographerTitle,
                LRelayExport = lCartographerTab.LCartographerExport,
                LRelayLayout = lCartographerTab.LCartographerLayout
            };
            lCartographerStages.Add(lCartographerTabId, lCartographerStage);

            if (lCartographerTab.LCartographerFunnel)
            {
                foreach (LSceneFunnelRule lCartographerRule in lCartographerStage.LRelayLayout.LSceneFunnelRules)
                {
                    Guid lCartographerRuleTarget = lCartographerRule.LSceneFunnelTarget >= 0
                        && lCartographerRule.LSceneFunnelTarget < lCartographerTabs.Count
                        ? LCartographerStageCreate(lCartographerTabs[lCartographerRule.LSceneFunnelTarget].LCartographerTabId)
                        : Guid.Empty;
                    lCartographerStage.LRelayFunnelRules.Add(new LFunnelRule
                    {
                        LRelayRule = lCartographerRule.LSceneFunnelClone(),
                        LRelayTargetStage = lCartographerRuleTarget
                    });
                }
            }
            else
            {
                lCartographerStage.LRelayNextStage = LCartographerStageCreate(LCartographerTargetRead(lCartographerTabId));
            }

            return lCartographerStage.LRelayStageId;
        }

        lCartographerPlan.LRelayEntryStage = LCartographerStageCreate(lCartographerTarget);
        lCartographerPlan.LRelayStages = lCartographerStages.Values.ToList();
        return lCartographerPlan.LRelayEntryStage == Guid.Empty ? null : lCartographerPlan;
    }

    public static LRelayPlanRecord LCartographerPlanCopy(LRelayPlanRecord lCartographerTemplate, Guid lCartographerPlanId)
    {
        string lCartographerJson = JsonSerializer.Serialize(lCartographerTemplate);
        LRelayPlanRecord lCartographerCopy = JsonSerializer.Deserialize<LRelayPlanRecord>(lCartographerJson)!;
        lCartographerCopy.LRelayPlanId = lCartographerPlanId;
        lCartographerCopy.LRelayCreated = DateTimeOffset.Now;
        lCartographerCopy.LRelayDeliveredWork.Clear();
        foreach (LRelayStageRecord lCartographerStage in lCartographerCopy.LRelayStages)
        {
            lCartographerStage.LRelayPendingInputs.Clear();
        }
        return lCartographerCopy;
    }

    public static Guid LCartographerRouteRead(LRelayStageRecord lCartographerStage, string lCartographerPath)
    {
        string lCartographerName = Path.GetFileName(lCartographerPath);
        foreach (LFunnelRule lCartographerRule in lCartographerStage.LRelayFunnelRules)
        {
            if (LCartographerRuleMatch(lCartographerRule.LRelayRule, lCartographerName))
            {
                return lCartographerRule.LRelayTargetStage;
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
        LRelayPlanStore.LRelayPlanRead(lCartographerItem.LWorkBatchId, out LRelayPlanRecord lCartographerPlan)
        && lCartographerPlan.LRelayStages.Any(
            lCartographerStage => lCartographerStage.LRelayStageId == lCartographerItem.LWorkRelayTarget);

    public static bool LCartographerMergeCheck(
        LRelayPlanRecord lCartographerPlan,
        LRelayStageRecord lCartographerMerge,
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
                && (lCartographerPlan.LRelayDeliveredWork.Contains(lCartographerItem.LWorkId)
                    || lCartographerMerge.LRelayPendingInputs.Any(lCartographerInput => string.Equals(
                        lCartographerInput.LRelayPath, lCartographerItem.LWorkOutputPath, StringComparison.OrdinalIgnoreCase))))
            {
                continue;
            }

            if (LCartographerReachCheck(lCartographerPlan, lCartographerItem.LWorkRelayTarget, lCartographerMerge.LRelayStageId))
            {
                return true;
            }
        }

        return false;
    }

    private static bool LCartographerReachCheck(LRelayPlanRecord lCartographerPlan, Guid lCartographerFrom, Guid lCartographerTarget)
    {
        var lCartographerSeen = new HashSet<Guid>();
        var lCartographerPending = new Queue<Guid>();
        lCartographerPending.Enqueue(lCartographerFrom);
        while (lCartographerPending.Count > 0)
        {
            Guid lCartographerCurrent = lCartographerPending.Dequeue();
            if (lCartographerCurrent == lCartographerTarget) return true;
            if (!lCartographerSeen.Add(lCartographerCurrent)) continue;
            if (lCartographerPlan.LRelayStages.FirstOrDefault(
                lCartographerStage => lCartographerStage.LRelayStageId == lCartographerCurrent) is not { } lCartographerStage) continue;
            if (lCartographerStage.LRelayNextStage != Guid.Empty) lCartographerPending.Enqueue(lCartographerStage.LRelayNextStage);
            foreach (LFunnelRule lCartographerRule in lCartographerStage.LRelayFunnelRules)
            {
                if (lCartographerRule.LRelayTargetStage != Guid.Empty) lCartographerPending.Enqueue(lCartographerRule.LRelayTargetStage);
            }
        }

        return false;
    }
}
