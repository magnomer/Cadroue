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
        StackPanel pBody = PNoisePanelBuild();
        PNoiseAttach();
        return pBody;
    }

    private StackPanel PNoisePanelBuild()
    {
        pNoiseApplyBox = PInspectorSwitchBuild(
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Common.Apply"),
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Noise.ApplyTooltip"));

        pNoisePersistent = PInspectorSwitchBuild(
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Common.Persistent"),
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Noise.PersistentTooltip"));

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
            Foreground = QPanel.QPanelTextRead(),
            VerticalContentAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 8, 0, 0)
        };
        QInteractionClick.QInteractionClickAttach(pNoiseTrack);
        QCheckbox.QCheckboxAttach(pNoiseTrack);

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

    private void PNoiseAttach()
    {
        PInspectorSwitchAttach(pNoiseApplyBox, QNoise.QNoiseActiveSet);
        PInspectorSwitchAttach(pNoisePersistent, QNoise.QNoisePersistentSet);
        PInspectorSwitchAttach(pNoiseTrack, QNoise.QNoiseTrackSet);
    }

    // Each row hands its own position in the list to the driver.
    private PInspectorRow PNoiseRowBuild(QInspectorRow lRow, int pRow) => PInspectorRowBuild(
        lRow,
        () => QNoise.QNoiseValueRead(pRow),
        () => QNoise.QNoiseDefaultRead(pRow),
        pNumber => QNoise.QNoiseValueSet(pRow, pNumber));

    private void PNoiseUpdate() =>
        QNoise.QNoiseUpdate(
            pNoiseApplyBox,
            pNoisePersistent,
            pNoiseTrack,
            pNoiseRows.Select(pRow => (pRow.PInspectorRowSlider, pRow.PInspectorRowValue)).ToList(),
            pNoiseType,
            pNoisePreset,
            pNoiseStack);
}
