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

    private static readonly IReadOnlyDictionary<string, Brush> pCompassAccents = new Dictionary<string, Brush>
    {
        [QCompass.QCompassAccentPositive] = new SolidColorBrush(Color.FromRgb(0x2F, 0x9E, 0x64)),
        [QCompass.QCompassAccentNegative] = new SolidColorBrush(Color.FromRgb(0xD6, 0x45, 0x45)),
    };

    private readonly WrapPanel pCompassLinePanel;
    private readonly IReadOnlyList<StackPanel> pCompassGroups;
    private readonly IReadOnlyList<StackPanel> pCompassSectionGroups;
    private readonly List<Border> pCompassSeparators = new();
    private readonly Dictionary<string, PCompassFace> pCompassFaces = new(StringComparer.Ordinal);
    private readonly PCompassVolume pCompassVolume;
    private readonly PCompassWaveform pCompassWaveform;

    public QCompass QCompass { get; }

    public PCompass(PFlow pFlow, PViewer pViewer, QCompass lCompass)
    {
        QCompass = lCompass;
        pCompassLinePanel = PCompassLineBuild();
        pCompassVolume = new PCompassVolume(lCompass);
        pCompassWaveform = new PCompassWaveform(lCompass);
        pCompassGroups = PCompassGroupsBuild();
        pCompassSectionGroups = QCompass.QCompassSectionRead().Select(PCompassGroupRead).ToList();
        PCompassEventsAttach(pFlow, pViewer);
        PCompassEditApply(QCompass.QCompassEditActive);
        pCompassVolume.PCompassValueHandle(QCompass.QCompassVolume);
        pCompassWaveform.PCompassWaveformApply(QCompass.QCompassWaveformActive);
        Content = PCompassFrameBuild();
        PCompassSizeAttach();
    }

    private static WrapPanel PCompassLineBuild() => new() { VerticalAlignment = VerticalAlignment.Center };

    private List<StackPanel> PCompassGroupsBuild()
    {
        List<StackPanel> pGroups = QCompass.QCompassGroupsRead().Select(PCompassGroupBuild).ToList();
        StackPanel pVolumeGroup = PCompassGroupBuild();
        pVolumeGroup.Children.Add(pCompassVolume.PCompassVolumeBuild());
        pVolumeGroup.Children.Add(pCompassWaveform.PCompassWaveformBuild());
        pGroups.Add(pVolumeGroup);
        pGroups.ForEach(pGroup => pCompassLinePanel.Children.Add(pGroup));
        return pGroups;
    }

    private void PCompassEventsAttach(PFlow pFlow, PViewer pViewer)
    {
        pCompassVolume.PCompassVolumeAttach(pViewer.QViewer.QViewerAudio);
        pViewer.QViewer.QViewerPlayingChanged += PCompassPlayingApply;
        pFlow.QFlow.QFlowEdit.QFlowEditChanged += PCompassEditApply;
        pFlow.QFlow.QFlowWaveformChanged += pCompassWaveform.PCompassWaveformApply;
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

    private void PCompassSizeHandle(object sender, SizeChangedEventArgs e) =>
        pCompassSeparators
            .Zip(QCompass.QCompassSeparatorsResolve(pCompassGroups.Select(PCompassTopRead).ToList()))
            .ToList()
            .ForEach(pPair => PCompassSeparatorApply(pPair.First, pPair.Second));

    private double PCompassTopRead(StackPanel pGroup) => pGroup.TranslatePoint(new Point(0, 0), pCompassLinePanel).Y;

    private static void PCompassSeparatorApply(Border pSeparator, double lOpacity) => pSeparator.Opacity = lOpacity;
}
