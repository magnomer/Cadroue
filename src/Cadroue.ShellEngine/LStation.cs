using Cadroue.Core;
using Cadroue.Infrastructure;

namespace Cadroue.ShellEngine;

public sealed class LStation
{
    private static readonly List<LStation> lStationRecords = new();
    private static readonly object lStationBoardLock = new();
    private static LStation? lStationInternal;

    private bool lStationAutoActive;

    public static LScheduleContract? LStationSchedule { get; set; }

    public static Action<Action>? LStationPost { get; set; }

    public static Func<string>? LStationProgramSource { get; set; }

    public static Func<LPreferenceState>? LStationPreferenceSource { get; set; }

    private LStation(string lStationLabel)
    {
        LStationLabel = lStationLabel;
        LStationRunner = new LRunner(LStationSchedule!, LStationPost!)
        {
            LRunnerProgramPath = LStationProgramSource!()
        };
        lStationAutoActive = LStationPreferenceSource!().LPreferenceAutoActive;
        LStationSchedule!.LScheduleChange += LStationScheduleHandle;
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

    public static IReadOnlyList<LStation> LStationRecords
    {
        get
        {
            lock (lStationBoardLock)
            {
                return lStationRecords.ToArray();
            }
        }
    }

    public static LStation LStationCreate(string lStationLabel)
    {
        LStation lStation;
        LStation? lStationRetired;
        lock (lStationBoardLock)
        {
            lStation = LStationSeedAccept(lStationLabel);
            lStationRecords.Add(lStation);
            lStationRetired = LStationInternalRemove();
        }

        lStationRetired?.LStationRunner.LRunnerDispose();
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
        lock (lStationBoardLock)
        {
            lStationInternal ??= new LStation("Background worklist");
            return lStationInternal;
        }
    }

    public static LStation[] LStationBoardRead()
    {
        lock (lStationBoardLock)
        {
            if (lStationRecords.Count > 0)
            {
                return lStationRecords.ToArray();
            }
        }

        return new[] { LStationInternalRead() };
    }

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
            || !LStationSchedule!.LSchedulePendingExist())
        {
            return;
        }

        LPreferenceState lPreferenceState = LStationPreferenceSource!();
        LStationRunner.LRunnerProgramPath = LStationProgramSource!();
        LStationRunner.LRunnerFailurePaused = lPreferenceState.LPreferenceFailurePaused;
        LStationRunner.LRunnerRetryAllowed = lPreferenceState.LPreferenceRetryAllowed;
        LStationRunner.LRunnerRetryMaximum = (int)lPreferenceState.LPreferenceRetryMaximum;
        LStationRunner.LRunnerStart();
    }

    public void LStationClose()
    {
        lStationAutoActive = false;
        LStationSelectionSource = null;
        LStationSchedule!.LScheduleChange -= LStationScheduleHandle;

        bool lStationRemoved;
        lock (lStationBoardLock)
        {
            lStationRemoved = lStationRecords.Remove(this);
        }

        if (!lStationRemoved)
        {
            return;
        }

        LStationRunner.LRunnerDispose();
        LStationChange?.Invoke();
    }

    private static LStation? LStationInternalRemove()
    {
        if (lStationInternal is null || lStationRecords.Count == 0)
        {
            return null;
        }

        if (lStationInternal.LStationRunner.LRunnerRunning)
        {
            return null;
        }

        LStation lStationRetired = lStationInternal;
        lStationInternal = null;
        return lStationRetired;
    }
}
