using Cadroue.Application;
using Cadroue.Conduct;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.Media;

namespace Cadroue.UIDeportment;

public enum QFlowLosslesscutKind
{
    QFlowKindDecision,
    QFlowKindChoice,
    QFlowKindNotice,
    QFlowKindWarning,
}

public sealed record QFlowLosslesscutPrompt(
    QFlowLosslesscutKind QFlowPromptKind,
    string QFlowPromptTitle,
    string QFlowPromptMessage,
    string QFlowPromptPrimary,
    string QFlowPromptAlternate,
    string QFlowPromptDismiss);

public sealed class QFlowLosslesscut
{
    private const int QFlowIssueLimit = 5;
    private const int QFlowImportPrimary = 0;
    private const int QFlowImportAlternate = 1;
    private const int QFlowImportDismiss = -1;

    private readonly QFlow qFlow;
    private readonly CUsher qFlowUsher;

    public QFlowLosslesscut(QFlow lOwner, CUsher lUsher)
    {
        qFlow = lOwner;
        qFlowUsher = lUsher;
    }

    public event Action<QFlowLosslesscutPrompt, Action<int>>? QFlowLosslesscutAsk;

    public void QFlowLosslesscutFind()
    {
        if (!qFlow.QFlowCommandActive || !qFlow.QFlowEdit.QFlowSectionEditable || !qFlow.QFlowSourceCheck())
        {
            return;
        }

        IReadOnlyList<string> lPaths = LLosslesscut.LLosslesscutAdjacentRead(qFlow.QFlowSourcePath!);
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
            int lChoice = QFlowLosslesscutRaise(new QFlowLosslesscutPrompt(
                QFlowLosslesscutKind.QFlowKindDecision,
                LLocalization.LLocalizationTextRead("Flow.LosslessCut.Detect.Title"),
                LLocalization.LLocalizationFormat(
                    "Flow.LosslessCut.Detect.Message",
                    lIndex + 1,
                    lPaths.Count,
                    qFlowUsher.CUsherNameRead(lPath),
                    LLosslesscut.LLosslesscutStampRead(lPath),
                    lProject.LLosslesscutProjectVersion?.ToString() ?? lUnspecified,
                    lProject.LLosslesscutProjectSegments.Count,
                    QFlowFallbackRead(lProject.LLosslesscutProjectMedia, lUnspecified)),
                LLocalization.LLocalizationTextRead("Terms.Import"),
                string.Empty,
                LLocalization.LLocalizationTextRead("Terms.Skip")));
            if (lChoice == QFlowImportPrimary)
            {
                QFlowLosslesscutRun(lPath);
                return;
            }
        }
    }

    public void QFlowLosslesscutRun(string lPath)
    {
        if (!qFlow.QFlowCommandActive || !qFlow.QFlowEdit.QFlowSectionEditable || !qFlow.QFlowSourceCheck())
        {
            QFlowLosslesscutRaise(QFlowNoticeCreate(
                QFlowLosslesscutKind.QFlowKindNotice,
                "Flow.LosslessCut.Import.Title",
                LLocalization.LLocalizationTextRead("Flow.LosslessCut.Import.NoMedia")));
            return;
        }

        LLosslesscutProject? lProject = LLosslesscut.LLosslesscutProjectRead(lPath, out Exception? lFault);
        if (lProject is null)
        {
            LTraceLog.LTraceErrorRecord("LosslessCut project could not be read", lFault);
            QFlowLosslesscutRaise(QFlowNoticeCreate(
                QFlowLosslesscutKind.QFlowKindWarning,
                "Flow.LosslessCut.Import.Title",
                LLocalization.LLocalizationFormat("Flow.LosslessCut.Import.ReadError", lFault?.Message)));
            return;
        }

        if (!LLosslesscut.LLosslesscutVersionCheck(lProject.LLosslesscutProjectVersion)
            && QFlowLosslesscutRaise(QFlowConfirmCreate(
                "Flow.LosslessCut.Import.VersionTitle",
                LLocalization.LLocalizationFormat(
                    "Flow.LosslessCut.Import.VersionWarning", lProject.LLosslesscutProjectVersion),
                "Terms.Continue")) != QFlowImportPrimary)
        {
            return;
        }

        LLosslesscutResult lResult = LLosslesscut.LLosslesscutValidate(
            lProject, qFlow.QFlowSourcePath!, qFlow.QFlowDuration);
        if (!lResult.LLosslesscutResultAgreement
            && QFlowLosslesscutRaise(QFlowConfirmCreate(
                "Flow.LosslessCut.Import.MediaMismatchTitle",
                LLocalization.LLocalizationFormat(
                    "Flow.LosslessCut.Import.MediaMismatch",
                    lResult.LLosslesscutResultMedia,
                    qFlow.QFlowSourceFormat()),
                "Terms.Import")) != QFlowImportPrimary)
        {
            return;
        }

        if (lResult.LLosslesscutResultSections.Count == 0)
        {
            QFlowLosslesscutRaise(QFlowNoticeCreate(
                QFlowLosslesscutKind.QFlowKindNotice,
                "Flow.LosslessCut.Import.EmptyTitle",
                QFlowLosslesscutFormat(qFlowUsher, lPath, lResult, false)));
            return;
        }

        int lMode = QFlowLosslesscutRaise(new QFlowLosslesscutPrompt(
            QFlowLosslesscutKind.QFlowKindChoice,
            LLocalization.LLocalizationTextRead("Flow.LosslessCut.Import.PreviewTitle"),
            QFlowLosslesscutFormat(qFlowUsher, lPath, lResult, true)
            + Environment.NewLine
            + Environment.NewLine
            + LLocalization.LLocalizationTextRead("Flow.LosslessCut.Import.ModeChoices"),
            LLocalization.LLocalizationTextRead("Terms.Replace"),
            LLocalization.LLocalizationTextRead("Terms.Append"),
            LLocalization.LLocalizationTextRead("Terms.Cancel")));
        if (lMode == QFlowImportDismiss)
        {
            return;
        }

        LSegment lSegment = qFlow.QFlowSection.QFlowSegment;
        if (lMode == QFlowImportPrimary)
        {
            lSegment.LSegmentLosslesscutSet(lResult.LLosslesscutResultSections, qFlow.QFlowPaletteCount);
        }
        else
        {
            lSegment.LSegmentLosslesscutAppend(lResult.LLosslesscutResultSections, qFlow.QFlowPaletteCount);
        }

        LTraceLog.LTraceInfoRecord(
            $"LosslessCut project imported from '{lPath}': "
            + $"{lResult.LLosslesscutResultSections.Count} segment(s), "
            + $"{lResult.LLosslesscutResultIssues.Count} skipped");
    }

    public static string QFlowLosslesscutFormat(
        CUsher lUsher, string lPath, LLosslesscutResult lResult, bool lShowRange)
    {
        string lUnspecified = LLocalization.LLocalizationTextRead("Flow.LosslessCut.Value.NotSpecified");
        var lLines = new List<string>
        {
            LLocalization.LLocalizationFormat("Flow.LosslessCut.Summary.Project", lUsher.CUsherNameRead(lPath)),
            LLocalization.LLocalizationFormat(
                "Flow.LosslessCut.Summary.Version", lResult.LLosslesscutResultVersion?.ToString() ?? lUnspecified),
            LLocalization.LLocalizationFormat(
                "Flow.LosslessCut.Summary.Media", QFlowFallbackRead(lResult.LLosslesscutResultMedia, lUnspecified)),
            LLocalization.LLocalizationFormat(
                "Flow.LosslessCut.Summary.Valid", lResult.LLosslesscutResultSections.Count),
            LLocalization.LLocalizationFormat(
                "Flow.LosslessCut.Summary.Skipped", lResult.LLosslesscutResultIssues.Count)
        };

        if (lShowRange && lResult.LLosslesscutResultSections.Count > 0)
        {
            LSidecarSectionEntry lFirst = lResult.LLosslesscutResultSections[0];
            LSidecarSectionEntry lLast = lResult.LLosslesscutResultSections[^1];
            lLines.Add(LLocalization.LLocalizationFormat(
                "Flow.LosslessCut.Summary.Range",
                TimeSpan.FromMilliseconds(lFirst.LSidecarStartMilliseconds).ToString(@"hh\:mm\:ss\.fff"),
                TimeSpan.FromMilliseconds(lLast.LSidecarEndMilliseconds).ToString(@"hh\:mm\:ss\.fff")));
        }

        foreach (LLosslesscutIssue lIssue in lResult.LLosslesscutResultIssues.Take(QFlowIssueLimit))
        {
            lLines.Add(LLocalization.LLocalizationFormat(
                "Flow.LosslessCut.Summary.Issue", lIssue.LLosslesscutIssueIndex + 1, lIssue.LLosslesscutIssueReason));
        }

        if (lResult.LLosslesscutResultIssues.Count > QFlowIssueLimit)
        {
            lLines.Add(LLocalization.LLocalizationFormat(
                "Flow.LosslessCut.Summary.MoreIssues", lResult.LLosslesscutResultIssues.Count - QFlowIssueLimit));
        }

        return string.Join(Environment.NewLine, lLines);
    }

    private int QFlowLosslesscutRaise(QFlowLosslesscutPrompt lPrompt)
    {
        int lAnswer = QFlowImportDismiss;
        QFlowLosslesscutAsk?.Invoke(lPrompt, lChoice => lAnswer = lChoice);
        return lAnswer;
    }

    private static QFlowLosslesscutPrompt QFlowNoticeCreate(
        QFlowLosslesscutKind lKind,
        string lTitleKey,
        string lMessage) =>
        new(lKind, LLocalization.LLocalizationTextRead(lTitleKey), lMessage, string.Empty, string.Empty, string.Empty);

    private static QFlowLosslesscutPrompt QFlowConfirmCreate(string lTitleKey, string lMessage, string lPrimaryKey) =>
        new(
            QFlowLosslesscutKind.QFlowKindDecision,
            LLocalization.LLocalizationTextRead(lTitleKey),
            lMessage,
            LLocalization.LLocalizationTextRead(lPrimaryKey),
            string.Empty,
            LLocalization.LLocalizationTextRead("Terms.Cancel"));

    private static string QFlowFallbackRead(string lValue, string lFallback) =>
        string.IsNullOrWhiteSpace(lValue) ? lFallback : lValue;
}
