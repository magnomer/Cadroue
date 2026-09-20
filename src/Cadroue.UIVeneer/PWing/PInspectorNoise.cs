using System.Windows;
using System.Windows.Controls;
using Cadroue.Application;
using Cadroue.UIDeportment;
using Cadroue.UIVeneer.PHouse;

namespace Cadroue.UIVeneer.PWing;

public sealed partial class PInspector
{
    private CheckBox pNoiseApplyBox = null!;
    private CheckBox pNoisePersistent = null!;
    private ComboBox pNoisePreset = null!;
    private ComboBox pNoiseType = null!;
    private CheckBox pNoiseTrack = null!;
    private StackPanel pNoiseStack = null!;
    private StackPanel pNoiseBody = null!;
    private List<PInspectorRow> pNoiseRows = null!;

    private StackPanel PNoiseBodyBuild()
    {
        pNoiseApplyBox = PInspectorSwitchBuild(
            LLocalization.LLocalizationTextRead("Inspector.Common.Apply"),
            LLocalization.LLocalizationTextRead("Inspector.Noise.ApplyTooltip"));
        PInspectorSwitchAttach(pNoiseApplyBox, LNoise.LNoiseActiveSet);

        pNoisePersistent = PInspectorSwitchBuild(
            LLocalization.LLocalizationTextRead("Inspector.Common.Persistent"),
            LLocalization.LLocalizationTextRead("Inspector.Noise.PersistentTooltip"));
        PInspectorSwitchAttach(pNoisePersistent, LNoise.LNoisePersistentSet);

        pNoisePreset = PInspectorChoiceBuild(
            LNoise.LNoiseChoiceRead().LInspectorChoiceNames, LNoise.LNoiseChoiceSelect);
        pNoiseRows = LNoise.LNoiseRows.Select(PNoiseRowBuild).ToList();

        pNoiseType = PInspectorChoiceBuild(LNoise.LNoiseTypeNames, LNoise.LNoiseTypeSelect);
        pNoiseType.Width = 120;

        pNoiseTrack = new CheckBox
        {
            Content = LLocalization.LLocalizationTextRead("Inspector.Noise.Track"),
            ToolTip = LLocalization.LLocalizationTextRead("Inspector.Noise.TrackTooltip"),
            FontSize = 12,
            FontFamily = pInspectorFontFamily,
            Foreground = PPanelTextBrush,
            VerticalContentAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 8, 0, 0)
        };
        PCheckbox.PCheckboxApply(pNoiseTrack);
        PInspectorSwitchAttach(pNoiseTrack, LNoise.LNoiseTrackSet);

        pNoiseStack = new StackPanel();
        pNoiseStack.Children.Add(
            PInspectorFieldBuild(LLocalization.LLocalizationTextRead("Inspector.Common.Preset"), pNoisePreset));
        pNoiseRows.ForEach(pRow => pNoiseStack.Children.Add(pRow.PInspectorRowGrid));
        pNoiseStack.Children.Add(
            PInspectorFieldBuild(LLocalization.LLocalizationTextRead("Inspector.Noise.Noise"), pNoiseType));
        pNoiseStack.Children.Add(pNoiseTrack);

        pNoiseBody = new StackPanel
        {
            Margin = new Thickness(12, 12, 12, 12),
            Visibility = Visibility.Collapsed
        };
        pNoiseBody.Children.Add(pNoiseApplyBox);
        pNoiseBody.Children.Add(PInspectorSeparatorBuild());
        pNoiseBody.Children.Add(pNoiseStack);
        return pNoiseBody;
    }

    private PInspectorRow PNoiseRowBuild(LInspectorRow lRow) => PInspectorRowBuild(
        lRow,
        () => LNoise.LNoiseValueRead(lRow.LInspectorRowIndex),
        () => LNoise.LNoiseDefaultRead(lRow.LInspectorRowIndex),
        pNumber => LNoise.LNoiseValueSet(lRow.LInspectorRowIndex, pNumber));

    private void PNoiseUpdate()
    {
        bool lActive = LNoise.LNoiseStep.LWorkStepActive;
        PInspectorSwitchUpdate(pNoiseApplyBox, lActive);
        PInspectorSwitchUpdate(pNoisePersistent, LNoise.LNoisePersistent);
        PInspectorSwitchUpdate(pNoiseTrack, LNoise.LNoiseStep.LWorkNoiseTrack);
        pNoiseRows.ForEach(pRow => PInspectorRowUpdate(pRow, LNoise.LNoiseValueRead(pRow.PInspectorRowIndex)));
        pNoiseType.SelectedIndex = LNoise.LNoiseTypeIndex;
        PInspectorChoiceApply(pNoisePreset, LNoise.LNoiseChoiceRead());
        PInspectorSectionUpdate(pNoiseStack, lActive);
    }
}
