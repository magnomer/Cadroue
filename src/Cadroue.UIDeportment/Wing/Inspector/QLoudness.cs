using System.Windows;
using System.Windows.Controls;
using Cadroue.Conduct;

namespace Cadroue.UIDeportment;

// The leveling tool's wiring: paints the gate's mode, values and preset rows onto the section's parts, and hands every
// input to the gate. Each list runs in mode order, loudness first; rows follow the gate's rows of the mode: target,
// peak, range, or frame, smoothness, maximum gain, compression.
public sealed class QLoudness
{
    private const string QLoudnessPattern = "0.###";

    private static readonly string[][] qLoudnessLabelKeys =
    {
        new[] { "Inspector.Normalize.Target", "Inspector.Normalize.Peak", "Inspector.Normalize.Range" },
        new[]
        {
            "Inspector.Dynamic.Frame",
            "Inspector.Dynamic.Smoothness",
            "Inspector.Dynamic.MaxGain",
            "Inspector.Dynamic.Compress"
        }
    };

    private static readonly string[][] qLoudnessUnits =
    {
        new[] { "LUFS", "dBTP", "LU" },
        new[] { "ms", "g", "×", "s" }
    };

    private readonly CLoudness qLoudnessGate;

    public QLoudness(CLoudness lGate)
    {
        qLoudnessGate = lGate;
    }

    public event Action? QLoudnessChanged
    {
        add => qLoudnessGate.CLoudnessChanged += value;
        remove => qLoudnessGate.CLoudnessChanged -= value;
    }

    public IReadOnlyList<string> QLoudnessModeNames => qLoudnessGate.CLoudnessModes
        .Select(static lRow => QLocalizationCatalog.QLocalizationTextRead(lRow.CChoiceKey))
        .ToList();

    public IReadOnlyList<QInspectorRow> QLoudnessRowsRead(bool lDynamic)
    {
        int lMode = lDynamic ? 1 : 0;
        IReadOnlyList<double> lLeasts = qLoudnessGate.CLoudnessLeasts[lMode];
        IReadOnlyList<double> lMosts = qLoudnessGate.CLoudnessMosts[lMode];
        return qLoudnessLabelKeys[lMode]
            .Select((lKey, lIndex) => QInspectorPlan.QInspectorRowCreate(
                lIndex, lKey, qLoudnessUnits[lMode][lIndex], QLoudnessPattern, lLeasts[lIndex], lMosts[lIndex]))
            .ToList();
    }

    // The combo shows each row's wording and selects the row the gate marked chosen.
    public QInspectorChoice QLoudnessChoiceRead(bool lDynamic) =>
        QInspectorPlan.QInspectorChoiceBuild(
            CChoicePlan.CChoicePlanBuild(qLoudnessGate.CLoudnessChoicesRead(lDynamic)));

    public double QLoudnessDefaultRead(bool lDynamic, int lIndex) =>
        qLoudnessGate.CLoudnessDefaultRead(lDynamic, lIndex);

    public void QLoudnessValueSet(bool lDynamic, int lIndex, double lValue) =>
        qLoudnessGate.CLoudnessValueSet(lDynamic, lIndex, lValue);

    public void QLoudnessActiveSet(bool lActive) => qLoudnessGate.CLoudnessActiveSet(lActive);

    public void QLoudnessPersistentSet(bool lPersistent) => qLoudnessGate.CLoudnessPersistentSet(lPersistent);

    public void QLoudnessTwopassSet(bool lTwoPass) => qLoudnessGate.CLoudnessTwopassSet(lTwoPass);

    public void QLoudnessModeSelect(int lIndex) => qLoudnessGate.CLoudnessModeSelect(lIndex);

    public void QLoudnessChoiceSelect(bool lDynamic, int lIndex) =>
        qLoudnessGate.CLoudnessChoiceSelect(lDynamic, lIndex);

    // The section's parts take the gate's state: the switches, the mode, which mode's stack and preset row show, each
    // row's slider and number, each preset combo, then the panel is lit while the tool applies. Every list runs in
    // mode order; a combo's custom row is the gate's last row and the combo's last item.
    public void QLoudnessUpdate(
        CheckBox lApply,
        CheckBox lPersistent,
        CheckBox lTwoPass,
        ComboBox lMode,
        IReadOnlyList<UIElement> lStacks,
        IReadOnlyList<UIElement> lPresetRows,
        IReadOnlyList<IReadOnlyList<(Slider QLoudnessSlider, TextBox QLoudnessValue)>> lRows,
        IReadOnlyList<ComboBox> lPresets,
        FrameworkElement lPanel)
    {
        QTool.QToolSwitchUpdate(lApply, qLoudnessGate.CLoudnessActive);
        QTool.QToolSwitchUpdate(lPersistent, qLoudnessGate.CLoudnessPersistent);
        QTool.QToolSwitchUpdate(lTwoPass, qLoudnessGate.CLoudnessTwoPass);
        IReadOnlyList<CChoice> lModes = qLoudnessGate.CLoudnessModes;
        lMode.SelectedIndex = lModes.ToList().FindIndex(lRow => lRow.CChoiceChosen);
        for (int lShown = 0; lShown < lStacks.Count; lShown++)
        {
            Visibility lLook = lModes[lShown].CChoiceChosen ? Visibility.Visible : Visibility.Collapsed;
            lStacks[lShown].Visibility = lLook;
            lPresetRows[lShown].Visibility = lLook;
        }

        IReadOnlyList<IReadOnlyList<double>> lValues = qLoudnessGate.CLoudnessRows;
        for (int lList = 0; lList < lRows.Count; lList++)
        {
            for (int lRow = 0; lRow < lRows[lList].Count; lRow++)
            {
                (Slider lSlider, TextBox lValue) = lRows[lList][lRow];
                QInspectorField.QInspectorFieldUpdate(lSlider, lValue, lValues[lList][lRow], QLoudnessPattern);
            }
        }

        for (int lList = 0; lList < lPresets.Count; lList++)
        {
            QInspectorChoice lChoice = QLoudnessChoiceRead(lList == 1);
            lPresets[lList].Items[lChoice.QInspectorChoiceCustom] = lChoice.QInspectorChoiceText;
            lPresets[lList].SelectedIndex = lChoice.QInspectorChoiceIndex;
        }

        QTool.QToolStackUpdate(lPanel, qLoudnessGate.CLoudnessActive);
    }
}
