using Cadroue.Core;

namespace Cadroue.Application;

public static class LEdit
{
    public static LWorkItem? LEditWorkCreate(
        LWorkPriority lWorkPriority,
        string lEditSourcePath,
        TimeSpan lEditDuration,
        LWorkCrop lEditCrop,
        LWorkVideo lEditVideo,
        LEncoding lEditOutput,
        Guid lEditBatchId)
    {
        if (!lEditOutput.LEncodingSupportCheck(LWorkKind.LWorkKindEdit))
        {
            return null;
        }

        string lEditFolder = LEncodingPath.LEncodingFolderRead(lEditOutput, lEditSourcePath);
        string lEditOutputName = LEditName.LEditNameCreate(lEditOutput, lEditSourcePath, lEditFolder, lEditDuration);

        return new LWorkItem(
            lEditBatchId,
            LWorkKind.LWorkKindEdit,
            lWorkPriority,
            lEditSourcePath,
            TimeSpan.Zero,
            lEditDuration,
            lEditOutputName,
            Path.Combine(lEditFolder, lEditOutputName),
            lEditOutput,
            lWorkCrop: lEditCrop,
            lWorkVideo: lEditVideo);
    }

    public static IReadOnlyList<LWorkItem> LEditItemsCreate(
        LWorkPriority lWorkPriority,
        LEditWorkDescription lEditWorkDescription,
        string lEditTab,
        Action<string> lInfoLog,
        Action<string> lErrorLog,
        Guid lEditBatchId = default)
    {
        LWorkCrop lEditCrop = lEditWorkDescription.LEditCrop;
        if (string.IsNullOrWhiteSpace(lEditWorkDescription.LEditSourcePath))
        {
            lErrorLog("Edit not queued: no source file is open");
            return Array.Empty<LWorkItem>();
        }

        if (LEditWorkCreate(
                lWorkPriority,
                lEditWorkDescription.LEditSourcePath,
                lEditWorkDescription.LEditDuration,
                lEditCrop,
                lEditWorkDescription.LEditVideo,
                lEditWorkDescription.LEditOutput,
                lEditBatchId == Guid.Empty ? LGate.LGateBatchCreate() : lEditBatchId) is not { } lEditWorkItem)
        {
            lErrorLog("Edit not queued: the export preset copies the video stream, so no edit can be applied");
            return Array.Empty<LWorkItem>();
        }

        lEditWorkItem.LWorkTab = lEditTab;
        string lEditOutputName = lEditWorkItem.LWorkOutputName;
        lInfoLog(
            $"Edit built job '{lEditOutputName}' at {lWorkPriority} from " +
            $"'{Path.GetFileName(lEditWorkItem.LWorkSourcePath)}' " +
            $"into '{Path.GetDirectoryName(lEditWorkItem.LWorkOutputPath)}'");

        if (lEditCrop.LWorkCropActive)
        {
            lInfoLog(
                $"Edit job '{lEditOutputName}' crop: " +
                $"left {lEditCrop.LWorkCropLeft}, top {lEditCrop.LWorkCropTop}, " +
                $"right {lEditCrop.LWorkCropRight}, bottom {lEditCrop.LWorkCropBottom}, " +
                $"rotate {lEditCrop.LWorkCropRotation}, " +
                $"hflip {lEditCrop.LWorkFlipHorizontal}, vflip {lEditCrop.LWorkFlipVertical}");
        }

        if (lEditWorkDescription.LEditVideo.LWorkVideoActive)
        {
            string lVideoSteps = string.Join(", ", lEditWorkDescription.LEditVideo.LWorkVideoSteps
                .Where(lStep => lStep.LWorkStepActive)
                .Select(LWorkVideoDiagnostic.LWorkDiagnosticRead));
            lInfoLog($"Edit job '{lEditOutputName}' video: {lVideoSteps}");
        }

        return new[] { lEditWorkItem };
    }

    public static IReadOnlyList<LWorkItem> LEditSourcesCreate(
        LWorkPriority lWorkPriority,
        IReadOnlyList<LWorkSource> lEditSources,
        LEncoding lEditOutput,
        Func<string, LSidecarEditEntry?> lSidecarRead,
        Func<string, TimeSpan> lDurationRead,
        bool lEditEqCapable,
        Guid lEditLooseBatch)
    {
        var lEditItems = new List<LWorkItem>();
        foreach (LWorkSource lEditSource in lEditSources)
        {
            string lEditSourcePath = lEditSource.LWorkSourcePath;
            LEditPlan lEditPlan = LEditSidecar.LEditSidecarRead(lEditSourcePath, lSidecarRead)
                ?? LEditPlan.LEditEmptyCreate();
            (LWorkCrop lEditCrop, LWorkVideo lEditVideo) = LEditPlan.LEditWorkResolve(lEditPlan, lEditEqCapable);
            if (LEditWorkCreate(
                    lWorkPriority,
                    lEditSourcePath,
                    lDurationRead(lEditSourcePath),
                    lEditCrop,
                    lEditVideo,
                    lEditOutput,
                    lEditSource.LWorkSourceBatch != Guid.Empty ? lEditSource.LWorkSourceBatch : lEditLooseBatch)
                is { } lEditItem)
            {
                lEditItems.Add(lEditItem);
            }
        }

        return lEditItems;
    }
}
