using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

internal sealed class PSLoupePlayback
{
    private const double PSLoupeIconSize = 22;
    private const double PSLoupeButtonSize = 36;

    private static readonly SolidColorBrush PSLoupePlayBrush = new(Color.FromRgb(0x2F, 0x9E, 0x64));

    private static readonly IReadOnlyDictionary<bool, Brush?> psLoupePlayTints = new Dictionary<bool, Brush?>
    {
        [true] = PSLoupePlayBrush,
        [false] = null,
    };

    private readonly QSLoupe lsLoupe;
    private readonly Image psLoupePlayImage;

    internal Button PSLoupePlayButton { get; }

    internal PSLoupePlayback(QSLoupe lLoupe)
    {
        lsLoupe = lLoupe;
        psLoupePlayImage = PSLoupeImageBuild();
        PSLoupePlayButton = PSLoupePlayBuild(psLoupePlayImage);
        PSLoupePlayAttach();
        QButton.QButtonAttach(PSLoupePlayButton, QButtonLook.QButtonLookPanel);
    }

    internal void PSLoupeIconApply() =>
        psLoupePlayImage.Source = QIcon.QIconRead(lsLoupe.QSLoupePlayIcon, psLoupePlayTints[lsLoupe.QSLoupePlayTinted]);

    internal void PSLoupeTipShow() => PSLoupePlayButton.ToolTip = lsLoupe.QSLoupePlayTip;

    private static Image PSLoupeImageBuild() => new()
    {
        Width = PSLoupeIconSize,
        Height = PSLoupeIconSize,
        Stretch = Stretch.Uniform,
        HorizontalAlignment = HorizontalAlignment.Center,
        VerticalAlignment = VerticalAlignment.Center
    };

    private static Button PSLoupePlayBuild(Image pPlayImage) => new()
    {
        Width = PSLoupeButtonSize,
        Height = PSLoupeButtonSize,
        Content = pPlayImage
    };

    private void PSLoupePlayAttach() => PSLoupePlayButton.Click += PSLoupePlayHandle;

    private void PSLoupePlayHandle(object pSender, RoutedEventArgs pEvent) => lsLoupe.QSLoupePlayToggle();
}
