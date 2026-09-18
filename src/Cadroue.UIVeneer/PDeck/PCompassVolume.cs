using Cadroue.Core;
using Cadroue.Application;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Shapes;
using Cadroue.UIVeneer.PPanel;

namespace Cadroue.UIVeneer.PDeck;

public sealed partial class PCompass
{
    private readonly Slider pCompassVolumeSlider;
    private readonly TextBlock pCompassVolumeText;
    private Border pCompassTrackFill = null!;
    private Grid pCompassSliderHost = null!;

    private Border PCompassVolumeBuild()
    {
        var pGrid = new Grid
        {
            Width = 268,
            Margin = new Thickness(10, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        FrameworkElement pIcon = PCompassIconBuild();
        var pLabel = new TextBlock
        {
            Text = LLocalization.LLocalizationTextRead("Compass.Volume.Label"),
            FontSize = 11,
            Foreground = new SolidColorBrush(Color.FromRgb(0x2D, 0x37, 0x48)),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(8, 0, 10, 0)
        };

        pCompassSliderHost = new Grid { Width = 132, Height = 22, VerticalAlignment = VerticalAlignment.Center };
        var pTrackBase = new Border
        {
            Height = 4,
            CornerRadius = new CornerRadius(2),
            Background = new SolidColorBrush(Color.FromRgb(0xD9, 0xE0, 0xEA)),
            VerticalAlignment = VerticalAlignment.Center
        };
        pCompassTrackFill = new Border
        {
            Height = 4,
            CornerRadius = new CornerRadius(2),
            Background = new SolidColorBrush(Color.FromRgb(0x2F, 0x80, 0xED)),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center
        };
        pCompassSliderHost.Children.Add(pTrackBase);
        pCompassSliderHost.Children.Add(pCompassTrackFill);
        pCompassSliderHost.Children.Add(pCompassVolumeSlider);
        pCompassSliderHost.SizeChanged += (_, _) => PCompassTrackUpdate();

        Grid.SetColumn(pIcon, 0);
        Grid.SetColumn(pLabel, 1);
        Grid.SetColumn(pCompassSliderHost, 2);
        Grid.SetColumn(pCompassVolumeText, 3);
        pGrid.Children.Add(pIcon);
        pGrid.Children.Add(pLabel);
        pGrid.Children.Add(pCompassSliderHost);
        pGrid.Children.Add(pCompassVolumeText);
        return new Border { Height = 58, Padding = new Thickness(8, 0, 0, 0), Child = pGrid };
    }

    private void PCompassValueHandle(double pVolume)
    {
        double pVolumeClamp = LPreferenceState.LPreferenceVolumeClamp(pVolume);
        pCompassVolumeSlider.Value = pVolumeClamp;
        pCompassVolumeText.Text = Math.Round(pVolumeClamp).ToString("0");
        PCompassTrackUpdate();
    }

    private void PCompassVolumeHandle(PViewer pViewer)
    {
        double pVolume = LPreferenceState.LPreferenceVolumeClamp(pCompassVolumeSlider.Value);
        if (pVolume == LCompass.LCompassVolume) return;
        pCompassVolumeText.Text = Math.Round(pVolume).ToString("0");
        PCompassTrackUpdate();
        pViewer.PViewerVolumeSet(pVolume);
    }

    private void PCompassTrackUpdate()
    {
        if (pCompassSliderHost is null || pCompassTrackFill is null) return;
        if (pCompassSliderHost.ActualWidth <= 0) return;
        double pRange = pCompassVolumeSlider.Maximum - pCompassVolumeSlider.Minimum;
        if (pRange <= 0) return;
        double pRate = (pCompassVolumeSlider.Value - pCompassVolumeSlider.Minimum) / pRange;
        pCompassTrackFill.Width = Math.Max(0, pCompassSliderHost.ActualWidth * pRate);
    }

    private static FrameworkElement PCompassIconBuild()
    {
        var pCanvas = new Canvas { Width = 24, Height = 24 };
        Brush pBrush = new SolidColorBrush(Color.FromRgb(0x3E, 0x4A, 0x5E));
        var pSpeaker = new Path { Fill = pBrush, Data = Geometry.Parse("M3,9 L7,9 L12,4 L12,20 L7,15 L3,15 Z") };
        var pWaveSmall = new Path
        {
            Stroke = pBrush,
            StrokeThickness = 1.8,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round,
            Data = Geometry.Parse("M15,8 C17,10 17,14 15,16")
        };
        var pWaveLarge = new Path
        {
            Stroke = pBrush,
            StrokeThickness = 1.8,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round,
            Data = Geometry.Parse("M17,5 C21,9 21,15 17,19")
        };
        pCanvas.Children.Add(pSpeaker);
        pCanvas.Children.Add(pWaveSmall);
        pCanvas.Children.Add(pWaveLarge);
        return new Viewbox { Width = 22, Height = 22, VerticalAlignment = VerticalAlignment.Center, Child = pCanvas };
    }

    private static Style PCompassSliderBuild()
    {
        const string pXaml = @"<Style xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'
               xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
               TargetType='{x:Type Slider}'>
  <Setter Property='Template'>
    <Setter.Value>
      <ControlTemplate TargetType='{x:Type Slider}'>
        <Grid Height='22' Background='Transparent'>
          <Track x:Name='PART_Track' Focusable='False' VerticalAlignment='Center'>
            <Track.DecreaseRepeatButton>
              <RepeatButton Command='{x:Static Slider.DecreaseLarge}' Focusable='False'>
                <RepeatButton.Template>
                  <ControlTemplate TargetType='{x:Type RepeatButton}'>
                    <Border Background='Transparent'/>
                  </ControlTemplate>
                </RepeatButton.Template>
              </RepeatButton>
            </Track.DecreaseRepeatButton>
            <Track.IncreaseRepeatButton>
              <RepeatButton Command='{x:Static Slider.IncreaseLarge}' Focusable='False'>
                <RepeatButton.Template>
                  <ControlTemplate TargetType='{x:Type RepeatButton}'>
                    <Border Background='Transparent'/>
                  </ControlTemplate>
                </RepeatButton.Template>
              </RepeatButton>
            </Track.IncreaseRepeatButton>
            <Track.Thumb>
              <Thumb Width='18' Height='18' Focusable='False'>
                <Thumb.Template>
                  <ControlTemplate TargetType='{x:Type Thumb}'>
                    <Ellipse Fill='White' Stroke='#C9D3E0' StrokeThickness='1'/>
                  </ControlTemplate>
                </Thumb.Template>
              </Thumb>
            </Track.Thumb>
          </Track>
        </Grid>
      </ControlTemplate>
    </Setter.Value>
  </Setter>
</Style>";
        return (Style)XamlReader.Parse(pXaml);
    }
}
