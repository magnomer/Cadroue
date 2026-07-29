using Cadroue.Core;
using Cadroue.UIShell.PPanels;

namespace Cadroue.UIShell.PMainArea;

public sealed record LEditWorkDescription(
    string? LEditSourcePath,
    TimeSpan LEditDuration,
    LWorkCrop LEditCrop,
    LWorkOutput LEditOutput);

public static partial class LEdit
{
    public static int LEditDescribe(
        LWorkPriority lWorkPriority,
        string? lEditSourcePath,
        TimeSpan lEditDuration,
        LWorkCrop lEditCrop,
        LExportSpecificState lExportSpecificState)
    {
        LEditWorkDescription lEditWorkDescription = new(
            lEditSourcePath,
            lEditDuration,
            lEditCrop,
            lExportSpecificState.LPresetOutputCreate());

        return LEdit.LEditInterpret(lWorkPriority, lEditWorkDescription);
    }

    public static async Task<int> LEditAllDescribe(
        LWorkPriority lWorkPriority,
        IReadOnlyList<string> lEditSourcePaths,
        LExportSpecificState lExportSpecificState,
        LWorkCrop? lEditCarried = null)
    {
        LWorkOutput lEditOutput = lExportSpecificState.LPresetOutputCreate();
        var lEditWorkItems = new List<LWorkItem>();

        foreach (string lEditSourcePath in lEditSourcePaths)
        {
            if (LEditPlanResolve(lEditSourcePath, lEditCarried) is not { } lEditCrop)
            {
                continue;
            }

            if (lEditCarried is not null)
            {
                LEditPlanSave(lEditSourcePath, lEditCrop);
            }

            lEditWorkItems.Add(LEditWorkCreate(
                lWorkPriority,
                lEditSourcePath,
                Cadroue.Media.LSidecarStore.LSidecarDurationRead(lEditSourcePath),
                lEditCrop,
                lEditOutput));
        }

        int lEditAdded = LSchedule.LScheduleCurrent.LScheduleAdd(lEditWorkItems);
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

    private static LWorkCrop? LEditPlanResolve(string lEditSourcePath, LWorkCrop? lEditCarried)
    {
        if (lEditCarried is { } lEditPersistent)
        {
            return lEditPersistent;
        }

        return Cadroue.Media.LSidecarStore.LSidecarEditRead(lEditSourcePath) is { } lEditRecord
            ? LEditCropCreate(lEditRecord)
            : null;
    }

    private static int LEditParallelRead() => Math.Clamp(Environment.ProcessorCount, 1, 8);

    public static LWorkCrop? LEditPlanRead(string lEditSourcePath) =>
        LEditSidecarRead(lEditSourcePath)?.Edit is { } lEditRecord ? LEditCropCreate(lEditRecord) : null;

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

    private static LWorkCrop LEditCropCreate(Cadroue.Media.LSidecarEditRecord lEditRecord) => new(
        lEditRecord.CropLeft,
        lEditRecord.CropTop,
        lEditRecord.CropRight,
        lEditRecord.CropBottom,
        lEditRecord.Rotation,
        lEditRecord.FlipHorizontal,
        lEditRecord.FlipVertical);

    public static void LEditPlanSave(string lEditSourcePath, LWorkCrop lEditCrop)
    {
        Cadroue.Media.LSidecarStore.LSidecarEditSave(
            lEditSourcePath,
            new Cadroue.Media.LSidecarEditRecord
            {
                CropLeft = lEditCrop.LWorkCropLeft,
                CropTop = lEditCrop.LWorkCropTop,
                CropRight = lEditCrop.LWorkCropRight,
                CropBottom = lEditCrop.LWorkCropBottom,
                Rotation = lEditCrop.LWorkCropRotation,
                FlipHorizontal = lEditCrop.LWorkCropFlipHorizontal,
                FlipVertical = lEditCrop.LWorkCropFlipVertical
            });
    }
}
