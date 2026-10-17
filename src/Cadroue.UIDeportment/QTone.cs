using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.Conduct;

namespace Cadroue.UIDeportment;

// The tone tools' wiring: fills each section's field row from the gate's range and origin, hands every input to the
// gate and shows its state. Slots follow the gate: 0 brightness, 1 contrast, 2 saturation.
public sealed class QTone
{
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

    private readonly QInspector qToneInspector;
    private readonly CTone qToneGate;
    private readonly IReadOnlyList<CheckBox> qToneSwitches;
    private readonly IReadOnlyList<CheckBox> qTonePersistent;
    private readonly IReadOnlyList<StackPanel> qToneStacks;
    private readonly IReadOnlyList<StackPanel> qToneBodies;
    private readonly Grid[] qToneFields = new Grid[3];
    private readonly List<int> qToneSlots = [0, 1, 2];

    // Each section's parts in slot order: its switches, the stack the field row heads, and the body the tip lands on.
    public QTone(
        QInspector lInspector,
        IReadOnlyList<CheckBox> lSwitches,
        IReadOnlyList<CheckBox> lPersistent,
        IReadOnlyList<StackPanel> lStacks,
        IReadOnlyList<StackPanel> lBodies)
    {
        qToneInspector = lInspector;
        qToneGate = lInspector.QInspectorVideo.QInspectorTone;
        qToneSwitches = lSwitches;
        qTonePersistent = lPersistent;
        qToneStacks = lStacks;
        qToneBodies = lBodies;

        qToneSlots.ForEach(QToneFieldBuild);
        qToneSlots.ForEach(QToneActiveAttach);
        qToneSlots.ForEach(QTonePersistentAttach);
        QToneNoticeBuild(qToneStacks[1]);

        qToneGate.CToneChanged += QToneUpdate;
        QToneUpdate();
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
        QField.QFieldAttach(
            qToneFields[lSlot],
            null,
            null,
            () => qToneGate.CToneValues[lSlot],
            lNumber => qToneGate.CToneValueSet(lSlot, lNumber));
    }

    private void QToneActiveAttach(int lSlot) =>
        QToneSwitchAttach(qToneSwitches[lSlot], lActive => qToneGate.CToneActiveSet(lSlot, lActive));

    private void QTonePersistentAttach(int lSlot) =>
        QToneSwitchAttach(qTonePersistent[lSlot], lFlag => qToneGate.CTonePersistentSet(lSlot, lFlag));

    private static void QToneSwitchAttach(CheckBox lSwitch, Action<bool> lSet)
    {
        lSwitch.Checked += (_, _) => lSet(true);
        lSwitch.Unchecked += (_, _) => lSet(false);
    }

    private void QToneUpdate() => qToneSlots.ForEach(QToneSlotUpdate);

    private void QToneSlotUpdate(int lSlot)
    {
        QTool.QToolSwitchUpdate(qToneSwitches[lSlot], qToneGate.CToneActives[lSlot]);
        QTool.QToolSwitchUpdate(qTonePersistent[lSlot], qToneGate.CTonePersistents[lSlot]);
        QField.QFieldUpdate(qToneFields[lSlot], qToneGate.CToneValues[lSlot], QTonePattern);
        QInspectorTip lTip = qToneInspector.QInspectorTipResolve(
            qToneGate.CToneActives[lSlot],
            qToneGate.CToneCapable,
            true,
            qToneRequireKeys[lSlot],
            string.Empty,
            qToneApplyKeys[lSlot],
            qTonePersistKeys[lSlot]);
        QTool.QToolTipApply(
            qToneSwitches[lSlot], qTonePersistent[lSlot], qToneStacks[lSlot], qToneBodies[lSlot], lTip);
    }
}
