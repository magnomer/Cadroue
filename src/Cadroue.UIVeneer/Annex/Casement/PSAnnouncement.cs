using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

internal sealed class PSAnnouncement : Window
{
    private const double PSAnnouncementWidth = 420;

    private PSAnnouncement(Window? pOwner, string pTitle, string pMessage)
    {
        PSAnnouncementTitleSet(pTitle);
        PSAnnouncementFrameApply();
        PSDialog.PSDialogOwnerApply(this, pOwner);
        PSDialogLocation.PSDialogLocationAttach(this);
        PSAnnouncementBuild(pTitle, pMessage);
    }

    private void PSAnnouncementTitleSet(string pTitle) => Title = pTitle;

    private void PSAnnouncementFrameApply()
    {
        Width = PSAnnouncementWidth;
        SizeToContent = SizeToContent.Height;
        ResizeMode = ResizeMode.NoResize;
        ShowInTaskbar = false;
        PSDialog.PSDialogApply(this, PSDialogTheme.PSDialogThemeBlue);
    }

    internal static void PSAnnouncementShow(Window? pOwner, string pTitle, string pMessage) =>
        new PSAnnouncement(pOwner, pTitle, pMessage).ShowDialog();

    private void PSAnnouncementBuild(string pTitle, string pMessage) =>
        Content = PSDialog.PSDialogBuild(
            this, pTitle, PSAnnouncementBodyBuild(pMessage), PSDialogTheme.PSDialogThemeBlue);

    private DockPanel PSAnnouncementBodyBuild(string pMessage)
    {
        Button pConfirm = PSFooter.PSFooterButtonBuild(
            QLocalizationCatalog.QLocalizationTextRead("Terms.OK"));
        PSAnnouncementConfirmAttach(pConfirm);
        return PSDialogMessage.PSDialogMessageBuild(
            pMessage, "/PAsset/PSDialog/PSAnnouncement.svg", Color.FromRgb(0x1F, 0x6F, 0xB4), pConfirm);
    }

    private void PSAnnouncementConfirmAttach(Button pConfirm) => pConfirm.Click += (_, _) => Close();
}
