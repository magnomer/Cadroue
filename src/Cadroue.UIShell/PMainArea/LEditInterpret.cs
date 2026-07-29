using System.IO;
using Cadroue.Core;
using Cadroue.UIShell;

namespace Cadroue.UIShell.PMainArea;

public static partial class LEdit
{
    internal static LWorkItem LEditWorkCreate(
        LWorkPriority lWorkPriority,
        string lEditSourcePath,
        TimeSpan lEditDuration,
        LWorkCrop lEditCrop,
        LWorkOutput lEditOutput)
    {
        string lEditFolder = lEditOutput.LWorkFolderRead(lEditSourcePath);
        string lEditOutputName = LEditNameCreate(lEditOutput, lEditSourcePath, lEditFolder);

        return new LWorkItem(
            Guid.NewGuid(),
            LWorkKind.LWorkKindEdit,
            lWorkPriority,
            lEditSourcePath,
            TimeSpan.Zero,
            lEditDuration,
            lEditOutputName,
            Path.Combine(lEditFolder, lEditOutputName),
            lEditOutput,
            lWorkCrop: lEditCrop);
    }

    public static int LEditInterpret(
        LWorkPriority lWorkPriority,
        LEditWorkDescription lEditWorkDescription)
    {
        LWorkCrop lEditCrop = lEditWorkDescription.LEditCrop;
        if (string.IsNullOrWhiteSpace(lEditWorkDescription.LEditSourcePath))
        {
            LAppLog.LError("Edit not queued: no source file is open");
            return 0;
        }

        LWorkItem lEditWorkItem = LEditWorkCreate(
            lWorkPriority,
            lEditWorkDescription.LEditSourcePath,
            lEditWorkDescription.LEditDuration,
            lEditCrop,
            lEditWorkDescription.LEditOutput);

        string lEditSourcePath = lEditWorkItem.LWorkSourcePath;
        string lEditOutputName = lEditWorkItem.LWorkOutputName;
        int lEditAdded = LSchedule.LScheduleCurrent.LScheduleAdd([lEditWorkItem]);
        LAppLog.LInfo(
            $"Edit queued {lEditAdded} job(s) at {lWorkPriority} from '{Path.GetFileName(lEditSourcePath)}' " +
            $"into '{Path.GetDirectoryName(lEditWorkItem.LWorkOutputPath)}'");

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


    private static string LEditNameCreate(LWorkOutput lEditOutput, string lEditSourcePath, string lEditFolder)
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
        string lEditFileName = LEditNameFormat(lEditOutput, lEditBaseName, lEditSourcePath);
        return LEditSourceMatch(Path.Combine(lEditFolder, lEditFileName), lEditSourcePath)
            ? LEditNameFormat(lEditOutput, $"{lEditBaseName}_edit", lEditSourcePath)
            : lEditFileName;
    }

    private static string LEditNameFormat(LWorkOutput lEditOutput, string lEditBaseName, string lEditSourcePath)
    {
        string lEditExtension = lEditOutput.LWorkExtensionResolve(lEditSourcePath);
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
