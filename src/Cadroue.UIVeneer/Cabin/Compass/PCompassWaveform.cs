using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

internal sealed class PCompassWaveform
{
    private const string PCompassWaveformIcon = "/PAsset/PCompass/PCompassWaveform.svg";

    private static readonly IReadOnlyDictionary<bool, Brush> pCompassWaveformBrushes = new Dictionary<bool, Brush>
    {
        [true] = new SolidColorBrush(Color.FromRgb(0x2F, 0x80, 0xED)),
        [false] = new SolidColorBrush(Color.FromRgb(0x8A, 0x94, 0xA3)),
    };

    private readonly QCompass lCompass;
    private readonly Image pCompassWaveformIcon;

    public PCompassWaveform(QCompass lCompassOwner)
    {
        lCompass = lCompassOwner;
        pCompassWaveformIcon = PCompassGlyphBuild();
    }

    private static Image PCompassGlyphBuild() => new()
    {
        Width = 24,
        Height = 24,
        Stretch = Stretch.Uniform,
        HorizontalAlignment = HorizontalAlignment.Center
    };

    public void PCompassWaveformApply(bool lActive) =>
        pCompassWaveformIcon.Source = QIcon.QIconRead(PCompassWaveformIcon, pCompassWaveformBrushes[lActive]);

    public Button PCompassWaveformBuild()
    {
        Button pButton = PCompassSwitchBuild();
        PCompassSwitchAttach(pButton);
        QButton.QButtonAttach(pButton, QButtonLook.QButtonLookCommand);
        return pButton;
    }

    private void PCompassSwitchAttach(Button pButton) =>
        pButton.Click += (_, _) => lCompass.QCompassWaveformToggle();

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
}
