using Cadroue.Core;

namespace Cadroue.ShellEngine;

public static partial class LCartographer
{
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
