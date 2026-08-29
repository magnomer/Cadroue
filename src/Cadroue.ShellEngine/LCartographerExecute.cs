using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Infrastructure;

namespace Cadroue.ShellEngine;

public static partial class LCartographer
{
    public static IReadOnlyList<string> LCartographerStageRun(LCartographerStagePlan lCartographerPlan)
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
            "Convert" => LCartographerConvertRun(
                lCartographerPaths, lCartographerOwner, lCartographerTarget, lCartographerSource, lCartographerBatch),
            "Merge" => LCartographerMergeRun(
                lCartographerPlan.LCartographerLayout, lCartographerPaths, lCartographerOwner,
                lCartographerTarget, lCartographerSource, lCartographerBatch),
            "Edit" => LCartographerEditRun(
                lCartographerPlan.LCartographerLayout, lCartographerPaths, lCartographerOwner,
                lCartographerTarget, lCartographerSource, lCartographerBatch),
            "Fix" => LCartographerFixRun(
                lCartographerPaths, lCartographerOwner,
                lCartographerTarget, lCartographerSource, lCartographerBatch),
            "Audio" => LCartographerAudioRun(
                lCartographerPlan.LCartographerLayout, lCartographerPaths, lCartographerOwner,
                lCartographerTarget, lCartographerSource, lCartographerBatch),
            _ => LCartographerSplitRun(
                lCartographerPaths, lCartographerOwner, lCartographerTarget, lCartographerSource, lCartographerBatch)
        };
    }

    private static IReadOnlyList<string> LCartographerConvertRun(
        IReadOnlyList<string> lCartographerPaths,
        LPresetSelection lCartographerOwner,
        Guid lCartographerTarget,
        Guid lCartographerSource,
        Guid lCartographerBatch)
    {
        LWorkSource[] lCartographerSources = lCartographerPaths
            .Select(lCartographerPath => new LWorkSource(lCartographerPath, lCartographerBatch))
            .ToArray();
        LCartographerFaultRecord(LMessenger.LMessengerConvertDescribe(
            LWorkPriority.LWorkPriorityNormal, lCartographerSources, lCartographerOwner,
            lCartographerTarget, lCartographerSource));
        return LCartographerAcknowledgedRead(lCartographerBatch, lCartographerSource, lCartographerPaths);
    }

    private static IReadOnlyList<string> LCartographerSplitRun(
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

    private static IReadOnlyList<string> LCartographerMergeRun(
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
            lCartographerLayout.LSceneGroupMode);
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

    private static IReadOnlyList<string> LCartographerEditRun(
        LSceneTabRecord lCartographerLayout,
        IReadOnlyList<string> lCartographerPaths,
        LPresetSelection lCartographerOwner,
        Guid lCartographerTarget,
        Guid lCartographerSource,
        Guid lCartographerBatch)
    {
        LEditPlan lCartographerPlan = lCartographerLayout.LSceneInspector?.LSceneInspectorEdit is { } lCartographerRecord
            ? LEdit.LEditPersistentRead(lCartographerRecord)
            : LEditPlan.LEditEmptyCreate();
        bool lCartographerEqCapable = LInventory.LInventoryFilterExist("eq");
        LWorkCrop lCartographerCrop = lCartographerPlan.LEditSkip
            ? LWorkCrop.LWorkCropCreate()
            : lCartographerPlan.LEditCrop;
        LWorkVideo lCartographerVideo = lCartographerPlan.LEditSkip
            ? LWorkVideo.LWorkVideoCreate()
            : LEdit.LEditVideoCreate(
                lCartographerPlan.LEditVideo.LWorkVideoSteps, true, lCartographerEqCapable);

        var lCartographerAcknowledged = new List<string>();
        foreach (string lCartographerPath in lCartographerPaths)
        {
            int lCartographerAdded = LMessenger.LMessengerEditDescribe(
                LWorkPriority.LWorkPriorityNormal, lCartographerPath,
                LLibrarian.LLibrarianDurationRead(lCartographerPath),
                lCartographerCrop, lCartographerVideo, lCartographerOwner,
                lCartographerTarget, lCartographerSource, lCartographerBatch);
            if (lCartographerAdded > 0)
            {
                lCartographerAcknowledged.Add(lCartographerPath);
            }
        }

        return lCartographerAcknowledged;
    }

    private static IReadOnlyList<string> LCartographerFixRun(
        IReadOnlyList<string> lCartographerPaths,
        LPresetSelection lCartographerOwner,
        Guid lCartographerTarget,
        Guid lCartographerSource,
        Guid lCartographerBatch)
    {
        LWorkSource[] lCartographerSources = lCartographerPaths
            .Select(lCartographerPath => new LWorkSource(lCartographerPath, lCartographerBatch))
            .ToArray();
        LCartographerFaultRecord(LMessenger.LMessengerFixDescribe(
            LWorkPriority.LWorkPriorityNormal, lCartographerSources, lCartographerOwner,
            lCartographerTarget, lCartographerSource));
        return LCartographerAcknowledgedRead(lCartographerBatch, lCartographerSource, lCartographerPaths);
    }

    private static IReadOnlyList<string> LCartographerAudioRun(
        LSceneTabRecord lCartographerLayout,
        IReadOnlyList<string> lCartographerPaths,
        LPresetSelection lCartographerOwner,
        Guid lCartographerTarget,
        Guid lCartographerSource,
        Guid lCartographerBatch)
    {
        LWorkAudio lCartographerProcessing = lCartographerLayout.LSceneInspector?.LSceneInspectorAudio is { } lCartographerRecord
            ? LAudio.LAudioPersistentRead(lCartographerRecord)
            : LWorkAudio.LWorkAudioCreate();

        foreach (string lCartographerPath in lCartographerPaths)
        {
            LCartographerFaultRecord(LMessenger.LMessengerAudioDescribe(
                LWorkPriority.LWorkPriorityNormal, lCartographerPath, lCartographerProcessing,
                lCartographerOwner, lCartographerTarget, lCartographerSource, lCartographerBatch));
        }

        return LCartographerAcknowledgedRead(lCartographerBatch, lCartographerSource, lCartographerPaths);
    }

    private static IReadOnlyList<string> LCartographerAcknowledgedRead(
        Guid lCartographerBatch,
        Guid lCartographerSource,
        IReadOnlyList<string> lCartographerPaths)
    {
        var lCartographerRepresented = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (LWorkItem lCartographerWork in LCartographerScheduleRead())
        {
            if (lCartographerWork.LWorkBatchId != lCartographerBatch
                || lCartographerWork.LWorkRelaySource != lCartographerSource)
            {
                continue;
            }

            if (!string.IsNullOrEmpty(lCartographerWork.LWorkSourcePath))
            {
                lCartographerRepresented.Add(lCartographerWork.LWorkSourcePath);
            }
            foreach (string lCartographerMergeSource in lCartographerWork.LWorkMergeSources)
            {
                lCartographerRepresented.Add(lCartographerMergeSource);
            }
        }

        return lCartographerPaths
            .Where(lCartographerPath => lCartographerRepresented.Contains(lCartographerPath))
            .ToArray();
    }

    private static void LCartographerFaultRecord(Task<int> lCartographerTask) =>
        lCartographerTask.ContinueWith(
            lCartographerFaulted => LTraceLog.LTraceErrorRecord(
                "Relay stage execution failed", lCartographerFaulted.Exception),
            TaskContinuationOptions.OnlyOnFaulted);
}
