using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.ShellEngine;

public static partial class LCartographer
{
    public static async Task<IReadOnlyList<string>> LCartographerStageExecute(LCartographerStagePlan lCartographerPlan)
    {
        LPreset lCartographerPreset = LPreset.LPresetStateCreate(lCartographerPlan.LCartographerExport);
        var lCartographerOwner = new LPresetSelection(
            lCartographerPreset.LPresetRecordCreate(), lCartographerPreset.LPresetName);
        Guid lCartographerTarget = lCartographerPlan.LCartographerNextStage;
        Guid lCartographerSource = lCartographerPlan.LCartographerStageId;
        Guid lCartographerBatch = lCartographerPlan.LCartographerBatch;
        IReadOnlyList<string> lCartographerPaths = lCartographerPlan.LCartographerPaths;

        return lCartographerPlan.LCartographerLayoutKey switch
        {
            "Convert" => await LCartographerConvertExecute(
                lCartographerPaths, lCartographerOwner, lCartographerTarget, lCartographerSource, lCartographerBatch)
                .ConfigureAwait(false),
            "Merge" => LCartographerMergeExecute(
                lCartographerPlan.LCartographerLayout, lCartographerPaths, lCartographerOwner,
                lCartographerTarget, lCartographerSource, lCartographerBatch),
            _ => LCartographerSplitExecute(
                lCartographerPaths, lCartographerOwner, lCartographerTarget, lCartographerSource, lCartographerBatch)
        };
    }

    private static async Task<IReadOnlyList<string>> LCartographerConvertExecute(
        IReadOnlyList<string> lCartographerPaths,
        LPresetSelection lCartographerOwner,
        Guid lCartographerTarget,
        Guid lCartographerSource,
        Guid lCartographerBatch)
    {
        LWorkSource[] lCartographerSources = lCartographerPaths
            .Select(lCartographerPath => new LWorkSource(lCartographerPath, lCartographerBatch))
            .ToArray();
        int lCartographerAdded = await LMessenger.LMessengerConvertDescribe(
            LWorkPriority.LWorkPriorityNormal, lCartographerSources, lCartographerOwner,
            lCartographerTarget, lCartographerSource).ConfigureAwait(false);
        return lCartographerAdded > 0 ? lCartographerPaths.ToArray() : Array.Empty<string>();
    }

    private static IReadOnlyList<string> LCartographerSplitExecute(
        IReadOnlyList<string> lCartographerPaths,
        LPresetSelection lCartographerOwner,
        Guid lCartographerTarget,
        Guid lCartographerSource,
        Guid lCartographerBatch)
    {
        var lCartographerAcknowledged = new List<string>();
        foreach (string lCartographerPath in lCartographerPaths)
        {
            IReadOnlyList<LSplitSectionDescription> lCartographerSections =
                LMessenger.LMessengerSplitRead(lCartographerPath);
            if (lCartographerSections.Count == 0)
            {
                continue;
            }

            int lCartographerAdded = LMessenger.LMessengerSplitDescribe(
                LWorkPriority.LWorkPriorityNormal, lCartographerPath, lCartographerSections,
                lCartographerOwner, lCartographerTarget, lCartographerSource, lCartographerBatch);
            if (lCartographerAdded > 0)
            {
                lCartographerAcknowledged.Add(lCartographerPath);
            }
        }

        return lCartographerAcknowledged;
    }

    private static IReadOnlyList<string> LCartographerMergeExecute(
        LSceneTabRecord lCartographerLayout,
        IReadOnlyList<string> lCartographerPaths,
        LPresetSelection lCartographerOwner,
        Guid lCartographerTarget,
        Guid lCartographerSource,
        Guid lCartographerBatch)
    {
        var lCartographerGroupOwner = new LGroupSelection(
            lCartographerLayout.LSceneGroupAuto,
            lCartographerLayout.LSceneGroupStrict,
            lCartographerLayout.LSceneGroupNameMode);
        LWorkGroup[] lCartographerGroups = lCartographerGroupOwner.LGroupResolve(lCartographerPaths)
            .Select(lCartographerGroup => new LWorkGroup(lCartographerGroup.Name, lCartographerGroup.Paths))
            .Where(lCartographerGroup => lCartographerGroup.LWorkGroupPaths.Count > 0)
            .ToArray();
        if (lCartographerGroups.Length == 0)
        {
            return Array.Empty<string>();
        }

        var lCartographerRelays = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        foreach (string lCartographerPath in lCartographerPaths)
        {
            lCartographerRelays[lCartographerPath] = lCartographerBatch;
        }

        int lCartographerAdded = LMessenger.LMessengerMergeDescribe(
            LWorkPriority.LWorkPriorityNormal, lCartographerGroups, lCartographerOwner,
            lCartographerTarget, lCartographerSource, lCartographerRelays);
        if (lCartographerAdded == 0)
        {
            return Array.Empty<string>();
        }

        return lCartographerGroups
            .SelectMany(lCartographerGroup => lCartographerGroup.LWorkGroupPaths)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
