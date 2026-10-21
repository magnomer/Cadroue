using System.Windows.Controls;
using Cadroue.Conduct;

namespace Cadroue.UIDeportment;

// The gamma tool's wiring: fills fresh PGamma copies, hands every input to the gate and shows the gate's state.
public sealed class QGamma
{
    private const string QGammaSheet = "PGamma";

    private const string QGammaPersistentSheet = "PGammaPersistent";

    private const string QGammaActivePart = "PGammaActive";

    private const string QGammaStackPart = "PGammaStack";

    private const string QGammaDefaultPart = "PGammaDefault";

    private const double QGammaMost = 100;

    private const double QGammaOrigin = 0;

    private const string QGammaPattern = "0.#";

    private static readonly string[] qGammaLabelKeys =
    {
        "Inspector.Video.Midtone",
        "Inspector.Video.RedGamma",
        "Inspector.Video.GreenGamma",
        "Inspector.Video.BlueGamma",
        "Inspector.Video.HighlightProtection"
    };

    private static readonly string[] qGammaUnits = { "", "", "", "", "%" };

    private readonly CGamma qGammaGate;
    private readonly CheckBox qGammaActive;
    private readonly StackPanel qGammaStack;
    private readonly Grid[] qGammaFields = new Grid[5];
    private readonly List<int> qGammaSlots = [0, 1, 2, 3, 4];

    public QGamma(QInspector lInspector)
    {
        qGammaGate = lInspector.QInspectorVideo.QInspectorGamma;
        QGammaBody = QContract.QContractSheetFind<StackPanel>(QGammaSheet);
        QGammaPersistent = QContract.QContractSheetFind<CheckBox>(QGammaPersistentSheet);
        qGammaActive = QContract.QContractFind<CheckBox>(QGammaBody, QGammaActivePart);
        qGammaStack = QContract.QContractFind<StackPanel>(QGammaBody, QGammaStackPart);

        QGammaSwitchBuild();
        QTool.QToolSwitchAttach(qGammaActive, qGammaGate.CGammaActiveSet);
        QTool.QToolSwitchAttach(QGammaPersistent, qGammaGate.CGammaPersistentSet);
        qGammaSlots.ForEach(QGammaFieldBuild);
        QGammaMidtoneAttach();
        QGammaRedAttach();
        QGammaGreenAttach();
        QGammaBlueAttach();
        QGammaHighlightAttach();

        Button lDefault = QGammaDefaultBuild();
        QGammaDefaultAttach(lDefault);
        QButton.QButtonAttach(lDefault, QButtonLook.QButtonLookPanel);

        QGammaGateAttach();
        QGammaUpdate();
    }

    public StackPanel QGammaBody { get; }

    public CheckBox QGammaPersistent { get; }

    private void QGammaSwitchBuild()
    {
        qGammaActive.Content = QLocalizationCatalog.QLocalizationTextRead("Inspector.Common.Apply");
        qGammaActive.ToolTip = QLocalizationCatalog.QLocalizationTextRead("Inspector.Video.ApplyGamma");
        QGammaPersistent.Content = QLocalizationCatalog.QLocalizationTextRead("Inspector.Common.Persistent");
        QGammaPersistent.ToolTip = QLocalizationCatalog.QLocalizationTextRead("Inspector.Video.PersistGamma");
    }

    private Button QGammaDefaultBuild()
    {
        Button lDefault = QContract.QContractFind<Button>(QGammaBody, QGammaDefaultPart);
        lDefault.Content = QLocalizationCatalog.QLocalizationTextRead("Inspector.Video.GammaReset");
        lDefault.ToolTip = QLocalizationCatalog.QLocalizationTextRead("Inspector.Video.GammaResetTooltip");
        return lDefault;
    }

    private void QGammaDefaultAttach(Button lDefault) => lDefault.Click += (_, _) => qGammaGate.CGammaReset();

    private void QGammaGateAttach() => qGammaGate.CGammaChanged += QGammaUpdate;

    private void QGammaFieldBuild(int lSlot)
    {
        QGammaFieldCreate(lSlot);
        qGammaStack.Children.Insert(lSlot, qGammaFields[lSlot]);
    }

    private void QGammaMidtoneAttach() =>
        QField.QFieldAttach(
            qGammaFields[0], qGammaGate.CGammaLeasts[0], QGammaMost,
            () => qGammaGate.CGammaValues[0], qGammaGate.CGammaMidtoneSet);

    private void QGammaRedAttach() =>
        QField.QFieldAttach(
            qGammaFields[1], qGammaGate.CGammaLeasts[1], QGammaMost,
            () => qGammaGate.CGammaValues[1], qGammaGate.CGammaRedSet);

    private void QGammaGreenAttach() =>
        QField.QFieldAttach(
            qGammaFields[2], qGammaGate.CGammaLeasts[2], QGammaMost,
            () => qGammaGate.CGammaValues[2], qGammaGate.CGammaGreenSet);

    private void QGammaBlueAttach() =>
        QField.QFieldAttach(
            qGammaFields[3], qGammaGate.CGammaLeasts[3], QGammaMost,
            () => qGammaGate.CGammaValues[3], qGammaGate.CGammaBlueSet);

    private void QGammaHighlightAttach() =>
        QField.QFieldAttach(
            qGammaFields[4], qGammaGate.CGammaLeasts[4], QGammaMost,
            () => qGammaGate.CGammaValues[4], qGammaGate.CGammaHighlightSet);

    private void QGammaFieldCreate(int lSlot) =>
        qGammaFields[lSlot] = QField.QFieldBuild(
            QLocalizationCatalog.QLocalizationTextRead(qGammaLabelKeys[lSlot]),
            qGammaUnits[lSlot],
            qGammaGate.CGammaLeasts[lSlot],
            QGammaMost,
            QGammaOrigin);

    private void QGammaFieldUpdate(int lSlot) =>
        QField.QFieldUpdate(qGammaFields[lSlot], qGammaGate.CGammaValues[lSlot], QGammaPattern);

    private void QGammaUpdate()
    {
        QTool.QToolSwitchUpdate(qGammaActive, qGammaGate.CGammaActive);
        QTool.QToolSwitchUpdate(QGammaPersistent, qGammaGate.CGammaPersistent);
        qGammaSlots.ForEach(QGammaFieldUpdate);
        QInspectorTip lTip = QInspectorTip.QInspectorTipResolve(
            qGammaGate.CGammaActive,
            qGammaGate.CGammaCapable,
            qGammaGate.CGammaPreview,
            "Inspector.Video.GammaRequiresEq",
            "Inspector.Video.GammaPreviewMpv",
            "Inspector.Video.ApplyGamma",
            "Inspector.Video.PersistGamma");
        QTool.QToolTipApply(qGammaActive, QGammaPersistent, qGammaStack, QGammaBody, lTip);
    }
}
