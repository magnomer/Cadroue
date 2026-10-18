using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

internal sealed class PSAlert : Window
{
    private const double PSAlertWidth = 420;

    private PSAlert(Window? pOwner, string pTitle, string pQuestion, string pAction, string? pCancel)
    {
        PSAlertTitleSet(pTitle);
        PSAlertFrameApply();
        PSDialog.PSDialogOwnerApply(this, pOwner);
        PSDialogLocation.PSDialogLocationAttach(this);
        PSAlertBuild(pTitle, pQuestion, pAction, pCancel);
    }

    private void PSAlertTitleSet(string pTitle) => Title = pTitle;

    private void PSAlertFrameApply()
    {
        Width = PSAlertWidth;
        SizeToContent = SizeToContent.Height;
        ResizeMode = ResizeMode.NoResize;
        ShowInTaskbar = false;
        PSDialog.PSDialogApply(this, PSDialogTheme.PSDialogThemeRed);
    }

    internal static bool PSAlertConfirm(
        Window? pOwner,
        string pTitle,
        string pQuestion,
        string pAction,
        string? pCancel = null) =>
        new PSAlert(pOwner, pTitle, pQuestion, pAction, pCancel).ShowDialog() == true;

    internal static void PSAlertConfirm(Window? pOwner, QAsk lAsk, Action<bool> lAnswer) =>
        lAnswer(PSAlertConfirm(pOwner, lAsk.QAskTitle, lAsk.QAskQuestion, lAsk.QAskAction, lAsk.QAskDismiss));

    private void PSAlertBuild(string pTitle, string pQuestion, string pAction, string? pCancel) =>
        Content = PSDialog.PSDialogBuild(
            this,
            pTitle,
            PSAlertBodyBuild(pQuestion, pAction, pCancel),
            PSDialogTheme.PSDialogThemeRed);

    private DockPanel PSAlertBodyBuild(string pQuestion, string pAction, string? pCancel)
    {
        Button pCancelButton = PSAlertCancelBuild(pCancel);
        Button pConfirm = PSAlertConfirmBuild(pAction);
        return PSDialogMessage.PSDialogMessageBuild(
            pQuestion, "/PAsset/PSDialog/PSAlert.svg", Color.FromRgb(0xB4, 0x23, 0x18), pCancelButton, pConfirm);
    }

    private Button PSAlertCancelBuild(string? pCancel)
    {
        Button pCancelButton = PSFooter.PSFooterButtonBuild(PSAlertCancelResolve(pCancel));
        PSAlertEscapeSet(pCancelButton);
        PSAlertCancelAttach(pCancelButton);
        return pCancelButton;
    }

    private static string PSAlertCancelResolve(string? pCancel) =>
        pCancel ?? QLocalizationCatalog.QLocalizationTextRead("Terms.Cancel");

    private static void PSAlertEscapeSet(Button pCancelButton) => pCancelButton.IsCancel = true;

    private void PSAlertCancelAttach(Button pCancelButton) => pCancelButton.Click += (_, _) => Close();

    private Button PSAlertConfirmBuild(string pAction)
    {
        Button pConfirm = PSFooter.PSFooterButtonBuild(pAction);
        PSAlertConfirmApply(pConfirm);
        PSAlertConfirmAttach(pConfirm);
        return pConfirm;
    }

    private static void PSAlertConfirmApply(Button pConfirm)
    {
        pConfirm.Foreground = new SolidColorBrush(Color.FromRgb(0xE8, 0x11, 0x23));
        pConfirm.BorderBrush = new SolidColorBrush(Color.FromRgb(0xE8, 0x11, 0x23));
    }

    private void PSAlertConfirmAttach(Button pConfirm) => pConfirm.Click += PSAlertConfirmHandle;

    private void PSAlertConfirmHandle(object sender, RoutedEventArgs e) => DialogResult = true;
}
