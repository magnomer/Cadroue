using System.Windows;
using System.Windows.Controls;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

internal sealed class PInspectorLoudness
{
    private readonly CheckBox pLoudnessApplyBox;
    private readonly ComboBox pLoudnessPreset;
    private readonly ComboBox pDynamicPreset;
    private readonly UIElement pLoudnessPresetRow;
    private readonly UIElement pDynamicPresetRow;
    private readonly ComboBox pLoudnessMode;
    private readonly CheckBox pLoudnessTwoPass;
    private readonly StackPanel pLoudnessPanel;
    private readonly StackPanel pLoudnessStack;
    private readonly StackPanel pDynamicStack;
    private readonly List<PInspectorRow> pLoudnessRows;
    private readonly List<PInspectorRow> pDynamicRows;

    internal PInspectorLoudness(QLoudness lLoudness)
    {
        QLoudness = lLoudness;
        pLoudnessApplyBox = PInspectorSwitch.PInspectorSwitchBuild(
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Common.Apply"),
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Normalize.ApplyTooltip"));

        PLoudnessPersistent = PInspectorSwitch.PInspectorSwitchBuild(
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Common.Persistent"),
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Normalize.PersistentTooltip"));

        pLoudnessPreset = PInspectorChoice.PInspectorChoiceBuild(
            QLoudness.QLoudnessChoiceRead(false).QInspectorChoiceNames,
            pIndex => QLoudness.QLoudnessChoiceSelect(false, pIndex));
        pDynamicPreset = PInspectorChoice.PInspectorChoiceBuild(
            QLoudness.QLoudnessChoiceRead(true).QInspectorChoiceNames,
            pIndex => QLoudness.QLoudnessChoiceSelect(true, pIndex));
        pLoudnessMode = PInspectorChoice.PInspectorChoiceBuild(
            QLoudness.QLoudnessModeNames, QLoudness.QLoudnessModeSelect);

        pLoudnessRows = QLoudness.QLoudnessRowsRead(false).Select(lRow => PLoudnessRowBuild(false, lRow)).ToList();
        pDynamicRows = QLoudness.QLoudnessRowsRead(true).Select(lRow => PLoudnessRowBuild(true, lRow)).ToList();

        pLoudnessTwoPass = new CheckBox
        {
            Content = QLocalizationCatalog.QLocalizationTextRead("Inspector.Normalize.TwoPass"),
            ToolTip = QLocalizationCatalog.QLocalizationTextRead("Inspector.Normalize.TwoPassTooltip"),
            FontSize = 12,
            FontFamily = PInspectorLook.PInspectorFontFamily,
            Foreground = QPanel.QPanelTextRead(),
            VerticalContentAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 8, 0, 0)
        };
        QInteractionClick.QInteractionClickAttach(pLoudnessTwoPass);
        QCheckbox.QCheckboxAttach(pLoudnessTwoPass);

        pLoudnessStack = new StackPanel();
        pLoudnessRows.ForEach(pRow => pLoudnessStack.Children.Add(pRow.PInspectorRowGrid));
        pLoudnessStack.Children.Add(pLoudnessTwoPass);

        pDynamicStack = new StackPanel { Visibility = Visibility.Collapsed };
        pDynamicRows.ForEach(pRow => pDynamicStack.Children.Add(pRow.PInspectorRowGrid));

        var pNotice = new TextBlock
        {
            Text = QLocalizationCatalog.QLocalizationTextRead("Inspector.Normalize.Notice"),
            FontSize = 11,
            FontFamily = PInspectorLook.PInspectorFontFamily,
            Foreground = PInspectorLook.PInspectorMutedBrush,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 4, 0, 0)
        };

        string pPresetLabel = QLocalizationCatalog.QLocalizationTextRead("Inspector.Common.Preset");
        pLoudnessPresetRow = PInspectorField.PInspectorFieldBuild(pPresetLabel, pLoudnessPreset);
        pDynamicPresetRow = PInspectorField.PInspectorFieldBuild(pPresetLabel, pDynamicPreset);
        pLoudnessPanel = new StackPanel();
        pLoudnessPanel.Children.Add(
            PInspectorField.PInspectorFieldBuild(QLocalizationCatalog.QLocalizationTextRead("Inspector.Normalize.Mode"),
                pLoudnessMode));
        pLoudnessPanel.Children.Add(pLoudnessPresetRow);
        pLoudnessPanel.Children.Add(pDynamicPresetRow);
        pLoudnessPanel.Children.Add(pLoudnessStack);
        pLoudnessPanel.Children.Add(pDynamicStack);
        pLoudnessPanel.Children.Add(pNotice);

        PLoudnessBody = new StackPanel
        {
            Margin = new Thickness(12, 12, 12, 12),
            Visibility = Visibility.Collapsed
        };
        PLoudnessBody.Children.Add(pLoudnessApplyBox);
        PLoudnessBody.Children.Add(PInspectorField.PInspectorSeparatorBuild());
        PLoudnessBody.Children.Add(pLoudnessPanel);
        PLoudnessAttach();
    }

    private QLoudness QLoudness { get; }

    internal CheckBox PLoudnessPersistent { get; }

    internal StackPanel PLoudnessBody { get; }

    private void PLoudnessAttach()
    {
        PInspectorSwitch.PInspectorSwitchAttach(pLoudnessApplyBox, QLoudness.QLoudnessActiveSet);
        PInspectorSwitch.PInspectorSwitchAttach(PLoudnessPersistent, QLoudness.QLoudnessPersistentSet);
        PInspectorSwitch.PInspectorSwitchAttach(pLoudnessTwoPass, QLoudness.QLoudnessTwopassSet);
    }

    internal void PLoudnessChangeAttach() => QLoudness.QLoudnessChanged += PLoudnessUpdate;

    private PInspectorRow PLoudnessRowBuild(bool pDynamic, QInspectorRow lRow) => PInspectorRow.PInspectorRowBuild(
        lRow,
        () => QLoudness.QLoudnessValueRead(pDynamic, lRow.QInspectorRowIndex),
        () => QLoudness.QLoudnessDefaultRead(pDynamic, lRow.QInspectorRowIndex),
        pNumber => QLoudness.QLoudnessValueSet(pDynamic, lRow.QInspectorRowIndex, pNumber));

    internal void PLoudnessUpdate() =>
        QLoudness.QLoudnessUpdate(
            pLoudnessApplyBox,
            PLoudnessPersistent,
            pLoudnessTwoPass,
            pLoudnessMode,
            [pLoudnessStack, pDynamicStack],
            [pLoudnessPresetRow, pDynamicPresetRow],
            [PLoudnessRowsRead(pLoudnessRows), PLoudnessRowsRead(pDynamicRows)],
            [pLoudnessPreset, pDynamicPreset],
            pLoudnessPanel);

    private static IReadOnlyList<(Slider, TextBox)> PLoudnessRowsRead(List<PInspectorRow> pRows) =>
        pRows.Select(pRow => (pRow.PInspectorRowSlider, pRow.PInspectorRowValue)).ToList();
}
