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
                ? LLocalization.LLocalizationTextRead("Flow.LosslessCut.Detect.NextChoice")
                : LLocalization.LLocalizationTextRead("Flow.LosslessCut.Detect.StopChoice");
            string pLosslessCutUnspecified = LLocalization.LLocalizationTextRead("Flow.LosslessCut.Value.NotSpecified");
            MessageBoxResult pLosslessCutChoice = MessageBox.Show(
                LLocalization.LLocalizationFormat(
                    "Flow.LosslessCut.Detect.Message",
                    pLosslessCutIndex + 1,
                    pLosslessCutPaths.Count,
                    Path.GetFileName(pLosslessCutPath),
                    File.GetLastWriteTime(pLosslessCutPath),
                    pLosslessCutProject.LLosslessCutProjectVersion?.ToString() ?? pLosslessCutUnspecified,
                    pLosslessCutProject.LLosslessCutProjectSegments.Count,
                    pLosslessCutProject.LLosslessCutProjectMediaFileName.DefaultIfEmpty(pLosslessCutUnspecified),
                    pLosslessCutChoiceMeaning),
                LLocalization.LLocalizationTextRead("Flow.LosslessCut.Detect.Title"),
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
                LLocalization.LLocalizationTextRead("Flow.LosslessCut.Import.NoMedia"),
                LLocalization.LLocalizationTextRead("Flow.LosslessCut.Import.Title"),
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
                LLocalization.LLocalizationFormat("Flow.LosslessCut.Import.ReadError", pLosslessCutException.Message),
                LLocalization.LLocalizationTextRead("Flow.LosslessCut.Import.Title"),
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        if (!LLosslessCut.LLosslessCutVersionCheck(pLosslessCutProject.LLosslessCutProjectVersion)
            && MessageBox.Show(
                LLocalization.LLocalizationFormat(
                    "Flow.LosslessCut.Import.VersionWarning",
                    pLosslessCutProject.LLosslessCutProjectVersion),
                LLocalization.LLocalizationTextRead("Flow.LosslessCut.Import.VersionTitle"),
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
                LLocalization.LLocalizationFormat(
                    "Flow.LosslessCut.Import.MediaMismatch",
                    pLosslessCutResult.LLosslessCutResultMediaFileName,
                    Path.GetFileName(lSourcePath)),
                LLocalization.LLocalizationTextRead("Flow.LosslessCut.Import.MediaMismatchTitle"),
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning) != MessageBoxResult.Yes)
        {
            return;
        }

        if (pLosslessCutResult.LLosslessCutResultSections.Count == 0)
        {
            MessageBox.Show(
                PFlowLosslessCutSummaryCreate(pLosslessCutPath, pLosslessCutResult, false),
                LLocalization.LLocalizationTextRead("Flow.LosslessCut.Import.EmptyTitle"),
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        string pLosslessCutSummary = PFlowLosslessCutSummaryCreate(pLosslessCutPath, pLosslessCutResult, true);
        MessageBoxResult pLosslessCutMode = MessageBox.Show(
            pLosslessCutSummary
            + Environment.NewLine
            + Environment.NewLine
            + LLocalization.LLocalizationTextRead("Flow.LosslessCut.Import.ModeChoices"),
            LLocalization.LLocalizationTextRead("Flow.LosslessCut.Import.PreviewTitle"),
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
        string pLosslessCutUnspecified = LLocalization.LLocalizationTextRead("Flow.LosslessCut.Value.NotSpecified");
        var pLosslessCutLines = new List<string>
        {
            LLocalization.LLocalizationFormat("Flow.LosslessCut.Summary.Project", Path.GetFileName(pLosslessCutPath)),
            LLocalization.LLocalizationFormat(
                "Flow.LosslessCut.Summary.Version",
                pLosslessCutResult.LLosslessCutResultVersion?.ToString() ?? pLosslessCutUnspecified),
            LLocalization.LLocalizationFormat(
                "Flow.LosslessCut.Summary.Media",
                pLosslessCutResult.LLosslessCutResultMediaFileName.DefaultIfEmpty(pLosslessCutUnspecified)),
            LLocalization.LLocalizationFormat("Flow.LosslessCut.Summary.Valid", pLosslessCutResult.LLosslessCutResultSections.Count),
            LLocalization.LLocalizationFormat("Flow.LosslessCut.Summary.Skipped", pLosslessCutResult.LLosslessCutResultIssues.Count)
        };

        if (pLosslessCutShowRange && pLosslessCutResult.LLosslessCutResultSections.Count > 0)
        {
            LSidecarSectionRecord pLosslessCutFirst = pLosslessCutResult.LLosslessCutResultSections[0];
            LSidecarSectionRecord pLosslessCutLast = pLosslessCutResult.LLosslessCutResultSections[^1];
            pLosslessCutLines.Add(LLocalization.LLocalizationFormat(
                "Flow.LosslessCut.Summary.Range",
                TimeSpan.FromMilliseconds(pLosslessCutFirst.StartMilliseconds).ToString(@"hh\:mm\:ss\.fff"),
                TimeSpan.FromMilliseconds(pLosslessCutLast.EndMilliseconds).ToString(@"hh\:mm\:ss\.fff")));
        }

        foreach (LLosslessCutIssue pLosslessCutIssue in pLosslessCutResult.LLosslessCutResultIssues.Take(5))
        {
            pLosslessCutLines.Add(LLocalization.LLocalizationFormat(
                "Flow.LosslessCut.Summary.Issue",
                pLosslessCutIssue.LLosslessCutIssueIndex + 1,
                pLosslessCutIssue.LLosslessCutIssueReason));
        }

        if (pLosslessCutResult.LLosslessCutResultIssues.Count > 5)
        {
            pLosslessCutLines.Add(LLocalization.LLocalizationFormat(
                "Flow.LosslessCut.Summary.MoreIssues",
                pLosslessCutResult.LLosslessCutResultIssues.Count - 5));
        }

        return string.Join(Environment.NewLine, pLosslessCutLines);
    }
}

internal static class PFlowLosslessCutString
{
    public static string DefaultIfEmpty(this string pLosslessCutValue, string pLosslessCutFallback) =>
        string.IsNullOrWhiteSpace(pLosslessCutValue) ? pLosslessCutFallback : pLosslessCutValue;
}
