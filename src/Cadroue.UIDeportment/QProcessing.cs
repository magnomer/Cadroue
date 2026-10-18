namespace Cadroue.UIDeportment;

public sealed record QProcessingRow(string QProcessingRowKey, string QProcessingRowIcon, string QProcessingRowLabel);

public sealed class QProcessing
{
    public const string QProcessingSkipStep = "No Processing";
    private const double QProcessingDragOpacity = 0.72;
    private const double QProcessingDisabledOpacity = 0.4;

    private readonly List<string> qProcessingSteps = [];
    private readonly List<QProcessingRow> qProcessingRows = [];
    private readonly HashSet<string> qProcessingActive = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string?> qProcessingNotices = new(StringComparer.Ordinal);
    private string? qProcessingStep;
    private bool qProcessingMinimized;
    private bool qProcessingOrdered;
    private bool qProcessingSkipActive;

    public QProcessing()
    {
        QProcessingDrag = new QProcessingDrag(this, () => QProcessingChanged?.Invoke());
    }

    public event Action? QProcessingChanged;
    public event Action? QProcessingOrderChanged;
    public event Action<string>? QProcessingStepChanged;
    public event Action<bool>? QProcessingMinimizeChanged;
    public event Action? QProcessingDragCanceled;

    public IReadOnlyList<string> QProcessingSteps => qProcessingSteps;

    public IReadOnlyList<QProcessingRow> QProcessingRows => qProcessingRows;

    public string? QProcessingStep => qProcessingStep;

    public bool QProcessingMinimized => qProcessingMinimized;

    public bool QProcessingOrdered => qProcessingOrdered;

    public bool QProcessingSkipActive => qProcessingSkipActive;

    public QProcessingDrag QProcessingDrag { get; }

    public bool QProcessingSkipSelected => qProcessingStep == QProcessingSkipStep;

    public bool QProcessingActiveCheck(string lStep) => qProcessingActive.Contains(lStep);

    public bool QProcessingEnabledCheck(string lStep) => !qProcessingNotices.ContainsKey(lStep);

    public string? QProcessingNoticeRead(string lStep) => qProcessingNotices.GetValueOrDefault(lStep);

    public bool QProcessingSelectedCheck(string lStep) => lStep == qProcessingStep && QProcessingEnabledCheck(lStep);

    public string QProcessingNumberRead(string lStep) => (qProcessingSteps.IndexOf(lStep) + 1).ToString();

    public string QProcessingNoticeFormat(string lStep) => QProcessingNoticeRead(lStep) ?? string.Empty;

    public double QProcessingOpacityRead(string lStep)
    {
        if (!QProcessingEnabledCheck(lStep))
        {
            return QProcessingDisabledOpacity;
        }

        return QProcessingDrag.QProcessingDragActive
            && QProcessingDrag.QProcessingDragIndex == qProcessingSteps.IndexOf(lStep)
            ? QProcessingDragOpacity
            : 1;
    }

    public double QProcessingSkipOpacity => qProcessingSkipActive ? QProcessingDisabledOpacity : 1;

    public void QProcessingStepAdd(string lStep) => qProcessingSteps.Add(lStep);

    public void QProcessingRowAdd(QProcessingRow lRow)
    {
        qProcessingRows.Add(lRow);
        qProcessingSteps.Add(lRow.QProcessingRowKey);
        QProcessingChanged?.Invoke();
    }

    public void QProcessingKeyHandle(string lStep, bool lActivate)
    {
        if (lActivate)
        {
            QProcessingStepSelect(lStep);
        }
    }

    internal int QProcessingIndexRead(string lStep) => qProcessingSteps.IndexOf(lStep);

    public static int QProcessingIndexResolve(double lY, IReadOnlyList<double> lTops, IReadOnlyList<double> lHeights)
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

    public void QProcessingActiveSet(string lStep, bool lActive)
    {
        bool lChanged = lActive ? qProcessingActive.Add(lStep) : qProcessingActive.Remove(lStep);
        if (lChanged)
        {
            QProcessingChanged?.Invoke();
        }
    }

    public void QProcessingEnabledSet(string lStep, bool lEnabled, string? lNotice = null)
    {
        bool lChanged = QProcessingNoticeSet(lStep, lEnabled, lNotice);
        QProcessingDragCancel(lStep, lEnabled);

        if (lChanged)
        {
            QProcessingChanged?.Invoke();
        }
    }

    private bool QProcessingNoticeSet(string lStep, bool lEnabled, string? lNotice)
    {
        bool lChanged = lEnabled
            ? qProcessingNotices.Remove(lStep)
            : !qProcessingNotices.TryGetValue(lStep, out string? lPrevious) || lPrevious != lNotice;
        if (!lEnabled)
        {
            qProcessingNotices[lStep] = lNotice;
        }

        return lChanged;
    }

    private void QProcessingDragCancel(string lStep, bool lEnabled)
    {
        if (!lEnabled && QProcessingDrag.QProcessingDragIndex == qProcessingSteps.IndexOf(lStep))
        {
            QProcessingDrag.QProcessingDragClear();
            QProcessingDragCanceled?.Invoke();
        }
    }

    public void QProcessingOrderedSet(bool lOrdered)
    {
        if (qProcessingOrdered == lOrdered)
        {
            return;
        }

        qProcessingOrdered = lOrdered;
        QProcessingChanged?.Invoke();
    }

    public void QProcessingSkipSet(bool lSkipActive)
    {
        if (qProcessingSkipActive == lSkipActive)
        {
            return;
        }

        qProcessingSkipActive = lSkipActive;
        QProcessingChanged?.Invoke();
    }

    public void QProcessingMinimizedSet(bool lMinimized)
    {
        if (qProcessingMinimized == lMinimized)
        {
            return;
        }

        qProcessingMinimized = lMinimized;
        QProcessingMinimizeChanged?.Invoke(lMinimized);
    }

    public bool QProcessingStepSelect(string lStep)
    {
        if (!QProcessingStepCheck(lStep))
        {
            return false;
        }

        QProcessingStepSet(lStep);
        return true;
    }

    private bool QProcessingStepCheck(string lStep) => lStep == QProcessingSkipStep || QProcessingEnabledCheck(lStep);

    private void QProcessingStepSet(string lStep)
    {
        qProcessingStep = lStep;
        QProcessingChanged?.Invoke();
        QProcessingStepChanged?.Invoke(lStep);
    }

    public bool QProcessingStepMove(int lDelta)
    {
        if (qProcessingStep is null || !QProcessingEnabledCheck(qProcessingStep))
        {
            return false;
        }

        int lIndex = qProcessingSteps.IndexOf(qProcessingStep);
        return lIndex >= 0 && QProcessingIndexMove(lIndex, lIndex + lDelta);
    }

    public bool QProcessingIndexMove(int lFrom, int lTo)
    {
        if (!QProcessingIndexCheck(lFrom, lTo))
        {
            return false;
        }

        QProcessingOrderApply(lFrom, lTo);
        return true;
    }

    private bool QProcessingIndexCheck(int lFrom, int lTo) =>
        lFrom >= 0 && lFrom < qProcessingSteps.Count
        && lTo >= 0 && lTo < qProcessingSteps.Count
        && lFrom != lTo
        && QProcessingEnabledCheck(qProcessingSteps[lTo]);

    private void QProcessingOrderApply(int lFrom, int lTo)
    {
        string lStep = qProcessingSteps[lFrom];
        qProcessingSteps.RemoveAt(lFrom);
        qProcessingSteps.Insert(lTo, lStep);
        QProcessingDrag.QProcessingDragAdjust(lFrom, lTo);
        QProcessingOrderChanged?.Invoke();
    }
}
