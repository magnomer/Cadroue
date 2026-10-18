using System.Windows;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

internal sealed class PFlowLosslesscut
{
    private static readonly IReadOnlyDictionary<bool, int> pFlowConfirmChoices = new Dictionary<bool, int>
    {
        [true] = 0,
        [false] = -1,
    };

    private static readonly IReadOnlyDictionary<PSDecisionChoice, int> pFlowSelectChoices =
        new Dictionary<PSDecisionChoice, int>
        {
            [PSDecisionChoice.PSDecisionPrimary] = 0,
            [PSDecisionChoice.PSDecisionAlternate] = 1,
            [PSDecisionChoice.PSDecisionDismiss] = -1,
        };

    private readonly IReadOnlyDictionary<QFlowLosslesscutKind, Action<QFlowLosslesscutPrompt, Action<int>>>
        pFlowLosslesscutShows;

    private readonly DependencyObject pFlowLosslesscutHost;

    public PFlowLosslesscut(QFlowLosslesscut lLosslesscut, DependencyObject pHost)
    {
        pFlowLosslesscutHost = pHost;
        pFlowLosslesscutShows = PFlowLosslesscutCreate();
        lLosslesscut.QFlowLosslesscutAsk += PFlowLosslesscutHandle;
    }

    private Dictionary<QFlowLosslesscutKind, Action<QFlowLosslesscutPrompt, Action<int>>> PFlowLosslesscutCreate() =>
        new()
        {
            [QFlowLosslesscutKind.QFlowKindDecision] = PFlowConfirmShow,
            [QFlowLosslesscutKind.QFlowKindChoice] = PFlowSelectShow,
            [QFlowLosslesscutKind.QFlowKindNotice] = PFlowNoticeShow,
            [QFlowLosslesscutKind.QFlowKindWarning] = PFlowWarningShow,
        };

    private void PFlowLosslesscutHandle(QFlowLosslesscutPrompt lPrompt, Action<int> lAnswer) =>
        pFlowLosslesscutShows[lPrompt.QFlowPromptKind](lPrompt, lAnswer);

    private void PFlowConfirmShow(QFlowLosslesscutPrompt lPrompt, Action<int> lAnswer) =>
        lAnswer(pFlowConfirmChoices[PSDecision.PSDecisionConfirm(
            Window.GetWindow(pFlowLosslesscutHost),
            lPrompt.QFlowPromptTitle,
            lPrompt.QFlowPromptMessage,
            lPrompt.QFlowPromptPrimary,
            lPrompt.QFlowPromptDismiss)]);

    private void PFlowSelectShow(QFlowLosslesscutPrompt lPrompt, Action<int> lAnswer) =>
        lAnswer(pFlowSelectChoices[PSDecision.PSDecisionSelect(
            Window.GetWindow(pFlowLosslesscutHost),
            lPrompt.QFlowPromptTitle,
            lPrompt.QFlowPromptMessage,
            lPrompt.QFlowPromptPrimary,
            lPrompt.QFlowPromptAlternate,
            lPrompt.QFlowPromptDismiss)]);

    private void PFlowNoticeShow(QFlowLosslesscutPrompt lPrompt, Action<int> lAnswer)
    {
        PSAnnouncement.PSAnnouncementShow(
            Window.GetWindow(pFlowLosslesscutHost), lPrompt.QFlowPromptTitle, lPrompt.QFlowPromptMessage);
        lAnswer(0);
    }

    private void PFlowWarningShow(QFlowLosslesscutPrompt lPrompt, Action<int> lAnswer)
    {
        PSWarning.PSWarningShow(
            Window.GetWindow(pFlowLosslesscutHost), lPrompt.QFlowPromptTitle, lPrompt.QFlowPromptMessage);
        lAnswer(0);
    }
}
