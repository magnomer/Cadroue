using System.Windows;
using System.Windows.Controls;
using Cadroue.Application;
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
            LLocalization.LLocalizationTextRead("Inspector.Common.Apply"),
            LLocalization.LLocalizationTextRead("Inspector.Normalize.ApplyTooltip"));
        PInspectorSwitchAttach(pLoudnessApplyBox, LLoudness.LLoudnessActiveSet);

        pLoudnessPersistent = PInspectorSwitchBuild(
            LLocalization.LLocalizationTextRead("Inspector.Common.Persistent"),
            LLocalization.LLocalizationTextRead("Inspector.Normalize.PersistentTooltip"));
        PInspectorSwitchAttach(pLoudnessPersistent, LLoudness.LLoudnessPersistentSet);

        pLoudnessPreset = PInspectorChoiceBuild(
            LLoudness.LLoudnessChoiceRead(false).LInspectorChoiceNames,
            pIndex => LLoudness.LLoudnessChoiceSelect(false, pIndex));
        pDynamicPreset = PInspectorChoiceBuild(
            LLoudness.LLoudnessChoiceRead(true).LInspectorChoiceNames,
            pIndex => LLoudness.LLoudnessChoiceSelect(true, pIndex));
        pLoudnessMode = PInspectorChoiceBuild(LLoudness.LLoudnessModeNames, LLoudness.LLoudnessModeSelect);

        pLoudnessRows = LLoudness.LLoudnessRowsRead(false).Select(lRow => PLoudnessRowBuild(false, lRow)).ToList();
        pDynamicRows = LLoudness.LLoudnessRowsRead(true).Select(lRow => PLoudnessRowBuild(true, lRow)).ToList();

        pLoudnessTwoPass = new CheckBox
        {
            Content = LLocalization.LLocalizationTextRead("Inspector.Normalize.TwoPass"),
            ToolTip = LLocalization.LLocalizationTextRead("Inspector.Normalize.TwoPassTooltip"),
            FontSize = 12,
            FontFamily = pInspectorFontFamily,
            Foreground = PPanelTextBrush,
            VerticalContentAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 8, 0, 0)
        };
        PCheckbox.PCheckboxApply(pLoudnessTwoPass);
        PInspectorSwitchAttach(pLoudnessTwoPass, LLoudness.LLoudnessTwopassSet);

        pLoudnessStack = new StackPanel();
        pLoudnessRows.ForEach(pRow => pLoudnessStack.Children.Add(pRow.PInspectorRowGrid));
        pLoudnessStack.Children.Add(pLoudnessTwoPass);

        pDynamicStack = new StackPanel { Visibility = Visibility.Collapsed };
        pDynamicRows.ForEach(pRow => pDynamicStack.Children.Add(pRow.PInspectorRowGrid));

        var pNotice = new TextBlock
        {
            Text = LLocalization.LLocalizationTextRead("Inspector.Normalize.Notice"),
            FontSize = 11,
            FontFamily = pInspectorFontFamily,
            Foreground = pInspectorMutedBrush,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 4, 0, 0)
        };

        string pPresetLabel = LLocalization.LLocalizationTextRead("Inspector.Common.Preset");
        pLoudnessPresetRow = PInspectorFieldBuild(pPresetLabel, pLoudnessPreset);
        pDynamicPresetRow = PInspectorFieldBuild(pPresetLabel, pDynamicPreset);
        pLoudnessPanel = new StackPanel();
        pLoudnessPanel.Children.Add(
            PInspectorFieldBuild(LLocalization.LLocalizationTextRead("Inspector.Normalize.Mode"), pLoudnessMode));
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

    private PInspectorRow PLoudnessRowBuild(bool pDynamic, LInspectorRow lRow) => PInspectorRowBuild(
        lRow,
        () => LLoudness.LLoudnessValueRead(pDynamic, lRow.LInspectorRowIndex),
        () => LLoudness.LLoudnessDefaultRead(pDynamic, lRow.LInspectorRowIndex),
        pNumber => LLoudness.LLoudnessValueSet(pDynamic, lRow.LInspectorRowIndex, pNumber));

    private void PLoudnessRowUpdate(bool pDynamic, PInspectorRow pRow) =>
        PInspectorRowUpdate(pRow, LLoudness.LLoudnessValueRead(pDynamic, pRow.PInspectorRowIndex));

    private void PLoudnessUpdate()
    {
        bool lActive = LLoudness.LLoudnessStep.LWorkStepActive;
        bool lDynamic = LLoudness.LLoudnessDynamic;
        PInspectorSwitchUpdate(pLoudnessApplyBox, lActive);
        PInspectorSwitchUpdate(pLoudnessPersistent, LLoudness.LLoudnessPersistent);
        PInspectorSwitchUpdate(pLoudnessTwoPass, LLoudness.LLoudnessStep.LWorkTwoPass);
        pLoudnessMode.SelectedIndex = LLoudness.LLoudnessModeIndex;
        pLoudnessStack.Visibility = PLook.PLookVisible[!lDynamic];
        pLoudnessPresetRow.Visibility = PLook.PLookVisible[!lDynamic];
        pDynamicStack.Visibility = PLook.PLookVisible[lDynamic];
        pDynamicPresetRow.Visibility = PLook.PLookVisible[lDynamic];
        pLoudnessRows.ForEach(pRow => PLoudnessRowUpdate(false, pRow));
        pDynamicRows.ForEach(pRow => PLoudnessRowUpdate(true, pRow));
        PInspectorChoiceApply(pLoudnessPreset, LLoudness.LLoudnessChoiceRead(false));
        PInspectorChoiceApply(pDynamicPreset, LLoudness.LLoudnessChoiceRead(true));
        PInspectorSectionUpdate(pLoudnessPanel, lActive);
    }
}
