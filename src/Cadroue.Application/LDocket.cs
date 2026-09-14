namespace Cadroue.Application;

public sealed class LDocket
{
    private readonly List<LDocketEntry> lDocketEntries = new();

    public event Action<IReadOnlyList<LDocketEntry>>? LDocketChange;

    public event Action<IReadOnlyList<LDocketEntry>>? LDocketAdded;

    public event Action<IReadOnlyList<string>>? LDocketRemoved;

    public IReadOnlyList<LDocketEntry> LDocketItemsRead() => lDocketEntries.ToArray();

    public IReadOnlyList<LDocketEntry> LDocketUnlockedRead() =>
        lDocketEntries.Where(lDocketEntry => !lDocketEntry.LDocketEntryLocked).ToArray();

    public IReadOnlyList<LDocketEntry> LDocketStaleRead(IReadOnlySet<Guid> lDocketActiveBatches) =>
        lDocketEntries.Where(lDocketEntry => !LDocketProtectedCheck(lDocketEntry, lDocketActiveBatches)).ToArray();

    public IReadOnlyList<LDocketEntry> LDocketProtectedRead(IReadOnlySet<Guid> lDocketActiveBatches) =>
        lDocketEntries.Where(lDocketEntry => LDocketProtectedCheck(lDocketEntry, lDocketActiveBatches)).ToArray();

    private static bool LDocketProtectedCheck(LDocketEntry lDocketEntry, IReadOnlySet<Guid> lDocketActiveBatches) =>
        lDocketEntry.LDocketEntryLocked || lDocketActiveBatches.Contains(lDocketEntry.LDocketEntryBatch);

    public IReadOnlyList<string> LDocketPathsRead() =>
        lDocketEntries.Select(lDocketEntry => lDocketEntry.LDocketEntryPath).ToArray();

    public LDocketEntry? LDocketItemFind(string lDocketPath) =>
        lDocketEntries.FirstOrDefault(lDocketEntry =>
            string.Equals(lDocketEntry.LDocketEntryPath, lDocketPath, StringComparison.OrdinalIgnoreCase));

    public bool LDocketLockCheck(string lDocketPath) =>
        LDocketItemFind(lDocketPath)?.LDocketEntryLocked == true;

    public int LDocketPathsAdd(IReadOnlyList<string> lDocketPaths, Guid lDocketBatch = default)
    {
        if (lDocketPaths.Count == 0)
        {
            return 0;
        }

        if (lDocketBatch == Guid.Empty)
        {
            lDocketBatch = LGate.LGateBatchCreate();
        }

        var lDocketAddedItems = new List<LDocketEntry>();
        foreach (string lDocketPath in lDocketPaths)
        {
            if (LDocketItemFind(lDocketPath) is not null)
            {
                continue;
            }

            var lDocketEntry = new LDocketEntry(lDocketPath, lDocketBatch);
            lDocketEntries.Add(lDocketEntry);
            lDocketAddedItems.Add(lDocketEntry);
        }

        if (lDocketAddedItems.Count > 0)
        {
            LDocketRaise();
            LDocketAdded?.Invoke(lDocketAddedItems);
        }

        return lDocketAddedItems.Count;
    }

    public int LDocketPathsRemove(IReadOnlyList<string> lDocketPaths)
    {
        HashSet<string> lDocketRemoved = lDocketPaths.ToHashSet(StringComparer.OrdinalIgnoreCase);
        string[] lDocketRemovedPaths = lDocketEntries
            .Where(lDocketEntry => lDocketRemoved.Contains(lDocketEntry.LDocketEntryPath))
            .Select(lDocketEntry => lDocketEntry.LDocketEntryPath)
            .ToArray();
        lDocketEntries.RemoveAll(lDocketEntry => lDocketRemoved.Contains(lDocketEntry.LDocketEntryPath));
        if (lDocketRemovedPaths.Length > 0)
        {
            LDocketRaise();
            LDocketRemoved?.Invoke(lDocketRemovedPaths);
        }

        return lDocketRemovedPaths.Length;
    }

    public int LDocketClaim(IReadOnlyList<(string LDocketPath, Guid LDocketBatch)> lDocketClaims)
    {
        int lDocketClaimed = 0;
        foreach ((string lDocketPath, Guid lDocketBatch) in lDocketClaims)
        {
            LDocketEntry? lDocketEntry = LDocketItemFind(lDocketPath);
            if (lDocketEntry is null || lDocketEntry.LDocketEntryLocked)
            {
                continue;
            }

            if (lDocketBatch != Guid.Empty)
            {
                lDocketEntry.LDocketEntryBatch = lDocketBatch;
            }

            lDocketEntry.LDocketEntryLocked = true;
            lDocketClaimed++;
        }

        if (lDocketClaimed > 0)
        {
            LDocketRaise();
        }

        return lDocketClaimed;
    }

    public int LDocketRelease(IReadOnlyList<(string LDocketPath, Guid LDocketBatch)> lDocketReleases)
    {
        int lDocketReleased = 0;
        Guid lDocketFresh = LGate.LGateBatchCreate();
        foreach ((string lDocketPath, Guid lDocketBatch) in lDocketReleases)
        {
            LDocketEntry? lDocketEntry = LDocketItemFind(lDocketPath);
            if (lDocketEntry is null
                || !lDocketEntry.LDocketEntryLocked
                || lDocketEntry.LDocketEntryBatch != lDocketBatch)
            {
                continue;
            }

            lDocketEntry.LDocketEntryBatch = lDocketFresh;
            lDocketEntry.LDocketEntryLocked = false;
            lDocketReleased++;
        }

        if (lDocketReleased > 0)
        {
            LDocketRaise();
        }

        return lDocketReleased;
    }

    public int LDocketDeliveredAdd(IReadOnlyList<string> lDocketPaths, Guid lDocketBatch, bool lDocketLocked)
    {
        int lDocketTracked = 0;
        var lDocketAddedItems = new List<LDocketEntry>();
        foreach (string lDocketPath in lDocketPaths)
        {
            LDocketEntry? lDocketEntry = LDocketItemFind(lDocketPath);
            if (lDocketEntry is null)
            {
                lDocketEntry = new LDocketEntry(lDocketPath, lDocketBatch)
                {
                    LDocketEntryDelivered = true,
                    LDocketEntryLocked = lDocketLocked
                };
                lDocketEntries.Add(lDocketEntry);
                lDocketAddedItems.Add(lDocketEntry);
                lDocketTracked++;
                continue;
            }

            if (lDocketEntry.LDocketEntryLocked && lDocketEntry.LDocketEntryBatch != lDocketBatch)
            {
                continue;
            }

            lDocketEntry.LDocketEntryBatch = lDocketBatch;
            lDocketEntry.LDocketEntryDelivered = true;
            lDocketEntry.LDocketEntryLocked |= lDocketLocked;
            lDocketTracked++;
        }

        if (lDocketTracked > 0)
        {
            LDocketRaise();
        }

        if (lDocketAddedItems.Count > 0)
        {
            LDocketAdded?.Invoke(lDocketAddedItems);
        }

        return lDocketTracked;
    }

    private void LDocketRaise() => LDocketChange?.Invoke(LDocketItemsRead());
}
