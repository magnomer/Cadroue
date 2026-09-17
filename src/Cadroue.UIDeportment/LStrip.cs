using Cadroue.Application;

namespace Cadroue.UIDeportment;

public sealed class LStripTab
{
    public LStripTab(string lKey)
    {
        LStripTabId = Guid.NewGuid();
        LStripTabKey = lKey;
    }

    public Guid LStripTabId { get; }

    public string LStripTabKey { get; }

    public string LStripTabTitle { get; internal set; } = string.Empty;

    public string LStripTabCustom { get; internal set; } = string.Empty;

    public bool LStripTabSelected { get; internal set; }

    public bool LStripTabSeparator { get; internal set; }

    public bool LStripTabEditing { get; internal set; }
}

public sealed class LStrip
{
    private readonly List<LStripTab> lStripTabs = [];
    private readonly Func<string, string> lStripTitleSource;
    private readonly Func<string, int, string> lStripNumberSource;
    private LStripTab? lStripSelected;
    private LStripTab? lStripHovered;
    private bool lStripSuspended;
    private bool lStripVertical;

    public LStrip(Func<string, string> lTitleSource, Func<string, int, string> lNumberSource)
    {
        lStripTitleSource = lTitleSource;
        lStripNumberSource = lNumberSource;
    }

    public event Action? LStripChange;
    public event Action<LStripTab?>? LStripSelectChange;
    public event Action<LStripTab>? LStripTabChange;
    public event Action? LStripTitleChange;

    public IReadOnlyList<LStripTab> LStripTabs => lStripTabs;

    public LStripTab? LStripSelected => lStripSelected;

    public bool LStripSuspended => lStripSuspended;

    public bool LStripVertical => lStripVertical;

    public string LStripTitleRead(string lKey) => lStripTitleSource(lKey);

    public LStripTab? LStripTabFind(Guid lId) => lStripTabs.FirstOrDefault(lTab => lTab.LStripTabId == lId);

    public void LStripUpdateSuspend() => lStripSuspended = true;

    public void LStripUpdateResume() => lStripSuspended = false;

    public void LStripVerticalSet(bool lVertical)
    {
        if (lStripVertical == lVertical)
        {
            return;
        }

        lStripVertical = lVertical;
        LStripHoverClear(null);
    }

    public void LStripAdd(LStripTab lTab)
    {
        lStripTabs.Add(lTab);
        LStripChange?.Invoke();
        LStripTitleSet(lTab, lStripTitleSource(lTab.LStripTabKey));
        if (lStripSuspended)
        {
            return;
        }

        LStripTitleUpdate();
        if (lStripSelected is null)
        {
            LStripSelect(lTab);
        }
        else
        {
            LStripSeparatorUpdate();
        }
    }

    public void LStripTitleUpdate()
    {
        if (lStripSuspended)
        {
            return;
        }

        IReadOnlyList<LTabsetSlot> lSlots = lStripTabs
            .Select(lTab => new LTabsetSlot(lTab.LStripTabId, lTab.LStripTabKey, lTab.LStripTabCustom))
            .ToList();
        foreach (LTabsetTitlePlan lPlan in LTabset.LTabsetTitleResolve(lSlots))
        {
            if (LStripTabFind(lPlan.LTabsetId) is not { } lTab)
            {
                continue;
            }

            string lTitle = lPlan.LTabsetCustom
                ? lTab.LStripTabCustom
                : lPlan.LTabsetNumbered
                    ? lStripNumberSource(lStripTitleSource(lTab.LStripTabKey), lPlan.LTabsetOrdinal)
                    : lStripTitleSource(lTab.LStripTabKey);
            LStripTitleSet(lTab, lTitle);
        }

        LStripTitleChange?.Invoke();
    }

    private void LStripTitleSet(LStripTab lTab, string lTitle)
    {
        if (lTab.LStripTabTitle == lTitle)
        {
            return;
        }

        lTab.LStripTabTitle = lTitle;
        LStripTabChange?.Invoke(lTab);
    }

    public bool LStripNameSet(LStripTab lTab, string? lName)
    {
        string lTrimmed = (lName ?? string.Empty).Trim();
        if (lTrimmed.Length == 0
            || string.Equals(lTrimmed, lStripTitleSource(lTab.LStripTabKey), StringComparison.Ordinal))
        {
            lTab.LStripTabCustom = string.Empty;
            LStripTitleUpdate();
            return false;
        }

        var lTaken = lStripTabs
            .Where(lOther => !ReferenceEquals(lOther, lTab))
            .Select(lOther => lOther.LStripTabTitle)
            .ToList();
        lTab.LStripTabCustom = LTabset.LTabsetNameResolve(lTaken, lTrimmed, lStripNumberSource);
        LStripTitleUpdate();
        return true;
    }

    public void LStripEditSet(LStripTab lTab, bool lEditing)
    {
        if (lTab.LStripTabEditing == lEditing)
        {
            return;
        }

        lTab.LStripTabEditing = lEditing;
        LStripTabChange?.Invoke(lTab);
    }

    public void LStripHoverSet(LStripTab lTab)
    {
        if (ReferenceEquals(lStripHovered, lTab))
        {
            return;
        }

        lStripHovered = lTab;
        LStripSeparatorUpdate();
    }

    public void LStripHoverClear(LStripTab? lTab)
    {
        if (lStripHovered is null || (lTab is not null && !ReferenceEquals(lStripHovered, lTab)))
        {
            return;
        }

        lStripHovered = null;
        LStripSeparatorUpdate();
    }

    public void LStripSelect(LStripTab? lTab)
    {
        foreach (LStripTab lItem in lStripTabs)
        {
            bool lSelected = ReferenceEquals(lItem, lTab);
            if (lItem.LStripTabSelected != lSelected)
            {
                lItem.LStripTabSelected = lSelected;
                LStripTabChange?.Invoke(lItem);
            }
        }

        LStripSelectedSet(lTab);
        LStripSeparatorUpdate();
    }

    private void LStripSelectedSet(LStripTab? lTab)
    {
        if (ReferenceEquals(lStripSelected, lTab))
        {
            return;
        }

        lStripSelected = lTab;
        LStripSelectChange?.Invoke(lTab);
    }

    public bool LStripMove(LStripTab lTab, int lTargetIndex)
    {
        int lSourceIndex = lStripTabs.IndexOf(lTab);
        if (lSourceIndex < 0)
        {
            return false;
        }

        int lClamped = Math.Clamp(lTargetIndex, 0, lStripTabs.Count - 1);
        if (lSourceIndex == lClamped)
        {
            return false;
        }

        lStripTabs.RemoveAt(lSourceIndex);
        lStripTabs.Insert(lClamped, lTab);
        LStripChange?.Invoke();
        LStripSeparatorUpdate();
        return true;
    }

    public void LStripRemove(LStripTab lTab)
    {
        int lIndex = lStripTabs.IndexOf(lTab);
        if (lIndex < 0)
        {
            return;
        }

        bool lWasSelected = ReferenceEquals(lStripSelected, lTab);
        if (ReferenceEquals(lStripHovered, lTab))
        {
            lStripHovered = null;
        }

        lStripTabs.RemoveAt(lIndex);
        LStripChange?.Invoke();
        LStripTitleUpdate();
        if (!lWasSelected)
        {
            LStripSeparatorUpdate();
            return;
        }

        if (lStripTabs.Count == 0)
        {
            LStripSelectedSet(null);
            return;
        }

        LStripSelect(lStripTabs[LTabset.LTabsetNextResolve(lStripTabs.Count, lIndex)]);
    }

    public void LStripClear()
    {
        lStripHovered = null;
        lStripTabs.Clear();
        LStripChange?.Invoke();
        LStripSelectedSet(null);
    }

    private void LStripSeparatorUpdate()
    {
        int lSelectedIndex = lStripSelected is null ? -1 : lStripTabs.IndexOf(lStripSelected);
        int lHoveredIndex = lStripHovered is null ? -1 : lStripTabs.IndexOf(lStripHovered);
        for (int lIndex = 0; lIndex < lStripTabs.Count; lIndex++)
        {
            bool lSeparator = lIndex < lStripTabs.Count - 1
                && lIndex != lSelectedIndex
                && lIndex != lSelectedIndex - 1
                && lIndex != lHoveredIndex
                && lIndex != lHoveredIndex - 1;
            if (lStripTabs[lIndex].LStripTabSeparator == lSeparator)
            {
                continue;
            }

            lStripTabs[lIndex].LStripTabSeparator = lSeparator;
            LStripTabChange?.Invoke(lStripTabs[lIndex]);
        }
    }
}
