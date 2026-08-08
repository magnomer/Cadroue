namespace Cadroue.ShellEngine;

public static partial class LCartographer
{
    public const int LCartographerFinishSlot = -2;

    public static IReadOnlyList<int> LCartographerSlotResolve(IReadOnlyList<Guid> lCartographerTabIds)
    {
        var lCartographerSlots = new List<int>(lCartographerTabIds.Count);
        foreach (Guid lCartographerTabId in lCartographerTabIds)
        {
            Guid lCartographerTarget = LCartographerTargetRead(lCartographerTabId);
            if (lCartographerTarget == LCartographerFinishTarget)
            {
                lCartographerSlots.Add(LCartographerFinishSlot);
                continue;
            }

            int lCartographerSlot = -1;
            for (int lCartographerIndex = 0; lCartographerIndex < lCartographerTabIds.Count; lCartographerIndex++)
            {
                if (lCartographerTabIds[lCartographerIndex] == lCartographerTarget)
                {
                    lCartographerSlot = lCartographerIndex;
                    break;
                }
            }

            lCartographerSlots.Add(lCartographerSlot);
        }

        return lCartographerSlots;
    }

    public static IReadOnlyList<(Guid Source, Guid Target)> LCartographerAssignmentResolve(
        IReadOnlyList<Guid> lCartographerTabIds,
        IReadOnlyList<int> lCartographerSlots)
    {
        var lCartographerAssignments = new List<(Guid Source, Guid Target)>();
        for (int lCartographerIndex = 0; lCartographerIndex < lCartographerTabIds.Count; lCartographerIndex++)
        {
            if (lCartographerIndex >= lCartographerSlots.Count)
            {
                break;
            }

            int lCartographerSlot = lCartographerSlots[lCartographerIndex];
            if (lCartographerSlot == LCartographerFinishSlot)
            {
                lCartographerAssignments.Add((lCartographerTabIds[lCartographerIndex], LCartographerFinishTarget));
                continue;
            }

            if (lCartographerSlot < 0 || lCartographerSlot >= lCartographerTabIds.Count || lCartographerSlot == lCartographerIndex)
            {
                continue;
            }

            lCartographerAssignments.Add((lCartographerTabIds[lCartographerIndex], lCartographerTabIds[lCartographerSlot]));
        }

        return lCartographerAssignments;
    }
}
