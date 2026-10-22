using System.Windows;
using System.Windows.Controls;
using Cadroue.Conduct;

namespace Cadroue.UIDeportment;

// The volume tool's wiring: fills fresh PVolume copies, hands every input to the gate and shows the gate's state.
public sealed class QVolume
{
    private const string QVolumeSheet = "PVolume";

    private const string QVolumePersistentSheet = "PVolumePersistent";

    private const string QVolumeActivePart = "PVolumeActive";

    private const string QVolumeStackPart = "PVolumeStack";

    private const string QVolumeClipPart = "PVolumeClip";

    private const double QVolumeLeast = -24;

    private const double QVolumeMost = 24;

    private const double QVolumeOrigin = 0;

    private const string QVolumeUnit = "dB";

    private const string QVolumePattern = "0.#";

    private readonly CVolume qVolumeGate;
    private readonly CheckBox qVolumeActive;
    private readonly StackPanel qVolumeStack;
    private readonly TextBlock qVolumeClip;
    private readonly Grid qVolumeField;

    public QVolume(QInspector lInspector)
    {
        qVolumeGate = lInspector.QInspectorAudio.QInspectorVolume;
        QVolumeBody = QContract.QContractSheetFind<StackPanel>(QVolumeSheet);
        QVolumePersistent = QContract.QContractSheetFind<CheckBox>(QVolumePersistentSheet);
        qVolumeActive = QContract.QContractFind<CheckBox>(QVolumeBody, QVolumeActivePart);
        qVolumeStack = QContract.QContractFind<StackPanel>(QVolumeBody, QVolumeStackPart);
        qVolumeClip = QContract.QContractFind<TextBlock>(QVolumeBody, QVolumeClipPart);
        qVolumeField = QField.QFieldBuild(
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Volume.Gain"),
            QVolumeUnit,
            QVolumeLeast,
            QVolumeMost,
            QVolumeOrigin);
        qVolumeStack.Children.Insert(0, qVolumeField);

        QVolumeTextShow();
        QVolumeBody.Resources[typeof(QVolume)] = new List<(Action, Action)>
        {
            QTool.QToolSwitchAttach(qVolumeActive, qVolumeGate.CVolumeActiveSet),
            QTool.QToolSwitchAttach(QVolumePersistent, qVolumeGate.CVolumePersistentSet),
            QField.QFieldAttach(
                qVolumeField,
                QVolumeLeast,
                QVolumeMost,
                () => qVolumeGate.CVolumeGain,
                qVolumeGate.CVolumeGainSet),
        };
        QVolumeHandlersAttach();
        QVolumeUpdate();
    }

    public StackPanel QVolumeBody { get; }

    public CheckBox QVolumePersistent { get; }

    private void QVolumeTextShow()
    {
        qVolumeActive.Content = QLocalizationCatalog.QLocalizationTextRead("Inspector.Common.Apply");
        qVolumeActive.ToolTip = QLocalizationCatalog.QLocalizationTextRead("Inspector.Volume.ApplyTooltip");
        QVolumePersistent.Content = QLocalizationCatalog.QLocalizationTextRead("Inspector.Common.Persistent");
        QVolumePersistent.ToolTip = QLocalizationCatalog.QLocalizationTextRead("Inspector.Volume.PersistentTooltip");
        qVolumeClip.Text = QLocalizationCatalog.QLocalizationTextRead("Inspector.Volume.ClipNotice");
    }

    // Every input goes to the gate, and every gate change comes back to the controls.
    public void QVolumeHandlersAttach()
    {
        foreach ((Action lAttach, _) in (List<(Action, Action)>)QVolumeBody.Resources[typeof(QVolume)])
        {
            lAttach();
        }

        qVolumeGate.CVolumeChanged -= QVolumeUpdate;
        qVolumeGate.CVolumeChanged += QVolumeUpdate;
    }

    public void QVolumeHandlersDetach()
    {
        qVolumeGate.CVolumeChanged -= QVolumeUpdate;
        foreach ((_, Action lDetach) in (List<(Action, Action)>)QVolumeBody.Resources[typeof(QVolume)])
        {
            lDetach();
        }
    }

    private void QVolumeUpdate()
    {
        QTool.QToolSwitchUpdate(qVolumeActive, qVolumeGate.CVolumeActive);
        QTool.QToolSwitchUpdate(QVolumePersistent, qVolumeGate.CVolumePersistent);
        QField.QFieldUpdate(qVolumeField, qVolumeGate.CVolumeGain, QVolumePattern);
        QTool.QToolStackUpdate(qVolumeStack, qVolumeGate.CVolumeActive);
        qVolumeClip.Visibility = QVolumeClipResolve(qVolumeGate.CVolumeClipShown);
    }

    private static Visibility QVolumeClipResolve(bool lClipShown) =>
        lClipShown ? Visibility.Visible : Visibility.Collapsed;
}
