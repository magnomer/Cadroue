using System.Windows;
using System.Windows.Controls;
using Cadroue.Application;
using Cadroue.UIVeneer.PHouse;

namespace Cadroue.UIVeneer.PWing;

public sealed partial class PInspector
{
    public void PCropPersistentApply(bool pCropPersistent) => LCropboxState.LCropboxPersistentSet(pCropPersistent);

    private UIElement PInspectorPersistentBuild()
    {
        pInspectorPersistentBox = PInspectorSwitchBuild(
            LLocalization.LLocalizationTextRead("Inspector.Common.Persistent"),
            LLocalization.LLocalizationTextRead("Inspector.Crop.PersistentTooltip"));
        PInspectorSwitchAttach(pInspectorPersistentBox, LCropboxState.LCropboxPersistentSet);

        var pPersistentPanel = new StackPanel { Visibility = Visibility.Collapsed };
        pPersistentPanel.Children.Add(new Border
        {
            Height = 1,
            Background = PPanelLineBrush,
            Margin = new Thickness(12, 0, 12, 12)
        });
        foreach ((_, _, CheckBox pPersistent) in PInspectorSectionsRead())
        {
            pPersistent.Margin = new Thickness(12, 0, 12, 12);
            pPersistent.Visibility = Visibility.Collapsed;
            pPersistentPanel.Children.Add(pPersistent);
        }

        return pPersistentPanel;
    }
}
