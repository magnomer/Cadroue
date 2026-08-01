using System.Windows;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.ShellEngine;

namespace Cadroue.UIShell.PMainArea;

public sealed class LStation
{
    private static readonly List<LStation> lStationRecords = new();
    private static LStation? lStationInternal;

    private bool lStationAutoActive;

    private LStation(string lStationLabel)
    {
        LStationLabel = lStationLabel;
        LStationRunner = new LRunner(LSchedule.LScheduleCurrent, LStationDispatch)
        {
            LRunnerProgramPath = PProgram.LRendererProgramCurrent
        };
        lStationAutoActive = PProgram.LPreferenceStateCurrent.LPreferenceAutoActive;
        LSchedule.LScheduleCurrent.LScheduleChange += LStationScheduleHandle;
    }

    public string LStationLabel { get; private set; }

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

    public static event Action? LStationChange;

    public static IReadOnlyList<LStation> LStationRecords => lStationRecords.ToArray();

    public static LStation LStationCreate(string lStationLabel)
    {
        LStation lStation = LStationSeedAccept(lStationLabel);
        lStationRecords.Add(lStation);
        LStationInternalRemove();
        LStationChange?.Invoke();
        return lStation;
    }

    private static LStation LStationSeedAccept(string lStationLabel)
    {
        if (lStationRecords.Count > 0 || lStationInternal is null)
        {
            return new LStation(lStationLabel);
        }

        LStation lStationSeed = lStationInternal;
        lStationInternal = null;
        lStationSeed.LStationLabel = lStationLabel;
        return lStationSeed;
    }

    public static LStation LStationInternalRead()
    {
        lStationInternal ??= new LStation("Background worklist");
        return lStationInternal;
    }

    public static LStation[] LStationBoardRead() =>
        lStationRecords.Count > 0 ? lStationRecords.ToArray() : new[] { LStationInternalRead() };

    public IReadOnlyList<LWorkItem> LStationSelectionRead()
    {
        try
        {
            return LStationSelectionSource?.Invoke() ?? Array.Empty<LWorkItem>();
        }
        catch (Exception lStationException)
        {
            LTraceLog.LTraceErrorRecord("Worklist selection could not be read", lStationException);
            return Array.Empty<LWorkItem>();
        }
    }

    public bool LStationBusyCheck() =>
        LStationRunner.LRunnerRunning || LStationRunner.LRunnerSuspended;

    private void LStationScheduleHandle(LScheduleContract lSchedule) => LStationAutoApply();

    private void LStationAutoApply()
    {
        if (!lStationAutoActive
            || LStationRunner.LRunnerRunning
            || !LSchedule.LScheduleCurrent.LSchedulePendingExist())
        {
            return;
        }

        LPreferenceState lPreferenceState = PProgram.LPreferenceStateCurrent;
        LStationRunner.LRunnerProgramPath = PProgram.LRendererProgramCurrent;
        LStationRunner.LRunnerParallelMaximum = (int)lPreferenceState.LPreferenceParallelMaximum;
        LStationRunner.LRunnerFailurePaused = lPreferenceState.LPreferenceFailurePaused;
        LStationRunner.LRunnerRetryAllowed = lPreferenceState.LPreferenceRetryAllowed;
        LStationRunner.LRunnerRetryMaximum = (int)lPreferenceState.LPreferenceRetryMaximum;
        LStationRunner.LRunnerStart();
    }

    public void LStationClose()
    {
        lStationAutoActive = false;
        LStationSelectionSource = null;
        LSchedule.LScheduleCurrent.LScheduleChange -= LStationScheduleHandle;
        if (!lStationRecords.Remove(this))
        {
            return;
        }

        LStationRunner.LRunnerDispose();
        LStationChange?.Invoke();
    }

    private static void LStationInternalRemove()
    {
        if (lStationInternal is null || lStationRecords.Count == 0)
        {
            return;
        }

        if (lStationInternal.LStationRunner.LRunnerRunning)
        {
            return;
        }

        lStationInternal.LStationRunner.LRunnerDispose();
        lStationInternal = null;
    }

    private static void LStationDispatch(Action lStationAction)
    {
        if (Application.Current?.Dispatcher is { } lStationDispatcher)
        {
            lStationDispatcher.Invoke(lStationAction);
            return;
        }

        lStationAction();
    }
}
