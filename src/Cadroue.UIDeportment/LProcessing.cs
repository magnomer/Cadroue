namespace Cadroue.UIDeportment;

public sealed record LProcessingRow(string LProcessingRowKey, string LProcessingRowIcon, string LProcessingRowLabel);

public sealed class LProcessing
{
    public const string LProcessingSkipStep = "No Processing";
    private const double LProcessingDragOpacity = 0.72;
    private const double LProcessingDisabledOpacity = 0.4;

    private readonly List<string> lProcessingSteps = [];
    private readonly List<LProcessingRow> lProcessingRows = [];
    private readonly HashSet<string> lProcessingActive = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string?> lProcessingNotices = new(StringComparer.Ordinal);
    private string? lProcessingStep;
    private bool lProcessingMinimized;
    private bool lProcessingOrdered;
    private bool lProcessingSkipActive;
    private int? lProcessingDragIndex;
    private double lProcessingDragX;
    private double lProcessingDragY;
    private bool lProcessingDragActive;

    public event Action? LProcessingChange;
    public event Action? LProcessingOrderChange;
    public event Action<string>? LProcessingStepChange;
    public event Action<bool>? LProcessingMinimizeChange;
    public event Action? LProcessingDragCancel;

    public IReadOnlyList<string> LProcessingSteps => lProcessingSteps;

    public IReadOnlyList<LProcessingRow> LProcessingRows => lProcessingRows;

    public string? LProcessingStep => lProcessingStep;

    public bool LProcessingMinimized => lProcessingMinimized;

    public bool LProcessingOrdered => lProcessingOrdered;

    public bool LProcessingSkipActive => lProcessingSkipActive;

    public int? LProcessingDragIndex => lProcessingDragIndex;

    public bool LProcessingDragActive => lProcessingDragActive;

    public bool LProcessingSkipSelected => lProcessingStep == LProcessingSkipStep;

    public bool LProcessingActiveCheck(string lStep) => lProcessingActive.Contains(lStep);

    public bool LProcessingEnabledCheck(string lStep) => !lProcessingNotices.ContainsKey(lStep);

    public string? LProcessingNoticeRead(string lStep) => lProcessingNotices.GetValueOrDefault(lStep);

    public bool LProcessingSelectedCheck(string lStep) => lStep == lProcessingStep && LProcessingEnabledCheck(lStep);

    public string LProcessingNumberRead(string lStep) => (lProcessingSteps.IndexOf(lStep) + 1).ToString();

    public string LProcessingNoticeFormat(string lStep) => LProcessingNoticeRead(lStep) ?? string.Empty;

    public double LProcessingOpacityRead(string lStep)
    {
        if (!LProcessingEnabledCheck(lStep))
        {
            return LProcessingDisabledOpacity;
        }

        return lProcessingDragActive && lProcessingDragIndex == lProcessingSteps.IndexOf(lStep)
            ? LProcessingDragOpacity
            : 1;
    }

    public double LProcessingSkipOpacity => lProcessingSkipActive ? LProcessingDisabledOpacity : 1;

    public void LProcessingStepAdd(string lStep) => lProcessingSteps.Add(lStep);

    public void LProcessingRowAdd(LProcessingRow lRow)
    {
        lProcessingRows.Add(lRow);
        lProcessingSteps.Add(lRow.LProcessingRowKey);
        LProcessingChange?.Invoke();
    }

    public void LProcessingKeyHandle(string lStep, bool lActivate)
    {
        if (lActivate)
        {
            LProcessingStepSelect(lStep);
        }
    }

    public void LProcessingDragStart(string lStep, double lX, double lY)
    {
        LProcessingStepSelect(lStep);
        lProcessingDragIndex = lProcessingSteps.IndexOf(lStep);
        lProcessingDragX = lX;
        lProcessingDragY = lY;
        lProcessingDragActive = false;
    }

    public void LProcessingDragMove(
        double lX,
        double lY,
        bool lPressed,
        double lMinimumX,
        double lMinimumY,
        IReadOnlyList<double> lTops,
        IReadOnlyList<double> lHeights)
    {
        if (!lProcessingOrdered || lProcessingDragIndex is not int lDragIndex || !lPressed)
        {
            return;
        }

        if (!lProcessingDragActive
            && Math.Abs(lX - lProcessingDragX) < lMinimumX
            && Math.Abs(lY - lProcessingDragY) < lMinimumY)
        {
            return;
        }

        bool lStarted = !lProcessingDragActive;
        lProcessingDragActive = true;
        if (lStarted)
        {
            LProcessingChange?.Invoke();
        }

        LProcessingIndexMove(lDragIndex, LProcessingIndexResolve(lY, lTops, lHeights));
    }

    public void LProcessingDragClear()
    {
        bool lShown = lProcessingDragActive;
        lProcessingDragIndex = null;
        lProcessingDragActive = false;
        if (lShown)
        {
            LProcessingChange?.Invoke();
        }
    }

    public static int LProcessingIndexResolve(double lY, IReadOnlyList<double> lTops, IReadOnlyList<double> lHeights)
    {
        for (int lIndex = 0; lIndex < lTops.Count; lIndex++)
        {
            if (lY < lTops[lIndex] + lHeights[lIndex] / 2)
            {
                return lIndex;
            }
        }

        return Math.Max(0, lTops.Count - 1);
    }

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

        if (!lEnabled && lProcessingDragIndex == lProcessingSteps.IndexOf(lStep))
        {
            LProcessingDragClear();
            LProcessingDragCancel?.Invoke();
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
