using System.Windows;
using System.Windows.Controls;

namespace Cadroue.UIVeneer;

internal static class PSFieldProgress
{
    internal static ProgressBar PSFieldProgressBuild() =>
        new()
        {
            Minimum = 0,
            Maximum = 1,
            Width = 140,
            Height = 8,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(10, 0, 0, 0),
            Foreground = null,
            Background = null,
            BorderThickness = new Thickness(0),
            Template = PSFieldTemplateBuild(),
            Visibility = Visibility.Collapsed
        };

    private static ControlTemplate PSFieldTemplateBuild()
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
        return (ControlTemplate)System.Windows.Markup.XamlReader.Parse(pXaml);
    }
}
