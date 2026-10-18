using System.Windows;
using System.Windows.Controls;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer.PWing;

internal sealed class PInspectorNoise
{
    private readonly CheckBox pNoiseApplyBox;
    private readonly ComboBox pNoisePreset;
    private readonly ComboBox pNoiseType;
    private readonly CheckBox pNoiseTrack;
    private readonly StackPanel pNoiseStack;
    private readonly List<PInspectorRow> pNoiseRows;

    internal PInspectorNoise(QNoise lNoise)
    {
        QNoise = lNoise;
        pNoiseApplyBox = PInspectorSwitch.PInspectorSwitchBuild(
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Common.Apply"),
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Noise.ApplyTooltip"));

        PNoisePersistent = PInspectorSwitch.PInspectorSwitchBuild(
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Common.Persistent"),
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Noise.PersistentTooltip"));

        pNoisePreset = PInspectorChoice.PInspectorChoiceBuild(
            QNoise.QNoiseChoiceRead().QInspectorChoiceNames, QNoise.QNoiseChoiceSelect);
        pNoiseRows = QNoise.QNoiseRows.Select(PNoiseRowBuild).ToList();

        pNoiseType = PInspectorChoice.PInspectorChoiceBuild(QNoise.QNoiseTypeNames, QNoise.QNoiseTypeSelect);
        pNoiseType.Width = 120;

        pNoiseTrack = new CheckBox
        {
            Content = QLocalizationCatalog.QLocalizationTextRead("Inspector.Noise.Track"),
            ToolTip = QLocalizationCatalog.QLocalizationTextRead("Inspector.Noise.TrackTooltip"),
            FontSize = 12,
            FontFamily = PInspectorLook.PInspectorFontFamily,
            Foreground = QPanel.QPanelTextRead(),
            VerticalContentAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 8, 0, 0)
        };
        QInteractionClick.QInteractionClickAttach(pNoiseTrack);
        QCheckbox.QCheckboxAttach(pNoiseTrack);

        pNoiseStack = new StackPanel();
        pNoiseStack.Children.Add(PInspectorField.PInspectorFieldBuild(
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Common.Preset"), pNoisePreset));
        pNoiseRows.ForEach(pRow => pNoiseStack.Children.Add(pRow.PInspectorRowGrid));
        pNoiseStack.Children.Add(PInspectorField.PInspectorFieldBuild(
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Noise.Noise"), pNoiseType));
        pNoiseStack.Children.Add(pNoiseTrack);

        PNoiseBody = new StackPanel
        {
            Margin = new Thickness(12, 12, 12, 12),
            Visibility = Visibility.Collapsed
        };
        PNoiseBody.Children.Add(pNoiseApplyBox);
        PNoiseBody.Children.Add(PInspectorField.PInspectorSeparatorBuild());
        PNoiseBody.Children.Add(pNoiseStack);
        PNoiseAttach();
    }

    private QNoise QNoise { get; }

    internal CheckBox PNoisePersistent { get; }

    internal StackPanel PNoiseBody { get; }

    private void PNoiseAttach()
    {
        PInspectorSwitch.PInspectorSwitchAttach(pNoiseApplyBox, QNoise.QNoiseActiveSet);
        PInspectorSwitch.PInspectorSwitchAttach(PNoisePersistent, QNoise.QNoisePersistentSet);
        PInspectorSwitch.PInspectorSwitchAttach(pNoiseTrack, QNoise.QNoiseTrackSet);
    }

    internal void PNoiseChangeAttach() => QNoise.QNoiseChanged += PNoiseUpdate;

    // Each row hands its own position in the list to the driver.
    private PInspectorRow PNoiseRowBuild(QInspectorRow lRow, int pRow) => PInspectorRow.PInspectorRowBuild(
        lRow,
        () => QNoise.QNoiseValueRead(pRow),
        () => QNoise.QNoiseDefaultRead(pRow),
        pNumber => QNoise.QNoiseValueSet(pRow, pNumber));

    internal void PNoiseUpdate() =>
        QNoise.QNoiseUpdate(
            pNoiseApplyBox,
            PNoisePersistent,
            pNoiseTrack,
            pNoiseRows.Select(pRow => (pRow.PInspectorRowSlider, pRow.PInspectorRowValue)).ToList(),
            pNoiseType,
            pNoisePreset,
            pNoiseStack);
}
