using System;
using System.Collections.Generic;
using System.IO;
using Cadroue.Core;

namespace Cadroue.Application;

public static class LSalvage
{
    public static IReadOnlyList<LSalvageOutput> LSalvagePlanCreate(
        IReadOnlyList<LSalvageSpan> lSalvageSpans,
        LSalvageMode lSalvageMode,
        string lSalvageSourcePath,
        LEncoding lSalvageOutput)
    {
        if (lSalvageSpans.Count == 0)
        {
            return Array.Empty<LSalvageOutput>();
        }

        string lSalvageFolder = lSalvageOutput.LEncodingFolderRead(lSalvageSourcePath);
        string lSalvageStem = Path.GetFileNameWithoutExtension(lSalvageSourcePath);

        if (lSalvageMode == LSalvageMode.LSalvageModeRejoin || lSalvageSpans.Count == 1)
        {
            var lSalvageWhole = new LSalvageSpan(
                lSalvageSpans[0].LSalvageSpanOrigin,
                lSalvageSpans[lSalvageSpans.Count - 1].LSalvageSpanLimit);
            string lSalvageName = LSalvageNameFormat(lSalvageStem, lSalvageFolder, lSalvageSourcePath);
            return new[] { new LSalvageOutput(lSalvageName, lSalvageWhole) };
        }

        bool lSalvageTokened = LSalvageTokenCheck(lSalvageOutput.LEncodingNamePattern);
        var lSalvageOutputs = new List<LSalvageOutput>(lSalvageSpans.Count);
        var lSalvageTaken = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (int lSalvageIndex = 0; lSalvageIndex < lSalvageSpans.Count; lSalvageIndex++)
        {
            LSalvageSpan lSalvageSpan = lSalvageSpans[lSalvageIndex];
            int lSalvageNumber = lSalvageIndex + 1;
            string lSalvagePartStem = lSalvageTokened
                ? LSalvageStemResolve(lSalvageOutput.LEncodingNamePattern, lSalvageStem, lSalvageNumber, lSalvageSpan)
                : $"{lSalvageStem} ({lSalvageNumber})";
            string lSalvageName = LSalvageNameResolve(lSalvagePartStem, lSalvageFolder, lSalvageSourcePath, lSalvageTaken);
            lSalvageOutputs.Add(new LSalvageOutput(lSalvageName, lSalvageSpan));
        }

        return lSalvageOutputs;
    }

    private static bool LSalvageTokenCheck(string lSalvagePattern) =>
        !string.IsNullOrEmpty(lSalvagePattern)
        && (lSalvagePattern.Contains("{SectionNumber}", StringComparison.OrdinalIgnoreCase)
            || lSalvagePattern.Contains("{SectionName}", StringComparison.OrdinalIgnoreCase));

    private static string LSalvageStemResolve(string lSalvagePattern, string lSalvageStem, int lSalvageNumber, LSalvageSpan lSalvageSpan)
    {
        TimeSpan lSalvageDuration = lSalvageSpan.LSalvageSpanLimit - lSalvageSpan.LSalvageSpanOrigin;
        string lSalvageResolved = lSalvagePattern
            .Replace("{Prefix}", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("{Suffix}", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("{OriginalName}", lSalvageStem, StringComparison.OrdinalIgnoreCase)
            .Replace("{SectionNumber}", lSalvageNumber.ToString("D2"), StringComparison.OrdinalIgnoreCase)
            .Replace("{SectionName}", "Salvage", StringComparison.OrdinalIgnoreCase)
            .Replace("{SectionStart}", LEncoding.LEncodingTimeFormat(lSalvageSpan.LSalvageSpanOrigin), StringComparison.OrdinalIgnoreCase)
            .Replace("{SectionEnd}", LEncoding.LEncodingTimeFormat(lSalvageSpan.LSalvageSpanLimit), StringComparison.OrdinalIgnoreCase)
            .Replace("{SectionDuration}", LEncoding.LEncodingTimeFormat(lSalvageDuration), StringComparison.OrdinalIgnoreCase)
            .Replace("{Date}", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("{Time}", string.Empty, StringComparison.OrdinalIgnoreCase);

        return LEncoding.LEncodingShorten(lSalvageResolved);
    }

    private static string LSalvageNameResolve(string lSalvageStem, string lSalvageFolder, string lSalvageSourcePath, HashSet<string> lSalvageTaken)
    {
        string lSalvageName = LSalvageNameFormat(lSalvageStem, lSalvageFolder, lSalvageSourcePath);
        int lSalvageAttempt = 2;
        while (!lSalvageTaken.Add(lSalvageName))
        {
            lSalvageName = LSalvageNameFormat($"{lSalvageStem} ({lSalvageAttempt})", lSalvageFolder, lSalvageSourcePath);
            lSalvageAttempt++;
        }

        return lSalvageName;
    }

    private static string LSalvageNameFormat(string lSalvageStem, string lSalvageFolder, string lSalvageSourcePath)
    {
        string lSalvageBaseName = LSalvageNameNormalize(lSalvageStem);
        string lSalvageExtension = Path.GetExtension(lSalvageSourcePath).TrimStart('.');
        string lSalvageSuffix = string.IsNullOrWhiteSpace(lSalvageExtension) ? string.Empty : $".{lSalvageExtension}";
        string lSalvageFileName = $"{lSalvageBaseName}{lSalvageSuffix}";

        // Fix/salvage keeps the source container: the destination extension mirrors the
        // source, never the export preset's container, and must never equal the source path.
        return LSalvageSourceMatch(Path.Combine(lSalvageFolder, lSalvageFileName), lSalvageSourcePath)
            ? $"{lSalvageBaseName}_fix{lSalvageSuffix}"
            : lSalvageFileName;
    }

    private static bool LSalvageSourceMatch(string lSalvageOutputPath, string lSalvageSourcePath)
    {
        try
        {
            return string.Equals(
                Path.GetFullPath(lSalvageOutputPath),
                Path.GetFullPath(lSalvageSourcePath),
                StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception lSalvageError) when (lSalvageError is ArgumentException or IOException or NotSupportedException)
        {
            return string.Equals(lSalvageOutputPath, lSalvageSourcePath, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static string LSalvageNameNormalize(string lSalvageName)
    {
        char[] lSalvageInvalidChars = Path.GetInvalidFileNameChars();
        var lSalvageBuilder = new System.Text.StringBuilder(lSalvageName.Length);
        foreach (char lSalvageChar in lSalvageName)
        {
            lSalvageBuilder.Append(Array.IndexOf(lSalvageInvalidChars, lSalvageChar) >= 0 ? '_' : lSalvageChar);
        }

        string lSalvageTrimmed = lSalvageBuilder.ToString().Trim();
        return lSalvageTrimmed.Length == 0 ? "output" : lSalvageTrimmed;
    }
}
