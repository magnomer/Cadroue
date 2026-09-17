using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.Application;
using Cadroue.UIVeneer.PHouse;

namespace Cadroue.UIVeneer.PPanel;

public sealed partial class PProcessing
{
    private bool pProcessingMinimized;

    public bool PProcessingMinimizedCheck() => pProcessingMinimized;

    public void PProcessingMinimizeSet(bool pProcessingMinimizeRequest)
    {
        if (pProcessingMinimized == pProcessingMinimizeRequest)
        {
            return;
        }

        pProcessingMinimized = pProcessingMinimizeRequest;
        pProcessingFullBody.Visibility = pProcessingMinimized ? Visibility.Collapsed : Visibility.Visible;
        pProcessingStripBody.Visibility = pProcessingMinimized ? Visibility.Visible : Visibility.Collapsed;
        PProcessingMinimizeChange?.Invoke(pProcessingMinimized);
    }

    private UIElement PProcessingStripBuild()
    {
        Button pMaximizeButton = PProcessingButtonBuild(
            "/PAsset/PPanel/PListMaximize.svg",
            LLocalization.LLocalizationTextRead("Processing.Show.Tooltip"),
            () => PProcessingMinimizeSet(false));
        pMaximizeButton.Margin = new Thickness(0, 6, 0, 0);
        pMaximizeButton.HorizontalAlignment = HorizontalAlignment.Center;

        var pStrip = new StackPanel { Background = Brushes.White };
        pStrip.Children.Add(pMaximizeButton);
        return pStrip;
    }
}
