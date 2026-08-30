using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.UIShell.PMainWindow;

namespace Cadroue.UIShell.PSShared;

internal sealed class PSAlert : Window
{
    private const double PSAlertWidth = 420;

    private PSAlert(Window pOwner, string pTitle, string pQuestion, string pAction)
    {
        Title = pTitle;
        Owner = pOwner;
        Width = PSAlertWidth;
        SizeToContent = SizeToContent.Height;
        ResizeMode = ResizeMode.NoResize;
        ShowInTaskbar = false;
        PSDialog.PSDialogApply(this, PSCasement.PSCasementBandFill);
        Content = PSAlertBuild(pTitle, pQuestion, pAction);
    }

    internal static bool PSAlertConfirm(Window pOwner, string pTitle, string pQuestion, string pAction) =>
        new PSAlert(pOwner, pTitle, pQuestion, pAction).ShowDialog() == true;

    private UIElement PSAlertBuild(string pTitle, string pQuestion, string pAction) =>
        PSDialog.PSDialogBuild(this, pTitle, PSAlertBodyBuild(pQuestion, pAction));

    private DockPanel PSAlertBodyBuild(string pQuestion, string pAction)
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

        Button pCancel = PSFooter.PSFooterButtonBuild(
            LLocalization.LLocalizationTextRead("Terms.Cancel"));
        pCancel.Click += (_, _) => Close();

        Button pConfirm = PSFooter.PSFooterButtonBuild(pAction);
        pConfirm.Foreground = new SolidColorBrush(Color.FromRgb(0xB4, 0x23, 0x18));
        pConfirm.BorderBrush = new SolidColorBrush(Color.FromRgb(0xEF, 0xC5, 0xC2));
        pConfirm.Click += (_, _) => DialogResult = true;

        pFooter.Children.Add(pCancel);
        pFooter.Children.Add(pConfirm);
        DockPanel.SetDock(pFooter, Dock.Bottom);
        pBody.Children.Add(pFooter);

        var pMessage = new TextBlock
        {
            Text = pQuestion,
            FontSize = 14,
            Foreground = PSField.PSFieldText,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(24, 24, 24, 12)
        };
        pBody.Children.Add(pMessage);
        return pBody;
    }
}
