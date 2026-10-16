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
        pLoudnessApplyBox = PInspectorSwitchBuild(
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Common.Apply"),
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Normalize.ApplyTooltip"));
        PInspectorSwitchAttach(pLoudnessApplyBox, QLoudness.QLoudnessActiveSet);

        pLoudnessPersistent = PInspectorSwitchBuild(
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Common.Persistent"),
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Normalize.PersistentTooltip"));
        PInspectorSwitchAttach(pLoudnessPersistent, QLoudness.QLoudnessPersistentSet);

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
            Foreground = PPanelTextBrush,
            VerticalContentAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 8, 0, 0)
        };
        QInteractionClick.QInteractionClickAttach(pLoudnessTwoPass);
        QCheckbox.QCheckboxAttach(pLoudnessTwoPass);
        PInspectorSwitchAttach(pLoudnessTwoPass, QLoudness.QLoudnessTwopassSet);

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

    private PInspectorRow PLoudnessRowBuild(bool pDynamic, QInspectorRow lRow) => PInspectorRowBuild(
        lRow,
        () => QLoudness.QLoudnessValueRead(pDynamic, lRow.QInspectorRowIndex),
        () => QLoudness.QLoudnessDefaultRead(pDynamic, lRow.QInspectorRowIndex),
        pNumber => QLoudness.QLoudnessValueSet(pDynamic, lRow.QInspectorRowIndex, pNumber));

    private void PLoudnessRowUpdate(bool pDynamic, PInspectorRow pRow) =>
        PInspectorRowUpdate(pRow, QLoudness.QLoudnessValueRead(pDynamic, pRow.PInspectorRowIndex));

    private void PLoudnessUpdate()
    {
        bool lActive = QLoudness.QLoudnessStep.LWorkStepActive;
        bool lDynamic = QLoudness.QLoudnessDynamic;
        PInspectorSwitchUpdate(pLoudnessApplyBox, lActive);
        PInspectorSwitchUpdate(pLoudnessPersistent, QLoudness.QLoudnessPersistent);
        PInspectorSwitchUpdate(pLoudnessTwoPass, QLoudness.QLoudnessStep.LWorkTwoPass);
        pLoudnessMode.SelectedIndex = QLoudness.QLoudnessModeIndex;
        pLoudnessStack.Visibility = PLook.PLookVisible[!lDynamic];
        pLoudnessPresetRow.Visibility = PLook.PLookVisible[!lDynamic];
        pDynamicStack.Visibility = PLook.PLookVisible[lDynamic];
        pDynamicPresetRow.Visibility = PLook.PLookVisible[lDynamic];
        pLoudnessRows.ForEach(pRow => PLoudnessRowUpdate(false, pRow));
        pDynamicRows.ForEach(pRow => PLoudnessRowUpdate(true, pRow));
        PInspectorChoiceApply(pLoudnessPreset, QLoudness.QLoudnessChoiceRead(false));
        PInspectorChoiceApply(pDynamicPreset, QLoudness.QLoudnessChoiceRead(true));
        PInspectorSectionUpdate(pLoudnessPanel, lActive);
    }
}
