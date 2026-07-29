using System.IO;
using System.Windows;
using Cadroue.Media;
using Newtonsoft.Json;

namespace Cadroue.UIShell.PFlow;

public sealed partial class PFlow
{
    public void PFlowLosslessCutDetect()
    {
        if (!pFlowCommandActive || lSourcePath is null || lSpool is null)
        {
            return;
        }

        IReadOnlyList<string> pLosslessCutPaths = LLosslessCut.LLosslessCutAdjacentRead(lSourcePath);
        if (pLosslessCutPaths.Count == 0)
        {
            return;
        }

        for (int pLosslessCutIndex = 0; pLosslessCutIndex < pLosslessCutPaths.Count; pLosslessCutIndex++)
        {
            string pLosslessCutPath = pLosslessCutPaths[pLosslessCutIndex];
            LLosslessCutProject pLosslessCutProject;
            try
            {
                pLosslessCutProject = LLosslessCut.LLosslessCutRead(pLosslessCutPath);
            }
            catch (Exception pLosslessCutException) when (
                pLosslessCutException is IOException
                    or UnauthorizedAccessException
                    or JsonException
                    or ArgumentException)
            {
                LAppLog.LError("Adjacent LosslessCut project could not be read", pLosslessCutException);
                continue;
            }

            bool pLosslessCutHasNext = pLosslessCutIndex + 1 < pLosslessCutPaths.Count;
            string pLosslessCutChoiceMeaning = pLosslessCutHasNext
                ? "No: Review the next matching project"
                : "No: Do not import";
            MessageBoxResult pLosslessCutChoice = MessageBox.Show(
                $"A LosslessCut project for the open media was found"
                + $" ({pLosslessCutIndex + 1} of {pLosslessCutPaths.Count}):\n\n"
                + $"Project: {Path.GetFileName(pLosslessCutPath)}\n"
                + $"Modified: {File.GetLastWriteTime(pLosslessCutPath):g}\n"
                + $"Version: {pLosslessCutProject.LLosslessCutProjectVersion?.ToString() ?? "not specified"}\n"
                + $"Segments: {pLosslessCutProject.LLosslessCutProjectSegments.Count}\n"
                + $"Source: {pLosslessCutProject.LLosslessCutProjectMediaFileName.DefaultIfEmpty("not specified")}\n\n"
                + "Yes: Review this project's segments for import\n"
                + pLosslessCutChoiceMeaning,
                "LosslessCut project found",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            if (pLosslessCutChoice == MessageBoxResult.Yes)
            {
                PFlowLosslessCutImport(pLosslessCutPath);
                return;
            }
        }
    }

    public void PFlowLosslessCutImport(string pLosslessCutPath)
    {
        if (!pFlowCommandActive || lSourcePath is null || lSpool is null)
        {
            MessageBox.Show(
                "Open a media file before importing a LosslessCut project.",
                "Import LosslessCut project",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        LLosslessCutProject pLosslessCutProject;
        try
        {
            pLosslessCutProject = LLosslessCut.LLosslessCutRead(pLosslessCutPath);
        }
        catch (Exception pLosslessCutException) when (
            pLosslessCutException is IOException
                or UnauthorizedAccessException
                or JsonException
                or ArgumentException)
        {
            LAppLog.LError("LosslessCut project could not be read", pLosslessCutException);
            MessageBox.Show(
                $"That LosslessCut project could not be read.\n\n{pLosslessCutException.Message}",
                "Import LosslessCut project",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        if (!LLosslessCut.LLosslessCutVersionCheck(pLosslessCutProject.LLosslessCutProjectVersion)
            && MessageBox.Show(
                $"This LosslessCut project uses version {pLosslessCutProject.LLosslessCutProjectVersion}, "
                + "which Cadroue does not explicitly support. Continue with the recognized fields?",
                "LosslessCut project version",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning) != MessageBoxResult.Yes)
        {
            return;
        }

        LLosslessCutResult pLosslessCutResult = LLosslessCut.LLosslessCutValidate(
            pLosslessCutProject,
            lSourcePath,
            lSpool.LSpoolDuration);

        if (!pLosslessCutResult.LLosslessCutResultMediaMatch
            && MessageBox.Show(
                $"The LosslessCut project names this media:\n\n"
                + $"{pLosslessCutResult.LLosslessCutResultMediaFileName}\n\n"
                + $"Cadroue currently has this media open:\n\n{Path.GetFileName(lSourcePath)}\n\n"
                + "Import the segments anyway?",
                "Media name does not match",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning) != MessageBoxResult.Yes)
        {
            return;
        }

        if (pLosslessCutResult.LLosslessCutResultSections.Count == 0)
        {
            MessageBox.Show(
                PFlowLosslessCutSummaryCreate(pLosslessCutPath, pLosslessCutResult, false),
                "No LosslessCut segments to import",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        string pLosslessCutSummary = PFlowLosslessCutSummaryCreate(pLosslessCutPath, pLosslessCutResult, true);
        MessageBoxResult pLosslessCutMode = MessageBox.Show(
            pLosslessCutSummary
            + "\n\nYes: Replace the current Cadroue segments"
            + "\nNo: Append to the current Cadroue segments"
            + "\nCancel: Do not import",
            "Preview LosslessCut import",
            MessageBoxButton.YesNoCancel,
            MessageBoxImage.Question);

        if (pLosslessCutMode == MessageBoxResult.Cancel)
        {
            return;
        }

        IReadOnlyList<LSegment> pLosslessCutImported = PFlowLosslessCutSegmentsCreate(
            pLosslessCutResult.LLosslessCutResultSections,
            pLosslessCutMode == MessageBoxResult.No ? lSectionList.Count : 0);

        int pLosslessCutSelection = pLosslessCutMode == MessageBoxResult.Yes ? 0 : lSectionList.Count;
        IReadOnlyList<LSegment> pLosslessCutTarget = pLosslessCutMode == MessageBoxResult.Yes
            ? pLosslessCutImported
            : lSectionList.Concat(pLosslessCutImported).ToArray();

        PFlowSectionsSet(pLosslessCutTarget, pLosslessCutTarget.Count > 0 ? pLosslessCutSelection : null);
        LAppLog.LInfo(
            $"LosslessCut project imported from '{pLosslessCutPath}': "
            + $"{pLosslessCutImported.Count} segment(s), "
            + $"{pLosslessCutResult.LLosslessCutResultIssues.Count} skipped");
    }

    private static IReadOnlyList<LSegment> PFlowLosslessCutSegmentsCreate(
        IReadOnlyList<LSidecarSectionRecord> pLosslessCutSections,
        int pLosslessCutColorOffset)
    {
        int pLosslessCutPaletteCount = Math.Max(1, PSectionPalette.PSectionActiveCount);
        return pLosslessCutSections
            .Select((pLosslessCutSection, pLosslessCutIndex) => new LSegment(
                TimeSpan.FromMilliseconds(pLosslessCutSection.StartMilliseconds),
                TimeSpan.FromMilliseconds(pLosslessCutSection.EndMilliseconds),
                (pLosslessCutColorOffset + pLosslessCutIndex) % pLosslessCutPaletteCount,
                pLosslessCutSection.Name ?? string.Empty))
            .ToArray();
    }

    private static string PFlowLosslessCutSummaryCreate(
        string pLosslessCutPath,
        LLosslessCutResult pLosslessCutResult,
        bool pLosslessCutShowRange)
    {
        var pLosslessCutLines = new List<string>
        {
            $"Project: {Path.GetFileName(pLosslessCutPath)}",
            $"Version: {pLosslessCutResult.LLosslessCutResultVersion?.ToString() ?? "not specified"}",
            $"Media: {pLosslessCutResult.LLosslessCutResultMediaFileName.DefaultIfEmpty("not specified")}",
            $"Valid segments: {pLosslessCutResult.LLosslessCutResultSections.Count}",
            $"Skipped segments: {pLosslessCutResult.LLosslessCutResultIssues.Count}"
        };

        if (pLosslessCutShowRange && pLosslessCutResult.LLosslessCutResultSections.Count > 0)
        {
            LSidecarSectionRecord pLosslessCutFirst = pLosslessCutResult.LLosslessCutResultSections[0];
            LSidecarSectionRecord pLosslessCutLast = pLosslessCutResult.LLosslessCutResultSections[^1];
            pLosslessCutLines.Add(
                $"Imported range: {TimeSpan.FromMilliseconds(pLosslessCutFirst.StartMilliseconds):hh\\:mm\\:ss\\.fff}"
                + $" – {TimeSpan.FromMilliseconds(pLosslessCutLast.EndMilliseconds):hh\\:mm\\:ss\\.fff}");
        }

        foreach (LLosslessCutIssue pLosslessCutIssue in pLosslessCutResult.LLosslessCutResultIssues.Take(5))
        {
            pLosslessCutLines.Add($"Segment {pLosslessCutIssue.LLosslessCutIssueIndex + 1}: {pLosslessCutIssue.LLosslessCutIssueReason}");
        }

        if (pLosslessCutResult.LLosslessCutResultIssues.Count > 5)
        {
            pLosslessCutLines.Add($"…and {pLosslessCutResult.LLosslessCutResultIssues.Count - 5} more issue(s)");
        }

        return string.Join(Environment.NewLine, pLosslessCutLines);
    }
}

internal static class PFlowLosslessCutString
{
    public static string DefaultIfEmpty(this string pLosslessCutValue, string pLosslessCutFallback) =>
        string.IsNullOrWhiteSpace(pLosslessCutValue) ? pLosslessCutFallback : pLosslessCutValue;
}
