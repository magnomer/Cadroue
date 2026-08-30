using Cadroue.Core;
using Cadroue.ShellEngine;

namespace Cadroue.Tests;

internal sealed record TCartographerTabSpec(bool Funnel, int ProcessingTarget, IReadOnlyList<int> FunnelTargets)
{
    internal static TCartographerTabSpec Processing(int target) =>
        new(false, target, Array.Empty<int>());

    internal static TCartographerTabSpec FunnelInto(params int[] targets) =>
        new(true, -1, targets);
}

internal sealed record TCartographerStageView(int TabIndex, Guid StageId, Guid NextStage, IReadOnlyList<Guid> FunnelTargets);

internal sealed record TCartographerPlanView(Guid EntryStage, IReadOnlyList<TCartographerStageView> Stages)
{
    internal TCartographerStageView Stage(int tabIndex) =>
        Stages.Single(stage => stage.TabIndex == tabIndex);
}

internal static class TCartographer
{
    internal static Guid FinishTarget => LCartographer.LCartographerFinishTarget;

    internal static TCartographerPlanView PlanCreate(IReadOnlyList<TCartographerTabSpec> specs, int entryIndex)
    {
        Guid[] ids = specs.Select(_ => Guid.NewGuid()).ToArray();
        var tabs = new List<LCartographerTab>();
        for (int index = 0; index < specs.Count; index++)
        {
            TCartographerTabSpec spec = specs[index];
            var layout = new LSceneTabRecord
            {
                LSceneFunnelRules = spec.FunnelTargets
                    .Select(target => new LSceneFunnelRule { LSceneFunnelTarget = target })
                    .ToList()
            };
            tabs.Add(new LCartographerTab(
                ids[index],
                spec.Funnel ? "Funnel" : "Convert",
                $"tab-{index}",
                new LPresetRecord(),
                layout,
                spec.Funnel));
        }

        LCartographer.LCartographerTabsSource = () => tabs;
        try
        {
            for (int index = 0; index < specs.Count; index++)
            {
                if (!specs[index].Funnel && specs[index].ProcessingTarget >= 0)
                {
                    LCartographer.LCartographerTargetSet(ids[index], ids[specs[index].ProcessingTarget]);
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
