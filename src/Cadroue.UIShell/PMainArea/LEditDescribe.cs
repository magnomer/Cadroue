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
    public static LEditPlan LEditPlanNoneCreate() =>
        new(LWorkCrop.LWorkCropNoneCreate(), LWorkVideo.LWorkVideoNoneCreate(), false);

    public bool LEditPlanActive =>
        LEditCropApply || LEditCrop.LWorkCropActive || LEditVideo.LWorkVideoActive;
}

public static partial class LEdit
{
    public static int LEditDescribe(
        LWorkPriority lWorkPriority,
        string? lEditSourcePath,
        TimeSpan lEditDuration,
        LWorkCrop lEditCrop,
        LWorkVideo lEditVideo,
        LExportSpecificState lExportSpecificState,
        Guid lEditRelayTarget = default)
    {
        LEditWorkDescription lEditWorkDescription = new(
            lEditSourcePath,
            lEditDuration,
            lEditCrop,
            lEditVideo,
            lExportSpecificState.LPresetOutputCreate());

        return LEdit.LEditInterpret(lWorkPriority, lEditWorkDescription, lEditRelayTarget);
    }

    public static async Task<int> LEditAllDescribe(
        LWorkPriority lWorkPriority,
        IReadOnlyList<string> lEditSourcePaths,
        LExportSpecificState lExportSpecificState,
        LEditPlan? lEditCarried = null,
        Guid lEditRelayTarget = default)
    {
        LWorkOutput lEditOutput = lExportSpecificState.LPresetOutputCreate();
        var lEditWorkItems = new List<LWorkItem>();

        foreach (string lEditSourcePath in lEditSourcePaths)
        {
            if (LEditPlanResolve(lEditSourcePath, lEditCarried) is not { } lEditPlan)
            {
                continue;
            }

            if (lEditCarried is not null)
            {
                LEditPlanSave(lEditSourcePath, lEditPlan);
            }

            lEditWorkItems.Add(LEditWorkCreate(
                lWorkPriority,
                lEditSourcePath,
                Cadroue.Media.LSidecarStore.LSidecarDurationRead(lEditSourcePath),
                lEditPlan.LEditCrop,
                lEditPlan.LEditVideo,
                lEditOutput));
        }

        int lEditAdded = LSchedule.LScheduleCurrent.LScheduleAdd(lEditWorkItems, lEditRelayTarget);
        LAppLog.LInfo(
            $"Edit Add All: {lEditSourcePaths.Count} listed, {lEditAdded} queued, "
            + $"plan source {(lEditCarried is null ? "sidecar per file" : "persistent for every file")}");

        await LEditDurationFill(lEditWorkItems).ConfigureAwait(true);
        return lEditAdded;
    }

    private static Task LEditDurationFill(IReadOnlyList<LWorkItem> lEditWorkItems)
    {
        LWorkItem[] lEditUnknown = lEditWorkItems
            .Where(lWorkItem => lWorkItem.LWorkEnd <= TimeSpan.Zero)
            .ToArray();
        if (lEditUnknown.Length == 0)
        {
            return Task.CompletedTask;
        }

        return Task.Run(() => Parallel.ForEach(
            lEditUnknown,
            new ParallelOptions { MaxDegreeOfParallelism = LEditParallelRead() },
            lWorkItem => LSchedule.LScheduleCurrent.LScheduleDurationSet(
                lWorkItem.LWorkId,
                Cadroue.Media.LSidecarStore.LSidecarDurationResolve(lWorkItem.LWorkSourcePath))));
    }

    public static LEditPlan LEditPlanResolve(LEditPlan? lEditSaved, LEditPlan? lEditPersistent)
    {
        if (lEditPersistent is not { } lPersistent)
        {
            return lEditSaved ?? LEditPlan.LEditPlanNoneCreate();
        }

        LWorkCrop lCrop = lPersistent.LEditCropApply
            ? lPersistent.LEditCrop
            : lEditSaved?.LEditCrop ?? LWorkCrop.LWorkCropNoneCreate();
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

        return new LEditPlan(lCrop, new LWorkVideo(lSteps), lCropApply);
    }

    private static LEditPlan? LEditPlanResolve(string lEditSourcePath, LEditPlan? lEditCarried)
    {
        if (lEditCarried is { } lEditPersistent)
        {
            return lEditPersistent;
        }

        return LEditPlanRead(lEditSourcePath);
    }

    private static int LEditParallelRead() => Math.Clamp(Environment.ProcessorCount, 1, 8);

    public static LEditPlan? LEditPlanRead(string lEditSourcePath) =>
        LEditSidecarRead(lEditSourcePath)?.Edit is { } lEditRecord ? LEditPlanCreate(lEditRecord) : null;

    private static Cadroue.Media.LSidecar? LEditSidecarRead(string lEditSourcePath)
    {
        try
        {
            return Cadroue.Media.LSidecarStore.LSidecarRead(
                Cadroue.Media.LSidecarStore.LSidecarPathRead(lEditSourcePath));
        }
        catch (Exception lEditException)
        {
            LAppLog.LError($"Edit plan could not be read for '{lEditSourcePath}'", lEditException);
            return null;
        }
    }

    private static LEditPlan LEditPlanCreate(Cadroue.Media.LSidecarEditRecord lEditRecord) => new(
        new LWorkCrop(
            lEditRecord.CropLeft,
            lEditRecord.CropTop,
            lEditRecord.CropRight,
            lEditRecord.CropBottom,
            lEditRecord.Rotation,
            lEditRecord.FlipHorizontal,
            lEditRecord.FlipVertical),
        new LWorkVideo(lEditRecord.Steps.Select(LEditVideoStepCreate).ToList()),
        lEditRecord.CropActive);

    private static LWorkVideoStep LEditVideoStepCreate(Cadroue.Media.LSidecarVideoStepRecord lEditRecord) =>
        string.Equals(lEditRecord.Kind, "Contrast", StringComparison.Ordinal)
            ? LWorkVideoStep.LWorkVideoContrastCreate(lEditRecord.Active, lEditRecord.Value)
            : LWorkVideoStep.LWorkVideoBrightnessCreate(lEditRecord.Active, lEditRecord.Value);

    public static void LEditPlanSave(string lEditSourcePath, LEditPlan lEditPlan)
    {
        Cadroue.Media.LSidecarStore.LSidecarEditSave(
            lEditSourcePath,
            new Cadroue.Media.LSidecarEditRecord
            {
                CropLeft = lEditPlan.LEditCrop.LWorkCropLeft,
                CropTop = lEditPlan.LEditCrop.LWorkCropTop,
                CropRight = lEditPlan.LEditCrop.LWorkCropRight,
                CropBottom = lEditPlan.LEditCrop.LWorkCropBottom,
                Rotation = lEditPlan.LEditCrop.LWorkCropRotation,
                FlipHorizontal = lEditPlan.LEditCrop.LWorkCropFlipHorizontal,
                FlipVertical = lEditPlan.LEditCrop.LWorkCropFlipVertical,
                CropActive = lEditPlan.LEditCropApply,
                Steps = lEditPlan.LEditVideo.LWorkVideoSteps.Select(LEditVideoStepRecordCreate).ToList()
            });
    }

    private static Cadroue.Media.LSidecarVideoStepRecord LEditVideoStepRecordCreate(LWorkVideoStep lEditStep) => new()
    {
        Kind = lEditStep.LWorkVideoStepKind == LWorkVideoKind.LWorkVideoKindContrast ? "Contrast" : "Brightness",
        Active = lEditStep.LWorkVideoStepActive,
        Value = lEditStep.LWorkVideoStepValue
    };
}
