using System.Windows.Controls;
using Cadroue.Conduct;

namespace Cadroue.UIDeportment;

// The exposure tool's wiring: fills fresh PExposure copies, hands every input to the gate and shows the gate's state.
public sealed class QExposure
{
    private const string QExposureSheet = "PExposure";

    private const string QExposurePersistentSheet = "PExposurePersistent";

    private const string QExposureActivePart = "PExposureActive";

    private const string QExposureStackPart = "PExposureStack";

    private const double QExposureLeast = -3;

    private const double QExposureMost = 3;

    private const double QExposureOrigin = 0;

    private const string QExposureUnit = "EV";

    private const string QExposurePattern = "0.#";

    private readonly CExposure qExposureGate;
    private readonly CheckBox qExposureActive;
    private readonly StackPanel qExposureStack;
    private readonly Grid qExposureField;

    public QExposure(QInspector lInspector)
    {
        qExposureGate = lInspector.QInspectorVideo.QInspectorExposure;
        QExposureBody = QContract.QContractSheetFind<StackPanel>(QExposureSheet);
        QExposurePersistent = QContract.QContractSheetFind<CheckBox>(QExposurePersistentSheet);
        qExposureActive = QContract.QContractFind<CheckBox>(QExposureBody, QExposureActivePart);
        qExposureStack = QContract.QContractFind<StackPanel>(QExposureBody, QExposureStackPart);

        qExposureActive.Content = QLocalizationCatalog.QLocalizationTextRead("Inspector.Common.Apply");
        qExposureActive.ToolTip = QLocalizationCatalog.QLocalizationTextRead("Inspector.Video.ApplyExposure");
        QExposurePersistent.Content = QLocalizationCatalog.QLocalizationTextRead("Inspector.Common.Persistent");
        QExposurePersistent.ToolTip = QLocalizationCatalog.QLocalizationTextRead("Inspector.Video.PersistExposure");

        qExposureField = QField.QFieldBuild(
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Common.Amount"),
            QExposureUnit,
            QExposureLeast,
            QExposureMost,
            QExposureOrigin);
        qExposureStack.Children.Insert(0, qExposureField);

        QExposureAttach();
        QExposureUpdate();
    }

    public StackPanel QExposureBody { get; }

    public CheckBox QExposurePersistent { get; }

    private void QExposureAttach()
    {
        QTool.QToolSwitchAttach(qExposureActive, qExposureGate.CExposureActiveSet);
        QTool.QToolSwitchAttach(QExposurePersistent, qExposureGate.CExposurePersistentSet);
        QField.QFieldAttach(
            qExposureField,
            QExposureLeast,
            QExposureMost,
            () => qExposureGate.CExposureValue,
            qExposureGate.CExposureValueSet);
        qExposureGate.CExposureChanged += QExposureUpdate;
    }

    private void QExposureUpdate()
    {
        QTool.QToolSwitchUpdate(qExposureActive, qExposureGate.CExposureActive);
        QTool.QToolSwitchUpdate(QExposurePersistent, qExposureGate.CExposurePersistent);
        QField.QFieldUpdate(qExposureField, qExposureGate.CExposureValue, QExposurePattern);
        QInspectorTip lTip = QInspectorTip.QInspectorTipResolve(
            qExposureGate.CExposureActive,
            qExposureGate.CExposureCapable,
            qExposureGate.CExposurePreview,
            "Inspector.Video.ExposureRequiresEq",
            "Inspector.Video.ExposurePreviewMpv",
            "Inspector.Video.ApplyExposure",
            "Inspector.Video.PersistExposure");
        QTool.QToolTipApply(qExposureActive, QExposurePersistent, qExposureStack, QExposureBody, lTip);
    }
}
