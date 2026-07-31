using System.IO;
using System.Windows;
using Cadroue.Media;
using Newtonsoft.Json;

namespace Cadroue.UIShell.PFlow;

public sealed partial class PFlow
{
    public void PFlowLosslesscutFind()
    {
        if (!pFlowCommandActive || lSourcePath is null || lSpool is null)
        {
            return;
        }

        IReadOnlyList<string> pLosslesscutPaths = LLosslesscut.LLosslesscutAdjacentRead(lSourcePath);
        if (pLosslesscutPaths.Count == 0)
        {
            return;
        }

        for (int pLosslesscutIndex = 0; pLosslesscutIndex < pLosslesscutPaths.Count; pLosslesscutIndex++)
        {
            string pLosslesscutPath = pLosslesscutPaths[pLosslesscutIndex];
            LLosslesscutProject pLosslesscutProject;
            try
            {
                pLosslesscutProject = LLosslesscut.LLosslesscutRead(pLosslesscutPath);
            }
            catch (Exception pLosslesscutException) when (
                pLosslesscutException is IOException
                    or UnauthorizedAccessException
                    or JsonException
                    or ArgumentException)
            {
                LTraceLog.LTraceErrorRecord("Adjacent LosslessCut project could not be read", pLosslesscutException);
                continue;
            }

            bool pLosslesscutHasNext = pLosslesscutIndex + 1 < pLosslesscutPaths.Count;
            string pLosslesscutChoiceMeaning = pLosslesscutHasNext
                ? LLocalization.LLocalizationTextRead("Flow.LosslessCut.Detect.NextChoice")
                : LLocalization.LLocalizationTextRead("Flow.LosslessCut.Detect.StopChoice");
            string pLosslesscutUnspecified = LLocalization.LLocalizationTextRead("Flow.LosslessCut.Value.NotSpecified");
            MessageBoxResult pLosslesscutChoice = MessageBox.Show(
                LLocalization.LLocalizationFormat(
                    "Flow.LosslessCut.Detect.Message",
                    pLosslesscutIndex + 1,
                    pLosslesscutPaths.Count,
                    Path.GetFileName(pLosslesscutPath),
                    File.GetLastWriteTime(pLosslesscutPath),
                    pLosslesscutProject.LLosslesscutProjectVersion?.ToString() ?? pLosslesscutUnspecified,
                    pLosslesscutProject.LLosslesscutProjectSegments.Count,
                    pLosslesscutProject.LLosslesscutProjectMediaFileName.PFlowFallbackRead(pLosslesscutUnspecified),
                    pLosslesscutChoiceMeaning),
                LLocalization.LLocalizationTextRead("Flow.LosslessCut.Detect.Title"),
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            if (pLosslesscutChoice == MessageBoxResult.Yes)
            {
                PFlowLosslesscutImport(pLosslesscutPath);
                return;
            }
        }
    }

    public void PFlowLosslesscutImport(string pLosslesscutPath)
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

        LLosslesscutProject pLosslesscutProject;
        try
        {
            pLosslesscutProject = LLosslesscut.LLosslesscutRead(pLosslesscutPath);
        }
        catch (Exception pLosslesscutException) when (
            pLosslesscutException is IOException
                or UnauthorizedAccessException
                or JsonException
                or ArgumentException)
        {
            LTraceLog.LTraceErrorRecord("LosslessCut project could not be read", pLosslesscutException);
            MessageBox.Show(
                LLocalization.LLocalizationFormat("Flow.LosslessCut.Import.ReadError", pLosslesscutException.Message),
                LLocalization.LLocalizationTextRead("Flow.LosslessCut.Import.Title"),
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        if (!LLosslesscut.LLosslesscutVersionCheck(pLosslesscutProject.LLosslesscutProjectVersion)
            && MessageBox.Show(
                LLocalization.LLocalizationFormat(
                    "Flow.LosslessCut.Import.VersionWarning",
                    pLosslesscutProject.LLosslesscutProjectVersion),
                LLocalization.LLocalizationTextRead("Flow.LosslessCut.Import.VersionTitle"),
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning) != MessageBoxResult.Yes)
        {
            return;
        }

        LLosslesscutResult pLosslesscutResult = LLosslesscut.LLosslesscutValidate(
            pLosslesscutProject,
            lSourcePath,
            lSpool.LSpoolDuration);

        if (!pLosslesscutResult.LLosslesscutResultMediaMatch
            && MessageBox.Show(
                LLocalization.LLocalizationFormat(
                    "Flow.LosslessCut.Import.MediaMismatch",
                    pLosslesscutResult.LLosslesscutResultMediaFileName,
                    Path.GetFileName(lSourcePath)),
                LLocalization.LLocalizationTextRead("Flow.LosslessCut.Import.MediaMismatchTitle"),
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning) != MessageBoxResult.Yes)
        {
            return;
        }

        if (pLosslesscutResult.LLosslesscutResultSections.Count == 0)
        {
            MessageBox.Show(
                PFlowLosslesscutFormat(pLosslesscutPath, pLosslesscutResult, false),
                LLocalization.LLocalizationTextRead("Flow.LosslessCut.Import.EmptyTitle"),
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        string pLosslesscutSummary = PFlowLosslesscutFormat(pLosslesscutPath, pLosslesscutResult, true);
        MessageBoxResult pLosslesscutMode = MessageBox.Show(
            pLosslesscutSummary
            + Environment.NewLine
            + Environment.NewLine
            + LLocalization.LLocalizationTextRead("Flow.LosslessCut.Import.ModeChoices"),
            LLocalization.LLocalizationTextRead("Flow.LosslessCut.Import.PreviewTitle"),
            MessageBoxButton.YesNoCancel,
            MessageBoxImage.Question);

        if (pLosslesscutMode == MessageBoxResult.Cancel)
        {
            return;
        }

        IReadOnlyList<LSegment> pLosslesscutImported = PFlowLosslesscutCreate(
            pLosslesscutResult.LLosslesscutResultSections,
            pLosslesscutMode == MessageBoxResult.No ? lSectionList.Count : 0);

        int pLosslesscutSelection = pLosslesscutMode == MessageBoxResult.Yes ? 0 : lSectionList.Count;
        IReadOnlyList<LSegment> pLosslesscutTarget = pLosslesscutMode == MessageBoxResult.Yes
            ? pLosslesscutImported
            : lSectionList.Concat(pLosslesscutImported).ToArray();

        PFlowSectionsSet(pLosslesscutTarget, pLosslesscutTarget.Count > 0 ? pLosslesscutSelection : null);
        LTraceLog.LTraceInfoRecord(
            $"LosslessCut project imported from '{pLosslesscutPath}': "
            + $"{pLosslesscutImported.Count} segment(s), "
            + $"{pLosslesscutResult.LLosslesscutResultIssues.Count} skipped");
    }

    private static IReadOnlyList<LSegment> PFlowLosslesscutCreate(
        IReadOnlyList<LSidecarSectionRecord> pLosslesscutSections,
        int pLosslesscutColorOffset)
    {
        int pLosslesscutPaletteCount = Math.Max(1, PSectionPalette.PSectionActiveCount);
        return pLosslesscutSections
            .Select((pLosslesscutSection, pLosslesscutIndex) => new LSegment(
                TimeSpan.FromMilliseconds(pLosslesscutSection.LSidecarStartMilliseconds),
                TimeSpan.FromMilliseconds(pLosslesscutSection.LSidecarEndMilliseconds),
                (pLosslesscutColorOffset + pLosslesscutIndex) % pLosslesscutPaletteCount,
                pLosslesscutSection.LSidecarName ?? string.Empty))
            .ToArray();
    }

    private static string PFlowLosslesscutFormat(
        string pLosslesscutPath,
        LLosslesscutResult pLosslesscutResult,
        bool pLosslesscutShowRange)
    {
        string pLosslesscutUnspecified = LLocalization.LLocalizationTextRead("Flow.LosslessCut.Value.NotSpecified");
        var pLosslesscutLines = new List<string>
        {
            LLocalization.LLocalizationFormat("Flow.LosslessCut.Summary.Project", Path.GetFileName(pLosslesscutPath)),
            LLocalization.LLocalizationFormat(
                "Flow.LosslessCut.Summary.Version",
                pLosslesscutResult.LLosslesscutResultVersion?.ToString() ?? pLosslesscutUnspecified),
            LLocalization.LLocalizationFormat(
                "Flow.LosslessCut.Summary.Media",
                pLosslesscutResult.LLosslesscutResultMediaFileName.PFlowFallbackRead(pLosslesscutUnspecified)),
            LLocalization.LLocalizationFormat("Flow.LosslessCut.Summary.Valid", pLosslesscutResult.LLosslesscutResultSections.Count),
            LLocalization.LLocalizationFormat("Flow.LosslessCut.Summary.Skipped", pLosslesscutResult.LLosslesscutResultIssues.Count)
        };

        if (pLosslesscutShowRange && pLosslesscutResult.LLosslesscutResultSections.Count > 0)
        {
            LSidecarSectionRecord pLosslesscutFirst = pLosslesscutResult.LLosslesscutResultSections[0];
            LSidecarSectionRecord pLosslesscutLast = pLosslesscutResult.LLosslesscutResultSections[^1];
            pLosslesscutLines.Add(LLocalization.LLocalizationFormat(
                "Flow.LosslessCut.Summary.Range",
                TimeSpan.FromMilliseconds(pLosslesscutFirst.LSidecarStartMilliseconds).ToString(@"hh\:mm\:ss\.fff"),
                TimeSpan.FromMilliseconds(pLosslesscutLast.LSidecarEndMilliseconds).ToString(@"hh\:mm\:ss\.fff")));
        }

        foreach (LLosslesscutIssue pLosslesscutIssue in pLosslesscutResult.LLosslesscutResultIssues.Take(5))
        {
            pLosslesscutLines.Add(LLocalization.LLocalizationFormat(
                "Flow.LosslessCut.Summary.Issue",
                pLosslesscutIssue.LLosslesscutIssueIndex + 1,
                pLosslesscutIssue.LLosslesscutIssueReason));
        }

        if (pLosslesscutResult.LLosslesscutResultIssues.Count > 5)
        {
            pLosslesscutLines.Add(LLocalization.LLocalizationFormat(
                "Flow.LosslessCut.Summary.MoreIssues",
                pLosslesscutResult.LLosslesscutResultIssues.Count - 5));
        }

        return string.Join(Environment.NewLine, pLosslesscutLines);
    }
}

internal static class PFlowLosslesscutString
{
    public static string PFlowFallbackRead(this string pLosslesscutValue, string pLosslesscutFallback) =>
        string.IsNullOrWhiteSpace(pLosslesscutValue) ? pLosslesscutFallback : pLosslesscutValue;
}
