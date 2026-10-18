using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Cadroue.UIVeneer;

internal enum PSDecisionChoice
{
    PSDecisionPrimary,
    PSDecisionAlternate,
    PSDecisionDismiss
}

internal sealed class PSDecision : Window
{
    private const double PSDecisionWidth = 440;

    private PSDecisionChoice psDecisionChoice = PSDecisionChoice.PSDecisionDismiss;

    private PSDecision(
        Window? pOwner,
        string pTitle,
        string pMessage,
        string pPrimary,
        string? pAlternate,
        string pCancel)
    {
        PSDecisionTitleSet(pTitle);
        PSDecisionFrameApply();
        PSDialog.PSDialogOwnerApply(this, pOwner);
        PSDialogLocation.PSDialogLocationAttach(this);
        PSDecisionBuild(pTitle, pMessage, pPrimary, pAlternate, pCancel);
    }

    private void PSDecisionTitleSet(string pTitle) => Title = pTitle;

    private void PSDecisionFrameApply()
    {
        Width = PSDecisionWidth;
        SizeToContent = SizeToContent.Height;
        ResizeMode = ResizeMode.NoResize;
        ShowInTaskbar = false;
        PSDialog.PSDialogApply(this, PSDialogTheme.PSDialogThemeBlue);
    }

    internal static bool PSDecisionConfirm(
        Window? pOwner,
        string pTitle,
        string pMessage,
        string pPrimary,
        string pCancel) =>
        new PSDecision(pOwner, pTitle, pMessage, pPrimary, null, pCancel).PSDecisionShow()
            == PSDecisionChoice.PSDecisionPrimary;

    internal static PSDecisionChoice PSDecisionSelect(
        Window? pOwner,
        string pTitle,
        string pMessage,
        string pPrimary,
        string pAlternate,
        string pCancel) =>
        new PSDecision(pOwner, pTitle, pMessage, pPrimary, pAlternate, pCancel).PSDecisionShow();

    private PSDecisionChoice PSDecisionShow()
    {
        ShowDialog();
        return psDecisionChoice;
    }

    private void PSDecisionBuild(
        string pTitle,
        string pMessage,
        string pPrimary,
        string? pAlternate,
        string pCancel) =>
        Content = PSDialog.PSDialogBuild(
            this,
            pTitle,
            PSDecisionBodyBuild(pMessage, pPrimary, pAlternate, pCancel),
            PSDialogTheme.PSDialogThemeBlue);

    private DockPanel PSDecisionBodyBuild(string pMessage, string pPrimary, string? pAlternate, string pCancel)
    {
        Button pCancelButton = PSDecisionCancelBuild(pCancel);
        Button? pAlternateButton = null;
        if (pAlternate is not null)
        {
            pAlternateButton = PSDecisionAlternateBuild(pAlternate);
        }

        Button pPrimaryButton = PSDecisionPrimaryBuild(pPrimary);
        return PSDialogMessage.PSDialogMessageBuild(
            pMessage,
            "/PAsset/PSDialog/PSDecision.svg",
            Color.FromRgb(0x1F, 0x6F, 0xB4),
            pCancelButton,
            pAlternateButton,
            pPrimaryButton);
    }

    private Button PSDecisionCancelBuild(string pCancel)
    {
        Button pCancelButton = PSFooter.PSFooterButtonBuild(pCancel);
        PSDecisionEscapeSet(pCancelButton);
        PSDecisionCancelAttach(pCancelButton);
        return pCancelButton;
    }

    private static void PSDecisionEscapeSet(Button pCancelButton) => pCancelButton.IsCancel = true;

    private void PSDecisionCancelAttach(Button pCancelButton) =>
        pCancelButton.Click += (_, _) => PSDecisionClose(PSDecisionChoice.PSDecisionDismiss);

    private Button PSDecisionAlternateBuild(string pAlternate)
    {
        Button pAlternateButton = PSFooter.PSFooterButtonBuild(pAlternate);
        PSDecisionAlternateAttach(pAlternateButton);
        return pAlternateButton;
    }

    private void PSDecisionAlternateAttach(Button pAlternateButton) =>
        pAlternateButton.Click += (_, _) => PSDecisionClose(PSDecisionChoice.PSDecisionAlternate);

    private Button PSDecisionPrimaryBuild(string pPrimary)
    {
        Button pPrimaryButton = PSFooter.PSFooterButtonBuild(pPrimary);
        PSDecisionEnterSet(pPrimaryButton);
        PSDecisionPrimaryApply(pPrimaryButton);
        PSDecisionPrimaryAttach(pPrimaryButton);
        return pPrimaryButton;
    }

    private static void PSDecisionEnterSet(Button pPrimaryButton) => pPrimaryButton.IsDefault = true;

    private static void PSDecisionPrimaryApply(Button pPrimaryButton)
    {
        pPrimaryButton.Foreground = new SolidColorBrush(Color.FromRgb(0x1F, 0x6F, 0xB4));
        pPrimaryButton.BorderBrush = new SolidColorBrush(Color.FromRgb(0xC2, 0xD8, 0xEF));
    }

    private void PSDecisionPrimaryAttach(Button pPrimaryButton) =>
        pPrimaryButton.Click += (_, _) => PSDecisionClose(PSDecisionChoice.PSDecisionPrimary);

    private void PSDecisionClose(PSDecisionChoice pChoice)
    {
        PSDecisionChoiceSet(pChoice);
        Close();
    }

    private void PSDecisionChoiceSet(PSDecisionChoice pChoice) => psDecisionChoice = pChoice;
}
