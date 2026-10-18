using System.Windows;
using System.Windows.Controls;

namespace Cadroue.UIVeneer;

internal static class PSEncoderSource
{
    internal static void PSVideoHintSet(UIElement? pNotice, bool pShown)
    {
        if (pNotice is not null)
        {
            pNotice.Visibility = pShown ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    internal static void PSVideoInkApply(Control pBox, bool pMuted) =>
        pBox.Foreground = pMuted ? PSField.PSFieldMuted : PSField.PSFieldText;
}
