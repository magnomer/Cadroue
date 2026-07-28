using System.IO;
using Cadroue.Core;
using Cadroue.UIShell;

namespace Cadroue.UIShell.PMainArea;

public static partial class LEdit
{
    public static int LEditInterpret(
        LWorkPriority lWorkPriority,
        LEditWorkDescription lEditWorkDescription)
    {
        string? lEditSourcePath = lEditWorkDescription.LEditSourcePath;
        if (string.IsNullOrWhiteSpace(lEditSourcePath))
        {
            LAppLog.LError("Edit not queued: no source file is open");
            return 0;
        }

        if (lEditWorkDescription.LEditDuration <= TimeSpan.Zero)
        {
            LAppLog.LError($"Edit not queued for '{Path.GetFileName(lEditSourcePath)}': media duration is unknown");
            return 0;
        }

        LWorkOutput lEditOutput = lEditWorkDescription.LEditOutput;
        LWorkCrop lEditCrop = lEditWorkDescription.LEditCrop;
        string lEditFolder = lEditOutput.LWorkFolderRead(lEditSourcePath);
        string lEditOutputName = LEditNameCreate(lEditOutput, lEditSourcePath);

        var lEditWorkItem = new LWorkItem(
            Guid.NewGuid(),
            LWorkKind.LWorkKindEdit,
            lWorkPriority,
            lEditSourcePath,
            TimeSpan.Zero,
            lEditWorkDescription.LEditDuration,
            lEditOutputName,
            Path.Combine(lEditFolder, lEditOutputName),
            lEditOutput,
            lWorkCrop: lEditCrop);

        int lEditAdded = LSchedule.LScheduleCurrent.LScheduleAdd([lEditWorkItem]);
        LAppLog.LInfo(
            $"Edit queued {lEditAdded} job(s) at {lWorkPriority} from '{Path.GetFileName(lEditSourcePath)}' " +
            $"into '{lEditFolder}'");

        if (lEditCrop.LWorkCropActive)
        {
            LAppLog.LInfo(
                $"Edit job '{lEditOutputName}' crop: " +
                $"left {lEditCrop.LWorkCropLeft}, top {lEditCrop.LWorkCropTop}, " +
                $"right {lEditCrop.LWorkCropRight}, bottom {lEditCrop.LWorkCropBottom}, " +
                $"rotate {lEditCrop.LWorkCropRotation}, " +
                $"hflip {lEditCrop.LWorkCropFlipHorizontal}, vflip {lEditCrop.LWorkCropFlipVertical}");
        }

        return lEditAdded;
    }


    private static string LEditNameCreate(LWorkOutput lEditOutput, string lEditSourcePath)
    {
        string lEditSourceStem = Path.GetFileNameWithoutExtension(lEditSourcePath);
        string lEditPattern = string.IsNullOrWhiteSpace(lEditOutput.LWorkOutputNamePattern)
            ? "{OriginalName}"
            : lEditOutput.LWorkOutputNamePattern;

        DateTimeOffset lEditStamp = DateTimeOffset.Now;
        string lEditStem = lEditPattern
            .Replace("{Prefix}", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("{Suffix}", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("{OriginalName}", lEditSourceStem, StringComparison.OrdinalIgnoreCase)
            .Replace("{SectionNumber}", "01", StringComparison.OrdinalIgnoreCase)
            .Replace("{SectionName}", "Edit", StringComparison.OrdinalIgnoreCase)
            .Replace("{Date}", lEditStamp.ToString("yyyy-MM-dd"), StringComparison.OrdinalIgnoreCase)
            .Replace("{Time}", lEditStamp.ToString("HHmmss"), StringComparison.OrdinalIgnoreCase);

        string lEditBaseName = LEditNameSanitize(lEditStem);
        if (string.Equals(lEditBaseName, lEditSourceStem, StringComparison.OrdinalIgnoreCase))
        {
            lEditBaseName = $"{lEditBaseName}_edit";
        }

        return string.IsNullOrWhiteSpace(lEditOutput.LWorkOutputExtension)
            ? lEditBaseName
            : $"{lEditBaseName}.{lEditOutput.LWorkOutputExtension}";
    }

    private static string LEditNameSanitize(string lEditName)
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
