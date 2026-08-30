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
        PSDialog.PSDialogApply(this, PSDialogTheme.PSDialogThemeOrange);
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
        Content = PSWarningBuild(pTitle, pMessage);
    }

    internal static void PSWarningShow(Window? pOwner, string pTitle, string pMessage) =>
        new PSWarning(pOwner, pTitle, pMessage).ShowDialog();

    private UIElement PSWarningBuild(string pTitle, string pMessage) =>
        PSDialog.PSDialogBuild(this, pTitle, PSWarningBodyBuild(pMessage), PSDialogTheme.PSDialogThemeOrange);

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
        pConfirm.Foreground = new SolidColorBrush(Color.FromRgb(0xB4, 0x23, 0x18));
        pConfirm.BorderBrush = new SolidColorBrush(Color.FromRgb(0xB4, 0x23, 0x18));
        pConfirm.Click += (_, _) => Close();

        pFooter.Children.Add(pConfirm);
        DockPanel.SetDock(pFooter, Dock.Bottom);
        pBody.Children.Add(pFooter);

        pBody.Children.Add(PSWarningContentBuild(pMessage));
        return pBody;
    }

    private static Grid PSWarningContentBuild(string pMessage)
    {
        var pContent = new Grid();
        pContent.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        pContent.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var pIcon = new Image
        {
            Source = PAssets.PIcon.PIconRead("/PAssets/PSShared/PSWarning.svg", new SolidColorBrush(Color.FromRgb(0xB4, 0x6A, 0x18))),
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
