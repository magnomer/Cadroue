using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.UIShell.PMainWindow;

namespace Cadroue.UIShell.PSShared;

internal sealed class PSAlert : Window
{
    private const double PSAlertWidth = 420;

    private PSAlert(Window? pOwner, string pTitle, string pQuestion, string pAction, string? pCancel)
    {
        Title = pTitle;
        Width = PSAlertWidth;
        SizeToContent = SizeToContent.Height;
        ResizeMode = ResizeMode.NoResize;
        ShowInTaskbar = false;
        PSDialog.PSDialogApply(this, PSDialogTheme.PSDialogThemeRed);
        Window? pResolvedOwner = pOwner ?? System.Windows.Application.Current?.MainWindow;
        if (pResolvedOwner is not null && pResolvedOwner != this)
        {
            Owner = pResolvedOwner;
        }
        else
        {
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }
        PSDialog.PSDialogLocationAttach(this);
        Content = PSAlertBuild(pTitle, pQuestion, pAction, pCancel);
    }

    internal static bool PSAlertConfirm(Window? pOwner, string pTitle, string pQuestion, string pAction, string? pCancel = null) =>
        new PSAlert(pOwner, pTitle, pQuestion, pAction, pCancel).ShowDialog() == true;

    private UIElement PSAlertBuild(string pTitle, string pQuestion, string pAction, string? pCancel) =>
        PSDialog.PSDialogBuild(this, pTitle, PSAlertBodyBuild(pQuestion, pAction, pCancel), PSDialogTheme.PSDialogThemeRed);

    private DockPanel PSAlertBodyBuild(string pQuestion, string pAction, string? pCancel)
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

        Button pCancelButton = PSFooter.PSFooterButtonBuild(
            pCancel ?? LLocalization.LLocalizationTextRead("Terms.Cancel"));
        pCancelButton.IsCancel = true;
        pCancelButton.Click += (_, _) => Close();

        Button pConfirm = PSFooter.PSFooterButtonBuild(pAction);
        pConfirm.Foreground = new SolidColorBrush(Color.FromRgb(0xE8, 0x11, 0x23));
        pConfirm.BorderBrush = new SolidColorBrush(Color.FromRgb(0xE8, 0x11, 0x23));
        pConfirm.Click += (_, _) => DialogResult = true;

        pFooter.Children.Add(pCancelButton);
        pFooter.Children.Add(pConfirm);
        DockPanel.SetDock(pFooter, Dock.Bottom);
        pBody.Children.Add(pFooter);

        pBody.Children.Add(PSAlertContentBuild(pQuestion));
        return pBody;
    }

    private static Grid PSAlertContentBuild(string pMessage)
    {
        var pContent = new Grid();
        pContent.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        pContent.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var pIcon = new Image
        {
            Source = PAssets.PIcon.PIconRead("/PAssets/PSShared/PSAlert.svg", new SolidColorBrush(Color.FromRgb(0xB4, 0x23, 0x18))),
            Width = 28,
            Height = 28,
            Stretch = Stretch.Uniform,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(24, 24, 0, 12)
        };
        Grid.SetColumn(pIcon, 0);
        pContent.Children.Add(pIcon);

        var pMessageText = new TextBlock
        {
            Text = pMessage,
            FontSize = 14,
            Foreground = PSField.PSFieldText,
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(16, 24, 24, 12)
        };
        Grid.SetColumn(pMessageText, 1);
        pContent.Children.Add(pMessageText);
        return pContent;
    }
}
