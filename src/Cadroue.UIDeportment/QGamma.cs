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

    private readonly QInspector qGammaInspector;
    private readonly CGamma qGammaGate;
    private readonly CheckBox qGammaActive;
    private readonly StackPanel qGammaStack;
    private readonly Grid[] qGammaFields = new Grid[5];
    private readonly List<int> qGammaSlots = [0, 1, 2, 3, 4];

    public QGamma(QInspector lInspector)
    {
        qGammaInspector = lInspector;
        qGammaGate = lInspector.QInspectorVideo.QInspectorGamma;
        QGammaBody = QContract.QContractSheetFind<StackPanel>(QGammaSheet);
        QGammaPersistent = QContract.QContractSheetFind<CheckBox>(QGammaPersistentSheet);
        qGammaActive = QContract.QContractFind<CheckBox>(QGammaBody, QGammaActivePart);
        qGammaStack = QContract.QContractFind<StackPanel>(QGammaBody, QGammaStackPart);

        qGammaActive.Content = QLocalizationCatalog.QLocalizationTextRead("Inspector.Common.Apply");
        qGammaActive.ToolTip = QLocalizationCatalog.QLocalizationTextRead("Inspector.Video.ApplyGamma");
        QGammaPersistent.Content = QLocalizationCatalog.QLocalizationTextRead("Inspector.Common.Persistent");
        QGammaPersistent.ToolTip = QLocalizationCatalog.QLocalizationTextRead("Inspector.Video.PersistGamma");
        QTool.QToolSwitchAttach(qGammaActive, qGammaGate.CGammaActiveSet);
        QTool.QToolSwitchAttach(QGammaPersistent, qGammaGate.CGammaPersistentSet);
        qGammaSlots.ForEach(QGammaFieldBuild);

        Button lDefault = QContract.QContractFind<Button>(QGammaBody, QGammaDefaultPart);
        lDefault.Content = QLocalizationCatalog.QLocalizationTextRead("Inspector.Video.GammaReset");
        lDefault.ToolTip = QLocalizationCatalog.QLocalizationTextRead("Inspector.Video.GammaResetTooltip");
        lDefault.Click += (_, _) => qGammaGate.CGammaReset();
        QButton.QButtonAttach(lDefault, QButtonLook.QButtonLookPanel);

        qGammaGate.CGammaChanged += QGammaUpdate;
        QGammaUpdate();
    }

    public StackPanel QGammaBody { get; }

    public CheckBox QGammaPersistent { get; }

    private void QGammaFieldBuild(int lSlot)
    {
        double lLeast = qGammaGate.CGammaLeasts[lSlot];
        qGammaFields[lSlot] = QField.QFieldBuild(
            QLocalizationCatalog.QLocalizationTextRead(qGammaLabelKeys[lSlot]),
            qGammaUnits[lSlot],
            lLeast,
            QGammaMost,
            QGammaOrigin);
        QField.QFieldAttach(
            qGammaFields[lSlot],
            lLeast,
            QGammaMost,
            () => qGammaGate.CGammaValues[lSlot],
            lNumber => qGammaGate.CGammaValueSet(lSlot, lNumber));
        qGammaStack.Children.Insert(lSlot, qGammaFields[lSlot]);
    }

    private void QGammaFieldUpdate(int lSlot) =>
        QField.QFieldUpdate(qGammaFields[lSlot], qGammaGate.CGammaValues[lSlot], QGammaPattern);

    private void QGammaUpdate()
    {
        QTool.QToolSwitchUpdate(qGammaActive, qGammaGate.CGammaActive);
        QTool.QToolSwitchUpdate(QGammaPersistent, qGammaGate.CGammaPersistent);
        qGammaSlots.ForEach(QGammaFieldUpdate);
        QInspectorTip lTip = qGammaInspector.QInspectorTipResolve(
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
