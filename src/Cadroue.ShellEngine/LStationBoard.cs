namespace Cadroue.ShellEngine;

public static class LStationBoard
{
    private static readonly List<LStation> lStationRecords = new();
    private static readonly object lStationBoardLock = new();
    private static LStation? lStationInternal;

    public static event Action? LStationChanged;

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

        _ = lStationRetired?.LStationRunner.LRunnerDispose();
        LStationChanged?.Invoke();
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

    private static LStation LStationInternalRead()
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

    public static bool LStationActiveCheck()
    {
        foreach (LStation lStation in LStationBoardRead())
        {
            if (lStation.LStationBusyCheck())
            {
                return true;
            }
        }

        return false;
    }

    internal static bool LStationBoardRemove(LStation lStation)
    {
        lock (lStationBoardLock)
        {
            return lStationRecords.Remove(lStation);
        }
    }

    internal static void LStationBoardRaise() => LStationChanged?.Invoke();

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
