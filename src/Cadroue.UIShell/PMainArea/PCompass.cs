using Cadroue.Core;
using Cadroue.Application;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Shapes;
using Cadroue.UIShell;
using Cadroue.UIShell.PAssets;
using PFlowControl = Cadroue.UIShell.PFlow.PFlow;

namespace Cadroue.UIShell.PMainArea;

public sealed class PCompass : UserControl
{
    private enum PCompassAction
    {
        PCompassZoomIn,
        PCompassZoomOut,
        PCompassPlayback,
        PCompassStandby,
        PCompassSectionNew,
        PCompassSectionDrop,
        PCompassSectionIn,
        PCompassSectionCut,
        PCompassSectionOut,
        PCompassKeyframePrevious,
        PCompassKeyframeNearest,
        PCompassKeyframeNext
    }

    private static readonly Brush pCompassPositiveBrush = new SolidColorBrush(Color.FromRgb(0x2F, 0x9E, 0x64));
    private static readonly Brush pCompassNegativeBrush = new SolidColorBrush(Color.FromRgb(0xD6, 0x45, 0x45));
    private static readonly Brush pCompassAccentBrush = new SolidColorBrush(Color.FromRgb(0x2F, 0x80, 0xED));
    private static readonly Brush pCompassRestBrush = new SolidColorBrush(Color.FromRgb(0x8A, 0x94, 0xA3));
    private const string PCompassWaveformIcon = "/PAssets/PCompass/PCompassWaveform.svg";
    private Image pCompassWaveformIcon = null!;
    private readonly Slider pCompassVolumeSlider;
    private readonly TextBlock pCompassVolumeText;

    private readonly WrapPanel pCompassLinePanel;
    private Border pCompassTrackFill = null!;
    private Grid pCompassSliderHost = null!;
    private bool pCompassProgramValue;

    private readonly PFlowControl pCompassFlow;
    private Image pCompassPlayImage = null!;
    private TextBlock pCompassPlayLabel = null!;
    private Button pCompassPlayButton = null!;
    private bool pCompassPlaying;

    public PCompass(PFlowControl pFlow, bool pCompassSectionShow = false)
    {
        pCompassFlow = pFlow;
        pCompassLinePanel = new WrapPanel { VerticalAlignment = VerticalAlignment.Center };

        (PCompassAction Action, string Icon, string LabelKey, string TooltipKey, Action Click, bool GroupEnd, bool Section)[] pButtons =
        {
            (PCompassAction.PCompassZoomIn, "PCompassZoomIncrease.svg", "Compass.ZoomIn.Label", "Compass.ZoomIn.Tooltip", () => pFlow.PFlowShortcutDispatch("zoomIn"), false, false),
            (PCompassAction.PCompassZoomOut, "PCompassZoomDecrease.svg", "Compass.ZoomOut.Label", "Compass.ZoomOut.Tooltip", () => pFlow.PFlowShortcutDispatch("zoomOut"), true, false),
            (PCompassAction.PCompassPlayback, "PCompassPlay.svg", "Compass.Play.Label", "Compass.Play.Tooltip", PCompassPlayToggle, true, false),
            (PCompassAction.PCompassSectionNew, "PCompassSectionAdd.svg", "Compass.SectionAdd.Label", "Compass.SectionAdd.Tooltip", () => pFlow.PFlowShortcutDispatch("addSection"), false, true),
            (PCompassAction.PCompassSectionDrop, "PCompassRemove.svg", "Compass.SectionDelete.Label", "Compass.SectionDelete.Tooltip", () => pFlow.PFlowShortcutDispatch("deleteSection"), true, true),
            (PCompassAction.PCompassSectionIn, "PCompassStart.svg", "Compass.SectionStart.Label", "Compass.SectionStart.Tooltip", () => pFlow.PFlowShortcutDispatch("setStart"), false, true),
            (PCompassAction.PCompassSectionCut, "PCompassSplit.svg", "Compass.SectionSplit.Label", "Compass.SectionSplit.Tooltip", () => pFlow.PFlowShortcutDispatch("splitSection"), false, true),
            (PCompassAction.PCompassSectionOut, "PCompassEnd.svg", "Compass.SectionEnd.Label", "Compass.SectionEnd.Tooltip", () => pFlow.PFlowShortcutDispatch("setEnd"), true, true),
            (PCompassAction.PCompassKeyframePrevious, "PCompassKeyframePrevious.svg", "Compass.KeyframePrevious.Label", "Compass.KeyframePrevious.Tooltip", () => pFlow.PFlowShortcutDispatch("previousKey"), false, false),
            (PCompassAction.PCompassKeyframeNearest, "PCompassKeyframeNear.svg", "Compass.KeyframeNearest.Label", "Compass.KeyframeNearest.Tooltip", () => pFlow.PFlowShortcutDispatch("nearestKey"), false, false),
            (PCompassAction.PCompassKeyframeNext, "PCompassKeyframeNext.svg", "Compass.KeyframeNext.Label", "Compass.KeyframeNext.Tooltip", () => pFlow.PFlowShortcutDispatch("nextKey"), true, false)
        };

        StackPanel pGroup = PCompassGroupBuild();
        foreach ((PCompassAction pAction, string pIcon, string pLabelKey, string pTooltipKey, Action pClick, bool pGroupEnd, bool pSection) in pButtons)
        {
            if (pSection && !pCompassSectionShow)
            {
                continue;
            }

            Button pButton = pAction == PCompassAction.PCompassPlayback
                ? PCompassToggleBuild()
                : PCompassButtonBuild(pAction, pIcon, pLabelKey, pTooltipKey);
            pButton.Click += (_, _) => pClick();
            pGroup.Children.Add(pButton);
            if (pGroupEnd)
            {
                pCompassLinePanel.Children.Add(pGroup);
                pGroup = PCompassGroupBuild();
            }
        }

        pCompassVolumeText = new TextBlock { Width = 32, FontSize = 11, VerticalAlignment = VerticalAlignment.Center, TextAlignment = TextAlignment.Right };
        pCompassVolumeSlider = new Slider
        {
            Width = 132,
            Minimum = 0,
            Maximum = 100,
            VerticalAlignment = VerticalAlignment.Center,
            Focusable = false,
            ToolTip = LLocalization.LLocalizationTextRead("Compass.Volume.Label"),
            Style = PCompassSliderBuild()
        };
        pCompassVolumeSlider.ValueChanged += (_, _) => PCompassVolumeHandle(pFlow);
        pFlow.PFlowVolumeValue += PCompassValueHandle;
        pFlow.PFlowPlayingChange += PCompassPlayingApply;

        StackPanel pVolumeGroup = PCompassGroupBuild();
        pVolumeGroup.Children.Add(PCompassVolumeBuild());
        pVolumeGroup.Children.Add(PCompassWaveformBuild(pFlow));
        pCompassLinePanel.Children.Add(pVolumeGroup);
        PCompassValueHandle(LPreference.LPreferenceStateCurrent.LPreferenceVolume);
        pFlow.PFlowWaveformChange += PCompassWaveformApply;
        PCompassWaveformApply(pFlow.PFlowWaveformCheck());

        Content = new Border
        {
            MinHeight = 72,
            Child = pCompassLinePanel,
            SnapsToDevicePixels = true
        };
        pCompassLinePanel.SizeChanged += PCompassSizeHandle;
    }

    private static StackPanel PCompassGroupBuild()
    {
        var pGroup = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        pGroup.Children.Add(PCompassSeparatorBuild());
        return pGroup;
    }

    private void PCompassSizeHandle(object sender, SizeChangedEventArgs e)
    {
        PCompassSeparatorUpdate();
    }

    private void PCompassSeparatorUpdate()
    {
        double pLineTop = double.NaN;
        foreach (UIElement pChild in pCompassLinePanel.Children)
        {
            if (pChild is not Panel pGroup || pGroup.Children.Count == 0) continue;
            double pGroupTop = pGroup.TranslatePoint(new Point(0, 0), pCompassLinePanel).Y;
            bool pLineStart = double.IsNaN(pLineTop) || pGroupTop > pLineTop;
            if (pLineStart)
            {
                pLineTop = pGroupTop;
            }

            if (pGroup.Children[0] is Border pSeparator)
            {
                pSeparator.Opacity = pLineStart ? 0 : 1;
            }
        }
    }

    private Border PCompassVolumeBuild()
    {
        var pGrid = new Grid { Width = 268, Margin = new Thickness(10, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
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

    private Button PCompassWaveformBuild(PFlowControl pFlow)
    {
        pCompassWaveformIcon = new Image
        {
            Width = 24,
            Height = 24,
            Stretch = Stretch.Uniform,
            HorizontalAlignment = HorizontalAlignment.Center
        };

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
            Style = PMainWindow.PButton.PButtonCommandCreate(),
            ToolTip = LLocalization.LLocalizationTextRead("Compass.Waveform.Tooltip")
        };
        pButton.Click += (_, _) => pFlow.PFlowWaveformSet(!pFlow.PFlowWaveformCheck());
        return pButton;
    }

    private void PCompassWaveformApply(bool pCompassWaveformActive)
    {
        pCompassWaveformIcon.Source = PIcon.PIconRead(
            PCompassWaveformIcon,
            pCompassWaveformActive ? pCompassAccentBrush : pCompassRestBrush);
    }

    private void PCompassValueHandle(double pVolume)
    {
        pCompassProgramValue = true;
        double pVolumeClamp = LPreferenceState.LPreferenceVolumeClamp(pVolume);
        pCompassVolumeSlider.Value = pVolumeClamp;
        pCompassVolumeText.Text = Math.Round(pVolumeClamp).ToString("0");
        PCompassTrackUpdate();
        pCompassProgramValue = false;
    }

    private void PCompassVolumeHandle(PFlowControl pFlow)
    {
        if (pCompassProgramValue) return;
        double pVolume = LPreferenceState.LPreferenceVolumeClamp(pCompassVolumeSlider.Value);
        pCompassVolumeText.Text = Math.Round(pVolume).ToString("0");
        PCompassTrackUpdate();
        pFlow.PFlowVolumeRaise(pVolume);
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

    private static Button PCompassButtonBuild(
        PCompassAction pAction,
        string pIconAssetName,
        string pLabelKey,
        string pTooltipKey)
    {
        string pLabelText = LLocalization.LLocalizationTextRead(pLabelKey);
        var pStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        pStack.Children.Add(new Image
        {
            Source = PIcon.PIconRead($"/PAssets/PCompass/{pIconAssetName}", PCompassAccentRead(pAction)),
            Width = 24,
            Height = 24,
            Stretch = Stretch.Uniform,
            HorizontalAlignment = HorizontalAlignment.Center
        });
        pStack.Children.Add(new Border { Height = 1 });
        pStack.Children.Add(new TextBlock { Text = pLabelText, FontSize = 11, HorizontalAlignment = HorizontalAlignment.Center, TextAlignment = TextAlignment.Center });
        return new Button
        {
            Width = 58,
            Height = 58,
            Content = pStack,
            Style = PMainWindow.PButton.PButtonCommandCreate(),
            ToolTip = LLocalization.LLocalizationTextRead(pTooltipKey)
        };
    }

    private Button PCompassToggleBuild()
    {
        pCompassPlayImage = new Image
        {
            Width = 24,
            Height = 24,
            Stretch = Stretch.Uniform,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        pCompassPlayLabel = new TextBlock { FontSize = 11, HorizontalAlignment = HorizontalAlignment.Center, TextAlignment = TextAlignment.Center };
        var pStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        pStack.Children.Add(pCompassPlayImage);
        pStack.Children.Add(new Border { Height = 1 });
        pStack.Children.Add(pCompassPlayLabel);
        pCompassPlayButton = new Button
        {
            Width = 58,
            Height = 58,
            Content = pStack,
            Style = PMainWindow.PButton.PButtonCommandCreate()
        };
        PCompassPlayingApply(false);
        return pCompassPlayButton;
    }

    private void PCompassPlayingApply(bool pCompassPlayingNow)
    {
        pCompassPlaying = pCompassPlayingNow;
        PCompassAction pAction = pCompassPlayingNow ? PCompassAction.PCompassStandby : PCompassAction.PCompassPlayback;
        string pIcon = pCompassPlayingNow ? "PCompassPause.svg" : "PCompassPlay.svg";
        string pLabelKey = pCompassPlayingNow ? "Compass.Pause.Label" : "Compass.Play.Label";
        string pTooltipKey = pCompassPlayingNow ? "Compass.Pause.Tooltip" : "Compass.Play.Tooltip";
        pCompassPlayImage.Source = PIcon.PIconRead($"/PAssets/PCompass/{pIcon}", PCompassAccentRead(pAction));
        pCompassPlayLabel.Text = LLocalization.LLocalizationTextRead(pLabelKey);
        pCompassPlayButton.ToolTip = LLocalization.LLocalizationTextRead(pTooltipKey);
    }

    private void PCompassPlayToggle()
    {
        if (pCompassPlaying)
        {
            pCompassFlow.PFlowPauseRaise();
        }
        else
        {
            pCompassFlow.PFlowPlayRaise();
        }
    }

    private static Border PCompassSeparatorBuild() => new() { Width = 1, Margin = new Thickness(1, 14, 1, 12), Background = new SolidColorBrush(Color.FromRgb(0xDD, 0xE3, 0xEC)) };

    private static Brush? PCompassAccentRead(PCompassAction pAction) => pAction switch
    {
        PCompassAction.PCompassPlayback => pCompassPositiveBrush,
        PCompassAction.PCompassSectionDrop => pCompassNegativeBrush,
        _ => null
    };
}
