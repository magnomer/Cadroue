namespace Cadroue.Application;

public sealed record LTabsetSlot(
    Guid LTabsetId,
    string LTabsetLayoutKey,
    string LTabsetNameCustom);

public sealed record LTabsetTitlePlan(
    Guid LTabsetId,
    bool LTabsetCustom,
    int LTabsetOrdinal,
    bool LTabsetNumbered);

public static class LTabset
{
    public static IReadOnlyList<LTabsetTitlePlan> LTabsetTitleResolve(IReadOnlyList<LTabsetSlot> lTabsetSlots)
    {
        var lTabsetPlans = new List<LTabsetTitlePlan>(lTabsetSlots.Count);

        foreach (LTabsetSlot lTabsetSlot in lTabsetSlots.Where(lTabsetSlot => lTabsetSlot.LTabsetNameCustom.Length > 0))
        {
            lTabsetPlans.Add(new LTabsetTitlePlan(lTabsetSlot.LTabsetId, true, 0, false));
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

            for (int lTabsetPosition = 0; lTabsetPosition < lTabsetKindSlots.Count; lTabsetPosition++)
            {
                lTabsetPlans.Add(new LTabsetTitlePlan(
                    lTabsetKindSlots[lTabsetPosition].LTabsetId, false, lTabsetPosition + 1, true));
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
