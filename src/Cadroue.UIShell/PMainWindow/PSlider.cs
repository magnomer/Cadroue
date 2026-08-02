using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Markup;
using System.Windows.Media;

namespace Cadroue.UIShell.PMainWindow;

internal static class PSlider
{
    internal const double PSliderHeight = 24;

    internal static void PSliderApply(Slider pSlider)
    {
        pSlider.Height = PSliderHeight;
        pSlider.IsMoveToPointEnabled = true;
        pSlider.IsSnapToTickEnabled = false;
        pSlider.FocusVisualStyle = null;
        pSlider.VerticalAlignment = VerticalAlignment.Center;
        pSlider.Template = PSliderTemplateBuild();
    }

    internal static void PSliderResetApply(Slider pSlider, Func<double> pDefaultRead)
    {
        pSlider.PreviewMouseLeftButtonDown += (_, pEvent) =>
        {
            if (pEvent.ClickCount < 2
                || pEvent.OriginalSource is not DependencyObject pSource
                || !PSliderThumbHit(pSource))
            {
                return;
            }

            pSlider.Value = pDefaultRead();
            pEvent.Handled = true;
        };
    }

    private static bool PSliderThumbHit(DependencyObject pSource)
    {
        for (DependencyObject? pNode = pSource; pNode is not null; pNode = VisualTreeHelper.GetParent(pNode))
        {
            if (pNode is Thumb)
            {
                return true;
            }
        }

        return false;
    }

    private static ControlTemplate PSliderTemplateBuild()
    {
        const string pXaml = @"
<ControlTemplate xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                 xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
                 TargetType=""{x:Type Slider}"">
    <Grid Background=""Transparent"" VerticalAlignment=""Center"" Height=""24"">
        <Border Height=""4""
                CornerRadius=""2""
                Background=""#E4E9F0""
                VerticalAlignment=""Center"" />
        <Track x:Name=""PART_Track"">
            <Track.DecreaseRepeatButton>
                <RepeatButton Focusable=""False"" OverridesDefaultStyle=""True"" Command=""Slider.DecreaseLarge"">
                    <RepeatButton.Template>
                        <ControlTemplate TargetType=""{x:Type RepeatButton}"">
                            <Grid Background=""Transparent"">
                                <Border x:Name=""pSliderFill""
                                        Height=""4""
                                        CornerRadius=""2""
                                        Background=""#4C86F7""
                                        VerticalAlignment=""Center"" />
                            </Grid>
                        </ControlTemplate>
                    </RepeatButton.Template>
                </RepeatButton>
            </Track.DecreaseRepeatButton>
            <Track.IncreaseRepeatButton>
                <RepeatButton Focusable=""False"" OverridesDefaultStyle=""True"" Command=""Slider.IncreaseLarge"">
                    <RepeatButton.Template>
                        <ControlTemplate TargetType=""{x:Type RepeatButton}"">
                            <Grid Background=""Transparent"" />
                        </ControlTemplate>
                    </RepeatButton.Template>
                </RepeatButton>
            </Track.IncreaseRepeatButton>
            <Track.Thumb>
                <Thumb x:Name=""pSliderThumb"" Width=""16"" Height=""16"" Focusable=""False"" OverridesDefaultStyle=""True"">
                    <Thumb.Template>
                        <ControlTemplate TargetType=""{x:Type Thumb}"">
                            <Grid Background=""Transparent"">
                                <Ellipse x:Name=""pSliderKnob""
                                         Width=""16""
                                         Height=""16""
                                         Fill=""White""
                                         Stroke=""#4C86F7""
                                         StrokeThickness=""2"" />
                            </Grid>
                            <ControlTemplate.Triggers>
                                <Trigger Property=""IsMouseOver"" Value=""True"">
                                    <Setter TargetName=""pSliderKnob"" Property=""Stroke"" Value=""#2F6BDB"" />
                                    <Setter TargetName=""pSliderKnob"" Property=""Fill"" Value=""#F7F9FC"" />
                                </Trigger>
                                <Trigger Property=""IsDragging"" Value=""True"">
                                    <Setter TargetName=""pSliderKnob"" Property=""Stroke"" Value=""#2F6BDB"" />
                                    <Setter TargetName=""pSliderKnob"" Property=""StrokeThickness"" Value=""5"" />
                                </Trigger>
                            </ControlTemplate.Triggers>
                        </ControlTemplate>
                    </Thumb.Template>
                </Thumb>
            </Track.Thumb>
        </Track>
    </Grid>
    <ControlTemplate.Triggers>
        <Trigger Property=""IsEnabled"" Value=""False"">
            <Setter Property=""Opacity"" Value=""0.45"" />
        </Trigger>
    </ControlTemplate.Triggers>
</ControlTemplate>";
        return (ControlTemplate)XamlReader.Parse(pXaml);
    }
}
