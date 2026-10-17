using System.Windows;
using System.Windows.Controls;
using Cadroue.UIDeportment;
using Cadroue.UIVeneer.PHouse;

namespace Cadroue.UIVeneer.PWing;

public sealed partial class PInspector
{
    private CheckBox pLoudnessApplyBox = null!;
    private CheckBox pLoudnessPersistent = null!;
    private ComboBox pLoudnessPreset = null!;
    private ComboBox pDynamicPreset = null!;
    private UIElement pLoudnessPresetRow = null!;
    private UIElement pDynamicPresetRow = null!;
    private ComboBox pLoudnessMode = null!;
    private CheckBox pLoudnessTwoPass = null!;
    private StackPanel pLoudnessPanel = null!;
    private StackPanel pLoudnessStack = null!;
    private StackPanel pDynamicStack = null!;
    private StackPanel pLoudnessBody = null!;
    private List<PInspectorRow> pLoudnessRows = null!;
    private List<PInspectorRow> pDynamicRows = null!;

    private StackPanel PLoudnessBodyBuild()
    {
        StackPanel pBody = PLoudnessPanelBuild();
        PLoudnessAttach();
        return pBody;
    }

    private StackPanel PLoudnessPanelBuild()
    {
        pLoudnessApplyBox = PInspectorSwitchBuild(
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Common.Apply"),
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Normalize.ApplyTooltip"));

        pLoudnessPersistent = PInspectorSwitchBuild(
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Common.Persistent"),
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Normalize.PersistentTooltip"));

        pLoudnessPreset = PInspectorChoiceBuild(
            QLoudness.QLoudnessChoiceRead(false).QInspectorChoiceNames,
            pIndex => QLoudness.QLoudnessChoiceSelect(false, pIndex));
        pDynamicPreset = PInspectorChoiceBuild(
            QLoudness.QLoudnessChoiceRead(true).QInspectorChoiceNames,
            pIndex => QLoudness.QLoudnessChoiceSelect(true, pIndex));
        pLoudnessMode = PInspectorChoiceBuild(QLoudness.QLoudnessModeNames, QLoudness.QLoudnessModeSelect);

        pLoudnessRows = QLoudness.QLoudnessRowsRead(false).Select(lRow => PLoudnessRowBuild(false, lRow)).ToList();
        pDynamicRows = QLoudness.QLoudnessRowsRead(true).Select(lRow => PLoudnessRowBuild(true, lRow)).ToList();

        pLoudnessTwoPass = new CheckBox
        {
            Content = QLocalizationCatalog.QLocalizationTextRead("Inspector.Normalize.TwoPass"),
            ToolTip = QLocalizationCatalog.QLocalizationTextRead("Inspector.Normalize.TwoPassTooltip"),
            FontSize = 12,
            FontFamily = pInspectorFontFamily,
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
            FontFamily = pInspectorFontFamily,
            Foreground = pInspectorMutedBrush,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 4, 0, 0)
        };

        string pPresetLabel = QLocalizationCatalog.QLocalizationTextRead("Inspector.Common.Preset");
        pLoudnessPresetRow = PInspectorFieldBuild(pPresetLabel, pLoudnessPreset);
        pDynamicPresetRow = PInspectorFieldBuild(pPresetLabel, pDynamicPreset);
        pLoudnessPanel = new StackPanel();
        pLoudnessPanel.Children.Add(
            PInspectorFieldBuild(QLocalizationCatalog.QLocalizationTextRead("Inspector.Normalize.Mode"),
                pLoudnessMode));
        pLoudnessPanel.Children.Add(pLoudnessPresetRow);
        pLoudnessPanel.Children.Add(pDynamicPresetRow);
        pLoudnessPanel.Children.Add(pLoudnessStack);
        pLoudnessPanel.Children.Add(pDynamicStack);
        pLoudnessPanel.Children.Add(pNotice);

        pLoudnessBody = new StackPanel
        {
            Margin = new Thickness(12, 12, 12, 12),
            Visibility = Visibility.Collapsed
        };
        pLoudnessBody.Children.Add(pLoudnessApplyBox);
        pLoudnessBody.Children.Add(PInspectorSeparatorBuild());
        pLoudnessBody.Children.Add(pLoudnessPanel);
        return pLoudnessBody;
    }

    private void PLoudnessAttach()
    {
        PInspectorSwitchAttach(pLoudnessApplyBox, QLoudness.QLoudnessActiveSet);
        PInspectorSwitchAttach(pLoudnessPersistent, QLoudness.QLoudnessPersistentSet);
        PInspectorSwitchAttach(pLoudnessTwoPass, QLoudness.QLoudnessTwopassSet);
    }

    private PInspectorRow PLoudnessRowBuild(bool pDynamic, QInspectorRow lRow) => PInspectorRowBuild(
        lRow,
        () => QLoudness.QLoudnessValueRead(pDynamic, lRow.QInspectorRowIndex),
        () => QLoudness.QLoudnessDefaultRead(pDynamic, lRow.QInspectorRowIndex),
        pNumber => QLoudness.QLoudnessValueSet(pDynamic, lRow.QInspectorRowIndex, pNumber));

    private void PLoudnessUpdate() =>
        QLoudness.QLoudnessUpdate(
            pLoudnessApplyBox,
            pLoudnessPersistent,
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
