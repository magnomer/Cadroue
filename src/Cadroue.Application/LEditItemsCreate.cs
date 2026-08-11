using System.IO;
using Cadroue.Core;

namespace Cadroue.Application;

public static partial class LEdit
{
    public static LWorkItem LEditWorkCreate(
        LWorkPriority lWorkPriority,
        string lEditSourcePath,
        TimeSpan lEditDuration,
        LWorkCrop lEditCrop,
        LWorkVideo lEditVideo,
        LEncoding lEditOutput,
        Guid lEditBatchId)
    {
        string lEditFolder = lEditOutput.LEncodingFolderRead(lEditSourcePath);
        string lEditOutputName = LEditNameCreate(lEditOutput, lEditSourcePath, lEditFolder);

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

        LWorkItem lEditWorkItem = LEditWorkCreate(
            lWorkPriority,
            lEditWorkDescription.LEditSourcePath,
            lEditWorkDescription.LEditDuration,
            lEditCrop,
            lEditWorkDescription.LEditVideo,
            lEditWorkDescription.LEditOutput,
            lEditBatchId == Guid.Empty ? Guid.NewGuid() : lEditBatchId);

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
                .Select(lStep => lStep.LWorkDiagnosticRead()));
            lInfoLog($"Edit job '{lEditOutputName}' video: {lVideoSteps}");
        }

        return new[] { lEditWorkItem };
    }


    private static string LEditNameCreate(LEncoding lEditOutput, string lEditSourcePath, string lEditFolder)
    {
        string lEditSourceStem = Path.GetFileNameWithoutExtension(lEditSourcePath);
        string lEditPattern = string.IsNullOrWhiteSpace(lEditOutput.LEncodingNamePattern)
            ? "{OriginalName}"
            : lEditOutput.LEncodingNamePattern;

        DateTimeOffset lEditStamp = DateTimeOffset.Now;
        string lEditStem = lEditPattern
            .Replace("{Prefix}", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("{Suffix}", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("{OriginalName}", lEditSourceStem, StringComparison.OrdinalIgnoreCase)
            .Replace("{SectionNumber}", "01", StringComparison.OrdinalIgnoreCase)
            .Replace("{SectionName}", "Edit", StringComparison.OrdinalIgnoreCase)
            .Replace("{Date}", lEditStamp.ToString("yyyy-MM-dd"), StringComparison.OrdinalIgnoreCase)
            .Replace("{Time}", lEditStamp.ToString("HHmmss"), StringComparison.OrdinalIgnoreCase);

        lEditStem = LEncoding.LEncodingShorten(lEditStem);

        string lEditBaseName = LEditNameNormalize(lEditStem);
        string lEditFileName = LEditNameFormat(lEditOutput, lEditBaseName, lEditSourcePath);
        return LEditSourceMatch(Path.Combine(lEditFolder, lEditFileName), lEditSourcePath)
            ? LEditNameFormat(lEditOutput, $"{lEditBaseName}_edit", lEditSourcePath)
            : lEditFileName;
    }

    private static string LEditNameFormat(LEncoding lEditOutput, string lEditBaseName, string lEditSourcePath)
    {
        string lEditExtension = lEditOutput.LEncodingExtensionResolve(lEditSourcePath);
        return string.IsNullOrWhiteSpace(lEditExtension)
            ? lEditBaseName
            : $"{lEditBaseName}.{lEditExtension}";
    }

    private static bool LEditSourceMatch(string lEditOutputPath, string lEditSourcePath)
    {
        try
        {
            return string.Equals(
                Path.GetFullPath(lEditOutputPath),
                Path.GetFullPath(lEditSourcePath),
                StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception lEditError) when (lEditError is ArgumentException or IOException or NotSupportedException)
        {
            return string.Equals(lEditOutputPath, lEditSourcePath, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static string LEditNameNormalize(string lEditName)
    {
        char[] lEditInvalidChars = Path.GetInvalidFileNameChars();
        var lEditBuilder = new System.Text.StringBuilder(lEditName.Length);
        foreach (char lEditChar in lEditName)
        {
            lEditBuilder.Append(Array.IndexOf(lEditInvalidChars, lEditChar) >= 0 ? '_' : lEditChar);
        }

        string lEditTrimmed = lEditBuilder.ToString().Trim();
        return lEditTrimmed.Length == 0 ? "output" : lEditTrimmed;
    }
}
