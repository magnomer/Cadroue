using System.Windows;
using System.Windows.Controls;
using Cadroue.Conduct;

namespace Cadroue.UIDeportment;

// The sweep-row driver: it turns direct user acts into requests and paints the Conduct-owned state.
public sealed class QSensorSweep
{
    private readonly CSweep qSensorSweepGate;
    private CheckBox qSensorRunPersistent = null!;
    private Button qSensorRunButton = null!;
    private ProgressBar qSensorProgress = null!;

    internal QSensorSweep(CSweep sweep)
    {
        qSensorSweepGate = sweep;
    }

    public Border QSensorSweepBody { get; private set; } = null!;

    public void QSensorSweepBuild()
    {
        QSensorSweepClose();
        QSensorSweepBody = QContract.QContractSheetFind<Border>("PSensorSweepBody");
        qSensorRunPersistent = QContract.QContractFind<CheckBox>(QSensorSweepBody, "PSensorRunPersistent");
        qSensorRunButton = QContract.QContractFind<Button>(QSensorSweepBody, "PSensorRunButton");
        qSensorProgress = QContract.QContractFind<ProgressBar>(QSensorSweepBody, "PSensorProgress");

        qSensorRunPersistent.Content = QLocalizationCatalog.QLocalizationTextRead("Inspector.Detect.Persistent");
        qSensorRunPersistent.ToolTip =
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Detect.PersistentTooltip");
        QInteractionClick.QInteractionClickAttach(qSensorRunPersistent);
        QCheckbox.QCheckboxAttach(qSensorRunPersistent);
        QButton.QButtonAttach(qSensorRunButton, QButtonLook.QButtonLookWhite);
        Action<bool> lRunning = _ => QSensorSweepUpdate();
        Action<bool> lPersistent = _ => QSensorSweepUpdate();
        Action lAttach = () =>
        {
            qSensorRunPersistent.Click -= QSensorRunAttach;
            qSensorRunButton.Click -= QSensorRunAttach;
            qSensorSweepGate.CSweepRunningChanged -= lRunning;
            qSensorSweepGate.CSweepPersistentChanged -= lPersistent;
            qSensorRunPersistent.Click += QSensorRunAttach;
            qSensorRunButton.Click += QSensorRunAttach;
            qSensorSweepGate.CSweepRunningChanged += lRunning;
            qSensorSweepGate.CSweepPersistentChanged += lPersistent;
        };
        Action lDetach = () =>
        {
            qSensorRunPersistent.Click -= QSensorRunAttach;
            qSensorRunButton.Click -= QSensorRunAttach;
            qSensorSweepGate.CSweepRunningChanged -= lRunning;
            qSensorSweepGate.CSweepPersistentChanged -= lPersistent;
        };
        QSensorSweepBody.Resources[typeof(QSensorSweep)] = (lAttach, lDetach);
        QSensorSweepAttach();
    }

    public void QSensorSweepClose()
    {
        if (qSensorRunButton is not null
            && QSensorSweepBody.Resources[typeof(QSensorSweep)] is ValueTuple<Action, Action> lHandlers)
        {
            lHandlers.Item2();
        }
    }

    public void QSensorSweepUpdate(double progress) => qSensorProgress.Value = progress;

    private void QSensorRunAttach(object lSource, RoutedEventArgs __)
    {
        if (ReferenceEquals(lSource, qSensorRunPersistent))
        {
            qSensorSweepGate.CSweepPersistentSet(qSensorRunPersistent.IsChecked == true);
            return;
        }

        qSensorSweepGate.CSweepRunHandle();
    }

    public void QSensorSweepAttach()
    {
        if (QSensorSweepBody.Resources[typeof(QSensorSweep)] is ValueTuple<Action, Action> lHandlers)
        {
            lHandlers.Item1();
        }

        QSensorSweepUpdate();
    }

    private void QSensorSweepUpdate()
    {
        qSensorRunPersistent.IsChecked = qSensorSweepGate.CSweepPersistent;
        qSensorRunButton.Content = QLocalizationCatalog.QLocalizationTextRead(
            qSensorSweepGate.CSweepRunning ? "Inspector.Detect.Stop" : "Inspector.Detect.Run");
        qSensorProgress.Visibility = qSensorSweepGate.CSweepRunning
            ? Visibility.Visible
            : Visibility.Collapsed;
    }
}
