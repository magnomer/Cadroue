using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

internal sealed class PSWarning : Window
{
    private const double PSWarningWidth = 420;

    private PSWarning(Window? pOwner, string pTitle, string pMessage)
    {
        PSWarningTitleSet(pTitle);
        PSWarningFrameApply();
        PSDialog.PSDialogOwnerApply(this, pOwner);
        PSDialogLocation.PSDialogLocationAttach(this);
        PSWarningBuild(pTitle, pMessage);
    }

    private void PSWarningTitleSet(string pTitle) => Title = pTitle;

    private void PSWarningFrameApply()
    {
        Width = PSWarningWidth;
        SizeToContent = SizeToContent.Height;
        ResizeMode = ResizeMode.NoResize;
        ShowInTaskbar = false;
        PSDialog.PSDialogApply(this, PSDialogTheme.PSDialogThemeOrange);
    }

    internal static void PSWarningShow(Window? pOwner, string pTitle, string pMessage) =>
        new PSWarning(pOwner, pTitle, pMessage).ShowDialog();

    private void PSWarningBuild(string pTitle, string pMessage) =>
        Content = PSDialog.PSDialogBuild(
            this, pTitle, PSWarningBodyBuild(pMessage), PSDialogTheme.PSDialogThemeOrange);

    private DockPanel PSWarningBodyBuild(string pMessage)
    {
        Button pConfirm = PSFooter.PSFooterButtonBuild(
            QLocalizationCatalog.QLocalizationTextRead("Terms.OK"));
        PSWarningConfirmApply(pConfirm);
        PSWarningConfirmAttach(pConfirm);
        return PSDialogMessage.PSDialogMessageBuild(
            pMessage, "/PAsset/PSDialog/PSWarning.svg", Color.FromRgb(0xB4, 0x6A, 0x18), pConfirm);
    }

    private static void PSWarningConfirmApply(Button pConfirm)
    {
        pConfirm.Foreground = new SolidColorBrush(Color.FromRgb(0xB4, 0x23, 0x18));
        pConfirm.BorderBrush = new SolidColorBrush(Color.FromRgb(0xB4, 0x23, 0x18));
    }

    private void PSWarningConfirmAttach(Button pConfirm) => pConfirm.Click += (_, _) => Close();
}
