namespace Cadroue.ShellEngine;

public static class LSealPending
{
    private static readonly Dictionary<Guid, int> lSealPending = new();

    public static void LSealPendingAdd(Guid lSealCohort)
    {
        if (lSealCohort == Guid.Empty)
        {
            return;
        }

        lSealPending[lSealCohort] = LSealPendingRead(lSealCohort) + 1;
    }

    public static void LSealPendingRemove(Guid lSealCohort)
    {
        if (!lSealPending.TryGetValue(lSealCohort, out int lSealCount))
        {
            return;
        }

        if (lSealCount <= 1)
        {
            lSealPending.Remove(lSealCohort);
        }
        else
        {
            lSealPending[lSealCohort] = lSealCount - 1;
        }
    }

    public static int LSealPendingRead(Guid lSealCohort) =>
        lSealPending.TryGetValue(lSealCohort, out int lSealCount) ? lSealCount : 0;
}
