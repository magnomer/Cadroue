using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

internal static class PSDialogMessage
{
    internal static DockPanel PSDialogMessageBuild(
        string pMessage,
        string pIconPath,
        Color pIconColor,
        params Button?[] pButtons)
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

        pButtons.OfType<Button>().ToList().ForEach(pButton => pFooter.Children.Add(pButton));
        DockPanel.SetDock(pFooter, Dock.Bottom);
        pBody.Children.Add(pFooter);

        pBody.Children.Add(PSDialogContentBuild(pMessage, pIconPath, pIconColor));
        return pBody;
    }

    private static Grid PSDialogContentBuild(string pMessage, string pIconPath, Color pIconColor)
    {
        var pContent = new Grid();
        pContent.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        pContent.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var pIcon = new Image
        {
            Source = QIcon.QIconRead(pIconPath, new SolidColorBrush(pIconColor)),
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
