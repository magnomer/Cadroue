using Cadroue.Core;
using Cadroue.UIShell.PPanels;

namespace Cadroue.UIShell.PMainArea;

public sealed record LEditPlanRecord(
    string LEditPlanSourcePath,
    TimeSpan LEditPlanDuration,
    LWorkCrop LEditPlanCrop);

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
        IReadOnlyList<LEditPlanRecord> lEditPlans =
            await Task.Run(() => LEditPlanCollect(lEditSourcePaths, lEditCarried)).ConfigureAwait(true);

        int lEditAdded = 0;
        foreach (LEditPlanRecord lEditPlan in lEditPlans)
        {
            lEditAdded += LEditDescribe(
                lWorkPriority,
                lEditPlan.LEditPlanSourcePath,
                lEditPlan.LEditPlanDuration,
                lEditPlan.LEditPlanCrop,
                lExportSpecificState);
        }

        LAppLog.LInfo(
            $"Edit Add All: {lEditSourcePaths.Count} listed, {lEditPlans.Count} planned, {lEditAdded} queued, "
            + $"plan source {(lEditCarried is null ? "sidecar per file" : "persistent for every file")}");
        return lEditAdded;
    }

    private static IReadOnlyList<LEditPlanRecord> LEditPlanCollect(
        IReadOnlyList<string> lEditSourcePaths,
        LWorkCrop? lEditCarried)
    {
        var lEditPlans = new List<LEditPlanRecord>();
        foreach (string lEditSourcePath in lEditSourcePaths)
        {
            Cadroue.Media.LSidecar? lEditSidecar = LEditSidecarRead(lEditSourcePath);
            LWorkCrop? lEditPlanned = lEditCarried
                ?? (lEditSidecar?.Edit is { } lEditRecord ? LEditCropCreate(lEditRecord) : null);

            if (lEditPlanned is not { LWorkCropActive: true } lEditCrop)
            {
                continue;
            }

            TimeSpan lEditDuration = lEditSidecar is { Source.DurationMilliseconds: > 0 }
                ? TimeSpan.FromMilliseconds(lEditSidecar.Source.DurationMilliseconds)
                : LEditProbeDuration(lEditSourcePath);

            if (lEditCarried is not null)
            {
                LEditPlanSave(lEditSourcePath, lEditCrop);
            }

            lEditPlans.Add(new LEditPlanRecord(lEditSourcePath, lEditDuration, lEditCrop));
        }

        return lEditPlans;
    }

    private static TimeSpan LEditProbeDuration(string lEditSourcePath)
    {
        try
        {
            return Cadroue.Media.LMediaInfo.LMediaFfprobeRead(lEditSourcePath).LMediaInfoDuration;
        }
        catch (Exception lEditException)
        {
            LAppLog.LError($"Duration could not be read for '{lEditSourcePath}'", lEditException);
            return TimeSpan.Zero;
        }
    }

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
