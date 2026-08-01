using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.UIShell.PPanels;

namespace Cadroue.UIShell.PMainArea;

public sealed record LEditWorkDescription(
    string? LEditSourcePath,
    TimeSpan LEditDuration,
    LWorkCrop LEditCrop,
    LWorkVideo LEditVideo,
    LWorkOutput LEditOutput);

public sealed record LEditPlan(LWorkCrop LEditCrop, LWorkVideo LEditVideo, bool LEditCropApply)
{
    public bool LEditSkip { get; init; }

    public static LEditPlan LEditEmptyCreate() =>
        new(LWorkCrop.LWorkCropCreate(), LWorkVideo.LWorkVideoCreate(), false);

    public bool LEditPlanActive =>
        LEditSkip || LEditCropApply || LEditCrop.LWorkCropActive || LEditVideo.LWorkVideoActive;
}

public static partial class LEdit
{
    public static int LEditDescribe(
        LWorkPriority lWorkPriority,
        string? lEditSourcePath,
        TimeSpan lEditDuration,
        LWorkCrop lEditCrop,
        LWorkVideo lEditVideo,
        LPreset lExportSpecificState,
        Guid lEditRelayTarget = default,
        Guid lEditRelaySource = default)
    {
        LEditWorkDescription lEditWorkDescription = new(
            lEditSourcePath,
            lEditDuration,
            lEditCrop,
            lEditVideo,
            lExportSpecificState.LPresetOutputCreate());

        string lEditTab = PControlBar.LTabset.LTabsetTitleRead(lEditRelaySource);
        IReadOnlyList<LWorkItem> lEditWorkItems = LEdit.LEditItemsCreate(
            lWorkPriority, lEditWorkDescription, lEditTab);
        if (lEditWorkItems.Count == 0)
        {
            return 0;
        }

        int lEditAdded = LSchedule.LScheduleCurrent.LScheduleAdd(
            lEditWorkItems, lEditRelayTarget, lEditRelaySource);
        LTraceLog.LTraceInfoRecord(
            $"Edit queued {lEditAdded} job(s) at {lWorkPriority} from " +
            $"'{System.IO.Path.GetFileName(lEditSourcePath)}'");
        return lEditAdded;
    }

    public static async Task<int> LEditAllDescribe(
        LWorkPriority lWorkPriority,
        IReadOnlyList<LWorkSource> lEditSources,
        LPreset lExportSpecificState,
        Guid lEditRelayTarget = default,
        Guid lEditRelaySource = default)
    {
        LWorkOutput lEditOutput = lExportSpecificState.LPresetOutputCreate();
        var lEditWorkItems = new List<LWorkItem>();
        Guid lEditLooseBatch = Guid.NewGuid();

        foreach (LWorkSource lEditSource in lEditSources)
        {
            string lEditSourcePath = lEditSource.LWorkSourcePath;
            if (LEditPlanRead(lEditSourcePath) is not { LEditPlanActive: true } lEditPlan)
            {
                continue;
            }

            Guid lEditBatch = lEditSource.LWorkSourceBatch != Guid.Empty
                ? lEditSource.LWorkSourceBatch
                : lEditLooseBatch;
            lEditWorkItems.Add(LEditWorkCreate(
                lWorkPriority,
                lEditSourcePath,
                Cadroue.Media.LSidecarStore.LSidecarDurationRead(lEditSourcePath),
                lEditPlan.LEditSkip ? LWorkCrop.LWorkCropCreate() : lEditPlan.LEditCrop,
                lEditPlan.LEditSkip ? LWorkVideo.LWorkVideoCreate() : lEditPlan.LEditVideo,
                lEditOutput,
                lEditBatch));
        }

        string lEditTab = PControlBar.LTabset.LTabsetTitleRead(lEditRelaySource);
        foreach (LWorkItem lEditItem in lEditWorkItems)
        {
            lEditItem.LWorkTab = lEditTab;
        }

        int lEditAdded = LSchedule.LScheduleCurrent.LScheduleAdd(lEditWorkItems, lEditRelayTarget, lEditRelaySource);
        LTraceLog.LTraceInfoRecord(
            $"Edit Add All: {lEditSources.Count} listed, {lEditAdded} queued from saved plans");

        await LEditDurationResolve(lEditWorkItems).ConfigureAwait(true);
        return lEditAdded;
    }

    private static async Task LEditDurationResolve(IReadOnlyList<LWorkItem> lEditWorkItems)
    {
        LWorkItem[] lEditUnknown = lEditWorkItems
            .Where(lWorkItem => lWorkItem.LWorkEnd <= TimeSpan.Zero)
            .ToArray();
        if (lEditUnknown.Length == 0)
        {
            return;
        }

        var lEditResolved = new TimeSpan[lEditUnknown.Length];
        await Task.Run(() => Parallel.For(
            0,
            lEditUnknown.Length,
            new ParallelOptions { MaxDegreeOfParallelism = LEditParallelRead() },
            lEditIndex => lEditResolved[lEditIndex] =
                Cadroue.Media.LSidecarStore.LSidecarDurationResolve(lEditUnknown[lEditIndex].LWorkSourcePath)))
            .ConfigureAwait(true);

        for (int lEditIndex = 0; lEditIndex < lEditUnknown.Length; lEditIndex++)
        {
            LSchedule.LScheduleCurrent.LScheduleDurationSet(
                lEditUnknown[lEditIndex].LWorkId, lEditResolved[lEditIndex]);
        }
    }

    public static LEditPlan LEditPlanResolve(LEditPlan? lEditSaved, LEditPlan? lEditPersistent)
    {
        if (lEditPersistent is not { } lPersistent)
        {
            return lEditSaved ?? LEditPlan.LEditEmptyCreate();
        }

        bool lEditSkip = lPersistent.LEditSkip || (lEditSaved?.LEditSkip ?? false);

        LWorkCrop lCrop = lPersistent.LEditCropApply
            ? lPersistent.LEditCrop
            : lEditSaved?.LEditCrop ?? LWorkCrop.LWorkCropCreate();
        bool lCropApply = lPersistent.LEditCropApply || (lEditSaved?.LEditCropApply ?? false);
        var lSteps = new List<LWorkVideoStep>();
        foreach (LWorkVideoKind lKind in Enum.GetValues<LWorkVideoKind>())
        {
            LWorkVideoStep? lPersistentStep = lPersistent.LEditVideo.LWorkVideoSteps
                .FirstOrDefault(lStep => lStep.LWorkStepKind == lKind);
            LWorkVideoStep? lSavedStep = lEditSaved?.LEditVideo.LWorkVideoSteps
                .FirstOrDefault(lStep => lStep.LWorkStepKind == lKind);
            if (lPersistentStep is not null)
            {
                lSteps.Add(lPersistentStep);
            }
            else if (lSavedStep is not null)
            {
                lSteps.Add(lSavedStep);
            }
        }

        return new LEditPlan(lCrop, new LWorkVideo(lSteps), lCropApply) { LEditSkip = lEditSkip };
    }

    private static int LEditParallelRead() => Math.Clamp(Environment.ProcessorCount, 1, 8);

    public static Cadroue.Core.LSidecarEditRecord LEditPersistentCreate(LEditPlan lEditPlan) => new()
    {
        LSidecarCropLeft = lEditPlan.LEditCrop.LWorkCropLeft,
        LSidecarCropTop = lEditPlan.LEditCrop.LWorkCropTop,
        LSidecarCropRight = lEditPlan.LEditCrop.LWorkCropRight,
        LSidecarCropBottom = lEditPlan.LEditCrop.LWorkCropBottom,
        LSidecarRotation = lEditPlan.LEditCrop.LWorkCropRotation,
        LSidecarFlipHorizontal = lEditPlan.LEditCrop.LWorkCropFlipHorizontal,
        LSidecarFlipVertical = lEditPlan.LEditCrop.LWorkCropFlipVertical,
        LSidecarCropActive = lEditPlan.LEditCropApply,
        LSidecarSkip = lEditPlan.LEditSkip,
        LSidecarSteps = lEditPlan.LEditVideo.LWorkVideoSteps.Select(LEditRecordCreate).ToList()
    };

    public static LEditPlan LEditPersistentRead(Cadroue.Core.LSidecarEditRecord lEditRecord) =>
        LEditPlanCreate(lEditRecord);

    public static LEditPlan? LEditPlanRead(string lEditSourcePath) =>
        LEditSidecarRead(lEditSourcePath)?.LSidecarEdit is { } lEditRecord ? LEditPlanCreate(lEditRecord) : null;

    private static Cadroue.Media.LSidecar? LEditSidecarRead(string lEditSourcePath)
    {
        try
        {
            return Cadroue.Media.LSidecarStore.LSidecarRead(
                Cadroue.Media.LSidecarStore.LSidecarPathRead(lEditSourcePath));
        }
        catch (Exception lEditException)
        {
            LTraceLog.LTraceErrorRecord($"Edit plan could not be read for '{lEditSourcePath}'", lEditException);
            return null;
        }
    }

    private static LEditPlan LEditPlanCreate(Cadroue.Core.LSidecarEditRecord lEditRecord) => new(
        new LWorkCrop(
            lEditRecord.LSidecarCropLeft,
            lEditRecord.LSidecarCropTop,
            lEditRecord.LSidecarCropRight,
            lEditRecord.LSidecarCropBottom,
            lEditRecord.LSidecarRotation,
            lEditRecord.LSidecarFlipHorizontal,
            lEditRecord.LSidecarFlipVertical),
        new LWorkVideo(lEditRecord.LSidecarSteps.Select(LEditStepCreate).ToList()),
        lEditRecord.LSidecarCropActive) { LEditSkip = lEditRecord.LSidecarSkip };

    private static LWorkVideoStep LEditStepCreate(Cadroue.Core.LSidecarVideoStepRecord lEditRecord) =>
        string.Equals(lEditRecord.LSidecarKind, "Contrast", StringComparison.Ordinal)
            ? LWorkVideoStep.LWorkContrastCreate(lEditRecord.LSidecarActive, lEditRecord.LSidecarValue)
            : LWorkVideoStep.LWorkBrightnessCreate(lEditRecord.LSidecarActive, lEditRecord.LSidecarValue);

    public static void LEditPlanSave(string lEditSourcePath, LEditPlan lEditPlan)
    {
        Cadroue.Media.LSidecarStore.LSidecarEditSave(
            lEditSourcePath,
            new Cadroue.Core.LSidecarEditRecord
            {
                LSidecarCropLeft = lEditPlan.LEditCrop.LWorkCropLeft,
                LSidecarCropTop = lEditPlan.LEditCrop.LWorkCropTop,
                LSidecarCropRight = lEditPlan.LEditCrop.LWorkCropRight,
                LSidecarCropBottom = lEditPlan.LEditCrop.LWorkCropBottom,
                LSidecarRotation = lEditPlan.LEditCrop.LWorkCropRotation,
                LSidecarFlipHorizontal = lEditPlan.LEditCrop.LWorkCropFlipHorizontal,
                LSidecarFlipVertical = lEditPlan.LEditCrop.LWorkCropFlipVertical,
                LSidecarCropActive = lEditPlan.LEditCropApply,
                LSidecarSkip = lEditPlan.LEditSkip,
                LSidecarSteps = lEditPlan.LEditVideo.LWorkVideoSteps.Select(LEditRecordCreate).ToList()
            });
    }

    private static Cadroue.Core.LSidecarVideoStepRecord LEditRecordCreate(LWorkVideoStep lEditStep) => new()
    {
        LSidecarKind = lEditStep.LWorkStepKind == LWorkVideoKind.LWorkVideoKindContrast ? "Contrast" : "Brightness",
        LSidecarActive = lEditStep.LWorkStepActive,
        LSidecarValue = lEditStep.LWorkStepValue
    };
}
