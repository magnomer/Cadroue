namespace Cadroue.Application;

public sealed record LTabsetSlot(
    Guid LTabsetId,
    string LTabsetLayoutKey,
    string LTabsetNameCustom,
    int LTabsetOrdinal);

public sealed record LTabsetTitlePlan(
    Guid LTabsetId,
    bool LTabsetCustom,
    int LTabsetOrdinal,
    bool LTabsetNumbered);

public static class LTabset
{
    public static int LTabsetOrdinalRead(IReadOnlyList<LTabsetSlot> lTabsetSlots, string lTabsetLayoutKey)
    {
        HashSet<int> lTabsetTaken = lTabsetSlots
            .Where(lTabsetSlot => string.Equals(lTabsetSlot.LTabsetLayoutKey, lTabsetLayoutKey, StringComparison.Ordinal))
            .Select(lTabsetSlot => lTabsetSlot.LTabsetOrdinal)
            .ToHashSet();

        int lTabsetOrdinal = 1;
        while (lTabsetTaken.Contains(lTabsetOrdinal))
        {
            lTabsetOrdinal++;
        }

        return lTabsetOrdinal;
    }

    public static IReadOnlyList<LTabsetTitlePlan> LTabsetTitleResolve(IReadOnlyList<LTabsetSlot> lTabsetSlots)
    {
        var lTabsetPlans = new List<LTabsetTitlePlan>(lTabsetSlots.Count);

        foreach (LTabsetSlot lTabsetSlot in lTabsetSlots.Where(lTabsetSlot => lTabsetSlot.LTabsetNameCustom.Length > 0))
        {
            lTabsetPlans.Add(new LTabsetTitlePlan(lTabsetSlot.LTabsetId, true, lTabsetSlot.LTabsetOrdinal, false));
        }

        foreach (IGrouping<string, LTabsetSlot> lTabsetGroup in lTabsetSlots
                     .Where(lTabsetSlot => lTabsetSlot.LTabsetNameCustom.Length == 0)
                     .GroupBy(lTabsetSlot => lTabsetSlot.LTabsetLayoutKey, StringComparer.Ordinal))
        {
            var lTabsetKindSlots = lTabsetGroup.ToList();
            if (lTabsetKindSlots.Count == 1)
            {
                lTabsetPlans.Add(new LTabsetTitlePlan(lTabsetKindSlots[0].LTabsetId, false, 1, false));
                continue;
            }

            foreach (LTabsetSlot lTabsetSlot in lTabsetKindSlots)
            {
                lTabsetPlans.Add(new LTabsetTitlePlan(lTabsetSlot.LTabsetId, false, lTabsetSlot.LTabsetOrdinal, true));
            }
        }

        return lTabsetPlans;
    }

    public static string LTabsetNameResolve(
        IReadOnlyCollection<string> lTabsetTakenTitles,
        string lTabsetCandidate,
        Func<string, int, string> lTabsetNumberFormat)
    {
        var lTabsetTaken = new HashSet<string>(lTabsetTakenTitles, StringComparer.OrdinalIgnoreCase);
        string lTabsetDistinct = lTabsetCandidate;
        int lTabsetAttempt = 2;
        while (lTabsetTaken.Contains(lTabsetDistinct))
        {
            lTabsetDistinct = lTabsetNumberFormat(lTabsetCandidate, lTabsetAttempt);
            lTabsetAttempt++;
        }

        return lTabsetDistinct;
    }

    public static int LTabsetNextResolve(int lTabsetCount, int lTabsetClosedIndex) =>
        Math.Min(lTabsetClosedIndex, lTabsetCount - 1);
}
