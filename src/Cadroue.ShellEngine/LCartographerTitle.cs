using Cadroue.Core;

namespace Cadroue.ShellEngine;

public static class LCartographerTitle
{
    public static Func<Guid, string>? LCartographerTitleSource { get; set; }

    private static readonly Dictionary<Guid, string> lCartographerStageTitles = new();

    public static string LCartographerStageRead(Guid lCartographerStageId) =>
        lCartographerStageTitles.TryGetValue(lCartographerStageId, out string? lCartographerTitle)
            ? lCartographerTitle
            : string.Empty;

    public static void LCartographerStageSet(Guid lCartographerStageId, string lCartographerTitle) =>
        lCartographerStageTitles[lCartographerStageId] = lCartographerTitle;

    public static void LCartographerTitleRemove(Guid lCartographerStageId) =>
        lCartographerStageTitles.Remove(lCartographerStageId);

    public static string LCartographerTitleRead(
        Guid lCartographerBatchId, Guid lCartographerRelaySource, string lCartographerTab)
    {
        string lCartographerTitle =
            LCartographerTitleSource?.Invoke(lCartographerRelaySource) ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(lCartographerTitle))
        {
            return lCartographerTitle;
        }

        if (LCartographerPlanStore.LCartographerPlanRead(
                lCartographerBatchId, out LCartographerPlanDocument lCartographerPlan)
            && lCartographerPlan.LCartographerStages.FirstOrDefault(
                lCartographerStage => lCartographerStage.LCartographerStageId == lCartographerRelaySource)
                is { } lCartographerSourceStage)
        {
            lCartographerStageTitles[lCartographerSourceStage.LCartographerStageId] =
                lCartographerSourceStage.LCartographerTitle;
            return lCartographerSourceStage.LCartographerTitle;
        }

        return lCartographerTab;
    }
}
