using System.Globalization;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Markup;

namespace Cadroue.UIShell.PMainWindow;

internal static class PScrollbar
{
    internal const double PScrollbarThickness = 10;

    internal static void PScrollbarApply(FrameworkElement pHost)
    {
        pHost.Resources[typeof(ScrollBar)] = PScrollbarStyleBuild();
    }

    internal static Style PScrollbarStyleBuild()
    {
        const string pXaml = @"
<Style xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
       xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
       TargetType=""{x:Type ScrollBar}"">
    <Setter Property=""Background"" Value=""Transparent"" />
    <Setter Property=""BorderThickness"" Value=""0"" />
    <Setter Property=""Width"" Value=""$Thickness$"" />
    <Setter Property=""MinWidth"" Value=""$Thickness$"" />
    <Setter Property=""Focusable"" Value=""False"" />
    <Setter Property=""Template"">
        <Setter.Value>
            <ControlTemplate TargetType=""{x:Type ScrollBar}"">
                <Grid x:Name=""pScrollRoot"" Background=""Transparent"" SnapsToDevicePixels=""True"">
                    <Track x:Name=""PART_Track"" IsDirectionReversed=""True"">
                        <Track.DecreaseRepeatButton>
                            <RepeatButton Command=""ScrollBar.PageUpCommand"" Focusable=""False"" OverridesDefaultStyle=""True"">
                                <RepeatButton.Template>
                                    <ControlTemplate TargetType=""{x:Type RepeatButton}"">
                                        <Border Background=""Transparent"" />
                                    </ControlTemplate>
                                </RepeatButton.Template>
                            </RepeatButton>
                        </Track.DecreaseRepeatButton>
                        <Track.IncreaseRepeatButton>
                            <RepeatButton Command=""ScrollBar.PageDownCommand"" Focusable=""False"" OverridesDefaultStyle=""True"">
                                <RepeatButton.Template>
                                    <ControlTemplate TargetType=""{x:Type RepeatButton}"">
                                        <Border Background=""Transparent"" />
                                    </ControlTemplate>
                                </RepeatButton.Template>
                            </RepeatButton>
                        </Track.IncreaseRepeatButton>
                        <Track.Thumb>
                            <Thumb Focusable=""False"" OverridesDefaultStyle=""True"">
                                <Thumb.Template>
                                    <ControlTemplate TargetType=""{x:Type Thumb}"">
                                        <Border Background=""Transparent"" Padding=""3"">
                                            <Border x:Name=""pScrollThumb""
                                                    Background=""#D9DEE7""
                                                    CornerRadius=""2"" />
                                        </Border>
                                        <ControlTemplate.Triggers>
                                            <Trigger Property=""IsMouseOver"" Value=""True"">
                                                <Setter TargetName=""pScrollThumb"" Property=""Background"" Value=""#4C86F7"" />
                                            </Trigger>
                                            <Trigger Property=""IsDragging"" Value=""True"">
                                                <Setter TargetName=""pScrollThumb"" Property=""Background"" Value=""#2F6BDB"" />
                                            </Trigger>
                                        </ControlTemplate.Triggers>
                                    </ControlTemplate>
                                </Thumb.Template>
                            </Thumb>
                        </Track.Thumb>
                    </Track>
                </Grid>
                <ControlTemplate.Triggers>
                    <Trigger Property=""Orientation"" Value=""Horizontal"">
                        <Setter Property=""Width"" Value=""Auto"" />
                        <Setter Property=""MinWidth"" Value=""0"" />
                        <Setter Property=""Height"" Value=""$Thickness$"" />
                        <Setter Property=""MinHeight"" Value=""$Thickness$"" />
                        <Setter TargetName=""PART_Track"" Property=""IsDirectionReversed"" Value=""False"" />
                    </Trigger>
                </ControlTemplate.Triggers>
            </ControlTemplate>
        </Setter.Value>
    </Setter>
</Style>";
        return (Style)XamlReader.Parse(
            pXaml.Replace("$Thickness$", PScrollbarThickness.ToString(CultureInfo.InvariantCulture)));
    }
}
