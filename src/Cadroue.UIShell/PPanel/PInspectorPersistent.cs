using System.Windows;
using System.Windows.Controls;
using Cadroue.UIShell.PHouse;

namespace Cadroue.UIShell.PPanel;

public sealed partial class PInspector
{
    public void PCropPersistentApply(bool pCropPersistent)
    {
        pInspectorPersistentBox.IsChecked = pCropPersistent;
    }

    private UIElement PInspectorPersistentBuild()
    {
        pInspectorPersistentBox = PInspectorSwitchBuild(
            LLocalization.LLocalizationTextRead("Inspector.Common.Persistent"),
            LLocalization.LLocalizationTextRead("Inspector.Crop.PersistentTooltip"));
        pInspectorPersistentBox.Checked += (_, _) => PInspectorPersistentRaise();
        pInspectorPersistentBox.Unchecked += (_, _) => PInspectorPersistentRaise();

        var pPersistentPanel = new StackPanel { Visibility = Visibility.Collapsed };
        pPersistentPanel.Children.Add(new Border
        {
            Height = 1,
            Background = PPanelLineBrush,
            Margin = new Thickness(12, 0, 12, 12)
        });
        pPersistentPanel.Children.Add(PInspectorPersistentPrepare(pInspectorPersistentBox));
        pPersistentPanel.Children.Add(PInspectorPersistentPrepare(pInspectorBrightnessPersistent));
        pPersistentPanel.Children.Add(PInspectorPersistentPrepare(pInspectorContrastPersistent));
        pPersistentPanel.Children.Add(PInspectorPersistentPrepare(pInspectorSaturationPersistent));
        pPersistentPanel.Children.Add(PInspectorPersistentPrepare(pGammaPersistent));
        pPersistentPanel.Children.Add(PInspectorPersistentPrepare(pWhitebalancePersistent));
        pPersistentPanel.Children.Add(PInspectorPersistentPrepare(pExposurePersistent));
        pPersistentPanel.Children.Add(PInspectorPersistentPrepare(pCurvePersistent));
        pPersistentPanel.Children.Add(PInspectorPersistentPrepare(pInspectorHighPass.PInspectorPassPersistent));
        pPersistentPanel.Children.Add(PInspectorPersistentPrepare(pInspectorLowPass.PInspectorPassPersistent));
        pPersistentPanel.Children.Add(PInspectorPersistentPrepare(pNoisePersistent));
        pPersistentPanel.Children.Add(PInspectorPersistentPrepare(pInspectorVolumePersistent));
        pPersistentPanel.Children.Add(PInspectorPersistentPrepare(pLoudnessPersistent));
        pPersistentPanel.Children.Add(PInspectorPersistentPrepare(pEqualizerPersistent));
        pPersistentPanel.Children.Add(PInspectorPersistentPrepare(pSkipPersistentBox));
        return pPersistentPanel;
    }

    private CheckBox PInspectorPersistentPrepare(CheckBox pPersistentBox)
    {
        pPersistentBox.Margin = new Thickness(12, 0, 12, 12);
        pPersistentBox.Visibility = Visibility.Collapsed;
        pPersistentBox.Checked += (_, _) => PInspectorPlanChange?.Invoke();
        pPersistentBox.Unchecked += (_, _) => PInspectorPlanChange?.Invoke();
        return pPersistentBox;
    }
}
