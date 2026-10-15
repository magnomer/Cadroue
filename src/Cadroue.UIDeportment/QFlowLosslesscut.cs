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

            string lUnspecified = QLocalizationCatalog.QLocalizationTextRead("Flow.LosslessCut.Value.NotSpecified");
            int lChoice = QFlowLosslesscutRaise(new QFlowLosslesscutPrompt(
                QFlowLosslesscutKind.QFlowKindDecision,
                QLocalizationCatalog.QLocalizationTextRead("Flow.LosslessCut.Detect.Title"),
                QLocalizationCatalog.QLocalizationFormat(
                    "Flow.LosslessCut.Detect.Message",
                    lIndex + 1,
                    lPaths.Count,
                    qFlowUsher.CUsherNameRead(lPath),
                    LLosslesscut.LLosslesscutStampRead(lPath),
                    lProject.LLosslesscutProjectVersion?.ToString() ?? lUnspecified,
                    lProject.LLosslesscutProjectSegments.Count,
                    QFlowFallbackRead(lProject.LLosslesscutProjectMedia, lUnspecified)),
                QLocalizationCatalog.QLocalizationTextRead("Terms.Import"),
                string.Empty,
                QLocalizationCatalog.QLocalizationTextRead("Terms.Skip")));
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
                QLocalizationCatalog.QLocalizationTextRead("Flow.LosslessCut.Import.NoMedia")));
            return;
        }

        LLosslesscutProject? lProject = LLosslesscut.LLosslesscutProjectRead(lPath, out Exception? lFault);
        if (lProject is null)
        {
            LTraceLog.LTraceErrorRecord("LosslessCut project could not be read", lFault);
            QFlowLosslesscutRaise(QFlowNoticeCreate(
                QFlowLosslesscutKind.QFlowKindWarning,
                "Flow.LosslessCut.Import.Title",
                QLocalizationCatalog.QLocalizationFormat("Flow.LosslessCut.Import.ReadError", lFault?.Message)));
            return;
        }

        if (!LLosslesscut.LLosslesscutVersionCheck(lProject.LLosslesscutProjectVersion)
            && QFlowLosslesscutRaise(QFlowConfirmCreate(
                "Flow.LosslessCut.Import.VersionTitle",
                QLocalizationCatalog.QLocalizationFormat(
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
                QLocalizationCatalog.QLocalizationFormat(
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
            QLocalizationCatalog.QLocalizationTextRead("Flow.LosslessCut.Import.PreviewTitle"),
            QFlowLosslesscutFormat(qFlowUsher, lPath, lResult, true)
            + Environment.NewLine
            + Environment.NewLine
            + QLocalizationCatalog.QLocalizationTextRead("Flow.LosslessCut.Import.ModeChoices"),
            QLocalizationCatalog.QLocalizationTextRead("Terms.Replace"),
            QLocalizationCatalog.QLocalizationTextRead("Terms.Append"),
            QLocalizationCatalog.QLocalizationTextRead("Terms.Cancel")));
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
        string lUnspecified = QLocalizationCatalog.QLocalizationTextRead("Flow.LosslessCut.Value.NotSpecified");
        var lLines = new List<string>
        {
            QLocalizationCatalog.QLocalizationFormat("Flow.LosslessCut.Summary.Project", lUsher.CUsherNameRead(lPath)),
            QLocalizationCatalog.QLocalizationFormat(
                "Flow.LosslessCut.Summary.Version", lResult.LLosslesscutResultVersion?.ToString() ?? lUnspecified),
            QLocalizationCatalog.QLocalizationFormat(
                "Flow.LosslessCut.Summary.Media", QFlowFallbackRead(lResult.LLosslesscutResultMedia, lUnspecified)),
            QLocalizationCatalog.QLocalizationFormat(
                "Flow.LosslessCut.Summary.Valid", lResult.LLosslesscutResultSections.Count),
            QLocalizationCatalog.QLocalizationFormat(
                "Flow.LosslessCut.Summary.Skipped", lResult.LLosslesscutResultIssues.Count)
        };

        if (lShowRange && lResult.LLosslesscutResultSections.Count > 0)
        {
            LSidecarSectionEntry lFirst = lResult.LLosslesscutResultSections[0];
            LSidecarSectionEntry lLast = lResult.LLosslesscutResultSections[^1];
            lLines.Add(QLocalizationCatalog.QLocalizationFormat(
                "Flow.LosslessCut.Summary.Range",
                TimeSpan.FromMilliseconds(lFirst.LSidecarStartMilliseconds).ToString(@"hh\:mm\:ss\.fff"),
                TimeSpan.FromMilliseconds(lLast.LSidecarEndMilliseconds).ToString(@"hh\:mm\:ss\.fff")));
        }

        foreach (LLosslesscutIssue lIssue in lResult.LLosslesscutResultIssues.Take(QFlowIssueLimit))
        {
            lLines.Add(QLocalizationCatalog.QLocalizationFormat(
                "Flow.LosslessCut.Summary.Issue", lIssue.LLosslesscutIssueIndex + 1, lIssue.LLosslesscutIssueReason));
        }

        if (lResult.LLosslesscutResultIssues.Count > QFlowIssueLimit)
        {
            lLines.Add(QLocalizationCatalog.QLocalizationFormat(
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
        new(lKind, QLocalizationCatalog.QLocalizationTextRead(lTitleKey), lMessage, string.Empty, string.Empty,
            string.Empty);

    private static QFlowLosslesscutPrompt QFlowConfirmCreate(string lTitleKey, string lMessage, string lPrimaryKey) =>
        new(
            QFlowLosslesscutKind.QFlowKindDecision,
            QLocalizationCatalog.QLocalizationTextRead(lTitleKey),
            lMessage,
            QLocalizationCatalog.QLocalizationTextRead(lPrimaryKey),
            string.Empty,
            QLocalizationCatalog.QLocalizationTextRead("Terms.Cancel"));

    private static string QFlowFallbackRead(string lValue, string lFallback) =>
        string.IsNullOrWhiteSpace(lValue) ? lFallback : lValue;
}
