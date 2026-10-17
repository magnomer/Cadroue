using Cadroue.Core;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.UIDeportment;
using Cadroue.UIVeneer.PBench;
using Cadroue.UIVeneer.PWing;

namespace Cadroue.UIVeneer.PCabin;

public sealed class PCompass : UserControl
{
    private sealed record PCompassFace(Button PCompassFaceButton, Image PCompassFaceImage, TextBlock PCompassFaceLabel);

    private const string PCompassWaveformIcon = "/PAsset/PCompass/PCompassWaveform.svg";

    private static readonly IReadOnlyDictionary<string, Brush> pCompassAccents = new Dictionary<string, Brush>
    {
        [QCompass.QCompassAccentPositive] = new SolidColorBrush(Color.FromRgb(0x2F, 0x9E, 0x64)),
        [QCompass.QCompassAccentNegative] = new SolidColorBrush(Color.FromRgb(0xD6, 0x45, 0x45)),
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

    public QCompass QCompass { get; }

    public PCompass(PFlow pFlow, PViewer pViewer, QCompass lCompass)
    {
        QCompass = lCompass;
        pCompassLinePanel = PCompassLineBuild();
        pCompassVolumeText = PCompassTextBuild();
        pCompassVolumeSlider = PCompassKnobBuild();
        pCompassTrackFill = PCompassFillBuild();
        pCompassSliderHost = PCompassHostBuild();
        pCompassWaveformIcon = PCompassGlyphBuild();
        pCompassGroups = PCompassGroupsBuild();
        pCompassSectionGroups = QCompass.QCompassSectionRead().Select(PCompassGroupRead).ToList();
        PCompassEventsAttach(pFlow, pViewer);
        PCompassEditApply(QCompass.QCompassEditActive);
        PCompassValueHandle(QCompass.QCompassVolume);
        PCompassWaveformApply(QCompass.QCompassWaveformActive);
        Content = PCompassFrameBuild();
        PCompassSizeAttach();
    }

    private static WrapPanel PCompassLineBuild() => new() { VerticalAlignment = VerticalAlignment.Center };

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

    private static Image PCompassGlyphBuild() => new()
    {
        Width = 24,
        Height = 24,
        Stretch = Stretch.Uniform,
        HorizontalAlignment = HorizontalAlignment.Center
    };

    private List<StackPanel> PCompassGroupsBuild()
    {
        List<StackPanel> pGroups = QCompass.QCompassGroupsRead().Select(PCompassGroupBuild).ToList();
        StackPanel pVolumeGroup = PCompassGroupBuild();
        pVolumeGroup.Children.Add(PCompassVolumeBuild());
        pVolumeGroup.Children.Add(PCompassWaveformBuild());
        pGroups.Add(pVolumeGroup);
        pGroups.ForEach(pGroup => pCompassLinePanel.Children.Add(pGroup));
        return pGroups;
    }

    private void PCompassEventsAttach(PFlow pFlow, PViewer pViewer)
    {
        pCompassVolumeSlider.ValueChanged += (_, _) => PCompassVolumeHandle();
        pViewer.QViewer.QViewerAudio.QViewerVolumeChanged += PCompassValueHandle;
        pViewer.QViewer.QViewerPlayingChanged += PCompassPlayingApply;
        pFlow.QFlow.QFlowEdit.QFlowEditChanged += PCompassEditApply;
        pFlow.QFlow.QFlowWaveformChanged += PCompassWaveformApply;
    }

    private Border PCompassFrameBuild() => new()
    {
        MinHeight = 72,
        Child = pCompassLinePanel,
        SnapsToDevicePixels = true
    };

    private void PCompassSizeAttach() => pCompassLinePanel.SizeChanged += PCompassSizeHandle;

    private StackPanel PCompassGroupBuild(QCompassGroup lGroup)
    {
        StackPanel pGroup = PCompassGroupBuild();
        lGroup.QCompassGroupButtons
            .Select(PCompassButtonBuild)
            .ToList()
            .ForEach(pButton => pGroup.Children.Add(pButton));
        return pGroup;
    }

    private StackPanel PCompassGroupBuild()
    {
        StackPanel pGroup = PCompassStackBuild();
        Border pSeparator = PCompassSeparatorBuild();
        pCompassSeparators.Add(pSeparator);
        pGroup.Children.Add(pSeparator);
        return pGroup;
    }

    private static StackPanel PCompassStackBuild() => new()
    {
        Orientation = Orientation.Horizontal,
        VerticalAlignment = VerticalAlignment.Center
    };

    private static Border PCompassSeparatorBuild() => new()
    {
        Width = 1,
        Margin = new Thickness(1, 14, 1, 12),
        Background = new SolidColorBrush(Color.FromRgb(0xDD, 0xE3, 0xEC))
    };

    private StackPanel PCompassGroupRead(int pIndex) => pCompassGroups[pIndex];

    private Button PCompassButtonBuild(QCompassButton lButton)
    {
        PCompassFace pFace = PCompassFaceBuild();
        PCompassClickAttach(pFace.PCompassFaceButton, lButton);
        QButton.QButtonAttach(pFace.PCompassFaceButton, QButtonLook.QButtonLookCommand);
        pCompassFaces.Add(lButton.QCompassButtonKey, pFace);
        PCompassFaceApply(lButton);
        return pFace.PCompassFaceButton;
    }

    private static PCompassFace PCompassFaceBuild()
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
            Content = pStack
        };
        return new PCompassFace(pButton, pImage, pLabel);
    }

    private void PCompassClickAttach(Button pButton, QCompassButton lButton) =>
        pButton.Click += (_, _) => QCompass.QCompassRun(lButton.QCompassButtonKey);

    private void PCompassFaceApply(QCompassButton lButton)
    {
        PCompassFace pFace = pCompassFaces[lButton.QCompassButtonKey];
        pFace.PCompassFaceImage.Source = QIcon.QIconRead(
            $"/PAsset/PCompass/{lButton.QCompassButtonIcon}",
            pCompassAccents.GetValueOrDefault(lButton.QCompassButtonAccent));
        pFace.PCompassFaceLabel.Text = lButton.QCompassButtonLabel;
        pFace.PCompassFaceButton.ToolTip = lButton.QCompassButtonTooltip;
    }

    private void PCompassPlayingApply(bool lPlaying) => PCompassFaceApply(QCompass.QCompassPlayRead(lPlaying));

    private void PCompassEditApply(bool lEditable) =>
        pCompassSectionGroups.ToList().ForEach(pGroup => pGroup.IsEnabled = lEditable);

    private void PCompassWaveformApply(bool lActive) =>
        pCompassWaveformIcon.Source = QIcon.QIconRead(PCompassWaveformIcon, pCompassWaveformBrushes[lActive]);

    private Button PCompassWaveformBuild()
    {
        Button pButton = PCompassSwitchBuild();
        PCompassSwitchAttach(pButton);
        QButton.QButtonAttach(pButton, QButtonLook.QButtonLookCommand);
        return pButton;
    }

    private void PCompassSwitchAttach(Button pButton) =>
        pButton.Click += (_, _) => QCompass.QCompassWaveformToggle();

    private Button PCompassSwitchBuild()
    {
        var pStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        pStack.Children.Add(pCompassWaveformIcon);
        pStack.Children.Add(new Border { Height = 1 });
        pStack.Children.Add(new TextBlock
        {
            Text = QLocalizationCatalog.QLocalizationTextRead("Compass.Waveform.Label"),
            FontSize = 11,
            HorizontalAlignment = HorizontalAlignment.Center,
            TextAlignment = TextAlignment.Center
        });

        var pButton = new Button
        {
            Width = 58,
            Height = 58,
            Content = pStack,
            ToolTip = QLocalizationCatalog.QLocalizationTextRead("Compass.Waveform.Tooltip")
        };
        return pButton;
    }

    private void PCompassSizeHandle(object sender, SizeChangedEventArgs e) =>
        pCompassSeparators
            .Zip(QCompass.QCompassSeparatorsResolve(pCompassGroups.Select(PCompassTopRead).ToList()))
            .ToList()
            .ForEach(pPair => PCompassSeparatorApply(pPair.First, pPair.Second));

    private double PCompassTopRead(StackPanel pGroup) => pGroup.TranslatePoint(new Point(0, 0), pCompassLinePanel).Y;

    private static void PCompassSeparatorApply(Border pSeparator, double lOpacity) => pSeparator.Opacity = lOpacity;

    private Border PCompassVolumeBuild()
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

    private void PCompassValueHandle(double lVolume)
    {
        pCompassVolumeSlider.Value = lVolume;
        pCompassVolumeText.Text = QCompass.QCompassVolumeFormat(lVolume);
        PCompassTrackUpdate();
    }

    private void PCompassVolumeHandle()
    {
        PCompassTextApply(pCompassVolumeSlider.Value);
        PCompassTrackUpdate();
        QCompass.QCompassVolumeSet(pCompassVolumeSlider.Value);
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
