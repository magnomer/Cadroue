using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.Media;

namespace Cadroue.UIDeportment;

public enum LFlowLosslesscutKind
{
    LFlowKindDecision,
    LFlowKindChoice,
    LFlowKindNotice,
    LFlowKindWarning,
}

public sealed record LFlowLosslesscutPrompt(
    LFlowLosslesscutKind LFlowPromptKind,
    string LFlowPromptTitle,
    string LFlowPromptMessage,
    string LFlowPromptPrimary,
    string LFlowPromptAlternate,
    string LFlowPromptDismiss);

public sealed class LFlowLosslesscut
{
    private const int LFlowIssueLimit = 5;
    private const int LFlowImportPrimary = 0;
    private const int LFlowImportAlternate = 1;
    private const int LFlowImportDismiss = -1;

    private readonly LFlow lFlow;

    public LFlowLosslesscut(LFlow lOwner)
    {
        lFlow = lOwner;
    }

    public event Action<LFlowLosslesscutPrompt, Action<int>>? LFlowLosslesscutAsk;

    public void LFlowLosslesscutFind()
    {
        if (!lFlow.LFlowCommandActive || !lFlow.LFlowSectionEditable || !lFlow.LFlowSourceCheck())
        {
            return;
        }

        IReadOnlyList<string> lPaths = LLosslesscut.LLosslesscutAdjacentRead(lFlow.LFlowSourcePath!);
        for (int lIndex = 0; lIndex < lPaths.Count; lIndex++)
        {
            string lPath = lPaths[lIndex];
            LLosslesscutProject? lProject = LLosslesscut.LLosslesscutProjectRead(lPath, out Exception? lFault);
            if (lProject is null)
            {
                LTraceLog.LTraceErrorRecord("Adjacent LosslessCut project could not be read", lFault);
                continue;
            }

            string lUnspecified = LLocalization.LLocalizationTextRead("Flow.LosslessCut.Value.NotSpecified");
            int lChoice = LFlowLosslesscutRaise(new LFlowLosslesscutPrompt(
                LFlowLosslesscutKind.LFlowKindDecision,
                LLocalization.LLocalizationTextRead("Flow.LosslessCut.Detect.Title"),
                LLocalization.LLocalizationFormat(
                    "Flow.LosslessCut.Detect.Message",
                    lIndex + 1,
                    lPaths.Count,
                    LUsher.LUsherNameRead(lPath),
                    LLosslesscut.LLosslesscutStampRead(lPath),
                    lProject.LLosslesscutProjectVersion?.ToString() ?? lUnspecified,
                    lProject.LLosslesscutProjectSegments.Count,
                    LFlowFallbackRead(lProject.LLosslesscutProjectMedia, lUnspecified)),
                LLocalization.LLocalizationTextRead("Terms.Import"),
                string.Empty,
                LLocalization.LLocalizationTextRead("Terms.Skip")));
            if (lChoice == LFlowImportPrimary)
            {
                LFlowLosslesscutRun(lPath);
                return;
            }
        }
    }

    public void LFlowLosslesscutRun(string lPath)
    {
        if (!lFlow.LFlowCommandActive || !lFlow.LFlowSectionEditable || !lFlow.LFlowSourceCheck())
        {
            LFlowLosslesscutRaise(LFlowNoticeCreate(
                LFlowLosslesscutKind.LFlowKindNotice,
                "Flow.LosslessCut.Import.Title",
                LLocalization.LLocalizationTextRead("Flow.LosslessCut.Import.NoMedia")));
            return;
        }

        LLosslesscutProject? lProject = LLosslesscut.LLosslesscutProjectRead(lPath, out Exception? lFault);
        if (lProject is null)
        {
            LTraceLog.LTraceErrorRecord("LosslessCut project could not be read", lFault);
            LFlowLosslesscutRaise(LFlowNoticeCreate(
                LFlowLosslesscutKind.LFlowKindWarning,
                "Flow.LosslessCut.Import.Title",
                LLocalization.LLocalizationFormat("Flow.LosslessCut.Import.ReadError", lFault?.Message)));
            return;
        }

        if (!LLosslesscut.LLosslesscutVersionCheck(lProject.LLosslesscutProjectVersion)
            && LFlowLosslesscutRaise(LFlowConfirmCreate(
                "Flow.LosslessCut.Import.VersionTitle",
                LLocalization.LLocalizationFormat(
                    "Flow.LosslessCut.Import.VersionWarning", lProject.LLosslesscutProjectVersion),
                "Terms.Continue")) != LFlowImportPrimary)
        {
            return;
        }

        LLosslesscutResult lResult = LLosslesscut.LLosslesscutValidate(
            lProject, lFlow.LFlowSourcePath!, lFlow.LFlowDuration);
        if (!lResult.LLosslesscutResultAgreement
            && LFlowLosslesscutRaise(LFlowConfirmCreate(
                "Flow.LosslessCut.Import.MediaMismatchTitle",
                LLocalization.LLocalizationFormat(
                    "Flow.LosslessCut.Import.MediaMismatch",
                    lResult.LLosslesscutResultMedia,
                    lFlow.LFlowSourceFormat()),
                "Terms.Import")) != LFlowImportPrimary)
        {
            return;
        }

        if (lResult.LLosslesscutResultSections.Count == 0)
        {
            LFlowLosslesscutRaise(LFlowNoticeCreate(
                LFlowLosslesscutKind.LFlowKindNotice,
                "Flow.LosslessCut.Import.EmptyTitle",
                LFlowLosslesscutFormat(lPath, lResult, false)));
            return;
        }

        int lMode = LFlowLosslesscutRaise(new LFlowLosslesscutPrompt(
            LFlowLosslesscutKind.LFlowKindChoice,
            LLocalization.LLocalizationTextRead("Flow.LosslessCut.Import.PreviewTitle"),
            LFlowLosslesscutFormat(lPath, lResult, true)
            + Environment.NewLine
            + Environment.NewLine
            + LLocalization.LLocalizationTextRead("Flow.LosslessCut.Import.ModeChoices"),
            LLocalization.LLocalizationTextRead("Terms.Replace"),
            LLocalization.LLocalizationTextRead("Terms.Append"),
            LLocalization.LLocalizationTextRead("Terms.Cancel")));
        if (lMode == LFlowImportDismiss)
        {
            return;
        }

        LSegment lSegment = lFlow.LFlowSection.LFlowSegment;
        if (lMode == LFlowImportPrimary)
        {
            lSegment.LSegmentLosslesscutSet(lResult.LLosslesscutResultSections, lFlow.LFlowPaletteCount);
        }
        else
        {
            lSegment.LSegmentLosslesscutAppend(lResult.LLosslesscutResultSections, lFlow.LFlowPaletteCount);
        }

        LTraceLog.LTraceInfoRecord(
            $"LosslessCut project imported from '{lPath}': "
            + $"{lResult.LLosslesscutResultSections.Count} segment(s), "
            + $"{lResult.LLosslesscutResultIssues.Count} skipped");
    }

    public static string LFlowLosslesscutFormat(string lPath, LLosslesscutResult lResult, bool lShowRange)
    {
        string lUnspecified = LLocalization.LLocalizationTextRead("Flow.LosslessCut.Value.NotSpecified");
        var lLines = new List<string>
        {
            LLocalization.LLocalizationFormat("Flow.LosslessCut.Summary.Project", LUsher.LUsherNameRead(lPath)),
            LLocalization.LLocalizationFormat(
                "Flow.LosslessCut.Summary.Version", lResult.LLosslesscutResultVersion?.ToString() ?? lUnspecified),
            LLocalization.LLocalizationFormat(
                "Flow.LosslessCut.Summary.Media", LFlowFallbackRead(lResult.LLosslesscutResultMedia, lUnspecified)),
            LLocalization.LLocalizationFormat(
                "Flow.LosslessCut.Summary.Valid", lResult.LLosslesscutResultSections.Count),
            LLocalization.LLocalizationFormat(
                "Flow.LosslessCut.Summary.Skipped", lResult.LLosslesscutResultIssues.Count)
        };

        if (lShowRange && lResult.LLosslesscutResultSections.Count > 0)
        {
            LSidecarSectionRecord lFirst = lResult.LLosslesscutResultSections[0];
            LSidecarSectionRecord lLast = lResult.LLosslesscutResultSections[^1];
            lLines.Add(LLocalization.LLocalizationFormat(
                "Flow.LosslessCut.Summary.Range",
                TimeSpan.FromMilliseconds(lFirst.LSidecarStartMilliseconds).ToString(@"hh\:mm\:ss\.fff"),
                TimeSpan.FromMilliseconds(lLast.LSidecarEndMilliseconds).ToString(@"hh\:mm\:ss\.fff")));
        }

        foreach (LLosslesscutIssue lIssue in lResult.LLosslesscutResultIssues.Take(LFlowIssueLimit))
        {
            lLines.Add(LLocalization.LLocalizationFormat(
                "Flow.LosslessCut.Summary.Issue", lIssue.LLosslesscutIssueIndex + 1, lIssue.LLosslesscutIssueReason));
        }

        if (lResult.LLosslesscutResultIssues.Count > LFlowIssueLimit)
        {
            lLines.Add(LLocalization.LLocalizationFormat(
                "Flow.LosslessCut.Summary.MoreIssues", lResult.LLosslesscutResultIssues.Count - LFlowIssueLimit));
        }

        return string.Join(Environment.NewLine, lLines);
    }

    private int LFlowLosslesscutRaise(LFlowLosslesscutPrompt lPrompt)
    {
        int lAnswer = LFlowImportDismiss;
        LFlowLosslesscutAsk?.Invoke(lPrompt, lChoice => lAnswer = lChoice);
        return lAnswer;
    }

    private static LFlowLosslesscutPrompt LFlowNoticeCreate(
        LFlowLosslesscutKind lKind,
        string lTitleKey,
        string lMessage) =>
        new(lKind, LLocalization.LLocalizationTextRead(lTitleKey), lMessage, string.Empty, string.Empty, string.Empty);

    private static LFlowLosslesscutPrompt LFlowConfirmCreate(string lTitleKey, string lMessage, string lPrimaryKey) =>
        new(
            LFlowLosslesscutKind.LFlowKindDecision,
            LLocalization.LLocalizationTextRead(lTitleKey),
            lMessage,
            LLocalization.LLocalizationTextRead(lPrimaryKey),
            string.Empty,
            LLocalization.LLocalizationTextRead("Terms.Cancel"));

    private static string LFlowFallbackRead(string lValue, string lFallback) =>
        string.IsNullOrWhiteSpace(lValue) ? lFallback : lValue;
}
