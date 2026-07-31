using Cadroue.Core;
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

        return LEdit.LEditInterpret(lWorkPriority, lEditWorkDescription, lEditRelayTarget, lEditRelaySource);
    }

    public static async Task<int> LEditAllDescribe(
        LWorkPriority lWorkPriority,
        IReadOnlyList<string> lEditSourcePaths,
        LPreset lExportSpecificState,
        Guid lEditRelayTarget = default,
        Guid lEditRelaySource = default)
    {
        LWorkOutput lEditOutput = lExportSpecificState.LPresetOutputCreate();
        var lEditWorkItems = new List<LWorkItem>();

        foreach (string lEditSourcePath in lEditSourcePaths)
        {
            if (LEditPlanRead(lEditSourcePath) is not { LEditPlanActive: true } lEditPlan)
            {
                continue;
            }

            lEditWorkItems.Add(LEditWorkCreate(
                lWorkPriority,
                lEditSourcePath,
                Cadroue.Media.LSidecarStore.LSidecarDurationRead(lEditSourcePath),
                lEditPlan.LEditSkip ? LWorkCrop.LWorkCropCreate() : lEditPlan.LEditCrop,
                lEditPlan.LEditSkip ? LWorkVideo.LWorkVideoCreate() : lEditPlan.LEditVideo,
                lEditOutput));
        }

        int lEditAdded = LSchedule.LScheduleCurrent.LScheduleAdd(lEditWorkItems, lEditRelayTarget, lEditRelaySource);
        LTraceLog.LTraceInfoRecord(
            $"Edit Add All: {lEditSourcePaths.Count} listed, {lEditAdded} queued from saved plans");

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
                .FirstOrDefault(lStep => lStep.LWorkVideoStepKind == lKind);
            LWorkVideoStep? lSavedStep = lEditSaved?.LEditVideo.LWorkVideoSteps
                .FirstOrDefault(lStep => lStep.LWorkVideoStepKind == lKind);
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

    public static Cadroue.Media.LSidecarEditRecord LEditPersistentCreate(LEditPlan lEditPlan) => new()
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

    public static LEditPlan LEditPersistentRead(Cadroue.Media.LSidecarEditRecord lEditRecord) =>
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

    private static LEditPlan LEditPlanCreate(Cadroue.Media.LSidecarEditRecord lEditRecord) => new(
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

    private static LWorkVideoStep LEditStepCreate(Cadroue.Media.LSidecarVideoStepRecord lEditRecord) =>
        string.Equals(lEditRecord.LSidecarKind, "Contrast", StringComparison.Ordinal)
            ? LWorkVideoStep.LWorkContrastCreate(lEditRecord.LSidecarActive, lEditRecord.LSidecarValue)
            : LWorkVideoStep.LWorkBrightnessCreate(lEditRecord.LSidecarActive, lEditRecord.LSidecarValue);

    public static void LEditPlanSave(string lEditSourcePath, LEditPlan lEditPlan)
    {
        Cadroue.Media.LSidecarStore.LSidecarEditSave(
            lEditSourcePath,
            new Cadroue.Media.LSidecarEditRecord
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

    private static Cadroue.Media.LSidecarVideoStepRecord LEditRecordCreate(LWorkVideoStep lEditStep) => new()
    {
        LSidecarKind = lEditStep.LWorkVideoStepKind == LWorkVideoKind.LWorkVideoKindContrast ? "Contrast" : "Brightness",
        LSidecarActive = lEditStep.LWorkVideoStepActive,
        LSidecarValue = lEditStep.LWorkVideoStepValue
    };
}
