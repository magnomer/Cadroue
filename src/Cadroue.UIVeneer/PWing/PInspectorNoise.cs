using System.Windows;
using System.Windows.Controls;
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
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Common.Apply"),
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Noise.ApplyTooltip"));
        PInspectorSwitchAttach(pNoiseApplyBox, QNoise.QNoiseActiveSet);

        pNoisePersistent = PInspectorSwitchBuild(
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Common.Persistent"),
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Noise.PersistentTooltip"));
        PInspectorSwitchAttach(pNoisePersistent, QNoise.QNoisePersistentSet);

        pNoisePreset = PInspectorChoiceBuild(
            QNoise.QNoiseChoiceRead().QInspectorChoiceNames, QNoise.QNoiseChoiceSelect);
        pNoiseRows = QNoise.QNoiseRows.Select(PNoiseRowBuild).ToList();

        pNoiseType = PInspectorChoiceBuild(QNoise.QNoiseTypeNames, QNoise.QNoiseTypeSelect);
        pNoiseType.Width = 120;

        pNoiseTrack = new CheckBox
        {
            Content = QLocalizationCatalog.QLocalizationTextRead("Inspector.Noise.Track"),
            ToolTip = QLocalizationCatalog.QLocalizationTextRead("Inspector.Noise.TrackTooltip"),
            FontSize = 12,
            FontFamily = pInspectorFontFamily,
            Foreground = PPanelTextBrush,
            VerticalContentAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 8, 0, 0)
        };
        QInteractionClick.QInteractionClickAttach(pNoiseTrack);
        QCheckbox.QCheckboxAttach(pNoiseTrack);
        PInspectorSwitchAttach(pNoiseTrack, QNoise.QNoiseTrackSet);

        pNoiseStack = new StackPanel();
        pNoiseStack.Children.Add(
            PInspectorFieldBuild(QLocalizationCatalog.QLocalizationTextRead("Inspector.Common.Preset"), pNoisePreset));
        pNoiseRows.ForEach(pRow => pNoiseStack.Children.Add(pRow.PInspectorRowGrid));
        pNoiseStack.Children.Add(
            PInspectorFieldBuild(QLocalizationCatalog.QLocalizationTextRead("Inspector.Noise.Noise"), pNoiseType));
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

    private PInspectorRow PNoiseRowBuild(QInspectorRow lRow) => PInspectorRowBuild(
        lRow,
        () => QNoise.QNoiseValueRead(lRow.QInspectorRowIndex),
        () => QNoise.QNoiseDefaultRead(lRow.QInspectorRowIndex),
        pNumber => QNoise.QNoiseValueSet(lRow.QInspectorRowIndex, pNumber));

    private void PNoiseUpdate()
    {
        bool lActive = QNoise.QNoiseStep.LWorkStepActive;
        PInspectorSwitchUpdate(pNoiseApplyBox, lActive);
        PInspectorSwitchUpdate(pNoisePersistent, QNoise.QNoisePersistent);
        PInspectorSwitchUpdate(pNoiseTrack, QNoise.QNoiseStep.LWorkNoiseTrack);
        pNoiseRows.ForEach(pRow => PInspectorRowUpdate(pRow, QNoise.QNoiseValueRead(pRow.PInspectorRowIndex)));
        pNoiseType.SelectedIndex = QNoise.QNoiseTypeIndex;
        PInspectorChoiceApply(pNoisePreset, QNoise.QNoiseChoiceRead());
        PInspectorSectionUpdate(pNoiseStack, lActive);
    }
}
