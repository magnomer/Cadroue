namespace Cadroue.UIDeportment;

// The detection sweep's switches on the sensor panel: whether it runs, whether it reruns by itself, and the run button.
public sealed class QSensorSweep
{
    private bool qSensorRunning;
    private bool qSensorPersistent;

    public event Action<bool>? QSensorRunningChanged;
    public event Action<bool>? QSensorPersistentChanged;
    public event Action? QSensorRunApplied;
    public event Action? QSensorStopApplied;

    public string QSensorRunText =>
        QLocalizationCatalog.QLocalizationTextRead(qSensorRunning ? "Inspector.Detect.Stop" : "Inspector.Detect.Run");

    public bool QSensorRunning => qSensorRunning;

    public bool QSensorPersistent => qSensorPersistent;

    // The run button asks for a stop while a sweep runs, else for a run; the split tab answers either.
    public void QSensorRunHandle()
    {
        if (qSensorRunning)
        {
            QSensorStopApplied?.Invoke();
        }
        else
        {
            QSensorRunApplied?.Invoke();
        }
    }

    public void QSensorRunningSet(bool lRunning)
    {
        if (qSensorRunning == lRunning)
        {
            return;
        }

        qSensorRunning = lRunning;
        QSensorRunningChanged?.Invoke(lRunning);
    }

    public void QSensorPersistentSet(bool lPersistent)
    {
        if (qSensorPersistent == lPersistent)
        {
            return;
        }

        qSensorPersistent = lPersistent;
        QSensorPersistentChanged?.Invoke(lPersistent);
    }
}
