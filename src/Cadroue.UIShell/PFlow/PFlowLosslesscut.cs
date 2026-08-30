using System.IO;
using Cadroue.UIShell.PSShared;
using System.Windows;
using Cadroue.Core;
using Cadroue.Media;
using Newtonsoft.Json;

using Cadroue.Infrastructure;


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

            string pLosslesscutUnspecified = LLocalization.LLocalizationTextRead("Flow.LosslessCut.Value.NotSpecified");
            bool pLosslesscutChoice = PSDecision.PSDecisionConfirm(
                Window.GetWindow(this),
                LLocalization.LLocalizationTextRead("Flow.LosslessCut.Detect.Title"),
                LLocalization.LLocalizationFormat(
                    "Flow.LosslessCut.Detect.Message",
                    pLosslesscutIndex + 1,
                    pLosslesscutPaths.Count,
                    Path.GetFileName(pLosslesscutPath),
                    File.GetLastWriteTime(pLosslesscutPath),
                    pLosslesscutProject.LLosslesscutProjectVersion?.ToString() ?? pLosslesscutUnspecified,
                    pLosslesscutProject.LLosslesscutProjectSegments.Count,
                    pLosslesscutProject.LLosslesscutProjectMedia.PFlowFallbackRead(pLosslesscutUnspecified)),
                LLocalization.LLocalizationTextRead("Terms.Import"),
                LLocalization.LLocalizationTextRead("Terms.Skip"));
            if (pLosslesscutChoice)
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
            PSAnnouncement.PSAnnouncementShow(
                Window.GetWindow(this),
                LLocalization.LLocalizationTextRead("Flow.LosslessCut.Import.Title"),
                LLocalization.LLocalizationTextRead("Flow.LosslessCut.Import.NoMedia"));
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
            PSWarning.PSWarningShow(
                Window.GetWindow(this),
                LLocalization.LLocalizationTextRead("Flow.LosslessCut.Import.Title"),
                LLocalization.LLocalizationFormat("Flow.LosslessCut.Import.ReadError", pLosslesscutException.Message));
            return;
        }

        if (!LLosslesscut.LLosslesscutVersionCheck(pLosslesscutProject.LLosslesscutProjectVersion)
            && !PSDecision.PSDecisionConfirm(
                Window.GetWindow(this),
                LLocalization.LLocalizationTextRead("Flow.LosslessCut.Import.VersionTitle"),
                LLocalization.LLocalizationFormat(
                    "Flow.LosslessCut.Import.VersionWarning",
                    pLosslesscutProject.LLosslesscutProjectVersion),
                LLocalization.LLocalizationTextRead("Terms.Continue"),
                LLocalization.LLocalizationTextRead("Terms.Cancel")))
        {
            return;
        }

        LLosslesscutResult pLosslesscutResult = LLosslesscut.LLosslesscutValidate(
            pLosslesscutProject,
            lSourcePath,
            lSpool.LSpoolDuration);

        if (!pLosslesscutResult.LLosslesscutResultAgreement
            && !PSDecision.PSDecisionConfirm(
                Window.GetWindow(this),
                LLocalization.LLocalizationTextRead("Flow.LosslessCut.Import.MediaMismatchTitle"),
                LLocalization.LLocalizationFormat(
                    "Flow.LosslessCut.Import.MediaMismatch",
                    pLosslesscutResult.LLosslesscutResultMedia,
                    Path.GetFileName(lSourcePath)),
                LLocalization.LLocalizationTextRead("Terms.Import"),
                LLocalization.LLocalizationTextRead("Terms.Cancel")))
        {
            return;
        }

        if (pLosslesscutResult.LLosslesscutResultSections.Count == 0)
        {
            PSAnnouncement.PSAnnouncementShow(
                Window.GetWindow(this),
                LLocalization.LLocalizationTextRead("Flow.LosslessCut.Import.EmptyTitle"),
                PFlowLosslesscutFormat(pLosslesscutPath, pLosslesscutResult, false));
            return;
        }

        string pLosslesscutSummary = PFlowLosslesscutFormat(pLosslesscutPath, pLosslesscutResult, true);
        PSDecisionChoice pLosslesscutMode = PSDecision.PSDecisionSelect(
            Window.GetWindow(this),
            LLocalization.LLocalizationTextRead("Flow.LosslessCut.Import.PreviewTitle"),
            pLosslesscutSummary
            + Environment.NewLine
            + Environment.NewLine
            + LLocalization.LLocalizationTextRead("Flow.LosslessCut.Import.ModeChoices"),
            LLocalization.LLocalizationTextRead("Terms.Replace"),
            LLocalization.LLocalizationTextRead("Terms.Append"),
            LLocalization.LLocalizationTextRead("Terms.Cancel"));

        if (pLosslesscutMode == PSDecisionChoice.PSDecisionDismiss)
        {
            return;
        }

        int pLosslesscutPalette = Math.Max(1, PSectionPalette.PSectionActiveCount);
        if (pLosslesscutMode == PSDecisionChoice.PSDecisionPrimary)
        {
            lSegment.LSegmentLosslesscutSet(pLosslesscutResult.LLosslesscutResultSections, pLosslesscutPalette);
        }
        else
        {
            lSegment.LSegmentLosslesscutAppend(pLosslesscutResult.LLosslesscutResultSections, pLosslesscutPalette);
        }

        LTraceLog.LTraceInfoRecord(
            $"LosslessCut project imported from '{pLosslesscutPath}': "
            + $"{pLosslesscutResult.LLosslesscutResultSections.Count} segment(s), "
            + $"{pLosslesscutResult.LLosslesscutResultIssues.Count} skipped");
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
                pLosslesscutResult.LLosslesscutResultMedia.PFlowFallbackRead(pLosslesscutUnspecified)),
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
