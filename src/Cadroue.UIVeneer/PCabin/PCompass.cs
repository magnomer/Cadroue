using Cadroue.Core;
using Cadroue.Application;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Shapes;
using Cadroue.UIDeportment;
using Cadroue.UIVeneer.PAsset;
using Cadroue.UIVeneer.PBench;
using Cadroue.UIVeneer.PWing;

namespace Cadroue.UIVeneer.PCabin;

public sealed class PCompass : UserControl
{
    private sealed record PCompassFace(Button PCompassFaceButton, Image PCompassFaceImage, TextBlock PCompassFaceLabel);

    private const string PCompassWaveformIcon = "/PAsset/PCompass/PCompassWaveform.svg";

    private static readonly IReadOnlyDictionary<string, Brush> pCompassAccents = new Dictionary<string, Brush>
    {
        [LCompass.LCompassAccentPositive] = new SolidColorBrush(Color.FromRgb(0x2F, 0x9E, 0x64)),
        [LCompass.LCompassAccentNegative] = new SolidColorBrush(Color.FromRgb(0xD6, 0x45, 0x45)),
    };

    private static readonly IReadOnlyDictionary<bool, Brush> pCompassWaveformBrushes = new Dictionary<bool, Brush>
    {
        [true] = new SolidColorBrush(Color.FromRgb(0x2F, 0x80, 0xED)),
        [false] = new SolidColorBrush(Color.FromRgb(0x8A, 0x94, 0xA3)),
    };

    private readonly WrapPanel pCompassLinePanel;
    private readonly IReadOnlyList<StackPanel> pCompassGroups;
    private readonly IReadOnlyList<StackPanel> pCompassSectionGroups;
    private readonly List<Border> pCompassSeparators = new();
    private readonly Dictionary<string, PCompassFace> pCompassFaces = new(StringComparer.Ordinal);
    private readonly Image pCompassWaveformIcon;
    private readonly Slider pCompassVolumeSlider;
    private readonly TextBlock pCompassVolumeText;
    private readonly Border pCompassTrackFill;
    private readonly Grid pCompassSliderHost;

    public LCompass LCompass { get; }

    public PCompass(PFlow pFlow, PViewer pViewer, bool pCompassSectionShow = false)
    {
        LCompass = new LCompass(pFlow.LFlow, pViewer.LViewer, pCompassSectionShow);
        pCompassLinePanel = new WrapPanel { VerticalAlignment = VerticalAlignment.Center };
        pCompassVolumeText = new TextBlock
        {
            Width = 32,
            FontSize = 11,
            VerticalAlignment = VerticalAlignment.Center,
            TextAlignment = TextAlignment.Right
        };
        pCompassVolumeSlider = new Slider
        {
            Width = 132,
            Minimum = LCompass.LCompassVolumeMinimum,
            Maximum = LCompass.LCompassVolumeMaximum,
            VerticalAlignment = VerticalAlignment.Center,
            Focusable = false,
            ToolTip = LLocalization.LLocalizationTextRead("Compass.Volume.Label"),
            Style = PCompassSliderBuild()
        };
        pCompassTrackFill = new Border
        {
            Height = 4,
            CornerRadius = new CornerRadius(2),
            Background = new SolidColorBrush(Color.FromRgb(0x2F, 0x80, 0xED)),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center
        };
        pCompassSliderHost = new Grid { Width = 132, Height = 22, VerticalAlignment = VerticalAlignment.Center };
        pCompassWaveformIcon = new Image
        {
            Width = 24,
            Height = 24,
            Stretch = Stretch.Uniform,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        List<StackPanel> pGroups = LCompass.LCompassGroupsRead().Select(PCompassGroupBuild).ToList();
        StackPanel pVolumeGroup = PCompassGroupBuild();
        pVolumeGroup.Children.Add(PCompassVolumeBuild());
        pVolumeGroup.Children.Add(PCompassWaveformBuild());
        pGroups.Add(pVolumeGroup);
        pGroups.ForEach(pGroup => pCompassLinePanel.Children.Add(pGroup));
        pCompassGroups = pGroups;
        pCompassSectionGroups = LCompass.LCompassSectionRead().Select(PCompassGroupRead).ToList();

        pCompassVolumeSlider.ValueChanged += (_, _) => PCompassVolumeHandle();
        pViewer.LViewer.LViewerVolumeChange += PCompassValueHandle;
        pViewer.LViewer.LViewerPlayingChange += PCompassPlayingApply;
        pFlow.LFlow.LFlowEditChange += PCompassEditApply;
        pFlow.LFlow.LFlowWaveformChange += PCompassWaveformApply;
        PCompassEditApply(LCompass.LCompassEditActive);
        PCompassValueHandle(LCompass.LCompassVolume);
        PCompassWaveformApply(LCompass.LCompassWaveformActive);

        Content = new Border
        {
            MinHeight = 72,
            Child = pCompassLinePanel,
            SnapsToDevicePixels = true
        };
        pCompassLinePanel.SizeChanged += PCompassSizeHandle;
    }

    private StackPanel PCompassGroupBuild(LCompassGroup lGroup)
    {
        StackPanel pGroup = PCompassGroupBuild();
        lGroup.LCompassGroupButtons
            .Select(PCompassButtonBuild)
            .ToList()
            .ForEach(pButton => pGroup.Children.Add(pButton));
        return pGroup;
    }

    private StackPanel PCompassGroupBuild()
    {
        var pGroup = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center
        };
        var pSeparator = new Border
        {
            Width = 1,
            Margin = new Thickness(1, 14, 1, 12),
            Background = new SolidColorBrush(Color.FromRgb(0xDD, 0xE3, 0xEC))
        };
        pCompassSeparators.Add(pSeparator);
        pGroup.Children.Add(pSeparator);
        return pGroup;
    }

    private StackPanel PCompassGroupRead(int pIndex) => pCompassGroups[pIndex];

    private Button PCompassButtonBuild(LCompassButton lButton)
    {
        var pImage = new Image
        {
            Width = 24,
            Height = 24,
            Stretch = Stretch.Uniform,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        var pLabel = new TextBlock
        {
            FontSize = 11,
            HorizontalAlignment = HorizontalAlignment.Center,
            TextAlignment = TextAlignment.Center
        };
        var pStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        pStack.Children.Add(pImage);
        pStack.Children.Add(new Border { Height = 1 });
        pStack.Children.Add(pLabel);
        var pButton = new Button
        {
            Width = 58,
            Height = 58,
            Content = pStack,
            Style = PHouse.PButton.PButtonCommandCreate()
        };
        pButton.Click += (_, _) => LCompass.LCompassRun(lButton.LCompassButtonKey);
        pCompassFaces.Add(lButton.LCompassButtonKey, new PCompassFace(pButton, pImage, pLabel));
        PCompassFaceApply(lButton);
        return pButton;
    }

    private void PCompassFaceApply(LCompassButton lButton)
    {
        PCompassFace pFace = pCompassFaces[lButton.LCompassButtonKey];
        pFace.PCompassFaceImage.Source = PIcon.PIconRead(
            $"/PAsset/PCompass/{lButton.LCompassButtonIcon}",
            pCompassAccents.GetValueOrDefault(lButton.LCompassButtonAccent));
        pFace.PCompassFaceLabel.Text = lButton.LCompassButtonLabel;
        pFace.PCompassFaceButton.ToolTip = lButton.LCompassButtonTooltip;
    }

    private void PCompassPlayingApply(bool lPlaying) => PCompassFaceApply(LCompass.LCompassPlayRead(lPlaying));

    private void PCompassEditApply(bool lEditable) =>
        pCompassSectionGroups.ToList().ForEach(pGroup => pGroup.IsEnabled = lEditable);

    private void PCompassWaveformApply(bool lActive) =>
        pCompassWaveformIcon.Source = PIcon.PIconRead(PCompassWaveformIcon, pCompassWaveformBrushes[lActive]);

    private Button PCompassWaveformBuild()
    {
        var pStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        pStack.Children.Add(pCompassWaveformIcon);
        pStack.Children.Add(new Border { Height = 1 });
        pStack.Children.Add(new TextBlock
        {
            Text = LLocalization.LLocalizationTextRead("Compass.Waveform.Label"),
            FontSize = 11,
            HorizontalAlignment = HorizontalAlignment.Center,
            TextAlignment = TextAlignment.Center
        });

        var pButton = new Button
        {
            Width = 58,
            Height = 58,
            Content = pStack,
            Style = PHouse.PButton.PButtonCommandCreate(),
            ToolTip = LLocalization.LLocalizationTextRead("Compass.Waveform.Tooltip")
        };
        pButton.Click += (_, _) => LCompass.LCompassWaveformToggle();
        return pButton;
    }

    private void PCompassSizeHandle(object sender, SizeChangedEventArgs e) =>
        pCompassSeparators
            .Zip(LCompass.LCompassSeparatorsResolve(pCompassGroups.Select(PCompassTopRead).ToList()))
            .ToList()
            .ForEach(pPair => PCompassSeparatorApply(pPair.First, pPair.Second));

    private double PCompassTopRead(StackPanel pGroup) => pGroup.TranslatePoint(new Point(0, 0), pCompassLinePanel).Y;

    private static void PCompassSeparatorApply(Border pSeparator, double lOpacity) => pSeparator.Opacity = lOpacity;

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

    private void PCompassValueHandle(double lVolume)
    {
        pCompassVolumeSlider.Value = lVolume;
        pCompassVolumeText.Text = LCompass.LCompassVolumeFormat(lVolume);
        PCompassTrackUpdate();
    }

    private void PCompassVolumeHandle()
    {
        pCompassVolumeText.Text = LCompass.LCompassVolumeFormat(pCompassVolumeSlider.Value);
        PCompassTrackUpdate();
        LCompass.LCompassVolumeSet(pCompassVolumeSlider.Value);
    }

    private void PCompassTrackUpdate() =>
        pCompassTrackFill.Width = LCompass.LCompassFillResolve(
            pCompassSliderHost.ActualWidth,
            pCompassVolumeSlider.Value);

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
