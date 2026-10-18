using System.Windows;
using System.Windows.Controls;

namespace Cadroue.UIVeneer;

internal static class PSOptionsInstall
{
    internal static void PSSystemProgressApply(ProgressBar pProgress, double pValue) => pProgress.Value = pValue;

    internal static void PSSystemBusyApply(Button pInstall, ProgressBar pProgress)
    {
        pInstall.IsEnabled = false;
        pProgress.Value = 0;
        pProgress.Visibility = Visibility.Visible;
    }

    internal static void PSSystemProgressHide(ProgressBar pProgress) => pProgress.Visibility = Visibility.Collapsed;

    internal static void PSSystemInstallShow(Button pInstall, string pText) => pInstall.Content = pText;

    internal static void PSSystemIdleApply(Button pInstall) => pInstall.IsEnabled = true;

    internal static UIElement PSSystemLineBuild(Button pInstall, Button pBrowse, ProgressBar pProgress)
    {
        var pButtons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center
        };
        pButtons.Children.Add(pInstall);
        pButtons.Children.Add(pBrowse);
        pButtons.Children.Add(pProgress);

        return PSField.PSFieldBuild(string.Empty, pButtons);
    }

    internal static ProgressBar PSSystemProgressBuild() =>
        new()
        {
            Minimum = 0,
            Maximum = 1,
            Width = 220,
            Height = 8,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(16, 0, 0, 0),
            Foreground = null,
            Background = null,
            BorderThickness = new Thickness(0),
            Template = PSSystemTemplateBuild(),
            Visibility = Visibility.Collapsed
        };

    private static System.Windows.Controls.ControlTemplate PSSystemTemplateBuild()
    {
        const string pXaml = @"
<ControlTemplate xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                 xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
                 TargetType=""{x:Type ProgressBar}"">
    <Border CornerRadius=""4"" Background=""#E4E9F0"" ClipToBounds=""True"">
        <Grid>
            <Rectangle x:Name=""PART_Track"" />
            <Border x:Name=""PART_Indicator""
                    HorizontalAlignment=""Left""
                    CornerRadius=""4""
                    Background=""#4C86F7"" />
        </Grid>
    </Border>
</ControlTemplate>";
        return (System.Windows.Controls.ControlTemplate)System.Windows.Markup.XamlReader.Parse(pXaml);
    }
}
