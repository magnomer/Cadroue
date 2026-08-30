using System.Text.Json;
using Cadroue.Core;

namespace Cadroue.ShellEngine;

public static partial class LCartographer
{
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
}
