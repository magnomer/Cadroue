using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.Application;

namespace Cadroue.UIVeneer.PPanel;

public sealed partial class PClinic
{
    public bool PClinicMinimizedCheck() => pClinicMinimized;

    public void PClinicMinimizeSet(bool pClinicMinimizeRequest)
    {
        if (pClinicMinimized == pClinicMinimizeRequest)
        {
            return;
        }

        pClinicMinimized = pClinicMinimizeRequest;
        pClinicFullBody.Visibility = pClinicMinimized ? Visibility.Collapsed : Visibility.Visible;
        pClinicStripBody.Visibility = pClinicMinimized ? Visibility.Visible : Visibility.Collapsed;
        PClinicMinimizeChange?.Invoke(pClinicMinimized);
    }

    private UIElement PClinicStripBuild()
    {
        Button pMaximizeButton = PClinicButtonBuild(
            "/PAsset/PPanel/PListMaximize.svg",
            LLocalization.LLocalizationTextRead("Inspector.Panel.ShowTooltip"),
            () => PClinicMinimizeSet(false));
        pMaximizeButton.Margin = new Thickness(0, 6, 0, 0);
        pMaximizeButton.HorizontalAlignment = HorizontalAlignment.Center;

        var pStrip = new StackPanel { Background = Brushes.White };
        pStrip.Children.Add(pMaximizeButton);
        return pStrip;
    }
}
