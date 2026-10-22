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
        qGammaSlots.ForEach(QGammaFieldBuild);

        Button lDefault = QGammaDefaultBuild();
        QButton.QButtonAttach(lDefault, QButtonLook.QButtonLookPanel);
        QGammaBody.Resources[typeof(QGamma)] = new List<(Action, Action)>
        {
            QTool.QToolSwitchAttach(qGammaActive, qGammaGate.CGammaActiveSet),
            QTool.QToolSwitchAttach(QGammaPersistent, qGammaGate.CGammaPersistentSet),
            QGammaMidtoneAttach(),
            QGammaRedAttach(),
            QGammaGreenAttach(),
            QGammaBlueAttach(),
            QGammaHighlightAttach(),
            QGammaDefaultAttach(lDefault),
        };

        QGammaAttach();
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

    private (Action, Action) QGammaDefaultAttach(Button lDefault)
    {
        System.Windows.RoutedEventHandler lHandler = (_, _) => qGammaGate.CGammaReset();
        Action lAttach = () =>
        {
            lDefault.Click -= lHandler;
            lDefault.Click += lHandler;
        };
        Action lDetach = () => lDefault.Click -= lHandler;
        lAttach();
        return (lAttach, lDetach);
    }

    private void QGammaFieldBuild(int lSlot)
    {
        QGammaFieldCreate(lSlot);
        qGammaStack.Children.Insert(lSlot, qGammaFields[lSlot]);
    }

    private (Action, Action) QGammaMidtoneAttach() =>
        QField.QFieldAttach(
            qGammaFields[0], qGammaGate.CGammaLeasts[0], QGammaMost,
            () => qGammaGate.CGammaValues[0], qGammaGate.CGammaMidtoneSet);

    private (Action, Action) QGammaRedAttach() =>
        QField.QFieldAttach(
            qGammaFields[1], qGammaGate.CGammaLeasts[1], QGammaMost,
            () => qGammaGate.CGammaValues[1], qGammaGate.CGammaRedSet);

    private (Action, Action) QGammaGreenAttach() =>
        QField.QFieldAttach(
            qGammaFields[2], qGammaGate.CGammaLeasts[2], QGammaMost,
            () => qGammaGate.CGammaValues[2], qGammaGate.CGammaGreenSet);

    private (Action, Action) QGammaBlueAttach() =>
        QField.QFieldAttach(
            qGammaFields[3], qGammaGate.CGammaLeasts[3], QGammaMost,
            () => qGammaGate.CGammaValues[3], qGammaGate.CGammaBlueSet);

    private (Action, Action) QGammaHighlightAttach() =>
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

    public void QGammaAttach()
    {
        foreach ((Action lAttach, _) in (List<(Action, Action)>)QGammaBody.Resources[typeof(QGamma)])
        {
            lAttach();
        }

        qGammaGate.CGammaChanged -= QGammaUpdate;
        qGammaGate.CGammaChanged += QGammaUpdate;
    }

    public void QGammaDetach()
    {
        qGammaGate.CGammaChanged -= QGammaUpdate;
        foreach ((_, Action lDetach) in (List<(Action, Action)>)QGammaBody.Resources[typeof(QGamma)])
        {
            lDetach();
        }
    }

    private void QGammaUpdate()
    {
        QTool.QToolSwitchUpdate(qGammaActive, qGammaGate.CGammaActive);
        QTool.QToolSwitchUpdate(QGammaPersistent, qGammaGate.CGammaPersistent);
        qGammaSlots.ForEach(QGammaFieldUpdate);
        QInspectorTip lTip = QInspectorTip.QInspectorTipResolve(
            CInspectorTip.CInspectorTipResolve(
                qGammaGate.CGammaActive,
                qGammaGate.CGammaCapable,
                qGammaGate.CGammaPreview,
                "Inspector.Video.GammaRequiresEq",
                "Inspector.Video.GammaPreviewMpv"),
            "Inspector.Video.ApplyGamma",
            "Inspector.Video.PersistGamma");
        QTool.QToolTipApply(qGammaActive, QGammaPersistent, qGammaStack, QGammaBody, lTip);
    }
}
