namespace Cadroue.Application;

public sealed class LDocket
{
    private readonly List<LDocketEntry> lDocketEntries = new();

    public event Action<IReadOnlyList<LDocketEntry>>? LDocketChange;

    public IReadOnlyList<LDocketEntry> LDocketItemsRead() => lDocketEntries.ToArray();

    public IReadOnlyList<LDocketEntry> LDocketUnlockedRead() =>
        lDocketEntries.Where(lDocketEntry => !lDocketEntry.LDocketEntryLocked).ToArray();

    public IReadOnlyList<string> LDocketPathsRead() =>
        lDocketEntries.Select(lDocketEntry => lDocketEntry.LDocketEntryPath).ToArray();

    public LDocketEntry? LDocketItemFind(string lDocketPath) =>
        lDocketEntries.FirstOrDefault(lDocketEntry =>
            string.Equals(lDocketEntry.LDocketEntryPath, lDocketPath, StringComparison.OrdinalIgnoreCase));

    public bool LDocketLockCheck(string lDocketPath) =>
        LDocketItemFind(lDocketPath)?.LDocketEntryLocked == true;

    public int LDocketPathsAdd(IReadOnlyList<string> lDocketPaths, Guid lDocketBatch = default, bool lDocketDelivered = false)
    {
        if (lDocketPaths.Count == 0)
        {
            return 0;
        }

        if (lDocketBatch == Guid.Empty)
        {
            lDocketBatch = LGate.LGateBatchCreate();
        }

        int lDocketAdded = 0;
        foreach (string lDocketPath in lDocketPaths)
        {
            if (LDocketItemFind(lDocketPath) is not null)
            {
                continue;
            }

            lDocketEntries.Add(new LDocketEntry(lDocketPath, lDocketBatch) { LDocketEntryDelivered = lDocketDelivered });
            lDocketAdded++;
        }

        if (lDocketAdded > 0)
        {
            LDocketRaise();
        }

        return lDocketAdded;
    }

    public int LDocketPathsRemove(IReadOnlyList<string> lDocketPaths)
    {
        HashSet<string> lDocketRemoved = lDocketPaths.ToHashSet(StringComparer.OrdinalIgnoreCase);
        int lDocketCount = lDocketEntries.RemoveAll(lDocketEntry => lDocketRemoved.Contains(lDocketEntry.LDocketEntryPath));
        if (lDocketCount > 0)
        {
            LDocketRaise();
        }

        return lDocketCount;
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

    public int LDocketDeliveredAdd(IReadOnlyList<string> lDocketPaths, Guid lDocketBatch)
    {
        int lDocketTracked = 0;
        foreach (string lDocketPath in lDocketPaths)
        {
            LDocketEntry? lDocketEntry = LDocketItemFind(lDocketPath);
            if (lDocketEntry is null)
            {
                lDocketEntries.Add(new LDocketEntry(lDocketPath, lDocketBatch)
                {
                    LDocketEntryDelivered = true,
                    LDocketEntryLocked = true
                });
                lDocketTracked++;
                continue;
            }

            if (!lDocketEntry.LDocketEntryLocked || lDocketEntry.LDocketEntryBatch != lDocketBatch)
            {
                lDocketEntry.LDocketEntryBatch = lDocketBatch;
                lDocketEntry.LDocketEntryDelivered = true;
                lDocketEntry.LDocketEntryLocked = true;
                lDocketTracked++;
            }
        }

        if (lDocketTracked > 0)
        {
            LDocketRaise();
        }

        return lDocketTracked;
    }

    private void LDocketRaise() => LDocketChange?.Invoke(LDocketItemsRead());
}
