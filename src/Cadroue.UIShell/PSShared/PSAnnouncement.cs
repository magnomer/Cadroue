using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Cadroue.UIShell.PSShared;

internal sealed class PSAnnouncement : Window
{
    private const double PSAnnouncementWidth = 420;

    private PSAnnouncement(Window? pOwner, string pTitle, string pMessage)
    {
        Title = pTitle;
        Width = PSAnnouncementWidth;
        SizeToContent = SizeToContent.Height;
        ResizeMode = ResizeMode.NoResize;
        ShowInTaskbar = false;
        PSDialog.PSDialogApply(this, PSCasement.PSCasementBandFill);
        Window? pResolvedOwner = pOwner ?? System.Windows.Application.Current?.MainWindow;
        if (pResolvedOwner is not null && pResolvedOwner != this)
        {
            Owner = pResolvedOwner;
        }
        else
        {
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }
        Content = PSAnnouncementBuild(pTitle, pMessage);
    }

    internal static void PSAnnouncementShow(Window? pOwner, string pTitle, string pMessage) =>
        new PSAnnouncement(pOwner, pTitle, pMessage).ShowDialog();

    private UIElement PSAnnouncementBuild(string pTitle, string pMessage) =>
        PSDialog.PSDialogBuild(this, pTitle, PSAnnouncementBodyBuild(pMessage));

    private DockPanel PSAnnouncementBodyBuild(string pMessage)
    {
        var pBody = new DockPanel
        {
            Background = Brushes.White
        };

        var pFooter = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(12)
        };

        Button pConfirm = PSFooter.PSFooterButtonBuild(
            LLocalization.LLocalizationTextRead("Terms.OK"));
        pConfirm.Click += (_, _) => Close();

        pFooter.Children.Add(pConfirm);
        DockPanel.SetDock(pFooter, Dock.Bottom);
        pBody.Children.Add(pFooter);

        var pMessageText = new TextBlock
        {
            Text = pMessage,
            FontSize = 14,
            Foreground = PSField.PSFieldText,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(24, 24, 24, 12)
        };
        pBody.Children.Add(pMessageText);
        return pBody;
    }
}
