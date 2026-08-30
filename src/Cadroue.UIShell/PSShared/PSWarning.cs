using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Cadroue.UIShell.PSShared;

internal sealed class PSWarning : Window
{
    private const double PSWarningWidth = 420;

    private PSWarning(Window? pOwner, string pTitle, string pMessage)
    {
        Title = pTitle;
        Width = PSWarningWidth;
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
        Content = PSWarningBuild(pTitle, pMessage);
    }

    internal static void PSWarningShow(Window? pOwner, string pTitle, string pMessage) =>
        new PSWarning(pOwner, pTitle, pMessage).ShowDialog();

    private UIElement PSWarningBuild(string pTitle, string pMessage) =>
        PSDialog.PSDialogBuild(this, pTitle, PSWarningBodyBuild(pMessage));

    private DockPanel PSWarningBodyBuild(string pMessage)
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
        pConfirm.Foreground = new SolidColorBrush(Color.FromRgb(0xB4, 0x6A, 0x18));
        pConfirm.BorderBrush = new SolidColorBrush(Color.FromRgb(0xEF, 0xDC, 0xC2));
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
