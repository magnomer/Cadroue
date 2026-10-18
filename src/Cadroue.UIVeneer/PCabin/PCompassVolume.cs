using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer.PCabin;

internal sealed class PCompassVolume
{
    private readonly QCompass lCompass;
    private readonly Slider pCompassVolumeSlider;
    private readonly TextBlock pCompassVolumeText;
    private readonly Border pCompassTrackFill;
    private readonly Grid pCompassSliderHost;

    public PCompassVolume(QCompass lCompassOwner)
    {
        lCompass = lCompassOwner;
        pCompassVolumeText = PCompassTextBuild();
        pCompassVolumeSlider = PCompassKnobBuild();
        pCompassTrackFill = PCompassFillBuild();
        pCompassSliderHost = PCompassHostBuild();
    }

    private static TextBlock PCompassTextBuild() => new()
    {
        Width = 32,
        FontSize = 11,
        VerticalAlignment = VerticalAlignment.Center,
        TextAlignment = TextAlignment.Right
    };

    private Slider PCompassKnobBuild()
    {
        return new Slider
        {
            Width = 132,
            Minimum = QCompass.QCompassVolumeMinimum,
            Maximum = QCompass.QCompassVolumeMaximum,
            VerticalAlignment = VerticalAlignment.Center,
            Focusable = false,
            ToolTip = QLocalizationCatalog.QLocalizationTextRead("Compass.Volume.Label"),
            Style = PCompassSlider.PCompassSliderBuild()
        };
    }

    private static Border PCompassFillBuild() => new()
    {
        Height = 4,
        CornerRadius = new CornerRadius(2),
        Background = new SolidColorBrush(Color.FromRgb(0x2F, 0x80, 0xED)),
        HorizontalAlignment = HorizontalAlignment.Left,
        VerticalAlignment = VerticalAlignment.Center
    };

    private static Grid PCompassHostBuild() =>
        new() { Width = 132, Height = 22, VerticalAlignment = VerticalAlignment.Center };

    public void PCompassVolumeAttach(QViewerAudio lAudio)
    {
        pCompassVolumeSlider.ValueChanged += (_, _) => PCompassVolumeHandle();
        lAudio.QViewerVolumeChanged += PCompassValueHandle;
    }

    public Border PCompassVolumeBuild()
    {
        Border pVolume = PCompassPanelBuild();
        PCompassHostAttach();
        return pVolume;
    }

    private void PCompassHostAttach() => pCompassSliderHost.SizeChanged += (_, _) => PCompassTrackUpdate();

    private Border PCompassPanelBuild()
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
            Text = QLocalizationCatalog.QLocalizationTextRead("Compass.Volume.Label"),
            FontSize = 11,
            Foreground = new SolidColorBrush(Color.FromRgb(0x2D, 0x37, 0x48)),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(8, 0, 10, 0)
        };

        var pTrackBase = new Border
        {
            Height = 4,
            CornerRadius = new CornerRadius(2),
            Background = new SolidColorBrush(Color.FromRgb(0xD9, 0xE0, 0xEA)),
            VerticalAlignment = VerticalAlignment.Center
        };
        pCompassSliderHost.Children.Add(pTrackBase);
        pCompassSliderHost.Children.Add(pCompassTrackFill);
        pCompassSliderHost.Children.Add(pCompassVolumeSlider);

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

    public void PCompassValueHandle(double lVolume)
    {
        pCompassVolumeSlider.Value = lVolume;
        pCompassVolumeText.Text = QCompass.QCompassVolumeFormat(lVolume);
        PCompassTrackUpdate();
    }

    private void PCompassVolumeHandle()
    {
        PCompassTextApply(pCompassVolumeSlider.Value);
        PCompassTrackUpdate();
        lCompass.QCompassVolumeSet(pCompassVolumeSlider.Value);
    }

    private void PCompassTextApply(double lVolume) => pCompassVolumeText.Text = QCompass.QCompassVolumeFormat(lVolume);

    private void PCompassTrackUpdate() =>
        pCompassTrackFill.Width = QCompass.QCompassFillResolve(
            pCompassSliderHost.ActualWidth,
            pCompassVolumeSlider.Value);

    private static FrameworkElement PCompassIconBuild()
    {
        return new Image
        {
            Source = QIcon.QIconRead("/PAsset/P32/PCompass/CompassVolume.png"),
            Width = 22,
            Height = 22,
            Stretch = Stretch.Uniform,
            VerticalAlignment = VerticalAlignment.Center
        };
    }
}
