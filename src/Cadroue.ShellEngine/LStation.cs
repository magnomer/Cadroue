using Cadroue.Core;

namespace Cadroue.ShellEngine;

public sealed class LStation
{
    private bool lStationAutoActive;

    internal LStation(string lStationLabel)
    {
        LStationLabel = lStationLabel;
        LStationRunner = new LRunner(
            LStationEngine.LStationSchedule!,
            LStationEngine.LStationIntake!,
            LStationEngine.LStationLease!,
            LStationEngine.LStationTransition!,
            LStationEngine.LStationPost!)
        {
            LRunnerProgramPath = LStationEngine.LStationProgramSource!()
        };
        lStationAutoActive = LStationEngine.LStationPreferenceSource!().LPreferenceAutoActive;
        LStationEngine.LStationSchedule!.LScheduleChanged += LStationScheduleHandle;
    }

    public string LStationLabel { get; internal set; }

    public bool LStationAutoActive
    {
        get => lStationAutoActive;
        set
        {
            if (lStationAutoActive == value)
            {
                return;
            }

            lStationAutoActive = value;
            LStationAutoApply();
        }
    }

    public LRunner LStationRunner { get; }

    public Func<IReadOnlyList<LWorkItem>>? LStationSelectionSource { get; set; }

    public void LStationSelectionRaise() => LStationBoard.LStationBoardRaise();

    public IReadOnlyList<LWorkItem> LStationSelectionRead()
    {
        try
        {
            return LStationSelectionSource?.Invoke() ?? Array.Empty<LWorkItem>();
        }
        catch (Exception lStationException)
        {
            LTraceEngine.LTraceErrorSeam("Worklist selection could not be read", lStationException);
            return Array.Empty<LWorkItem>();
        }
    }

    public bool LStationBusyCheck() =>
        LStationRunner.LRunnerRunning || LStationRunner.LRunnerProcess.LRunnerProcessSuspended;

    private void LStationScheduleHandle(LScheduleContract lSchedule) => LStationAutoApply();

    private void LStationAutoApply()
    {
        if (!lStationAutoActive
            || LStationRunner.LRunnerRunning
            || LStationRunner.LRunnerPaused
            || !LStationEngine.LStationLease!.LSchedulePendingExist())
        {
            return;
        }

        LPreferenceState lPreferenceState = LStationEngine.LStationPreferenceSource!();
        LStationRunner.LRunnerProgramPath = LStationEngine.LStationProgramSource!();
        LStationRunner.LRunnerFailurePaused = lPreferenceState.LPreferenceFailurePaused;
        LStationRunner.LRunnerRetryAllowed = lPreferenceState.LPreferenceRetryAllowed;
        LStationRunner.LRunnerRetryMaximum = (int)lPreferenceState.LPreferenceRetryMaximum;
        LStationRunner.LRunnerStart();
    }

    public void LStationClose()
    {
        lStationAutoActive = false;
        LStationSelectionSource = null;
        LStationEngine.LStationSchedule!.LScheduleChanged -= LStationScheduleHandle;

        if (!LStationBoard.LStationBoardRemove(this))
        {
            return;
        }

        _ = LStationRunner.LRunnerDispose();
        LStationBoard.LStationBoardRaise();
    }
}
