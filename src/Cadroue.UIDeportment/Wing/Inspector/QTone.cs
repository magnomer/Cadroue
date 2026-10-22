using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.Conduct;

namespace Cadroue.UIDeportment;

// The tone tools' wiring: fills each section's field row from the gate's range and origin, hands every input to the
// gate and shows its state. Slots follow the gate: 0 brightness, 1 contrast, 2 saturation.
public sealed class QTone
{
    private const string QToneSheet = "PTone";

    private const string QTonePersistentSheet = "PTonePersistent";

    private const string QToneActivePart = "PToneActive";

    private const string QToneStackPart = "PToneStack";

    private const string QTonePattern = "0.#";

    private static readonly string[] qToneUnits = { string.Empty, "%", "%" };

    private static readonly string[] qToneRequireKeys =
    {
        "Inspector.Video.BrightnessRequiresEq",
        "Inspector.Video.ContrastRequiresEq",
        "Inspector.Video.SaturationRequiresEq"
    };

    private static readonly string[] qToneApplyKeys =
    {
        "Inspector.Video.ApplyBrightness",
        "Inspector.Video.ApplyContrast",
        "Inspector.Video.ApplySaturation"
    };

    private static readonly string[] qTonePersistKeys =
    {
        "Inspector.Video.PersistBrightness",
        "Inspector.Video.PersistContrast",
        "Inspector.Video.PersistSaturation"
    };

    private readonly CTone qToneGate;
    private readonly IReadOnlyList<CheckBox> qToneSwitches;
    private readonly IReadOnlyList<CheckBox> qTonePersistent;
    private readonly IReadOnlyList<StackPanel> qToneStacks;
    private readonly IReadOnlyList<StackPanel> qToneBodies;
    private readonly Grid[] qToneFields = new Grid[3];
    private readonly List<int> qToneSlots = [0, 1, 2];

    public QTone(QInspector lInspector)
    {
        qToneGate = lInspector.QInspectorVideo.QInspectorTone;
        qToneBodies =
        [
            QContract.QContractSheetFind<StackPanel>(QToneSheet),
            QContract.QContractSheetFind<StackPanel>(QToneSheet),
            QContract.QContractSheetFind<StackPanel>(QToneSheet)
        ];
        qTonePersistent =
        [
            QContract.QContractSheetFind<CheckBox>(QTonePersistentSheet),
            QContract.QContractSheetFind<CheckBox>(QTonePersistentSheet),
            QContract.QContractSheetFind<CheckBox>(QTonePersistentSheet)
        ];
        qToneSwitches = qToneBodies
            .Select(lBody => QContract.QContractFind<CheckBox>(lBody, QToneActivePart))
            .ToList();
        qToneStacks = qToneBodies
            .Select(lBody => QContract.QContractFind<StackPanel>(lBody, QToneStackPart))
            .ToList();

        qToneSlots.ForEach(QToneTextShow);
        qToneSlots.ForEach(QToneFieldBuild);
        var lHandlers = new List<(Action, Action)>
        {
            QToneBrightnessAttach(),
            QToneContrastAttach(),
            QToneSaturationAttach(),
        };
        lHandlers.AddRange(qToneSlots.Select(QToneActiveAttach));
        lHandlers.AddRange(qToneSlots.Select(QTonePersistentAttach));
        qToneBodies[0].Resources[typeof(QTone)] = lHandlers;
        QToneNoticeBuild(qToneStacks[1]);

        QToneAttach();
        QToneUpdate();
    }

    public StackPanel QToneBrightnessBody => qToneBodies[0];

    public CheckBox QToneBrightnessPersistent => qTonePersistent[0];

    public StackPanel QToneContrastBody => qToneBodies[1];

    public CheckBox QToneContrastPersistent => qTonePersistent[1];

    public StackPanel QToneSaturationBody => qToneBodies[2];

    public CheckBox QToneSaturationPersistent => qTonePersistent[2];

    private void QToneTextShow(int lSlot)
    {
        qToneSwitches[lSlot].Content = QLocalizationCatalog.QLocalizationTextRead("Inspector.Common.Apply");
        qToneSwitches[lSlot].ToolTip = QLocalizationCatalog.QLocalizationTextRead(qToneApplyKeys[lSlot]);
        qTonePersistent[lSlot].Content = QLocalizationCatalog.QLocalizationTextRead("Inspector.Common.Persistent");
        qTonePersistent[lSlot].ToolTip = QLocalizationCatalog.QLocalizationTextRead(qTonePersistKeys[lSlot]);
    }

    // The contrast preview notice under its field row, shown as the gate answers once at build.
    private void QToneNoticeBuild(StackPanel lStack) =>
        lStack.Children.Add(new TextBlock
        {
            Text = QLocalizationCatalog.QLocalizationTextRead("Inspector.Video.ContrastPreview"),
            Foreground = new SolidColorBrush(Color.FromRgb(0x64, 0x70, 0x82)),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 2, 0, 0),
            Visibility = qToneGate.CToneNoticeShown ? Visibility.Visible : Visibility.Collapsed
        });

    private void QToneFieldBuild(int lSlot)
    {
        qToneFields[lSlot] = QField.QFieldBuild(
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Common.Amount"),
            qToneUnits[lSlot],
            qToneGate.CToneLeasts[lSlot],
            qToneGate.CToneMosts[lSlot],
            qToneGate.CToneOrigins[lSlot]);
        qToneStacks[lSlot].Children.Insert(0, qToneFields[lSlot]);
    }

    private (Action, Action) QToneBrightnessAttach() =>
        QField.QFieldAttach(
            qToneFields[0], null, null, () => qToneGate.CToneValues[0], qToneGate.CToneBrightnessSet);

    private (Action, Action) QToneContrastAttach() =>
        QField.QFieldAttach(
            qToneFields[1], null, null, () => qToneGate.CToneValues[1], qToneGate.CToneContrastSet);

    private (Action, Action) QToneSaturationAttach() =>
        QField.QFieldAttach(
            qToneFields[2], null, null, () => qToneGate.CToneValues[2], qToneGate.CToneSaturationSet);

    private (Action, Action) QToneActiveAttach(int lSlot) =>
        QToneSwitchAttach(qToneSwitches[lSlot], lActive => qToneGate.CToneActiveSet(lSlot, lActive));

    private (Action, Action) QTonePersistentAttach(int lSlot) =>
        QToneSwitchAttach(qTonePersistent[lSlot], lFlag => qToneGate.CTonePersistentSet(lSlot, lFlag));

    private static (Action, Action) QToneSwitchAttach(CheckBox lSwitch, Action<bool> lSet)
    {
        RoutedEventHandler lChecked = (_, _) => lSet(true);
        RoutedEventHandler lUnchecked = (_, _) => lSet(false);
        Action lAttach = () =>
        {
            lSwitch.Checked -= lChecked;
            lSwitch.Unchecked -= lUnchecked;
            lSwitch.Checked += lChecked;
            lSwitch.Unchecked += lUnchecked;
        };
        Action lDetach = () =>
        {
            lSwitch.Checked -= lChecked;
            lSwitch.Unchecked -= lUnchecked;
        };
        lAttach();
        return (lAttach, lDetach);
    }

    public void QToneAttach()
    {
        foreach ((Action lAttach, _) in (List<(Action, Action)>)qToneBodies[0].Resources[typeof(QTone)])
        {
            lAttach();
        }

        qToneGate.CToneChanged -= QToneUpdate;
        qToneGate.CToneChanged += QToneUpdate;
    }

    public void QToneDetach()
    {
        qToneGate.CToneChanged -= QToneUpdate;
        foreach ((_, Action lDetach) in (List<(Action, Action)>)qToneBodies[0].Resources[typeof(QTone)])
        {
            lDetach();
        }
    }

    private void QToneUpdate() => qToneSlots.ForEach(QToneSlotUpdate);

    private void QToneSlotUpdate(int lSlot)
    {
        QTool.QToolSwitchUpdate(qToneSwitches[lSlot], qToneGate.CToneActives[lSlot]);
        QTool.QToolSwitchUpdate(qTonePersistent[lSlot], qToneGate.CTonePersistents[lSlot]);
        QField.QFieldUpdate(qToneFields[lSlot], qToneGate.CToneValues[lSlot], QTonePattern);
        QInspectorTip lTip = QInspectorTip.QInspectorTipResolve(
            CInspectorTip.CInspectorTipResolve(
                qToneGate.CToneActives[lSlot],
                qToneGate.CToneCapable,
                true,
                qToneRequireKeys[lSlot],
                string.Empty),
            qToneApplyKeys[lSlot],
            qTonePersistKeys[lSlot]);
        QTool.QToolTipApply(
            qToneSwitches[lSlot], qTonePersistent[lSlot], qToneStacks[lSlot], qToneBodies[lSlot], lTip);
    }
}
