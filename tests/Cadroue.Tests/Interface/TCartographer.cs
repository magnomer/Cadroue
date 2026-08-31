using Cadroue.Core;
using Cadroue.ShellEngine;

namespace Cadroue.Tests;

internal sealed record TCartographerTabSpec(bool TCartographerFunnel, int TCartographerProcessingTarget, IReadOnlyList<int> TCartographerFunnelTargets)
{
    internal static TCartographerTabSpec TCartographerProcessCreate(int target) =>
        new(false, target, Array.Empty<int>());

    internal static TCartographerTabSpec TCartographerFunnelCreate(params int[] targets) =>
        new(true, -1, targets);
}

internal sealed record TCartographerStageView(int TCartographerTabIndex, Guid TCartographerStageId, Guid TCartographerNextStage, IReadOnlyList<Guid> TCartographerFunnelTargets);

internal sealed record TCartographerPlanView(Guid TCartographerEntryStage, IReadOnlyList<TCartographerStageView> TCartographerStages)
{
    internal TCartographerStageView TCartographerStageRead(int tabIndex) =>
        TCartographerStages.Single(stage => stage.TCartographerTabIndex == tabIndex);
}

internal static class TCartographer
{
    internal static Guid TCartographerFinish => LCartographer.LCartographerFinishTarget;

    internal static TCartographerPlanView TCartographerPlanCreate(IReadOnlyList<TCartographerTabSpec> specs, int entryIndex)
    {
        Guid[] ids = specs.Select(_ => Guid.NewGuid()).ToArray();
        var tabs = new List<LCartographerTab>();
        for (int index = 0; index < specs.Count; index++)
        {
            TCartographerTabSpec spec = specs[index];
            var layout = new LSceneTabRecord
            {
                LSceneFunnelRules = spec.TCartographerFunnelTargets
                    .Select(target => new LSceneFunnelRule { LSceneFunnelTarget = target })
                    .ToList()
            };
            tabs.Add(new LCartographerTab(
                ids[index],
                spec.TCartographerFunnel ? "Funnel" : "Convert",
                $"tab-{index}",
                new LPresetRecord(),
                layout,
                spec.TCartographerFunnel));
        }

        LCartographer.LCartographerTabsSource = () => tabs;
        try
        {
            for (int index = 0; index < specs.Count; index++)
            {
                if (!specs[index].TCartographerFunnel && specs[index].TCartographerProcessingTarget >= 0)
                {
                    LCartographer.LCartographerTargetSet(ids[index], ids[specs[index].TCartographerProcessingTarget]);
                }
            }

            LCartographerPlanRecord plan =
                LCartographer.LCartographerPlanCreate(Guid.NewGuid(), ids[entryIndex])
                ?? throw new InvalidOperationException("Production returned no relay plan.");

            var stages = plan.LCartographerStages
                .Select(stage => new TCartographerStageView(
                    Array.IndexOf(ids, stage.LCartographerOriginalTab),
                    stage.LCartographerStageId,
                    stage.LCartographerNextStage,
                    stage.LCartographerFunnelRules
                        .Select(rule => rule.LCartographerTargetStage)
                        .ToArray()))
                .ToArray();

            return new TCartographerPlanView(plan.LCartographerEntryStage, stages);
        }
        finally
        {
            LCartographer.LCartographerTabsSource = null;
            foreach (Guid id in ids)
            {
                LCartographer.LCartographerTabRemove(id);
            }
        }
    }
}
