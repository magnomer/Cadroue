using System.IO;
using Cadroue.Core;

namespace Cadroue.Application;

public static partial class LFix
{
    public static LWorkItem LFixWorkCreate(
        LWorkPriority lWorkPriority,
        string lFixSourcePath,
        TimeSpan lFixDuration,
        LWorkCrop lFixCrop,
        LWorkVideo lFixVideo,
        LEncoding lFixOutput,
        Guid lFixBatchId)
    {
        string lFixFolder = lFixOutput.LEncodingFolderRead(lFixSourcePath);
        string lFixOutputName = LFixNameCreate(lFixOutput, lFixSourcePath, lFixFolder, lFixDuration);

        return new LWorkItem(
            lFixBatchId,
            LWorkKind.LWorkKindFix,
            lWorkPriority,
            lFixSourcePath,
            TimeSpan.Zero,
            lFixDuration,
            lFixOutputName,
            Path.Combine(lFixFolder, lFixOutputName),
            lFixOutput,
            lWorkCrop: lFixCrop,
            lWorkVideo: lFixVideo);
    }

    public static IReadOnlyList<LWorkItem> LFixItemsCreate(
        LWorkPriority lWorkPriority,
        LFixWorkDescription lFixWorkDescription,
        string lFixTab,
        Action<string> lInfoLog,
        Action<string> lErrorLog,
        Guid lFixBatchId = default)
    {
        LWorkCrop lFixCrop = lFixWorkDescription.LFixCrop;
        if (string.IsNullOrWhiteSpace(lFixWorkDescription.LFixSourcePath))
        {
            lErrorLog("Fix not queued: no source file is open");
            return Array.Empty<LWorkItem>();
        }

        LWorkItem lFixWorkItem = LFixWorkCreate(
            lWorkPriority,
            lFixWorkDescription.LFixSourcePath,
            lFixWorkDescription.LFixDuration,
            lFixCrop,
            lFixWorkDescription.LFixVideo,
            lFixWorkDescription.LFixOutput,
            lFixBatchId == Guid.Empty ? Guid.NewGuid() : lFixBatchId);

        lFixWorkItem.LWorkTab = lFixTab;
        string lFixOutputName = lFixWorkItem.LWorkOutputName;
        lInfoLog(
            $"Fix built job '{lFixOutputName}' at {lWorkPriority} from " +
            $"'{Path.GetFileName(lFixWorkItem.LWorkSourcePath)}' " +
            $"into '{Path.GetDirectoryName(lFixWorkItem.LWorkOutputPath)}'");

        if (lFixCrop.LWorkCropActive)
        {
            lInfoLog(
                $"Fix job '{lFixOutputName}' crop: " +
                $"left {lFixCrop.LWorkCropLeft}, top {lFixCrop.LWorkCropTop}, " +
                $"right {lFixCrop.LWorkCropRight}, bottom {lFixCrop.LWorkCropBottom}, " +
                $"rotate {lFixCrop.LWorkCropRotation}, " +
                $"hflip {lFixCrop.LWorkFlipHorizontal}, vflip {lFixCrop.LWorkFlipVertical}");
        }

        if (lFixWorkDescription.LFixVideo.LWorkVideoActive)
        {
            string lVideoSteps = string.Join(", ", lFixWorkDescription.LFixVideo.LWorkVideoSteps
                .Where(lStep => lStep.LWorkStepActive)
                .Select(lStep => lStep.LWorkDiagnosticRead()));
            lInfoLog($"Fix job '{lFixOutputName}' video: {lVideoSteps}");
        }

        return new[] { lFixWorkItem };
    }


    private static string LFixNameCreate(LEncoding lFixOutput, string lFixSourcePath, string lFixFolder, TimeSpan lFixDuration)
    {
        string lFixSourceStem = Path.GetFileNameWithoutExtension(lFixSourcePath);
        string lFixPattern = string.IsNullOrWhiteSpace(lFixOutput.LEncodingNamePattern)
            ? "{OriginalName}"
            : lFixOutput.LEncodingNamePattern;

        DateTimeOffset lFixStamp = DateTimeOffset.Now;
        string lFixStem = lFixPattern
            .Replace("{Prefix}", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("{Suffix}", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("{OriginalName}", lFixSourceStem, StringComparison.OrdinalIgnoreCase)
            .Replace("{SectionNumber}", "01", StringComparison.OrdinalIgnoreCase)
            .Replace("{SectionName}", "Fix", StringComparison.OrdinalIgnoreCase)
            .Replace("{SectionStart}", LEncoding.LEncodingTimeFormat(TimeSpan.Zero), StringComparison.OrdinalIgnoreCase)
            .Replace("{SectionEnd}", LEncoding.LEncodingTimeFormat(lFixDuration), StringComparison.OrdinalIgnoreCase)
            .Replace("{SectionDuration}", LEncoding.LEncodingTimeFormat(lFixDuration), StringComparison.OrdinalIgnoreCase)
            .Replace("{Date}", lFixStamp.ToString("yyyy-MM-dd"), StringComparison.OrdinalIgnoreCase)
            .Replace("{Time}", lFixStamp.ToString("HHmmss"), StringComparison.OrdinalIgnoreCase);

        lFixStem = LEncoding.LEncodingShorten(lFixStem);

        string lFixBaseName = LFixNameNormalize(lFixStem);
        string lFixFileName = LFixNameFormat(lFixOutput, lFixBaseName, lFixSourcePath);
        return LFixSourceMatch(Path.Combine(lFixFolder, lFixFileName), lFixSourcePath)
            ? LFixNameFormat(lFixOutput, $"{lFixBaseName}_fix", lFixSourcePath)
            : lFixFileName;
    }

    private static string LFixNameFormat(LEncoding lFixOutput, string lFixBaseName, string lFixSourcePath)
    {
        string lFixExtension = lFixOutput.LEncodingExtensionResolve(lFixSourcePath);
        return string.IsNullOrWhiteSpace(lFixExtension)
            ? lFixBaseName
            : $"{lFixBaseName}.{lFixExtension}";
    }

    private static bool LFixSourceMatch(string lFixOutputPath, string lFixSourcePath)
    {
        try
        {
            return string.Equals(
                Path.GetFullPath(lFixOutputPath),
                Path.GetFullPath(lFixSourcePath),
                StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception lFixError) when (lFixError is ArgumentException or IOException or NotSupportedException)
        {
            return string.Equals(lFixOutputPath, lFixSourcePath, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static string LFixNameNormalize(string lFixName)
    {
        char[] lFixInvalidChars = Path.GetInvalidFileNameChars();
        var lFixBuilder = new System.Text.StringBuilder(lFixName.Length);
        foreach (char lFixChar in lFixName)
        {
            lFixBuilder.Append(Array.IndexOf(lFixInvalidChars, lFixChar) >= 0 ? '_' : lFixChar);
        }

        string lFixTrimmed = lFixBuilder.ToString().Trim();
        return lFixTrimmed.Length == 0 ? "output" : lFixTrimmed;
    }
}
