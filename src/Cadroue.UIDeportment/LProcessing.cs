namespace Cadroue.UIDeportment;

public sealed record LProcessingRow(string LProcessingRowKey, string LProcessingRowIcon, string LProcessingRowLabel);

public sealed class LProcessing
{
    public const string LProcessingSkipStep = "No Processing";

    private readonly List<string> lProcessingSteps = [];
    private readonly HashSet<string> lProcessingActive = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string?> lProcessingNotices = new(StringComparer.Ordinal);
    private string? lProcessingStep;
    private bool lProcessingMinimized;
    private bool lProcessingOrdered;
    private bool lProcessingSkipActive;
    private int? lProcessingDragIndex;

    public event Action? LProcessingChange;
    public event Action? LProcessingOrderChange;
    public event Action<string>? LProcessingStepChange;
    public event Action<bool>? LProcessingMinimizeChange;

    public IReadOnlyList<string> LProcessingSteps => lProcessingSteps;

    public string? LProcessingStep => lProcessingStep;

    public bool LProcessingMinimized => lProcessingMinimized;

    public bool LProcessingOrdered => lProcessingOrdered;

    public bool LProcessingSkipActive => lProcessingSkipActive;

    public int? LProcessingDragIndex => lProcessingDragIndex;

    public bool LProcessingActiveCheck(string lStep) => lProcessingActive.Contains(lStep);

    public bool LProcessingEnabledCheck(string lStep) => !lProcessingNotices.ContainsKey(lStep);

    public string? LProcessingNoticeRead(string lStep) => lProcessingNotices.GetValueOrDefault(lStep);

    public bool LProcessingSelectedCheck(string lStep) => lStep == lProcessingStep && LProcessingEnabledCheck(lStep);

    public void LProcessingStepAdd(string lStep) => lProcessingSteps.Add(lStep);

    public void LProcessingActiveSet(string lStep, bool lActive)
    {
        bool lChanged = lActive ? lProcessingActive.Add(lStep) : lProcessingActive.Remove(lStep);
        if (lChanged)
        {
            LProcessingChange?.Invoke();
        }
    }

    public void LProcessingEnabledSet(string lStep, bool lEnabled, string? lNotice = null)
    {
        bool lChanged = lEnabled
            ? lProcessingNotices.Remove(lStep)
            : !lProcessingNotices.TryGetValue(lStep, out string? lPrevious) || lPrevious != lNotice;
        if (!lEnabled)
        {
            lProcessingNotices[lStep] = lNotice;
        }

        if (lChanged)
        {
            LProcessingChange?.Invoke();
        }
    }

    public void LProcessingOrderedSet(bool lOrdered)
    {
        if (lProcessingOrdered == lOrdered)
        {
            return;
        }

        lProcessingOrdered = lOrdered;
        LProcessingChange?.Invoke();
    }

    public void LProcessingSkipSet(bool lSkipActive)
    {
        if (lProcessingSkipActive == lSkipActive)
        {
            return;
        }

        lProcessingSkipActive = lSkipActive;
        LProcessingChange?.Invoke();
    }

    public void LProcessingMinimizedSet(bool lMinimized)
    {
        if (lProcessingMinimized == lMinimized)
        {
            return;
        }

        lProcessingMinimized = lMinimized;
        LProcessingMinimizeChange?.Invoke(lMinimized);
    }

    public void LProcessingDragSet(int? lDragIndex) => lProcessingDragIndex = lDragIndex;

    public bool LProcessingStepSelect(string lStep)
    {
        if (lStep != LProcessingSkipStep && !LProcessingEnabledCheck(lStep))
        {
            return false;
        }

        lProcessingStep = lStep;
        LProcessingChange?.Invoke();
        LProcessingStepChange?.Invoke(lStep);
        return true;
    }

    public bool LProcessingStepMove(int lDelta)
    {
        if (lProcessingStep is null || !LProcessingEnabledCheck(lProcessingStep))
        {
            return false;
        }

        int lIndex = lProcessingSteps.IndexOf(lProcessingStep);
        return lIndex >= 0 && LProcessingIndexMove(lIndex, lIndex + lDelta);
    }

    public bool LProcessingIndexMove(int lFrom, int lTo)
    {
        if (lFrom < 0 || lFrom >= lProcessingSteps.Count
            || lTo < 0 || lTo >= lProcessingSteps.Count
            || lFrom == lTo
            || !LProcessingEnabledCheck(lProcessingSteps[lTo]))
        {
            return false;
        }

        string lStep = lProcessingSteps[lFrom];
        lProcessingSteps.RemoveAt(lFrom);
        lProcessingSteps.Insert(lTo, lStep);
        if (lProcessingDragIndex == lFrom)
        {
            lProcessingDragIndex = lTo;
        }

        LProcessingOrderChange?.Invoke();
        return true;
    }
}
